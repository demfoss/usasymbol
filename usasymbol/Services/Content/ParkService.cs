using Microsoft.Extensions.Caching.Memory;
using USASymbol.Models.Content;
using USASymbol.Models.ViewModels;
using USASymbol.Services.Interface;
using YamlDotNet.Serialization;

namespace USASymbol.Services.Content
{
    public class ParkService : IParkService
    {
        private readonly IMemoryCache _cache;
        private readonly IWebHostEnvironment _env;
        private readonly IDeserializer _yaml;
        private readonly ILogger<ParkService> _logger;

        public ParkService(
            IMemoryCache cache,
            IWebHostEnvironment env,
            ILogger<ParkService> logger)
        {
            _cache = cache;
            _env = env;
            _logger = logger;
            _yaml = new DeserializerBuilder().Build();
        }

        public async Task<ParkContent?> GetParkAsync(string parkSlug)
        {
            var cacheKey = $"park-{parkSlug}";
            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromHours(24);
                var path = ParkPath("national", parkSlug);
                if (!File.Exists(path))
                    return null;
                return await LoadAsync(path);
            });
        }

        public async Task<List<ParkContent>> GetAllNationalParksAsync()
        {
            const string cacheKey = "parks-national-all";
            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromHours(24);
                var dir = Path.Combine(_env.ContentRootPath, "Content", "parks", "national");
                if (!Directory.Exists(dir))
                    return new List<ParkContent>();

                var results = new List<ParkContent>();
                foreach (var file in Directory.EnumerateFiles(dir, "*.yml"))
                {
                    var park = await LoadAsync(file);
                    if (park != null)
                        results.Add(park);
                }
                return results.OrderBy(p => p.Name).ToList();
            }) ?? new List<ParkContent>();
        }

        public async Task<ParkCollectionConfig?> GetCollectionConfigAsync(string slug)
        {
            var cacheKey = $"park-collection-config-{slug}";
            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromHours(24);
                var path = Path.Combine(_env.ContentRootPath, "Content", "parks", "collections", $"{slug}.yml");
                if (!File.Exists(path)) return null;

                var raw = await File.ReadAllTextAsync(path);
                var data = _yaml.Deserialize<Dictionary<object, object>>(raw);
                if (data == null) return null;

                return new ParkCollectionConfig
                {
                    Slug        = S(data, "slug"),
                    H1          = S(data, "h1"),
                    SeoTitle    = S(data, "seo_title"),
                    SeoDescription = S(data, "seo_description"),
                    Intro       = S(data, "intro"),
                    QuickAnswer = S(data, "quick_answer"),
                    SortBy      = S(data, "sort_by") is { Length: > 0 } sb ? sb : "name",
                    SortDir     = S(data, "sort_dir") is { Length: > 0 } sd ? sd : "asc",
                    FilterField = SN(data, "filter_field"),
                    FilterValue = SN(data, "filter_value"),
                    MetricLabel = S(data, "metric_label"),
                    MetricFormat = S(data, "metric_format") is { Length: > 0 } mf ? mf : "text",
                    Updated     = SN(data, "updated"),
                };
            });
        }

        public async Task<List<ParkContent>> GetCollectionParksAsync(ParkCollectionConfig config)
        {
            var all = await GetAllNationalParksAsync();
            IEnumerable<ParkContent> parks = all;

            // Apply filter
            if (!string.IsNullOrWhiteSpace(config.FilterField))
            {
                parks = config.FilterField switch
                {
                    "free"        => parks.Where(p => !p.Filters.HasEntranceFee),
                    "activity"    => parks.Where(p => p.Filters.Activities.Contains(config.FilterValue ?? "", StringComparer.OrdinalIgnoreCase)),
                    "pets_allowed"=> parks.Where(p => !string.IsNullOrWhiteSpace(p.Filters.PetsAllowed) && p.Filters.PetsAllowed != "none"),
                    "dark_sky"    => parks.Where(p => p.Filters.DarkSky),
                    _             => parks,
                };
            }

            // Apply sort
            parks = config.SortBy switch
            {
                "visitation_rank" => config.SortDir == "desc"
                    ? parks.Where(p => p.Stats.VisitationRank > 0).OrderByDescending(p => p.Stats.VisitationRank)
                    : parks.Where(p => p.Stats.VisitationRank > 0).OrderBy(p => p.Stats.VisitationRank),

                "area_acres" => config.SortDir == "desc"
                    ? parks.OrderByDescending(p => p.Stats.AreaAcres)
                    : parks.OrderBy(p => p.Stats.AreaAcres),

                "established_year" => config.SortDir == "desc"
                    ? parks.OrderByDescending(p => p.EstablishedYear ?? int.MaxValue)
                    : parks.OrderBy(p => p.EstablishedYear ?? int.MaxValue),

                _ => parks.OrderBy(p => p.Name),
            };

            return parks.ToList();
        }

        private string ParkPath(string designation, string slug)
            => Path.Combine(_env.ContentRootPath, "Content", "parks", designation, $"{slug}.yml");

        private async Task<ParkContent?> LoadAsync(string path)
        {
            try
            {
                var raw = await File.ReadAllTextAsync(path);
                var data = _yaml.Deserialize<Dictionary<object, object>>(raw);
                if (data == null) return null;

                var park = new ParkContent
                {
                    Slug = S(data, "slug"),
                    Designation = S(data, "designation"),
                    Name = S(data, "name"),
                    NpsCode = S(data, "nps_code"),
                    SeoTitle = S(data, "seo_title"),
                    SeoDescription = S(data, "seo_description"),
                    IntroText = S(data, "intro_text"),
                    Author = S(data, "author"),
                    EstablishedYear = ParseInt(data, "established_year") is > 0 and var ey ? ey : null,
                    DatePublished = ParseDate(data, "date_published"),
                    DateModified = ParseDate(data, "date_modified"),
                    LastModified = File.GetLastWriteTime(path),
                };

                if (data.TryGetValue("location", out var locObj) && locObj is Dictionary<object, object> loc)
                {
                    var state = S(loc, "state");
                    var stateCode = S(loc, "state_code");
                    var states = ParseStringList(loc, "states");
                    var stateCodes = ParseStringList(loc, "state_codes");

                    if (states.Count == 0 && !string.IsNullOrWhiteSpace(state))
                        states.Add(state);
                    if (stateCodes.Count == 0 && !string.IsNullOrWhiteSpace(stateCode))
                        stateCodes.Add(stateCode);

                    park.Location = new ParkLocation
                    {
                        State = state,
                        StateCode = stateCode,
                        States = states,
                        StateCodes = stateCodes,
                        Region = S(loc, "region"),
                        Latitude = ParseDouble(loc, "latitude"),
                        Longitude = ParseDouble(loc, "longitude"),
                        NearestCity = S(loc, "nearest_city"),
                        NearestMajorAirport = S(loc, "nearest_major_airport"),
                    };
                }

                if (data.TryGetValue("map", out var mapObj) && mapObj is Dictionary<object, object> map)
                {
                    park.Map = new ParkMap
                    {
                        Zoom = ParseInt(map, "zoom", 10),
                        GoogleSearchUrl = S(map, "google_search_url"),
                        GoogleDirectionsUrl = S(map, "google_directions_url"),
                    };
                }

                if (data.TryGetValue("quick_facts", out var qfObj) && qfObj is List<object> qfList)
                {
                    foreach (var item in qfList)
                    {
                        if (item is not Dictionary<object, object> d) continue;
                        var label = S(d, "label");
                        var value = S(d, "value");
                        if (string.IsNullOrWhiteSpace(label) || string.IsNullOrWhiteSpace(value)) continue;
                        park.QuickFacts.Add(new QuickFactItem { Label = label, Value = value });
                    }
                }

                if (data.TryGetValue("highlight_stats", out var hsObj) && hsObj is List<object> hsList)
                {
                    foreach (var item in hsList)
                    {
                        if (item is not Dictionary<object, object> d) continue;
                        var stat = S(d, "stat");
                        var label = S(d, "label");
                        if (string.IsNullOrWhiteSpace(stat)) continue;
                        park.HighlightStats.Add(new ParkHighlightStat { Stat = stat, Label = label });
                    }
                }

                if (data.TryGetValue("filters", out var fObj) && fObj is Dictionary<object, object> f)
                {
                    park.Filters = new ParkFilters
                    {
                        HasEntranceFee = ParseBool(f, "has_entrance_fee"),
                        ReservationStatus = S(f, "reservation_status"),
                        Landscapes = ParseStringList(f, "landscapes"),
                        Activities = ParseStringList(f, "activities"),
                        Seasons = ParseStringList(f, "seasons"),
                        PetsAllowed = S(f, "pets_allowed"),
                        DarkSky = ParseBool(f, "dark_sky"),
                    };
                }

                if (data.TryGetValue("media", out var mObj) && mObj is Dictionary<object, object> m)
                {
                    park.Media = new ParkMedia
                    {
                        HeroImage = S(m, "hero_image"),
                        HeroAlt = S(m, "hero_alt"),
                        HeroCredit = S(m, "hero_credit"),
                    };

                    if (m.TryGetValue("highlights", out var hlObj) && hlObj is List<object> hlList)
                    {
                        foreach (var item in hlList)
                        {
                            if (item is not Dictionary<object, object> d) continue;
                            park.Media.Highlights.Add(new ParkHighlight
                            {
                                Image = S(d, "image"),
                                Alt = S(d, "alt"),
                                Credit = S(d, "credit"),
                            });
                        }
                    }
                }

                if (data.TryGetValue("stats", out var statsObj) && statsObj is Dictionary<object, object> st)
                {
                    park.Stats = new ParkStats
                    {
                        AreaAcres = ParseInt(st, "area_acres"),
                        VisitationRank = ParseInt(st, "visitation_rank"),
                        EntranceFeeDisplay = S(st, "entrance_fee_display"),
                    };
                }

                if (data.TryGetValue("rankings", out var rankObj) && rankObj is Dictionary<object, object> rank)
                {
                    park.Rankings = new ParkRankings
                    {
                        OverallRank   = ParseInt(rank, "overall_rank"),
                        Personality   = ParseInt(rank, "personality"),
                        Beauty        = ParseInt(rank, "beauty"),
                        Recreation    = ParseInt(rank, "recreation"),
                        Privacy       = ParseInt(rank, "privacy"),
                        Weather       = ParseInt(rank, "weather"),
                        Wildlife      = ParseInt(rank, "wildlife"),
                        Practicality  = ParseInt(rank, "practicality"),
                        Accessibility = ParseInt(rank, "accessibility"),
                        Amenities     = ParseInt(rank, "amenities"),
                        Lodging       = ParseInt(rank, "lodging"),
                        Frugality     = ParseInt(rank, "frugality"),
                        Family        = ParseInt(rank, "family"),
                    };
                }

                if (data.TryGetValue("section_images", out var siObj) && siObj is Dictionary<object, object> si)
                {
                    park.SectionImageHiking   = SN(si, "hiking");
                    park.SectionImageHistory  = SN(si, "history");
                    park.SectionImageWildlife = SN(si, "wildlife");
                    park.SectionImageCamping  = SN(si, "camping");
                }

                if (data.TryGetValue("sections", out var secObj) && secObj is Dictionary<object, object> secs)
                {
                    park.SectionOverview = SN(secs, "overview");
                    park.SectionKnownFor = SN(secs, "known_for");
                    park.SectionBestThingsToSee = SN(secs, "best_things_to_see");

                    if (secs.TryGetValue("best_things_to_see_items", out var btsObj) && btsObj is List<object> btsList)
                    {
                        foreach (var item in btsList)
                        {
                            if (item is not Dictionary<object, object> d) continue;
                            var name = S(d, "name");
                            if (string.IsNullOrWhiteSpace(name)) continue;
                            park.BestThingsToSeeItems.Add(new ParkAttractionItem
                            {
                                Name        = name,
                                Description = S(d, "description"),
                                Image       = SN(d, "image"),
                                Alt         = SN(d, "alt"),
                                Credit      = SN(d, "credit"),
                            });
                        }
                    }
                    park.SectionBestTimeToVisit = SN(secs, "best_time_to_visit");
                    park.SectionHiking = SN(secs, "hiking");
                    park.SectionCamping = SN(secs, "camping");
                    park.SectionFeesReservations = SN(secs, "fees_reservations");

                    if (secs.TryGetValue("hiking_trails", out var trailsObj) && trailsObj is List<object> trailsList)
                    {
                        foreach (var item in trailsList)
                        {
                            if (item is not Dictionary<object, object> d) continue;
                            park.HikingTrails.Add(new ParkTrail
                            {
                                Name = S(d, "name"),
                                Difficulty = S(d, "difficulty"),
                                Distance = S(d, "distance"),
                                Elevation = S(d, "elevation"),
                                Note = S(d, "note"),
                            });
                        }
                    }

                    if (secs.TryGetValue("seasons", out var seasonsObj) && seasonsObj is List<object> seasonsList)
                    {
                        foreach (var item in seasonsList)
                        {
                            if (item is not Dictionary<object, object> d) continue;
                            park.Seasons.Add(new ParkSeason
                            {
                                Season = S(d, "season"),
                                Months = S(d, "months"),
                                TempRim = S(d, "temp_rim"),
                                CrowdLevel = S(d, "crowd_level"),
                                Verdict = S(d, "verdict"),
                            });
                        }
                    }

                    if (secs.TryGetValue("campgrounds", out var campObj) && campObj is List<object> campList)
                    {
                        foreach (var item in campList)
                        {
                            if (item is not Dictionary<object, object> d) continue;
                            park.Campgrounds.Add(new ParkCampground
                            {
                                Name = S(d, "name"),
                                Sites = ParseInt(d, "sites"),
                                Season = S(d, "season"),
                                Reservations = S(d, "reservations"),
                                Note = S(d, "note"),
                            });
                        }
                    }

                    if (secs.TryGetValue("fees", out var feesObj) && feesObj is List<object> feesList)
                    {
                        foreach (var item in feesList)
                        {
                            if (item is not Dictionary<object, object> d) continue;
                            park.Fees.Add(new ParkFeeItem
                            {
                                PassType = S(d, "pass_type"),
                                Cost = S(d, "cost"),
                                Note = S(d, "note"),
                            });
                        }
                    }
                    park.SectionGettingThere = SN(secs, "getting_there");
                    park.SectionGeology = SN(secs, "geology");
                    park.SectionWildlife = SN(secs, "wildlife");
                    park.SectionHistory = SN(secs, "history");
                }

                if (data.TryGetValue("faq", out var faqObj) && faqObj is List<object> faqList)
                {
                    foreach (var item in faqList)
                    {
                        if (item is not Dictionary<object, object> d) continue;
                        park.Faq.Add(new ParkFaq
                        {
                            Question = S(d, "question"),
                            Answer = S(d, "answer"),
                        });
                    }
                }

                if (data.TryGetValue("sources", out var srcObj) && srcObj is List<object> srcList)
                {
                    foreach (var item in srcList)
                    {
                        if (item is not Dictionary<object, object> d) continue;
                        park.Sources.Add(new ParkSource
                        {
                            Name = S(d, "name"),
                            Url = S(d, "url"),
                            Description = S(d, "description"),
                        });
                    }
                }

                return park;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load national park content from {Path}", path);
                return null;
            }
        }

        private static string S(Dictionary<object, object> d, string key)
            => d.TryGetValue(key, out var v) ? v?.ToString() ?? "" : "";

        private static string? SN(Dictionary<object, object> d, string key)
        {
            var v = S(d, key);
            return string.IsNullOrWhiteSpace(v) ? null : v;
        }

        private static double ParseDouble(Dictionary<object, object> d, string key)
            => double.TryParse(S(d, key), System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : 0;

        private static int ParseInt(Dictionary<object, object> d, string key, int fallback = 0)
            => int.TryParse(S(d, key), out var v) ? v : fallback;

        private static bool ParseBool(Dictionary<object, object> d, string key)
        {
            if (!d.TryGetValue(key, out var v)) return false;
            if (v is bool b) return b;
            return bool.TryParse(v?.ToString(), out var parsed) && parsed;
        }

        private static DateTime? ParseDate(Dictionary<object, object> d, string key)
            => DateTime.TryParse(S(d, key), out var dt) ? dt : null;

        private static List<string> ParseStringList(Dictionary<object, object> d, string key)
        {
            if (!d.TryGetValue(key, out var v) || v is not List<object> list)
                return new List<string>();
            return list.Select(x => x?.ToString() ?? "").Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        }
    }
}
