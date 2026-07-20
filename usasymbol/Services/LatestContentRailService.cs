using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using USASymbol.Data;
using USASymbol.Extensions;
using USASymbol.Models.ViewModels;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace USASymbol.Services
{
    public interface ILatestContentRailService
    {
        Task<LatestContentRailViewModel> GetLatestItemsAsync(int count = 8);
    }

    public class LatestContentRailService : ILatestContentRailService
    {
        private const int PerTypeLimit = 5;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(15);
        private readonly IWebHostEnvironment _env;
        private readonly AppDbContext _db;
        private readonly IMemoryCache _cache;
        private readonly ILogger<LatestContentRailService> _logger;

        public LatestContentRailService(
            IWebHostEnvironment env,
            AppDbContext db,
            IMemoryCache cache,
            ILogger<LatestContentRailService> logger)
        {
            _env = env;
            _db = db;
            _cache = cache;
            _logger = logger;
        }

        public async Task<LatestContentRailViewModel> GetLatestItemsAsync(int count = 8)
        {
            var cacheKey = $"latest-content-rail-{count}";

            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;

                var items = new List<LatestContentRailItemViewModel>();

                items.AddRange(await LoadLatestSymbolItemsAsync(PerTypeLimit));
                items.AddRange(await LoadLatestPageItemsAsync("rankings", "Rankings", PerTypeLimit));
                items.AddRange(await LoadLatestPageItemsAsync("collections", "Collections", PerTypeLimit));

                return new LatestContentRailViewModel
                {
                    Title = "You Might Also Like",
                    Items = items
                        .Where(x => !string.IsNullOrWhiteSpace(x.Url) && !string.IsNullOrWhiteSpace(x.Title))
                        .OrderByDescending(x => x.DateModified ?? DateTime.MinValue)
                        .Take(count)
                        .ToList()
                };
            }) ?? new LatestContentRailViewModel { Title = "You Might Also Like", Items = new List<LatestContentRailItemViewModel>() };
        }

        private async Task<List<LatestContentRailItemViewModel>> LoadLatestSymbolItemsAsync(int take)
        {
            var result = new List<LatestContentRailItemViewModel>();
            var basePath = Path.Combine(_env.ContentRootPath, "Content", "states");
            if (!Directory.Exists(basePath))
            {
                return result;
            }

            var states = await _db.States.AsNoTracking()
                .Select(s => new { s.Id, s.Name, s.Slug })
                .ToListAsync();

            var stateLookup = states.ToDictionary(x => x.Slug, x => x, StringComparer.OrdinalIgnoreCase);

            var symbols = await _db.Symbols.AsNoTracking()
                .Select(s => new { s.StateId, s.Type, s.Slug, s.Name, s.ImageUrl })
                .ToListAsync();

            foreach (var file in GetLatestFiles(basePath, "*.yaml", take))
            {
                var rel = Path.GetRelativePath(basePath, file);
                var parts = rel.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToArray();

                if (parts.Length < 2)
                {
                    continue;
                }

                var stateSlug = parts[0];
                if (!stateLookup.TryGetValue(stateSlug, out var state))
                {
                    continue;
                }

                var fileSlug = Path.GetFileNameWithoutExtension(file);
                var type = parts.Length == 2 ? fileSlug : parts[1];

                var dbSymbol = parts.Length == 2
                    ? symbols.FirstOrDefault(s => s.StateId == state.Id && string.Equals(s.Type, type, StringComparison.OrdinalIgnoreCase))
                    : symbols.FirstOrDefault(s => s.StateId == state.Id &&
                                                  string.Equals(s.Type, type, StringComparison.OrdinalIgnoreCase) &&
                                                  string.Equals(s.Slug, fileSlug, StringComparison.OrdinalIgnoreCase));

                if (dbSymbol == null)
                {
                    continue;
                }

                var data = await TryReadYamlAsync(file);
                if (data == null)
                {
                    continue;
                }

                var title = GetString(data, "title");
                var description = GetString(data, "seo_description");
                var dateModified = GetDate(data, "date_modified")
                    ?? GetDate(data, "date_published")
                    ?? File.GetLastWriteTimeUtc(file);

                result.Add(new LatestContentRailItemViewModel
                {
                    Title = string.IsNullOrWhiteSpace(title) ? dbSymbol.Name : title,
                    Description = description ?? string.Empty,
                    Url = new Models.Symbol
                    {
                        Slug = dbSymbol.Slug,
                        Type = dbSymbol.Type,
                        Name = dbSymbol.Name
                    }.ToSymbolUrl(state.Slug),
                    ImageUrl = ResolveRailImage(GetString(data, "hero_image"), dbSymbol.ImageUrl),
                    Eyebrow = state.Name,
                    SectionLabel = ToTitle(type),
                    DateModified = dateModified
                });
            }

            return result;
        }

        private async Task<List<LatestContentRailItemViewModel>> LoadLatestPageItemsAsync(string rootFolder, string sectionLabel, int take)
        {
            var result = new List<LatestContentRailItemViewModel>();
            var basePath = Path.Combine(_env.ContentRootPath, "Content", rootFolder);
            if (!Directory.Exists(basePath))
            {
                return result;
            }

            foreach (var file in GetLatestFiles(basePath, "*.yml", take))
            {
                var rel = Path.GetRelativePath(basePath, file);
                var parts = rel.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToArray();

                if (parts.Length < 2)
                {
                    continue;
                }

                var group = parts[0];
                var slug = Path.GetFileNameWithoutExtension(file);
                var data = await TryReadYamlAsync(file);
                if (data == null)
                {
                    continue;
                }

                var title = GetNestedString(data, "page", "h1");
                var description = GetNestedListFirst(data, "page", "intro_paragraphs") ?? GetNestedString(data, "seo", "description");
                var heroImage = GetString(data, "hero_image");
                var dateModified = GetDate(data, "date_modified")
                    ?? GetDate(data, "date_published")
                    ?? File.GetLastWriteTimeUtc(file);

                result.Add(new LatestContentRailItemViewModel
                {
                    Title = title ?? string.Empty,
                    Description = description ?? string.Empty,
                    Url = $"/{rootFolder}/{group}/{slug}",
                    ImageUrl = ResolveRailImage(heroImage),
                    Eyebrow = ToTitle(group) ?? string.Empty,
                    SectionLabel = sectionLabel,
                    DateModified = dateModified
                });
            }

            return result;
        }

        private static IEnumerable<string> GetLatestFiles(string basePath, string pattern, int take)
        {
            if (!Directory.Exists(basePath) || take <= 0)
            {
                return Enumerable.Empty<string>();
            }

            return Directory.EnumerateFiles(basePath, pattern, SearchOption.AllDirectories)
                .Select(path => new FileInfo(path))
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .Take(take)
                .Select(file => file.FullName)
                .ToList();
        }

        private async Task<Dictionary<object, object>?> TryReadYamlAsync(string path)
        {
            try
            {
                var yaml = await File.ReadAllTextAsync(path);
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .IgnoreUnmatchedProperties()
                    .Build();

                return deserializer.Deserialize<Dictionary<object, object>>(yaml) ?? new Dictionary<object, object>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Skipping malformed YAML while building latest content rail: {Path}", path);
                return null;
            }
        }

        private static string? GetString(Dictionary<object, object> data, string key)
        {
            return data.TryGetValue(key, out var value) ? value?.ToString() : null;
        }

        private static string? GetNestedString(Dictionary<object, object> data, string parentKey, string childKey)
        {
            if (data.TryGetValue(parentKey, out var parentObj) && parentObj is Dictionary<object, object> parent)
            {
                return GetString(parent, childKey);
            }

            if (data.TryGetValue("seo", out var seoObj) && seoObj is Dictionary<object, object> seo && parentKey == "seo")
            {
                return GetString(seo, childKey);
            }

            return null;
        }

        private static string? GetNestedListFirst(Dictionary<object, object> data, string parentKey, string listKey)
        {
            if (data.TryGetValue(parentKey, out var parentObj) && parentObj is Dictionary<object, object> parent)
            {
                if (parent.TryGetValue(listKey, out var listObj) && listObj is IEnumerable<object> list)
                {
                    return list.FirstOrDefault()?.ToString();
                }
            }

            return null;
        }

        private static DateTime? GetDate(Dictionary<object, object> data, string key)
        {
            if (!data.TryGetValue(key, out var value) || value == null)
            {
                return null;
            }

            return DateTime.TryParse(value.ToString(), out var parsed) ? parsed : null;
        }

        private string ResolveRailImage(params string?[] candidates)
        {
            foreach (var candidate in candidates)
            {
                var image = NormalizeImagePath(candidate);
                if (string.IsNullOrWhiteSpace(image))
                {
                    continue;
                }

                if (IsRemoteUrl(image) || LocalImageExists(image))
                {
                    return image;
                }
            }

            return "/images/usasymbol.png";
        }

        private static string NormalizeImagePath(string? image)
        {
            if (string.IsNullOrWhiteSpace(image))
            {
                return string.Empty;
            }

            var normalized = image.Trim().Trim('"', '\'').Replace('\\', '/');
            if (IsRemoteUrl(normalized))
            {
                return normalized;
            }

            return normalized.StartsWith("/", StringComparison.Ordinal) ? normalized : "/" + normalized;
        }

        private bool LocalImageExists(string image)
        {
            var pathOnly = image.Split('?', '#')[0];
            if (string.IsNullOrWhiteSpace(pathOnly) || !pathOnly.StartsWith("/images/", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var relativePath = pathOnly.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            return File.Exists(Path.Combine(_env.WebRootPath, relativePath));
        }

        private static bool IsRemoteUrl(string image)
        {
            return Uri.TryCreate(image, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }

        private static string ToTitle(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var normalized = value.Replace("-", " ").Replace("_", " ").Trim();
            return System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(normalized);
        }
    }
}
