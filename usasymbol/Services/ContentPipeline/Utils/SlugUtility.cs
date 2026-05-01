using System.Text.RegularExpressions;

namespace USASymbol.Services.ContentPipeline.Utils;

public sealed class SlugUtility
{
    public string ToSlug(string value)
    {
        var normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
        normalized = Regex.Replace(normalized, @"[^a-z0-9]+", "-");
        return normalized.Trim('-');
    }
}
