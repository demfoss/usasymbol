using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace USASymbol.Services
{
    public class SitemapCacheService
    {
        private const string BaseUrl = "https://usasymbol.com";

        public const string SitemapIndexCacheKey = "sitemap_index_xml";
        public const string SitemapMainCacheKey = "sitemap_main_xml";
        public const string SitemapCompareCacheKey = "sitemap_compare_xml";
        public const string SitemapImagesCacheKey = "sitemap_images_xml";

        private readonly IMemoryCache _cache;
        private readonly SitemapBuilder _builder;
        private readonly ILogger<SitemapCacheService> _logger;

        public SitemapCacheService(
            IMemoryCache cache,
            SitemapBuilder builder,
            ILogger<SitemapCacheService> logger)
        {
            _cache = cache;
            _builder = builder;
            _logger = logger;
        }

        public Task<string> GetSitemapIndexAsync()
        {
            var xml = _cache.GetOrCreate(SitemapIndexCacheKey, entry =>
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
            return GetUrlSetAsync(SitemapMainCacheKey, () => _builder.BuildMainUrlsAsync());
        }

        public Task<string> GetCompareSitemapAsync()
        {
            return GetUrlSetAsync(SitemapCompareCacheKey, () => _builder.BuildCompareUrlsAsync());
        }

        public Task<string> GetImageSitemapAsync()
        {
            return _cache.GetOrCreateAsync(SitemapImagesCacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);

                var entries = await _builder.BuildImageEntriesAsync();
                var groupedEntries = entries
                    .GroupBy(e => e.PageUrl)
                    .Select(group => new
                    {
                        PageUrl = group.Key,
                        Images = group
                            .Select(e => (
                                ImageUrl: NormalizeImageUrl(e.ImageUrl),
                                Title: e.Title))
                            .Where(e => !string.IsNullOrWhiteSpace(e.ImageUrl))
                            .Distinct()
                            .ToList()
                    })
                    .Where(group => group.Images.Count > 0)
                    .ToList();

                var sb = new System.Text.StringBuilder();

                sb.AppendLine(@"<?xml version=""1.0"" encoding=""utf-8""?>");
                sb.AppendLine(@"<urlset xmlns=""http://www.sitemaps.org/schemas/sitemap/0.9"" xmlns:image=""http://www.google.com/schemas/sitemap-image/1.1"">");

                foreach (var group in groupedEntries)
                {
                    sb.AppendLine("<url>");
                    sb.AppendLine($"<loc>{BaseUrl}{group.PageUrl}</loc>");
                    foreach (var image in group.Images)
                    {
                        sb.AppendLine("<image:image>");
                        sb.AppendLine($"<image:loc>{System.Security.SecurityElement.Escape(image.ImageUrl)}</image:loc>");
                        sb.AppendLine($"<image:title>{System.Security.SecurityElement.Escape(image.Title)}</image:title>");
                        sb.AppendLine("</image:image>");
                    }
                    sb.AppendLine("</url>");
                }

                sb.AppendLine("</urlset>");

                _logger.LogInformation(
                    "Built image sitemap with {PageCount} pages and {ImageCount} image entries.",
                    groupedEntries.Count,
                    groupedEntries.Sum(group => group.Images.Count));

                return sb.ToString();
            }) ?? Task.FromResult(string.Empty);
        }

        public void InvalidateImageSitemap()
        {
            _cache.Remove(SitemapImagesCacheKey);
            _cache.Remove(SitemapIndexCacheKey);
            _logger.LogInformation("Invalidated cached image sitemap.");
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

        private static string NormalizeImageUrl(string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return string.Empty;
            }

            return imageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? imageUrl
                : $"{BaseUrl}{imageUrl}";
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
