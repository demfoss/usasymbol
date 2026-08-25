using USASymbol.Models.ViewModels;

namespace USASymbol.Models.Content
{
    public class SongContent
    {
        public string Type { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string StateFips { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int? AdoptedYear { get; set; }
        public bool IsOfficial { get; set; }
        public string Legislation { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public DateTime? DatePublished { get; set; }
        public DateTime? DateModified { get; set; }
        public DateTime LastModified { get; set; }
        public string SeoTitle { get; set; } = string.Empty;
        public string SeoDescription { get; set; } = string.Empty;
        public string HeroImage { get; set; } = string.Empty;
        public string HeroImageAlt { get; set; } = string.Empty;
        public string HeroImageCaption { get; set; } = string.Empty;
        public string IntroText { get; set; } = string.Empty;

        public string Composer { get; set; } = string.Empty;
        public string Lyricist { get; set; } = string.Empty;
        public int? PublicationYear { get; set; }

        public string YoutubeUrl { get; set; } = string.Empty;
        public string YoutubeTitle { get; set; } = string.Empty;
        public string YoutubeCaption { get; set; } = string.Empty;
        public string SpotifyUrl { get; set; } = string.Empty;
        public string SpotifyCaption { get; set; } = string.Empty;
        public string AudioUrl { get; set; } = string.Empty;

        public bool LyricsIsPublicDomain { get; set; }
        public bool LyricsIsUserProvided { get; set; }
        public List<string> LyricsVerses { get; set; } = new();
        public string LyricsNote { get; set; } = string.Empty;
        public string LyricsSourceUrl { get; set; } = string.Empty;
        public string LyricsSourceName { get; set; } = string.Empty;

        public List<SecondarySong> SecondarySongs { get; set; } = new();

        public List<SongSection> Sections { get; set; } = new();
        public List<SongFaq> Faq { get; set; } = new();
        public List<SongSource> Sources { get; set; } = new();
        public List<QuickFactItem> QuickFacts { get; set; } = new();
        public List<VisualAsset> VisualAssets { get; set; } = new();
    }

    public class SecondarySong
    {
        public string Name { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public string Composer { get; set; } = string.Empty;
        public string Lyricist { get; set; } = string.Empty;
        public string Year { get; set; } = string.Empty;
    }

    public class SongSection : IContentSection
    {
        public string Id { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Style { get; set; } = string.Empty;
        public string? Img { get; set; }
        public List<string> Paragraphs { get; set; } = new();
        public List<string> Facts { get; set; } = new();
        public List<IContentSubsection>? Subsections { get; set; }
        public List<string>? ListItems { get; set; }
    }

    public class SongFaq : IFaqItem
    {
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
    }

    public class SongSource : ISource
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
