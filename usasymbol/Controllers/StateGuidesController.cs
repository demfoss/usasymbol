using Microsoft.AspNetCore.Mvc;
using usasymbol.Models;
using usasymbol.Services;
using usasymbol.Services.Interface;
using USASymbol.Models.ViewModels;
using USASymbol.Services;
using USASymbol.Models;
using USASymbol.Models.Content;
using USASymbol.Services.Interface;

namespace USASymbol.Controllers
{
    public class StateGuidesController : Controller
    {
        private readonly IStateService _stateService;
        private readonly ISymbolService _symbolService;
        private readonly IBorderService _borderService;
        private readonly IStateAbbreviationContentService _stateAbbreviationContentService;
        private readonly ISurnamesService _surnamesService;
        private readonly ILatestContentRailService _latestContentRailService;
        private readonly IWebHostEnvironment _env;
        private readonly QuizService _quizService;
        private readonly ILogger<StateGuidesController> _logger;

        public StateGuidesController(
            IStateService stateService,
            ISymbolService symbolService,
            IBorderService borderService,
            IStateAbbreviationContentService stateAbbreviationContentService,
            ISurnamesService surnamesService,
            ILatestContentRailService latestContentRailService,
            IWebHostEnvironment env,
            QuizService quizService,
            ILogger<StateGuidesController> logger)
        {
            _stateService = stateService;
            _symbolService = symbolService;
            _borderService = borderService;
            _stateAbbreviationContentService = stateAbbreviationContentService;
            _surnamesService = surnamesService;
            _latestContentRailService = latestContentRailService;
            _env = env;
            _quizService = quizService;
            _logger = logger;
        }

        [Route("states/{stateSlug}/surnames")]
        public async Task<IActionResult> Surnames(string stateSlug)
        {
            var state = await _stateService.GetStateBySlugAsync(stateSlug);
            if (state == null) return NotFound();

            var content = await _surnamesService.GetSurnamesContentAsync(stateSlug);
            if (content == null) return NotFound();

            ViewData["Title"] = content.SeoTitle ?? $"Most Common Last Names in {state.Name}";
            ViewData["Description"] = content.SeoDescription;
            ViewData["OgImage"] = state.FlagImageUrl;
            ViewData["Canonical"] = $"/states/{stateSlug}/surnames";
            ViewData["AllStates"] = await _stateService.GetAllStatesAsync();
            ViewData["LatestContentRail"] = await _latestContentRailService.GetLatestItemsAsync(8);

            var surnamesPath = Path.Combine(_env.ContentRootPath, "Content", "states", stateSlug, "surnames.yaml");
            if (System.IO.File.Exists(surnamesPath))
            {
                ViewData["AuthorBox"] = new AuthorBox
                {
                    DateModified = System.IO.File.GetLastWriteTimeUtc(surnamesPath)
                };
            }

            return View(new SurnamesViewModel { State = state, Content = content });
        }

        [Route("states/{stateSlug}/borders")]
        public async Task<IActionResult> Borders(string stateSlug)
        {
            var state = await _stateService.GetStateBySlugAsync(stateSlug);
            if (state == null)
            {
                _logger.LogWarning("State not found: {StateSlug}", stateSlug);
                return NotFound();
            }

            var borderContent = await _borderService.GetBorderContentAsync(stateSlug);
            if (borderContent == null)
            {
                _logger.LogWarning("Border content not found for state: {StateSlug}", stateSlug);
                return NotFound();
            }

            var allSymbols = await _symbolService.GetSymbolsByStateAsync(state.Id);
            var relatedSymbols = allSymbols.Take(6).ToList();
            var quizQuestions = BuildQuizQuestions("us-states-general-quiz");

            var model = new BorderDetailViewModel
            {
                State = state,
                BorderContent = borderContent,
                RelatedSymbols = relatedSymbols,
                QuizQuestions = quizQuestions
            };

            ViewData["Title"] = borderContent.SeoTitle;
            ViewData["Description"] = borderContent.SeoDescription;
            ViewData["OgImage"] = borderContent.Map?.Image ?? state.FlagImageUrl;

            return View(model);
        }

