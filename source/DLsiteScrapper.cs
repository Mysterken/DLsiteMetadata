using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Extensions;
using DLsiteMetadata.Enums;
using DLsiteMetadata.Exceptions;
using Newtonsoft.Json.Linq;
using Playnite.SDK;

namespace DLsiteMetadata;

public class DLsiteScrapper(ILogger logger)
{
    private const SupportedLanguages DefaultLanguage = SupportedLanguages.en_US;

    public const string SiteBaseUrl = "https://www.dlsite.com/";

    public async Task<DLsiteScrapperResult> ScrapGamePage(
        string url,
        SupportedLanguages language = DefaultLanguage,
        string categoryMappingTarget = "Genres"
    )
    {
        var result = new DLsiteScrapperResult
        {
            Links = new Dictionary<string, string>()
        };

        HttpResponseMessage res;

        try
        {
            var handler = new HttpClientHandler { UseCookies = true, CookieContainer = new CookieContainer() };
            var baseUri = new Uri(SiteBaseUrl);

            handler.CookieContainer.Add(baseUri, new Cookie("locale", language.ToString(), "/", ".dlsite.com"));
            handler.CookieContainer.Add(baseUri, new Cookie("loginchecked", "1", "/", ".dlsite.com"));

            using var client = new HttpClient(handler);
            res = await client.GetAsync(url);

            // Withdrawn or unknown products answer 404 with a regular DLsite error page.
            if (res.StatusCode == HttpStatusCode.NotFound)
            {
                var notFoundBody = await res.Content.ReadAsStringAsync();
                var notFoundDocument = await BrowsingContext.New(Configuration.Default)
                    .OpenAsync(req => req.Content(notFoundBody));
                var notFoundMessage = notFoundDocument.QuerySelector(".p-errorPc__title")?.Text().Trim();

                logger.Warn($"Product not found: {url}");
                throw new ProductUnavailableException(
                    string.IsNullOrEmpty(notFoundMessage) ? "Product not found on DLsite" : notFoundMessage);
            }

            res.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException e)
        {
            logger.Error(e, $"Failed to fetch URL: {url}");
            throw;
        }

        result.Links.Add("DLsite", res.RequestMessage.RequestUri.ToString()); // for redirects
        var responseBody = await res.Content.ReadAsStringAsync();

        var context = BrowsingContext.New(Configuration.Default);
        var document = await context.OpenAsync(req => req.Content(responseBody));

        var productIdMatch = Regex.Match(url, @"product_id/([A-Z]{2}\d+)");
        var productId = productIdMatch.Success ? productIdMatch.Groups[1].Value : null;

        // Products that cannot be bought from the user's country/region still answer 200,
        // but only render a restriction notice instead of the product.
        var restrictionMessage = document.QuerySelector(".p-restricted .error_message")?.Text().Trim();
        if (!string.IsNullOrEmpty(restrictionMessage))
        {
            logger.Warn($"Product unavailable: {restrictionMessage}");
            throw new ProductUnavailableException(restrictionMessage);
        }

        var errorBox = document.QuerySelector(".error_box_work");
        if (errorBox != null)
        {
            var errorMessage = errorBox.QuerySelector(".error_large_text")?.TextContent ?? "Unknown error";
            var detailMessage = errorBox.QuerySelector(".title_text")?.TextContent;

            var fullErrorMessage = string.IsNullOrEmpty(detailMessage)
                ? errorMessage
                : $"{errorMessage} : {detailMessage}";

            logger.Warn($"Product unavailable: {fullErrorMessage}");
            throw new ProductUnavailableException(fullErrorMessage.Trim());
        }

        if (!string.IsNullOrEmpty(productId))
        {
            // Rating is not available in the raw HTML — it's dynamically loaded via JavaScript.
            // Use JSON API instead for reliable access.
            var (jsonRating, jsonTitle) = await FetchJsonInfoAsync(productId, language);

            if (jsonRating.HasValue)
                result.Rating = (float)jsonRating.Value;
            if (!string.IsNullOrWhiteSpace(jsonTitle))
                result.Title = jsonTitle;
        }

        result.Title ??= document.GetElementById("work_name")?.Text().Trim();

        var productLd = ParseProductJsonLd(document);

        result.Title ??= productLd?.Value<string>("name")?.Trim();

        result.Circle = document.QuerySelector(".c-productInfo__makerName")?.Text().Trim()
                        ?? productLd?["brand"]?.Value<string>("name")?.Trim();

        var ciEn = document.QuerySelector(".cienLink a, .link_cien a")?.GetAttribute("href");
        if (ciEn is not null) result.Links.Add("Ci-en", ciEn);

        // The work outline is rendered as <dt class="c-productInfo__key"> / <dd> pairs
        // (the former #work_outline table no longer exists).
        var outlineKeys = document.QuerySelectorAll("dt.c-productInfo__key");
        if (outlineKeys.Length > 0)
        {
            var tableHeaders = outlineKeys;

            foreach (var header in tableHeaders)
            {
                if (HeaderMatches(header, TranslationDictionary.ReleaseDate[language]))
                {
                    // The displayed text is formatted differently for every locale, but the
                    // link wrapping it always points to ".../year/YYYY/mon/MM/day/DD/...".
                    var releaseDateHref = header.NextElementSibling?.QuerySelector("a")?.GetAttribute("href");
                    var releaseDateMatch = releaseDateHref is null
                        ? Match.Empty
                        : ReleaseDateHrefRegex.Match(releaseDateHref);

                    if (releaseDateMatch.Success)
                        result.ReleaseDate = new DateTime(
                            int.Parse(releaseDateMatch.Groups[1].Value),
                            int.Parse(releaseDateMatch.Groups[2].Value),
                            int.Parse(releaseDateMatch.Groups[3].Value));
                    else
                        logger.Warn($"Unable to parse release date for {url}");

                    continue;
                }

                if (HeaderMatches(header, TranslationDictionary.UpdateDate[language]))
                {
                    result.UpdateDate = header.NextElementSibling?.ChildNodes
                        .FirstOrDefault(node => node.NodeType == NodeType.Text)?.TextContent.Trim();
                    continue;
                }

                if (HeaderMatches(header, TranslationDictionary.Series[language]))
                {
                    result.Series = header.NextElementSibling?.Text().Trim();
                    continue;
                }

                if (HeaderMatches(header, TranslationDictionary.Author[language]))
                {
                    result.Author = header.NextElementSibling?
                        .QuerySelectorAll("a")
                        .Select(x => x.Text().Trim())
                        .ToList();
                    continue;
                }

                if (HeaderMatches(header, TranslationDictionary.Scenario[language]))
                {
                    result.ScenarioWriters = header.NextElementSibling?
                        .QuerySelectorAll("a")
                        .Select(x => x.Text().Trim())
                        .ToList();
                    continue;
                }

                if (HeaderMatches(header, TranslationDictionary.Illustration[language]))
                {
                    result.Illustrators = header.NextElementSibling?
                        .QuerySelectorAll("a")
                        .Select(x => x.Text().Trim())
                        .ToList();
                    continue;
                }

                if (HeaderMatches(header, TranslationDictionary.VoiceActor[language]))
                {
                    result.VoiceActors = header.NextElementSibling?
                        .QuerySelectorAll("a")
                        .Select(x => x.Text().Trim())
                        .ToList();
                    continue;
                }

                if (HeaderMatches(header, TranslationDictionary.Music[language]))
                {
                    result.MusicCreators = header.NextElementSibling?
                        .QuerySelectorAll("a")
                        .Select(x => x.Text().Trim())
                        .ToList();
                    continue;
                }

                if (HeaderMatches(header, TranslationDictionary.Age[language]))
                {
                    var ageRating = header.NextElementSibling?.Text().Trim();
                    var ageLabelClasses = header.NextElementSibling?.QuerySelector("[class^='label_']")?.ClassName;

                    // The label's CSS class is language independent, prefer it over the translated text.
                    if (ageLabelClasses?.Contains("label_all") == true)
                        result.Age = DLsiteScrapperResult.AgeRating.AllAges;
                    else if (ageLabelClasses?.Contains("label_r15") == true)
                        result.Age = DLsiteScrapperResult.AgeRating.RRated;
                    else if (ageLabelClasses?.Contains("label_r18") == true)
                        result.Age = DLsiteScrapperResult.AgeRating.Adult;
                    else if (ageRating is null)
                        logger.Error("Unknown age rating: <missing>");
                    else if (ContainsIgnoreCase(TranslationDictionary.AllAges[language], ageRating))
                        result.Age = DLsiteScrapperResult.AgeRating.AllAges;
                    else if (ContainsIgnoreCase(TranslationDictionary.R15[language], ageRating))
                        result.Age = DLsiteScrapperResult.AgeRating.RRated;
                    else if (ContainsIgnoreCase(TranslationDictionary.Adult[language], ageRating))
                        result.Age = DLsiteScrapperResult.AgeRating.Adult;
                    else
                        logger.Error($"Unknown age rating: {ageRating}");

                    continue;
                }

                if (HeaderMatches(header, TranslationDictionary.ProductFormat[language]))
                {
                    // The main format is a link to the work type (Action, Manga, Voice / ASMR...),
                    // additional formats (Music, Video...) are plain labels.
                    var formatElements = header.NextElementSibling?.QuerySelectorAll("a, span[class^='label_']") ??
                                         Enumerable.Empty<IElement>();

                    // Group formats by type (game vs. other) and extract text.
                    var formatsByType = formatElements
                        .Select(e => new
                        {
                            Text = e.Text()?.Trim(),
                            IsGameType = e.GetAttribute("href")?.Contains("work_type") == true
                        })
                        .Where(item => !string.IsNullOrEmpty(item.Text))
                        .ToLookup(item => item.IsGameType);

                    result.GameProductFormat = formatsByType[true].Select(item => item.Text).ToList();
                    result.ProductFormat = formatsByType[false].Select(item => item.Text).ToList();
                    continue;
                }

                if (HeaderMatches(header, TranslationDictionary.FileFormat[language]))
                {
                    result.FileFormat = header.NextElementSibling?
                        .QuerySelectorAll("span")
                        .Select(x => x.Text().Trim())
                        .ToList();
                    continue;
                }

                if (HeaderMatches(header, TranslationDictionary.Miscellaneous[language]))
                {
                    result.Miscellaneous = header.NextElementSibling?
                        .QuerySelectorAll("a, span[class^='label_']")
                        .Select(x => x.Text().Trim())
                        .ToList();
                    continue;
                }

                if (HeaderMatches(header, TranslationDictionary.GameSupportedLanguages[language]))
                {
                    result.SupportedLanguages = header.NextElementSibling?
                        .QuerySelectorAll("a")
                        .Select(x => x.Text().Trim())
                        .ToList();
                    continue;
                }

                if (HeaderMatches(header, TranslationDictionary.Genre[language]))
                {
                    // Only genre links: the cell may also hold campaign badges (e.g. DLsite Awards).
                    var genres = header.NextElementSibling?
                        .QuerySelectorAll("a[href*='/genre/']")
                        .Select(x => x.Text().Trim())
                        .ToList();

                    if (categoryMappingTarget == "Genres")
                        result.Genres = genres;
                    else if (categoryMappingTarget == "Tags")
                        result.Tags = genres;

                    continue;
                }

                if (HeaderMatches(header, TranslationDictionary.Filesize[language]))
                    result.Filesize = header.NextElementSibling?.Text().Trim();
            }
        }

        var images = ParseImages(document);
        if (images.Count == 0) images = ParseJsonLdImages(productLd);
        result.MainImage = images.FirstOrDefault(x => x.Contains("_img_main"));
        result.Icon = result.MainImage?.Replace("_img_main", "_img_sam_mini");
        result.ProductImages = images;

        var description = ParseDescription(document)
                          ?? productLd?.Value<string>("description")?.Trim();

        result.Description = string.IsNullOrWhiteSpace(description)
            ? null
            : Regex.Replace(description.Replace("//img.dlsite.jp", "https://img.dlsite.jp").Trim(),
                @"^\s*$\n|\r", string.Empty, RegexOptions.Multiline);

        return result;
    }

