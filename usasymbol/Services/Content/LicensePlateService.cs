using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using usasymbol.Services.Interface;
using USASymbol.Models;
using USASymbol.Models.Content;
using USASymbol.Models.ViewModels;
using YamlDotNet.Serialization;
using System.Globalization;

namespace USASymbol.Services
{
    public class LicensePlateService : ILicensePlateService
    {
        private readonly IMemoryCache _cache;
        private readonly IWebHostEnvironment _env;
        private readonly IDeserializer _yamlDeserializer;

        public LicensePlateService(IMemoryCache cache, IWebHostEnvironment env)
        {
            _cache = cache;
            _env = env;
            _yamlDeserializer = new DeserializerBuilder().Build();
        }

        public async Task<LicensePlateContent?> GetLicensePlateContentAsync(string stateSlug, string symbolSlug)
        {
            var cacheKey = $"license-plate-{stateSlug}";

            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromHours(24);

                var path = Path.Combine(_env.ContentRootPath, "Content", "states", stateSlug, "license-plate.yaml");
                if (!File.Exists(path))
                    return null;

                var yaml = await File.ReadAllTextAsync(path);

                try
                {
                    var data = _yamlDeserializer.Deserialize<Dictionary<object, object>>(yaml);

                    var content = new LicensePlateContent
                    {
                        Title = GetString(data, "title"),
                        AdoptedYear = GetInt(data, "adopted_year"),
                        WikidataId = string.Empty,
                        Legislation = GetString(data, "legislation"),
                        Meaning = GetString(data, "meaning"),
                        Author = GetString(data, "author"),
                        DatePublished = GetDate(data, "date_published"),
                        DateModified = GetDate(data, "date_modified"),
                        LastModified = File.GetLastWriteTime(path),
                        SeoTitle = GetString(data, "seo_title"),
                        SeoDescription = GetString(data, "seo_description"),
                        IntroText = GetString(data, "intro_text"),
                        BigStatAfterSectionId = GetString(data, "big_stat_after_section"),
                        TimelineAfterSectionId = GetString(data, "timeline_after_section"),
                        ExpertQuoteAfterSectionId = GetString(data, "expert_quote_after_section")
                    };

                    if (data.TryGetValue("sections", out var sectionsObj) && sectionsObj is List<object> sections)
                    {
                        foreach (var sec in sections)
                        {
                            if (sec is not Dictionary<object, object> secDict) continue;

                            var section = new LicensePlateSection
                            {
                                Id = GetString(secDict, "id"),
                                Icon = GetString(secDict, "icon"),
                                Title = GetString(secDict, "title"),
                                Style = GetString(secDict, "style"),
                                Img = secDict.ContainsKey("img") ? GetString(secDict, "img") : null
                            };

                            if (secDict.TryGetValue("paragraphs", out var paragraphsObj) && paragraphsObj is List<object> paragraphs)
                                section.Paragraphs = paragraphs.Select(p => p?.ToString() ?? "").ToList();

                            if (secDict.TryGetValue("subsections", out var subsectionsObj) && subsectionsObj is List<object> subsections)
                            {
                                section.Subsections = new List<LicensePlateSubsection>();

                                foreach (var sub in subsections)
                                {
                                    if (sub is not Dictionary<object, object> subDict) continue;

                                    var subsection = new LicensePlateSubsection
                                    {
                                        Subtitle = GetString(subDict, "subtitle"),
                                        Text = GetString(subDict, "text"),
                                        Link = ParseLinkData(subDict, stateSlug)
                                    };

                                    if (TryGetList(subDict, "list_items", out var listItems) || TryGetList(subDict, "list", out listItems))
                                        subsection.ListItems = listItems.Select(l => l?.ToString() ?? "").ToList();

                                    section.Subsections.Add(subsection);
                                }
                            }

                            if (secDict.TryGetValue("facts", out var factsObj) && factsObj is List<object> facts)
                                section.Facts = facts.Select(f => f?.ToString() ?? "").ToList();

                            if (TryGetList(secDict, "list_items", out var sectionList) || TryGetList(secDict, "list", out sectionList))
                                section.ListItems = sectionList.Select(item => item?.ToString() ?? "").ToList();

                            if (secDict.TryGetValue("versions", out var versionsObj) && versionsObj is List<object> versions)
                            {
                                foreach (var version in versions.OfType<Dictionary<object, object>>())
                                {
                                    section.Versions.Add(new LicensePlateVersion
                                    {
                                        Name = GetString(version, "name"),
                                        Years = GetString(version, "years"),
                                        Image = GetString(version, "image"),
                                        Description = GetString(version, "description")
                                    });
                                }
                            }

                            content.Sections.Add(section);
                        }
                    }

                    if (data.TryGetValue("big_stat", out var bigStatObj) && bigStatObj is Dictionary<object, object> bigStatDict)
                    {
                        content.BigStat = new BigStatData
                        {
                            Number = GetString(bigStatDict, "number"),
                            Description = GetString(bigStatDict, "description")
                        };
                    }

                    if (data.TryGetValue("timeline", out var timelineObj) && timelineObj is List<object> timelineList)
                    {
                        foreach (var evt in timelineList.OfType<Dictionary<object, object>>())
                        {
                            content.Timeline.Add(new TimelineEvent
                            {
                                Year = GetString(evt, "year"),
                                Description = GetString(evt, "description")
                            });
                        }
                    }

                    if (data.TryGetValue("expert_quote", out var quoteObj) && quoteObj is Dictionary<object, object> quoteDict)
                    {
                        content.ExpertQuote = new ExpertQuoteData
                        {
                            Text = GetString(quoteDict, "text"),
                            Source = GetString(quoteDict, "source")
                        };
                    }

                    if (data.TryGetValue("sources", out var sourcesObj) && sourcesObj is List<object> sources)
                    {
                        foreach (var src in sources.OfType<Dictionary<object, object>>())
                        {
                            content.Sources.Add(new LicensePlateSource
                            {
                                Name = GetString(src, "name"),
                                Url = GetString(src, "url"),
                                Description = GetString(src, "description")
                            });
                        }
                    }

                    if (data.TryGetValue("faq", out var faqObj) && faqObj is List<object> faqList)
                    {
                        foreach (var faq in faqList.OfType<Dictionary<object, object>>())
                        {
                            content.Faq.Add(new LicensePlateFaq
                            {
                                Question = GetString(faq, "question"),
                                Answer = GetString(faq, "answer")
                            });
                        }
                    }

                    if (data.TryGetValue("quick_facts", out var quickFactsObj) && quickFactsObj is List<object> quickFacts)
                    {
                        foreach (var fact in quickFacts.OfType<Dictionary<object, object>>())
                        {
                            var label = GetString(fact, "label");
                            var value = GetString(fact, "value");

                            if (string.IsNullOrWhiteSpace(label) || string.IsNullOrWhiteSpace(value))
                                continue;

                            content.QuickFacts.Add(new QuickFactItem
                            {
                                Label = label,
                                Value = value.Trim(),
                                Italic = GetBool(fact, "italic")
                            });
                        }
                    }

                    content.VisualAssets = YamlParse.VisualAssets(data);

                    return content;
                }
                catch
                {
                    return null;
                }
            });
        }

        public LicensePlateContent BuildFallbackContent(State state, Symbol symbol, IReadOnlyCollection<Symbol> stateSlogans)
        {
            var designation = NormalizeDesignation(symbol.Designation);
            var designationLower = designation.ToLowerInvariant();
            var sameStateSlogans = stateSlogans
                .Where(s => s.Type == "license-plate")
                .OrderBy(s => s.AdoptedYear ?? int.MaxValue)
                .ThenBy(s => s.Name)
                .ToList();

            var otherSlogans = sameStateSlogans
                .Where(s => !string.Equals(s.Slug, symbol.Slug, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var title = symbol.Name;
            var adoptedYear = symbol.AdoptedYear;
            var yearText = adoptedYear?.ToString(CultureInfo.InvariantCulture) ?? "an unknown year";
            var meaning = BuildMeaningText(symbol, state);
            var intro = $"\"{title}\" is the {designationLower} of {state.Name}. {BuildIntroFollowUp(state, symbol)}";

            var content = new LicensePlateContent
            {
                Title = title,
                AdoptedYear = adoptedYear,
                WikidataId = string.Empty,
                Legislation = symbol.Legislation ?? "",
                Meaning = meaning,
                Author = "USA Symbol Team",
                DatePublished = DateTime.UtcNow.Date,
                DateModified = DateTime.UtcNow.Date,
                LastModified = DateTime.UtcNow,
                SeoTitle = $"{state.Name} License Plate Slogan: \"{title}\"",
                SeoDescription = $"Learn about \"{title}\", the license plate slogan of {state.Name}, including its history, meaning, and how it represents the state.",
                IntroText = intro
            };

            AddFact(content, "Slogan", title);
            AddFact(content, "Designation", designation);
            if (adoptedYear.HasValue && adoptedYear.Value > 0)
                AddFact(content, "Introduced", adoptedYear.Value.ToString(CultureInfo.InvariantCulture));
            AddFact(content, "State", state.Name);

            content.Sections.Add(new LicensePlateSection
            {
                Id = "official-slogan",
                Icon = "fa-solid fa-id-card",
                Title = "The Slogan on the Plate",
                Paragraphs = new List<string>
                {
                    $"\"{title}\" appears on {state.Name}'s standard license plates as a condensed statement of state identity. License plate slogans function as everyday public-facing symbols: they appear on millions of vehicles and are seen across state lines more consistently than almost any other official emblem.",
                    BuildNationalContext(symbol, state)
                }
            });

            content.Sections.Add(new LicensePlateSection
            {
                Id = "slogan-meaning",
                Icon = "fa-solid fa-book-open",
                Title = $"What \"{title}\" Means for {state.Name}",
                Paragraphs = new List<string>
                {
                    meaning,
                    BuildSymbolismFollowUp(state, symbol)
                }
            });

            content.Sections.Add(new LicensePlateSection
            {
                Id = "history",
                Icon = "fa-solid fa-clock-rotate-left",
                Title = $"History of the Slogan",
                Paragraphs = new List<string>
                {
                    BuildHistoryParagraph(state, symbol),
                    $"License plate slogans represent one of the most visible categories of state identity. Unlike a state bird or state flower that most residents never encounter directly, a plate slogan travels with every registered vehicle across {state.Name} and beyond — making it a constantly moving piece of state branding."
                }
            });

            if (otherSlogans.Count > 0)
            {
                var connections = BuildConnectionSubsections(state, otherSlogans);
                content.Sections.Add(new LicensePlateSection
                {
                    Id = "related-slogans",
                    Icon = "fa-solid fa-link",
                    Title = "Related License Plate Slogans",
                    Subsections = connections
                });
            }

            foreach (var evt in BuildTimeline(state, symbol, sameStateSlogans))
                content.Timeline.Add(evt);

            content.Faq.Add(new LicensePlateFaq
            {
                Question = $"What does \"{title}\" mean on {state.Name}'s license plate?",
                Answer = meaning
            });

            content.Faq.Add(new LicensePlateFaq
            {
                Question = $"When did {state.Name} start using \"{title}\" on its plates?",
                Answer = adoptedYear.HasValue
                    ? $"{state.Name} introduced \"{title}\" on its license plates around {adoptedYear.Value.ToString(CultureInfo.InvariantCulture)}."
                    : $"{state.Name} uses \"{title}\" on its license plates, though the exact introduction year is not confirmed in this record."
            });

            content.Faq.Add(new LicensePlateFaq
            {
                Question = $"Is \"{title}\" the official slogan of {state.Name}?",
                Answer = $"\"{title}\" is the slogan printed on {state.Name}'s standard license plates. It serves as a shorthand identifier for the state and reflects its public image and identity."
            });

            content.Sources.Add(new LicensePlateSource
            {
                Name = "License plate slogans hub",
                Url = "/symbols/license-plate-slogans",
                Description = "National hub listing license plate slogans from all 50 U.S. states."
            });

            content.Sources.Add(new LicensePlateSource
            {
                Name = $"{state.Name} symbols overview",
                Url = $"/states/{state.Slug}",
                Description = $"Internal state guide showing how the plate slogan fits within the broader symbol set of {state.Name}."
            });

            return content;
        }

        private LinkData? ParseLinkData(Dictionary<object, object> subDict, string currentStateSlug)
        {
            if (!subDict.TryGetValue("link", out var linkObj) || linkObj is null)
                return null;

            if (linkObj is string linkStr)
            {
                var url = NormalizeInternalUrl(linkStr, currentStateSlug);
                return string.IsNullOrWhiteSpace(url) ? null : new LinkData { Url = url };
            }

            if (linkObj is Dictionary<object, object> linkDict)
            {
                var url = GetString(linkDict, "url");
                var label = GetString(linkDict, "label");

                if (!string.IsNullOrWhiteSpace(url))
                {
                    url = NormalizeInternalUrl(url, currentStateSlug);
                    return string.IsNullOrWhiteSpace(url) ? null : new LinkData { Url = url, Label = label };
                }

                var stateSlug = GetString(linkDict, "state_slug") ?? GetString(linkDict, "state") ?? currentStateSlug;
                var sloganSlug = GetString(linkDict, "slogan_slug") ?? GetString(linkDict, "symbol_slug");

                if (!string.IsNullOrWhiteSpace(stateSlug) && !string.IsNullOrWhiteSpace(sloganSlug))
                    return new LinkData { Url = $"/states/{stateSlug.Trim()}/license-plate/{sloganSlug.Trim()}", Label = label };
            }

            return null;
        }

        private string? NormalizeInternalUrl(string raw, string currentStateSlug)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;

            var s = raw.Trim();
            if (s.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase)) return null;
            if (s.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return null;
            if (s.StartsWith("/", StringComparison.Ordinal)) return s;

            return $"/states/{currentStateSlug}/license-plate/{s}";
        }

        private static bool TryGetList(Dictionary<object, object> dict, string key, out List<object> items)
        {
            if (dict.TryGetValue(key, out var value) && value is List<object> list)
            {
                items = list;
                return true;
            }

            items = new List<object>();
            return false;
        }

        private static string GetString(Dictionary<object, object> dict, string key)
            => dict.ContainsKey(key) ? dict[key]?.ToString() ?? "" : "";

        private static int? GetInt(Dictionary<object, object> dict, string key)
        {
            if (dict.ContainsKey(key) && int.TryParse(dict[key]?.ToString(), out var result))
                return result;
            return null;
        }

        private static bool GetBool(Dictionary<object, object> dict, string key)
        {
            if (!dict.ContainsKey(key))
                return false;

            return bool.TryParse(dict[key]?.ToString(), out var result) && result;
        }

        private static DateTime? GetDate(Dictionary<object, object> dict, string key)
        {
            if (dict.ContainsKey(key) && DateTime.TryParse(dict[key]?.ToString(), out var result))
                return result;
            return null;
        }

        private static void AddFact(LicensePlateContent content, string label, string? value, bool italic = false)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            content.QuickFacts.Add(new QuickFactItem
            {
                Label = label,
                Value = value.Trim(),
                Italic = italic
            });
        }

        private static string NormalizeDesignation(string? designation)
        {
            if (string.IsNullOrWhiteSpace(designation))
                return "License Plate Slogan";

            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(designation.Trim().ToLowerInvariant());
        }

        private static string BuildMeaningText(Symbol symbol, State state)
        {
            if (!string.IsNullOrWhiteSpace(symbol.Meaning))
                return symbol.Meaning.Trim();

            return $"\"{symbol.Name}\" reflects a key aspect of {state.Name}'s public identity — the geography, history, spirit, or appeal that the state most wants to project on every vehicle crossing its roads and highways.";
        }

        private static string BuildIntroFollowUp(State state, Symbol symbol)
        {
            return $"The slogan appears on standard {state.Name} license plates and functions as one of the most widely seen state identifiers — visible to drivers across state lines far more often than most official emblems.";
        }

        private static string BuildNationalContext(Symbol symbol, State state)
        {
            var slogan = symbol.Name.ToLowerInvariant();

            if (slogan.Contains("vacation") || slogan.Contains("sunshine") || slogan.Contains("golden") || slogan.Contains("paradise"))
                return "Tourism-oriented slogans like this one are among the most common on state plates. They frame the state as a destination and work equally well on residents' plates and on visitors' license plates heading home.";

            if (slogan.Contains("frontier") || slogan.Contains("wild") || slogan.Contains("big sky") || slogan.Contains("scenic"))
                return "Slogans tied to landscape and wilderness are a natural fit for states whose public identity is closely linked to outdoors culture, open land, or distinctive geography that visitors associate with the state name.";

            if (slogan.Contains("free") || slogan.Contains("liberty") || slogan.Contains("constitution") || slogan.Contains("first state"))
                return "Historical and political slogans draw on founding-era identity. These choices tend to be older and more stable — states that pick liberty or constitutional themes rarely change them, because the reference transcends contemporary politics.";

            if (slogan.Contains("lincoln") || slogan.Contains("volunteer") || slogan.Contains("crossroads") || slogan.Contains("heart"))
                return "Heritage slogans reference a specific person, event, or geographic role. They work best when the association is so well-known it doesn't require explanation on the plate itself.";

            if (slogan.Contains("dairyland") || slogan.Contains("potatoes") || slogan.Contains("peach") || slogan.Contains("garden"))
                return "Agricultural identity slogans connect the state to a product it's famous for producing. They lean into a single memorable association that survives even as the economy diversifies.";

            return "License plate slogans compress state identity into just a few words. The most enduring ones work because they're specific enough to feel true and broad enough to apply across the whole state.";
        }

        private static string BuildSymbolismFollowUp(State state, Symbol symbol)
        {
            return $"The slogan acts as shorthand for {state.Name}'s public image — a phrase residents recognize and outsiders associate with the state before they learn anything else about it. That compression is both the strength and the limitation of a license plate slogan as a form of state identity.";
        }

        private static string BuildHistoryParagraph(State state, Symbol symbol)
        {
            var yearText = symbol.AdoptedYear.HasValue
                ? $"around {symbol.AdoptedYear.Value.ToString(CultureInfo.InvariantCulture)}"
                : "at a point in the state's modern plate history";

            return $"{state.Name} began using \"{symbol.Name}\" on its standard plates {yearText}. The choice reflects how the state wanted to be identified by the millions of drivers who would see the plates both within {state.Name} and across state lines.";
        }

        private static List<LicensePlateSubsection> BuildConnectionSubsections(State state, IReadOnlyCollection<Symbol> otherSlogans)
        {
            var sections = new List<LicensePlateSubsection>();

            foreach (var other in otherSlogans)
            {
                sections.Add(new LicensePlateSubsection
                {
                    Subtitle = $"\"{other.Name}\"",
                    Text = $"{state.Name} has also used \"{other.Name}\" on its license plates, showing that the state has maintained more than one slogan across different plate designs or time periods.",
                    Link = new LinkData
                    {
                        Label = $"Open \"{other.Name}\"",
                        Url = $"/states/{state.Slug}/license-plate/{other.Slug}"
                    }
                });
            }

            sections.Add(new LicensePlateSubsection
            {
                Subtitle = "Compare all state slogans",
                Text = "Browse the full list to compare license plate slogans across all 50 states.",
                Link = new LinkData
                {
                    Label = "Browse all license plate slogans",
                    Url = "/symbols/license-plate-slogans"
                }
            });

            return sections;
        }

        private static List<TimelineEvent> BuildTimeline(State state, Symbol currentSymbol, IReadOnlyCollection<Symbol> stateSlogans)
        {
            var timeline = new List<TimelineEvent>();

            foreach (var symbol in stateSlogans)
            {
                timeline.Add(new TimelineEvent
                {
                    Year = symbol.AdoptedYear?.ToString(CultureInfo.InvariantCulture) ?? "Active",
                    Description = string.Equals(symbol.Slug, currentSymbol.Slug, StringComparison.OrdinalIgnoreCase)
                        ? $"{state.Name} introduced \"{symbol.Name}\" on its standard license plates."
                        : $"{state.Name} also used \"{symbol.Name}\" on its license plates."
                });
            }

            if (timeline.Count == 0)
            {
                timeline.Add(new TimelineEvent
                {
                    Year = "Active",
                    Description = $"\"{currentSymbol.Name}\" appears on {state.Name}'s standard license plates."
                });
            }

            return timeline;
        }
    }
}
