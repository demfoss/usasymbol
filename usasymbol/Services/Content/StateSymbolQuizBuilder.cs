using Microsoft.Extensions.Caching.Memory;
using usasymbol.Models;
using YamlDotNet.Serialization;

namespace usasymbol.Services
{
    /// <summary>
    /// Builds a pool of "how well do you know {state}'s symbols" questions from the
    /// symbol listing files (Content/symbols/*.yml). Nothing is hand-written: every
    /// question comes from the same table rows the listing pages show.
    /// The browser picks and shuffles questions and choices on every play.
    /// </summary>
    public class StateSymbolQuizBuilder
    {
        // file, column holding the symbol name, label used in the question
        private static readonly (string File, string Field, string Label)[] Sources =
        {
            ("birds", "bird", "bird"),
            ("flowers", "flower", "flower"),
            ("trees", "tree", "tree"),
            ("reptiles", "reptile_name", "reptile"),
            ("amphibians", "amphibian_name", "amphibian"),
            ("minerals", "mineral_name", "mineral"),
            ("gemstones", "gemstone_name", "gemstone"),
            ("rocks", "rock_name", "rock"),
            ("fossils", "fossil_name", "fossil"),
            ("dinosaurs", "dinosaur", "dinosaur"),
            ("songs", "song", "song"),
            ("fruits", "food_name", "fruit"),
            ("vegetables", "food_name", "vegetable"),
            ("desserts", "food_name", "dessert"),
        };

        private static readonly HashSet<string> ReverseLabels = new() { "bird", "flower", "tree", "dinosaur", "mineral", "fossil" };

        private readonly IWebHostEnvironment _env;
        private readonly IMemoryCache _cache;
        private readonly IDeserializer _yaml = new DeserializerBuilder().Build();

        public StateSymbolQuizBuilder(IWebHostEnvironment env, IMemoryCache cache)
        {
            _env = env;
            _cache = cache;
        }

        private sealed record Row(string State, string Value, string? Extra);

        private sealed class Table
        {
            public string Label = "";
            public List<Row> All = new();      // every row (used to detect shared symbols)
            public List<Row> Single = new();   // states with exactly one entry
        }

        private static readonly string[] StateNames =
        {
            "Alabama","Alaska","Arizona","Arkansas","California","Colorado","Connecticut","Delaware","Florida","Georgia",
            "Hawaii","Idaho","Illinois","Indiana","Iowa","Kansas","Kentucky","Louisiana","Maine","Maryland","Massachusetts",
            "Michigan","Minnesota","Mississippi","Missouri","Montana","Nebraska","Nevada","New Hampshire","New Jersey",
            "New Mexico","New York","North Carolina","North Dakota","Ohio","Oklahoma","Oregon","Pennsylvania","Rhode Island",
            "South Carolina","South Dakota","Tennessee","Texas","Utah","Vermont","Virginia","Washington","West Virginia",
            "Wisconsin","Wyoming"
        };

        // A value like "Alabama Red-bellied Turtle" or "Missouri Waltz" gives the answer away.
        private static bool NamesAState(string v) =>
            StateNames.Any(n => v.Contains(n, StringComparison.OrdinalIgnoreCase));

        private List<Table> LoadTables() =>
            _cache.GetOrCreate("state-symbol-quiz-tables", e =>
            {
                e.SlidingExpiration = TimeSpan.FromHours(12);
                var tables = new List<Table>();
                foreach (var (file, field, label) in Sources)
                    AddTable(tables, file, field, label, null);

                AddTable(tables, "mottos", "motto", "motto", "translation");
                AddTable(tables, "nicknames", "nickname", "nickname", null);
                AddTable(tables, "license-plate-slogans", "slogan", "slogan", null);
                return tables;
            })!;

