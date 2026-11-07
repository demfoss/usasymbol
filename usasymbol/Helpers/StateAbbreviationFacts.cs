namespace USASymbol.Helpers
{
    /// <summary>
    /// Per-state facts for the abbreviation guide pages that are not in the YAML content:
    /// a ZIP Code for the address example, the GPO form, codes people really mix up,
    /// and a short note on why the code looks the way it does.
    /// </summary>
    public static class StateAbbreviationFacts
    {
        // Real ZIP Codes in or next to each state capital (used only as an address example).
        private static readonly Dictionary<string, string> Zips = new(StringComparer.OrdinalIgnoreCase)
        {
            ["AL"] = "36104", ["AK"] = "99801", ["AZ"] = "85007", ["AR"] = "72201", ["CA"] = "95814",
            ["CO"] = "80203", ["CT"] = "06103", ["DE"] = "19901", ["FL"] = "32399", ["GA"] = "30334",
            ["HI"] = "96813", ["ID"] = "83702", ["IL"] = "62701", ["IN"] = "46204", ["IA"] = "50319",
            ["KS"] = "66612", ["KY"] = "40601", ["LA"] = "70802", ["ME"] = "04330", ["MD"] = "21401",
            ["MA"] = "02133", ["MI"] = "48933", ["MN"] = "55155", ["MS"] = "39201", ["MO"] = "65101",
            ["MT"] = "59601", ["NE"] = "68509", ["NV"] = "89701", ["NH"] = "03301", ["NJ"] = "08625",
            ["NM"] = "87501", ["NY"] = "12224", ["NC"] = "27601", ["ND"] = "58505", ["OH"] = "43215",
            ["OK"] = "73105", ["OR"] = "97301", ["PA"] = "17120", ["RI"] = "02903", ["SC"] = "29201",
            ["SD"] = "57501", ["TN"] = "37243", ["TX"] = "78701", ["UT"] = "84114", ["VT"] = "05633",
            ["VA"] = "23219", ["WA"] = "98504", ["WV"] = "25305", ["WI"] = "53702", ["WY"] = "82002",
        };

        // GPO Style Manual forms (note the space in several of them).
        private static readonly Dictionary<string, string> Gpo = new(StringComparer.OrdinalIgnoreCase)
        {
            ["AL"] = "Ala.", ["AZ"] = "Ariz.", ["AR"] = "Ark.", ["CA"] = "Calif.", ["CO"] = "Colo.",
            ["CT"] = "Conn.", ["DE"] = "Del.", ["FL"] = "Fla.", ["GA"] = "Ga.", ["IL"] = "Ill.",
            ["IN"] = "Ind.", ["KS"] = "Kans.", ["KY"] = "Ky.", ["LA"] = "La.", ["MD"] = "Md.",
            ["MA"] = "Mass.", ["MI"] = "Mich.", ["MN"] = "Minn.", ["MS"] = "Miss.", ["MO"] = "Mo.",
            ["MT"] = "Mont.", ["NE"] = "Nebr.", ["NV"] = "Nev.", ["NH"] = "N.H.", ["NJ"] = "N.J.",
            ["NM"] = "N. Mex.", ["NY"] = "N.Y.", ["NC"] = "N.C.", ["ND"] = "N. Dak.", ["OK"] = "Okla.",
            ["OR"] = "Oreg.", ["PA"] = "Pa.", ["RI"] = "R.I.", ["SC"] = "S.C.", ["SD"] = "S. Dak.",
            ["TN"] = "Tenn.", ["TX"] = "Tex.", ["VT"] = "Vt.", ["VA"] = "Va.", ["WA"] = "Wash.",
            ["WV"] = "W. Va.", ["WI"] = "Wis.", ["WY"] = "Wyo.",
        };

        // Codes that people actually swap for this one on forms and lists.
        private static readonly Dictionary<string, string[]> Confusable = new(StringComparer.OrdinalIgnoreCase)
        {
            ["AL"] = new[] { "AK", "AR", "AZ" }, ["AK"] = new[] { "AL", "AR", "AZ" },
            ["AZ"] = new[] { "AL", "AK", "AR" }, ["AR"] = new[] { "AL", "AK", "AZ" },
            ["CA"] = new[] { "CO", "CT" }, ["CO"] = new[] { "CA", "CT" }, ["CT"] = new[] { "CA", "CO" },
            ["ID"] = new[] { "IA", "IL", "IN" }, ["IA"] = new[] { "ID", "IL", "IN" },
            ["IL"] = new[] { "ID", "IA", "IN" }, ["IN"] = new[] { "ID", "IA", "IL" },
            ["KS"] = new[] { "KY" }, ["KY"] = new[] { "KS" },
            ["MA"] = new[] { "MD", "ME", "MI" }, ["MD"] = new[] { "MA", "ME", "MI" },
            ["ME"] = new[] { "MA", "MD", "MI" },
            ["MI"] = new[] { "MA", "MN", "MO", "MS" }, ["MN"] = new[] { "MI", "MO", "MS", "MT" },
            ["MO"] = new[] { "MI", "MN", "MS", "MT" }, ["MS"] = new[] { "MI", "MN", "MO", "MT" },
            ["MT"] = new[] { "MI", "MN", "MO", "MS" },
            ["NE"] = new[] { "NV", "NH", "NJ" }, ["NV"] = new[] { "NE", "NH", "NJ", "NM", "NY" },
            ["NH"] = new[] { "NE", "NV", "NJ", "NM", "NY" }, ["NJ"] = new[] { "NE", "NV", "NH", "NM", "NY" },
            ["NM"] = new[] { "NV", "NH", "NJ", "NY" }, ["NY"] = new[] { "NV", "NH", "NJ", "NM" },
            ["NC"] = new[] { "SC", "ND" }, ["ND"] = new[] { "NC", "SD" },
            ["SC"] = new[] { "NC", "SD" }, ["SD"] = new[] { "SC", "ND" },
            ["OH"] = new[] { "OK", "OR" }, ["OK"] = new[] { "OH", "OR" }, ["OR"] = new[] { "OH", "OK" },
            ["TN"] = new[] { "TX" }, ["TX"] = new[] { "TN" },
            ["UT"] = new[] { "VT" }, ["VT"] = new[] { "UT", "VA" },
            ["VA"] = new[] { "WV", "WA", "VT" },
            ["WA"] = new[] { "VA", "WV", "WI", "WY" }, ["WV"] = new[] { "VA", "WA", "WI", "WY" },
            ["WI"] = new[] { "WA", "WV", "WY" }, ["WY"] = new[] { "WA", "WV", "WI" },
        };

        // Short, checkable notes on the code itself. Only states with something real to say.
        private static readonly Dictionary<string, string> Why = new(StringComparer.OrdinalIgnoreCase)
        {
            ["AK"] = "AL already belongs to Alabama, so Alaska takes AK.",
            ["AZ"] = "AZ is the only A-state code that ends in Z.",
            ["CT"] = "CA and CO were already taken by California and Colorado, so Connecticut uses its first and last letters, C and T.",
            ["IN"] = "IN is also a common English word, so it is easy to misread inside a sentence. In an address it always follows the city and a comma.",
            ["LA"] = "In an address, LA means Louisiana, not Los Angeles. Los Angeles belongs to California, so it is written Los Angeles, CA.",
            ["MA"] = "Eight states begin with M, and three of them share the first two letters: Maine, Maryland, and Massachusetts. Their codes (ME, MD, MA) differ only in the second letter.",
            ["MD"] = "MA belongs to Massachusetts, so Maryland uses its first and last letters, M and D.",
            ["ME"] = "ME is also an English word. In an address it always follows the city and a comma, so it reads as Maine.",
            ["MI"] = "Eight states begin with M, which makes MI one of the easiest codes to swap for MN, MO, MS, or MT.",
            ["MN"] = "Eight states begin with M. MN is the one to check against MI, MO, MS, and MT.",
            ["MO"] = "Missouri's MO is easy to confuse with MI, MN, MS, and MT. Check the second letter.",
            ["MS"] = "MS is Mississippi. Michigan (MI), Minnesota (MN), Missouri (MO), and Montana (MT) all start with the same letter.",
            ["MT"] = "MT is Montana. Four other M-states, MI, MN, MO, and MS, are one letter away.",
            ["NE"] = "Nebraska used NB until 1969. The USPS switched it to NE so it would not be mistaken for NB, the code for New Brunswick, Canada.",
            ["OH"] = "OH is also an English word. In an address it follows the city and a comma, so it reads as Ohio.",
            ["OK"] = "OK is also an English word. In an address it follows the city and a comma, so it reads as Oklahoma.",
            ["OR"] = "OR is also an English word. In an address it follows the city and a comma, so it reads as Oregon.",
            ["PA"] = "PA uses the first and last letters of Pennsylvania.",
            ["VA"] = "West Virginia split from Virginia in 1863, but the two states have different codes: VA and WV.",
            ["WA"] = "WA is Washington state. Washington, D.C. is a separate code, DC.",
            ["AL"] = "AL uses the first two letters of Alabama. Alaska (AK) and Arizona (AZ) do not follow that pattern, which is why the four A-state codes are easy to mix up.",
            ["AR"] = "AR is Arkansas, not Arizona. Arizona's code is AZ.",
            ["CA"] = "CA uses the first two letters of California. Colorado and Connecticut also start with C, but their codes are CO and CT.",
            ["CO"] = "CO uses the first two letters of Colorado. California (CA) and Connecticut (CT) are the other two C-states.",
            ["DE"] = "DE uses the first two letters of Delaware. DC, the code for Washington, D.C., sits right next to it in alphabetical code lists.",
            ["FL"] = "FL uses the first two letters of Florida. No other state name starts with Fl, so the code is never confused with a neighbor.",
            ["GA"] = "Georgia is the only state whose name starts with G, so GA is the only G code. Guam, a territory, uses GU.",
            ["HI"] = "Hawaii is the only state whose name starts with H, so HI is the only H code.",
            ["ID"] = "Four states start with I: Idaho, Illinois, Indiana, and Iowa. Their codes (ID, IL, IN, IA) differ only in the second letter.",
            ["IL"] = "IL is Illinois. In some sans-serif fonts a capital I and a lowercase l look almost the same, so type the code in capitals to keep it readable.",
            ["IA"] = "Iowa's code is IA, not IO. Keep it apart from the other I-states: ID, IL, and IN.",
            ["KS"] = "KS uses the first and last letters of Kansas. Kentucky is the only other K-state, with KY.",
            ["KY"] = "KY uses the first and last letters of Kentucky. Kansas is the only other K-state, with KS.",
            ["NV"] = "Nevada's code is NV, not NE. NE belongs to Nebraska.",
            ["NH"] = "NH is the initials of New Hampshire. Four states begin with New, and NH, NJ, NM, and NY all use initials.",
            ["NJ"] = "NJ is the initials of New Jersey. NH, NM, and NY follow the same pattern, so only the second letter tells them apart.",
            ["NM"] = "NM is the initials of New Mexico. The country Mexico has its own code, MX.",
            ["NY"] = "NY is the initials of New York. The same code is used for New York City addresses, written New York, NY.",
            ["NC"] = "NC is the initials of North Carolina. South Carolina is SC, and North Dakota is ND.",
            ["ND"] = "ND is the initials of North Dakota. South Dakota is SD, and North Carolina is NC.",
            ["RI"] = "RI is the initials of Rhode Island, the smallest state by area.",
            ["SC"] = "SC is the initials of South Carolina. Swap the S for an N and you get North Carolina's NC.",
            ["SD"] = "SD is the initials of South Dakota. Swap the S for an N and you get North Dakota's ND.",
            ["TN"] = "Tennessee and Texas are the only two states that start with T, and their codes are TN and TX.",
            ["TX"] = "TX is the only state code that contains an X.",
            ["UT"] = "Utah is the only state whose name starts with U, so UT is the only U code.",
            ["VT"] = "VT uses the first and last letters of Vermont. Virginia is VA, and Utah is UT.",
            ["WI"] = "Four states start with W: Washington, West Virginia, Wisconsin, and Wyoming. Their codes are WA, WV, WI, and WY.",
            ["WY"] = "WY is the last state code in an alphabetical list of postal codes, after WA, WI, and WV.",
            ["WV"] = "West Virginia separated from Virginia in 1863. By the time the USPS assigned two-letter codes, VA belonged to Virginia and WA to Washington, so West Virginia uses W and V.",
        };

        // Extra wrong spellings people type for specific states.
        private static readonly Dictionary<string, string[]> ExtraWrong = new(StringComparer.OrdinalIgnoreCase)
        {
            ["WV"] = new[] { "WVA", "WVa", "W.VA." },
            ["NE"] = new[] { "NB" },
        };

        public static string? Zip(string code) => Zips.GetValueOrDefault(code);

        public static string? GpoForm(string code) => Gpo.GetValueOrDefault(code);

        public static IReadOnlyList<string> ConfusableCodes(string code) =>
            Confusable.TryGetValue(code, out var list) ? list : Array.Empty<string>();

        public static string? WhyNote(string code) => Why.GetValueOrDefault(code);

        /// <summary>Spellings that are wrong for a postal address: dotted code, trailing period, lowercase, plus known extras.</summary>
        public static IReadOnlyList<string> WrongForms(string code)
        {
            var upper = code.ToUpperInvariant();
            var list = new List<string>
            {
                $"{upper[0]}.{upper[1]}.",
                upper + ".",
                upper.ToLowerInvariant()
            };
            if (ExtraWrong.TryGetValue(upper, out var extra))
                list.AddRange(extra);
            return list;
        }
    }
}
