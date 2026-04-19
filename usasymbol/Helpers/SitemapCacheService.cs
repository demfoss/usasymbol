using Microsoft.Extensions.Caching.Memory;

namespace USASymbol.Services
{
    public class SitemapCacheService
    {
        private readonly IMemoryCache _cache;
        private readonly SitemapBuilder _builder;

        public SitemapCacheService(
            IMemoryCache cache,
            SitemapBuilder builder)
        {
            _cache = cache;
            _builder = builder;
        }

        public async Task<string> GetSitemapAsync()
        {
            return await _cache.GetOrCreateAsync("sitemap_xml", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);

                var urls = await _builder.BuildUrlsAsync();

                var sb = new System.Text.StringBuilder();

                sb.AppendLine(@"<?xml version=""1.0"" encoding=""utf-8""?>");
                sb.AppendLine(@"<urlset xmlns=""http://www.sitemaps.org/schemas/sitemap/0.9"">");

                foreach (var url in urls)
                {
                    sb.AppendLine("<url>");
                    sb.AppendLine($"<loc>https://usasymbol.com{url}</loc>");
                    sb.AppendLine($"<lastmod>{DateTime.UtcNow:yyyy-MM-dd}</lastmod>");
                    sb.AppendLine("</url>");
                }

                sb.AppendLine("</urlset>");

                return sb.ToString();
            }) ?? string.Empty;
        }
    }
}
