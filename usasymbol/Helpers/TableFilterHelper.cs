using USASymbol.Models.Content;

namespace Usasymbol.Helpers
{
    public static class TableFilterHelper
    {
        private static readonly HashSet<string> StructuralTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "rank", "state-link", "image", "link"
        };

        public record QuickFilterColumn(string Key, string Label, List<string> Values);

        public static QuickFilterColumn? GetQuickFilterColumn(PageTable? table)
        {
            if (table == null || table.Rows.Count < 8) return null;

            foreach (var col in table.Columns)
            {
                if (string.IsNullOrWhiteSpace(col.Key)) continue;
                if (StructuralTypes.Contains(col.Type)) continue;
                if (string.Equals(col.Key, "notes", StringComparison.OrdinalIgnoreCase)) continue;
                if (string.Equals(col.Key, "note", StringComparison.OrdinalIgnoreCase)) continue;
                if (string.Equals(col.Key, "state", StringComparison.OrdinalIgnoreCase)) continue;
                if (col.Key.EndsWith("_slug", StringComparison.OrdinalIgnoreCase)) continue;

                var values = new List<string>();
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var nonEmptyCount = 0;

                foreach (var row in table.Rows)
                {
                    if (!row.Data.TryGetValue(col.Key, out var raw)) continue;
                    var value = raw?.ToString()?.Trim();
                    if (string.IsNullOrEmpty(value)) continue;

                    nonEmptyCount++;
                    if (seen.Add(value)) values.Add(value);
                }

                if (values.Count < 2 || values.Count > 6) continue;
                if (values.Count >= nonEmptyCount) continue;

                return new QuickFilterColumn(col.Key, col.Label, values);
            }

            return null;
        }

        public static string? GetQuickFilterValue(QuickFilterColumn? column, TableRow row)
        {
            if (column == null) return null;
            return row.Data.TryGetValue(column.Key, out var raw) ? raw?.ToString()?.Trim() : null;
        }
    }
}
