using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using Svg.Skia;
using System.Text;
using System.Text.RegularExpressions;
using USASymbol.Services.Interface;
using Usasymbol.ViewModels;

namespace USASymbol.Services
{
    public class MapPngService : IMapPngService
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<MapPngService> _logger;
        private readonly IMemoryCache _cache;

        private static string? _cachedBaseSvg;
        private static readonly object _svgLock = new();

        // Per-slug semaphores to avoid concurrent double-generation
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, SemaphoreSlim> _slugLocks = new();

        public MapPngService(
            IWebHostEnvironment env,
            ILogger<MapPngService> logger,
            IMemoryCache cache)
        {
            _env = env;
            _logger = logger;
            _cache = cache;
        }

        public async Task<string?> EnsureMapPngAsync(string slug, IReadOnlyList<StateMapEntry> entries)
        {
            if (string.IsNullOrWhiteSpace(slug) || entries == null || entries.Count == 0)
                return null;

            var safeSlug = Regex.Replace(slug, @"[^a-z0-9\-]", "");
            var dir = Path.Combine(_env.WebRootPath, "images", "maps");
            var fileName = $"{safeSlug}.png";
            var fullPath = Path.Combine(dir, fileName);
            var relativePath = $"/images/maps/{fileName}";

            if (File.Exists(fullPath))
                return relativePath;

            var sem = _slugLocks.GetOrAdd(safeSlug, _ => new SemaphoreSlim(1, 1));
            await sem.WaitAsync();
            try
            {
                if (File.Exists(fullPath))
                    return relativePath;

                var baseSvg = GetBaseSvg();
                if (string.IsNullOrEmpty(baseSvg))
                    return null;

                var exportSvg = BuildExportSvg(baseSvg, entries);
                var pngBytes = RenderToPng(exportSvg);
                if (pngBytes == null)
                    return null;

                Directory.CreateDirectory(dir);
                await File.WriteAllBytesAsync(fullPath, pngBytes);
                _cache.Remove(SitemapCacheService.SitemapImagesCacheKey);
                _cache.Remove(SitemapCacheService.SitemapIndexCacheKey);
                _logger.LogInformation("Generated map PNG: {Path}", relativePath);
                return relativePath;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate map PNG for slug '{Slug}'", safeSlug);
                return null;
            }
            finally
            {
                sem.Release();
            }
        }

        private string GetBaseSvg()
        {
            if (_cachedBaseSvg != null) return _cachedBaseSvg;
            lock (_svgLock)
            {
                if (_cachedBaseSvg != null) return _cachedBaseSvg;
                var path = Path.Combine(_env.WebRootPath, "maps", "us-states.svg");
                if (!File.Exists(path)) return string.Empty;
                _cachedBaseSvg = File.ReadAllText(path);
            }
            return _cachedBaseSvg!;
        }

        private static string BuildExportSvg(string baseSvg, IReadOnlyList<StateMapEntry> entries)
        {
            var fills = entries.ToDictionary(
                e => e.PostalCode.ToLowerInvariant(),
                e => e.FillColor,
                StringComparer.OrdinalIgnoreCase);

            var svg = Regex.Replace(baseSvg,
                @"<path class=""([a-z]{2})""",
                m =>
                {
                    var code = m.Groups[1].Value;
                    var color = fills.TryGetValue(code, out var f) ? f : "#e2e8f0";
                    return $@"<path fill=""{color}"" class=""{code}""";
                });

            svg = svg.Replace("<g class=\"state\">", "<g class=\"state\" fill=\"#e2e8f0\">");

            svg = Regex.Replace(svg,
                @"<svg([^>]*)>",
                m =>
                {
                    var attrs = m.Groups[1].Value;
                    if (!attrs.Contains("width=")) attrs += " width=\"959\"";
                    if (!attrs.Contains("height=")) attrs += " height=\"593\"";
                    if (!attrs.Contains("viewBox")) attrs += " viewBox=\"0 0 959 593\"";
                    return $"<svg{attrs}>";
                },
                RegexOptions.IgnoreCase);

            return svg;
        }

        private static byte[]? RenderToPng(string svgContent)
        {
            using var skSvg = new SKSvg();
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(svgContent));
            skSvg.Load(stream);

            if (skSvg.Picture == null)
                return null;

            const int targetWidth = 1200;
            const int sourceWidth = 959;
            const int sourceHeight = 593;
            var scale = targetWidth / (float)sourceWidth;
            var targetHeight = (int)Math.Round(sourceHeight * scale);

            var info = new SKImageInfo(targetWidth, targetHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var surface = SKSurface.Create(info);
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);
            canvas.Scale(scale);
            canvas.DrawPicture(skSvg.Picture);
            canvas.Flush();

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data?.ToArray();
        }
    }
}
