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
    public class BeverageService : IBeverageService
    {
        private readonly IMemoryCache _cache;
        private readonly IWebHostEnvironment _env;
        private readonly IDeserializer _yamlDeserializer;

        public BeverageService(IMemoryCache cache, IWebHostEnvironment env)
        {
            _cache = cache;
            _env = env;
            _yamlDeserializer = new DeserializerBuilder().Build();
        }

        public async Task<BeverageContent?> GetBeverageContentAsync(string stateSlug, string symbolSlug)
        {
            var cacheKey = $"beverage-{stateSlug}-{symbolSlug}";

            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromHours(24);

                var path = Path.Combine(_env.ContentRootPath, "Content", "states", stateSlug, "beverage", $"{symbolSlug}.yaml");
                if (!File.Exists(path))
                    return null;

                var yaml = await File.ReadAllTextAsync(path);

                try
                {
                    var data = _yamlDeserializer.Deserialize<Dictionary<object, object>>(yaml);

                    var content = new BeverageContent
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

                            var section = new BeverageSection
                            {
                                Id = GetString(secDict, "id"),
                                Icon = GetString(secDict, "icon"),
                                Title = GetString(secDict, "title"),
                                Style = GetString(secDict, "style"),
                                Img = secDict.ContainsKey("img") ? GetString(secDict, "img") : null
                            };

                            if (secDict.TryGetValue("paragraphs", out var paragraphsObj) && paragraphsObj is List<object> paragraphs)
                                section.Paragraphs = paragraphs.OfType<string>().ToList();

                            if (secDict.TryGetValue("subsections", out var subsectionsObj) && subsectionsObj is List<object> subsections)
                            {
                                section.Subsections = new List<BeverageSubsection>();

                                foreach (var sub in subsections)
                                {
                                    if (sub is not Dictionary<object, object> subDict) continue;

                                    var subsection = new BeverageSubsection
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
                            content.Sources.Add(new BeverageSource
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
                            content.Faq.Add(new BeverageFaq
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

        public BeverageContent BuildFallbackContent(State state, Symbol symbol, IReadOnlyCollection<Symbol> stateBeverages)
        {
            var designation = NormalizeDesignation(symbol.Designation);
            var designationLower = designation.ToLowerInvariant();
            var category = GetCategoryLabel(symbol);
            var sameStateBeverages = stateBeverages
                .Where(s => s.Type == "beverage")
                .OrderBy(s => s.AdoptedYear ?? int.MaxValue)
                .ThenBy(s => s.Name)
                .ToList();

            var otherBeverages = sameStateBeverages
                .Where(s => !string.Equals(s.Slug, symbol.Slug, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var title = symbol.Name;
            var adoptedYear = symbol.AdoptedYear;
            var yearText = adoptedYear?.ToString(CultureInfo.InvariantCulture) ?? "an unknown year";
            var meaning = BuildMeaningText(symbol);
            var intro = $"{title} is the official {designationLower} of {state.Name}, adopted in {yearText}. {BuildIntroFollowUp(state, otherBeverages)}";

            var content = new BeverageContent
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
                SeoTitle = $"{state.Name} {designation}: {title}",
                SeoDescription = $"Learn about {title}, the official {designationLower} of {state.Name}, including adoption history, symbolism, and related state drink designations.",
                IntroText = intro
            };

            AddFact(content, "Designation", designation);
            if (adoptedYear.HasValue && adoptedYear.Value > 0)
                AddFact(content, "Adopted", adoptedYear.Value.ToString(CultureInfo.InvariantCulture));
            AddFact(content, "Category", category);
            AddFact(content, "State", state.Name);
            if (otherBeverages.Count > 0)
                AddFact(content, "Also in state law", $"{otherBeverages.Count} more drink designation{(otherBeverages.Count == 1 ? "" : "s")}");
            else
                AddFact(content, "Status", "Only official drink-related symbol in the state");

            content.Sections.Add(new BeverageSection
            {
                Id = "official-designation",
                Icon = "fa-solid fa-scale-balanced",
                Title = "Official Designation",
                Paragraphs = new List<string>
                {
                    $"{state.Name} adopted {title} as its official {designationLower} in {yearText}. The designation gives the state a drink symbol that can be understood both as a legal emblem and as a shorthand for a broader piece of state identity.",
                    BuildNationalContext(symbol, designationLower)
                }
            });

            content.Sections.Add(new BeverageSection
            {
                Id = "state-symbolism",
                Icon = GetSymbolismIcon(symbol),
                Title = $"Why {title} Fits {state.Name}",
                Paragraphs = new List<string>
                {
                    meaning,
                    BuildStateSymbolismFollowUp(state, symbol, otherBeverages)
                }
            });

            content.Sections.Add(new BeverageSection
            {
                Id = "in-the-state",
                Icon = "fa-solid fa-map-pin",
                Title = $"{title} in {state.Name}",
                Paragraphs = new List<string>
                {
                    BuildInStateParagraph(state, symbol, otherBeverages),
                    BuildPortfolioParagraph(state, symbol, otherBeverages)
                }
            });

            var connections = BuildConnectionSubsections(state, otherBeverages);
            if (connections.Count > 0)
            {
                content.Sections.Add(new BeverageSection
                {
                    Id = "symbol-connections",
                    Icon = "fa-solid fa-link",
                    Title = "Related Drink Symbols",
                    Subsections = connections
                });
            }

            foreach (var evt in BuildTimeline(state, symbol, sameStateBeverages))
                content.Timeline.Add(evt);

            if (sameStateBeverages.Count > 1)
            {
                content.BigStat = new BigStatData
                {
                    Number = sameStateBeverages.Count.ToString(CultureInfo.InvariantCulture),
                    Description = $"{state.Name} has {sameStateBeverages.Count} official drink-related designation{(sameStateBeverages.Count == 1 ? "" : "s")} in this guide."
                };
            }

            content.Faq.Add(new BeverageFaq
            {
                Question = $"What is the official {designationLower} of {state.Name}?",
                Answer = $"{state.Name}'s official {designationLower} is {title}."
            });

            content.Faq.Add(new BeverageFaq
            {
                Question = $"When did {state.Name} adopt {title}?",
                Answer = adoptedYear.HasValue
                    ? $"{state.Name} adopted {title} in {adoptedYear.Value.ToString(CultureInfo.InvariantCulture)}."
                    : $"{state.Name} recognizes {title}, but the adoption year is not listed in this record."
            });

            content.Faq.Add(new BeverageFaq
            {
                Question = $"Why did {state.Name} choose {title}?",
                Answer = meaning
            });

            content.Faq.Add(new BeverageFaq
            {
                Question = $"Does {state.Name} have other official drink symbols?",
                Answer = otherBeverages.Count == 0
                    ? $"No. {title} is the only drink-related designation listed for {state.Name} in this guide."
                    : $"Yes. {state.Name} also recognizes {FormatSymbolList(otherBeverages)}."
            });

            content.Sources.Add(new BeverageSource
            {
                Name = "List of U.S. state beverages",
                Url = "https://en.wikipedia.org/wiki/List_of_U.S._state_beverages",
                Description = "Reference list covering state beverages, spirits, cocktails, juices, and related drink designations."
            });

            content.Sources.Add(new BeverageSource
            {
                Name = $"{state.Name} symbols overview",
                Url = $"/states/{state.Slug}",
                Description = $"Internal state guide showing how {title} fits within the broader symbol set of {state.Name}."
            });

            content.Sources.Add(new BeverageSource
            {
                Name = "State beverages hub",
                Url = "/symbols/beverages",
                Description = "National hub listing official state beverages, state drinks, spirits, cocktails, and other drink symbols."
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

                var state = GetString(linkDict, "state_slug") ?? GetString(linkDict, "state") ?? currentStateSlug;
                var beverage = GetString(linkDict, "beverage_slug") ?? GetString(linkDict, "beverage") ?? GetString(linkDict, "symbol_slug");

                if (!string.IsNullOrWhiteSpace(state) && !string.IsNullOrWhiteSpace(beverage))
                    return new LinkData { Url = $"/states/{state.Trim()}/beverage/{beverage.Trim()}", Label = label };
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

            return $"/states/{currentStateSlug}/beverage/{s}";
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

        private static void AddFact(BeverageContent content, string label, string? value, bool italic = false)
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
                return "State Beverage";

            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(designation.Trim().ToLowerInvariant());
        }

        private static string BuildMeaningText(Symbol symbol)
        {
            if (!string.IsNullOrWhiteSpace(symbol.Meaning))
                return symbol.Meaning.Trim();

            var designation = NormalizeDesignation(symbol.Designation).ToLowerInvariant();
            return $"{symbol.Name} is recognized as a {designation} because it reflects an identifiable part of the state's history, culture, or economy.";
        }

        private static string BuildIntroFollowUp(State state, IReadOnlyCollection<Symbol> otherBeverages)
        {
            if (otherBeverages.Count == 0)
                return $"The designation highlights a recognizable part of {state.Name}'s history, culture, agriculture, or public identity.";

            return $"It also sits alongside {FormatSymbolList(otherBeverages)} in the state's broader drink symbolism, showing that official drink laws can cover more than one side of state identity.";
        }

        private static string BuildNationalContext(Symbol symbol, string designationLower)
        {
            var name = symbol.Name.ToLowerInvariant();

            if (name == "milk")
                return "Milk is the most common official drink choice among the states, so laws like this one usually emphasize agriculture, nutrition, and dairy production more than a single famous recipe.";

            if (designationLower.Contains("cocktail"))
                return "Cocktail designations are much more specific than general beverage laws, which gives this symbol a distinctly local and place-based identity.";

            if (designationLower.Contains("spirit") || name.Contains("whiskey"))
                return "Spirit designations usually point to a state's distilling history, signature product, or a named tradition that lawmakers wanted to elevate above a more generic beverage symbol.";

            if (designationLower.Contains("juice") || name.Contains("juice") || name.Contains("cider"))
                return "Juice and cider designations usually highlight a crop that is strongly associated with the state and easy for visitors to connect to local agriculture.";

            if (designationLower.Contains("soft drink"))
                return "Soft-drink designations often celebrate a local invention, brand legacy, or a product that became widely associated with the state beyond agriculture alone.";

            if (name.Contains("water"))
                return "Water stands out as an unusual state beverage choice because it emphasizes public life and daily necessity rather than a crop, recipe, or branded drink.";

            if (name.Contains("coffee") || name.Contains("tea") || symbol.Slug == "awa")
                return "This kind of designation works especially well when a drink represents a distinctive local tradition, a hospitality practice, or a lifestyle strongly associated with the state.";

            return "Like many official drink laws, the designation works as a quick way to connect a familiar beverage to the state's agriculture, history, culture, or public image.";
        }

        private static string GetCategoryLabel(Symbol symbol)
        {
            var designation = NormalizeDesignation(symbol.Designation).ToLowerInvariant();
            var name = symbol.Name.ToLowerInvariant();

            if (name == "milk")
                return "Dairy beverage";
            if (designation.Contains("cocktail"))
                return "Cocktail";
            if (designation.Contains("spirit") || name.Contains("whiskey"))
                return "Spirit";
            if (designation.Contains("soft drink"))
                return "Soft drink";
            if (designation.Contains("juice") || name.Contains("juice"))
                return "Juice";
            if (name.Contains("cider"))
                return "Cider";
            if (name.Contains("coffee"))
                return "Coffee beverage";
            if (name.Contains("tea"))
                return "Tea beverage";
            if (name.Contains("water"))
                return "Water";
            if (symbol.Slug == "awa")
                return "Traditional beverage";

            return "Beverage";
        }

        private static string GetSymbolismIcon(Symbol symbol)
        {
            var designation = NormalizeDesignation(symbol.Designation).ToLowerInvariant();
            var name = symbol.Name.ToLowerInvariant();

            if (designation.Contains("cocktail"))
                return "fa-solid fa-martini-glass-citrus";
            if (designation.Contains("spirit") || name.Contains("whiskey"))
                return "fa-solid fa-whiskey-glass";
            if (designation.Contains("soft drink"))
                return "fa-solid fa-bottle-water";
            if (designation.Contains("juice") || name.Contains("juice") || name.Contains("cider"))
                return "fa-solid fa-apple-whole";
            if (name.Contains("coffee"))
                return "fa-solid fa-mug-hot";
            if (name.Contains("tea"))
                return "fa-solid fa-mug-saucer";
            if (name == "milk")
                return "fa-solid fa-cow";

            return "fa-solid fa-book-open";
        }

        private static string BuildStateSymbolismFollowUp(State state, Symbol symbol, IReadOnlyCollection<Symbol> otherBeverages)
        {
            if (otherBeverages.Count == 0)
                return $"That makes {symbol.Name} the clearest drink symbol on the books for {state.Name}, without any competing beverage, spirit, or cocktail designation in this guide.";

            return $"{symbol.Name} is part of a broader drink portfolio in {state.Name}. Alongside {FormatSymbolList(otherBeverages)}, it helps show that state drink laws can cover more than one part of a state's food and hospitality identity.";
        }

        private static string BuildInStateParagraph(State state, Symbol symbol, IReadOnlyCollection<Symbol> otherBeverages)
        {
            var category = GetCategoryLabel(symbol).ToLowerInvariant();
            if (otherBeverages.Count == 0)
                return $"Within {state.Name}, {symbol.Name} works as a concise public-facing symbol of state identity. It gives the state a recognizable {category} designation that can be discussed alongside other official emblems.";

            return $"Within {state.Name}, {symbol.Name} is one layer of a more detailed drink identity. Rather than relying on a single beverage law, the state uses multiple drink designations to represent agriculture, history, tourism, or local tradition.";
        }

        private static string BuildPortfolioParagraph(State state, Symbol symbol, IReadOnlyCollection<Symbol> otherBeverages)
        {
            if (otherBeverages.Count == 0)
                return $"That makes the symbol straightforward: when people look for the official drink of {state.Name}, {symbol.Name} is the main answer presented in state-symbol context.";

            return $"{symbol.Name} sits alongside {FormatSymbolList(otherBeverages)}, giving {state.Name} one of the more layered official drink profiles in the country.";
        }

        private static List<BeverageSubsection> BuildConnectionSubsections(State state, IReadOnlyCollection<Symbol> otherBeverages)
        {
            var sections = new List<BeverageSubsection>();

            foreach (var other in otherBeverages)
            {
                sections.Add(new BeverageSubsection
                {
                    Subtitle = $"{other.Name} in {state.Name}",
                    Text = $"{other.Name} is also recognized in {state.Name} as {WithArticle(NormalizeDesignation(other.Designation).ToLowerInvariant())}, giving the state more than one official drink symbol.",
                    Link = new LinkData
                    {
                        Label = $"Open {other.Name}",
                        Url = $"/states/{state.Slug}/beverage/{other.Slug}"
                    }
                });
            }

            sections.Add(new BeverageSubsection
            {
                Subtitle = "National comparison",
                Text = "Browse the full state-by-state list to compare how other legislatures designate beverages, drinks, spirits, cocktails, and juices.",
                Link = new LinkData
                {
                    Label = "Browse all state beverages",
                    Url = "/symbols/beverages"
                }
            });

            return sections;
        }

        private static List<TimelineEvent> BuildTimeline(State state, Symbol currentSymbol, IReadOnlyCollection<Symbol> stateBeverages)
        {
            var timeline = new List<TimelineEvent>();

            foreach (var symbol in stateBeverages)
            {
                var designationLower = NormalizeDesignation(symbol.Designation).ToLowerInvariant();
                timeline.Add(new TimelineEvent
                {
                    Year = symbol.AdoptedYear?.ToString(CultureInfo.InvariantCulture) ?? "Official",
                    Description = string.Equals(symbol.Slug, currentSymbol.Slug, StringComparison.OrdinalIgnoreCase)
                        ? $"{state.Name} designated {symbol.Name} as {WithArticle(designationLower)}."
                        : $"{state.Name} also recognized {symbol.Name} as {WithArticle(designationLower)}."
                });
            }

            if (timeline.Count == 0)
            {
                timeline.Add(new TimelineEvent
                {
                    Year = "Official",
                    Description = $"{currentSymbol.Name} appears in this guide as {WithArticle(NormalizeDesignation(currentSymbol.Designation).ToLowerInvariant())} of {state.Name}."
                });
            }

            return timeline;
        }

        private static string FormatSymbolList(IReadOnlyCollection<Symbol> symbols)
        {
            var parts = symbols
                .Select(s => $"{s.Name} as {WithArticle(NormalizeDesignation(s.Designation).ToLowerInvariant())}")
                .ToList();

            if (parts.Count == 0)
                return "";

            if (parts.Count == 1)
                return parts[0];

            if (parts.Count == 2)
                return $"{parts[0]} and {parts[1]}";

            return $"{string.Join(", ", parts.Take(parts.Count - 1))}, and {parts[^1]}";
        }

        private static string WithArticle(string phrase)
        {
            if (string.IsNullOrWhiteSpace(phrase))
                return "an official state symbol";

            var trimmed = phrase.Trim();
            if (trimmed.StartsWith("a ", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("an ", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("the ", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed;
            }

            var article = "aeiou".Contains(char.ToLowerInvariant(trimmed[0])) ? "an" : "a";
            return $"{article} {trimmed}";
        }
    }
}
