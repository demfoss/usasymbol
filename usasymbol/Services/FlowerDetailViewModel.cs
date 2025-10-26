using USASymbol.Models.Content;

namespace USASymbol.Models.ViewModels
{
    public class FlowerDetailViewModel : SymbolDetailViewModel
    {
        public FlowerContent? FlowerContent { get; set; }

        // Вспомогательные свойства для удобства в View
        public bool HasContent => FlowerContent != null && !string.IsNullOrEmpty(FlowerContent.HtmlContent);
        //public bool HasSources => FlowerContent?.Sources?.Any() == true;
        //public bool HasGrowingInfo => !string.IsNullOrEmpty(FlowerContent?.SoilType) ||
        //                              !string.IsNullOrEmpty(FlowerContent?.SunRequirements) ||
        //                              !string.IsNullOrEmpty(FlowerContent?.WaterNeeds);
        //public bool HasMedicinalUses => FlowerContent?.MedicinalUses?.Any() == true;
        //public bool HasCulturalSignificance => FlowerContent?.CulturalSignificance?.Any() == true;
    }
}