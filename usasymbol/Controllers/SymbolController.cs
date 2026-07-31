using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using usasymbol.Models;
using usasymbol.Services;
using usasymbol.Services.Interface;
using USASymbol.Services.Interface;
using USASymbol.Data;
using USASymbol.Extensions;
using USASymbol.Models;
using USASymbol.Models.ViewModels;
using USASymbol.Services;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Text.RegularExpressions;


namespace USASymbol.Controllers
{
    public class SymbolController : Controller
    {
        private readonly IStateService _stateService;
        private readonly ISymbolCanonicalService _symbolCanonicalService;
        private readonly ISymbolService _symbolService;
        private readonly IBirdService _birdService;
        private readonly INicknameService _nicknameService;
        private readonly IFlowerService _flowerService;
        private readonly IFlagService _flagService;
        private readonly ITreeService _treeService;
        private readonly IMottoService _mottoService;
        private readonly IMammalService _mammalService;
        private readonly IFirearmService _firearmService;
        private readonly IDinosaurService _dinosaurService;
        private readonly IBeverageService _beverageService;
        private readonly ILicensePlateService _licensePlateService;
        private readonly IColorService _colorService;
        private readonly ISealService _sealService;
        private readonly ISoilService _soilService;
        private readonly IFossilService _fossilService;
        private readonly ISportService _sportService;
        private readonly IDanceService _danceService;
        private readonly IInsectService _insectService;
        private readonly IMineralService _mineralService;
        private readonly IAmphibianService _amphibianService;
        private readonly IReptileService _reptileService;
        private readonly IFoodService _foodService;
        private readonly ILatestContentRailService _latestContentRailService;
        private readonly QuizService _quizService;
        private readonly ILogger<SymbolController> _logger;
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;
        private static readonly Dictionary<string, string> CategoryIcons = new(StringComparer.OrdinalIgnoreCase)
        {
            ["birds"] = "fa-solid fa-dove",
            ["flowers"] = "fa-solid fa-spa",
            ["trees"] = "fa-solid fa-tree",
            ["flags"] = "fa-solid fa-flag-usa",
            ["beverages"] = "fa-solid fa-glass-water",
            ["mammals"] = "fa-solid fa-paw",
            ["cats"] = "fa-solid fa-cat",
            ["dogs"] = "fa-solid fa-dog",
            ["horses"] = "fa-solid fa-horse",
            ["dinosaurs"] = "fa-solid fa-dragon",
            ["mottos"] = "fa-solid fa-scroll",
            ["nicknames"] = "fa-solid fa-tag",
            ["colors"] = "fa-solid fa-palette",
            ["marine-mammals"] = "fa-solid fa-fish",
            ["firearms"] = "fa-solid fa-bullseye",
            ["license-plate-slogans"] = "fa-solid fa-car-side",
            ["state-seals"] = "fa-solid fa-stamp",
            ["coats-of-arms"] = "fa-solid fa-shield-halved",
            ["soils"] = "fa-solid fa-mountain",
            ["fossils"] = "fa-solid fa-bone",
            ["sports"] = "fa-solid fa-medal",
            ["dances"] = "fa-solid fa-music",
            ["insects"] = "fa-solid fa-bug",
            ["butterflies"] = "fa-solid fa-bug",
            ["minerals"] = "fa-solid fa-cube",
            ["rocks"] = "fa-solid fa-mountain",
            ["gemstones"] = "fa-solid fa-gem",
            ["amphibians"] = "fa-solid fa-frog",
            ["reptiles"] = "fa-solid fa-turtle",
            ["fruits"] = "fa-solid fa-apple-whole",
            ["vegetables"] = "fa-solid fa-carrot",
            ["nuts"] = "fa-solid fa-seedling",
            ["desserts"] = "fa-solid fa-cookie-bite",
            ["spirits"] = "fa-solid fa-wine-bottle",
            ["dishes"] = "fa-solid fa-bowl-food",
            ["crops"] = "fa-solid fa-wheat-awn"
        };

        public SymbolController(
            INicknameService nicknameService,
            IStateService stateService,
            ISymbolCanonicalService symbolCanonicalService,
            ISymbolService symbolService,
            IBirdService birdService,
            IMottoService mottoService,
            IFlowerService flowerService,
            ITreeService treeService,
            IFlagService flagService,
            IMammalService mammalService,
            IFirearmService firearmService,
            IDinosaurService dinosaurService,
            IBeverageService beverageService,
            ILicensePlateService licensePlateService,
            IColorService colorService,
            ISealService sealService,
            ISoilService soilService,
            IFossilService fossilService,
            ISportService sportService,
            IDanceService danceService,
            IInsectService insectService,
            IMineralService mineralService,
            IAmphibianService amphibianService,
            IReptileService reptileService,
            IFoodService foodService,
            ILatestContentRailService latestContentRailService,
            QuizService quizService,
            ILogger<SymbolController> logger,
            AppDbContext db,
            IWebHostEnvironment env)
        {
            _stateService = stateService;
            _symbolCanonicalService = symbolCanonicalService;
            _symbolService = symbolService;
            _birdService = birdService;
            _mottoService = mottoService;
            _nicknameService = nicknameService;
            _flowerService = flowerService;
            _flagService = flagService;
            _treeService = treeService;
            _mammalService = mammalService;
            _firearmService = firearmService;
            _dinosaurService = dinosaurService;
            _beverageService = beverageService;
            _licensePlateService = licensePlateService;
            _colorService = colorService;
            _sealService = sealService;
            _soilService = soilService;
            _fossilService = fossilService;
            _sportService = sportService;
            _danceService = danceService;
            _insectService = insectService;
            _mineralService = mineralService;
            _amphibianService = amphibianService;
            _reptileService = reptileService;
            _foodService = foodService;
            _latestContentRailService = latestContentRailService;
            _quizService = quizService;
            _logger = logger;
            _db = db;
            _env = env;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var executedContext = await next();

            if (executedContext.Result is not ViewResult viewResult ||
                viewResult.Model is not ISymbolDetailViewModel)
            {
                return;
            }

            if (!ViewData.ContainsKey("AllStates"))
            {
                ViewData["AllStates"] = await _db.States.AsNoTracking()
                    .Select(s => new { s.Name, s.Slug, s.Abbreviation, s.Capital, s.Region })
                    .ToListAsync();
            }

            if (!ViewData.ContainsKey("LatestContentRail"))
            {
                ViewData["LatestContentRail"] = await _latestContentRailService.GetLatestItemsAsync(8);
            }
        }

