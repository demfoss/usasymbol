using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using usasymbol.Models;
using usasymbol.Services;
using usasymbol.Services.Interface;
using USASymbol.Data;
using USASymbol.Extensions;
using USASymbol.Models;
using USASymbol.Models.ViewModels;
using USASymbol.Services;
using Microsoft.AspNetCore.Mvc.Filters;


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
        private readonly ILatestContentRailService _latestContentRailService;
        private readonly QuizService _quizService;
        private readonly ILogger<SymbolController> _logger;
        private readonly AppDbContext _db;

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
            ILatestContentRailService latestContentRailService,
            QuizService quizService,
            ILogger<SymbolController> logger,
            AppDbContext db)
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
            _latestContentRailService = latestContentRailService;
            _quizService = quizService;
            _logger = logger;
            _db = db;
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

            var categories = await _db.SymbolCategories
                .OrderBy(c => c.Id)
                .ToListAsync();

            var symbolCounts = await _db.Symbols
                .GroupBy(s => s.Type)
                .Select(g => new
                {
                    Type = g.Key,
                    StateCount = g.Select(x => x.StateId).Distinct().Count()
                })
                .ToDictionaryAsync(x => x.Type, x => x.StateCount);

            var viewModel = categories.Select(c =>
            {
                var singularType = c.Type.EndsWith("s")
                    ? c.Type.Substring(0, c.Type.Length - 1)
                    : c.Type;

                symbolCounts.TryGetValue(singularType, out var stateCount);

                return new SymbolCategoryViewModel
                {
                    Type = c.Type,
                    Name = c.Name,
                    Description = c.Description,
                    ImageUrl = c.ImageUrl,
                    StateCount = stateCount,
                    Url = $"/symbols/{c.Type}",
                    Icon = c.Type switch
                    {
                        "birds" => "🦅",
                        "flowers" => "🌸",
                        "trees" => "🌳",
                        "flags" => "🏴",
                        "beverages" => "🥤",
                        "mottos" => "📜",
                        "nicknames" => "🏷️",
                        "mammals" => "🦌",
                        "colors" => "🎨",
                        "dogs" => "🐕",
                        "horses" => "🐴",
                        "marine-mammals" => "🐬",
                        "firearms" => "🔫",
                        "dinosaurs" => "🦖",
                        "license-plate-slogans" => "🚗",
                        "state-seals" => "🔖",
                        _ => "⭐"
                    }

                };
            }).ToList();

            return View("Categories", viewModel);
        }

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

        [Route("states/{stateSlug}/{symbolType}")]
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