        [Route("borders/{stateSlug}")]
        public IActionResult LegacyBorders(string stateSlug)
        {
            if (string.IsNullOrWhiteSpace(stateSlug))
                return NotFound();

            return RedirectPermanent($"/states/{stateSlug.ToLowerInvariant()}/borders");
        }

        [Route("guides")]
        public IActionResult Index()
        {
            ViewData["Title"] = "State Guides — Geography, Demographics & More";
            ViewData["Description"] = "Comprehensive guides about U.S. states — borders, abbreviations, last names, climate, and more.";
            return View();
        }

        [Route("guides/state-abbreviations")]
        public async Task<IActionResult> StateAbbreviations()
        {
            var states = await _stateService.GetAllStatesAsync();
            var highIntentSlugs = (await _stateAbbreviationContentService.GetHighIntentSlugsAsync()).ToList();

            ViewData["Title"] = "State Abbreviations and Capitals — All 50 US States";
            ViewData["Description"] = "Complete list of all 50 U.S. state abbreviations with capitals in alphabetical order. Two-letter postal codes, state capitals, regions, and common abbreviation mistakes.";
            ViewData["Canonical"] = "/guides/state-abbreviations";
            ViewData["TopAbbreviationStates"] = states
                .Where(s => highIntentSlugs.Contains(s.Slug, StringComparer.OrdinalIgnoreCase))
                .OrderBy(s => highIntentSlugs.IndexOf(s.Slug))
                .ToList();

            return View(states.OrderBy(s => s.Name).ToList());
        }

        [Route("states/{stateSlug}/abbreviation")]
        public async Task<IActionResult> StateAbbreviationDetail(string stateSlug)
        {
            var state = await _stateService.GetStateBySlugAsync(stateSlug);
            if (state == null)
                return NotFound();

            var content = await _stateAbbreviationContentService.GetContentAsync(stateSlug);
            if (content == null)
                return NotFound();

            var allStates = await _stateService.GetAllStatesAsync();
            var alphabetical = allStates.OrderBy(s => s.Name).ToList();
            var stateIndex = alphabetical.FindIndex(s => s.Id == state.Id);

            var model = new StateAbbreviationGuideViewModel
            {
                State = state,
                TraditionalAbbreviation = content.TraditionalAbbreviation,
                FormationSummary = content.FormationSummary,
                FormationDetail = content.FormationDetail,
                SearchIntentSummary = content.SearchIntentSummary,
                SimilarCodesSummary = content.SimilarCodesSummary,
                CapitalZip = content.CapitalZip,
                ApStyleNote = content.ApStyleNote,
                ConfusionWarning = content.ConfusionWarning,
                StateSpecificTitle = string.IsNullOrWhiteSpace(content.StateSpecificTitle) ? null : content.StateSpecificTitle,
                StateSpecificParagraphs = content.StateSpecificParagraphs ?? new List<string>(),
                SimilarStates = GetSimilarStates(state, alphabetical),
                RegionalStates = alphabetical
                    .Where(s => s.Id != state.Id && string.Equals(s.Region, state.Region, StringComparison.OrdinalIgnoreCase))
                    .Take(4)
                    .ToList(),
                AlphabeticalNeighbors = alphabetical
                    .Where((_, index) => index >= Math.Max(0, stateIndex - 2) && index <= Math.Min(alphabetical.Count - 1, stateIndex + 2))
                    .Where(s => s.Id != state.Id)
                    .ToList()
            };

            ViewData["Title"] = $"{state.Name} Abbreviation";
            ViewData["H1"] = $"What Is the Abbreviation for {state.Name}?";
            ViewData["Description"] = $"The standard 2-letter abbreviation for {state.Name} is {state.Abbreviation}. Learn the USPS state code, older short form {content.TraditionalAbbreviation}, and how {state.Abbreviation} is used in addresses and forms.";
            ViewData["Canonical"] = $"/states/{state.Slug}/abbreviation";
            ViewData["OgImage"] = state.FlagImageUrl;
            ViewData["LatestContentRail"] = await _latestContentRailService.GetLatestItemsAsync(8);
            ViewData["AllStates"] = allStates.Select(s => new { s.Slug, s.Name, s.Abbreviation, s.Region }).Cast<dynamic>().ToList();

            return View("StateAbbreviationDetail", model);
        }

