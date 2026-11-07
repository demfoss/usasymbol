namespace usasymbol.Services
{
    /// <summary>Two-way links between collection pages and quizzes, so neither needs content edits.</summary>
    public static class QuizLinks
    {
        /// <summary>Quiz slug that fits a collection page, or null. Url looks like /collections/flags/best-and-worst-state-flags.</summary>
        public static string? QuizForCollection(string? url)
        {
            var parts = (url ?? "").Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3 || !parts[0].Equals("collections", StringComparison.OrdinalIgnoreCase)) return null;

            var group = parts[1].ToLowerInvariant();
            var slug = parts[2].ToLowerInvariant();
            return group switch
            {
                "flags" => "state-flags-quiz",
                "capitals" => "state-capitals-quiz",
                "nature" when slug.Contains("bird") => "state-birds-quiz",
                "nature" => "us-rankings-trivia-quiz",
                "geography" => "us-rankings-trivia-quiz",
                _ => null
            };
        }

        /// <summary>Collections worth linking from a quiz page.</summary>
        public static IReadOnlyList<(string Url, string Title)> CollectionsForQuiz(string quizSlug) => quizSlug switch
        {
            "state-flags-quiz" or "state-flag-identification-game" => new[]
            {
                ("/collections/flags/best-and-worst-state-flags", "Best and worst state flags"),
                ("/collections/flags/oldest-and-newest-state-flags", "Oldest and newest state flags"),
                ("/collections/flags/state-flag-redesigns", "State flag redesigns"),
            },
            "state-capitals-quiz" => new[]
            {
                ("/collections/capitals/state-capitals-that-changed-location", "State capitals that changed location"),
                ("/collections/capitals/smallest-state-capitals-by-population", "Smallest state capitals by population"),
            },
            "state-birds-quiz" => new[]
            {
                ("/collections/nature/rarest-state-birds", "Rarest state birds"),
            },
            _ => Array.Empty<(string, string)>()
        };
    }
}
