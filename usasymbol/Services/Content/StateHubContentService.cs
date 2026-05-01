using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using USASymbol.Models.ViewModels;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace USASymbol.Services
{
    public interface IStateHubContentService
    {
        Task<StateHubContent?> GetHubAsync(string stateSlug);
    }

    public class StateHubContentService : IStateHubContentService
    {
        private readonly IMemoryCache _cache;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<StateHubContentService> _logger;
        private readonly IDeserializer _deserializer;

        public StateHubContentService(
            IMemoryCache cache,
            IWebHostEnvironment env,
            ILogger<StateHubContentService> logger)
        {
            _cache = cache;
            _env = env;
            _logger = logger;
            _deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();
        }

        public Task<StateHubContent?> GetHubAsync(string stateSlug)
        {
            var normalizedSlug = (stateSlug ?? string.Empty).Trim().Trim('/').ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(normalizedSlug))
            {
                return Task.FromResult<StateHubContent?>(null);
            }

            var cacheKey = $"state-hub:{normalizedSlug}";
            if (_cache.TryGetValue(cacheKey, out StateHubContent? cached))
            {
                return Task.FromResult(cached);
            }

            return LoadHubAsync(normalizedSlug, cacheKey);
        }

        private async Task<StateHubContent?> LoadHubAsync(string normalizedSlug, string cacheKey)
        {
            var path = Path.Combine(_env.ContentRootPath, "Content", "states", normalizedSlug, "hub.yaml");
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                var yaml = await File.ReadAllTextAsync(path);
                var hub = _deserializer.Deserialize<StateHubContent>(yaml);

                if (hub != null)
                {
                    _cache.Set(cacheKey, hub, new MemoryCacheEntryOptions
                    {
                        SlidingExpiration = TimeSpan.FromHours(6)
                    });
                }

                return hub;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not load state hub YAML: {Path}", path);
                return null;
            }
        }
    }
}
