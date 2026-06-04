using System;
using System.Collections.Generic;
using System.Linq;
using USASymbol.Models.Content;

namespace USASymbol.Models.ViewModels
{
    public class NicknameDetailViewModel : SymbolDetailViewModel, ISymbolDetailViewModel, IHasVisualAssets
    {
        public NicknameContent NicknameContent { get; set; } = null!;


        public string? ContentTitle => FirstNonEmpty(NicknameContent?.Title, Symbol?.Name);
        public string? ContentIntroText => NicknameContent?.IntroText;
        public override string? Author => NicknameContent?.Author;
        public override DateTime? DateModified => NicknameContent?.DateModified;
        public int? AdoptedYear => NicknameContent?.AdoptedYear ?? Symbol?.AdoptedYear;



        public override string? WikidataId => NicknameContent?.WikidataId ?? Symbol?.WikidataId;
        public override string? Legislation => NicknameContent?.Legislation ?? Symbol?.Legislation;
        public override string? Meaning => NicknameContent?.Meaning ?? Symbol?.Meaning;

        public List<IContentSection>? Sections => NicknameContent?.Sections?.Cast<IContentSection>().ToList();
        public List<ISource>? Sources => NicknameContent?.Sources?.Cast<ISource>().ToList();
        public List<IFaqItem>? Faq => NicknameContent?.Faq?.Cast<IFaqItem>().ToList();

        bool ISymbolDetailViewModel.HasSections => Sections?.Any() == true;
        bool ISymbolDetailViewModel.HasSources => Sources?.Any() == true;
        bool ISymbolDetailViewModel.HasFaq => Faq?.Any() == true;

        public SymbolColorScheme Colors => SymbolColorScheme.Slate;

        public string SymbolTypeName => "State Nickname";
        public string SymbolTypeSlug => "nickname";
        public string SymbolTypePlural => "nicknames";
        public string SymbolTypeIcon => "???";


        public bool HasSources => NicknameContent?.Sources?.Any() == true;
        public bool HasFaq => NicknameContent?.Faq?.Any() == true;
        public bool HasRelatedSymbols => RelatedSymbols.Count > 0;
        public bool HasOtherNicknames => NicknameContent?.OtherNicknames?.Any() == true;
        public IReadOnlyList<VisualAsset>? VisualAssets => NicknameContent?.VisualAssets;
        public override IReadOnlyList<QuickFactItem>? QuickFacts => NicknameContent?.QuickFacts;
    }
}
