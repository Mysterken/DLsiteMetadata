using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using DLsiteMetadata.Enums;
using DLsiteMetadata.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Playnite.SDK;

namespace DLsiteMetadata;

/// <summary>
///     Fetches a product's data from DLsite.
///     The primary source is the JSON API DLsite's own frontend uses
///     (<c>/api/product/{site}/fetchProduct/{id}</c>). If that call fails for a technical reason, the identical data
///     is read from the payload embedded in the work page instead. Products that are gone or restricted are reported
///     as <see cref="ProductUnavailableException" /> in both cases and never trigger the fallback.
/// </summary>
public class DLsiteProductApi
{
    public const string SiteBaseUrl = "https://www.dlsite.com/";

    private const string UserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36";

    private static readonly Lazy<HttpClient> SharedClient = new(() =>
        new HttpClient(new HttpClientHandler { UseCookies = false }) { Timeout = TimeSpan.FromSeconds(30) });

    private readonly HttpClient _client;
    private readonly ILogger _logger;

    public DLsiteProductApi(ILogger logger, HttpClient client = null)
    {
        _logger = logger;
        _client = client ?? SharedClient.Value;
    }

    /// <returns>The product's <c>data</c> object.</returns>
    /// <exception cref="ProductUnavailableException">The product does not exist or cannot be bought here.</exception>
    public async Task<JObject> FetchProductAsync(string productId, string site, SupportedLanguages language)
    {
        try
        {
            return await FetchFromApiAsync(productId, site, language).ConfigureAwait(false);
        }
        catch (Exception ex) when (IsTechnicalFailure(ex))
        {
            _logger.Warn(ex, $"Product API failed for {productId}, reading the work page instead");
        }

        return await FetchFromPageAsync(productId, site, language).ConfigureAwait(false);
    }

    private async Task<JObject> FetchFromApiAsync(string productId, string site, SupportedLanguages language)
    {
        // A product lives under exactly one site; the API names the right one when asked under another.
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var url = $"{SiteBaseUrl}api/product/{site}/fetchProduct/{productId}" +
                      $"?product_id={productId}&product_type=work&locale={language}";

            var response = await GetAsync(url, language).ConfigureAwait(false);

            if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.NotFound)
                throw new DLsiteApiException($"Product API answered HTTP {(int)response.StatusCode}");

            var json = JObject.Parse(response.Body);
            var correctSite = ReadResponse(json, out var product);

            if (product != null) return product;

            if (correctSite == null || correctSite == site)
                throw new DLsiteApiException("Product API returned no product data");

            _logger.Info($"{productId} belongs to the {correctSite} site, not {site}");
            site = correctSite;
        }

        throw new DLsiteApiException("Product API kept redirecting to another site");
    }

    private async Task<JObject> FetchFromPageAsync(string productId, string site, SupportedLanguages language)
    {
        var url = $"{SiteBaseUrl}{site}/work/=/product_id/{productId}.html?locale={language}";
        var response = await GetAsync(url, language).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new ProductUnavailableException("Product not found on DLsite");

        if (response.StatusCode != HttpStatusCode.OK)
            throw new DLsiteApiException($"Work page answered HTTP {(int)response.StatusCode}");

        var json = NuxtPayloadReader.TryReadProductResponse(response.Body, productId);
        if (json == null) throw new DLsiteApiException("The work page holds no product data");

        ReadResponse(json, out var product);

        return product ?? throw new DLsiteApiException("The work page holds no product data");
    }

    /// <summary>
    ///     Checks a <c>{ meta, data }</c> response.
    /// </summary>
    /// <param name="product">The product data, <c>null</c> when the response does not carry any.</param>
    /// <returns>The site the product belongs to, when the response says it was asked under the wrong one.</returns>
    private static string ReadResponse(JObject response, out JObject product)
    {
        product = null;

        // Unused meta members are JSON null (a JValue, not a C# null), only objects can be indexed.
        var meta = response["meta"] as JObject;
        var code = meta?["code"]?.Type == JTokenType.Integer ? (int)meta["code"] : 0;

        var hiddenByCountry = (meta?["restriction"] as JObject)?["hiddenByCountry"];
        if (hiddenByCountry?.Type == JTokenType.Boolean && (bool)hiddenByCountry)
            throw new ProductUnavailableException("You cannot buy this product from the country/region you live in");

        if (code == 404)
        {
            var detail = (string)meta["errorDetail"];
            throw new ProductUnavailableException(string.IsNullOrWhiteSpace(detail) ? "Product not found on DLsite" : detail);
        }

        if (code == 400) return (string)(meta["correct"] as JObject)?["site_id"];

        if (code != 200) throw new DLsiteApiException($"Product API answered code {code}: {(string)meta?["errorType"]}");

        product = response["data"] as JObject;
        return null;
    }

    private async Task<(HttpStatusCode StatusCode, string Body)> GetAsync(string url, SupportedLanguages language)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.ParseAdd(UserAgent);
        // loginchecked skips the age check page, locale selects the language of the answer.
        request.Headers.Add("Cookie", $"locale={language}; loginchecked=1");

        using var response = await _client.SendAsync(request).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        return (response.StatusCode, body);
    }

    private static bool IsTechnicalFailure(Exception ex)
    {
        return ex is HttpRequestException || ex is JsonException || ex is DLsiteApiException ||
               ex is OperationCanceledException;
    }
}
