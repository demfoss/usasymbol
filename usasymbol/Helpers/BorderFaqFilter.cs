using System.Text.RegularExpressions;
using USASymbol.Models.Content;

namespace USASymbol.Helpers
{
    /// <summary>
    /// Border FAQ lists contain several rewordings of the same question
    /// ("What borders X?", "What states border X?", "How many states border X?").
    /// This keeps the first question of each kind and drops near-identical ones.
    /// </summary>
    public static class BorderFaqFilter
    {
        private static readonly HashSet<string> Stop = new(StringComparer.OrdinalIgnoreCase)
        {
            "what", "which", "who", "how", "many", "does", "do", "did", "is", "are", "the", "a", "an", "of",
            "to", "in", "on", "and", "or", "state", "states", "border", "borders", "bordering", "bordered",
            "by", "with", "its", "it", "have", "has"
        };

        public static List<BorderFaq> Dedupe(IEnumerable<BorderFaq> items, string stateName, int max = 6)
        {
            var kept = new List<(BorderFaq Item, HashSet<string> Key)>();
            foreach (var item in items)
            {
                var key = Tokens(item.Question, stateName);
                var duplicate = kept.Any(k => Similar(k.Key, key));
                if (duplicate) continue;
                kept.Add((item, key));
                if (kept.Count >= max) break;
            }
            return kept.Select(k => k.Item).ToList();
        }

        private static HashSet<string> Tokens(string question, string stateName)
        {
            var text = question.Replace(stateName, " ", StringComparison.OrdinalIgnoreCase);
            return Regex.Matches(text.ToLowerInvariant(), "[a-z]+")
                .Select(m => m.Value)
                .Where(w => !Stop.Contains(w))
                .ToHashSet();
        }

        private static bool Similar(HashSet<string> a, HashSet<string> b)
        {
            if (a.Count == 0 && b.Count == 0) return true;
            var inter = a.Intersect(b).Count();
            var union = a.Union(b).Count();
            return union > 0 && (double)inter / union >= 0.6;
        }
    }
}
