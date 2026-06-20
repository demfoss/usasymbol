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
