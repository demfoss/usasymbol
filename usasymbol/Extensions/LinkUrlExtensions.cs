using System;
using System.Collections.Generic;

namespace USASymbol.Extensions;

public static class LinkUrlExtensions
{
    private static readonly HashSet<string> SuppressedSourceHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "licenseplateroom.com",
        "www.licenseplateroom.com",
        "m.media-amazon.com",
        "brandywinegeneralstore.com",
        "www.brandywinegeneralstore.com",
        "worldpopulationreview.com",
        "www.worldpopulationreview.com",
        "netstate.com",
        "www.netstate.com",
        "statesymbolsusa.org",
        "www.statesymbolsusa.org",
    };

    // Partner site links to these hosts are intentionally dofollow (no "nofollow" in rel).
    // Every other external link on the site stays nofollow by default.
    private static readonly HashSet<string> DofollowExternalHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "globallicenseplates.com",
        "www.globallicenseplates.com",
    };

    public static bool IsExternalUrl(this string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        var trimmed = url.Trim();
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return !string.Equals(uri.Host, "usasymbol.com", StringComparison.OrdinalIgnoreCase) &&
               !string.Equals(uri.Host, "www.usasymbol.com", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// True for the small allowlist of external hosts (currently just our partner site,
    /// globallicenseplates.com) that should get a dofollow link instead of the default nofollow.
    /// </summary>
    public static bool IsDofollowExternalUrl(this string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        return DofollowExternalHosts.Contains(uri.Host);
    }

    /// <summary>
    /// The rel attribute value to use for an external link: dofollow-allowlisted hosts drop
    /// "nofollow" but keep the security-related tokens; everything else stays nofollow.
    /// </summary>
    public static string ExternalRel(this string? url)
    {
        return url.IsDofollowExternalUrl()
            ? "noopener noreferrer"
            : "nofollow noopener noreferrer";
    }

    public static bool IsSuppressedSourceUrl(this string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
        {
            return false;
        }

        return SuppressedSourceHosts.Contains(uri.Host);
    }
}
