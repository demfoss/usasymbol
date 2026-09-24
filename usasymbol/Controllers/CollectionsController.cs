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
        private readonly CollectionsDirectoryService _directory;
        private readonly ILatestContentRailService _latestContentRailService;
        private readonly IMapPngService _mapPngService;
        private readonly ILogger<CollectionsController> _logger;

        public CollectionsController(
            ICollectionsContentService service,
            CollectionsDirectoryService directory,
            ILatestContentRailService latestContentRailService,
            IMapPngService mapPngService,
            ILogger<CollectionsController> logger)
        {
            _service = service;
            _directory = directory;
            _latestContentRailService = latestContentRailService;
            _mapPngService = mapPngService;
            _logger  = logger;
        }

        [Route("/collections")]
        public async Task<IActionResult> Index(string? q, string? topic, string? group, string? sort, int page = 1)
        {
            try
            {
                var categories = await _directory.GetCategoriesAsync();
                var model = CollectionsDirectoryService.Build(categories, null, q, topic, group, sort, page);
                if (model.Page > model.PageCount) return NotFound();
                return View("Directory", model);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error loading collections hub");
                throw;
            }
        }

        [Route("/collections/{group}")]
        public async Task<IActionResult> Group(string group, string? q, string? topic, string? sort, int page = 1)
        {
            try
            {
                var categories = await _directory.GetCategoriesAsync();
                var cat = categories.FirstOrDefault(c =>
                    string.Equals(c.Id, group, System.StringComparison.OrdinalIgnoreCase));

                if (cat == null) return NotFound();

                ViewData["Title"]       = $"{cat.Title} Collections";
                ViewData["Description"] = $"Browse U.S. state collections related to {cat.Title.ToLower()}";

                var model = CollectionsDirectoryService.Build(categories, cat, q, topic, null, sort, page);
                if (model.Page > model.PageCount) return NotFound();
                return View("Directory", model);
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

                if (group.Equals("crime", System.StringComparison.OrdinalIgnoreCase) &&
                    Request.Path.StartsWithSegments("/collections/crime") &&
                    content.Url.StartsWith("/most-dangerous-cities/", System.StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectPermanent(content.Url);
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

        [HttpGet("/most-dangerous-cities")]
        public IActionResult MostDangerousCitiesHub() => RedirectPermanent("/collections/crime");

        [HttpGet("/most-dangerous-cities/{state}")]
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any, VaryByHeader = "Accept-Encoding")]
        public Task<IActionResult> MostDangerousCitiesByState(string state) => Detail("crime", state);
    }
}