        [Route("symbols")]
        public async Task<IActionResult> Categories()
        {
            ViewData["Title"] = "All State Symbol Categories";
            ViewData["Description"] = "Browse all types of official U.S. state symbols - birds, flowers, trees, flags, and more.";

            var allSymbols = await _db.Symbols
                .AsNoTracking()
                .ToListAsync();

            var categories = await _db.SymbolCategories
                .AsNoTracking()
                .OrderBy(c => c.Id)
                .ToListAsync();

            var listingAssets = GetSymbolListingAssets();
            var licensePlatePreviewImages = GetLicensePlatePreviewImages();
            var rng = new Random();

            var viewModel = categories.Select(c =>
            {
                var matchedSymbols = GetCategorySymbols(c.Type, allSymbols).ToList();
                var stateCount = matchedSymbols
                    .Select(s => s.StateId)
                    .Distinct()
                    .Count();

                if (stateCount == 0 && listingAssets.TryGetValue(c.Type, out var listingFallback))
                    stateCount = listingFallback.StateCount;

                var previewImages = matchedSymbols
                    .Select(s => s.ImageUrl)
                    .Where(path => !string.IsNullOrWhiteSpace(path) && !path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                    .Cast<string>()
                    .ToList();

                if (c.Type == "cats")
                {
                    previewImages = new List<string> { "/images/mammals/maine/maine-coon-cat.jpg" };
                }
                else if (c.Type == "license-plate-slogans" && licensePlatePreviewImages.Count > 0)
                {
                    previewImages = licensePlatePreviewImages;
                }

                var randomImage = previewImages.Count > 0
                    ? previewImages[rng.Next(previewImages.Count)]
                    : listingAssets.TryGetValue(c.Type, out var listingPreview) && !string.IsNullOrWhiteSpace(listingPreview.ImageUrl)
                        ? listingPreview.ImageUrl
                        : c.ImageUrl;

                return new SymbolCategoryViewModel
                {
                    Type = c.Type,
                    Name = c.Name,
                    Description = c.Description,
                    ImageUrl = randomImage,
                    StateCount = stateCount,
                    Url = $"/symbols/{c.Type}",
                    Icon = GetCategoryIcon(c.Type)

                };
            }).ToList();

            return View("Categories", viewModel);
        }

        private static IEnumerable<Symbol> GetCategorySymbols(string categoryType, IEnumerable<Symbol> symbols)
        {
            return symbols.Where(symbol => IsCategoryMatch(categoryType, symbol));
        }

        private static bool IsCategoryMatch(string categoryType, Symbol symbol)
        {
            var designation = symbol.Designation ?? string.Empty;
            var name = symbol.Name ?? string.Empty;

            return categoryType switch
            {
                "birds" => symbol.Type == "bird",
                "flowers" => symbol.Type == "flower",
                "trees" => symbol.Type == "tree",
                "flags" => symbol.Type == "flag",
                "beverages" => symbol.Type == "beverage",
                "mammals" => symbol.Type == "mammal" && !IsDog(designation) && !IsHorse(designation, name) && !IsMarineMammal(designation) && !IsCat(designation, name),
                "cats" => symbol.Type == "mammal" && IsCat(designation, name),
                "dogs" => symbol.Type == "mammal" && IsDog(designation),
                "horses" => symbol.Type == "mammal" && IsHorse(designation, name),
                "marine-mammals" => symbol.Type == "mammal" && IsMarineMammal(designation),
                "dinosaurs" => symbol.Type == "dinosaur",
                "mottos" => symbol.Type == "motto",
                "nicknames" => symbol.Type == "nickname",
                "colors" => symbol.Type == "color",
                "firearms" => symbol.Type == "firearm",
                "license-plate-slogans" => symbol.Type == "license-plate",
                "state-seals" => symbol.Type == "state-seal",
                "coats-of-arms" => symbol.Type == "coat-of-arms",
                "sports" => symbol.Type == "sport",
                "dances" => symbol.Type == "dance",
                "insects" => symbol.Type == "insect",
                "butterflies" => symbol.Type == "insect" && IsButterfly(designation, name),
                "minerals" => symbol.Type == "mineral",
                "rocks" => symbol.Type == "rock",
                "gemstones" => symbol.Type == "gemstone",
                "amphibians" => symbol.Type == "amphibian",
                "reptiles" => symbol.Type == "reptile",
                "fruits" => symbol.Type == "food" && GetFoodBucket(designation) == "fruits",
                "vegetables" => symbol.Type == "food" && GetFoodBucket(designation) == "vegetables",
                "nuts" => symbol.Type == "food" && GetFoodBucket(designation) == "nuts",
                "desserts" => symbol.Type == "food" && GetFoodBucket(designation) == "desserts",
                "spirits" => symbol.Type == "food" && GetFoodBucket(designation) == "spirits",
                "dishes" => symbol.Type == "food" && GetFoodBucket(designation) == "dishes",
                "crops" => symbol.Type == "food" && GetFoodBucket(designation) == "crops",
                _ => false
            };
        }

        // State food designations vary wildly by name (State Cookie, State Nut, State Tree Fruit,
        // State Legume, State Cuisine...). Symbol.Type is the shared "food" value for all of them;
        // this buckets the specific designation into a broad, browsable listing category.
        private static string GetFoodBucket(string designation)
        {
            var d = designation ?? string.Empty;

            bool Has(params string[] keywords) =>
                keywords.Any(k => d.Contains(k, StringComparison.OrdinalIgnoreCase));

            if (Has("fruit", "berry")) return "fruits";
            if (Has("vegetable", "squash", "pepper")) return "vegetables";
            if (Has("nut")) return "nuts";
            if (Has("cookie", "cake", "pie", "dessert", "muffin", "doughnut", "treat", "pastry", "candy")) return "desserts";
            if (Has("spirit", "drink", "pop", "soda")) return "spirits";
            if (Has("meal", "cuisine", "dish", "sandwich", "bread", "snack", "steak", "cobbler")) return "dishes";
            return "crops";
        }

        private static bool IsButterfly(string designation, string name) =>
            designation.Contains("butterfly", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("butterfly", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("swallowtail", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("fritillary", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("hairstreak", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("sulphur", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("longwing", StringComparison.OrdinalIgnoreCase);

        private static bool IsDog(string designation) =>
            designation.Contains("dog", StringComparison.OrdinalIgnoreCase);

        private static bool IsMarineMammal(string designation) =>
            designation.Contains("marine mammal", StringComparison.OrdinalIgnoreCase);

        private static bool IsCat(string designation, string name) =>
            designation.Contains("state cat", StringComparison.OrdinalIgnoreCase) ||
            designation.Contains("official state cat", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("maine coon cat", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("calico cat", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("tabby cat", StringComparison.OrdinalIgnoreCase);

        private static bool IsHorse(string designation, string name) =>
            designation.Contains("horse", StringComparison.OrdinalIgnoreCase) ||
            designation.Contains("pony", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("horse", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("pony", StringComparison.OrdinalIgnoreCase);

        private Dictionary<string, ListingAsset> GetSymbolListingAssets()
        {
            var images = new Dictionary<string, ListingAsset>(StringComparer.OrdinalIgnoreCase);
            var symbolsDir = Path.Combine(_env.ContentRootPath, "Content", "symbols");
            if (!Directory.Exists(symbolsDir))
            {
                return images;
            }

            foreach (var file in Directory.EnumerateFiles(symbolsDir, "*.yml", SearchOption.TopDirectoryOnly))
            {
                var yaml = System.IO.File.ReadAllText(file);
                var rowImages = Regex.Matches(yaml, @"(?m)^\s+symbol_image:\s*""?(/images/[^\s""]+)""?\s*$")
                    .Select(m => m.Groups[1].Value.Trim())
                    .Where(path => !string.IsNullOrWhiteSpace(path))
                    .ToList();

                var usableRowImages = rowImages
                    .Where(path => System.IO.File.Exists(Path.Combine(_env.WebRootPath, path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar))))
                    .ToList();

                var previewImage = usableRowImages.FirstOrDefault(path => !path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                    ?? usableRowImages.FirstOrDefault();

                if (string.IsNullOrWhiteSpace(previewImage))
                {
                    var heroMatch = Regex.Match(yaml, @"(?m)^hero_image:\s*(.+?)\s*$");
                    if (heroMatch.Success)
                    {
                        var heroImage = heroMatch.Groups[1].Value.Trim().Trim('"', '\'');
                        var heroPath = Path.Combine(_env.WebRootPath, heroImage.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                        if (!string.IsNullOrWhiteSpace(heroImage) && System.IO.File.Exists(heroPath))
                        {
                            previewImage = heroImage;
                        }
                    }
                }

                var stateCount = Regex.Matches(yaml, @"(?m)^\s*state_slug:\s*(.+?)\s*$")
                    .Select(m => m.Groups[1].Value.Trim().Trim('"', '\''))
                    .Where(slug => !string.IsNullOrWhiteSpace(slug))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count();

                images[Path.GetFileNameWithoutExtension(file)] = new ListingAsset(previewImage, stateCount);
            }

            return images;
        }

        private List<string> GetLicensePlatePreviewImages()
        {
            var images = new List<string>();
            var statesDir = Path.Combine(_env.ContentRootPath, "Content", "states");
            if (!Directory.Exists(statesDir))
            {
                return images;
            }

            foreach (var file in Directory.EnumerateFiles(statesDir, "license-plate.yaml", SearchOption.AllDirectories))
            {
                var yaml = System.IO.File.ReadAllText(file);
                var versionMatch = Regex.Match(yaml, @"(?m)^\s*image:\s*(\/images\/license-plates\/.+?)\s*$");
                var heroMatch = Regex.Match(yaml, @"(?m)^hero_image:\s*(\/images\/license-plates\/.+?)\s*$");
                var imageUrl = versionMatch.Success
                    ? versionMatch.Groups[1].Value.Trim().Trim('"', '\'')
                    : heroMatch.Success
                        ? heroMatch.Groups[1].Value.Trim().Trim('"', '\'')
                        : string.Empty;

                if (string.IsNullOrWhiteSpace(imageUrl))
                {
                    continue;
                }

                var localPath = Path.Combine(_env.WebRootPath, imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(localPath))
                {
                    images.Add(imageUrl);
                }
            }

            return images;
        }

        private static string GetCategoryIcon(string type)
        {
            return CategoryIcons.TryGetValue(type, out var icon)
                ? icon
                : "fa-solid fa-star";
        }

        private sealed record ListingAsset(string? ImageUrl, int StateCount);

        [Route("symbol/{symbolType}")]
        public async Task<IActionResult> LegacySymbol(string symbolType)
        {
            var stateSlug = GetLegacyStateSlug();

            if (string.IsNullOrWhiteSpace(stateSlug))
                return NotFound();

            var state = await _stateService.GetStateBySlugAsync(stateSlug);
            if (state == null)
                return NotFound();

            var symbol = await _symbolCanonicalService.ResolveCanonicalSymbolAsync(state, symbolType);
            if (symbol == null)
                return NotFound();

            return RedirectPermanent(symbol.ToSymbolUrl(state.Slug));
        }

        [OutputCache(PolicyName = "SymbolDetail")]
        [Route("states/{stateSlug}/bird/{birdSlug}")]
        public async Task<IActionResult> Bird(string stateSlug, string birdSlug)
        {
            var state = await _stateService.GetStateBySlugAsync(stateSlug);
            if (state == null)
                return NotFound();

            var birdSymbol = await _symbolService.GetSymbolAsync(state.Id, "bird");
            if (birdSymbol == null)
                return NotFound();

            var redirect = RedirectToCanonicalIfNeeded(birdSlug, birdSymbol, state.Slug);
            if (redirect != null)
            {
                return redirect;
            }

            var birdContent = await _birdService.GetBirdContentAsync(stateSlug);
            var relatedSymbols = await GetRelatedSymbolsAsync(state.Id, birdSymbol.Id);
            var quizQuestions = BuildQuizQuestions("us-states-general-quiz");
            var model = new BirdDetailViewModel
            {
                State = state,
                Symbol = birdSymbol,
                BirdContent = birdContent,
                RelatedSymbols = relatedSymbols,
                QuizQuestions = quizQuestions,

                BigStat = birdContent?.BigStat == null ? null : new BigStatViewModel
                {
                    Number = birdContent.BigStat.Number,
                    Description = birdContent.BigStat.Description
                },

                Timeline = (birdContent?.Timeline == null || birdContent.Timeline.Count == 0)
                    ? null
                    : birdContent.Timeline.Select(t => new TimelineEventViewModel
                    {
                        Year = t.Year,
                        Description = t.Description
                    }).ToList(),

                ExpertQuote = birdContent?.ExpertQuote == null ? null : new ExpertQuoteViewModel
                {
                    Text = birdContent.ExpertQuote.Text,
                    Source = birdContent.ExpertQuote.Source
                }
            };

            return View(model);
        }

        [OutputCache(PolicyName = "SymbolDetail")]
        [Route("states/{stateSlug}/mammal/{mammalSlug}")]
        public async Task<IActionResult> Mammal(string stateSlug, string mammalSlug)
        {
            var state = await _stateService.GetStateBySlugAsync(stateSlug);
            if (state == null)
            {
                _logger.LogWarning("State not found: {StateSlug}", stateSlug);
                return NotFound();
            }

            var mammalSymbol = await _symbolService.GetSymbolBySlugAsync(state.Id, mammalSlug);
            if (mammalSymbol == null || mammalSymbol.Type != "mammal")
            {
                _logger.LogWarning("mammal symbol not found for state: {StateSlug}, slug: {mammalSlug}", stateSlug, mammalSlug);
                return NotFound();
            }

            var mammalContent = await _mammalService.GetMammalContentAsync(state.Slug, mammalSymbol.Slug);
            if (mammalContent == null)
            {
                _logger.LogWarning("Animal content YAML not found for state: {StateSlug}", stateSlug);
            }
            else
            {
                _logger.LogInformation("Animal content loaded: Name={Name}, Sections={SectionCount}, FAQ={FaqCount}",
                    mammalContent.Title,
                    mammalContent.Sections?.Count ?? 0,
                    mammalContent.Faq?.Count ?? 0);
            }

            var relatedSymbols = await GetRelatedSymbolsAsync(state.Id, mammalSymbol.Id);
            var quizQuestions = BuildQuizQuestions("us-states-general-quiz");
            var model = new MammalDetailViewModel
            {
                State = state,
                Symbol = mammalSymbol,
                MammalContent = mammalContent,
                RelatedSymbols = relatedSymbols,
                QuizQuestions = quizQuestions,
                BigStat = mammalContent?.BigStat == null ? null : new BigStatViewModel
                {
                    Number = mammalContent.BigStat.Number,
                    Description = mammalContent.BigStat.Description
                },

                Timeline = mammalContent?.Timeline?.Select(t => new TimelineEventViewModel
                {
                    Year = t.Year,
                    Description = t.Description
                }).ToList(),

                ExpertQuote = mammalContent?.ExpertQuote == null ? null : new ExpertQuoteViewModel
                {
                    Text = mammalContent.ExpertQuote.Text,
                    Source = mammalContent.ExpertQuote.Source
                }
            };

            return View("Mammal", model);
        }

        [OutputCache(PolicyName = "SymbolDetail")]
        [Route("states/{stateSlug}/firearm/{firearmSlug}")]
        public async Task<IActionResult> Firearm(string stateSlug, string firearmSlug = "")
        {
            var state = await _stateService.GetStateBySlugAsync(stateSlug);
            if (state == null)
                return NotFound();

            var firearmSymbol = await _symbolService.GetSymbolAsync(state.Id, "firearm");
            if (firearmSymbol == null)
                return NotFound();

            var redirect = RedirectToCanonicalIfNeeded(firearmSlug, firearmSymbol, state.Slug);
            if (redirect != null)
            {
                return redirect;
            }

            var firearmContent = await _firearmService.GetFirearmContentAsync(state.Slug);
            if (firearmContent == null)
                _logger.LogWarning("Firearm YAML not found for state: {StateSlug}", stateSlug);

            var relatedSymbols = await GetRelatedSymbolsAsync(state.Id, firearmSymbol.Id);
            var quizQuestions = BuildQuizQuestions("us-states-general-quiz");

            var model = new FirearmDetailViewModel
            {
                State = state,
                Symbol = firearmSymbol,
                FirearmContent = firearmContent,
                RelatedSymbols = relatedSymbols,
                QuizQuestions = quizQuestions,

                BigStat = firearmContent?.BigStat == null ? null : new BigStatViewModel
                {
                    Number = firearmContent.BigStat.Number,
                    Description = firearmContent.BigStat.Description
                },

                Timeline = (firearmContent?.Timeline == null || firearmContent.Timeline.Count == 0)
                    ? null
                    : firearmContent.Timeline.Select(t => new TimelineEventViewModel
                    {
                        Year = t.Year,
                        Description = t.Description
                    }).ToList(),

                ExpertQuote = firearmContent?.ExpertQuote == null ? null : new ExpertQuoteViewModel
                {
                    Text = firearmContent.ExpertQuote.Text,
                    Source = firearmContent.ExpertQuote.Source
                }
            };

            return View("Firearm", model);
        }

        [OutputCache(PolicyName = "SymbolDetail")]
        [Route("states/{stateSlug}/dinosaur/{dinosaurSlug}")]
        public async Task<IActionResult> Dinosaur(string stateSlug, string dinosaurSlug)
        {
            var state = await _stateService.GetStateBySlugAsync(stateSlug);
            if (state == null)
                return NotFound();

            var dinosaurSymbol = await _symbolService.GetSymbolAsync(state.Id, "dinosaur");
            if (dinosaurSymbol == null)
                return NotFound();

            var redirect = RedirectToCanonicalIfNeeded(dinosaurSlug, dinosaurSymbol, state.Slug);
            if (redirect != null)
            {
                return redirect;
            }

            var dinosaurContent = await _dinosaurService.GetDinosaurContentAsync(state.Slug);
            if (dinosaurContent == null)
                _logger.LogWarning("Dinosaur YAML not found for state: {StateSlug}", stateSlug);

            var relatedSymbols = await GetRelatedSymbolsAsync(state.Id, dinosaurSymbol.Id);
            var quizQuestions = BuildQuizQuestions("us-states-general-quiz");

            var model = new DinosaurDetailViewModel
            {
                State = state,
                Symbol = dinosaurSymbol,
                DinosaurContent = dinosaurContent,
                RelatedSymbols = relatedSymbols,
                QuizQuestions = quizQuestions,

                BigStat = dinosaurContent?.BigStat == null ? null : new BigStatViewModel
                {
                    Number = dinosaurContent.BigStat.Number,
                    Description = dinosaurContent.BigStat.Description
                },

                Timeline = (dinosaurContent?.Timeline == null || dinosaurContent.Timeline.Count == 0)
                    ? null
                    : dinosaurContent.Timeline.Select(t => new TimelineEventViewModel
                    {
                        Year = t.Year,
                        Description = t.Description
                    }).ToList(),

                ExpertQuote = dinosaurContent?.ExpertQuote == null ? null : new ExpertQuoteViewModel
                {
                    Text = dinosaurContent.ExpertQuote.Text,
                    Source = dinosaurContent.ExpertQuote.Source
                }
            };

            return View("Dinosaur", model);
        }

        [OutputCache(PolicyName = "SymbolDetail")]
        [Route("states/{stateSlug}/beverage/{beverageSlug}")]
        public async Task<IActionResult> Beverage(string stateSlug, string beverageSlug)
        {
            var state = await _stateService.GetStateBySlugAsync(stateSlug);
            if (state == null)
                return NotFound();

            var beverageSymbol = await _symbolService.GetSymbolBySlugAsync(state.Id, beverageSlug);
            if (beverageSymbol == null || beverageSymbol.Type != "beverage")
                return NotFound();

            var allSymbols = await _symbolService.GetSymbolsByStateAsync(state.Id);
            var stateBeverages = allSymbols
                .Where(s => s.Type == "beverage")
                .ToList();

            var beverageContent = await _beverageService.GetBeverageContentAsync(state.Slug, beverageSymbol.Slug);
            if (beverageContent == null)
            {
                _logger.LogInformation("Beverage YAML not found for state: {StateSlug}, slug: {BeverageSlug}. Using generated fallback content.", stateSlug, beverageSlug);
                beverageContent = _beverageService.BuildFallbackContent(state, beverageSymbol, stateBeverages);
            }

            var relatedSymbols = GetRelatedSymbols(allSymbols, beverageSymbol.Id);
            var quizQuestions = BuildQuizQuestions("us-states-general-quiz");

            var model = new BeverageDetailViewModel
            {
                State = state,
                Symbol = beverageSymbol,
                BeverageContent = beverageContent,
                RelatedSymbols = relatedSymbols,
                QuizQuestions = quizQuestions,

                BigStat = beverageContent?.BigStat == null ? null : new BigStatViewModel
                {
                    Number = beverageContent.BigStat.Number,
                    Description = beverageContent.BigStat.Description
                },

                Timeline = (beverageContent?.Timeline == null || beverageContent.Timeline.Count == 0)
                    ? null
                    : beverageContent.Timeline.Select(t => new TimelineEventViewModel
                    {
                        Year = t.Year,
                        Description = t.Description
                    }).ToList(),

                ExpertQuote = beverageContent?.ExpertQuote == null ? null : new ExpertQuoteViewModel
                {
                    Text = beverageContent.ExpertQuote.Text,
                    Source = beverageContent.ExpertQuote.Source
                }
            };

            return View("Beverage", model);
        }

        [OutputCache(PolicyName = "SymbolDetail")]
        [Route("states/{stateSlug}/license-plate/{sloganSlug}")]
        public async Task<IActionResult> LicensePlate(string stateSlug, string sloganSlug)
        {
            var state = await _stateService.GetStateBySlugAsync(stateSlug);
            if (state == null)
                return NotFound();

            var sloganSymbol = await _symbolService.GetSymbolBySlugAsync(state.Id, sloganSlug);
            if (sloganSymbol == null || sloganSymbol.Type != "license-plate")
                return NotFound();

            var allSymbols = await _symbolService.GetSymbolsByStateAsync(state.Id);
            var stateSlogans = allSymbols
                .Where(s => s.Type == "license-plate")
                .ToList();

            var licensePlateContent = await _licensePlateService.GetLicensePlateContentAsync(state.Slug, sloganSymbol.Slug);
            if (licensePlateContent == null)
            {
                _logger.LogInformation("LicensePlate YAML not found for state: {StateSlug}, slug: {SloganSlug}. Using generated fallback content.", stateSlug, sloganSlug);
                licensePlateContent = _licensePlateService.BuildFallbackContent(state, sloganSymbol, stateSlogans);
            }

            var relatedSymbols = GetRelatedSymbols(allSymbols, sloganSymbol.Id);
            var quizQuestions = BuildQuizQuestions("us-states-general-quiz");

            var model = new LicensePlateDetailViewModel
            {
                State = state,
                Symbol = sloganSymbol,
                LicensePlateContent = licensePlateContent,
                RelatedSymbols = relatedSymbols,
                QuizQuestions = quizQuestions,

                BigStat = licensePlateContent?.BigStat == null ? null : new BigStatViewModel
                {
                    Number = licensePlateContent.BigStat.Number,
                    Description = licensePlateContent.BigStat.Description
                },

                Timeline = (licensePlateContent?.Timeline == null || licensePlateContent.Timeline.Count == 0)
                    ? null
                    : licensePlateContent.Timeline.Select(t => new TimelineEventViewModel
                    {
                        Year = t.Year,
                        Description = t.Description
                    }).ToList(),

                ExpertQuote = licensePlateContent?.ExpertQuote == null ? null : new ExpertQuoteViewModel
                {
                    Text = licensePlateContent.ExpertQuote.Text,
                    Source = licensePlateContent.ExpertQuote.Source
                }
            };

            return View("LicensePlate", model);
        }

        [OutputCache(PolicyName = "SymbolDetail")]
        [Route("states/{stateSlug}/motto/{mottoSlug}")]
        public async Task<IActionResult> Motto(string stateSlug, string mottoSlug)
        {
            var state = await _stateService.GetStateBySlugAsync(stateSlug);
            if (state == null)
                return NotFound();

            var mottoSymbol = await _symbolService.GetSymbolAsync(state.Id, "motto");
            if (mottoSymbol == null)
                return NotFound();

            var redirect = RedirectToCanonicalIfNeeded(mottoSlug, mottoSymbol, state.Slug);
            if (redirect != null)
            {
                return redirect;
            }

            var mottoContent = await _mottoService.GetMottoContentAsync(stateSlug);
            if (mottoContent == null)
                return NotFound();

            var related = await GetRelatedSymbolsAsync(state.Id, mottoSymbol.Id);
            var quizQuestions = BuildQuizQuestions("us-states-general-quiz");
            var model = new MottoDetailViewModel
            {
                State = state,
                Symbol = mottoSymbol,
                MottoContent = mottoContent,
                RelatedSymbols = related,
                QuizQuestions = quizQuestions
            };

            return View(model);
        }

        [OutputCache(PolicyName = "SymbolDetail")]
        [Route("states/{stateSlug}/nickname/{nicknameSlug}")]
        public async Task<IActionResult> Nickname(string stateSlug, string nicknameSlug)
        {
            var state = await _stateService.GetStateBySlugAsync(stateSlug);
            if (state == null)
            {
                _logger.LogWarning("State not found: {StateSlug}", stateSlug);
                return NotFound();
            }

            var nicknameSymbol = await _symbolService.GetSymbolAsync(state.Id, "nickname");
            if (nicknameSymbol == null)
            {
                _logger.LogWarning("Nickname symbol not found for state: {StateSlug}, slug: {NicknameSlug}", stateSlug, nicknameSlug);
                return NotFound();
            }

            var redirect = RedirectToCanonicalIfNeeded(nicknameSlug, nicknameSymbol, state.Slug);
            if (redirect != null)
            {
                return redirect;
            }

            var nicknameContent = await _nicknameService.GetNicknameContentAsync(stateSlug);
            if (nicknameContent == null)
            {
                _logger.LogWarning("Nickname content not found for state: {StateSlug}", stateSlug);
                return NotFound();
            }

            var relatedSymbols = await GetRelatedSymbolsAsync(state.Id, nicknameSymbol.Id);
            var quizQuestions = BuildQuizQuestions("state-nicknames-quiz");
            var model = new NicknameDetailViewModel
            {
                State = state,
                Symbol = nicknameSymbol,
                NicknameContent = nicknameContent,
                RelatedSymbols = relatedSymbols,
                QuizQuestions = quizQuestions
            };

            return View(model);
        }

        [OutputCache(PolicyName = "SymbolDetail")]
        [Route("states/{stateSlug}/flower/{flowerSlug}")]
        public async Task<IActionResult> Flower(string stateSlug, string flowerSlug)
        {
            var state = await _stateService.GetStateBySlugAsync(stateSlug);
            if (state == null)
            {
                _logger.LogWarning("State not found: {StateSlug}", stateSlug);
                return NotFound();
            }

            var flowerSymbol = await _symbolService.GetSymbolAsync(state.Id, "flower");
            if (flowerSymbol == null)
            {
                _logger.LogWarning("Flower symbol not found for state: {StateSlug}", stateSlug);
                return NotFound();
            }

            var redirect = RedirectToCanonicalIfNeeded(flowerSlug, flowerSymbol, state.Slug);
            if (redirect != null)
            {
                return redirect;
            }

            var flowerContent = await _flowerService.GetFlowerContentAsync(stateSlug);

            if (flowerContent == null)
            {
                _logger.LogWarning("Flower content YAML not found for state: {StateSlug}", stateSlug);
            }
            else
            {
                _logger.LogInformation("Flower content loaded: Name={Name}, Sections={SectionCount}, FAQ={FaqCount}",
                    flowerContent.Name,
                    flowerContent.Sections?.Count ?? 0,
                    flowerContent.Faq?.Count ?? 0);
            }

            var relatedSymbols = await GetRelatedSymbolsAsync(state.Id, flowerSymbol.Id);
            var quizQuestions = BuildQuizQuestions("us-states-general-quiz");

            var model = new FlowerDetailViewModel
            {
                State = state,
                Symbol = flowerSymbol,
                FlowerContent = flowerContent,
                RelatedSymbols = relatedSymbols,
                QuizQuestions = quizQuestions,

                BigStat = flowerContent?.BigStat == null ? null : new BigStatViewModel
                {
                    Number = flowerContent.BigStat.Number,
                    Description = flowerContent.BigStat.Description
                },

                Timeline = (flowerContent?.Timeline == null || flowerContent.Timeline.Count == 0)
                    ? null
                    : flowerContent.Timeline.Select(t => new TimelineEventViewModel
                    {
                        Year = t.Year,
                        Description = t.Description
                    }).ToList(),

                ExpertQuote = flowerContent?.ExpertQuote == null ? null : new ExpertQuoteViewModel
                {
                    Text = flowerContent.ExpertQuote.Text,
                    Source = flowerContent.ExpertQuote.Source
                }
            };

            return View(model);
        }


        [OutputCache(PolicyName = "SymbolDetail")]
        [Route("states/{stateSlug}/flag/{flagSlug}")]
        public async Task<IActionResult> Flag(string stateSlug, string flagSlug)
        {
            var state = await _stateService.GetStateBySlugAsync(stateSlug);
            if (state == null)
            {
                _logger.LogWarning("State not found: {StateSlug}", stateSlug);
                return NotFound();
            }

            var flagSymbol = await _symbolService.GetSymbolAsync(state.Id, "flag");
            if (flagSymbol == null)
            {
                _logger.LogWarning("Flag symbol not found for state: {StateSlug}", stateSlug);
                return NotFound();
            }

            var redirect = RedirectToCanonicalIfNeeded(flagSlug, flagSymbol, state.Slug);
            if (redirect != null)
            {
                return redirect;
            }

            var flagContent = await _flagService.GetFlagContentAsync(stateSlug);
            if (flagContent == null)
            {
                _logger.LogWarning("Flag content YAML not found for state: {StateSlug}", stateSlug);
            }
            else
            {
                _logger.LogInformation("Flag content loaded: Name={Name}, Sections={SectionCount}, FAQ={FaqCount}",
                    flagContent.Name,
                    flagContent.Sections?.Count ?? 0,
                    flagContent.Faq?.Count ?? 0);
            }

            var relatedSymbols = await GetRelatedSymbolsAsync(state.Id, flagSymbol.Id);
            var quizQuestions = BuildQuizQuestions("state-flags-quiz");


            var model = new FlagDetailViewModel
            {
                State = state,
                Symbol = flagSymbol,
                FlagContent = flagContent,
                RelatedSymbols = relatedSymbols,
                QuizQuestions = quizQuestions,

                BigStat = flagContent?.BigStat == null ? null : new BigStatViewModel
                {
                    Number = flagContent.BigStat.Number,
                    Description = flagContent.BigStat.Description
                },

                Timeline = (flagContent?.Timeline == null || flagContent.Timeline.Count == 0)
                    ? null
                    : flagContent.Timeline.Select(t => new TimelineEventViewModel
                    {
                        Year = t.Year,
                        Description = t.Description
                    }).ToList(),

                ExpertQuote = flagContent?.ExpertQuote == null ? null : new ExpertQuoteViewModel
                {
                    Text = flagContent.ExpertQuote.Text,
                    Source = flagContent.ExpertQuote.Source
                }
            };

            return View(model);
        }

        [OutputCache(PolicyName = "SymbolDetail")]
        [Route("states/{stateSlug}/tree/{treeSlug}")]
        public async Task<IActionResult> Tree(string stateSlug, string treeSlug)
        {
            var state = await _stateService.GetStateBySlugAsync(stateSlug);
            if (state == null)
            {
                _logger.LogWarning("State not found: {StateSlug}", stateSlug);
                return NotFound();
            }

            var treeSymbol = await _symbolService.GetSymbolAsync(state.Id, "tree");
            if (treeSymbol == null)
            {
                _logger.LogWarning("Tree symbol not found for state: {StateSlug}", stateSlug);
                return NotFound();
            }

            var redirect = RedirectToCanonicalIfNeeded(treeSlug, treeSymbol, state.Slug);
            if (redirect != null)
            {
                return redirect;
            }

            var treeContent = await _treeService.GetTreeContentAsync(stateSlug);
            if (treeContent == null)
            {
                _logger.LogWarning("Tree content YAML not found for state: {StateSlug}", stateSlug);
            }
            else
            {
                _logger.LogInformation("Tree content loaded: Name={Name}, Sections={SectionCount}, FAQ={FaqCount}",
                    treeContent.Name,
                    treeContent.Sections?.Count ?? 0,
                    treeContent.Faq?.Count ?? 0);
            }

            var quizQuestions = BuildQuizQuestions("us-states-general-quiz");

            var relatedSymbols = await GetRelatedSymbolsAsync(state.Id, treeSymbol.Id);

            var model = new TreeDetailViewModel
            {
                State = state,
                Symbol = treeSymbol,
                TreeContent = treeContent,
                RelatedSymbols = relatedSymbols,
                QuizQuestions = quizQuestions,

                BigStat = treeContent?.BigStat == null ? null : new BigStatViewModel
                {
                    Number = treeContent.BigStat.Number,
                    Description = treeContent.BigStat.Description
                },

                Timeline = (treeContent?.Timeline == null || treeContent.Timeline.Count == 0)
                    ? null
                    : treeContent.Timeline.Select(t => new TimelineEventViewModel
                    {
                        Year = t.Year,
                        Description = t.Description
                    }).ToList(),

                ExpertQuote = treeContent?.ExpertQuote == null ? null : new ExpertQuoteViewModel
                {
                    Text = treeContent.ExpertQuote.Text,
                    Source = treeContent.ExpertQuote.Source
                }
            };

            return View(model);
        }

        [OutputCache(PolicyName = "SymbolDetail")]
        [Route("states/{stateSlug}/color/{colorSlug}")]
        public async Task<IActionResult> Color(string stateSlug, string colorSlug)
        {
            var state = await _stateService.GetStateBySlugAsync(stateSlug);
            if (state == null)
            {
                _logger.LogWarning("State not found: {StateSlug}", stateSlug);
                return NotFound();
            }

            var colorSymbol = await _symbolService.GetSymbolAsync(state.Id, "color");
            if (colorSymbol == null)
            {
                _logger.LogWarning("Color symbol not found for state: {StateSlug}", stateSlug);
                return NotFound();
            }

            var redirect = RedirectToCanonicalIfNeeded(colorSlug, colorSymbol, state.Slug);
            if (redirect != null)
            {
                return redirect;
            }

            var colorContent = await _colorService.GetColorContentAsync(stateSlug, colorSymbol.Slug);
            if (colorContent == null)
            {
                _logger.LogWarning("Color content YAML not found: state={StateSlug}, slug={ColorSlug}", stateSlug, colorSlug);
            }
            else
            {
                _logger.LogInformation("Color content loaded: Title={Title}, Sections={SectionCount}, FAQ={FaqCount}",
                    colorContent.Title,
                    colorContent.Sections?.Count ?? 0,
                    colorContent.Faq?.Count ?? 0);
            }

            var relatedSymbols = await GetRelatedSymbolsAsync(state.Id, colorSymbol.Id);
            var quizQuestions = BuildQuizQuestions("us-states-general-quiz");
            var model = new ColorDetailViewModel
            {
                State = state,
                Symbol = colorSymbol,
                ColorContent = colorContent,
                RelatedSymbols = relatedSymbols,
                QuizQuestions = quizQuestions,

                BigStat = colorContent?.BigStat == null ? null : new BigStatViewModel
                {
                    Number = colorContent.BigStat.Number,
                    Description = colorContent.BigStat.Description
                },

                Timeline = (colorContent?.Timeline == null || colorContent.Timeline.Count == 0)
                    ? null
                    : colorContent.Timeline.Select(t => new TimelineEventViewModel
                    {
                        Year = t.Year,
                        Description = t.Description
                    }).ToList(),

                ExpertQuote = colorContent?.ExpertQuote == null ? null : new ExpertQuoteViewModel
                {
                    Text = colorContent.ExpertQuote.Text,
                    Source = colorContent.ExpertQuote.Source
                }
            };

            return View(model);
        }

        [OutputCache(PolicyName = "SymbolDetail")]
        [Route("states/{stateSlug}/state-seal/{sealSlug}")]
        public async Task<IActionResult> StateSeal(string stateSlug, string sealSlug)
        {
            var state = await _stateService.GetStateBySlugAsync(stateSlug);
            if (state == null)
            {
                _logger.LogWarning("State not found: {StateSlug}", stateSlug);
                return NotFound();
            }

            var sealSymbol = await _symbolService.GetSymbolAsync(state.Id, "state-seal");
            if (sealSymbol == null)
            {
                _logger.LogWarning("State seal symbol not found for state: {StateSlug}", stateSlug);
                return NotFound();
            }

            var redirect = RedirectToCanonicalIfNeeded(sealSlug, sealSymbol, state.Slug);
            if (redirect != null)
                return redirect;

            var sealContent = await _sealService.GetSealContentAsync(stateSlug);
            if (sealContent == null)
                _logger.LogInformation("State seal YAML not found for state: {StateSlug}", stateSlug);
            else
                _logger.LogInformation("State seal content loaded: Name={Name}, Sections={SectionCount}, FAQ={FaqCount}",
                    sealContent.Name, sealContent.Sections?.Count ?? 0, sealContent.Faq?.Count ?? 0);

            var relatedSymbols = await GetRelatedSymbolsAsync(state.Id, sealSymbol.Id);
            var quizQuestions = BuildQuizQuestions("us-states-general-quiz");

            var model = new SealDetailViewModel
            {
                State = state,
                Symbol = sealSymbol,
                SealContent = sealContent,
                RelatedSymbols = relatedSymbols,
                QuizQuestions = quizQuestions,

                BigStat = sealContent?.BigStat == null ? null : new BigStatViewModel
                {
                    Number = sealContent.BigStat.Number,
                    Description = sealContent.BigStat.Description
                },

                Timeline = (sealContent?.Timeline == null || sealContent.Timeline.Count == 0)
                    ? null
                    : sealContent.Timeline.Select(t => new TimelineEventViewModel
                    {
                        Year = t.Year,
                        Description = t.Description
                    }).ToList(),

                ExpertQuote = sealContent?.ExpertQuote == null ? null : new ExpertQuoteViewModel
                {
                    Text = sealContent.ExpertQuote.Text,
                    Source = sealContent.ExpertQuote.Source
                }
            };

            return View("Seal", model);
        }

        [OutputCache(PolicyName = "SymbolDetail")]
        [Route("states/{stateSlug}/coat-of-arms/{coatOfArmsSlug}")]
        public async Task<IActionResult> CoatOfArms(string stateSlug, string coatOfArmsSlug)
        {
            var state = await _stateService.GetStateBySlugAsync(stateSlug);
            if (state == null)
            {
                _logger.LogWarning("State not found: {StateSlug}", stateSlug);
                return NotFound();
            }

            var symbol = await _symbolService.GetSymbolAsync(state.Id, "coat-of-arms");
            if (symbol == null)
            {
                _logger.LogWarning("State coat of arms symbol not found for state: {StateSlug}", stateSlug);
                return NotFound();
            }

            var redirect = RedirectToCanonicalIfNeeded(coatOfArmsSlug, symbol, state.Slug);
            if (redirect != null)
                return redirect;

            var content = await _sealService.GetSealContentAsync(stateSlug, "coat-of-arms.yaml");
            if (content == null)
                _logger.LogInformation("State coat of arms YAML not found for state: {StateSlug}", stateSlug);
            else
                _logger.LogInformation("State coat of arms content loaded: Name={Name}, Sections={SectionCount}, FAQ={FaqCount}",
                    content.Name, content.Sections?.Count ?? 0, content.Faq?.Count ?? 0);

            var relatedSymbols = await GetRelatedSymbolsAsync(state.Id, symbol.Id);
            var quizQuestions = BuildQuizQuestions("us-states-general-quiz");

            var model = new SealDetailViewModel
            {
                State = state,
                Symbol = symbol,
                SealContent = content,
                RelatedSymbols = relatedSymbols,
                QuizQuestions = quizQuestions,
                SymbolTypeName = "Coat of Arms",
                SymbolTypeSlug = "coat-of-arms",
                SymbolTypePlural = "coats-of-arms",
                SymbolTypeIcon = "🛡️",
                DefaultDesignation = "Coat of Arms",
                HeroFallbackIconClass = "fa-solid fa-shield-halved",
                OverviewIconClass = "fa-solid fa-shield-halved",
                AssetBasePath = "/images/coats-of-arms",
                EmptySectionsMessage = "No sections rendered yet for this state coat of arms.",
                ShowQuizPromo = false,

                BigStat = content?.BigStat == null ? null : new BigStatViewModel
                {
                    Number = content.BigStat.Number,
                    Description = content.BigStat.Description
                },

                Timeline = (content?.Timeline == null || content.Timeline.Count == 0)
                    ? null
                    : content.Timeline.Select(t => new TimelineEventViewModel
                    {
                        Year = t.Year,
                        Description = t.Description
                    }).ToList(),

                ExpertQuote = content?.ExpertQuote == null ? null : new ExpertQuoteViewModel
                {
                    Text = content.ExpertQuote.Text,
                    Source = content.ExpertQuote.Source
                }
            };

            return View("Seal", model);
        }

        [OutputCache(PolicyName = "SymbolDetail")]
        [Route("states/{stateSlug}/soil/{soilSlug}")]
        public async Task<IActionResult> StateSoil(string stateSlug, string soilSlug)
        {
            var state = await _stateService.GetStateBySlugAsync(stateSlug);
            if (state == null)
            {
                _logger.LogWarning("State not found: {StateSlug}", stateSlug);
                return NotFound();
            }

            var symbol = await _symbolService.GetSymbolAsync(state.Id, "soil");
            if (symbol == null)
            {
                _logger.LogWarning("State soil symbol not found for state: {StateSlug}", stateSlug);
                return NotFound();
            }

            var redirect = RedirectToCanonicalIfNeeded(soilSlug, symbol, state.Slug);
            if (redirect != null)
                return redirect;

            var content = await _soilService.GetSoilContentAsync(stateSlug);
            if (content == null)
                _logger.LogInformation("State soil YAML not found for state: {StateSlug}", stateSlug);
            else
                _logger.LogInformation("State soil content loaded: Name={Name}, Sections={SectionCount}", content.Name, content.Sections?.Count ?? 0);

            var relatedSymbols = await GetRelatedSymbolsAsync(state.Id, symbol.Id);
            var quizQuestions = BuildQuizQuestions("us-states-general-quiz");

            var model = new SoilDetailViewModel
            {
                State = state,
                Symbol = symbol,
                SoilContent = content,
                RelatedSymbols = relatedSymbols,
                QuizQuestions = quizQuestions,

                BigStat = content?.BigStat == null ? null : new BigStatViewModel
                {
                    Number = content.BigStat.Number,
                    Description = content.BigStat.Description
                },

                ExpertQuote = content?.ExpertQuote == null ? null : new ExpertQuoteViewModel
                {
                    Text = content.ExpertQuote.Text,
                    Source = content.ExpertQuote.Source
                }
            };

            return View("Soil", model);
        }

        [OutputCache(PolicyName = "SymbolDetail")]
        [Route("states/{stateSlug}/fossil/{fossilSlug}")]
        public async Task<IActionResult> StateFossil(string stateSlug, string fossilSlug)
        {
            var state = await _stateService.GetStateBySlugAsync(stateSlug);
            if (state == null)
            {
                _logger.LogWarning("State not found: {StateSlug}", stateSlug);
                return NotFound();
            }

            // Try by slug first to correctly handle multi-fossil states (Ohio, Kansas, Vermont)
            var symbol = await _symbolService.GetSymbolBySlugAsync(state.Id, fossilSlug)
                         ?? await _symbolService.GetSymbolAsync(state.Id, "fossil");
            if (symbol == null)
            {
                _logger.LogWarning("State fossil symbol not found for state: {StateSlug}", stateSlug);
                return NotFound();
            }

            var redirect = RedirectToCanonicalIfNeeded(fossilSlug, symbol, state.Slug);
            if (redirect != null)
                return redirect;

            var yamlFileName = string.IsNullOrWhiteSpace(symbol.YamlPath)
                ? "fossil.yaml"
                : Path.GetFileName(symbol.YamlPath);
            var content = await _fossilService.GetFossilContentAsync(stateSlug, yamlFileName);
            if (content == null)
                _logger.LogInformation("State fossil YAML not found for state: {StateSlug}", stateSlug);
            else
                _logger.LogInformation("State fossil content loaded: Name={Name}, Sections={SectionCount}", content.Name, content.Sections?.Count ?? 0);

            var relatedSymbols = await GetRelatedSymbolsAsync(state.Id, symbol.Id);
            var quizQuestions = BuildQuizQuestions("us-states-general-quiz");

            var model = new FossilDetailViewModel
            {
                State = state,
                Symbol = symbol,
                FossilContent = content,
                RelatedSymbols = relatedSymbols,
                QuizQuestions = quizQuestions
            };

            return View("Fossil", model);
        }

        [OutputCache(PolicyName = "SymbolDetail")]
        [Route("states/{stateSlug}/sport/{sportSlug}")]
        public async Task<IActionResult> StateSport(string stateSlug, string sportSlug)
        {
            var state = await _stateService.GetStateBySlugAsync(stateSlug);
            if (state == null)
            {
                _logger.LogWarning("State not found: {StateSlug}", stateSlug);
                return NotFound();
            }

            var symbol = await _symbolService.GetSymbolAsync(state.Id, "sport");
            if (symbol == null)
            {
                _logger.LogWarning("State sport symbol not found for state: {StateSlug}", stateSlug);
                return NotFound();
            }

            var redirect = RedirectToCanonicalIfNeeded(sportSlug, symbol, state.Slug);
            if (redirect != null)
                return redirect;

            var content = await _sportService.GetSportContentAsync(stateSlug, sportSlug);
            if (content == null)
                _logger.LogInformation("State sport YAML not found for state: {StateSlug}", stateSlug);
            else
                _logger.LogInformation("State sport content loaded: Name={Name}, Sections={SectionCount}", content.Name, content.Sections?.Count ?? 0);

            var relatedSymbols = await GetRelatedSymbolsAsync(state.Id, symbol.Id);
            var quizQuestions = BuildQuizQuestions("us-states-general-quiz");

            var model = new SportDetailViewModel
            {
                State = state,
                Symbol = symbol,
                SportContent = content,
                RelatedSymbols = relatedSymbols,
                QuizQuestions = quizQuestions
            };

            return View("Sport", model);
        }

        [OutputCache(PolicyName = "SymbolDetail")]
        [Route("states/{stateSlug}/dance/{danceSlug}")]
        public async Task<IActionResult> StateDance(string stateSlug, string danceSlug)
        {
            var state = await _stateService.GetStateBySlugAsync(stateSlug);
            if (state == null)
            {
                _logger.LogWarning("State not found: {StateSlug}", stateSlug);
                return NotFound();
            }

            var symbol = await _symbolService.GetSymbolAsync(state.Id, "dance");
            if (symbol == null)
            {
                _logger.LogWarning("State dance symbol not found for state: {StateSlug}", stateSlug);
                return NotFound();
            }

            var redirect = RedirectToCanonicalIfNeeded(danceSlug, symbol, state.Slug);
            if (redirect != null)
                return redirect;

            var content = await _danceService.GetDanceContentAsync(stateSlug, danceSlug);
            if (content == null)
                _logger.LogInformation("State dance YAML not found for state: {StateSlug}", stateSlug);
            else
                _logger.LogInformation("State dance content loaded: Name={Name}, Sections={SectionCount}", content.Name, content.Sections?.Count ?? 0);

            var relatedSymbols = await GetRelatedSymbolsAsync(state.Id, symbol.Id);
            var quizQuestions = BuildQuizQuestions("us-states-general-quiz");

            var model = new DanceDetailViewModel
            {
                State = state,
                Symbol = symbol,
                DanceContent = content,
                RelatedSymbols = relatedSymbols,
                QuizQuestions = quizQuestions
            };

            return View("Dance", model);
        }

        [OutputCache(PolicyName = "SymbolDetail")]
        [Route("states/{stateSlug}/insect/{insectSlug}")]
        public async Task<IActionResult> StateInsect(string stateSlug, string insectSlug)
        {
            var state = await _stateService.GetStateBySlugAsync(stateSlug);
            if (state == null)
            {
                _logger.LogWarning("State not found: {StateSlug}", stateSlug);
                return NotFound();
            }

            // Try by slug first to correctly handle multi-insect states (Alabama, Delaware, Tennessee, etc.)
            var symbol = await _symbolService.GetSymbolBySlugAsync(state.Id, insectSlug)
                         ?? await _symbolService.GetSymbolAsync(state.Id, "insect");
            if (symbol == null)
            {
                _logger.LogWarning("State insect symbol not found for state: {StateSlug}", stateSlug);
                return NotFound();
            }

            var redirect = RedirectToCanonicalIfNeeded(insectSlug, symbol, state.Slug);
            if (redirect != null)
                return redirect;

            var yamlFileName = string.IsNullOrWhiteSpace(symbol.YamlPath)
                ? "insect.yaml"
                : Path.GetFileName(symbol.YamlPath);
            var content = await _insectService.GetInsectContentAsync(stateSlug, yamlFileName);
            if (content == null)
                _logger.LogInformation("State insect YAML not found for state: {StateSlug}", stateSlug);
            else
                _logger.LogInformation("State insect content loaded: Name={Name}, Sections={SectionCount}", content.Name, content.Sections?.Count ?? 0);

            var relatedSymbols = await GetRelatedSymbolsAsync(state.Id, symbol.Id);
            var quizQuestions = BuildQuizQuestions("us-states-general-quiz");

            var model = new InsectDetailViewModel
            {
                State = state,
                Symbol = symbol,
                InsectContent = content,
                RelatedSymbols = relatedSymbols,
                QuizQuestions = quizQuestions
            };

            return View("Insect", model);
        }

        [OutputCache(PolicyName = "SymbolDetail")]
        [Route("states/{stateSlug}/mineral/{mineralSlug}")]
        public async Task<IActionResult> StateMineral(string stateSlug, string mineralSlug)
        {
            var state = await _stateService.GetStateBySlugAsync(stateSlug);
            if (state == null)
            {
                _logger.LogWarning("State not found: {StateSlug}", stateSlug);
                return NotFound();
            }

            var symbol = await _symbolService.GetSymbolAsync(state.Id, "mineral");
            if (symbol == null)
            {
                _logger.LogWarning("State mineral symbol not found for state: {StateSlug}", stateSlug);
                return NotFound();
            }

            var redirect = RedirectToCanonicalIfNeeded(mineralSlug, symbol, state.Slug);
            if (redirect != null)
                return redirect;

            var content = await _mineralService.GetMineralContentAsync(stateSlug, "mineral.yaml");
            if (content == null)
                _logger.LogInformation("State mineral YAML not found for state: {StateSlug}", stateSlug);
            else
                _logger.LogInformation("State mineral content loaded: Name={Name}, Sections={SectionCount}", content.Name, content.Sections?.Count ?? 0);

            var relatedSymbols = await GetRelatedSymbolsAsync(state.Id, symbol.Id);
            var quizQuestions = BuildQuizQuestions("us-states-general-quiz");

            var model = new MineralDetailViewModel
            {
                State = state,
                Symbol = symbol,
                MineralContent = content,
                RelatedSymbols = relatedSymbols,
                QuizQuestions = quizQuestions
            };

            return View("Mineral", model);
        }

        [OutputCache(PolicyName = "SymbolDetail")]
        [Route("states/{stateSlug}/rock/{rockSlug}")]
        public async Task<IActionResult> StateRock(string stateSlug, string rockSlug)
        {
            var state = await _stateService.GetStateBySlugAsync(stateSlug);
            if (state == null)
            {
                _logger.LogWarning("State not found: {StateSlug}", stateSlug);
                return NotFound();
            }

            var symbol = await _symbolService.GetSymbolAsync(state.Id, "rock");
            if (symbol == null)
            {
                _logger.LogWarning("State rock symbol not found for state: {StateSlug}", stateSlug);
                return NotFound();
            }

            var redirect = RedirectToCanonicalIfNeeded(rockSlug, symbol, state.Slug);
            if (redirect != null)
                return redirect;

            var content = await _mineralService.GetMineralContentAsync(stateSlug, "rock.yaml");
            if (content == null)
                _logger.LogInformation("State rock YAML not found for state: {StateSlug}", stateSlug);
            else
                _logger.LogInformation("State rock content loaded: Name={Name}, Sections={SectionCount}", content.Name, content.Sections?.Count ?? 0);

            var relatedSymbols = await GetRelatedSymbolsAsync(state.Id, symbol.Id);
            var quizQuestions = BuildQuizQuestions("us-states-general-quiz");

            var model = new MineralDetailViewModel
            {
                State = state,
                Symbol = symbol,
                MineralContent = content,
                RelatedSymbols = relatedSymbols,
                QuizQuestions = quizQuestions,
                SymbolTypeName = "State Rock",
                SymbolTypeSlug = "rock",
                SymbolTypePlural = "rocks",
                SymbolTypeIcon = "🪨",
                DefaultDesignation = "State Rock",
                HeroFallbackIconClass = "fa-solid fa-mountain",
                OverviewIconClass = "fa-solid fa-mountain",
                AssetBasePath = "/images/rocks",
                EmptySectionsMessage = "No sections rendered yet for this state rock."
            };

            return View("Mineral", model);
        }

        [OutputCache(PolicyName = "SymbolDetail")]
        [Route("states/{stateSlug}/gemstone/{gemstoneSlug}")]
        public async Task<IActionResult> StateGemstone(string stateSlug, string gemstoneSlug)
        {
            var state = await _stateService.GetStateBySlugAsync(stateSlug);
            if (state == null)
            {
                _logger.LogWarning("State not found: {StateSlug}", stateSlug);
                return NotFound();
            }

            var symbol = await _symbolService.GetSymbolAsync(state.Id, "gemstone");
            if (symbol == null)
            {
                _logger.LogWarning("State gemstone symbol not found for state: {StateSlug}", stateSlug);
                return NotFound();
            }

            var redirect = RedirectToCanonicalIfNeeded(gemstoneSlug, symbol, state.Slug);
            if (redirect != null)
                return redirect;

            var content = await _mineralService.GetMineralContentAsync(stateSlug, "gemstone.yaml");
            if (content == null)
                _logger.LogInformation("State gemstone YAML not found for state: {StateSlug}", stateSlug);
            else
                _logger.LogInformation("State gemstone content loaded: Name={Name}, Sections={SectionCount}", content.Name, content.Sections?.Count ?? 0);

            var relatedSymbols = await GetRelatedSymbolsAsync(state.Id, symbol.Id);
            var quizQuestions = BuildQuizQuestions("us-states-general-quiz");

            var model = new MineralDetailViewModel
            {
                State = state,
                Symbol = symbol,
                MineralContent = content,
                RelatedSymbols = relatedSymbols,
                QuizQuestions = quizQuestions,
                SymbolTypeName = "State Gemstone",
                SymbolTypeSlug = "gemstone",
                SymbolTypePlural = "gemstones",
                SymbolTypeIcon = "💎",
                DefaultDesignation = "State Gemstone",
                HeroFallbackIconClass = "fa-solid fa-gem",
                OverviewIconClass = "fa-solid fa-gem",
                AssetBasePath = "/images/gemstones",
                EmptySectionsMessage = "No sections rendered yet for this state gemstone."
            };

            return View("Mineral", model);
        }

        [OutputCache(PolicyName = "SymbolDetail")]
        [Route("states/{stateSlug}/amphibian/{amphibianSlug}")]
        public async Task<IActionResult> StateAmphibian(string stateSlug, string amphibianSlug)
        {
            var state = await _stateService.GetStateBySlugAsync(stateSlug);
            if (state == null)
            {
                _logger.LogWarning("State not found: {StateSlug}", stateSlug);
                return NotFound();
            }

            // Try by slug first to correctly handle multi-amphibian states
            var symbol = await _symbolService.GetSymbolBySlugAsync(state.Id, amphibianSlug)
                         ?? await _symbolService.GetSymbolAsync(state.Id, "amphibian");
            if (symbol == null)
            {
                _logger.LogWarning("State amphibian symbol not found for state: {StateSlug}", stateSlug);
                return NotFound();
            }

            var redirect = RedirectToCanonicalIfNeeded(amphibianSlug, symbol, state.Slug);
            if (redirect != null)
                return redirect;

            var yamlFileName = string.IsNullOrWhiteSpace(symbol.YamlPath)
                ? "amphibian.yaml"
                : Path.GetFileName(symbol.YamlPath);
            var content = await _amphibianService.GetAmphibianContentAsync(stateSlug, yamlFileName);
            if (content == null)
                _logger.LogInformation("State amphibian YAML not found for state: {StateSlug}", stateSlug);
            else
                _logger.LogInformation("State amphibian content loaded: Name={Name}, Sections={SectionCount}", content.Name, content.Sections?.Count ?? 0);

            var relatedSymbols = await GetRelatedSymbolsAsync(state.Id, symbol.Id);
            var quizQuestions = BuildQuizQuestions("us-states-general-quiz");

            var model = new AmphibianDetailViewModel
            {
                State = state,
                Symbol = symbol,
                AmphibianContent = content,
                RelatedSymbols = relatedSymbols,
                QuizQuestions = quizQuestions
            };

            return View("Amphibian", model);
        }

        [OutputCache(PolicyName = "SymbolDetail")]
        [Route("states/{stateSlug}/reptile/{reptileSlug}")]
        public async Task<IActionResult> StateReptile(string stateSlug, string reptileSlug)
        {
            var state = await _stateService.GetStateBySlugAsync(stateSlug);
            if (state == null)
            {
                _logger.LogWarning("State not found: {StateSlug}", stateSlug);
                return NotFound();
            }

            // Try by slug first to correctly handle multi-reptile states
            var symbol = await _symbolService.GetSymbolBySlugAsync(state.Id, reptileSlug)
                         ?? await _symbolService.GetSymbolAsync(state.Id, "reptile");
            if (symbol == null)
            {
                _logger.LogWarning("State reptile symbol not found for state: {StateSlug}", stateSlug);
                return NotFound();
            }

            var redirect = RedirectToCanonicalIfNeeded(reptileSlug, symbol, state.Slug);
            if (redirect != null)
                return redirect;

            var yamlFileName = string.IsNullOrWhiteSpace(symbol.YamlPath)
                ? "reptile.yaml"
                : Path.GetFileName(symbol.YamlPath);
            var content = await _reptileService.GetReptileContentAsync(stateSlug, yamlFileName);
            if (content == null)
                _logger.LogInformation("State reptile YAML not found for state: {StateSlug}", stateSlug);
            else
                _logger.LogInformation("State reptile content loaded: Name={Name}, Sections={SectionCount}", content.Name, content.Sections?.Count ?? 0);

            var relatedSymbols = await GetRelatedSymbolsAsync(state.Id, symbol.Id);
            var quizQuestions = BuildQuizQuestions("us-states-general-quiz");

            var model = new ReptileDetailViewModel
            {
                State = state,
                Symbol = symbol,
                ReptileContent = content,
                RelatedSymbols = relatedSymbols,
                QuizQuestions = quizQuestions
            };

            return View("Reptile", model);
        }

        private static readonly Dictionary<string, (string Name, string Plural, string Icon, string FaIcon)> FoodBucketInfo = new()
        {
            ["fruits"] = ("State Fruit", "fruits", "🍎", "fa-solid fa-apple-whole"),
            ["vegetables"] = ("State Vegetable", "vegetables", "🥕", "fa-solid fa-carrot"),
            ["nuts"] = ("State Nut", "nuts", "🥜", "fa-solid fa-seedling"),
            ["desserts"] = ("State Dessert", "desserts", "🍪", "fa-solid fa-cookie-bite"),
            ["spirits"] = ("State Drink", "spirits", "🥃", "fa-solid fa-wine-bottle"),
            ["dishes"] = ("State Dish", "dishes", "🍲", "fa-solid fa-bowl-food"),
            ["crops"] = ("State Crop", "crops", "🌾", "fa-solid fa-wheat-awn")
        };

        [OutputCache(PolicyName = "SymbolDetail")]
        [Route("states/{stateSlug}/food/{foodSlug}")]
        public async Task<IActionResult> StateFood(string stateSlug, string foodSlug)
        {
            var state = await _stateService.GetStateBySlugAsync(stateSlug);
            if (state == null)
            {
                _logger.LogWarning("State not found: {StateSlug}", stateSlug);
                return NotFound();
            }

            // Try by slug first to correctly handle multi-food states (most states have several)
            var symbol = await _symbolService.GetSymbolBySlugAsync(state.Id, foodSlug)
                         ?? await _symbolService.GetSymbolAsync(state.Id, "food");
            if (symbol == null)
            {
                _logger.LogWarning("State food symbol not found for state: {StateSlug}", stateSlug);
                return NotFound();
            }

            var redirect = RedirectToCanonicalIfNeeded(foodSlug, symbol, state.Slug);
            if (redirect != null)
                return redirect;

            var yamlFileName = string.IsNullOrWhiteSpace(symbol.YamlPath)
                ? "food.yaml"
                : Path.GetFileName(symbol.YamlPath);
            var content = await _foodService.GetFoodContentAsync(stateSlug, yamlFileName);
            if (content == null)
                _logger.LogInformation("State food YAML not found for state: {StateSlug}", stateSlug);
            else
                _logger.LogInformation("State food content loaded: Name={Name}, Sections={SectionCount}", content.Name, content.Sections?.Count ?? 0);

            var relatedSymbols = await GetRelatedSymbolsAsync(state.Id, symbol.Id);
            var quizQuestions = BuildQuizQuestions("us-states-general-quiz");

            var bucket = GetFoodBucket(content?.Designation ?? symbol.Designation ?? "");
            var bucketInfo = FoodBucketInfo.TryGetValue(bucket, out var info) ? info : FoodBucketInfo["crops"];

            var model = new FoodDetailViewModel
            {
                State = state,
                Symbol = symbol,
                FoodContent = content,
                RelatedSymbols = relatedSymbols,
                QuizQuestions = quizQuestions,
                SymbolTypeName = bucketInfo.Name,
                SymbolTypeSlug = bucket,
                SymbolTypePlural = bucketInfo.Plural,
                SymbolTypeIcon = bucketInfo.Icon,
                DefaultDesignation = bucketInfo.Name,
                HeroFallbackIconClass = bucketInfo.FaIcon,
                OverviewIconClass = bucketInfo.FaIcon,
                AssetBasePath = "/images/foods",
                EmptySectionsMessage = "No sections rendered yet for this state food."
            };

            return View("Food", model);
        }

        [Route("states/{stateSlug}/{symbolType}/{symbolSlug}")]
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any, VaryByHeader = "Accept-Encoding")]
        public async Task<IActionResult> LegacyDetail(string stateSlug, string symbolType, string symbolSlug)
        {
            var state = await _stateService.GetStateBySlugAsync(stateSlug);
            if (state == null)
            {
                return NotFound();
            }

            var symbol = await _symbolCanonicalService.ResolveCanonicalSymbolAsync(state, symbolType);
            if (symbol == null)
            {
                return NotFound();
            }

            return RedirectPermanent(symbol.ToSymbolUrl(state.Slug));
        }

        [Route("states/{stateSlug}/{symbolType}")]
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any, VaryByHeader = "Accept-Encoding")]
        public async Task<IActionResult> Detail(string stateSlug, string symbolType)
        {
            var state = await _stateService.GetStateBySlugAsync(stateSlug);
            if (state == null)
            {
                return NotFound();
            }

            var symbol = await _symbolCanonicalService.ResolveCanonicalSymbolAsync(state, symbolType);
            if (symbol == null)
            {
                return NotFound();
            }

            return RedirectPermanent(symbol.ToSymbolUrl(state.Slug));
        }

        private string? GetLegacyStateSlug()
        {
            var raw = Request.Query["stateSlug"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(raw))
            {
                raw = Request.Query["stateslug"].FirstOrDefault();
            }

            return raw?.Trim().ToLowerInvariant();
        }

        private IActionResult? RedirectToCanonicalIfNeeded(string? requestedSlug, USASymbol.Models.Symbol symbol, string stateSlug)
        {
            return string.Equals((requestedSlug ?? string.Empty).Trim(), symbol.Slug, StringComparison.OrdinalIgnoreCase)
                ? null
                : RedirectPermanent(symbol.ToSymbolUrl(stateSlug));
        }

        private async Task<List<USASymbol.Models.Symbol>> GetRelatedSymbolsAsync(int stateId, int currentSymbolId, int take = 6)
        {
            var allSymbols = await _symbolService.GetSymbolsByStateAsync(stateId);
            return GetRelatedSymbols(allSymbols, currentSymbolId, take);
        }

        private static List<USASymbol.Models.Symbol> GetRelatedSymbols(IEnumerable<USASymbol.Models.Symbol> symbols, int currentSymbolId, int take = 6)
        {
            return symbols
                .Where(s => s.Id != currentSymbolId)
                .Take(take)
                .ToList();
        }

        private List<QuizQuestion> BuildQuizQuestions(string quizSlug, int take = 10)
        {
            var questions = _quizService.GetBySlug(quizSlug)?.Questions;
            if (questions == null || questions.Count == 0 || take <= 0)
                return new List<QuizQuestion>();

            var pool = questions.ToList();
            var count = Math.Min(take, pool.Count);

            for (var index = 0; index < count; index++)
            {
                var swapIndex = Random.Shared.Next(index, pool.Count);
                (pool[index], pool[swapIndex]) = (pool[swapIndex], pool[index]);
            }

            return pool.Take(count).ToList();
        }
    }
}
