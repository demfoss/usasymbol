using Microsoft.Extensions.Caching.Memory;

namespace USASymbol.Services
{
    public class SitemapCacheService
    {
        private const string BaseUrl = "https://usasymbol.com";

        private readonly IMemoryCache _cache;
        private readonly SitemapBuilder _builder;

        public SitemapCacheService(
            IMemoryCache cache,
            SitemapBuilder builder)
        {
            _cache = cache;
            _builder = builder;
        }

        public Task<string> GetSitemapIndexAsync()
        {
            var xml = _cache.GetOrCreate("sitemap_index_xml", entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);

                var sb = new System.Text.StringBuilder();

                sb.AppendLine(@"<?xml version=""1.0"" encoding=""utf-8""?>");
                sb.AppendLine(@"<sitemapindex xmlns=""http://www.sitemaps.org/schemas/sitemap/0.9"">");

                AppendSitemap(sb, "/sitemap-main.xml");
                AppendSitemap(sb, "/sitemap-compare.xml");

                sb.AppendLine("</sitemapindex>");

                return sb.ToString();
            }) ?? string.Empty;

            return Task.FromResult(xml);
        }

        public Task<string> GetMainSitemapAsync()
        {
            return GetUrlSetAsync("sitemap_main_xml", () => _builder.BuildMainUrlsAsync());
        }

        public Task<string> GetCompareSitemapAsync()
        {
            return GetUrlSetAsync("sitemap_compare_xml", () => _builder.BuildCompareUrlsAsync());
        }

        public Task<string> GetSitemapAsync()
        {
            return GetSitemapIndexAsync();
        }

        private async Task<string> GetUrlSetAsync(string cacheKey, Func<Task<List<string>>> buildUrls)
        {
            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);

                var urls = await buildUrls();
                var sb = new System.Text.StringBuilder();

                sb.AppendLine(@"<?xml version=""1.0"" encoding=""utf-8""?>");
                sb.AppendLine(@"<urlset xmlns=""http://www.sitemaps.org/schemas/sitemap/0.9"">");

                foreach (var url in urls)
                {
                    sb.AppendLine("<url>");
                    sb.AppendLine($"<loc>{BaseUrl}{url}</loc>");
                    sb.AppendLine($"<lastmod>{DateTime.UtcNow:yyyy-MM-dd}</lastmod>");
                    sb.AppendLine("</url>");
                }

                sb.AppendLine("</urlset>");

                return sb.ToString();
            }) ?? string.Empty;
        }

        private static void AppendSitemap(System.Text.StringBuilder sb, string path)
        {
            sb.AppendLine("<sitemap>");
            sb.AppendLine($"<loc>{BaseUrl}{path}</loc>");
            sb.AppendLine($"<lastmod>{DateTime.UtcNow:yyyy-MM-dd}</lastmod>");
            sb.AppendLine("</sitemap>");
        }
    }
}
