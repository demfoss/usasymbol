using USASymbol.Models.Content;

namespace USASymbol.Models.ViewModels
{
    public class SongDetailViewModel : SymbolDetailViewModel, ISymbolDetailViewModel, IHasVisualAssets
    {
        public SongContent? SongContent { get; set; }

        public string? ContentTitle => FirstNonEmpty(SongContent?.Name, Symbol?.Name);
        public string? ContentIntroText => SongContent?.IntroText;
        public override string? Author => SongContent?.Author;
        public override DateTime? DateModified => SongContent?.DateModified;
        public int? AdoptedYear => SongContent?.AdoptedYear ?? Symbol?.AdoptedYear;

        public override string? Legislation => SongContent?.Legislation ?? Symbol?.Legislation;

        public List<IContentSection>? Sections => SongContent?.Sections?.Cast<IContentSection>().ToList();
        public List<ISource>? Sources => SongContent?.Sources?.Cast<ISource>().ToList();
        public List<IFaqItem>? Faq => SongContent?.Faq?.Cast<IFaqItem>().ToList();

        public bool HasSections => Sections?.Any() == true;
        public bool HasSources => Sources?.Any() == true;
        public bool HasFaq => Faq?.Any() == true;

        public string? YoutubeUrl => SongContent?.YoutubeUrl;
        public string? YoutubeTitle => SongContent?.YoutubeTitle;
        public string? YoutubeCaption => SongContent?.YoutubeCaption;
        public string? SpotifyUrl => SongContent?.SpotifyUrl;
        public string? SpotifyCaption => SongContent?.SpotifyCaption;
        public string? AudioUrl => SongContent?.AudioUrl;

        public SymbolColorScheme Colors => SymbolColorScheme.Indigo;

        public string SymbolTypeName { get; set; } = "State Song";
        public string SymbolTypeSlug { get; set; } = "song";
        public string SymbolTypePlural { get; set; } = "songs";
        public string SymbolTypeIcon { get; set; } = "🎵";
        public string DefaultDesignation { get; set; } = "State Song";
        public string HeroFallbackIconClass { get; set; } = "fa-solid fa-music";
        public string OverviewIconClass { get; set; } = "fa-solid fa-music";
        public string AssetBasePath { get; set; } = "/images/songs";
        public string EmptySectionsMessage { get; set; } = "No sections rendered yet for this state song.";

        public override IReadOnlyList<QuickFactItem>? QuickFacts
        {
            get => SongContent?.QuickFacts;
            set { }
        }

        public IReadOnlyList<VisualAsset>? VisualAssets => SongContent?.VisualAssets;
    }
}
