using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using USASymbol.Models.ViewModels;

namespace Usasymbol.Helpers
{
    public sealed class BigNumberStory
    {
        public string Label { get; set; } = "";
        public string Number { get; set; } = "";
        public string Subject { get; set; } = "";
        public string SubjectSlug { get; set; } = "";
        public string Copy { get; set; } = "";
    }

    public sealed class TypedTakeawayItem
    {
        public string Label { get; set; } = "";
        public string Icon { get; set; } = "fa-solid fa-star";
        public string Value { get; set; } = "";
        public string Subject { get; set; } = "";
        public string SubjectSlug { get; set; } = "";
    }

    public sealed class TypedTakeaways
    {
        public int StatesCovered { get; set; }
        public int StatesTotal { get; set; } = 50;
        public int CoveragePercent { get; set; }
        public List<TypedTakeawayItem> Highlights { get; set; } = new();
        public string Footnote { get; set; } = "";
    }

    /// <summary>
    /// Best-effort extraction of structured "key takeaway" data (a headline number, a state,
    /// a short label) out of the plain-prose quick_answer/table content that already exists in
    /// every page's YAML. There is no dedicated structured schema for this, so parsing is
    /// heuristic and callers should fall back to the plain list when too little can be found.
    /// </summary>
    public static class KeyTakeawaysBuilder
    {
        private static readonly (string Name, string Slug)[] States =
        {
            ("Alabama", "alabama"), ("Alaska", "alaska"), ("Arizona", "arizona"), ("Arkansas", "arkansas"),
            ("California", "california"), ("Colorado", "colorado"), ("Connecticut", "connecticut"), ("Delaware", "delaware"),
            ("Florida", "florida"), ("Georgia", "georgia"), ("Hawaii", "hawaii"), ("Idaho", "idaho"),
            ("Illinois", "illinois"), ("Indiana", "indiana"), ("Iowa", "iowa"), ("Kansas", "kansas"),
            ("Kentucky", "kentucky"), ("Louisiana", "louisiana"), ("Maine", "maine"), ("Maryland", "maryland"),
            ("Massachusetts", "massachusetts"), ("Michigan", "michigan"), ("Minnesota", "minnesota"), ("Mississippi", "mississippi"),
            ("Missouri", "missouri"), ("Montana", "montana"), ("Nebraska", "nebraska"), ("Nevada", "nevada"),
            ("New Hampshire", "new-hampshire"), ("New Jersey", "new-jersey"), ("New Mexico", "new-mexico"), ("New York", "new-york"),
            ("North Carolina", "north-carolina"), ("North Dakota", "north-dakota"), ("Ohio", "ohio"), ("Oklahoma", "oklahoma"),
            ("Oregon", "oregon"), ("Pennsylvania", "pennsylvania"), ("Rhode Island", "rhode-island"), ("South Carolina", "south-carolina"),
            ("South Dakota", "south-dakota"), ("Tennessee", "tennessee"), ("Texas", "texas"), ("Utah", "utah"),
            ("Vermont", "vermont"), ("Virginia", "virginia"), ("Washington", "washington"), ("West Virginia", "west-virginia"),
            ("Wisconsin", "wisconsin"), ("Wyoming", "wyoming"),
        };

        private static readonly Regex CurrencyPattern = new(@"\$\d[\d,]*(?:\.\d+)?", RegexOptions.Compiled);
        private static readonly Regex PercentPattern = new(@"\d[\d,]*(?:\.\d+)?%", RegexOptions.Compiled);
        private static readonly Regex NumberTokenPattern = new(@"\b\d[\d,]*(?:\.\d+)?\b", RegexOptions.Compiled);
        private static readonly Regex BareYearPattern = new(@"^(?:1[5-9]\d{2}|20\d{2})$", RegexOptions.Compiled);

        // Finds the first number-like token in reading order (currency, percent, or plain
        // number), skipping bare years (e.g. skips "2026" in "... by State 2026 puts Texas
        // first with 290 units" so it picks up "290" instead). Position in the sentence,
        // not token type, decides the winner: "... at 13.3%, ... over $1 million" correctly
        // resolves to 13.3% because it appears first, even though a dollar amount follows.
        private static Match? FindHeadlineNumber(string plain)
        {
            var candidates = new List<Match>();
            candidates.AddRange(CurrencyPattern.Matches(plain).Cast<Match>());
            candidates.AddRange(PercentPattern.Matches(plain).Cast<Match>());
            candidates.AddRange(NumberTokenPattern.Matches(plain).Cast<Match>());

            if (candidates.Count == 0)
            {
                return null;
            }

            // Several patterns can match at the same starting index (a percent match like
            // "13.3%" contains an embedded plain-number match "13.3"); keep the longest
            // match per position so the richer token (with $ or %) wins over its own substring.
            var ordered = candidates
                .GroupBy(m => m.Index)
                .Select(g => g.OrderByDescending(m => m.Length).First())
                .OrderBy(m => m.Index)
                .ToList();

            return ordered.FirstOrDefault(t => !LooksLikeBareYear(t.Value)) ?? ordered.FirstOrDefault();
        }

