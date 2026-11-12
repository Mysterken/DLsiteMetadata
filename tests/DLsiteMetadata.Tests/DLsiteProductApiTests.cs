using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using DLsiteMetadata.Enums;
using DLsiteMetadata.Exceptions;
using DLsiteMetadata.Tests.TestSupport;
using Xunit;

namespace DLsiteMetadata.Tests;

public class DLsiteProductApiTests
{
    private static string Json(string name) => Fixture.Text($"api_{name}.json");

    private static DLsiteProductApi Api(StubHttpHandler handler) => new(new NullLogger(), handler.CreateClient());

    [Fact]
    public async Task Reads_the_product_from_the_api()
    {
        var handler = new StubHttpHandler().Respond(HttpStatusCode.OK, Json("voice_en"));

        var product = await Api(handler).FetchProductAsync("RJ01005844", "home", SupportedLanguages.en_US);

        Assert.Equal("RJ01005844", (string)product["product_id"]);
        var request = Assert.Single(handler.Requests);
        Assert.Contains("/api/product/home/fetchProduct/RJ01005844", request.RequestUri.AbsoluteUri);
        Assert.Contains("locale=en_US", request.RequestUri.Query);
        Assert.Contains("locale=en_US", request.Headers.GetValues("Cookie").Single());
    }

    [Fact]
    public async Task Asks_again_under_the_site_the_api_names()
    {
        var handler = new StubHttpHandler()
            .Respond(HttpStatusCode.OK, Json("wrong_site"))
            .Respond(HttpStatusCode.OK, Json("cg_en"));

        var product = await Api(handler).FetchProductAsync("RJ01723044", "maniax", SupportedLanguages.en_US);

        Assert.NotNull(product);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Contains("/api/product/maniax/", handler.Requests[0].RequestUri.AbsoluteUri);
        Assert.Contains("/api/product/girls/", handler.Requests[1].RequestUri.AbsoluteUri);
    }

    [Fact]
    public async Task Does_not_loop_when_the_named_site_is_the_one_asked()
    {
        var handler = new StubHttpHandler()
            .Respond(HttpStatusCode.OK, Json("wrong_site"))
            .Respond(HttpStatusCode.OK, Json("wrong_site"))
            .Respond(HttpStatusCode.OK, "<html></html>");

        // girls -> 400 naming girls: no redirect possible, falls back to the page which holds nothing.
        await Assert.ThrowsAsync<DLsiteApiException>(() =>
            Api(handler).FetchProductAsync("RJ01723044", "girls", SupportedLanguages.en_US));
    }

    [Fact]
    public async Task Unknown_product_is_unavailable_without_fallback()
    {
        var handler = new StubHttpHandler().Respond(HttpStatusCode.OK, Json("not_found"));

        var ex = await Assert.ThrowsAsync<ProductUnavailableException>(() =>
            Api(handler).FetchProductAsync("RJ99999999", "maniax", SupportedLanguages.en_US));

        Assert.False(string.IsNullOrWhiteSpace(ex.Message));
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Product_hidden_in_the_region_is_unavailable_without_fallback()
    {
        var handler = new StubHttpHandler().Respond(HttpStatusCode.OK, Json("hidden_by_country"));

        var ex = await Assert.ThrowsAsync<ProductUnavailableException>(() =>
            Api(handler).FetchProductAsync("RJ01099639", "maniax", SupportedLanguages.en_US));

        Assert.Contains("country/region", ex.Message);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Reads_the_work_page_when_the_api_errors()
    {
        var handler = new StubHttpHandler()
            .Respond(HttpStatusCode.InternalServerError, "oops")
            .Respond(HttpStatusCode.OK, Fixture.Text("page_payload_cg_en.html"));

        var product = await Api(handler).FetchProductAsync("RJ01034407", "maniax", SupportedLanguages.en_US);

        Assert.Equal("RJ01034407", (string)product["product_id"]);
        Assert.Contains("/work/=/product_id/RJ01034407.html", handler.Requests[1].RequestUri.AbsoluteUri);
    }

    [Fact]
    public async Task Reads_the_work_page_when_the_api_answer_is_not_json()
    {
        var handler = new StubHttpHandler()
            .Respond(HttpStatusCode.OK, "<html>maintenance</html>")
            .Respond(HttpStatusCode.OK, Fixture.Text("page_payload_cg_en.html"));

        var product = await Api(handler).FetchProductAsync("RJ01034407", "maniax", SupportedLanguages.en_US);

        Assert.Equal("RJ01034407", (string)product["product_id"]);
    }

    [Fact]
    public async Task Reads_the_work_page_when_the_connection_fails()
    {
        var handler = new StubHttpHandler()
            .Throw(new HttpRequestException("no route"))
            .Respond(HttpStatusCode.OK, Fixture.Text("page_payload_cg_en.html"));

        var product = await Api(handler).FetchProductAsync("RJ01034407", "maniax", SupportedLanguages.en_US);

        Assert.NotNull(product);
    }

    [Fact]
    public async Task Fails_when_neither_source_has_the_product()
    {
        var handler = new StubHttpHandler()
            .Respond(HttpStatusCode.InternalServerError, "oops")
            .Respond(HttpStatusCode.OK, "<html>no payload</html>");

        await Assert.ThrowsAsync<DLsiteApiException>(() =>
            Api(handler).FetchProductAsync("RJ01034407", "maniax", SupportedLanguages.en_US));
    }

    [Fact]
    public async Task Missing_page_after_an_api_error_means_the_product_is_gone()
    {
        var handler = new StubHttpHandler()
            .Respond(HttpStatusCode.InternalServerError, "oops")
            .Respond(HttpStatusCode.NotFound, "<html></html>");

        await Assert.ThrowsAsync<ProductUnavailableException>(() =>
            Api(handler).FetchProductAsync("RJ99999999", "maniax", SupportedLanguages.en_US));
    }

    [Fact]
    public async Task Facade_rejects_urls_that_are_not_product_pages()
    {
        await Assert.ThrowsAsync<System.ArgumentException>(() =>
            new DLsiteScrapper(new NullLogger()).ScrapGamePage("https://example.com/", SupportedLanguages.en_US));
    }
}
