using Microsoft.AspNetCore.Mvc;
using USASymbol.Models.ViewModels;
using USASymbol.Services;

namespace USASymbol.Views.Shared.Components
{
    public class RankingEmbedViewComponent : ViewComponent
    {
        private readonly IRankingsContentService _rankingsContentService;

        public RankingEmbedViewComponent(IRankingsContentService rankingsContentService)
        {
            _rankingsContentService = rankingsContentService;
        }

        public async Task<IViewComponentResult> InvokeAsync(string category, string slug, string? idPrefix = null)
        {
            if (string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(slug))
            {
                return Content(string.Empty);
            }

            var content = await _rankingsContentService.GetContentAsync(category, slug);
            if (content == null)
            {
                return Content(string.Empty);
            }

            var pageModel = new PageDetailViewModel { Content = content };
            var heroAlt = content.HeroImageAlt ?? content.Map?.ImageAlt ?? content.Page?.H1 ?? "USA state rankings";
            var heroImage = content.HeroImage;
            var heroCaption = content.HeroImageCaption;

            if (string.IsNullOrWhiteSpace(heroImage) && !string.IsNullOrWhiteSpace(content.Map?.Image))
            {
                heroImage = content.Map.Image;
                heroCaption ??= content.Map.Caption;
            }

            if (string.IsNullOrWhiteSpace(heroImage))
            {
                heroImage = content.VisualAssets?.FirstOrDefault(v =>
                    !string.IsNullOrWhiteSpace(v?.Src) &&
                    !string.Equals(v.Layout, "full-width", StringComparison.OrdinalIgnoreCase))?.Src;
            }

            if (string.IsNullOrWhiteSpace(heroImage) && content.Table?.Rows?.Any() == true)
            {
                var firstRow = content.Table.Rows.First();
                heroImage = new[]
                {
                    firstRow.GetString("hero_image"),
                    firstRow.GetString("symbol_image"),
                    firstRow.GetString("image")
                }.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
            }

            var model = new RankingEmbedViewModel
            {
                PageModel = pageModel,
                HeroModel = new PageHeroViewModel
                {
                    PageModel = pageModel,
                    HeroLabel = "Guide",
                    FallbackIconClass = "fa-solid fa-chart-column",
                    Title = content.Page?.H1 ?? "Ranking",
                    Description = content.Seo?.Description,
                    ImageUrl = string.IsNullOrWhiteSpace(heroImage) ? "/images/og-default.webp" : heroImage,
                    ImageAlt = heroAlt,
                    FigureTitle = content.Page?.H1,
                    FigureSubtitle = $"Ranking - {pageModel.CategoryTitle}",
                    FigureCaption = heroCaption,
                    ImageLinkUrl = heroImage,
                    EyebrowItems = new List<string>
                    {
                        "Rankings",
                        pageModel.CategoryTitle,
                        $"Updated {(content.DateModified?.ToString("MMMM d, yyyy") ?? DateTime.UtcNow.ToString("MMMM d, yyyy"))}"
                    }
                },
                HeroVisualAsset = new VisualAsset
                {
                    Id = $"{slug}-hero-map",
                    Src = string.IsNullOrWhiteSpace(heroImage) ? "/images/og-default.webp" : heroImage,
                    Alt = heroAlt ?? string.Empty,
                    Caption = heroCaption ?? string.Empty,
                    Layout = "full-width-tall"
                },
                IdPrefix = string.IsNullOrWhiteSpace(idPrefix) ? $"ranking-{slug}" : idPrefix.Trim()
            };

            return View("~/Views/Shared/Components/RankingEmbed/Default.cshtml", model);
        }
    }
}
