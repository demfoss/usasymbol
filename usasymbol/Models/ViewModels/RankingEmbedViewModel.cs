namespace USASymbol.Models.ViewModels
{
    public class RankingEmbedViewModel
    {
        public PageDetailViewModel PageModel { get; set; } = null!;
        public PageHeroViewModel HeroModel { get; set; } = null!;
        public VisualAsset HeroVisualAsset { get; set; } = new();
        public string IdPrefix { get; set; } = "ranking";
    }
}