        private void AddTable(List<Table> tables, string file, string field, string label, string? extraField)
        {
            var path = Path.Combine(_env.ContentRootPath, "Content", "symbols", file + ".yml");
            if (!File.Exists(path)) return;
            try
            {
                var root = _yaml.Deserialize<Dictionary<object, object>>(File.ReadAllText(path));
                if (root == null || !root.TryGetValue("table", out var t) || t is not Dictionary<object, object> td) return;
                if (!td.TryGetValue("rows", out var r) || r is not List<object> rows) return;

                var list = new List<Row>();
                foreach (var item in rows.OfType<Dictionary<object, object>>())
                {
                    var state = item.GetValueOrDefault("state")?.ToString()?.Trim();
                    var value = item.GetValueOrDefault(field)?.ToString()?.Trim().Trim('"');
                    var extra = extraField == null ? null : item.GetValueOrDefault(extraField)?.ToString()?.Trim();
                    if (string.IsNullOrWhiteSpace(state) || string.IsNullOrWhiteSpace(value)) continue;
                    list.Add(new Row(state!, value!, extra));
                }

                // Only states with a single entry, so the "correct" answer is never ambiguous.
                var single = list.GroupBy(x => x.State).Where(g => g.Count() == 1).Select(g => g.First()).ToList();
                if (single.Count >= 8) tables.Add(new Table { Label = label, All = list, Single = single });
            }
            catch { /* malformed file: skip it */ }
        }

        public static string SlugFor(string stateName) =>
            StateLinks.Slug(stateName) + "-state-symbols-quiz";

        public static IReadOnlyList<string> AllStateNames => StateNames;

        /// <summary>Full quiz page for one state, e.g. slug "alabama-state-symbols-quiz".</summary>
        public Quiz? BuildQuiz(string slug)
        {
            var state = StateNames.FirstOrDefault(n => SlugFor(n).Equals(slug, StringComparison.OrdinalIgnoreCase));
            return state == null ? null : BuildQuizForState(state);
        }

        public Quiz? BuildQuizForState(string state)
        {
            var questions = Build(state);
            if (questions.Count < 8) return null;
            var stateSlug = state.ToLowerInvariant().Replace(' ', '-');
            return new Quiz
            {
                Title = $"{state} State Symbols Quiz",
                Slug = SlugFor(state),
                Category = "State Symbols",
                Difficulty = "Easy",
                EstimatedTime = "3-5 min",
                QuestionsPerRound = 10,
                Description = $"How well do you know {state}? Test yourself on its state bird, flower, tree, motto, nickname and more.",
                Tags = new List<string> { $"{state.ToLowerInvariant()} state symbols quiz", $"{state.ToLowerInvariant()} quiz", $"{state.ToLowerInvariant()} trivia" },
                Seo = new QuizSeo
                {
                    Title = $"{state} State Symbols Quiz",
                    Description = $"Free {state} quiz: name its official state bird, flower, tree, motto and more. Instant answers and explanations.",
                    IntroParagraphs = new List<string>
                    {
                        $"Think you know {state}? This quiz asks about the symbols the state has officially chosen, from the state bird and flower to its motto, nickname and license plate slogan.",
                        "Questions and answer positions are shuffled every round, so the right answer is never in the same place twice."
                    }
                },
                Content = new QuizContent
                {
                    Sections = new List<QuizSection>
                    {
                        new QuizSection
                        {
                            Id = "learn-more",
                            Title = $"Learn more about {state}",
                            Paragraphs = new List<string> { $"Missed a few? Every symbol in this quiz has its own page with the story behind it." },
                            Links = new List<QuizLink> { new QuizLink { Text = $"All {state} state symbols", Href = $"/states/{stateSlug}" } }
                        }
                    }
                },
                Faq = new List<QuizFaq>
                {
                    new QuizFaq { Question = $"What does the {state} quiz cover?", Answer = $"It covers {state}'s official symbols, such as its state bird, flower, tree and motto, plus a few questions that ask you to match a symbol to the state." },
                    new QuizFaq { Question = "Are the questions the same every time?", Answer = "No. Each round draws a fresh set of questions and shuffles the answers." }
                },
                Questions = questions
            };
        }

