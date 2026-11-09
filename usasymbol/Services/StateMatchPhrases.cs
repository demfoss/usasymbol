namespace USASymbol.Services
{
    /// <summary>
    /// Short human phrases per State Match metric. Keep identical to PHRASES in
    /// wwwroot/js/state-match.js so the server-rendered card text matches what the script shows.
    /// </summary>
    public static class StateMatchPhrases
    {
        private static readonly Dictionary<string, (string Good, string Bad)> Map = new()
        {
            ["cost"] = ("low everyday costs", "high everyday costs"),
            ["income"] = ("high incomes", "lower incomes"),
            ["jobs"] = ("a strong job market", "a tighter job market"),
            ["housing"] = ("affordable homes", "pricey homes"),
            ["safety"] = ("low crime", "higher crime"),
            ["health"] = ("good healthcare", "weaker healthcare"),
            ["education"] = ("strong schools", "weaker schools"),
            ["warmth"] = ("a warm climate", "cold winters"),
            ["incometax"] = ("low income tax", "high income tax"),
            ["propertytax"] = ("low property tax", "high property tax"),
            ["salestax"] = ("low sales tax", "high sales tax"),
            ["jobgrowth"] = ("fast job growth", "slow job growth"),
            ["childcare"] = ("cheaper childcare", "costly childcare"),
            ["commute"] = ("short commutes", "long commutes"),
            ["rent"] = ("low rent", "high rent"),
            ["lifeexpectancy"] = ("long life expectancy", "shorter life expectancy"),
            ["sunny"] = ("lots of sunshine", "fewer sunny days")
        };

        public static string Sentence(IReadOnlyList<string> strongKeys, string? weakKey)
        {
            var strong = strongKeys.Where(Map.ContainsKey).Select(key => Map[key].Good).ToList();
            var weak = weakKey != null && Map.TryGetValue(weakKey, out var phrase) ? phrase.Bad : null;
            if (strong.Count == 0)
            {
                return weak != null
                    ? $"A balanced match across your priorities, though {weak}."
                    : "A balanced match across your priorities.";
            }

            var text = string.Join(" and ", strong);
            text = char.ToUpperInvariant(text[0]) + text[1..];
            return weak != null ? $"{text}, but {weak}." : $"{text}.";
        }
    }
}
