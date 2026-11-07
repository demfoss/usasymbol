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

        public async Task<string?> EnsureSharePngAsync(string slug, string title, IReadOnlyList<StateMapEntry> entries,
            IReadOnlyList<MapLegendGroup> legend, string? sourceName, string pageUrl)
        {
            if (string.IsNullOrWhiteSpace(slug) || entries == null || entries.Count == 0)
                return null;

            var safeSlug = Regex.Replace(slug, @"[^a-z0-9\-]", "");
            var fingerprint = string.Join("|", entries.OrderBy(e => e.PostalCode).Select(e => e.PostalCode + e.FillColor))
                + "#" + DateTime.UtcNow.Year + "#" + title + "#" + sourceName + "#" + pageUrl
                + "#" + string.Join("|", legend.Select(g => g.Label + g.Range + g.Color + g.StateCodes.Count));
            var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(fingerprint)))[..8].ToLowerInvariant();
            var dir = Path.Combine(_env.WebRootPath, "images", "maps", "share");
            var fileName = $"{safeSlug}-{hash}.png";
            var fullPath = Path.Combine(dir, fileName);
            var relativePath = $"/images/maps/share/{fileName}";
            if (File.Exists(fullPath))
                return relativePath;

            var sem = _slugLocks.GetOrAdd("share-" + safeSlug, _ => new SemaphoreSlim(1, 1));
            await sem.WaitAsync();
            try
            {
                if (File.Exists(fullPath))
                    return relativePath;
                var baseSvg = GetBaseSvg();
                if (string.IsNullOrEmpty(baseSvg))
                    return null;
                var bytes = RenderSharePng(BuildExportSvg(baseSvg, entries), title, legend, sourceName, pageUrl);
                if (bytes == null)
                    return null;
                Directory.CreateDirectory(dir);
                await File.WriteAllBytesAsync(fullPath, bytes);
                return relativePath;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate share PNG for slug '{Slug}'", safeSlug);
                return null;
            }
            finally
            {
                sem.Release();
            }
        }

        private static float TextWidth(SKFont font, string text)
        {
            using var paint = new SKPaint { Typeface = font.Typeface, TextSize = font.Size };
            return paint.MeasureText(text);
        }

        private static byte[]? RenderSharePng(string svgContent, string title, IReadOnlyList<MapLegendGroup> legend,
            string? sourceName, string pageUrl)
        {
            using var skSvg = new SKSvg();
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(svgContent));
            skSvg.Load(stream);
            if (skSvg.Picture == null)
                return null;

            const int width = 1200;
            const float scale = width / 959f;
            var mapHeight = (int)Math.Round(593 * scale);
            const int headerHeight = 96;
            const int legendHeight = 96;
            const int footerHeight = 70;
            var height = headerHeight + mapHeight + legendHeight + footerHeight;

            SKTypeface Face(bool bold) =>
                SKTypeface.FromFamilyName("Segoe UI", bold ? SKFontStyle.Bold : SKFontStyle.Normal)
                ?? SKTypeface.FromFamilyName("Arial", bold ? SKFontStyle.Bold : SKFontStyle.Normal)
                ?? SKTypeface.Default;

            using var boldFace = Face(true);
            using var normalFace = Face(false);
            using var titleFont = new SKFont(boldFace, 38);
            using var legendFont = new SKFont(normalFace, 19);
            using var legendBold = new SKFont(boldFace, 19);
            using var footFont = new SKFont(normalFace, 19);
            using var footBold = new SKFont(boldFace, 21);
            using var ink = new SKPaint { Color = new SKColor(0x0e, 0x1a, 0x26), IsAntialias = true };
            using var muted = new SKPaint { Color = new SKColor(0x55, 0x69, 0x7b), IsAntialias = true };
            using var line = new SKPaint { Color = new SKColor(0xd6, 0xdf, 0xe7), StrokeWidth = 2 };

            var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var surface = SKSurface.Create(info);
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);

            // Title, shrunk until it fits on one line.
            while (TextWidth(titleFont, title) > width - 80 && titleFont.Size > 22) titleFont.Size -= 1;
            canvas.DrawText(title, 40, 62, titleFont, ink);
            canvas.DrawLine(40, headerHeight - 6, width - 40, headerHeight - 6, line);

            canvas.Save();
            canvas.Translate(0, headerHeight);
            canvas.Scale(scale);
            canvas.DrawPicture(skSvg.Picture);
            canvas.Restore();

            // Legend swatches in one or two rows.
            var x = 40f;
            var y = headerHeight + mapHeight + 34f;
            foreach (var group in legend)
            {
                var text = string.IsNullOrEmpty(group.Range) ? group.Label : $"{group.Label}  {group.Range}";
                var itemWidth = 30 + TextWidth(legendFont, text) + 34;
                if (x + itemWidth > width - 40) { x = 40; y += 34; }
                using var swatch = new SKPaint { Color = SKColor.TryParse(group.Color, out var c) ? c : new SKColor(0xe2, 0xe8, 0xf0), IsAntialias = true };
                canvas.DrawRoundRect(x, y - 16, 20, 20, 4, 4, swatch);
                canvas.DrawText(text, x + 30, y, legendFont, ink);
                x += itemWidth;
            }

            // Credit line: this is what travels with the image.
            canvas.DrawLine(40, height - footerHeight, width - 40, height - footerHeight, line);
            var footY = height - footerHeight + 42f;
            // Footer: only our own copyright line with the page address.
            var link = pageUrl.Replace("https://", "").Replace("http://", "");
            canvas.DrawText($"\u00A9 {DateTime.UtcNow.Year} USA Symbol  \u00B7  {link}", 40, footY, footBold, ink);

            canvas.Flush();
            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data?.ToArray();
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
