namespace USASymbol.Services.Content
{
    public class UsStateRef
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string FlagUrl { get; set; } = string.Empty;
    }

    /// <summary>
    /// Shared static reference for the 50 states (no DC, matching the rest of the site's
    /// state coverage). Used by every /tools page that needs a code/name/slug/flag lookup
    /// without a DB round-trip.
    /// </summary>
    public static class UsStatesReference
    {
        public static readonly IReadOnlyDictionary<string, string> Names = new Dictionary<string, string>
        {
            ["AL"] = "Alabama", ["AK"] = "Alaska", ["AZ"] = "Arizona", ["AR"] = "Arkansas",
            ["CA"] = "California", ["CO"] = "Colorado", ["CT"] = "Connecticut", ["DE"] = "Delaware",
            ["FL"] = "Florida", ["GA"] = "Georgia", ["HI"] = "Hawaii", ["ID"] = "Idaho",
            ["IL"] = "Illinois", ["IN"] = "Indiana", ["IA"] = "Iowa", ["KS"] = "Kansas",
            ["KY"] = "Kentucky", ["LA"] = "Louisiana", ["ME"] = "Maine", ["MD"] = "Maryland",
            ["MA"] = "Massachusetts", ["MI"] = "Michigan", ["MN"] = "Minnesota", ["MS"] = "Mississippi",
            ["MO"] = "Missouri", ["MT"] = "Montana", ["NE"] = "Nebraska", ["NV"] = "Nevada",
            ["NH"] = "New Hampshire", ["NJ"] = "New Jersey", ["NM"] = "New Mexico", ["NY"] = "New York",
            ["NC"] = "North Carolina", ["ND"] = "North Dakota", ["OH"] = "Ohio", ["OK"] = "Oklahoma",
            ["OR"] = "Oregon", ["PA"] = "Pennsylvania", ["RI"] = "Rhode Island", ["SC"] = "South Carolina",
            ["SD"] = "South Dakota", ["TN"] = "Tennessee", ["TX"] = "Texas", ["UT"] = "Utah",
            ["VT"] = "Vermont", ["VA"] = "Virginia", ["WA"] = "Washington", ["WV"] = "West Virginia",
            ["WI"] = "Wisconsin", ["WY"] = "Wyoming",
        };

        public static string SlugFor(string stateName) =>
            stateName.ToLowerInvariant().Replace(" ", "-");

        public static string FlagUrlFor(string stateCode) =>
            $"/images/states/flags/small/{stateCode.ToLowerInvariant()}.webp";

        public static string? NameFor(string stateCode) =>
            Names.TryGetValue(stateCode.Trim().ToUpperInvariant(), out var name) ? name : null;

        /// <summary>Resolves a state by full name, abbreviation, or slug (case-insensitive).</summary>
        public static UsStateRef? Resolve(string? query)
        {
            if (string.IsNullOrWhiteSpace(query)) return null;
            var q = query.Trim();

            if (q.Length == 2 && Names.TryGetValue(q.ToUpperInvariant(), out var byCode))
            {
                return new UsStateRef { Code = q.ToUpperInvariant(), Name = byCode, Slug = SlugFor(byCode), FlagUrl = FlagUrlFor(q) };
            }

            var normalized = q.ToLowerInvariant().Replace("-", " ").Trim();
            foreach (var kv in Names)
            {
                if (kv.Value.Equals(q, StringComparison.OrdinalIgnoreCase) ||
                    kv.Value.ToLowerInvariant() == normalized ||
                    SlugFor(kv.Value) == q.ToLowerInvariant())
                {
                    return new UsStateRef { Code = kv.Key, Name = kv.Value, Slug = SlugFor(kv.Value), FlagUrl = FlagUrlFor(kv.Key) };
                }
            }

            return null;
        }

        public static List<UsStateRef> All() =>
            Names
                .Select(kv => new UsStateRef { Code = kv.Key, Name = kv.Value, Slug = SlugFor(kv.Value), FlagUrl = FlagUrlFor(kv.Key) })
                .OrderBy(s => s.Name)
                .ToList();
    }
}
