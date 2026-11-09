namespace USASymbol.Models.ViewModels
{
    public record InlineMoreItem(string Title, string Url, string ImageUrl);

    /// <summary>
    /// A small "keep reading" block placed inside the article body (2 linked cards with photos).
    /// Items without a real image must never be added: the format only works with pictures.
    /// </summary>
    public class InlineMoreViewModel
    {
        public string Label { get; set; } = "Keep Exploring";
        public IReadOnlyList<InlineMoreItem> Items { get; set; } = new List<InlineMoreItem>();

        /// <summary>Index of the content section after which the block is rendered, or -1 if the page is too short.</summary>
        public static int AfterIndexFor(int sectionCount) => sectionCount >= 5 ? 2 : sectionCount >= 3 ? 1 : -1;

        /// <summary>Second block for long pages (8+ sections), or -1.</summary>
        public static int SecondAfterIndexFor(int sectionCount) => sectionCount >= 8 ? 5 : -1;
    }
}
