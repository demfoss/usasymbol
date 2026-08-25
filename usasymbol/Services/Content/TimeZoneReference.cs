namespace USASymbol.Services.Content
{
    public class TimeZoneInfo2
    {
        public string Name { get; set; } = string.Empty;
        public string Abbreviation { get; set; } = string.Empty;
        public string DstAbbreviation { get; set; } = string.Empty;
        public int StandardUtcOffset { get; set; }
        public bool ObservesDst { get; set; }
        public string IanaZone { get; set; } = string.Empty;
    }

    public class StateTimeZoneResult
    {
        public string StateCode { get; set; } = string.Empty;
        public string StateName { get; set; } = string.Empty;
        public string StateSlug { get; set; } = string.Empty;
        public string FlagUrl { get; set; } = string.Empty;
        public string ZoneName { get; set; } = string.Empty;
        public string Abbreviation { get; set; } = string.Empty;
        public string DstAbbreviation { get; set; } = string.Empty;
        public int StandardUtcOffset { get; set; }
        public bool ObservesDst { get; set; }
        public bool SpansMultipleZones { get; set; }
        public string IanaZone { get; set; } = string.Empty;
    }

    /// <summary>
    /// Primary (majority-area) time zone per state, for a quick-reference tool — not a
    /// parcel-accurate lookup. States that legitimately span more than one zone are flagged
    /// via <see cref="SplitStates"/> so the UI can show a disclaimer.
    /// </summary>
    public static class TimeZoneReference
    {
        public static readonly IReadOnlyDictionary<string, TimeZoneInfo2> Zones = new Dictionary<string, TimeZoneInfo2>
        {
            ["Eastern"] = new() { Name = "Eastern Time", Abbreviation = "EST", DstAbbreviation = "EDT", StandardUtcOffset = -5, ObservesDst = true, IanaZone = "America/New_York" },
            ["Central"] = new() { Name = "Central Time", Abbreviation = "CST", DstAbbreviation = "CDT", StandardUtcOffset = -6, ObservesDst = true, IanaZone = "America/Chicago" },
            ["Mountain"] = new() { Name = "Mountain Time", Abbreviation = "MST", DstAbbreviation = "MDT", StandardUtcOffset = -7, ObservesDst = true, IanaZone = "America/Denver" },
            ["MountainNoDst"] = new() { Name = "Mountain Time", Abbreviation = "MST", DstAbbreviation = "MST", StandardUtcOffset = -7, ObservesDst = false, IanaZone = "America/Phoenix" },
            ["Pacific"] = new() { Name = "Pacific Time", Abbreviation = "PST", DstAbbreviation = "PDT", StandardUtcOffset = -8, ObservesDst = true, IanaZone = "America/Los_Angeles" },
            ["Alaska"] = new() { Name = "Alaska Time", Abbreviation = "AKST", DstAbbreviation = "AKDT", StandardUtcOffset = -9, ObservesDst = true, IanaZone = "America/Anchorage" },
            ["Hawaii"] = new() { Name = "Hawaii-Aleutian Time", Abbreviation = "HST", DstAbbreviation = "HST", StandardUtcOffset = -10, ObservesDst = false, IanaZone = "Pacific/Honolulu" },
        };

        public static readonly IReadOnlyDictionary<string, string> StateZoneKey = new Dictionary<string, string>
        {
            ["AL"] = "Central", ["AK"] = "Alaska", ["AZ"] = "MountainNoDst", ["AR"] = "Central",
            ["CA"] = "Pacific", ["CO"] = "Mountain", ["CT"] = "Eastern", ["DE"] = "Eastern",
            ["FL"] = "Eastern", ["GA"] = "Eastern", ["HI"] = "Hawaii", ["ID"] = "Mountain",
            ["IL"] = "Central", ["IN"] = "Eastern", ["IA"] = "Central", ["KS"] = "Central",
            ["KY"] = "Eastern", ["LA"] = "Central", ["ME"] = "Eastern", ["MD"] = "Eastern",
            ["MA"] = "Eastern", ["MI"] = "Eastern", ["MN"] = "Central", ["MS"] = "Central",
            ["MO"] = "Central", ["MT"] = "Mountain", ["NE"] = "Central", ["NV"] = "Pacific",
            ["NH"] = "Eastern", ["NJ"] = "Eastern", ["NM"] = "Mountain", ["NY"] = "Eastern",
            ["NC"] = "Eastern", ["ND"] = "Central", ["OH"] = "Eastern", ["OK"] = "Central",
            ["OR"] = "Pacific", ["PA"] = "Eastern", ["RI"] = "Eastern", ["SC"] = "Eastern",
            ["SD"] = "Central", ["TN"] = "Central", ["TX"] = "Central", ["UT"] = "Mountain",
            ["VT"] = "Eastern", ["VA"] = "Eastern", ["WA"] = "Pacific", ["WV"] = "Eastern",
            ["WI"] = "Central", ["WY"] = "Mountain",
        };

        public static readonly HashSet<string> SplitStates = new(StringComparer.OrdinalIgnoreCase)
        {
            "FL", "ID", "IN", "KS", "KY", "MI", "NE", "NV", "ND", "OR", "SD", "TN", "TX"
        };

        public static StateTimeZoneResult? Lookup(string stateCode)
        {
            var code = (stateCode ?? "").Trim().ToUpperInvariant();
            if (!StateZoneKey.TryGetValue(code, out var zoneKey)) return null;
            if (!Zones.TryGetValue(zoneKey, out var zone)) return null;

            var stateName = UsStatesReference.NameFor(code) ?? code;

            return new StateTimeZoneResult
            {
                StateCode = code,
                StateName = stateName,
                StateSlug = UsStatesReference.SlugFor(stateName),
                FlagUrl = UsStatesReference.FlagUrlFor(code),
                ZoneName = zone.Name,
                Abbreviation = zone.Abbreviation,
                DstAbbreviation = zone.DstAbbreviation,
                StandardUtcOffset = zone.StandardUtcOffset,
                ObservesDst = zone.ObservesDst,
                SpansMultipleZones = SplitStates.Contains(code),
                IanaZone = zone.IanaZone
            };
        }
    }
}
