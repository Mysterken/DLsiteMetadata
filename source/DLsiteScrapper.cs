﻿using System;
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
using Newtonsoft.Json.Linq;
using Playnite.SDK;
using DLsiteMetadata.Exceptions;

namespace DLsiteMetadata;

public class DLsiteScrapper(ILogger logger)
{
    private const SupportedLanguages DefaultLanguage = SupportedLanguages.en_US;

    public const string SiteBaseUrl = "https://www.dlsite.com/";

    public async Task<DLsiteScrapperResult> ScrapGamePage
    (
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
            handler.CookieContainer.Add(
                new Uri(SiteBaseUrl),
                new Cookie("locale", language.ToString())
            );

            using var client = new HttpClient(handler);
            res = await client.GetAsync(url);
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

        var circle = document.QuerySelector("[itemprop='brand']")?.Text().Trim();
        result.Circle = circle;

        var ciEn = document.QuerySelector(".link_cien a")?.GetAttribute("href");
        if (ciEn is not null) result.Links.Add("Ci-en", ciEn);

        var table = document.GetElementById("work_outline");
        if (table is not null)
        {
            var tableHeaders = table.QuerySelectorAll("th");

            foreach (var header in tableHeaders)
            {
                if (header.TextContent.Contains(TranslationDictionary.ReleaseDate[language]))
                {
                    var releaseDateString = header.NextElementSibling?.Text().Trim();
                    DateTime releaseDate;

                    switch (language)
                    {
                        case SupportedLanguages.ja_JP:
                        case SupportedLanguages.zh_CN:
                        case SupportedLanguages.zh_TW:
                            DateTime.TryParseExact(releaseDateString,
                                ["yyyy'年'MM'月'dd'日'", "yyyy'年'MM'月'dd'日' H'時'"],
                                CultureInfo.InvariantCulture, DateTimeStyles.None, out releaseDate);
                            break;
                        case SupportedLanguages.en_US:
                            DateTime.TryParseExact(releaseDateString,
                                ["MMM/dd/yyyy", "MMM/dd/yyyy H"],
                                CultureInfo.InvariantCulture, DateTimeStyles.None, out releaseDate);
                            break;
                        case SupportedLanguages.ko_KR:
                            DateTime.TryParseExact(releaseDateString,
                                ["yyyy'년 'MM'월 'dd'일'", "yyyy'년 'MM'월 'dd'일 H'시'"],
                                CultureInfo.InvariantCulture, DateTimeStyles.None, out releaseDate);
                            break;
                        case SupportedLanguages.es_ES:
                            DateTime.TryParseExact(releaseDateString,
                                ["MM/dd/yyyy", "MM/dd/yyyy H"],
                                CultureInfo.InvariantCulture, DateTimeStyles.None, out releaseDate);
                            break;
                        case SupportedLanguages.de_DE:
                        case SupportedLanguages.fr_FR:
                        case SupportedLanguages.id_ID:
                        case SupportedLanguages.it_IT:
                        case SupportedLanguages.pt_BR:
                        case SupportedLanguages.sv_SE:
                        case SupportedLanguages.th_TH:
                        case SupportedLanguages.vi_VN:
                            DateTime.TryParseExact(releaseDateString,
                                ["dd/MM/yyyy", "dd/MM/yyyy H"],
                                CultureInfo.InvariantCulture, DateTimeStyles.None, out releaseDate);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(language), language, "Unsupported language");
                    }

                    result.ReleaseDate = releaseDate;
                    continue;
                }

                if (header.TextContent.Contains(TranslationDictionary.UpdateDate[language]))
                {
                    result.UpdateDate = header.NextElementSibling?.ChildNodes
                        .FirstOrDefault(node => node.NodeType == NodeType.Text)?.TextContent.Trim();
                    continue;
                }

                if (header.TextContent.Contains(TranslationDictionary.Series[language]))
                {
                    result.Series = header.NextElementSibling?.Text().Trim();
                    continue;
                }

                if (header.TextContent.Contains(TranslationDictionary.Author[language]))
                {
                    result.Author = header.NextElementSibling?
                        .QuerySelectorAll("a")
                        .Select(x => x.Text().Trim())
                        .ToList();
                    continue;
                }

                if (header.TextContent.Contains(TranslationDictionary.Scenario[language]))
                {
                    result.ScenarioWriters = header.NextElementSibling?
                        .QuerySelectorAll("a")
                        .Select(x => x.Text().Trim())
                        .ToList();
                    continue;
                }

                if (header.TextContent.Contains(TranslationDictionary.Illustration[language]))
                {
                    result.Illustrators = header.NextElementSibling?
                        .QuerySelectorAll("a")
                        .Select(x => x.Text().Trim())
                        .ToList();
                    continue;
                }

                if (header.TextContent.Contains(TranslationDictionary.VoiceActor[language]))
                {
                    result.VoiceActors = header.NextElementSibling?
                        .QuerySelectorAll("a")
                        .Select(x => x.Text().Trim())
                        .ToList();
                    continue;
                }

                if (header.TextContent.Contains(TranslationDictionary.Music[language]))
                {
                    result.MusicCreators = header.NextElementSibling?
                        .QuerySelectorAll("a")
                        .Select(x => x.Text().Trim())
                        .ToList();
                    continue;
                }

                if (header.TextContent.Contains(TranslationDictionary.Age[language]))
                {
                    var ageRating = header.NextElementSibling?.Text().Trim();

                    if (TranslationDictionary.AllAges[language].Contains(ageRating))
                        result.Age = DLsiteScrapperResult.AgeRating.AllAges;
                    else if (TranslationDictionary.R15[language].Contains(ageRating))
                        result.Age = DLsiteScrapperResult.AgeRating.RRated;
                    else if (TranslationDictionary.Adult[language].Contains(ageRating))
                        result.Age = DLsiteScrapperResult.AgeRating.Adult;
                    else
                        logger.Error($"Unknown age rating: {ageRating}");

                    continue;
                }

                if (header.TextContent.Contains(TranslationDictionary.ProductFormat[language]))
                {
                    var formatElements = header.NextElementSibling?.QuerySelectorAll("a") ??
                                         Enumerable.Empty<IElement>();

                    // Group formats by type (game vs. other) and extract text.
                    var formatsByType = formatElements
                        .Select(e => new
                        {
                            Text = e.QuerySelector("span")?.Text()?.Trim(),
                            IsGameType = e.GetAttribute("href")?.Contains("work_type") == true
                        })
                        .Where(item => !string.IsNullOrEmpty(item.Text))
                        .ToLookup(item => item.IsGameType);

                    result.GameProductFormat = formatsByType[true].Select(item => item.Text).ToList();
                    result.ProductFormat = formatsByType[false].Select(item => item.Text).ToList();
                    continue;
                }

                if (header.TextContent.Contains(TranslationDictionary.FileFormat[language]))
                {
                    result.FileFormat = header.NextElementSibling?
                        .QuerySelectorAll("span")
                        .Select(x => x.Text().Trim())
                        .ToList();
                    continue;
                }

                if (header.TextContent.Contains(TranslationDictionary.Miscellaneous[language]))
                {
                    result.Miscellaneous = header.NextElementSibling?
                        .QuerySelectorAll("span")
                        .Select(x => x.Text().Trim())
                        .ToList();
                    continue;
                }

                if (header.TextContent.Contains(TranslationDictionary.Genre[language]))
                {
                    var genres = header.NextElementSibling?
                        .QuerySelectorAll("a")
                        .Select(x => x.Text().Trim())
                        .ToList();

                    if (categoryMappingTarget == "Genres")
                        result.Genres = genres;
                    else if (categoryMappingTarget == "Tags")
                        result.Tags = genres;
                    
                    continue;
                }

                if (header.TextContent.Contains(TranslationDictionary.Filesize[language]))
                    result.Filesize = header.NextElementSibling?.Text().Trim();
            }
        }

