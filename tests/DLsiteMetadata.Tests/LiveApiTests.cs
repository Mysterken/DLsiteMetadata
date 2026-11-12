using System;
using System.Linq;
using System.Threading.Tasks;
using DLsiteMetadata.Enums;
using DLsiteMetadata.Exceptions;
using DLsiteMetadata.Tests.TestSupport;
using Xunit;

namespace DLsiteMetadata.Tests;

/// <summary>Runs a test only when <c>DLSITE_LIVE_TESTS=1</c>, as it needs the real DLsite.</summary>
public sealed class LiveFactAttribute : FactAttribute
{
    public LiveFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("DLSITE_LIVE_TESTS") != "1")
            Skip = "Set DLSITE_LIVE_TESTS=1 to run tests against the real DLsite.";
    }
}

/// <summary>
///     Canary for changes on DLsite's side: the recorded fixtures cannot notice those, these can.
///     Run with <c>DLSITE_LIVE_TESTS=1</c>, e.g. on a schedule in CI.
/// </summary>
public class LiveApiTests
{
    private static Task<DLsiteScrapperResult> Scrap(string site, string id,
        SupportedLanguages language = SupportedLanguages.en_US)
    {
        return new DLsiteScrapper(new NullLogger()).ScrapGamePage(
            $"https://www.dlsite.com/{site}/work/=/product_id/{id}.html", language);
    }

    [LiveFact]
    public async Task Voice_work_is_complete()
    {
        var result = await Scrap("home", "RJ01005844");

        Assert.False(string.IsNullOrWhiteSpace(result.Title));
        Assert.Equal("Patra Suou", result.Circle);
        Assert.Equal(new DateTime(2023, 5, 26), result.ReleaseDate);
        Assert.NotEmpty(result.Genres);
        Assert.NotEmpty(result.VoiceActors);
        Assert.NotNull(result.MainImage);
        Assert.False(string.IsNullOrWhiteSpace(result.Description));
        Assert.NotNull(result.Rating);
    }

    [LiveFact]
    public async Task Game_lists_its_formats()
    {
        var result = await Scrap("home", "RJ104685");

        Assert.Equal(new[] { "Action" }, result.GameProductFormat);
        Assert.Contains("Music", result.ProductFormat);
    }

    [LiveFact]
    public async Task Product_on_another_site_is_found()
    {
        var result = await Scrap("maniax", "RJ01723044");

        Assert.Contains("/girls/", result.Links["DLsite"]);
    }

    [LiveFact]
    public async Task Every_locale_answers_with_genres_and_language_names()
    {
        foreach (SupportedLanguages language in Enum.GetValues(typeof(SupportedLanguages)))
        {
            var result = await Scrap("maniax", "RJ01034407", language);

            Assert.True(result.Genres?.Count > 0, language.ToString());
            Assert.True(result.SupportedLanguages?.Count > 0, language.ToString());
            Assert.False(string.IsNullOrWhiteSpace(result.Title), language.ToString());
        }
    }

    [LiveFact]
    public async Task Unknown_product_is_unavailable()
    {
        await Assert.ThrowsAsync<ProductUnavailableException>(() => Scrap("maniax", "RJ99999999"));
    }

    [LiveFact]
    public async Task Work_page_still_holds_the_product_payload()
    {
        // The fallback source: fails when DLsite changes how the page embeds its data.
        using var client = new System.Net.Http.HttpClient();
        client.DefaultRequestHeaders.Add("Cookie", "locale=en_US; loginchecked=1");
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

        var html = await client.GetStringAsync("https://www.dlsite.com/maniax/work/=/product_id/RJ01034407.html");
        var response = NuxtPayloadReader.TryReadProductResponse(html, "RJ01034407");

        Assert.NotNull(response);
        Assert.Equal("RJ01034407", (string)response["data"]["product_id"]);
        Assert.NotEmpty(DLsiteProductMapper
            .Map((Newtonsoft.Json.Linq.JObject)response["data"], SupportedLanguages.en_US, "Genres").Genres);
    }
}
