using Microsoft.AspNetCore.Mvc;
using USASymbol.Models.Content;
using USASymbol.Models.ViewModels;
using USASymbol.Services;
using USASymbol.Services.Interface;
using Usasymbol.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace USASymbol.Controllers
{
    public class CollectionsController : Controller
    {
        private readonly ICollectionsContentService _service;
        private readonly ILatestContentRailService _latestContentRailService;
        private readonly IMapPngService _mapPngService;
        private readonly ILogger<CollectionsController> _logger;

        public CollectionsController(
            ICollectionsContentService service,
            ILatestContentRailService latestContentRailService,
            IMapPngService mapPngService,
            ILogger<CollectionsController> logger)
        {
            _service = service;
            _latestContentRailService = latestContentRailService;
            _mapPngService = mapPngService;
            _logger  = logger;
        }

        [Route("/collections")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var categories = await _service.GetAllCategoriesAsync();
                return View(new PageHubViewModel { Categories = categories });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error loading collections hub");
                throw;
            }
        }

        [Route("/collections/{group}")]
        public async Task<IActionResult> Group(string group)
        {
            try
            {
                var categories = await _service.GetAllCategoriesAsync();
                var cat = categories.Find(c =>
                    string.Equals(c.Id, group, System.StringComparison.OrdinalIgnoreCase));

                if (cat == null) return NotFound();

                ViewData["Title"]       = $"{cat.Title} Collections";
                ViewData["Description"] = $"Browse U.S. state collections related to {cat.Title.ToLower()}";

                return View("Category", BuildCategoryViewModel(cat));
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error loading collection group: {Group}", group);
                throw;
            }
        }

        [Route("/collections/{group}/{slug}")]
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any, VaryByHeader = "Accept-Encoding")]
        public async Task<IActionResult> Detail(string group, string slug)
        {
            try
            {
                var content = await _service.GetContentAsync(group, slug);
                if (content == null)
                {
                    _logger.LogWarning("Collection not found: {Group}/{Slug}", group, slug);
                    return NotFound();
                }

                var vm = new PageDetailViewModel { Content = content };

                ViewData["Title"]       = content.Seo?.Title;
                ViewData["Description"] = content.Seo?.Description;
                ViewData["Canonical"]   = content.Url;
                ViewData["LatestContentRail"] = await _latestContentRailService.GetLatestItemsAsync(8);

                if (string.IsNullOrWhiteSpace(content.HeroImage) && content.Map != null && content.Table?.Rows?.Count > 0)
                {
                    var choropleth = string.IsNullOrWhiteSpace(content.Map.MetricKey)
                        ? ChoroplethBuilder.BuildFlat(content.Table.Rows)
                        : ChoroplethBuilder.Build(content.Map, content.Table.Rows);

                    var mapPngPath = await _mapPngService.EnsureMapPngAsync(slug, choropleth.Entries);
                    if (mapPngPath != null)
                        ViewData["MapPngPath"] = mapPngPath;
                }

                return View("Detail", vm);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error loading collection: {Group}/{Slug}", group, slug);
                throw;
            }
        }

        private static PageCategoryViewModel BuildCategoryViewModel(PageCategory cat)
        {
            var mostPopular = cat.Items
                .OrderByDescending(i => i.DateModified ?? i.DatePublished ?? System.DateTime.MinValue)
                .Take(2)
                .ToList();

            var subcategoryFilters = cat.Items
                .GroupBy(i => string.IsNullOrWhiteSpace(i.Subcategory) ? "Not set" : i.Subcategory!)
                .Select(g => new SubcategoryFilterOption { Value = g.Key, Label = g.Key, Count = g.Count() })
                .OrderBy(f => f.Value == "Not set" ? 1 : 0)
                .ThenByDescending(f => f.Count)
                .ToList();

            return new PageCategoryViewModel
            {
                Category = cat,
                MostPopular = mostPopular,
                SubcategoryFilters = subcategoryFilters,
            };
        }
    }
}
