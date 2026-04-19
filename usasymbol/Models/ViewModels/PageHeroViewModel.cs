using System.Collections.Generic;

namespace USASymbol.Models.ViewModels
{
    public class PageHeroViewModel
    {
        public PageDetailViewModel PageModel { get; set; } = null!;
        public string BackgroundClass { get; set; } = "bg-[linear-gradient(180deg,#1b2230_0%,#1f2735_100%)]";
        public string TopRuleClass { get; set; } = "bg-gradient-to-r from-sky-300/0 via-sky-300/70 to-amber-200/0";
        public string PrimaryGlowClass { get; set; } = "bg-amber-300/10";
        public string SecondaryGlowClass { get; set; } = "bg-sky-200/10";
        public string AccentDotClass { get; set; } = "bg-amber-300";
        public string MediaBackgroundClass { get; set; } = "bg-slate-100/10";
        public string FallbackIconClass { get; set; } = "fa-solid fa-layer-group";
        public string HeroLabel { get; set; } = "Guide";
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? ImageAlt { get; set; }
        public string? FigureTitle { get; set; }
        public string? FigureSubtitle { get; set; }
        public string? FigureCaption { get; set; }
        public string? ImageLinkUrl { get; set; }
        public List<string> EyebrowItems { get; set; } = new();
    }
}