        private static bool LooksLikeBareYear(string token)
        {
            return !token.Contains(',') && !token.Contains('.') && BareYearPattern.IsMatch(token);
        }

        public static List<BigNumberStory> BuildBigNumberStories(IEnumerable<string>? quickAnswer, int max = 3)
        {
            var results = new List<BigNumberStory>();
            if (quickAnswer == null)
            {
                return results;
            }

            foreach (var raw in quickAnswer)
            {
                if (results.Count >= max)
                {
                    break;
                }

                if (string.IsNullOrWhiteSpace(raw))
                {
                    continue;
                }

                var plain = StripMarkdown(raw);
                var numberMatch = FindHeadlineNumber(plain);
                if (numberMatch == null)
                {
                    continue;
                }

                var subject = FindState(plain, numberMatch.Index);

                results.Add(new BigNumberStory
                {
                    Label = BuildLabel(plain, numberMatch.Index),
                    Number = numberMatch.Value,
                    Subject = subject.Name,
                    SubjectSlug = subject.Slug,
                    Copy = raw
                });
            }

            return results;
        }

        public static TypedTakeaways? BuildTypedTakeaways(PageDetailViewModel model)
        {
            var content = model?.Content;
            var table = content?.Table;
            if (table == null || table.Rows.Count == 0)
            {
                return null;
            }

            var stateSlugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in table.Rows)
            {
                var slug = row.GetString("state_slug");
                if (string.IsNullOrWhiteSpace(slug))
                {
                    var name = row.GetString("state");
                    slug = FindState(name).Slug;
                }

                if (!string.IsNullOrWhiteSpace(slug))
                {
                    stateSlugs.Add(slug);
                }
            }

            var covered = stateSlugs.Count;

            var highlights = BuildYearRangeHighlights(table) ?? BuildMostCommonHighlights(table, content?.DetailType);

            var footnote = content?.Page?.QuickAnswer?.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? "";

            return new TypedTakeaways
            {
                StatesCovered = covered,
                StatesTotal = 50,
                CoveragePercent = covered <= 0 ? 0 : (int)Math.Round(covered * 100.0 / 50),
                Highlights = highlights ?? new List<TypedTakeawayItem>(),
                Footnote = footnote
            };
        }

