using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using YamlDotNet.Serialization;

namespace USASymbol.Services.Yaml
{
    public sealed class YamlContentLoader
    {
        private readonly IMemoryCache _cache;
        private readonly IWebHostEnvironment _env;
        private readonly IDeserializer _deserializer;

        public YamlContentLoader(IMemoryCache cache, IWebHostEnvironment env)
        {
            _cache = cache;
            _env = env;
            _deserializer = new DeserializerBuilder().Build();
        }

        public Task<Dictionary<object, object>?> LoadAsync(string cacheKey, string relativePath)
        {
            return _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromHours(24);

                var path = Path.Combine(_env.ContentRootPath, relativePath);
                if (!File.Exists(path)) return null;

                var yaml = await File.ReadAllTextAsync(path);
                try
                {
                    return _deserializer.Deserialize<Dictionary<object, object>>(yaml);
                }
                catch
                {
                    return null;
                }
            });
        }
    }
}