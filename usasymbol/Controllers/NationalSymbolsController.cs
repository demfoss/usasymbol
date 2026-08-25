using Microsoft.AspNetCore.Mvc;
using USASymbol.Models;
using USASymbol.Models.Content;
using USASymbol.Models.ViewModels;
using USASymbol.Services.Interface;

namespace USASymbol.Controllers
{
    public sealed class NationalSymbolsController : Controller
    {
        private readonly INationalSymbolsContentService _contentService;

        public NationalSymbolsController(INationalSymbolsContentService contentService)
        {
            _contentService = contentService;
        }

        [HttpGet("/national-symbols")]
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any, VaryByHeader = "Accept-Encoding")]
        public async Task<IActionResult> Index()
        {
            var items = await _contentService.GetAllAsync();
            return View(new NationalSymbolsHubViewModel { Items = items });
        }

        [HttpGet("/national-symbols/{slug}")]
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any, VaryByHeader = "Accept-Encoding")]
        public async Task<IActionResult> Detail(string slug)
        {
            var content = await _contentService.GetContentAsync(slug);
            if (content == null)
                return NotFound();

            ViewData["Title"] = content.Seo.Title;
            ViewData["Description"] = content.Seo.Description;
            ViewData["Canonical"] = content.Url;

            var specializedView = await BuildSpecializedViewAsync(slug, content);
            if (specializedView != null)
            {
                ViewData["HideRelatedStates"] = true;
                return specializedView;
            }

            var pageModel = new PageDetailViewModel { Content = content };
            var civicModel = Prepare(
                new NationalCivicDetailViewModel { PageModel = pageModel },
                slug,
                content,
                "National Guide",
                content.Page.H1,
                null,
                null);
            ViewData["HideRelatedStates"] = true;
            return View("~/Views/NationalSymbols/Detail.cshtml", civicModel);
        }

        private async Task<IActionResult?> BuildSpecializedViewAsync(string slug, PageContent page)
        {
            switch (slug.ToLowerInvariant())
            {
                case "flag":
                {
                    var content = await _contentService.GetTypedContentAsync<FlagContent>(slug);
                    if (content == null) return null;
                    content.Name = First(content.Name, page.Page.H1);
                    var model = Prepare(new FlagDetailViewModel
                    {
                        FlagContent = content,
                        BigStat = ToBigStat(content.BigStat),
                        Timeline = ToTimeline(content.Timeline),
                        ExpertQuote = ToQuote(content.ExpertQuote)
                    }, slug, page, "National Flag", content.Name, content.AdoptedYear, content.Legislation);
                    return View("~/Views/Symbol/Flag.cshtml", model);
                }
                case "bird":
                {
                    var content = await _contentService.GetTypedContentAsync<BirdContent>(slug);
                    if (content == null) return null;
                    content.Title = First(content.Title, page.Page.H1);
                    var model = Prepare(new BirdDetailViewModel
                    {
                        BirdContent = content,
                        BigStat = ToBigStat(content.BigStat),
                        Timeline = ToTimeline(content.Timeline),
                        ExpertQuote = ToQuote(content.ExpertQuote)
                    }, slug, page, "National Bird", content.Title, content.AdoptedYear, content.Legislation, content.ScientificName);
                    return View("~/Views/Symbol/Bird.cshtml", model);
                }
                case "mammal":
                {
                    var content = await _contentService.GetTypedContentAsync<MammalContent>(slug);
                    if (content == null) return null;
                    content.Title = First(content.Title, page.Page.H1);
                    content.CommonName = First(content.CommonName, content.Title);
                    var model = Prepare(new MammalDetailViewModel
                    {
                        MammalContent = content,
                        BigStat = ToBigStat(content.BigStat),
                        Timeline = ToTimeline(content.Timeline),
                        ExpertQuote = ToQuote(content.ExpertQuote)
                    }, slug, page, "National Mammal", content.CommonName, content.AdoptedYear, content.Legislation, content.ScientificName);
                    return View("~/Views/Symbol/Mammal.cshtml", model);
                }
                case "flower":
                {
                    var content = await _contentService.GetTypedContentAsync<FlowerContent>(slug);
                    if (content == null) return null;
                    content.Name = First(content.Name, page.Page.H1);
                    var model = Prepare(new FlowerDetailViewModel
                    {
                        FlowerContent = content,
                        BigStat = ToBigStat(content.BigStat),
                        Timeline = ToTimeline(content.Timeline),
                        ExpertQuote = ToQuote(content.ExpertQuote)
                    }, slug, page, "National Floral Emblem", content.Name, content.AdoptedYear, content.Legislation, content.ScientificName);
                    return View("~/Views/Symbol/Flower.cshtml", model);
                }
                case "tree":
                {
                    var content = await _contentService.GetTypedContentAsync<TreeContent>(slug);
                    if (content == null) return null;
                    content.Name = First(content.Name, page.Page.H1);
                    var model = Prepare(new TreeDetailViewModel
                    {
                        TreeContent = content,
                        BigStat = ToBigStat(content.BigStat),
                        Timeline = ToTimeline(content.Timeline),
                        ExpertQuote = ToQuote(content.ExpertQuote)
                    }, slug, page, "National Tree", content.Name, content.AdoptedYear, content.Legislation, content.ScientificName);
                    return View("~/Views/Symbol/Tree.cshtml", model);
                }
                case "colors":
                {
                    var content = await _contentService.GetTypedContentAsync<ColorContent>(slug);
                    if (content == null) return null;
                    content.Title = First(content.Title, page.Page.H1);
                    var model = Prepare(new ColorDetailViewModel
                    {
                        ColorContent = content,
                        BigStat = ToBigStat(content.BigStat),
                        Timeline = ToTimeline(content.Timeline),
                        ExpertQuote = ToQuote(content.ExpertQuote)
                    }, slug, page, "National Colors", content.Title, content.AdoptedYear, content.Legislation);
                    return View("~/Views/Symbol/Color.cshtml", model);
                }
                case "seal":
                {
                    var content = await _contentService.GetTypedContentAsync<SealContent>(slug);
                    if (content == null) return null;
                    content.Name = First(content.Name, page.Page.H1);
                    content.HeroImage = First(content.HeroImage, page.HeroImage ?? string.Empty);
                    content.HeroImageAlt = First(content.HeroImageAlt, page.HeroImageAlt ?? content.Name);
                    var model = Prepare(new SealDetailViewModel
                    {
                        SealContent = content,
                        SymbolTypeName = "Great Seal",
                        SymbolTypeSlug = "seal",
                        SymbolTypePlural = "seals",
                        DefaultDesignation = "Great Seal",
                        ShowQuizPromo = false,
                        BigStat = ToBigStat(content.BigStat),
                        Timeline = ToTimeline(content.Timeline),
                        ExpertQuote = ToQuote(content.ExpertQuote)
                    }, slug, page, "Great Seal", content.Name, content.AdoptedYear, content.Legislation);
                    return View("~/Views/Symbol/Seal.cshtml", model);
                }
                case "motto":
                {
                    var content = await _contentService.GetTypedContentAsync<MottoContent>(slug);
                    if (content == null) return null;
                    content.Title = First(content.Title, page.Page.H1);
                    var model = Prepare(new MottoDetailViewModel { MottoContent = content }, slug, page,
                        "National Motto", content.Title, content.AdoptedYear, content.Legislation);
                    return View("~/Views/Symbol/Motto.cshtml", model);
                }
                case "anthem":
                case "march":
                {
                    var content = await _contentService.GetTypedContentAsync<SongContent>(slug);
                    if (content == null) return null;
                    content.Name = First(content.Name, page.Page.H1);
                    content.HeroImage = First(content.HeroImage, page.HeroImage ?? string.Empty);
                    content.HeroImageAlt = First(content.HeroImageAlt, page.HeroImageAlt ?? content.Name);
                    var designation = slug == "anthem" ? "National Anthem" : "National March";
                    var model = Prepare(new SongDetailViewModel
                    {
                        SongContent = content,
                        SymbolTypeName = designation,
                        SymbolTypeSlug = slug,
                        SymbolTypePlural = slug == "anthem" ? "anthems" : "marches",
                        DefaultDesignation = designation,
                        AssetBasePath = "/images/amphibians/national-symbols"
                    }, slug, page, designation, content.Name, content.AdoptedYear, content.Legislation);
                    return View("~/Views/Symbol/Song.cshtml", model);
                }
                default:
                    return null;
            }
        }

        private static T Prepare<T>(T model, string slug, PageContent page, string designation,
            string name, int? adoptedYear, string? legislation, string? scientificName = null)
            where T : SymbolDetailViewModel
        {
            var state = new State { Name = "United States", Slug = "united-states", Abbreviation = "USA" };
            model.State = state;
            model.Symbol = new Symbol
            {
                Name = name,
                Slug = slug,
                Type = slug,
                Designation = designation,
                AdoptedYear = adoptedYear,
                Legislation = legislation,
                ScientificName = scientificName,
                ImageUrl = page.HeroImage,
                State = state
            };
            model.RelatedSymbols = new List<Symbol>();
            model.QuizQuestions = new();
            model.IsNationalSymbol = true;
            model.CanonicalUrl = $"/national-symbols/{slug}";
            return model;
        }

        private static BigStatViewModel? ToBigStat(BigStatData? data) => data == null ? null : new BigStatViewModel
        {
            Number = data.Number,
            Description = data.Description
        };

        private static IReadOnlyList<TimelineEventViewModel>? ToTimeline(List<TimelineEvent>? events) =>
            events == null || events.Count == 0 ? null : events.Select(item => new TimelineEventViewModel
            {
                Year = item.Year,
                Description = item.Description
            }).ToList();

        private static ExpertQuoteViewModel? ToQuote(ExpertQuoteData? data) => data == null ? null : new ExpertQuoteViewModel
        {
            Text = data.Text,
            Source = data.Source
        };

        private static string First(string? value, string fallback) =>
            string.IsNullOrWhiteSpace(value) ? fallback : value;
    }
}
