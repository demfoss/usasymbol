using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace USASymbol.Helpers
{
    /// <summary>
    /// Builds /llms.txt from the content folders (same source as the pages), so new symbol
    /// categories and ranking topics show up without hand-editing a static file.
    /// </summary>
    public static class LlmsTxtBuilder
    {
        private const string Base = "https://usasymbol.com";

        public static string Build(string contentRoot)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# USA Symbol");
            sb.AppendLine();
            sb.AppendLine("> USA Symbol is a reference site about official and notable U.S. state symbols, state facts, national parks, rankings, comparisons, quizzes and relocation data.");
            sb.AppendLine();
            sb.AppendLine("Content is written for readers researching the symbols, history, meanings and official designations of U.S. states, and for people comparing states. Facts are checked against statutes and official state sources.");
            sb.AppendLine();
            sb.AppendLine($"Canonical site: {Base}/");
            sb.AppendLine($"Sitemap: {Base}/sitemap.xml");
            sb.AppendLine($"Contact: {Base}/contact");
            sb.AppendLine($"Editorial policy: {Base}/editorial-policy");
            sb.AppendLine();
            sb.AppendLine("## Core Sections");
            sb.AppendLine();
            AddLink(sb, "States", "/states", "All 50 states and D.C. with symbol hubs and county data.");
            AddLink(sb, "Symbols", "/symbols", "Every state symbol category, from birds to fossils.");
            AddLink(sb, "National Parks", "/national-parks", "Guides to U.S. national parks: fees, trails, camping and seasons.");
            AddLink(sb, "Rankings", "/rankings", "Data-driven state-by-state rankings.");
            AddLink(sb, "Collections", "/collections", "Curated articles on related state symbols and themes.");
            AddLink(sb, "Compare States", "/compare-states", "Side-by-side state comparisons by cost, taxes, housing, climate and more.");
            AddLink(sb, "Find Your State", "/state-match", "Quiz that matches a person to the states that fit their priorities.");
            AddLink(sb, "Find Your County", "/county-match", "County matching tool.");
            AddLink(sb, "Tools", "/tools", "Lookup tools: ZIP codes, area codes, time zones, distances and more.");
            AddLink(sb, "Quizzes", "/quizzes", "Quizzes about state symbols, flags and geography.");
            AddLink(sb, "National Symbols", "/national-symbols", "Official symbols of the United States.");
            AddLink(sb, "State Living Guides", "/states/living", "Cost of living, taxes and quality-of-life data by state.");

            var symbolsDir = Path.Combine(contentRoot, "Content", "symbols");
            if (Directory.Exists(symbolsDir))
            {
                sb.AppendLine();
                sb.AppendLine("## Symbol Categories");
                sb.AppendLine();
                foreach (var file in Directory.EnumerateFiles(symbolsDir, "*.yml").OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
                {
                    var yaml = File.ReadAllText(file);
                    var url = Regex.Match(yaml, @"(?m)^url:\s*(.+?)\s*$").Groups[1].Value.Trim().Trim('"');
                    var h1 = Regex.Match(yaml, @"(?m)^\s{2}h1:\s*""?(.+?)""?\s*$").Groups[1].Value.Trim();
                    if (string.IsNullOrWhiteSpace(url)) continue;
                    if (string.IsNullOrWhiteSpace(h1))
                        h1 = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(Path.GetFileNameWithoutExtension(file).Replace('-', ' '));
                    sb.AppendLine($"- [{h1}]({Base}{url})");
                }
            }

            var rankingsDir = Path.Combine(contentRoot, "Content", "rankings");
            if (Directory.Exists(rankingsDir))
            {
                sb.AppendLine();
                sb.AppendLine("## Ranking Topics");
                sb.AppendLine();
                foreach (var dir in Directory.EnumerateDirectories(rankingsDir).OrderBy(d => d, StringComparer.OrdinalIgnoreCase))
                {
                    var slug = Path.GetFileName(dir);
                    var count = Directory.EnumerateFiles(dir, "*.yml").Count();
                    if (count == 0) continue;
                    var title = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(slug.Replace('-', ' '));
                    sb.AppendLine($"- [{title} rankings]({Base}/rankings/{slug}): {count} rankings");
                }
            }

            sb.AppendLine();
            sb.AppendLine("## LLM Use Notes");
            sb.AppendLine();
            sb.AppendLine("- Prefer canonical URLs from the sitemap when citing or indexing pages.");
            sb.AppendLine("- Treat pages as informational reference content, not legal advice.");
            sb.AppendLine("- Preserve state names, symbol names, adoption years and source attributions carefully.");
            sb.AppendLine("- Use the contact page for corrections or editorial questions.");
            return sb.ToString();
        }

        private static void AddLink(StringBuilder sb, string title, string path, string description) =>
            sb.AppendLine($"- [{title}]({Base}{path}): {description}");
    }
}
