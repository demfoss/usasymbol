using Microsoft.AspNetCore.Html;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

namespace USASymbol.Extensions
{
    public static class MarkdownExtensions
    {
        private sealed class AutoLinkScope
        {
            public int PageLinkCount { get; set; }
            public HashSet<string> UsedUrls { get; } = new(StringComparer.OrdinalIgnoreCase);
            public string CurrentPath { get; set; } = string.Empty;
        }

        private sealed record AutoLinkTarget(string Label, string Url, Regex Pattern, int Priority, bool IsState, bool IsCompareHub = false);

        private static readonly Regex LeadingBulletRegex =
            new(@"^\s*(?:[•·●▪\-–—*]+\s+)+", RegexOptions.Compiled);

        private static readonly Regex MdLinkRegex =
            new(@"\[([^\]]+?)\]\(([^)\s]+?)\)", RegexOptions.Compiled);

        private static readonly Regex BareUrlRegex =
            new(@"(?<![""'>])(https?:\/\/[^\s<]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex BoldRegex =
            new(@"\*\*(.+?)\*\*", RegexOptions.Compiled);

        private static readonly Regex ItalicRegex =
            new(@"(?<!\*)\*(?!\*)(.+?)(?<!\*)\*(?!\*)", RegexOptions.Compiled);

        private static readonly Regex ExistingMarkdownLinkRegex =
            new(@"\[[^\]]+?\]\([^)]+?\)", RegexOptions.Compiled);

        private static readonly Regex SafeHtmlTagRegex = new(
            @"&lt;(/?(?:i|b|u|s|p|em|strong|br|sup|sub|mark|small|abbr|code|span))\s*/?&gt;",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex H1Regex = new(@"(?m)^\s{2,}h1:\s*""?(.+?)""?\s*$", RegexOptions.Compiled);
        private static readonly Regex AutoLinkPhrasesRegex = new(@"(?m)^auto_link_phrases:\s*\n((?:[ \t]+-[ \t]+.+\n?)+)", RegexOptions.Compiled);
        private static readonly Regex PhraseListItemRegex = new(@"(?m)^[ \t]+-[ \t]+""?(.+?)""?\s*$", RegexOptions.Compiled);
        private static readonly Regex LeadingUsRegex = new(@"^U\.?S\.?\s+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Dictionary<string, string> StateSlugs = new(StringComparer.OrdinalIgnoreCase)
        {
            {"Alabama", "alabama"}, {"Alaska", "alaska"}, {"Arizona", "arizona"}, {"Arkansas", "arkansas"},
            {"California", "california"}, {"Colorado", "colorado"}, {"Connecticut", "connecticut"},
            {"Delaware", "delaware"}, {"Florida", "florida"}, {"Georgia", "georgia"}, {"Hawaii", "hawaii"},
            {"Idaho", "idaho"}, {"Illinois", "illinois"}, {"Indiana", "indiana"}, {"Iowa", "iowa"},
            {"Kansas", "kansas"}, {"Kentucky", "kentucky"}, {"Louisiana", "louisiana"}, {"Maine", "maine"},
            {"Maryland", "maryland"}, {"Massachusetts", "massachusetts"}, {"Michigan", "michigan"},
            {"Minnesota", "minnesota"}, {"Mississippi", "mississippi"}, {"Missouri", "missouri"},
            {"Montana", "montana"}, {"Nebraska", "nebraska"}, {"Nevada", "nevada"}, {"New Hampshire", "new-hampshire"},
            {"New Jersey", "new-jersey"}, {"New Mexico", "new-mexico"}, {"New York", "new-york"},
            {"North Carolina", "north-carolina"}, {"North Dakota", "north-dakota"}, {"Ohio", "ohio"},
            {"Oklahoma", "oklahoma"}, {"Oregon", "oregon"}, {"Pennsylvania", "pennsylvania"},
            {"Rhode Island", "rhode-island"}, {"South Carolina", "south-carolina"}, {"South Dakota", "south-dakota"},
            {"Tennessee", "tennessee"}, {"Texas", "texas"}, {"Utah", "utah"}, {"Vermont", "vermont"},
            {"Virginia", "virginia"}, {"Washington", "washington"}, {"West Virginia", "west-virginia"},
            {"Wisconsin", "wisconsin"}, {"Wyoming", "wyoming"}
        };

        private static readonly AsyncLocal<AutoLinkScope?> CurrentAutoLinkScope = new();
        private static readonly List<AutoLinkTarget> AutoLinkTargets = BuildAutoLinkTargets();
        private const int MaxAutoLinksPerPage = 6;
        private const int MaxAutoLinksPerParagraph = 2;
        private const int MinCharsBetweenAutoLinks = 40;

        public static void ResetAutoLinkContext(string? currentPath = null)
        {
            CurrentAutoLinkScope.Value = new AutoLinkScope
            {
                CurrentPath = currentPath ?? string.Empty
            };
        }

        private static string Normalize(string text)
        {
            text = WebUtility.HtmlDecode((text ?? "").Trim());
            text = LeadingBulletRegex.Replace(text, "");
            return text;
        }

        private static bool TrySanitizeUrl(string rawUrl, out string safeUrl, out bool isExternal)
        {
            safeUrl = "";
            isExternal = false;

            if (string.IsNullOrWhiteSpace(rawUrl))
                return false;

            var url = rawUrl.Trim();

            if (url.StartsWith("/", StringComparison.Ordinal))
            {
                safeUrl = url;
                return true;
            }

            if (url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
            {
                safeUrl = url;
                return true;
            }

            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                if (uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) ||
                    uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
                {
                    safeUrl = uri.ToString();
                    isExternal = !IsInternalHost(uri.Host);
                    return true;
                }
            }

            return false;
        }

        private static bool IsInternalHost(string host)
        {
            return host.Equals("usasymbol.com", StringComparison.OrdinalIgnoreCase) ||
                   host.Equals("www.usasymbol.com", StringComparison.OrdinalIgnoreCase);
        }

        private static string RenderMarkdownLinks(string htmlEncodedText)
        {
            htmlEncodedText = MdLinkRegex.Replace(htmlEncodedText, m =>
            {
                var label = m.Groups[1].Value;
                var urlRaw = m.Groups[2].Value;
                var decodedUrl = WebUtility.HtmlDecode(urlRaw);

                if (!TrySanitizeUrl(decodedUrl, out var safeUrl, out var external))
                    return label;

                var href = WebUtility.HtmlEncode(safeUrl);
                var cls = "text-slate-900 border-b border-slate-300 hover:border-slate-900 hover:text-blue-700 transition-colors";
                var rel = external ? " rel=\"nofollow noopener noreferrer\"" : "";
                var target = external ? " target=\"_blank\"" : "";

                return $"<a href=\"{href}\" class=\"{cls}\"{target}{rel}>{label}</a>";
            });

            htmlEncodedText = BareUrlRegex.Replace(htmlEncodedText, m =>
            {
                var url = m.Groups[1].Value;
                var decoded = WebUtility.HtmlDecode(url);

                if (!TrySanitizeUrl(decoded, out var safeUrl, out var external))
                    return url;

                var href = WebUtility.HtmlEncode(safeUrl);
                var cls = "text-slate-900 border-b border-slate-300 hover:border-slate-900 hover:text-blue-700 transition-colors";
                var rel = external ? " rel=\"nofollow noopener noreferrer\"" : "";
                var target = external ? " target=\"_blank\"" : "";

                return $"<a href=\"{href}\" class=\"{cls}\"{target}{rel}>{url}</a>";
            });

            return htmlEncodedText;
        }

        private static List<AutoLinkTarget> BuildAutoLinkTargets()
        {
            var targets = new List<AutoLinkTarget>();

            foreach (var pair in StateSlugs)
            {
                var pattern = new Regex(@"\b" + Regex.Escape(pair.Key) + @"\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                targets.Add(new AutoLinkTarget(pair.Key, $"/states/{pair.Value}", pattern, 300 - pair.Key.Length, true));
            }

            foreach (var phrase in new[] { "compare states", "Compare States", "compare two states", "Compare two states" })
            {
                var pattern = new Regex(@"\b" + Regex.Escape(phrase) + @"\b", RegexOptions.Compiled);
                targets.Add(new AutoLinkTarget(phrase, "/compare-states", pattern, 180 - phrase.Length, false, true));
            }

            foreach (var target in LoadSymbolCategoryTargets())
                targets.Add(target);

            foreach (var target in BuildStateSymbolPhraseTargets())
                targets.Add(target);

            foreach (var target in LoadRankingTargets())
                targets.Add(target);

            foreach (var target in LoadCollectionTargets())
                targets.Add(target);

            foreach (var target in LoadParkTargets())
                targets.Add(target);

            foreach (var target in LoadBorderTargets())
                targets.Add(target);

            foreach (var target in LoadAbbreviationTargets())
                targets.Add(target);

            return targets
                .OrderBy(t => t.Priority)
                .ThenByDescending(t => t.Label.Length)
                .ToList();
        }

        private static IEnumerable<string> ExtractPhrasesFromYaml(string content, string url)
        {
            var phrases = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void Yield(string? raw)
            {
                if (string.IsNullOrWhiteSpace(raw)) return;
                var clean = raw.Trim().Trim('"');
                foreach (var sep in new[] { '|', ':', ',' })
                {
                    var idx = clean.IndexOf(sep);
                    if (idx > 0) clean = clean[..idx].Trim();
                }
                clean = LeadingUsRegex.Replace(clean, "").Trim();
                if (clean.Length < 4 || !seen.Add(clean)) return;
                phrases.Add(clean);
            }

            // 1. slug-derived phrase (always present)
            var slug = url.Split('/').Last().Replace('-', ' ');
            Yield(slug);

            // 2. page.h1 (nested under "page:")
            var h1 = H1Regex.Match(content);
            if (h1.Success)
                Yield(h1.Groups[1].Value.Trim().Trim('"'));

            // 3. auto_link_phrases: list in YAML (manual/generated override)
            var block = AutoLinkPhrasesRegex.Match(content);
            if (block.Success)
            {
                foreach (Match item in PhraseListItemRegex.Matches(block.Groups[1].Value))
                    Yield(item.Groups[1].Value.Trim().Trim('"'));
            }

            return phrases;
        }

        private static IEnumerable<AutoLinkTarget> LoadYamlUrlTargets(string contentSubPath, int basePriority)
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Content", contentSubPath);
            if (!Directory.Exists(path))
                yield break;

            var seenPhraseUrl = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var file in Directory.EnumerateFiles(path, "*.yml", SearchOption.AllDirectories))
            {
                var content = File.ReadAllText(file);
                var urlMatch = Regex.Match(content, @"(?m)^url:\s*(.+?)\s*$");
                if (!urlMatch.Success)
                    continue;

                var url = urlMatch.Groups[1].Value.Trim().Trim('"');
                if (string.IsNullOrWhiteSpace(url))
                    continue;

                foreach (var phrase in ExtractPhrasesFromYaml(content, url))
                {
                    // skip if another URL already owns this phrase (first writer wins)
                    if (!seenPhraseUrl.Add(phrase.ToLowerInvariant()))
                        continue;

                    var pattern = new Regex(@"\b" + Regex.Escape(phrase) + @"\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                    yield return new AutoLinkTarget(phrase, url, pattern, basePriority - phrase.Length, false);
                }
            }
        }

        private static IEnumerable<AutoLinkTarget> LoadRankingTargets()
            => LoadYamlUrlTargets("rankings", 95);

        private static IEnumerable<AutoLinkTarget> LoadCollectionTargets()
            => LoadYamlUrlTargets("collections", 90);

        private static IEnumerable<AutoLinkTarget> LoadParkTargets()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Content", "parks", "national");
            if (!Directory.Exists(path))
                yield break;

            foreach (var file in Directory.EnumerateFiles(path, "*.yml"))
            {
                var content = File.ReadAllText(file);
                var slugMatch = Regex.Match(content, @"(?m)^slug:\s*(.+?)\s*$");
                var nameMatch = Regex.Match(content, @"(?m)^name:\s*""?(.+?)""?\s*$");
                if (!slugMatch.Success || !nameMatch.Success)
                    continue;

                var slug = slugMatch.Groups[1].Value.Trim().Trim('"');
                var name = nameMatch.Groups[1].Value.Trim();
                if (string.IsNullOrWhiteSpace(slug) || string.IsNullOrWhiteSpace(name))
                    continue;

                var url = $"/national-parks/{slug}";
                var pattern = new Regex(@"\b" + Regex.Escape(name) + @"\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                yield return new AutoLinkTarget(name, url, pattern, 85 - name.Length, false);
            }
        }

        private static IEnumerable<AutoLinkTarget> LoadBorderTargets()
        {
            foreach (var (stateName, stateSlug) in StateSlugs)
            {
                var url = $"/states/{stateSlug}/borders";
                var phrases = new[]
                {
                    $"states that border {stateName}",
                    $"{stateName} border states",
                    $"{stateName} borders",
                };
                foreach (var phrase in phrases)
                {
                    var pattern = new Regex(@"\b" + Regex.Escape(phrase) + @"\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                    yield return new AutoLinkTarget(phrase, url, pattern, 60 - phrase.Length, false);
                }
            }
        }

        private static IEnumerable<AutoLinkTarget> LoadAbbreviationTargets()
        {
            foreach (var (stateName, stateSlug) in StateSlugs)
            {
                var url = $"/states/{stateSlug}/abbreviation";
                var phrases = new[]
                {
                    $"{stateName} abbreviation",
                    $"{stateName} postal abbreviation",
                    $"{stateName} state abbreviation",
                };
                foreach (var phrase in phrases)
                {
                    var pattern = new Regex(@"\b" + Regex.Escape(phrase) + @"\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                    yield return new AutoLinkTarget(phrase, url, pattern, 65 - phrase.Length, false);
                }
            }
        }

        private static IEnumerable<AutoLinkTarget> LoadSymbolCategoryTargets()
        {
            var contentSymbolsPath = Path.Combine(AppContext.BaseDirectory, "Content", "symbols");

            if (!Directory.Exists(contentSymbolsPath))
                yield break;

            foreach (var filePath in Directory.EnumerateFiles(contentSymbolsPath, "*.yml"))
            {
                var fileContent = File.ReadAllText(filePath);
                var urlMatch = Regex.Match(fileContent, @"(?m)^url:\s*(.+?)\s*$");
                var detailTypeMatch = Regex.Match(fileContent, @"(?m)^detail_type:\s*(.+?)\s*$");

                if (!urlMatch.Success || !detailTypeMatch.Success)
                    continue;

                var url = urlMatch.Groups[1].Value.Trim().Trim('"');
                var detailType = detailTypeMatch.Groups[1].Value.Trim().Trim('"');

                foreach (var phrase in GetSymbolPhrases(detailType))
                {
                    var pattern = new Regex(@"\b" + Regex.Escape(phrase) + @"\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                    yield return new AutoLinkTarget(phrase, url, pattern, 100 - phrase.Length, false);
                }
            }
        }

        private static IEnumerable<string> GetSymbolPhrases(string detailType)
        {
            return detailType.ToLowerInvariant() switch
            {
                "bird" => new[] { "official state bird", "official state birds", "state bird", "state birds" },
                "flower" => new[] { "official state flower", "official state flowers", "state flower", "state flowers" },
                "flag" => new[] { "official state flag", "official state flags", "state flag", "state flags" },
                "tree" => new[] { "official state tree", "official state trees", "state tree", "state trees" },
                "motto" => new[] { "official state motto", "official state mottos", "state motto", "state mottos" },
                "nickname" => new[] { "official state nickname", "official state nicknames", "state nickname", "state nicknames" },
                "mammal" => new[] { "official state mammal", "official state mammals", "state mammal", "state mammals" },
                "beverage" => new[] { "official state beverage", "official state beverages", "state beverage", "state beverages" },
                "dinosaur" => new[] { "official state dinosaur", "official state dinosaurs", "state dinosaur", "state dinosaurs" },
                "firearm" => new[] { "official state firearm", "official state firearms", "state firearm", "state firearms" },
                "color" => new[] { "official state color", "official state colors", "state color", "state colors" },
                "state-seal" => new[] { "official state seal", "official state seals", "state seal", "state seals", "great seal" },
                "song" => new[] { "official state song", "official state songs", "state song", "state songs" },
                "soil" => new[] { "official state soil", "state soil" },
                "license-plate" => new[] { "state license plate", "state license plates", "license plate slogan" },
                "coat-of-arms" => new[] { "coat of arms", "state coat of arms" },
                "abbreviation" => new[] { "state abbreviation", "postal abbreviation", "two-letter abbreviation" },
                _ => Array.Empty<string>()
            };
        }

        // Generates "Arizona state bird" → /states/arizona/bird for all 50 × categories
        private static IEnumerable<AutoLinkTarget> BuildStateSymbolPhraseTargets()
        {
            var categoryLabels = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["bird"] = new[] { "state bird", "state birds" },
                ["flower"] = new[] { "state flower", "state flowers" },
                ["flag"] = new[] { "state flag", "state flags" },
                ["tree"] = new[] { "state tree", "state trees" },
                ["motto"] = new[] { "state motto" },
                ["nickname"] = new[] { "state nickname" },
                ["mammal"] = new[] { "state mammal" },
                ["seal"] = new[] { "state seal", "great seal" },
            };

            foreach (var (stateName, stateSlug) in StateSlugs)
            {
                foreach (var (typeKey, labels) in categoryLabels)
                {
                    var url = $"/states/{stateSlug}/{typeKey}";
                    foreach (var label in labels)
                    {
                        var phrase = $"{stateName} {label}";
                        var pattern = new Regex(
                            @"\b" + Regex.Escape(phrase) + @"\b",
                            RegexOptions.Compiled | RegexOptions.IgnoreCase);
                        // priority 50 = higher than generic category phrases (100-N) so state-specific wins
                        yield return new AutoLinkTarget(phrase, url, pattern, 50 - phrase.Length, false);
                    }
                }
            }
        }

        private static HashSet<(int Start, int End)> GetOccupiedRanges(string text)
        {
            var occupied = new HashSet<(int, int)>();
            foreach (Match m in ExistingMarkdownLinkRegex.Matches(text))
                occupied.Add((m.Index, m.Index + m.Length));
            foreach (Match m in BareUrlRegex.Matches(text))
                occupied.Add((m.Index, m.Index + m.Length));
            return occupied;
        }

        private static bool IsInOccupiedRange(int start, int end, HashSet<(int Start, int End)> occupied)
        {
            foreach (var (oStart, oEnd) in occupied)
            {
                if (start < oEnd && end > oStart)
                    return true;
            }
            return false;
        }

        private static string AutoLinkContent(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            var scope = CurrentAutoLinkScope.Value ??= new AutoLinkScope();
            var remainingPageBudget = MaxAutoLinksPerPage - scope.PageLinkCount;
            if (remainingPageBudget <= 0)
                return text;

            var occupied = GetOccupiedRanges(text);
            var remainingParagraphBudget = Math.Min(MaxAutoLinksPerParagraph, remainingPageBudget);
            var matches = new List<(int Start, int Length, AutoLinkTarget Target, string Value)>();

            foreach (var target in AutoLinkTargets)
            {
                if (scope.UsedUrls.Contains(target.Url))
                    continue;

                if (IsSelfLink(scope, target))
                    continue;

                foreach (Match match in target.Pattern.Matches(text))
                {
                    if (!match.Success || !IsSafeAutoLinkMatch(text, match, target))
                        continue;

                    if (IsInOccupiedRange(match.Index, match.Index + match.Length, occupied))
                        continue;

                    matches.Add((match.Index, match.Length, target, match.Value));
                }
            }

            if (matches.Count == 0)
                return text;

            var accepted = new List<(int Start, int Length, AutoLinkTarget Target, string Value)>();

            foreach (var match in matches
                .OrderBy(m => m.Start)
                .ThenBy(m => m.Target.Priority)
                .ThenByDescending(m => m.Length))
            {
                if (accepted.Count >= remainingParagraphBudget)
                    break;

                var overlaps = accepted.Any(existing =>
                    match.Start < existing.Start + existing.Length &&
                    existing.Start < match.Start + match.Length);
                if (overlaps)
                    continue;

                var tooClose = accepted.Any(existing => Math.Abs(match.Start - existing.Start) < MinCharsBetweenAutoLinks);
                if (tooClose)
                    continue;

                var duplicateUrlInParagraph = accepted.Any(existing =>
                    string.Equals(existing.Target.Url, match.Target.Url, StringComparison.OrdinalIgnoreCase));
                if (duplicateUrlInParagraph)
                    continue;

                accepted.Add(match);
            }

            if (accepted.Count == 0)
                return text;

            var output = text;

            foreach (var match in accepted.OrderByDescending(m => m.Start))
            {
                var replacement = $"[{match.Value}]({match.Target.Url})";
                output = output[..match.Start] + replacement + output[(match.Start + match.Length)..];
            }

            scope.PageLinkCount += accepted.Count;
            foreach (var match in accepted)
            {
                scope.UsedUrls.Add(match.Target.Url);
            }
            return output;
        }

        private static bool IsSafeAutoLinkMatch(string text, Match match, AutoLinkTarget target)
        {
            if (match.Index > 0)
            {
                var before = text[match.Index - 1];
                if (before == '[' || before == '/' || before == '-')
                    return false;
            }

            var end = match.Index + match.Length;
            if (end < text.Length)
            {
                var after = text[end];
                if (after == ']' || after == '/' || after == '-' || after == '\'' || after == '’')
                    return false;
            }

            if (target.IsState)
            {
                var tail = text[end..];
                if (tail.StartsWith(" County", StringComparison.OrdinalIgnoreCase) ||
                    tail.StartsWith(" Parish", StringComparison.OrdinalIgnoreCase) ||
                    tail.StartsWith(" Avenue", StringComparison.OrdinalIgnoreCase) ||
                    tail.StartsWith(" Street", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsSelfLink(AutoLinkScope scope, AutoLinkTarget target)
        {
            if (string.IsNullOrWhiteSpace(scope.CurrentPath))
                return false;

            // General: never link to exact current page
            if (scope.CurrentPath.Equals(target.Url, StringComparison.OrdinalIgnoreCase))
                return true;

            if (target.IsState)
            {
                // Sub-pages don't link back to their parent state hub
                return scope.CurrentPath.StartsWith(target.Url + "/", StringComparison.OrdinalIgnoreCase);
            }

            if (target.IsCompareHub)
            {
                return scope.CurrentPath.StartsWith("/compare/", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        public static HtmlString RenderMarkdown(this string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new HtmlString(string.Empty);

            text = Normalize(text);

            var encoded = WebUtility.HtmlEncode(text);
            encoded = SafeHtmlTagRegex.Replace(encoded, "<$1>");
            encoded = RenderMarkdownLinks(encoded);
            encoded = BoldRegex.Replace(encoded, "<strong class=\"font-semibold text-gray-900\">$1</strong>");
            encoded = ItalicRegex.Replace(encoded, "<em class=\"italic\">$1</em>");
            encoded = encoded.Replace("\r\n", "\n").Replace("\r", "\n");
            encoded = encoded.Replace("\n", "<br />");

            return new HtmlString(encoded);
        }

        public static HtmlString RenderListItem(this string? item)
        {
            if (string.IsNullOrWhiteSpace(item))
                return new HtmlString(string.Empty);

            item = Normalize(item);

            var match = Regex.Match(item, @"^\*\*(.+?)\*\*:\s*(.+)$", RegexOptions.Singleline);
            if (match.Success)
            {
                var label = match.Groups[1].Value;
                var content = match.Groups[2].Value;
                var labelEncoded = WebUtility.HtmlEncode(label);
                var contentHtml = RenderMarkdown(content).Value;

                return new HtmlString(
                    $"<strong class=\"font-semibold text-gray-900\">{labelEncoded}:</strong> " +
                    $"<span class=\"text-gray-700\">{contentHtml}</span>"
                );
            }

            return RenderMarkdown(item);
        }

        public static HtmlString RenderAutoLinks(this string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new HtmlString(string.Empty);

            return RenderMarkdown(AutoLinkContent(Normalize(text)));
        }
    }
}
