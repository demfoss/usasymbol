using USASymbol.Models.Content;

namespace USASymbol.Models.ViewModels
{
    public class BorderDetailViewModel : SymbolDetailViewModel
    {
        public BorderContent? BorderContent { get; set; }

        public bool HasContent => BorderContent != null && !string.IsNullOrEmpty(BorderContent.HtmlContent);
        public bool HasSources => BorderContent?.Sources?.Any() == true;
        public bool HasSections => BorderContent?.Sections?.Any() == true;
        public bool HasFaq => BorderContent?.Faq?.Any() == true;
        public bool HasIntro => !string.IsNullOrEmpty(BorderContent?.IntroText);
        public bool HasCards => BorderContent?.Cards?.Any() == true;
        public bool HasMap => !string.IsNullOrEmpty(BorderContent?.Map?.Image);
    }
}