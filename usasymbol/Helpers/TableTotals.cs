using System.Globalization;
using USASymbol.Models.Content;

namespace Usasymbol.Helpers;

/// <summary>
/// Official U.S. total for an additive ranking metric (table.metric.additive + table.national.value),
/// with the part of it that the listed rows do not cover ("Other states").
/// </summary>
public sealed record TableTotals(string MetricKey, double Total, double Listed, string TotalLabel, bool ShowOther, string OtherLabel = "Other states")
{
    public double Other => Total - Listed;

    public static TableTotals? Resolve(PageTable? table)
    {
        if (table?.Metric?.Additive != true || table.National?.Value is not double total || total <= 0) return null;
        var key = table.DefaultColumn;
        var column = table.Columns.FirstOrDefault(c => c.Key == key);
        if (column == null || column.Type != "number") return null;

        double listed = 0;
        var count = 0;
        foreach (var row in table.Rows)
        {
            if (Number(row, key!) is double v) { listed += v; count++; }
        }
        if (count < 3 || listed > total * 1.001) return null; // listed rows must fit inside the total

        var label = string.IsNullOrWhiteSpace(table.National.Label) ? "U.S. total" : table.National.Label!;
        var other = string.IsNullOrWhiteSpace(table.National.OtherLabel) ? "Other states" : table.National.OtherLabel!;
        return new TableTotals(key!, total, listed, label, count < 50 && total - listed >= 0.5, other);
    }

    public static double? Number(TableRow row, string key) =>
        double.TryParse(row.GetString(key).Replace(",", "").Replace("$", "").Replace("%", ""),
            NumberStyles.Float, CultureInfo.InvariantCulture, out var v) && double.IsFinite(v) ? v : null;
}
