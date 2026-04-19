using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using USASymbol.Options;

namespace USASymbol.Services.Images;

public sealed class ImageUrlService : IImageUrlService
{
    private readonly BunnyOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly IImagePathNormalizer _pathNormalizer;

    public ImageUrlService(
        IOptions<BunnyOptions> options,
        IHostEnvironment environment,
        IImagePathNormalizer pathNormalizer)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _pathNormalizer = pathNormalizer ?? throw new ArgumentNullException(nameof(pathNormalizer));
    }

    public string Url(string? path) => Url(path, ImagePreset.Full);

    public string Url(string? path, ImagePreset preset)
    {
        var normalizedPath = _pathNormalizer.Normalize(path);

        if (string.IsNullOrEmpty(normalizedPath))
        {
            return string.Empty;
        }

        if (_pathNormalizer.IsAbsoluteUrl(normalizedPath))
        {
            return normalizedPath;
        }

        if (!_environment.IsProduction() || !_options.UseCdnInProduction)
        {
            return normalizedPath;
        }

        if (!IsImagePath(normalizedPath))
        {
            return normalizedPath;
        }

        var cdnBaseUrl = (_options.CdnBaseUrl ?? string.Empty).Trim().TrimEnd('/');
        if (string.IsNullOrEmpty(cdnBaseUrl))
        {
            return normalizedPath;
        }

        var resolved = $"{cdnBaseUrl}{normalizedPath}";
        if (IsVectorAsset(normalizedPath))
        {
            return resolved;
        }

        var optimizerClass = BunnyImageClassMap.GetClassName(preset);

        if (string.IsNullOrWhiteSpace(optimizerClass))
        {
            return resolved;
        }

        return AppendQueryString(resolved, "class", optimizerClass);
    }

    public string Thumb(string? path) => Url(path, ImagePreset.Thumbnail);

    public string Thumb(string? path, int width) => UrlWithWidth(path, width);

    public string Card(string? path) => Url(path, ImagePreset.Card);

    public string Card(string? path, int width) => UrlWithWidth(path, width);

    public string Hero(string? path) => Url(path, ImagePreset.Hero);

    public string Hero(string? path, int width) => UrlWithWidth(path, width);

    public string Full(string? path) => Url(path, ImagePreset.Full);

    private string UrlWithWidth(string? path, int width)
    {
        var normalizedPath = _pathNormalizer.Normalize(path);

        if (string.IsNullOrEmpty(normalizedPath))
        {
            return string.Empty;
        }

        if (_pathNormalizer.IsAbsoluteUrl(normalizedPath))
        {
            return normalizedPath;
        }

        if (!_environment.IsProduction() || !_options.UseCdnInProduction)
        {
            return normalizedPath;
        }

        if (!IsImagePath(normalizedPath))
        {
            return normalizedPath;
        }

        var cdnBaseUrl = (_options.CdnBaseUrl ?? string.Empty).Trim().TrimEnd('/');
        if (string.IsNullOrEmpty(cdnBaseUrl))
        {
            return normalizedPath;
        }

        var resolved = $"{cdnBaseUrl}{normalizedPath}";
        if (IsVectorAsset(normalizedPath) || width <= 0)
        {
            return resolved;
        }

        return AppendQueryString(resolved, "width", width.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    private bool IsImagePath(string path)
    {
        var localBasePath = (_options.LocalImagesBasePath ?? "/images").Trim();
        if (string.IsNullOrWhiteSpace(localBasePath))
        {
            localBasePath = "/images";
        }

        if (!localBasePath.StartsWith("/", StringComparison.Ordinal))
        {
            localBasePath = "/" + localBasePath;
        }

        localBasePath = localBasePath.TrimEnd('/');

        return path.StartsWith(localBasePath, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsVectorAsset(string path)
    {
        return path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".svgz", StringComparison.OrdinalIgnoreCase);
    }

    private static string AppendQueryString(string url, string key, string value)
    {
        var separator = url.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return $"{url}{separator}{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}";
    }
}