        var images = ParseImages(document);
        result.MainImage = images.FirstOrDefault(x => x.Contains("_img_main"));
        result.Icon = result.MainImage?.Replace("_img_main", "_img_sam_mini");
        result.ProductImages = images;

        var descriptionHtml = document.QuerySelector("[itemprop='description']")?.InnerHtml;
        var description = descriptionHtml?.Replace("//img.dlsite.jp", "https://img.dlsite.jp");
        result.Description = Regex.Replace(description?.Trim(), @"^\s*$\n|\r", string.Empty, RegexOptions.Multiline);

        return result;
    }

    public async Task<List<DLsiteSearchResult>> ScrapSearchPage(
        string searchCategory,
        string query,
        int maxSearchResults = 30,
        SupportedLanguages language = DefaultLanguage
    )
    {
        var searchUrl = searchCategory switch
        {
            "maniax" =>
                "{0}/fsr/=/language/jp/sex_category[0]/male/keyword/{1}/work_category[0]/doujin/work_category[1]/books/work_category[2]/pc/work_category[3]/app/order[0]/trend/options_and_or/and/per_page/{2}/show_type/1/from/fs.header/?locale={3}",
            "home" =>
                "{0}/fsr/=/language/jp/keyword/{1}/age_category[0]/general/work_category[0]/doujin/work_category[1]/pc/work_category[2]/app/order[0]/trend/options_and_or/and/per_page/{2}/show_type/1/from/fs.header/?locale={3}",
            "soft" =>
                "{0}/fsr/=/language/jp/keyword/{1}/age_category[0]/general/order[0]/trend/options_and_or/and/per_page/{2}/from/fs.header/?locale={3}",
            "pro" =>
                "{0}/fsr/=/language/jp/sex_category[0]/male/keyword/{1}/work_category[0]/pc/order[0]/trend/options_and_or/and/per_page/{2}/show_type/1/from/fs.header/?locale={3}",
            _ =>
                "{0}/fsr/=/language/jp/keyword/{1}/age_category[0]/general/work_category[0]/doujin/work_category[1]/pc/work_category[2]/app/order[0]/trend/options_and_or/and/per_page/{2}/show_type/1/from/fs.header/?locale={3}"
        };

        query = query.Replace(" ", "+");

        searchUrl = string.Format(searchUrl, searchCategory, query, maxSearchResults, language.ToString());

        var client = new HttpClient();
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
            handler.CookieContainer.Add(
                new Uri("https://www.dlsite.com"),
                new Cookie("locale", language.ToString())
            );

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

        var imagesContainer = document.GetElementById("work_left");

        if (imagesContainer is null) return images;

        var imageElements = imagesContainer.QuerySelectorAll(".product-slider-data div")
            .Where(x => x.GetAttribute("data-src") is not null)
            .Where(x => x.GetAttribute("data-src")!.Contains("img.dlsite.jp"))
            .Select(x => x.GetAttribute("data-src"))
            .Select(x => x?.Replace("//img.dlsite.jp", "https://img.dlsite.jp"))
            .ToList();

        images.AddRange(imageElements);

        return images;
    }
}