        // "Earliest" / "Most Recent" adoption, when the table tracks an adoption year —
        // a genuinely comparable, interesting pair rather than an arbitrary text metric.
        private static List<TypedTakeawayItem>? BuildYearRangeHighlights(USASymbol.Models.Content.PageTable table)
        {
            var yearKey = table.Columns
                .Select(c => c.Key)
                .FirstOrDefault(k =>
                    string.Equals(k, "adopted_year", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(k, "year_adopted", StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrWhiteSpace(yearKey))
            {
                return null;
            }

            var withYears = table.Rows
                .Select(row => new
                {
                    Year = ParseYear(row.GetString(yearKey!)),
                    State = row.GetString("state"),
                    StateSlug = ResolveSlug(row)
                })
                .Where(x => x.Year.HasValue && !string.IsNullOrWhiteSpace(x.State))
                .ToList();

            if (withYears.Count < 2)
            {
                return null;
            }

            var oldest = withYears.OrderBy(x => x.Year).First();
            var newest = withYears.OrderByDescending(x => x.Year).First();

            if (oldest.Year == newest.Year)
            {
                return null;
            }

            return new List<TypedTakeawayItem>
            {
                new() { Label = "Earliest", Icon = "fa-solid fa-clock-rotate-left", Value = oldest.Year!.Value.ToString(), Subject = oldest.State, SubjectSlug = oldest.StateSlug },
                new() { Label = "Most Recent", Icon = "fa-solid fa-clock", Value = newest.Year!.Value.ToString(), Subject = newest.State, SubjectSlug = newest.StateSlug }
            };
        }

        // Falls back to whichever answer states share most often (e.g. "Rodeo" claimed by
        // four states) when there's no adoption year to compare.
        private static List<TypedTakeawayItem>? BuildMostCommonHighlights(USASymbol.Models.Content.PageTable table, string? detailType)
        {
            var detailKey = detailType?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(detailKey))
            {
                return null;
            }

            var groups = table.Rows
                .Select(row => row.GetString(detailKey!).Trim())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .GroupBy(v => v, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .ToList();

            var top = groups.FirstOrDefault(g => g.Count() >= 2);
            if (top == null)
            {
                return null;
            }

            var highlights = new List<TypedTakeawayItem>
            {
                new() { Label = "Most Common", Icon = "fa-solid fa-layer-group", Value = top.First(), Subject = $"{top.Count()} states" }
            };

            var second = groups.Skip(1).FirstOrDefault(g => g.Count() >= 2);
            if (second != null)
            {
                highlights.Add(new TypedTakeawayItem { Label = "Also Common", Icon = "fa-solid fa-layer-group", Value = second.First(), Subject = $"{second.Count()} states" });
            }

            return highlights;
        }

        private static int? ParseYear(string value)
        {
            var digitsOnly = Regex.Match(value ?? "", @"\d{3,4}");
            return digitsOnly.Success && int.TryParse(digitsOnly.Value, out var year) ? year : null;
        }

        private static string ResolveSlug(USASymbol.Models.Content.TableRow row)
        {
            var slug = row.GetString("state_slug");
            return !string.IsNullOrWhiteSpace(slug) ? slug : FindState(row.GetString("state")).Slug;
        }

        private static string BuildLabel(string plain, int numberIndex)
        {
            var prefix = numberIndex > 0 ? plain.Substring(0, numberIndex) : "";
            prefix = prefix.Trim(' ', ',', '.', ';', ':', '-');

            if (prefix.Length > 46)
            {
                var cut = prefix.LastIndexOf(' ', Math.Min(45, prefix.Length - 1));
                prefix = cut > 10 ? prefix.Substring(0, cut) : prefix.Substring(0, 46);
            }

            return string.IsNullOrWhiteSpace(prefix) ? "Key fact" : prefix;
        }

        // Finds the state name that "owns" the number at numberIndex: the closest state
        // mentioned before it, since English almost always states the subject first (e.g.
        // "New York (10.9%), ... New Jersey (10.75%)" correctly attributes 10.9% to New
        // York, not to New Jersey just because "New Jersey" is the longer name). Pass
        // numberIndex = -1 to just take the first state mentioned, used when there's no
        // specific number to anchor against.
        private static (string Name, string Slug) FindState(string? text, int numberIndex = -1)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return ("", "");
            }

            var matches = new List<(string Name, string Slug, int Index)>();
            foreach (var state in States)
            {
                foreach (Match m in Regex.Matches(text, $@"\b{Regex.Escape(state.Name)}\b", RegexOptions.IgnoreCase))
                {
                    // "Washington, D.C." / "Washington D.C." names the district, not the
                    // state of Washington -- skip a match immediately followed by a D.C. marker.
                    if (string.Equals(state.Name, "Washington", StringComparison.OrdinalIgnoreCase))
                    {
                        var tail = text.Substring(m.Index + m.Length);
                        if (Regex.IsMatch(tail, @"^,?\s*D\.?C\.?\b", RegexOptions.IgnoreCase))
                        {
                            continue;
                        }
                    }

                    matches.Add((state.Name, state.Slug, m.Index));
                }
            }

            if (matches.Count == 0)
            {
                return ("", "");
            }

            if (numberIndex < 0)
            {
                var first = matches.OrderBy(m => m.Index).First();
                return (first.Name, first.Slug);
            }

            // English sentences almost always name the subject before its value ("Texas
            // requires 60 days", "New York (10.9%)"), so a state mentioned before the number
            // wins even if a different state sits closer in raw character count after it
            // (e.g. a trailing clause like "..., less than a tenth of Indiana's 90-day
            // requirement" shouldn't steal the subject from an earlier, correct state).
            var before = matches.Where(m => m.Index < numberIndex).ToList();
            var pick = before.Count > 0
                ? before.OrderBy(m => numberIndex - m.Index).First()
                : matches.OrderBy(m => m.Index - numberIndex).First();

            return (pick.Name, pick.Slug);
        }

        private static string StripMarkdown(string value)
        {
            var noLinks = Regex.Replace(value, @"\[(.*?)\]\((.*?)\)", "$1");
            var noBold = Regex.Replace(noLinks, @"\*\*(.*?)\*\*", "$1");
            return Regex.Replace(noBold, @"\*(.*?)\*", "$1");
        }
    }
}
