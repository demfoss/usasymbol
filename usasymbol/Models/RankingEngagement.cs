namespace USASymbol.Models;

/// <summary>
/// Anonymous, per-ranking engagement state. VisitorHash is derived from a random first-party
/// cookie; no IP address, user id, or raw cookie value is stored.
/// </summary>
public sealed class RankingEngagement
{
    public long Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string RankingSlug { get; set; } = string.Empty;
    public string VisitorHash { get; set; } = string.Empty;
    public int ViewCount { get; set; }
    public DateTime FirstViewedAt { get; set; }
    public DateTime LastViewedAt { get; set; }
    public string? Reaction { get; set; }
    public DateTime? FirstReactedAt { get; set; }
    public DateTime? LastReactedAt { get; set; }
    public int ReactionChangeCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class RankingReactionRequest
{
    public string? Reaction { get; set; }
}
