using USASymbol.Models.Content;
using USASymbol.Services.Interface;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace USASymbol.Services
{
    public class StateAbbreviationContentService : IStateAbbreviationContentService
    {
        private readonly ILogger<StateAbbreviationContentService> _logger;
        private readonly string _contentBasePath;
        private readonly IDeserializer _deserializer;

        public StateAbbreviationContentService(ILogger<StateAbbreviationContentService> logger, IWebHostEnvironment env)
        {
            _logger = logger;
            _contentBasePath = Path.Combine(env.ContentRootPath, "Content", "abbreviations");
            _deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();
        }

        public async Task<StateAbbreviationContent?> GetContentAsync(string stateSlug)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(stateSlug))
                {
                    return null;
                }

                var normalizedSlug = stateSlug.Trim().ToLowerInvariant();
                var filePath = Path.Combine(_contentBasePath, "states", $"{normalizedSlug}.yml");
                if (!File.Exists(filePath))
                {
                    return null;
                }

                var yaml = await File.ReadAllTextAsync(filePath);
                return _deserializer.Deserialize<StateAbbreviationContent>(yaml);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading abbreviation content for {StateSlug}", stateSlug);
                return null;
            }
        }

        public async Task<IReadOnlyList<string>> GetHighIntentSlugsAsync()
        {
            try
            {
                var filePath = Path.Combine(_contentBasePath, "index.yml");
                if (!File.Exists(filePath))
                {
                    return Array.Empty<string>();
                }

                var yaml = await File.ReadAllTextAsync(filePath);
                var content = _deserializer.Deserialize<StateAbbreviationIndexContent>(yaml);
                return content?.HighIntentSlugs ?? new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading abbreviation index content");
                return Array.Empty<string>();
            }
        }
    }
}
