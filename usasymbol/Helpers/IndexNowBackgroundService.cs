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
            var sitemapUrls = new[]
            {
                "https://usasymbol.com/sitemap-main.xml",
                "https://usasymbol.com/sitemap-compare.xml"
            };

            var urls = new List<string>();

            foreach (var sitemapUrl in sitemapUrls)
            {
                var xml = await _http.GetStringAsync(sitemapUrl);
                var doc = XDocument.Parse(xml);

                urls.AddRange(doc.Descendants()
                    .Where(x => x.Name.LocalName == "loc")
                    .Select(x => x.Value));
            }

            var urlList = urls
                .Distinct()
                .Take(10000)
                .ToArray();

            var payload = new
            {
                host = "usasymbol.com",
                key = "fe730da503bb4a9382cbc9ea9e56716e",
                keyLocation = "https://usasymbol.com/fe730da503bb4a9382cbc9ea9e56716e.txt",
                urlList
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
