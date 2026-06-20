using System;
using System.Collections.Generic;
using USASymbol.Models.ViewModels;

namespace USASymbol.Models.Content
{
    public class ColorContent
    {

        public string Title { get; set; } = "";
        public int? AdoptedYear { get; set; }


        public string WikidataId { get; set; } = "";
        public string Legislation { get; set; } = "";
        public string Meaning { get; set; } = "";


        public string OfficialColors { get; set; } = "";
        public string OfficialSince { get; set; } = "";
        public string PrimaryUse { get; set; } = "";
        public string KnownFor { get; set; } = "";

        public List<ColorSpecification> ColorData { get; set; } = new();


        public string Author { get; set; } = "";
        public DateTime? DatePublished { get; set; }
        public DateTime? DateModified { get; set; }
        public DateTime? LastModified { get; set; }
        public string SeoTitle { get; set; } = "";
        public string SeoDescription { get; set; } = "";
        public string IntroText { get; set; } = "";
        public string HtmlContent { get; set; } = "";


        public List<ColorSection> Sections { get; set; } = new();
        public List<ColorSource> Sources { get; set; } = new();
        public List<ColorFaq> Faq { get; set; } = new();


        public List<string> QuickSummary { get; set; } = new();
        public string DidYouKnow { get; set; } = "";
        public BigStatData? BigStat { get; set; }
        public List<TimelineEvent> Timeline { get; set; } = new();
        public ExpertQuoteData? ExpertQuote { get; set; }
        public string BigStatAfterSectionId { get; set; } = string.Empty;
        public string TimelineAfterSectionId { get; set; } = string.Empty;
        public string ExpertQuoteAfterSectionId { get; set; } = string.Empty;

        public List<QuickFactItem> QuickFacts { get; set; } = new();


        public List<VisualAsset> VisualAssets { get; set; } = new();
    }


    public class ColorSpecification
    {
        public string Name { get; set; } = "";
        public string Hex { get; set; } = "";
        public string Rgb { get; set; } = "";
        public string Cmyk { get; set; } = "";
        public string Pantone { get; set; } = "";
        public string Symbolism { get; set; } = "";
    }


    public class ColorSection : IContentSection
    {
        public string Id { get; set; } = "";
        public string Icon { get; set; } = "";
        public string Title { get; set; } = "";
        public string Style { get; set; } = "";
        public string? Img { get; set; }
        public List<string> Paragraphs { get; set; } = new();
        public List<IContentSubsection> Subsections { get; set; } = new();
        public List<string> Facts { get; set; } = new();
        public List<string> ListItems { get; set; } = new();

        public string Intro { get; set; } = "";
        public List<ColorMeaningCard> ColorCards { get; set; } = new();
        public List<ColorAppearCard> AppearCards { get; set; } = new();
    }

    public class ColorMeaningCard
    {
        public string ColorName { get; set; } = "";
        public string Hex { get; set; } = "";
        public string Heading { get; set; } = "";
        public string Meaning { get; set; } = "";
    }

    public class ColorAppearCard
    {
        public string Image { get; set; } = "";
        public string Alt { get; set; } = "";
        public string Heading { get; set; } = "";
        public string Description { get; set; } = "";
    }

    public class ColorSubsection : IContentSubsection
    {
        public string Subtitle { get; set; } = "";
        public string Text { get; set; } = "";
        public List<string> ListItems { get; set; } = new();
        public LinkData? Link { get; set; }
    }

    public class ColorSource : ISource
    {
        public string Name { get; set; } = "";
        public string Url { get; set; } = "";
        public string Description { get; set; } = "";
    }

    public class ColorFaq : IFaqItem
    {
        public string Question { get; set; } = "";
        public string Answer { get; set; } = "";
    }
}
