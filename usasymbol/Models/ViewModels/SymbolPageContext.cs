namespace USASymbol.Models.ViewModels
{
    public sealed class SymbolTile
    {
        public string Abbr { get; init; } = "";
        public string Name { get; init; } = "";
        public string Slug { get; init; } = "";
        /// <summary>current | same | other | none</summary>
        public string Kind { get; init; } = "none";
        public string? SymbolName { get; init; }
        public string? Url { get; init; }
    }

    public sealed class SymbolPeer
    {
        public string StateName { get; init; } = "";
        public string StateSlug { get; init; } = "";
        public string Abbr { get; init; } = "";
        public string SymbolName { get; init; } = "";
        public string Url { get; init; } = "";
        public int? Year { get; init; }
    }

    public sealed class SymbolNeighbor
    {
        public string Label { get; init; } = "";
        public string Name { get; init; } = "";
        public string Url { get; init; } = "";
    }

    public sealed class SymbolPageContext
    {
        public string StateName { get; init; } = "";
        public string Noun { get; init; } = "symbol";
        public string HubUrl { get; init; } = "/symbols";
        public string HubLabel { get; init; } = "All symbols";

        public string? UniqueBadge { get; init; }
        public bool IsUnique { get; init; }
        public string? RankBadge { get; init; }
        public string? AgeBadge { get; init; }
        public string? OldestNewest { get; init; }

        public string MapCaption { get; init; } = "";
        public int StatesWithType { get; init; }
        public List<SymbolTile> Tiles { get; init; } = new();

        public string PeersHeading { get; init; } = "";
        public List<SymbolPeer> Peers { get; init; } = new();

        public SymbolNeighbor? Previous { get; init; }
        public SymbolNeighbor? Next { get; init; }
    }
}
