using System;
using System.Text.RegularExpressions;

namespace USASymbol.Services.Images;

public sealed class ImagePathNormalizer : IImagePathNormalizer
{
    private static readonly Regex MultiSlashRegex = new("/+", RegexOptions.Compiled);

    public string Normalize(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        var value = path.Trim();

        if (IsAbsoluteUrl(value))
        {
            return value;
        }

        value = value.Replace('\\', '/');

        if (!value.StartsWith("/", StringComparison.Ordinal))
        {
            value = "/" + value;
        }

        return CollapseSlashes(value);
    }

    public bool IsAbsoluteUrl(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        return Uri.TryCreate(path.Trim(), UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    public bool IsLocalPath(string? path)
    {
        var normalized = Normalize(path);
        return !string.IsNullOrEmpty(normalized) && !IsAbsoluteUrl(normalized);
    }

    private static string CollapseSlashes(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return MultiSlashRegex.Replace(value, "/");
    }
}
