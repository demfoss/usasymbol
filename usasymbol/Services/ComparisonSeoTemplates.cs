using USASymbol.Models.ViewModels;
using System.Globalization;

namespace USASymbol.Services;

public static class ComparisonSeoTemplates
{
    public static string OverviewTitle(StatePairComparisonViewModel model)
    {
        var cost = model.MetricResults.FirstOrDefault(result => result.Metric.Slug == "cost-of-living");
        if (cost?.NumericA is double valueA && cost.NumericB is double valueB)
        {
            return FitTitle(
                $"{model.StateA.Name} vs {model.StateB.Name} Cost of Living: {valueA:0.#} vs {valueB:0.#} (2026)",
                $"{model.StateA.Abbreviation} vs {model.StateB.Abbreviation} Cost of Living: {valueA:0.#} vs {valueB:0.#} (2026)");
        }

        return LimitTitle($"{model.StateA.Name} vs {model.StateB.Name}: Cost, Taxes & Living (2026)");
    }

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
        if (model.Result.NumericA is double valueA && model.Result.NumericB is double valueB)
        {
            var compactA = CompactValue(model.Metric.Slug, model.Metric.Unit, valueA);
            var compactB = CompactValue(model.Metric.Slug, model.Metric.Unit, valueB);
            return FitTitle(
                $"{model.StateA.Name} vs {model.StateB.Name} {label}: {compactA} vs {compactB} (2026)",
                $"{model.StateA.Abbreviation} vs {model.StateB.Abbreviation} {label}: {compactA} vs {compactB} (2026)");
        }

        return LimitTitle($"{model.StateA.Name} vs {model.StateB.Name}: {label} (2026)");
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
        LimitTitle($"{model.Pair.StateA.Name} vs {model.Pair.StateB.Name}: {model.CategoryName} (2026)");

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

    private static string CompactValue(string slug, string unit, double value) => unit switch
    {
        "%" => $"{value:0.##}%",
        "USD" when slug == "gas-price" => $"${value:0.000}",
        "USD" when slug is "owner-costs-with-mortgage" or "owner-costs-without-mortgage" or "median-rent"
            => $"${value:N0}",
        "USD" => Math.Abs(value) >= 1_000_000
            ? $"${value / 1_000_000:0.#}M"
            : Math.Abs(value) >= 1_000
                ? $"${value / 1_000:0.#}K"
                : $"${value:0.##}",
        "people" => Math.Abs(value) >= 1_000_000
            ? $"{value / 1_000_000:0.#}M"
            : Math.Abs(value) >= 1_000
                ? $"{value / 1_000:0.#}K"
                : value.ToString("N0", CultureInfo.InvariantCulture),
        "temp-f" => $"{value:0.#}°F",
        "cents-kwh" => $"{value:0.##}¢/kWh",
        "cents-gal" => $"{value:0.##}¢/gal",
        "sq mi" => $"{value:N0} sq mi",
        "per sq mi" => $"{value:0.#}/sq mi",
        "per 100k" or "per 100,000" => $"{value:0.#}/100k",
        "per 1,000" => $"{value:0.##}/1k",
        "inches" => $"{value:0.#} in",
        "mph" => $"{value:0.0} mph",
        "ft" => $"{value:N0} ft",
        "days" => $"{value:N0} days",
        "years" => $"{value:0.#} yrs",
        _ => value.ToString("0.##", CultureInfo.InvariantCulture)
    };

    private static string FitTitle(string full, string compact) =>
        full.Length <= 68 ? full : LimitTitle(compact);

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
