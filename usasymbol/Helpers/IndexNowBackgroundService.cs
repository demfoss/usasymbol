using System.Text;
using System.Text.Json;
using System.Xml.Linq;

public class IndexNowBackgroundService : BackgroundService
{
    private readonly HttpClient _http = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await SubmitSitemap();


            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task SubmitSitemap()
    {
        try
        {
            var sitemapUrl = "https://usasymbol.com/sitemap.xml";

            var xml = await _http.GetStringAsync(sitemapUrl);

            var doc = XDocument.Parse(xml);

            var urls = doc.Descendants()
                .Where(x => x.Name.LocalName == "loc")
                .Select(x => x.Value)
                .Take(10000)
                .ToArray();

            var payload = new
            {
                host = "usasymbol.com",
                key = "fe730da503bb4a9382cbc9ea9e56716e",
                keyLocation = "https://usasymbol.com/fe730da503bb4a9382cbc9ea9e56716e.txt",
                urlList = urls
            };

            var json = JsonSerializer.Serialize(payload);

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            await _http.PostAsync("https://api.indexnow.org/indexnow", content);
        }
        catch
        {

        }
    }
}
