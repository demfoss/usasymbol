using System;
using System.Collections.Generic;
using System.Linq;

namespace USASymbol.Services.Images;

public static class ImageSrcSetExtensions
{
    public static string CardSrcSet(this IImageUrlService images, string? path, params int[] widths)
    {
        return Build(widths, width => images.Card(path, width));
    }

    public static string HeroSrcSet(this IImageUrlService images, string? path, params int[] widths)
    {
        return Build(widths, width => images.Hero(path, width));
    }

    public static string CropSrcSet(this IImageUrlService images, string? path, params (int Width, int Height)[] sizes)
    {
        return string.Join(", ", sizes
            .Where(size => size.Width > 0 && size.Height > 0)
            .GroupBy(size => size.Width)
            .Select(group => group.First())
            .Select(size => $"{images.Crop(path, size.Width, size.Height)} {size.Width}w"));
    }

    private static string Build(IEnumerable<int> widths, Func<int, string> urlForWidth)
    {
        return string.Join(", ", widths
            .Where(width => width > 0)
            .Distinct()
            .Select(width => $"{urlForWidth(width)} {width}w"));
    }
}
