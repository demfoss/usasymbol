using System.Globalization;
using USASymbol.Models.Content;
using USASymbol.Models.ViewModels;

namespace Usasymbol.Helpers;

/// <summary>The U.S. figure a ranking row is compared against, with an honest description of where it comes from.</summary>
public sealed record NationalReferenceView(double Value, string Display, string Caption, bool IsOfficial, bool IsPercent)
{
    /// <summary>"+4.0 pts" for percentages, "+41%" for everything else; null when the comparison would be meaningless.</summary>
    public string? Delta(double stateValue)
    {
        var diff = stateValue - Value;
        if (Math.Abs(diff) < 0.0005) return "= U.S.";
        var sign = diff > 0 ? "+" : "\u2212";
        if (IsPercent) return $"{sign}{Math.Abs(diff).ToString("0.0", CultureInfo.InvariantCulture)} pts";
        if (Value <= 0) return null;
        var relative = Math.Abs(diff) / Math.Abs(Value) * 100;
        return $"{sign}{relative.ToString(relative >= 10 ? "0" : "0.0", CultureInfo.InvariantCulture)}%";
    }
}

public static class NationalReferenceResolver
{
    private static readonly CultureInfo En = CultureInfo.GetCultureInfo("en-US");

    public static NationalReferenceView? Resolve(PageTable? table, RankingNumericInsightsViewModel? insights)
    {
        var national = table?.National;
        if (table == null || national == null || insights == null) return null;
        // An additive total is not a per-state yardstick ("-77% vs U.S." would be meaningless);
        // it is shown as the "U.S. total" row and the "Share of U.S." column instead.
        if (table.Metric?.Additive == true) return null;

        var column = insights.MetricColumn;
        var isPercent = (column.Format?.Contains('%') ?? false) || column.Label.Contains('%')
            || column.Key.Split('_').Any(p => p is "pct" or "percent" or "percentage");

        if (national.Value is double official)
        {
            var label = string.IsNullOrWhiteSpace(national.Label) ? "U.S. overall" : national.Label!;
            var origin = string.Join(", ", new[] { national.Source, national.Year?.ToString(CultureInfo.InvariantCulture) }
                .Where(x => !string.IsNullOrWhiteSpace(x)));
            return new NationalReferenceView(official, Format(official, column, isPercent),
                string.IsNullOrEmpty(origin) ? label : $"{label} ({origin})", true, isPercent);
        }

        if (string.IsNullOrWhiteSpace(national.WeightKey)) return null;

        double weightedSum = 0, weightTotal = 0, cases = 0, impliedPopulation = 0;
        var kind = (national.WeightKind ?? "population").Trim().ToLowerInvariant();
        var used = 0;
        foreach (var point in insights.Ranked)
        {
            if (!TryNumber(point.Row, national.WeightKey!, out var weight) || weight <= 0) continue;
            used++;
            if (kind == "cases")
            {
                if (point.Value <= 0) continue;
                cases += weight;
                impliedPopulation += weight / point.Value;
            }
            else
            {
                weightedSum += weight * point.Value;
                weightTotal += weight;
            }
        }

        if (used < 3 || used != insights.Ranked.Count) return null; // partial weights would bias the figure
        double pooled;
        if (kind == "cases")
        {
            if (impliedPopulation <= 0) return null;
            pooled = cases / impliedPopulation;
        }
        else
        {
            if (weightTotal <= 0) return null;
            pooled = weightedSum / weightTotal;
        }

        var caption = $"Population-weighted average of {used} reporting states (not an official national figure)";
        return new NationalReferenceView(pooled, Format(pooled, column, isPercent), caption, false, isPercent);
    }

    private static string Format(double value, TableColumn column, bool isPercent)
    {
        var text = value.ToString(column.Format is { Length: > 0 } f && !f.Contains('%') && !f.Contains('\'') ? f : "0.0", En);
        return isPercent ? text + "%" : text;
    }

    private static bool TryNumber(TableRow row, string key, out double value)
    {
        value = 0;
        var text = row.GetString(key);
        if (string.IsNullOrWhiteSpace(text)) return false;
        return double.TryParse(text.Replace(",", "").Replace("%", "").Replace("$", "").Trim(),
            NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }
}
