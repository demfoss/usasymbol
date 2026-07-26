namespace USASymbol.Services
{
    /// <summary>
    /// Short editorial blurb per category — keyed by ComparisonMetricDefinition.GroupSlug.
    /// Shared between CompareController (page meta/intro) and Views/Compare/Hub.cshtml (category cards)
    /// so there's one place to update copy. Keep in sync with ComparisonMetricsConfig.GroupOrder
    /// (which also drives the Program.cs route regex for /compare/{category}).
    /// </summary>
    public static class ComparisonCategoryCopy
    {
        public static readonly IReadOnlyDictionary<string, string> Descriptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["demographics"] = "Population, growth, domestic migration, density, and education levels across all 50 states.",
            ["economy"] = "Median income, poverty, employment, living wage, and the real cost of a paycheck in every state.",
            ["jobs"] = "Minimum wage and unemployment benefits by state.",
            ["quality-of-life"] = "Cost of living, commute times, gas and electricity prices, and overall livability by state.",
            ["climate"] = "Average and seasonal temperatures, sunny days, rainfall, wind speed, and lightning density across all 50 states.",
            ["housing"] = "Home values, rent, and how much of an income housing actually costs in every state.",
            ["retirement"] = "A composite score for comparing states to retire in.",
            ["taxes"] = "Income, sales, property, grocery, vehicle, estate, and inheritance taxes across all 50 states.",
            ["politics"] = "2024 presidential margins, governor party, and legislative control by state.",
            ["laws"] = "Gun, alcohol, marijuana, abortion, right-to-work, and marriage-age laws across all 50 states.",
            ["geography"] = "Land area, highest points, statehood dates, and capitals for every state.",
            ["health"] = "Life expectancy, insurance, obesity, infant mortality, and overdose rates across all 50 states.",
            ["safety"] = "Violent and property crime rates by state.",
            ["education"] = "School rankings, spending per pupil, teacher pay, and graduation rates by state.",
            ["disasters"] = "Hurricane, tornado, earthquake, and wildfire risk across all 50 states.",
            ["culture"] = "Zoos, casinos, UFO reports, and popular cars across all 50 states.",
            ["infrastructure"] = "Water quality, power reliability, roads, renewable electricity, and major airports across all 50 states.",
        };

        public static string Get(string categorySlug, string fallbackCategoryName) =>
            Descriptions.TryGetValue(categorySlug, out var desc) ? desc : $"Compare {fallbackCategoryName.ToLowerInvariant()} across all 50 states.";

        public static readonly IReadOnlyDictionary<string, string> Icons = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["demographics"] = "fa-solid fa-users",
            ["economy"] = "fa-solid fa-sack-dollar",
            ["jobs"] = "fa-solid fa-briefcase",
            ["quality-of-life"] = "fa-solid fa-heart-pulse",
            ["climate"] = "fa-solid fa-cloud-sun",
            ["housing"] = "fa-solid fa-house",
            ["retirement"] = "fa-solid fa-umbrella-beach",
            ["taxes"] = "fa-solid fa-file-invoice-dollar",
            ["politics"] = "fa-solid fa-landmark-dome",
            ["laws"] = "fa-solid fa-scale-balanced",
            ["geography"] = "fa-solid fa-mountain-sun",
            ["health"] = "fa-solid fa-stethoscope",
            ["safety"] = "fa-solid fa-shield-halved",
            ["education"] = "fa-solid fa-graduation-cap",
            ["disasters"] = "fa-solid fa-house-crack",
            ["culture"] = "fa-solid fa-paw",
            ["infrastructure"] = "fa-solid fa-road-bridge",
        };

        public static string GetIcon(string categorySlug) =>
            Icons.TryGetValue(categorySlug, out var icon) ? icon : "fa-solid fa-layer-group";

        /// <summary>
        /// Compare GroupSlug -> Rankings category Id (Content/rankings/{id}/), for cross-linking the
        /// category hub to the matching deep-dive rankings section. Only includes pairs confirmed to be
        /// topically aligned by checking actual ranking content in each folder — several Compare categories
        /// (jobs, quality-of-life, climate, housing, retirement, safety, disasters) have no confident Rankings
        /// equivalent and are intentionally omitted rather than linked to a loosely-related category.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string> RankingsCategoryMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["demographics"] = "demographics",
            ["economy"] = "economy",
            ["education"] = "education",
            ["geography"] = "geography",
            ["health"] = "health",
            ["taxes"] = "taxes",
            ["culture"] = "culture",
            ["laws"] = "law",
            ["politics"] = "government",
            ["infrastructure"] = "infrastructure",
        };
    }
}
