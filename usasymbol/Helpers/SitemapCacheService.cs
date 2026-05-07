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
                AppendSitemap(sb, "/sitemap-images.xml");

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

        public Task<string> GetImageSitemapAsync()
        {
            return _cache.GetOrCreateAsync("sitemap_images_xml", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);

                var entries = await _builder.BuildImageEntriesAsync();
                var sb = new System.Text.StringBuilder();

                sb.AppendLine(@"<?xml version=""1.0"" encoding=""utf-8""?>");
                sb.AppendLine(@"<urlset xmlns=""http://www.sitemaps.org/schemas/sitemap/0.9"" xmlns:image=""http://www.google.com/schemas/sitemap-image/1.1"">");

                foreach (var group in entries.GroupBy(e => e.PageUrl))
                {
                    sb.AppendLine("<url>");
                    sb.AppendLine($"<loc>{BaseUrl}{group.Key}</loc>");
                    foreach (var (_, imageUrl, title) in group)
                    {
                        var absUrl = imageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                            ? imageUrl
                            : $"{BaseUrl}{imageUrl}";
                        sb.AppendLine("<image:image>");
                        sb.AppendLine($"<image:loc>{System.Security.SecurityElement.Escape(absUrl)}</image:loc>");
                        sb.AppendLine($"<image:title>{System.Security.SecurityElement.Escape(title)}</image:title>");
                        sb.AppendLine("</image:image>");
                    }
                    sb.AppendLine("</url>");
                }

                sb.AppendLine("</urlset>");

                return sb.ToString();
            }) ?? Task.FromResult(string.Empty);
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
