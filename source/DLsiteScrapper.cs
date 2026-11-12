using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Extensions;
using DLsiteMetadata.Enums;
using Playnite.SDK;

namespace DLsiteMetadata;

public class DLsiteScrapper(ILogger logger)
{
    private const SupportedLanguages DefaultLanguage = SupportedLanguages.en_US;

    public const string SiteBaseUrl = "https://www.dlsite.com/";

    private static readonly Regex ProductUrlRegex = new(
        @"dlsite\.com/(?<site>[a-z][a-z0-9-]*)/work/=/product_id/(?<id>[A-Z]{2}\d+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    ///     Gets a product's metadata through DLsite's product API (see <see cref="DLsiteProductApi" />).
    /// </summary>
    /// <param name="url">A DLsite product page URL, it only provides the product ID and the site it was found on.</param>
    /// <exception cref="Exceptions.ProductUnavailableException">The product does not exist or cannot be bought here.</exception>
    public async Task<DLsiteScrapperResult> ScrapGamePage(
        string url,
        SupportedLanguages language = DefaultLanguage,
        string categoryMappingTarget = "Genres"
    )
    {
        var match = ProductUrlRegex.Match(url ?? string.Empty);
        if (!match.Success) throw new ArgumentException($"Not a DLsite product URL: {url}", nameof(url));

        var productId = match.Groups["id"].Value.ToUpperInvariant();
        var site = match.Groups["site"].Value.ToLowerInvariant();

        var product = await new DLsiteProductApi(logger).FetchProductAsync(productId, site, language);

        return DLsiteProductMapper.Map(product, language, categoryMappingTarget);
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
}
