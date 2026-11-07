namespace USASymbol.Helpers
{
    /// <summary>Display names and fallback icons for symbol types stored in Symbols.Type.</summary>
    public static class SymbolTypeInfo
    {
        private static readonly Dictionary<string, (string Label, string Icon)> Types = new(StringComparer.OrdinalIgnoreCase)
        {
            ["amphibian"] = ("State Amphibian", "fa-frog"),
            ["beverage"] = ("State Beverage", "fa-glass-water"),
            ["bird"] = ("State Bird", "fa-dove"),
            ["coat-of-arms"] = ("Coat of Arms", "fa-shield-halved"),
            ["color"] = ("State Colors", "fa-palette"),
            ["dance"] = ("State Dance", "fa-music"),
            ["dinosaur"] = ("State Dinosaur", "fa-bone"),
            ["firearm"] = ("State Firearm", "fa-crosshairs"),
            ["fish"] = ("State Fish", "fa-fish"),
            ["flag"] = ("State Flag", "fa-flag"),
            ["flower"] = ("State Flower", "fa-seedling"),
            ["food"] = ("State Food", "fa-utensils"),
            ["fossil"] = ("State Fossil", "fa-bone"),
            ["gemstone"] = ("State Gemstone", "fa-gem"),
            ["insect"] = ("State Insect", "fa-bug"),
            ["license-plate"] = ("License Plate", "fa-car"),
            ["mammal"] = ("State Mammal", "fa-paw"),
            ["mineral"] = ("State Mineral", "fa-gem"),
            ["motto"] = ("State Motto", "fa-scroll"),
            ["nickname"] = ("State Nickname", "fa-signature"),
            ["reptile"] = ("State Reptile", "fa-staff-snake"),
            ["rock"] = ("State Rock", "fa-mountain"),
            ["soil"] = ("State Soil", "fa-layer-group"),
            ["song"] = ("State Song", "fa-music"),
            ["sport"] = ("State Sport", "fa-medal"),
            ["sports"] = ("State Sport", "fa-medal"),
            ["state-seal"] = ("State Seal", "fa-stamp"),
            ["tree"] = ("State Tree", "fa-tree"),
        };

        public static string Label(string? type, string? designation = null)
        {
            var d = (designation ?? string.Empty).Trim();
            if (d.Length is > 3 and <= 24 && d.StartsWith("state ", StringComparison.OrdinalIgnoreCase))
                return System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(d.ToLowerInvariant());

            if (type != null && Types.TryGetValue(type, out var t)) return t.Label;
            var words = (type ?? "symbol").Split('-', StringSplitOptions.RemoveEmptyEntries);
            return "State " + System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(string.Join(' ', words));
        }

        public static string Icon(string? type)
            => type != null && Types.TryGetValue(type, out var t) ? t.Icon : "fa-star";
    }
}
