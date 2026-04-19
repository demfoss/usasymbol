using USASymbol.Models.Content;
using System.Collections.Generic;
using System.Linq;
using System;

namespace USASymbol.Models.ViewModels
{
    public class MottoDetailViewModel : SymbolDetailViewModel, ISymbolDetailViewModel, IHasVisualAssets
    {
        public MottoContent MottoContent { get; set; } = null!;


        public string? ContentTitle => MottoContent?.Title ?? Symbol?.Name;
        public string? ContentIntroText => MottoContent?.IntroText;
        public override string? Author => MottoContent?.Author;
        public override DateTime? DateModified => MottoContent?.DateModified;
        public int? AdoptedYear => MottoContent?.AdoptedYear ?? Symbol?.AdoptedYear;


        public override string? WikidataId => MottoContent?.WikidataId ?? Symbol?.WikidataId;
        public override string? Legislation => MottoContent?.Legislation ?? Symbol?.Legislation;


        public override string? Meaning => !string.IsNullOrEmpty(MottoContent?.Meaning)
                                    ? MottoContent.Meaning
                                    : (!string.IsNullOrEmpty(MottoContent?.EnglishTranslation)
                                        ? MottoContent.EnglishTranslation
                                        : Symbol?.Meaning);

        public List<IContentSection>? Sections => MottoContent?.Sections?.Cast<IContentSection>().ToList();
        public List<ISource>? Sources => MottoContent?.Sources?.Cast<ISource>().ToList();
        public List<IFaqItem>? Faq => MottoContent?.Faq?.Cast<IFaqItem>().ToList();

        bool ISymbolDetailViewModel.HasSections => Sections?.Any() == true;
        bool ISymbolDetailViewModel.HasSources => Sources?.Any() == true;
        bool ISymbolDetailViewModel.HasFaq => Faq?.Any() == true;

        public SymbolColorScheme Colors => SymbolColorScheme.Amber;

        public string SymbolTypeName => "State Motto";
        public string SymbolTypeSlug => "motto";
        public string SymbolTypePlural => "mottos";
        public string SymbolTypeIcon => "??";


        public bool HasSources => MottoContent?.Sources?.Any() == true;
        public bool HasFaq => MottoContent?.Faq?.Any() == true;
        public bool HasRelatedSymbols => RelatedSymbols.Count > 0;
        public IReadOnlyList<VisualAsset>? VisualAssets => MottoContent?.VisualAssets;
        public override IReadOnlyList<QuickFactItem>? QuickFacts => MottoContent?.QuickFacts;
    }
}
