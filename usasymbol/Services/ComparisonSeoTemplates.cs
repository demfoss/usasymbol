using USASymbol.Models.ViewModels;

namespace USASymbol.Services;

public static class ComparisonSeoTemplates
{
    public static string OverviewTitle(StatePairComparisonViewModel model) =>
        LimitTitle($"{model.StateA.Name} vs {model.StateB.Name} | Cost, Taxes & Living");

    public static string OverviewDescription(StatePairComparisonViewModel model)
    {
        var cost = model.MetricResults.FirstOrDefault(result => result.Metric.Slug == "cost-of-living");
        var numericLead = cost?.NumericA is double valueA && cost.NumericB is double valueB
            ? $"2026 cost-of-living index: {model.StateA.Name} {valueA:0.#}, {model.StateB.Name} {valueB:0.#}. "
            : string.Empty;
        return Limit($"{numericLead}Compare taxes, housing, jobs, climate, safety, laws, and quality of life using sourced state data.");
    }

    public static string MetricTitle(MetricComparisonViewModel model)
    {
        var label = ShortMetricName(model.Metric.Slug, model.Metric.Name);
        return LimitTitle($"{model.StateA.Name} vs {model.StateB.Name} | {label}");
    }

    public static string MetricDescription(MetricComparisonViewModel model)
    {
        var answer = model.Result.SummaryText;
        var fallback = $"Compare {model.Metric.Name.ToLowerInvariant()} in {model.StateA.Name} and {model.StateB.Name}. See values, the difference, all-state rankings, source, and data year.";
        var valueLead = !string.IsNullOrWhiteSpace(model.Result.DisplayValueA) && !string.IsNullOrWhiteSpace(model.Result.DisplayValueB)
            ? $"{model.StateA.Name}: {model.Result.DisplayValueA}; {model.StateB.Name}: {model.Result.DisplayValueB}. "
            : string.Empty;
        if (!string.IsNullOrWhiteSpace(valueLead))
        {
            var contextual = $"2026 comparison — {model.StateA.Name} {model.Metric.Name.ToLowerInvariant()}: {model.Result.DisplayValueA}; {model.StateB.Name}: {model.Result.DisplayValueB}. See the difference, all-state rankings, primary source, and data year.";
            if (contextual.Length <= 158)
            {
                return contextual;
            }

            var concise = $"2026 comparison — {valueLead}See the difference, all-state rankings, primary source, and data year.";
            if (concise.Length <= 158)
            {
                return concise;
            }

            return Limit($"2026 — {model.StateA.Abbreviation}: {model.Result.DisplayValueA}; {model.StateB.Abbreviation}: {model.Result.DisplayValueB}. See rankings and primary source.");
        }

        return Limit(string.IsNullOrWhiteSpace(answer)
            ? fallback
            : $"{answer} Compare 2026 values, rankings, source, and data year.");
    }

    public static string CategoryPairTitle(CompareCategoryPairViewModel model) =>
        LimitTitle($"{model.Pair.StateA.Name} vs {model.Pair.StateB.Name} | {model.CategoryName}");

    private static string ShortMetricName(string slug, string defaultName) => slug switch
    {
        "owner-costs-with-mortgage" => "Owner Costs With Mortgage",
        "owner-costs-without-mortgage" => "Owner Costs Without Mortgage",
        "employment-population-ratio" => "Employment Rate",
        "presidential-voting-margin" => "2024 Voting Margin",
        "older-adult-health-outcomes" => "Older Adult Health",
        "marijuana-legalization" => "Marijuana Laws",
        "student-teacher-ratio" => "Student–Teacher Ratio",
        "aza-zoos" => "Accredited Zoos",
        _ => defaultName
    };

    private static string LimitTitle(string value) => LimitAtWord(value, 68);

    private static string Limit(string value) => LimitAtWord(value, 158);

    private static string LimitAtWord(string value, int maxLength)
    {
        if (value.Length <= maxLength) return value;

        var cut = value.LastIndexOf(' ', maxLength - 1);
        if (cut < maxLength / 2) cut = maxLength - 1;
        return value[..cut].TrimEnd(' ', ',', ';', ':', '.') + "…";
    }
}
