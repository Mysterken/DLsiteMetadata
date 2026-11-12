using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using DLsiteMetadata.Enums;
using Newtonsoft.Json.Linq;

namespace DLsiteMetadata;

/// <summary>
///     Maps the <c>data</c> object of DLsite's product API (<c>/api/product/{site}/fetchProduct/{id}</c>) onto
///     <see cref="DLsiteScrapperResult" />.
///     The API is already localized by the requested locale (titles, genres, work type, icon labels) and exposes
///     credits, age rating and series as structured fields, so no translated label has to be matched here.
///     Every field is optional: a missing or reshaped field only drops that field, never the whole result.
/// </summary>
public static class DLsiteProductMapper
{
    private const string SiteBaseUrl = "https://www.dlsite.com/";

    private static readonly TimeSpan JapanOffset = TimeSpan.FromHours(9);

    private static readonly Regex ProtocolRelativeImageHost = new(@"(?<!https:)//img\.dlsite\.jp");

    /// Product file types whose code is not a readable name.
    private static readonly Dictionary<string, string> FileFormatNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["IJP"] = "JPEG",
        ["FLC"] = "FLAC",
        ["EXE"] = "Application"
    };

    /// Icons describing additional contents of a product (music, voice, animation), listed as extra product formats.
    private static readonly HashSet<string> ExtraFormatIconClasses = new(StringComparer.OrdinalIgnoreCase)
        { "MS2", "SND", "MV2" };

    public static DLsiteScrapperResult Map(JObject product, SupportedLanguages language, string categoryMappingTarget)
    {
        if (product == null) throw new ArgumentNullException(nameof(product));

        var productId = Text(product["product_id"]);
        var result = new DLsiteScrapperResult
        {
            Title = Text(product["work_name"]),
            Circle = Text(Get(Get(product, "maker"), "maker_name")),
            Series = Text(Get(Get(product, "title"), "title_name")),
            Links = new Dictionary<string, string> { ["DLsite"] = ProductUrl(product, productId) },
            Age = MapAge(product["age_category"]),
            ReleaseDate = MapReleaseDate(product["release_date"]),
            Rating = MapRating(product["rating"]),
            Author = Credits(product, "created_by"),
            ScenarioWriters = Credits(product, "scenario_by"),
            Illustrators = Credits(product, "illust_by"),
            VoiceActors = Credits(product, "voice_by"),
            MusicCreators = Credits(product, "music_by"),
            Filesize = MapFileSize(product),
            SupportedLanguages = MapSupportedLanguages(product["work_languages"], language)
        };

        var genres = NonEmpty(Items(product["genres"]).Select(genre => Text(Get(genre, "genre_name"))));
        if (categoryMappingTarget == "Genres") result.Genres = genres;
        else if (categoryMappingTarget == "Tags") result.Tags = genres;

        var workType = Text(Get(Get(product, "work_type"), "work_type_name"));
        if (workType != null) result.GameProductFormat = new List<string> { workType };

        result.ProductFormat = NonEmpty(
            Items(product["icons"])
                .Where(icon => ExtraFormatIconClasses.Contains(Text(Get(icon, "class")) ?? string.Empty))
                .Select(icon => Text(Get(icon, "label")) ?? Text(Get(icon, "title"))));

        var fileType = Text(product["file_type"]);
        if (fileType != null)
            result.FileFormat = new List<string>
                { FileFormatNames.TryGetValue(fileType, out var fileFormat) ? fileFormat : fileType };

        var images = MapImages(product);
        result.ProductImages = images;
        result.MainImage = images.FirstOrDefault(image => image.Contains("_img_main")) ?? images.FirstOrDefault();
        result.Icon = result.MainImage?.Replace("_img_main", "_img_sam_mini");

        result.Description = BuildDescription(product);

        return result;
    }

    private static DLsiteScrapperResult.AgeRating? MapAge(JToken age)
    {
        switch (age?.Type == JTokenType.Integer ? (int)age : 0)
        {
            case 1: return DLsiteScrapperResult.AgeRating.AllAges;
            case 2: return DLsiteScrapperResult.AgeRating.RRated;
            case 3: return DLsiteScrapperResult.AgeRating.Adult;
            default: return null;
        }
    }

    /// <summary>The API gives the release as a unix timestamp of the day's start in Japan time (UTC+9).</summary>
    private static DateTime? MapReleaseDate(JToken releaseDate)
    {
        if (releaseDate == null || releaseDate.Type != JTokenType.Integer) return null;

        return DateTimeOffset.FromUnixTimeSeconds((long)releaseDate).ToOffset(JapanOffset).Date;
    }

    private static float? MapRating(JToken rating)
    {
        var count = Get(rating, "count")?.Type == JTokenType.Integer ? (int)Get(rating, "count") : 0;
        var average = Get(rating, "average");

        if (count <= 0 || average == null || (average.Type != JTokenType.Float && average.Type != JTokenType.Integer))
            return null;

        return (float)average;
    }

    private static List<string> Credits(JObject product, string role)
    {
        return NonEmpty(
            Items(product["creators"])
                .Where(group => Text(Get(group, "role")) == role)
                .SelectMany(group => Items(Get(group, "creators")))
                .Select(creator => Text(Get(creator, "name"))));
    }

    private static List<string> MapSupportedLanguages(JToken languageCodes, SupportedLanguages language)
    {
        return NonEmpty(
            Items(languageCodes)
                .Select(code => SupportedLanguageNames.TryGet(Text(code), language, out var name) ? name : null));
    }

    private static string MapFileSize(JObject product)
    {
        foreach (var field in new[] { "contents_file_size", "file_size" })
        {
            var size = product[field];

            if (size == null || (size.Type != JTokenType.Integer && size.Type != JTokenType.Float)) continue;

            var bytes = (double)size;
            if (bytes <= 0) continue;

            const double kilo = 1024;
            if (bytes >= kilo * kilo * kilo) return Format(bytes / (kilo * kilo * kilo), "GB");
            if (bytes >= kilo * kilo) return Format(bytes / (kilo * kilo), "MB");
            if (bytes >= kilo) return Format(bytes / kilo, "KB");
            return Format(bytes, "B");
        }

        return null;
    }

    private static string Format(double value, string unit)
    {
        return value.ToString("0.##", CultureInfo.InvariantCulture) + unit;
    }

    /// <summary>Product images, the key visual first, as absolute URLs.</summary>
    private static List<string> MapImages(JObject product)
    {
        var urls = new[] { Text(Get(Get(product, "image_main"), "original_url")) }
            .Concat(Items(product["image_samples"]).Select(sample => Text(Get(sample, "original_url"))));

        return urls
            .Where(url => url != null)
            .Select(AbsoluteUrl)
            .Distinct()
            .ToList();
    }

    /// <summary>
    ///     The product page's content blocks as HTML: text, images, image lists and track lists,
    ///     each with an optional heading. Falls back to the short description.
    /// </summary>
    private static string BuildDescription(JObject product)
    {
        var html = new StringBuilder();

        foreach (var block in Items(product["introductions"]))
        {
            var title = Text(Get(block, "title"));
            if (title != null) html.AppendLine($"<h3>{Encode(title)}</h3>");

            var body = Items(Get(block, "body")).ToList();

            if (Text(Get(block, "type")) == "track_list")
            {
                if (body.Count == 0) continue;

                html.AppendLine("<ol>");
                foreach (var track in body)
                {
                    var name = Text(Get(track, "name"));
                    var duration = Text(Get(track, "duration"));
                    if (name == null) continue;

                    html.AppendLine(duration == null
                        ? $"<li>{Encode(name)}</li>"
                        : $"<li>{Encode(name)} ({Encode(duration)})</li>");
                }

                html.AppendLine("</ol>");
                continue;
            }

            foreach (var item in body)
            {
                var imageUrl = Text(Get(Get(item, "image_info"), "url"));
                if (imageUrl != null) html.AppendLine($"<img src=\"{AbsoluteUrl(imageUrl)}\" />");

                // The text is already HTML (line breaks, links).
                var text = Text(Get(item, "intro"));
                if (text != null) html.AppendLine(text);
            }
        }

        var description = html.ToString().Trim();

        if (description.Length == 0)
            description = Text(product["description"]) ?? Text(product["short_introduction"]) ?? string.Empty;

        // Protocol-relative image links inside the HTML, then the blank lines and carriage returns.
        description = ProtocolRelativeImageHost.Replace(description, "https://img.dlsite.jp");
        description = Regex.Replace(description, @"^\s*$\n|\r", string.Empty, RegexOptions.Multiline).Trim();

        return description.Length == 0 ? null : description;
    }

    private static string ProductUrl(JObject product, string productId)
    {
        var url = Text(product["url"]);
        if (url != null) return url;

        var site = Text(product["site_id"]) ?? "home";
        return $"{SiteBaseUrl}{site}/work/=/product_id/{productId}.html";
    }

    private static string AbsoluteUrl(string url)
    {
        return url.StartsWith("//") ? "https:" + url : url;
    }

    /// <summary>Titles and track names may already contain HTML entities, normalize before encoding.</summary>
    private static string Encode(string text)
    {
        return WebUtility.HtmlEncode(WebUtility.HtmlDecode(text));
    }

    /// <summary>The trimmed string value of a token, <c>null</c> when missing, null or blank.</summary>
    private static string Text(JToken token)
    {
        if (token == null || token.Type == JTokenType.Null || token.Type == JTokenType.Undefined) return null;

        var text = token.Type == JTokenType.String ? (string)token : token.ToString();
        text = text?.Trim();

        return string.IsNullOrEmpty(text) ? null : text;
    }

    /// <summary>A child of a JSON object, <c>null</c> when the token is missing, null or not an object.</summary>
    private static JToken Get(JToken token, string key)
    {
        return token is JObject obj ? obj[key] : null;
    }

    /// <summary>The items of a JSON array, nothing when the token is missing or not an array.</summary>
    private static IEnumerable<JToken> Items(JToken token)
    {
        return token is JArray array ? array.Where(item => item != null && item.Type != JTokenType.Null) : Enumerable.Empty<JToken>();
    }

    private static List<string> NonEmpty(IEnumerable<string> values)
    {
        var list = values.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).ToList();

        return list.Count == 0 ? null : list;
    }
}
