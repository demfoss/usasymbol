namespace USASymbol.Models.ViewModels
{
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
