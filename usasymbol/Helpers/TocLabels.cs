using System.Text.RegularExpressions;
using USASymbol.Models.ViewModels;

namespace USASymbol.Helpers
{
    /// <summary>
    /// Short (1-2 word) table-of-contents labels. Order of precedence:
    /// explicit nav_label in the content file, then keywords in the section title/id, then the
    /// first meaningful words of the title. Full question-style headings never reach the TOC.
    /// </summary>
    public static class TocLabels
    {
        private static readonly (Regex Pattern, string Label)[] Rules =
        {
            (new Regex(@"\b(adopt|enact|legislat|statute|law of|bird law|designat|official|wait|delay|chose|choose|chosen|vote|became|become|selection)\w*", RegexOptions.IgnoreCase | RegexOptions.Compiled), "Adoption"),
            (new Regex(@"\b(mean|symbol|say|represent|stand for|signif|reflect|fit)\w*", RegexOptions.IgnoreCase | RegexOptions.Compiled), "Meaning"),
            (new Regex(@"\b(histor|origin|timeline|earlier|previous|past)\w*", RegexOptions.IgnoreCase | RegexOptions.Compiled), "History"),
            (new Regex(@"\b(identif|look like|appearance|spot)\w*", RegexOptions.IgnoreCase | RegexOptions.Compiled), "Identification"),
            (new Regex(@"\b(habitat|range|where|location|live)\w*", RegexOptions.IgnoreCase | RegexOptions.Compiled), "Habitat"),
            (new Regex(@"\b(behavio|diet|eat|sing|song|call)\w*", RegexOptions.IgnoreCase | RegexOptions.Compiled), "Behavior"),
            (new Regex(@"\b(conserv|protect|threat|endanger|population)\w*", RegexOptions.IgnoreCase | RegexOptions.Compiled), "Conservation"),
            (new Regex(@"\b(compar|versus|vs\.?|other states|shared|also)\w*", RegexOptions.IgnoreCase | RegexOptions.Compiled), "Compare"),
            (new Regex(@"\b(fact|trivia|did you know)\w*", RegexOptions.IgnoreCase | RegexOptions.Compiled), "Facts"),
        };

        private static readonly HashSet<string> Stop = new(StringComparer.OrdinalIgnoreCase)
        {
            "a","an","the","of","to","in","on","at","for","and","or","is","are","was","were","does","do","did",
            "why","what","how","when","where","who","which","that","this","its","it","with","from","by","as","behind","beyond"
        };

        public static string For(IContentSection section, ISet<string>? used = null, int maxLength = 22)
        {
            var label = Pick(section, used, maxLength);
            if (used != null && !used.Add(label)) label = Unique(section, used, label);
            return label;
        }

        private static string Unique(IContentSection section, ISet<string> used, string label)
        {
            for (var i = 2; i < 10; i++)
            {
                var candidate = $"{label} {i}";
                if (used.Add(candidate)) return candidate;
            }
            return label;
        }

        private static string Pick(IContentSection section, ISet<string>? used, int maxLength)
        {
            var explicitLabel = section.NavLabel?.Trim();
            if (!string.IsNullOrEmpty(explicitLabel)) return explicitLabel;

            var title = (section.Title ?? string.Empty).Trim();
            var id = (section.Id ?? string.Empty).Replace('-', ' ').Replace('_', ' ');

            foreach (var (pattern, label) in Rules)
            {
                if (pattern.IsMatch(title) && used?.Contains(label) != true) return label;
            }

            // Short ids such as "history", "location", "about" are already section types.
            if (id.Length > 0 && id.Length <= 16 && id.Split(' ').Length <= 2 && used?.Contains(TitleCase(id)) != true) return TitleCase(id);

            foreach (var (pattern, label) in Rules)
            {
                if (pattern.IsMatch(id) && used?.Contains(label) != true) return label;
            }

            var words = Regex.Matches(title, @"[A-Za-z0-9'’]+")
                .Select(m => m.Value)
                .Where(w => !Stop.Contains(w))
                .Take(2)
                .ToList();
            var fallback = words.Count == 0 ? "Details" : string.Join(" ", words);
            return fallback.Length <= maxLength ? fallback : words[0];
        }

        private static string TitleCase(string text) =>
            string.Join(" ", text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => char.ToUpperInvariant(p[0]) + p[1..].ToLowerInvariant()));
    }
}
