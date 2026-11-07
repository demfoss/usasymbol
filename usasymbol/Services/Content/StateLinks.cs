using System.Text.RegularExpressions;

namespace usasymbol.Services
{
    /// <summary>State names, postal codes and slugs, plus quiz-question to symbol-page link rules.</summary>
    public static class StateLinks
    {
        public static readonly IReadOnlyDictionary<string, string> AbbrToName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["AL"] = "Alabama", ["AK"] = "Alaska", ["AZ"] = "Arizona", ["AR"] = "Arkansas", ["CA"] = "California",
            ["CO"] = "Colorado", ["CT"] = "Connecticut", ["DE"] = "Delaware", ["FL"] = "Florida", ["GA"] = "Georgia",
            ["HI"] = "Hawaii", ["ID"] = "Idaho", ["IL"] = "Illinois", ["IN"] = "Indiana", ["IA"] = "Iowa",
            ["KS"] = "Kansas", ["KY"] = "Kentucky", ["LA"] = "Louisiana", ["ME"] = "Maine", ["MD"] = "Maryland",
            ["MA"] = "Massachusetts", ["MI"] = "Michigan", ["MN"] = "Minnesota", ["MS"] = "Mississippi", ["MO"] = "Missouri",
            ["MT"] = "Montana", ["NE"] = "Nebraska", ["NV"] = "Nevada", ["NH"] = "New Hampshire", ["NJ"] = "New Jersey",
            ["NM"] = "New Mexico", ["NY"] = "New York", ["NC"] = "North Carolina", ["ND"] = "North Dakota", ["OH"] = "Ohio",
            ["OK"] = "Oklahoma", ["OR"] = "Oregon", ["PA"] = "Pennsylvania", ["RI"] = "Rhode Island", ["SC"] = "South Carolina",
            ["SD"] = "South Dakota", ["TN"] = "Tennessee", ["TX"] = "Texas", ["UT"] = "Utah", ["VT"] = "Vermont",
            ["VA"] = "Virginia", ["WA"] = "Washington", ["WV"] = "West Virginia", ["WI"] = "Wisconsin", ["WY"] = "Wyoming",
        };

        public static IEnumerable<string> Names => AbbrToName.Values;

        public static string Slug(string stateName) => stateName.ToLowerInvariant().Replace(' ', '-');

        // quiz slug -> (symbol url segment, human label)
        private static readonly Dictionary<string, (string Segment, string Label)> QuizTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            ["state-birds-quiz"] = ("bird", "state bird"),
            ["state-flags-quiz"] = ("flag", "state flag"),
            ["state-flag-identification-game"] = ("flag", "state flag"),
            ["state-license-plate-slogans-quiz"] = ("license-plate", "license plate slogan"),
            ["state-mottos-quiz"] = ("motto", "state motto"),
            ["state-nicknames-quiz"] = ("nickname", "state nickname"),
            ["state-seals-quiz"] = ("state-seal", "state seal"),
            ["state-abbreviations-quiz"] = ("abbreviation", "state abbreviation"),
        };

        /// <summary>Finds the state a question is about: postal code field, then the answer, then the question text.</summary>
        public static string? ResolveState(string? abbreviation, string? correctAnswer, string? question)
        {
            if (!string.IsNullOrWhiteSpace(abbreviation) && AbbrToName.TryGetValue(abbreviation.Trim(), out var byCode))
                return byCode;

            var answer = correctAnswer?.Trim();
            if (!string.IsNullOrEmpty(answer))
            {
                var hit = Names.FirstOrDefault(n => n.Equals(answer, StringComparison.OrdinalIgnoreCase));
                if (hit != null) return hit;
            }

            if (!string.IsNullOrEmpty(question))
            {
                var found = Names.Where(n => Regex.IsMatch(question, $@"\b{n}\b")).ToList();
                // "Virginia" inside "West Virginia" is the same hit, not a second state
                found = found.Where(n => !found.Any(o => o != n && o.Contains(n))).ToList();
                if (found.Count == 1) return found[0];
            }
            return null;
        }

        public static (string Url, string Label) For(string quizSlug, string stateName)
        {
            var slug = Slug(stateName);
            if (QuizTypes.TryGetValue(quizSlug, out var t))
                return ($"/states/{slug}/{t.Segment}", $"{stateName} {t.Label}");
            return ($"/states/{slug}", $"{stateName} state guide");
        }
    }
}
