namespace USASymbol.Services
{
    /// <summary>
    /// One SSR State Match landing page: a fixed set of metric weights plus the SEO copy
    /// to go with it. Keep the "family" / "warm" / "career" weight sets identical to the
    /// matching client-side presets in wwwroot/js/state-match.js so the tool's preset pill
    /// lights up correctly when someone lands here.
    /// </summary>
    public sealed record StateMatchPresetDefinition(
        string Slug,
        string NavLabel,
        string MetaTitle,
        string MetaDescription,
        string Heading,
        string Lede,
        IReadOnlyDictionary<string, int> Weights);

    public static class StateMatchPresetCatalog
    {
        public static readonly IReadOnlyList<StateMatchPresetDefinition> All = new[]
        {
            new StateMatchPresetDefinition(
                "best-states-for-families",
                "Families",
                "Best States to Raise a Family | State Match",
                "See which U.S. states rank highest for family life — safety, schools, healthcare, and housing cost — then adjust the weights yourself.",
                "The best states for raising a family",
                "Ranked by safety, school quality, healthcare, and housing cost combined. Adjust any priority below to make it personal.",
                Weights(("safety", 100), ("education", 95), ("health", 88), ("housing", 62), ("cost", 60), ("jobs", 55), ("income", 48), ("incometax", 15), ("propertytax", 10), ("salestax", 10), ("warmth", 20))),

            new StateMatchPresetDefinition(
                "low-taxes",
                "Low Taxes",
                "States With the Lowest Taxes | State Match",
                "Compare U.S. states by income tax, property tax, and sales tax side by side, then adjust the priorities to match your own situation.",
                "The best states with the lowest taxes",
                "Ranked by income, property, and sales tax combined, with cost of living as a tiebreaker. Adjust any priority below to see how the ranking changes for you.",
                Weights(("incometax", 100), ("propertytax", 90), ("salestax", 80), ("cost", 40), ("housing", 30), ("jobs", 20), ("income", 20), ("safety", 20), ("health", 20), ("education", 10), ("warmth", 10))),

            new StateMatchPresetDefinition(
                "affordable-housing",
                "Affordable Housing",
                "Most Affordable States for Housing | State Match",
                "Find the U.S. states with the lowest home prices and cost of living, then adjust the priorities to fit your budget.",
                "The most affordable states for housing",
                "Ranked by home prices and everyday cost of living, with jobs, safety, and property tax as secondary factors.",
                Weights(("housing", 100), ("cost", 80), ("jobs", 30), ("income", 30), ("safety", 30), ("propertytax", 30), ("health", 20), ("education", 15), ("incometax", 20), ("salestax", 10), ("warmth", 10))),

            new StateMatchPresetDefinition(
                "remote-workers",
                "Remote Workers",
                "Best States for Remote Workers | State Match",
                "See which U.S. states stretch a remote salary furthest, based on cost of living, income tax, safety, and climate.",
                "The best states for remote workers",
                "Ranked by cost of living and income tax — since your paycheck doesn't change when you move, those matter most — plus safety and climate.",
                Weights(("cost", 70), ("incometax", 70), ("safety", 50), ("housing", 50), ("warmth", 40), ("health", 30), ("propertytax", 30), ("salestax", 20), ("income", 20), ("jobs", 10), ("education", 10))),

            new StateMatchPresetDefinition(
                "retirees",
                "Retirees",
                "Best States to Retire In | State Match",
                "Compare U.S. states for retirement by healthcare, safety, income and property tax, and climate, then adjust the priorities yourself.",
                "The best states to retire in",
                "Ranked by healthcare quality, safety, and how little of a fixed income taxes take, plus climate and cost of living.",
                Weights(("health", 100), ("safety", 80), ("incometax", 90), ("propertytax", 80), ("warmth", 60), ("cost", 60), ("housing", 50), ("salestax", 40), ("jobs", 5), ("income", 5), ("education", 5))),

            new StateMatchPresetDefinition(
                "warm-climate",
                "Warm Climate",
                "Warmest States to Live In | State Match",
                "See which U.S. states have the warmest year-round climate, then weigh in cost of living, housing, and jobs yourself.",
                "The warmest states to live in",
                "Ranked by average annual temperature first, with cost of living, housing, and jobs as secondary factors.",
                Weights(("warmth", 100), ("cost", 58), ("housing", 52), ("jobs", 45), ("safety", 42), ("health", 40), ("incometax", 15), ("propertytax", 10), ("salestax", 10), ("income", 30), ("education", 25))),

            new StateMatchPresetDefinition(
                "best-states-for-young-professionals",
                "Young Professionals",
                "Best States for Young Professionals | State Match",
                "Compare U.S. states by income, job market, and education for early-career movers, then adjust the priorities to match your own.",
                "The best states for young professionals",
                "Ranked by income, job market strength, and education, with cost of living and housing as secondary factors.",
                Weights(("income", 100), ("jobs", 96), ("education", 68), ("cost", 42), ("housing", 35), ("safety", 35), ("health", 40), ("incometax", 15), ("propertytax", 10), ("salestax", 10), ("warmth", 15)))
        };

        public static StateMatchPresetDefinition? Find(string slug) =>
            All.FirstOrDefault(preset => string.Equals(preset.Slug, slug, StringComparison.OrdinalIgnoreCase));

        private static IReadOnlyDictionary<string, int> Weights(params (string Key, int Value)[] pairs) =>
            pairs.ToDictionary(pair => pair.Key, pair => pair.Value);
    }
}
