using System.Collections.Generic;

namespace USASymbol.Models.ViewModels
{
    public enum SymbolHeroVariant
    {
        Split,
        WideMedia,
        Quote,
        TextOnly
    }

    public class SymbolHeroViewModel
    {
        public ISymbolDetailViewModel PageModel { get; set; } = null!;
        public SymbolHeroVariant Variant { get; set; } = SymbolHeroVariant.Split;
        public List<QuickFactItem> HeroFacts { get; set; } = new();
        public int MaxHeroFacts { get; set; } = 5;
        public string BackgroundClass { get; set; } = "bg-[linear-gradient(180deg,#1b2230_0%,#1f2735_100%)]";
        public string TopRuleClass { get; set; } = "bg-gradient-to-r from-sky-300/0 via-sky-300/70 to-amber-200/0";
        public string PrimaryGlowClass { get; set; } = "bg-amber-300/10";
        public string SecondaryGlowClass { get; set; } = "bg-sky-200/10";
        public string AccentDotClass { get; set; } = "bg-amber-300";
        public string MediaBackgroundClass { get; set; } = "bg-slate-100/10";
        public string FallbackIconClass { get; set; } = "fa-solid fa-star";
        public string Title { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public string? Description { get; set; }
        public string? LegalReference { get; set; }
        public string? ImageUrl { get; set; }
        public string? ImageAlt { get; set; }
        public string ImageClass { get; set; } = "absolute inset-0 h-full w-full object-cover";
        public string? FigureTitle { get; set; }
        public string? FigureSubtitle { get; set; }
        public string? FigureCaption { get; set; }
        public string? ImageLinkUrl { get; set; }
        public string? HeroLabel { get; set; } = "Official state symbol";
        public List<string> EyebrowItems { get; set; } = new();
        public List<string> FeaturedBadges { get; set; } = new();
        public string? FeaturedLabel { get; set; }
        public string? FeaturedText { get; set; }
        public string? FeaturedTextClass { get; set; }
        public string? FeaturedNoteLabel { get; set; }
        public string? FeaturedNoteText { get; set; }
        public string FeaturedPanelClass { get; set; } = "bg-[linear-gradient(135deg,rgba(15,23,42,0.96),rgba(120,53,15,0.9),rgba(15,23,42,0.96))]";
        public string FeaturedNoteClass { get; set; } = "bg-white/10";
        public string? ActionButtonId { get; set; }
        public string? ActionButtonLabel { get; set; }
        public string? ActionButtonIconClass { get; set; }
        public string? ActionAudioSrc { get; set; }
        public List<string> PaletteHexes { get; set; } = new();
        public bool UseLightFigureCaption { get; set; }
    }
}
