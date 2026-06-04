namespace USASymbol.Models.ViewModels
{
    public class StateMapPageViewModel
    {
        public State State { get; set; } = new();
        // JSON: { "<fips>": { name, capital, population, region, statehood, slug } }
        public string AllStatesJson { get; set; } = "{}";
        public string StateSlugsJson { get; set; } = "{}";
        public List<StateCountyMapItem> Counties { get; set; } = new();
        public StateCountyMapSummary? CountySummary { get; set; }
    }

    public class StateCountyMapItem
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string StateCode { get; set; } = string.Empty;
        public string FipsCode { get; set; } = string.Empty;
        public long? Population { get; set; }
    }

    public class StateCountyMapSummary
    {
        public int CountyCount { get; set; }
        public StateCountyMapItem? LargestByPopulation { get; set; }
        public StateCountyMapItem? SmallestByPopulation { get; set; }
    }

    public class StateViewModel
    {
        public State State { get; set; } = new();
        public List<Symbol> Symbols { get; set; } = new();
        public List<State> RelatedStates { get; set; } = new();
        public StateHubContent? HubContent { get; set; }
    }

    public class StateHubContent
    {
        public string? SeoDescription { get; set; }
        public string? SnippetAnswer { get; set; }
        public List<StateHubFact> MiniFacts { get; set; } = new();
        public List<string> MostRecognized { get; set; } = new();
        public StateHubEditorial? NameMeaning { get; set; }
        public StateHubEditorial? Editorial { get; set; }
        public List<StateHubFaq> Faq { get; set; } = new();
        public List<StateHubSource> Sources { get; set; } = new();
    }

    public class StateHubFact
    {
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string? Url { get; set; }
    }

    public class StateHubEditorial
    {
        public string Heading { get; set; } = string.Empty;
        public List<string> Paragraphs { get; set; } = new();
        public List<StateHubInfoGroup> InfoGroups { get; set; } = new();
    }

    public class StateHubInfoGroup
    {
        public string Heading { get; set; } = string.Empty;
        public List<StateHubInfoItem> Items { get; set; } = new();
    }

    public class StateHubInfoItem
    {
        public string Label { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }

    public class StateHubFaq
    {
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
    }

    public class StateHubSource
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }
}
