using System.Net.Http;
using System.Threading.Tasks;
using Playnite.SDK;

namespace DLsiteMetadata;

public class DLsiteScrapper(ILogger logger)
{
    public const string DefaultLanguage = "en_US";

    public const string SiteBaseUrl = "https://www.dlsite.com";

    private readonly ILogger _logger = logger;

    public async Task<string> ScrapGamePage(string productId, string language)
    {
        var testUrl = "https://www.dlsite.com/home/work/=/product_id/RJ01038533.html";
        // Implement metadata retrieval logic here
        // we fetch the metadata from the DLsite website by its product ID
        var client = new HttpClient();
        var response = await client.GetStringAsync(testUrl);

        return response;
    }
}