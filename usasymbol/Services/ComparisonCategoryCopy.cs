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
            ["demographics"] = "Population, density, growth since 2020, domestic migration, and degree levels.",
            ["economy"] = "Median income, poverty rate, unemployment, job growth, childcare cost, and regional price parity.",
            ["jobs"] = "Minimum wage and unemployment benefits.",
            ["quality-of-life"] = "Livability score, cost of living, commute time, gas and electricity prices, and car insurance.",
            ["climate"] = "Average, summer, and winter temperature, sunny days, rainfall, wind speed, and lightning.",
            ["housing"] = "Home value, rent, owner costs, homeownership rate, and rent and home price relative to income.",
            ["retirement"] = "Retirement score.",
            ["taxes"] = "Income, sales, property, grocery, vehicle, and estate taxes, plus total tax burden.",
            ["politics"] = "Presidential margin, governor party, legislature control, trifecta, and electoral votes.",
            ["laws"] = "Gun, alcohol, marijuana, abortion, right-to-work, and marriage-age laws.",
            ["geography"] = "Land area, highest point, statehood date, capital, and region.",
            ["health"] = "Life expectancy, uninsured rate, obesity, infant mortality, and overdose deaths.",
            ["safety"] = "Violent and property crime rates.",
            ["education"] = "School rankings, spending per pupil, teacher salary, graduation rates, and student debt.",
            ["disasters"] = "Hurricane, tornado, earthquake, and wildfire risk.",
            ["culture"] = "Zoos, casinos, UFO sightings, and the most popular car.",
            ["infrastructure"] = "Water quality, power outages, road quality, renewable electricity, and largest airport.",
        };

        public static string Get(string categorySlug, string fallbackCategoryName) =>
            Descriptions.TryGetValue(categorySlug, out var desc) ? desc : $"{fallbackCategoryName} metrics for every state.";

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
