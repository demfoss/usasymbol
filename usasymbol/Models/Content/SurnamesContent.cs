namespace USASymbol.Models.Content
{
    public class SurnamesContent
    {
        public string State { get; set; } = "";
        public string StateSlug { get; set; } = "";
        public long Population { get; set; }
        public int DataYear { get; set; }
        public string? SeoTitle { get; set; }
        public string? SeoDescription { get; set; }
        public string? H1 { get; set; }
        public string? Intro { get; set; }
        public string? HeritageTitle { get; set; }
        public string? HeritageBody { get; set; }
        public string? FunFact { get; set; }
        public List<SurnameEntry> Surnames { get; set; } = new();
        public List<UniqueSurname> UniqueSurnames { get; set; } = new();
        public List<EtymologyGroup> EtymologyGroups { get; set; } = new();
        public List<SurnamesFaq> Faq { get; set; } = new();
        public List<SurnamesSource> Sources { get; set; } = new();
    }

    public class SurnameEntry
    {
        public int Rank { get; set; }
        public string Name { get; set; } = "";
        public long Count { get; set; }
        public int Ratio { get; set; }
        public string Origin { get; set; } = "english";
        public string? Type { get; set; }
        public string? Meaning { get; set; }
    }

    public class UniqueSurname
    {
        public string Name { get; set; } = "";
        public int StateRank { get; set; }
        public int NationalRank { get; set; }
        public string Origin { get; set; } = "english";
        public string? Why { get; set; }
    }

    public class EtymologyGroup
    {
        public string Type { get; set; } = "";
        public string Icon { get; set; } = "fa-regular fa-bookmark";
        public string Description { get; set; } = "";
        public List<string> Examples { get; set; } = new();
    }

    public class SurnamesFaq
    {
        public string Question { get; set; } = "";
        public string Answer { get; set; } = "";
    }

    public class SurnamesSource
    {
        public string Name { get; set; } = "";
        public string? Url { get; set; }
        public string? Description { get; set; }
    }
}