        public List<QuizQuestion> Build(string stateName)
        {
            var result = new List<QuizQuestion>();
            if (string.IsNullOrWhiteSpace(stateName)) return result;
            var rng = Random.Shared;

            foreach (var table in LoadTables())
            {
                var before = result.Count;
                var mine = table.Single.FirstOrDefault(x => x.State.Equals(stateName, StringComparison.OrdinalIgnoreCase));
                if (mine == null) continue;

                var others = table.Single.Where(x => !x.State.Equals(stateName, StringComparison.OrdinalIgnoreCase)).ToList();
                var shared = table.All.Count(x => x.Value.Equals(mine.Value, StringComparison.OrdinalIgnoreCase)) > 1;

                switch (table.Label)
                {
                    case "motto":
                        if (!string.IsNullOrWhiteSpace(mine.Extra))
                        {
                            var wrongMeanings = Pick(others.Select(x => x.Extra ?? ""), mine.Extra!, 3, rng);
                            if (wrongMeanings.Count == 3)
                                result.Add(Make($"What does {stateName}'s state motto, \"{mine.Value}\", mean?", mine.Extra!, wrongMeanings,
                                    $"\"{mine.Value}\" means \"{mine.Extra}\"."));
                        }
                        break;

                    case "nickname":
                        AddReverse(result, stateName, others, mine, shared,
                            $"Which state is known as \"{mine.Value}\"?", $"\"{mine.Value}\" is a nickname of {stateName}.");
                        break;

                    case "slogan":
                        AddReverse(result, stateName, others, mine, shared,
                            $"Which state's license plates carry the slogan \"{mine.Value}\"?", $"\"{mine.Value}\" is the license plate slogan of {stateName}.");
                        break;

                    default:
                        if (NamesAState(mine.Value)) break;
                        var wrong = Pick(others.Select(x => x.Value).Where(v => !NamesAState(v)), mine.Value, 3, rng);
                        if (wrong.Count == 3)
                            result.Add(Make($"What is the official state {table.Label} of {stateName}?", mine.Value, wrong,
                                $"The official state {table.Label} of {stateName} is {mine.Value}."));

                        if (ReverseLabels.Contains(table.Label))
                            AddReverse(result, stateName, others, mine, shared,
                                $"Which state has {mine.Value} as its state {table.Label}?",
                                $"{mine.Value} is the state {table.Label} of {stateName}.");
                        break;
                }

                var link = LinkFor(table.Label, stateName);
                for (var i = before; i < result.Count; i++)
                {
                    result[i].LinkUrl = link.Url;
                    result[i].LinkLabel = link.Label;
                }
            }
            return result;
        }

        private static (string Url, string Label) LinkFor(string label, string state)
        {
            var slug = StateLinks.Slug(state);
            return label switch
            {
                "slogan" => ($"/states/{slug}/license-plate", $"{state} license plate slogan"),
                "fruit" => ("/symbols/fruits", "All state fruits"),
                "vegetable" => ("/symbols/vegetables", "All state vegetables"),
                "dessert" => ("/symbols/desserts", "All state desserts"),
                _ => ($"/states/{slug}/{label}", $"{state} state {label}")
            };
        }

        private static void AddReverse(List<QuizQuestion> into, string stateName, List<Row> others, Row mine, bool shared,
            string question, string explanation)
        {
            if (shared || NamesAState(mine.Value)) return; // several states share it -> "which state" has no single answer
            var wrongStates = others
                .Where(x => !x.Value.Equals(mine.Value, StringComparison.OrdinalIgnoreCase))
                .Select(x => x.State).Distinct()
                .OrderBy(_ => Random.Shared.Next()).Take(3).ToList();
            if (wrongStates.Count < 3) return;
            into.Add(Make(question, stateName, wrongStates, explanation));
        }

        private static List<string> Pick(IEnumerable<string> pool, string correct, int n, Random rng) =>
            pool.Where(x => !string.IsNullOrWhiteSpace(x) && !x.Equals(correct, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(_ => rng.Next())
                .Take(n)
                .ToList();

        private static QuizQuestion Make(string q, string correct, List<string> wrong, string explanation)
        {
            // Correct answer lands in a random slot; the browser reshuffles on every play anyway.
            var choices = wrong.Append(correct).OrderBy(_ => Random.Shared.Next()).ToList();
            return new QuizQuestion { Question = q, Choices = choices, CorrectAnswer = correct, Explanation = explanation };
        }
    }
}
