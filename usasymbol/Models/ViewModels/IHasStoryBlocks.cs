namespace USASymbol.Models.ViewModels
{
    public interface IHasStoryBlocks
    {
        BigStatViewModel? BigStat { get; }
        IReadOnlyList<TimelineEventViewModel>? Timeline { get; }
        ExpertQuoteViewModel? ExpertQuote { get; }
    }
    public sealed class BigStatViewModel
    {
        public string Number { get; init; } = "";
        public string Description { get; init; } = "";
    }

    public sealed class TimelineEventViewModel
    {
        public string Year { get; init; } = "";
        public string Description { get; init; } = "";
    }

    public sealed class ExpertQuoteViewModel
    {
        public string Text { get; init; } = "";
        public string Source { get; init; } = "";
    }

}
