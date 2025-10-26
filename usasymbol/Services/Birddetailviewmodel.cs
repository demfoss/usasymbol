
using USASymbol.Models.Content;

namespace USASymbol.Models.ViewModels
{
    public class BirdDetailViewModel : SymbolDetailViewModel
    {
        public BirdContent? BirdContent { get; set; }

        // Вспомогательные свойства для удобства в View
        public bool HasContent => BirdContent != null && !string.IsNullOrEmpty(BirdContent.HtmlContent);
        public bool HasSources => BirdContent?.Sources?.Any() == true;
        public bool HasSharedStates => BirdContent?.SharedStates?.Any() == true;
        public bool HasPhysicalData => !string.IsNullOrEmpty(BirdContent?.Size) ||
                                       !string.IsNullOrEmpty(BirdContent?.Wingspan) ||
                                       !string.IsNullOrEmpty(BirdContent?.Weight);
    }
}