    private static readonly Regex ReleaseDateHrefRegex = new(@"year/(\d{4})/mon/(\d{1,2})/day/(\d{1,2})");

    private static bool ContainsIgnoreCase(string haystack, string needle)
    {
        return haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// DLsite's capitalisation of the outline labels differs from locale to locale (and over time).
    private static bool HeaderMatches(IElement header, string label)
    {
        return ContainsIgnoreCase(header.TextContent, label);
    }

    /// The Product entry of the page's schema.org JSON-LD blocks (title, circle, short description, images).
    private static JObject ParseProductJsonLd(IDocument document)
    {
        foreach (var script in document.QuerySelectorAll("script[type='application/ld+json']"))
            try
            {
                if (JToken.Parse(script.TextContent) is JObject obj && obj.Value<string>("@type") == "Product")
                    return obj;
            }
            catch (Exception)
            {
                // Malformed JSON-LD block, try the next one.
            }

        return null;
    }

    private static List<string> ParseJsonLdImages(JObject productLd)
    {
        return (productLd?["image"] as JArray)?
               .Select(x => x.Value<string>())
               .Where(x => x != null && x.Contains("img.dlsite.jp"))
               .Distinct()
               .ToList()
               ?? new List<string>();
    }

    /// Builds the description from the page's content sections (an optional heading followed by free-form HTML).
    private static string ParseDescription(IDocument document)
    {
        var sections = document.QuerySelectorAll(".c-productInfo__inner section.c-productContent");
        var html = new System.Text.StringBuilder();

        foreach (var section in sections)
        {
            var title = section.QuerySelector(".headline__title")?.Text().Trim();
            if (!string.IsNullOrEmpty(title)) html.AppendLine($"<h3>{WebUtility.HtmlEncode(title)}</h3>");

            var body = (IElement)section.Clone(true);
            foreach (var headline in body.QuerySelectorAll(".headline").ToList()) headline.Remove();

            html.AppendLine(body.InnerHtml);
        }

        // Drop Vue's hydration comments and scoped-style attributes, they only add noise.
        var cleaned = Regex.Replace(html.ToString(), @"<!--[\s\S]*?-->|\sdata-v-[0-9a-f]+(=""[^""]*"")?", string.Empty);

        return string.IsNullOrWhiteSpace(cleaned) ? null : cleaned;
    }

    public async Task<List<DLsiteSearchResult>> ScrapSearchPage(
        string searchCategory,
        string query,
        int maxSearchResults = 30,
        SupportedLanguages language = DefaultLanguage,
        bool includeAiGeneratedWorks = false
    )
    {
        var aiFilter = includeAiGeneratedWorks ? "/work_category[4]/ai" : string.Empty;

        var searchUrl = searchCategory switch
        {
            "maniax" =>
                "{0}/fsr/=/language/jp/sex_category[0]/male/keyword/{1}/work_category[0]/doujin/work_category[1]/books/work_category[2]/pc/work_category[3]/app" +
                aiFilter + "/order[0]/trend/options_and_or/and/per_page/{2}/show_type/1/from/fs.header/?locale={3}",
            "home" =>
                "{0}/fsr/=/language/jp/keyword/{1}/age_category[0]/general/work_category[0]/doujin/work_category[1]/pc/work_category[2]/app" +
                aiFilter + "/order[0]/trend/options_and_or/and/per_page/{2}/show_type/1/from/fs.header/?locale={3}",
            "soft" =>
                "{0}/fsr/=/language/jp/keyword/{1}/age_category[0]/general/work_category[0]/pc" + aiFilter +
                "/order[0]/trend/options_and_or/and/per_page/{2}/from/fs.header/?locale={3}",
            "pro" =>
                "{0}/fsr/=/language/jp/sex_category[0]/male/keyword/{1}/work_category[0]/pc" + aiFilter +
                "/order[0]/trend/options_and_or/and/per_page/{2}/show_type/1/from/fs.header/?locale={3}",
            _ =>
                "{0}/fsr/=/language/jp/keyword/{1}/age_category[0]/general/work_category[0]/doujin/work_category[1]/pc/work_category[2]/app" +
                aiFilter + "/order[0]/trend/options_and_or/and/per_page/{2}/show_type/1/from/fs.header/?locale={3}"
        };

        query = query.Replace(" ", "+");

        searchUrl = string.Format(searchUrl, searchCategory, query, maxSearchResults, language.ToString());

        var handler = new HttpClientHandler { UseCookies = true, CookieContainer = new CookieContainer() };
        var baseUri = new Uri(SiteBaseUrl);

        handler.CookieContainer.Add(baseUri, new Cookie("locale", language.ToString(), "/", ".dlsite.com"));
        handler.CookieContainer.Add(baseUri, new Cookie("loginchecked", "1", "/", ".dlsite.com"));

        using var client = new HttpClient(handler);
        var responseBody = await client.GetStringAsync(SiteBaseUrl + searchUrl);

        var context = BrowsingContext.New(Configuration.Default);
        var document = await context.OpenAsync(req => req.Content(responseBody));

        var searchResults = new List<DLsiteSearchResult>();

        var searchResultsTable = document.QuerySelector(".work_1col_table.n_worklist");

        if (searchResultsTable is null) return searchResults;

        var searchResultsRows = searchResultsTable.QuerySelectorAll("tr");

        searchResults.AddRange(from row in searchResultsRows
            let title = row.QuerySelector(".work_name a")?.Text().Trim()
            let link = row.QuerySelector(".work_name a")?.GetAttribute("href")
            let excerpt = row.QuerySelector(".work_text")?.Text().Trim()
            select new DLsiteSearchResult { Title = title, Link = link, Excerpt = excerpt });

        return searchResults;
    }

    /// Fetches work_name and rating from DLsite's JSON API (uses locale-sensitive data).
    /// This is more reliable than scraping masked HTML nodes.
    private async Task<(double? Rating, string WorkName)> FetchJsonInfoAsync(string productId,
        SupportedLanguages language)
    {
        try
        {
            var handler = new HttpClientHandler { UseCookies = true, CookieContainer = new CookieContainer() };
            var baseUri = new Uri("https://www.dlsite.com");

            handler.CookieContainer.Add(baseUri, new Cookie("locale", language.ToString(), "/", ".dlsite.com"));
            handler.CookieContainer.Add(baseUri, new Cookie("loginchecked", "1", "/", ".dlsite.com"));

            using var client = new HttpClient(handler);
            var url = $"https://www.dlsite.com/maniax/product/info/ajax?product_id={productId}&cdn_cache_min=1";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36 Edg/136.0.0.0");
            request.Headers.Referrer =
                new Uri($"https://www.dlsite.com/maniax/work/=/product_id/{productId}.html?locale={language}");
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var productNode = JObject.Parse(json)[productId];

            return (
                productNode?["rate_average_2dp"]?.Value<float>(),
                productNode?["work_name"]?.Value<string>()
            );
        }
        catch (Exception ex)
        {
            logger.Warn(ex, $"Failed to fetch JSON data for {productId}");
            return (null, null);
        }
    }


    private static List<string> ParseImages(IDocument document)
    {
        var images = new List<string>();

        // Full size images of the key visual slider (the thumbnail strip uses resized copies and is excluded).
        var imageElements = document.QuerySelectorAll(".work_slider .key-visual-slider img")
            .Select(x => x.GetAttribute("src"))
            .Where(x => x != null && x.Contains("img.dlsite.jp"))
            .Select(x => x.StartsWith("//") ? "https:" + x : x)
            .Distinct()
            .ToList();

        images.AddRange(imageElements);

        return images;
    }
}