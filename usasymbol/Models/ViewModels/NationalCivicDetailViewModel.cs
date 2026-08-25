using USASymbol.Models.Content;

namespace USASymbol.Models.ViewModels
{
    public sealed class NationalCivicDetailViewModel : SymbolDetailViewModel, ISymbolDetailViewModel, IHasVisualAssets
    {
        public PageDetailViewModel PageModel { get; set; } = null!;

        public string? ContentTitle => PageModel.Content?.Page?.H1;
        public string? ContentIntroText => PageModel.Content?.Page?.IntroParagraphs?.FirstOrDefault();
        public override string? Author => PageModel.Content?.Author;
        public override DateTime? DateModified => PageModel.Content?.DateModified;
        public int? AdoptedYear => Symbol?.AdoptedYear;

        public List<IContentSection>? Sections => PageModel.Content?.Sections?.Cast<IContentSection>().ToList();
        public List<ISource>? Sources => PageModel.Content?.Page?.Sources?.Cast<ISource>().ToList();
        public List<IFaqItem>? Faq => PageModel.Content?.Faq?.Cast<IFaqItem>().ToList();

        public bool HasSections => Sections?.Any() == true;
        public bool HasSources => Sources?.Any() == true;
        public bool HasFaq => Faq?.Any() == true;

        public override IReadOnlyList<QuickFactItem>? QuickFacts =>
            PageModel.Content?.Page?.QuickAnswer?
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(ToQuickFact)
                .ToList();

        public SymbolColorScheme Colors => SymbolColorScheme.Slate;
        public string SymbolTypeName => "National Symbol";
        public string SymbolTypeSlug => PageModel.Content?.Slug ?? string.Empty;
        public string SymbolTypePlural => "national-symbols";
        public string SymbolTypeIcon => "fa-solid fa-landmark";
        public IReadOnlyList<VisualAsset>? VisualAssets => PageModel.Content?.VisualAssets;

        private static QuickFactItem ToQuickFact(string value)
        {
            var separator = value.IndexOf(':');
            return separator > 0
                ? new QuickFactItem
                {
                    Label = value[..separator].Trim(),
                    Value = value[(separator + 1)..].Trim()
                }
                : new QuickFactItem { Label = "Key fact", Value = value.Trim() };
        }
    }
}
