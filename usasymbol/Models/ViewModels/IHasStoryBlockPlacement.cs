namespace USASymbol.Models.ViewModels
{
    public interface IHasStoryBlockPlacement
    {
        string? BigStatAfterSectionId { get; }
        string? TimelineAfterSectionId { get; }
        string? ExpertQuoteAfterSectionId { get; }
    }
}
