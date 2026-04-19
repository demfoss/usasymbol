using System;
using System.Collections.Generic;

namespace USASymbol.Services.Images;

public static class BunnyImageClassMap
{
    private static readonly IReadOnlyDictionary<ImagePreset, string?> PresetToClassName =
        new Dictionary<ImagePreset, string?>
        {
            [ImagePreset.Thumbnail] = "thumbnail",
            [ImagePreset.Card] = "card",
            [ImagePreset.Hero] = "hero",
            [ImagePreset.Full] = null
        };

    public static string? GetClassName(ImagePreset preset)
    {
        if (!PresetToClassName.TryGetValue(preset, out var className))
        {
            throw new ArgumentOutOfRangeException(nameof(preset), preset, "Unknown image preset.");
        }

        return className;
    }
}
