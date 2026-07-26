using YamlDotNet.Serialization;

namespace USASymbol.Services.Content
{
    public sealed class LegacyRedirectService
    {
        private readonly IReadOnlyDictionary<string, string> _redirects;

        public LegacyRedirectService(
            IWebHostEnvironment environment,
            ILogger<LegacyRedirectService> logger)
        {
            var path = Path.Combine(environment.ContentRootPath, "Content", "legacy-redirects.yml");
            _redirects = Load(path, logger);
        }

        public bool TryResolve(PathString requestPath, out string target)
        {
            var path = requestPath.Value?.TrimEnd('/');
            if (string.IsNullOrWhiteSpace(path))
            {
                target = string.Empty;
                return false;
            }

            return _redirects.TryGetValue(path, out target!);
        }

        private static IReadOnlyDictionary<string, string> Load(
            string path,
            ILogger<LegacyRedirectService> logger)
        {
            if (!File.Exists(path))
            {
                logger.LogWarning("Legacy redirect configuration was not found at {Path}", path);
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            try
            {
                var yaml = File.ReadAllText(path);
                var data = new DeserializerBuilder()
                    .Build()
                    .Deserialize<Dictionary<string, Dictionary<string, string>>>(yaml);

                if (data == null || !data.TryGetValue("redirects", out var configured))
                    return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                var redirects = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var (source, target) in configured)
                {
                    var normalizedSource = NormalizePath(source);
                    var normalizedTarget = NormalizePath(target);

                    if (normalizedSource == null ||
                        normalizedTarget == null ||
                        string.Equals(normalizedSource, normalizedTarget, StringComparison.OrdinalIgnoreCase))
                    {
                        logger.LogWarning(
                            "Ignoring invalid legacy redirect {Source} -> {Target}",
                            source,
                            target);
                        continue;
                    }

                    redirects[normalizedSource] = normalizedTarget;
                }

                return redirects;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load legacy redirects from {Path}", path);
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        private static string? NormalizePath(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var path = value.Trim();
            if (!path.StartsWith('/'))
                return null;

            return path.Length > 1 ? path.TrimEnd('/') : path;
        }
    }
}
