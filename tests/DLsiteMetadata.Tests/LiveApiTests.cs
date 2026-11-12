using System;
using System.Collections.Generic;
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
///     <para>
///         The tests check the shape of the answers (a field exists and is plausible), never exact values, and
///         spread over several works: a work that gets delisted, renamed or hidden in the region of the machine
///         must not turn the canary red, only a change of DLsite's formats should.
///     </para>
/// </summary>
public class LiveApiTests
{
    /// <summary>
    ///     A few long-standing works of different kinds. The site is the one the work is asked under, which for
    ///     the girls work is deliberately a wrong one.
    /// </summary>
    private static readonly Work[] Works =
    {
        new Work("voice", "home", "RJ01005844"),
        new Work("game", "home", "RJ104685"),
        new Work("cg", "maniax", "RJ01034407"),
        new Work("manga", "maniax", "RJ01073400"),
        new Work("music and series", "maniax", "RJ01753547"),
        new Work("work of another site", "maniax", "RJ01723044")
    };

    private sealed class Work
    {
        public Work(string kind, string site, string id)
        {
            Kind = kind;
            Site = site;
            Id = id;
        }

        public string Kind { get; }
        public string Site { get; }
        public string Id { get; }
    }

    /// <summary>How many of <see cref="Works" /> must still be available for the run to say anything.</summary>
    private const int MinimumAvailableWorks = 4;

    private static Task<DLsiteScrapperResult> Scrap(string site, string id,
        SupportedLanguages language = SupportedLanguages.en_US)
    {
        return new DLsiteScrapper(new NullLogger()).ScrapGamePage(
            $"https://www.dlsite.com/{site}/work/=/product_id/{id}.html", language);
    }

    /// <summary>The first of <see cref="Works" /> that DLsite still offers, with its result.</summary>
    private static async Task<Work> FirstAvailableWork()
    {
        foreach (var work in Works)
            try
            {
                await Scrap(work.Site, work.Id);
                return work;
            }
            catch (ProductUnavailableException)
            {
            }

        throw new Xunit.Sdk.XunitException("None of the pinned works is available anymore, pick new ones.");
    }

    [LiveFact]
    public async Task Works_keep_the_expected_shape()
    {
        var problems = new List<string>();
        var available = 0;

        foreach (var work in Works)
            try
            {
                var kind = work.Kind;
                var id = work.Id;
                var result = await Scrap(work.Site, id);
                available++;

                void Expect(bool condition, string what)
                {
                    if (!condition) problems.Add($"{kind} {id}: {what}");
                }

                Expect(!string.IsNullOrWhiteSpace(result.Title), "no title");
                Expect(!string.IsNullOrWhiteSpace(result.Circle), "no circle");
                Expect(result.ReleaseDate is { } date && date.Year >= 1990 && date <= DateTime.UtcNow.AddDays(30),
                    "release date missing or implausible");
                Expect(result.Age.HasValue, "no age rating");
                Expect(result.MainImage?.StartsWith("https://") == true, "main image is not an absolute URL");
                Expect(result.Links != null && result.Links.TryGetValue("DLsite", out var link) &&
                       link.Contains(id), "no DLsite link to the work");
                Expect(!string.IsNullOrWhiteSpace(result.Description), "no description");
                Expect(result.GameProductFormat?.Count > 0, "no product format");
                Expect(result.SupportedLanguages?.Count > 0, "no supported language");
            }
            catch (ProductUnavailableException)
            {
                // Delisted or hidden in this region: not a sign of a change of DLsite's API.
            }

        Assert.True(available >= MinimumAvailableWorks,
            $"Only {available} of {Works.Length} pinned works are available, pick new ones.");
        Assert.True(problems.Count == 0, string.Join(Environment.NewLine, problems));
    }

    [LiveFact]
    public async Task Every_locale_answers_with_genres_and_language_names()
    {
        var work = await FirstAvailableWork();

        foreach (SupportedLanguages language in Enum.GetValues(typeof(SupportedLanguages)))
        {
            var result = await Scrap(work.Site, work.Id, language);

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
        var work = await FirstAvailableWork();

        using var client = new System.Net.Http.HttpClient();
        client.DefaultRequestHeaders.Add("Cookie", "locale=en_US; loginchecked=1");
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

        var html = await client.GetStringAsync($"https://www.dlsite.com/{work.Site}/work/=/product_id/{work.Id}.html");
        var response = NuxtPayloadReader.TryReadProductResponse(html, work.Id);

        Assert.NotNull(response);
        Assert.Equal(work.Id, (string)response["data"]["product_id"]);
        Assert.NotEmpty(DLsiteProductMapper
            .Map((Newtonsoft.Json.Linq.JObject)response["data"], SupportedLanguages.en_US, "Genres").Genres);
    }

    /// <summary>
    ///     Game search is the one part that still reads a server-rendered page. It silently returns nothing when
    ///     the markup changes, so this is what would notice it.
    /// </summary>
    [LiveTheory]
    [InlineData("maniax", "ASMR")]
    [InlineData("home", "RPG")]
    public async Task Search_finds_works(string category, string query)
    {
        var results = await new DLsiteScrapper(new NullLogger())
            .ScrapSearchPage(category, query, 10);

        Assert.NotEmpty(results);
        Assert.All(results, result =>
        {
            Assert.False(string.IsNullOrWhiteSpace(result.Title));
            Assert.Matches(@"^https://www\.dlsite\.com/[a-z-]+/work/=/product_id/[A-Z]{2}\d+\.html", result.Link);
        });
    }
}

/// <summary>Theory counterpart of <see cref="LiveFactAttribute" />.</summary>
public sealed class LiveTheoryAttribute : TheoryAttribute
{
    public LiveTheoryAttribute()
    {
        if (Environment.GetEnvironmentVariable("DLSITE_LIVE_TESTS") != "1")
            Skip = "Set DLSITE_LIVE_TESTS=1 to run tests against the real DLsite.";
    }
}
