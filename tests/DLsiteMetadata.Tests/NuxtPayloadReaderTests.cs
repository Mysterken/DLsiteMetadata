using System.Linq;
using DLsiteMetadata.Enums;
using DLsiteMetadata.Tests.TestSupport;
using Xunit;

namespace DLsiteMetadata.Tests;

public class NuxtPayloadReaderTests
{
    private static string Page(string json)
    {
        return $"<html><script type=\"application/json\" data-nuxt-data=\"nuxt-app\" id=\"__NUXT_DATA__\">{json}</script></html>";
    }

    [Fact]
    public void Reads_the_product_response_from_a_recorded_page()
    {
        var response = NuxtPayloadReader.TryReadProductResponse(Fixture.Text("page_payload_cg_en.html"), "RJ01034407");

        Assert.NotNull(response);
        Assert.Equal(200, (int)response["meta"]["code"]);
        Assert.Equal("RJ01034407", (string)response["data"]["product_id"]);
        Assert.Equal("10日後メンヘラになるギャル", (string)response["data"]["work_name"]);
    }

    /// <summary>The fallback must produce the same metadata as the API, or the source would change results.</summary>
    [Fact]
    public void Page_payload_maps_like_the_api_response()
    {
        var fromPage = DLsiteProductMapper.Map(
            (Newtonsoft.Json.Linq.JObject)NuxtPayloadReader
                .TryReadProductResponse(Fixture.Text("page_payload_cg_en.html"), "RJ01034407")["data"],
            SupportedLanguages.en_US, "Genres");
        var fromApi = DLsiteProductMapper.Map(Fixture.Product("cg_en"), SupportedLanguages.en_US, "Genres");

        Assert.Equal(fromApi.Title, fromPage.Title);
        Assert.Equal(fromApi.Circle, fromPage.Circle);
        Assert.Equal(fromApi.Author, fromPage.Author);
        Assert.Equal(fromApi.Genres, fromPage.Genres);
        Assert.Equal(fromApi.Age, fromPage.Age);
        Assert.Equal(fromApi.ReleaseDate, fromPage.ReleaseDate);
        Assert.Equal(fromApi.GameProductFormat, fromPage.GameProductFormat);
        Assert.Equal(fromApi.FileFormat, fromPage.FileFormat);
        Assert.Equal(fromApi.Filesize, fromPage.Filesize);
        Assert.Equal(fromApi.SupportedLanguages, fromPage.SupportedLanguages);
        Assert.Equal(fromApi.ProductImages, fromPage.ProductImages);
        Assert.Equal(fromApi.Description, fromPage.Description);
    }

    [Fact]
    public void Another_product_id_finds_nothing()
    {
        Assert.Null(NuxtPayloadReader.TryReadProductResponse(Fixture.Text("page_payload_cg_en.html"), "RJ00000001"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("<html>no payload here</html>")]
    public void Pages_without_payload_find_nothing(string html)
    {
        Assert.Null(NuxtPayloadReader.TryReadProductResponse(html, "RJ1"));
    }

    [Fact]
    public void Broken_payload_json_finds_nothing()
    {
        Assert.Null(NuxtPayloadReader.TryReadProductResponse(Page("[[\"ShallowReactive\",1],{"), "RJ1"));
        Assert.Null(NuxtPayloadReader.TryReadProductResponse(Page("{\"not\":\"an array\"}"), "RJ1"));
    }

    [Fact]
    public void Resolves_wrappers_references_and_shared_values()
    {
        // Pool: 0 root wrapper -> 1 { data } -> 2 wrapper -> 3 { key -> 4 } -> 4 { meta, data } ...
        const string pool = "[[\"ShallowReactive\",1],{\"data\":2},[\"ShallowReactive\",3],{\"dlsite-product-RJ1\":4}," +
                            "{\"meta\":5,\"data\":7},{\"code\":6},200,{\"product_id\":8,\"tags\":9,\"same\":8},\"RJ1\",[8,6]]";

        var response = NuxtPayloadReader.TryReadProductResponse(Page(pool), "RJ1");

        Assert.NotNull(response);
        Assert.Equal(200, (int)response["meta"]["code"]);
        Assert.Equal("RJ1", (string)response["data"]["product_id"]);
        Assert.Equal("RJ1", (string)response["data"]["same"]);
        Assert.Equal(2, response["data"]["tags"].Count());
    }
}