        [Route("guides/surnames")]
        public async Task<IActionResult> SurnamesHub()
        {
            var states = await _stateService.GetAllStatesAsync();
            var availableSlugs = _surnamesService.GetAvailableSlugs().ToHashSet(StringComparer.OrdinalIgnoreCase);

            var items = new List<SurnamesHubItem>();
            foreach (var state in states.OrderBy(s => s.Name))
            {
                string? topSurname = null;
                if (availableSlugs.Contains(state.Slug))
                {
                    var content = await _surnamesService.GetSurnamesContentAsync(state.Slug);
                    topSurname = content?.Surnames.FirstOrDefault(s => s.Rank == 1)?.Name;
                }
                items.Add(new SurnamesHubItem { State = state, HasData = availableSlugs.Contains(state.Slug), TopSurname = topSurname });
            }

            ViewData["Title"] = "Most Common Last Names by State — All 50 States";
            ViewData["Description"] = "Explore the top 20 most common last names in every U.S. state. Etymology, origins, and Census data — find out if your surname made the list.";
            ViewData["Canonical"] = "/guides/surnames";
            return View(new SurnamesHubViewModel { Items = items });
        }

        [Route("guides/state-borders")]
        public async Task<IActionResult> BordersListing()
        {
            var states = await _stateService.GetAllStatesAsync();
            var items = new List<StateBorderItem>();
            foreach (var state in states.OrderBy(s => s.Name))
            {
                var borderContent = await _borderService.GetBorderContentAsync(state.Slug);
                if (borderContent?.BorderSummary != null)
                {
                    var summary = borderContent.BorderSummary;
                    items.Add(new StateBorderItem
                    {
                        State = state,
                        BorderCount = summary.BorderingStatesCount,
                        NeighborsList = summary.BorderingStatesList ?? "",
                        IsLandlocked = summary.Landlocked,
                        HasOcean = HasBorder(summary.OceanBorders),
                        HasGreatLakes = HasBorder(summary.GreatLakeBorders),
                        HasInternational = HasBorder(summary.CountryBorders),
                        MapImage = borderContent.Map?.Image ?? state.FlagImageUrl ?? ""
                    });
                }
            }
            var model = new StateBordersListViewModel
            {
                Items = items,
                TotalStates = items.Count,
                CoastalStates = items.Count(x => x.HasOcean),
                LandlockedStates = items.Count(x => x.IsLandlocked),
                GreatLakesStates = items.Count(x => x.HasGreatLakes),
                InternationalBorderStates = items.Count(x => x.HasInternational)
            };
            ViewData["Title"] = "US State Borders - Complete Border Guide";
            ViewData["Description"] = "Explore the borders of all 50 US states. Find which states share boundaries, landlocked states, and coastal information.";
            return View(model);
        }

        private bool HasBorder(string? borderValue)
        {
            var value = borderValue?.Trim() ?? "";
            return !string.IsNullOrEmpty(value) &&
                   !value.Equals("None", StringComparison.OrdinalIgnoreCase);
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

        private static List<State> GetSimilarStates(State state, List<State> alphabetical)
        {
            var firstLetter = state.Name[..1];
            return alphabetical
                .Where(s => s.Id != state.Id)
                .OrderByDescending(s => s.Name.StartsWith(firstLetter, StringComparison.OrdinalIgnoreCase))
                .ThenBy(s => s.Name)
                .Where(s =>
                    s.Name.StartsWith(firstLetter, StringComparison.OrdinalIgnoreCase) ||
                    s.Abbreviation.StartsWith(state.Abbreviation[..1], StringComparison.OrdinalIgnoreCase))
                .Take(4)
                .ToList();
        }
    }
}
