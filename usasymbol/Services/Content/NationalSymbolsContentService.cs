using USASymbol.Models.Content;
using USASymbol.Services.Interface;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace USASymbol.Services.Content
{
    public sealed class NationalSymbolsContentService : INationalSymbolsContentService
    {
        private readonly IListingsContentService _pageContent;
        private readonly ILogger<NationalSymbolsContentService> _logger;
        private readonly string _contentPath;

        public NationalSymbolsContentService(
            IListingsContentService pageContent,
            IWebHostEnvironment environment,
            ILogger<NationalSymbolsContentService> logger)
        {
            _pageContent = pageContent;
            _logger = logger;
            _contentPath = Path.Combine(environment.ContentRootPath, "Content", "national-symbols");
        }

        public async Task<IReadOnlyList<PageCategoryItem>> GetAllAsync()
        {
            if (!Directory.Exists(_contentPath))
                return Array.Empty<PageCategoryItem>();

            var slugs = Directory.EnumerateFiles(_contentPath, "*.yml", SearchOption.TopDirectoryOnly)
                .Concat(Directory.EnumerateFiles(_contentPath, "*.yaml", SearchOption.TopDirectoryOnly))
                .Select(Path.GetFileNameWithoutExtension)
                .Where(slug => !string.IsNullOrWhiteSpace(slug))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(slug => slug, StringComparer.OrdinalIgnoreCase);

            var items = new List<(int Order, PageCategoryItem Item)>();
            foreach (var slug in slugs)
            {
                var content = await GetContentAsync(slug!);
                if (content == null)
                    continue;

                var metadata = await LoadMetadataAsync(slug!);

                items.Add((metadata?.Order ?? int.MaxValue, new PageCategoryItem
                {
                    Title = string.IsNullOrWhiteSpace(content.Page.H1)
                        ? FormatTitle(slug!)
                        : content.Page.H1,
                    Url = content.Url,
                    Description = content.Seo.Description,
                    Image = content.HeroImage,
                    ImageAlt = content.HeroImageAlt,
                    DatePublished = content.DatePublished,
                    DateModified = content.DateModified
                }));
            }

            return items
                .OrderBy(item => item.Order)
                .ThenBy(item => item.Item.Title, StringComparer.OrdinalIgnoreCase)
                .Select(item => item.Item)
                .ToList();
        }

        public async Task<PageContent?> GetContentAsync(string slug)
        {
            var normalizedSlug = slug.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(normalizedSlug) ||
                normalizedSlug.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                normalizedSlug.Contains('/') || normalizedSlug.Contains('\\'))
            {
                return null;
            }

            var content = await _pageContent.GetContentAsync("national-symbols", normalizedSlug);
            if (content == null)
            {
                _logger.LogWarning("National symbol content not found: {Slug}", normalizedSlug);
                return null;
            }

            content.Slug = normalizedSlug;
            content.Category = "national-symbols";
            content.Url = $"/national-symbols/{normalizedSlug}";

            var metadata = await LoadMetadataAsync(normalizedSlug);
            if (metadata != null)
            {
                if (!string.IsNullOrWhiteSpace(metadata.Title))
                    content.Page.H1 = metadata.Title;
                if (!string.IsNullOrWhiteSpace(metadata.SeoTitle))
                    content.Seo.Title = metadata.SeoTitle;
                if (!string.IsNullOrWhiteSpace(metadata.SeoDescription))
                    content.Seo.Description = metadata.SeoDescription;

                if (!string.IsNullOrWhiteSpace(metadata.IntroText))
                    content.Page.IntroParagraphs = new List<string> { metadata.IntroText };

                if (!string.IsNullOrWhiteSpace(metadata.Audio))
                    content.AudioUrl = metadata.Audio;

                if (metadata.Sources?.Count > 0 && content.Page.Sources.Count == 0)
                {
                    content.Page.Sources = metadata.Sources
                        .Where(source => !string.IsNullOrWhiteSpace(source.Name) || !string.IsNullOrWhiteSpace(source.Url))
                        .Select(source => new PageSource
                        {
                            Name = source.Name,
                            Url = source.Url,
                            Description = source.Description
                        })
                        .ToList();
                }

                if (metadata.QuickFacts?.Count > 0)
                {
                    content.Page.QuickAnswerTitle = "Quick facts";
                    content.Page.QuickAnswer = metadata.QuickFacts
                        .Where(fact => !string.IsNullOrWhiteSpace(fact.Label) || !string.IsNullOrWhiteSpace(fact.Value))
                        .Select(fact => string.IsNullOrWhiteSpace(fact.Label)
                            ? fact.Value
                            : $"{fact.Label}: {fact.Value}")
                        .ToList();
                }
                else if (!string.IsNullOrWhiteSpace(metadata.Summary))
                {
                    content.Page.QuickAnswer = new List<string> { metadata.Summary };
                }
            }

            return content;
        }

        public async Task<T?> GetTypedContentAsync<T>(string slug) where T : class
        {
            var normalizedSlug = slug.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(normalizedSlug) ||
                normalizedSlug.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                normalizedSlug.Contains('/') || normalizedSlug.Contains('\\'))
            {
                return null;
            }

            var filePath = FindContentFile(normalizedSlug);
            if (filePath == null)
                return null;

            try
            {
                var yaml = await File.ReadAllTextAsync(filePath);
                return new DeserializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .IgnoreUnmatchedProperties()
                    .Build()
                    .Deserialize<T>(yaml);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to read typed national symbol content: {Slug}", normalizedSlug);
                return null;
            }
        }

        private async Task<NationalSymbolMetadata?> LoadMetadataAsync(string slug)
        {
            var filePath = FindContentFile(slug);

            if (filePath == null)
                return null;

            try
            {
                var yaml = await File.ReadAllTextAsync(filePath);
                return new DeserializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .IgnoreUnmatchedProperties()
                    .Build()
                    .Deserialize<NationalSymbolMetadata>(yaml);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to read national symbol metadata: {Slug}", slug);
                return null;
            }
        }

        private string? FindContentFile(string slug) => new[]
        {
            Path.Combine(_contentPath, $"{slug}.yaml"),
            Path.Combine(_contentPath, $"{slug}.yml")
        }.FirstOrDefault(File.Exists);

        private static string FormatTitle(string slug) =>
            System.Globalization.CultureInfo.InvariantCulture.TextInfo
                .ToTitleCase(slug.Replace('-', ' '));

        private sealed class NationalSymbolMetadata
        {
            public string Title { get; set; } = string.Empty;
            public string Summary { get; set; } = string.Empty;
            public int? Order { get; set; }
            public string SeoTitle { get; set; } = string.Empty;
            public string SeoDescription { get; set; } = string.Empty;
            public string IntroText { get; set; } = string.Empty;
            public string Audio { get; set; } = string.Empty;
            public List<NationalSymbolQuickFact>? QuickFacts { get; set; } = new();
            public List<NationalSymbolSource>? Sources { get; set; } = new();
        }

        private sealed class NationalSymbolQuickFact
        {
            public string Label { get; set; } = string.Empty;
            public string Value { get; set; } = string.Empty;
        }

        private sealed class NationalSymbolSource
        {
            public string Name { get; set; } = string.Empty;
            public string Url { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
        }
    }
}
