using Microsoft.Extensions.Caching.Memory;
using usasymbol.Services.Interface;
using USASymbol.Models.Content;
using USASymbol.Models.ViewModels;
using YamlDotNet.Serialization;

namespace USASymbol.Services
{
    public class FlagService : IFlagService
    {
        private readonly IMemoryCache _cache;
        private readonly IWebHostEnvironment _env;
        private readonly IDeserializer _yamlDeserializer;

        public FlagService(IMemoryCache cache, IWebHostEnvironment env)
        {
            _cache = cache;
            _env = env;
            _yamlDeserializer = new DeserializerBuilder().Build();
        }

        public async Task<FlagContent?> GetFlagContentAsync(string stateSlug)
        {
            var path = Path.Combine(_env.ContentRootPath, "Content", "states", stateSlug, "flag.yaml");

            if (!File.Exists(path))
                return null;

            var cacheKey = $"flag-{stateSlug}-{File.GetLastWriteTimeUtc(path).Ticks}";

            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromHours(24);

                var yaml = await File.ReadAllTextAsync(path);

                try
                {
                    var data = _yamlDeserializer.Deserialize<Dictionary<object, object>>(yaml);

                    var flagContent = new FlagContent
                    {
                        Type = GetString(data, "type"),
                        State = GetString(data, "state"),
                        Name = !string.IsNullOrWhiteSpace(GetString(data, "name")) ? GetString(data, "name") : GetString(data, "title"),
                        AdoptedYear = GetInt(data, "adopted_year"),
                        StandardizedYear = GetInt(data, "standardized_year"),
                        IsOfficial = GetBool(data, "is_official"),
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
                        ExpertQuoteAfterSectionId = GetString(data, "expert_quote_after_section"),
                        ComparisonCardsAfterSectionId = GetString(data, "comparison_cards_after_section")
                    };


                    if (data.ContainsKey("sections") && data["sections"] is List<object> sections)
                    {
                        foreach (var sec in sections)
                        {
                            if (sec is Dictionary<object, object> secDict)
                            {
                                var section = new FlagSection
                                {
                                    Id = GetString(secDict, "id"),
                                    Icon = GetString(secDict, "icon"),
                                    Style = GetString(secDict, "style"),
                                    Title = GetString(secDict, "title"),
                                    Img = secDict.ContainsKey("img") ? GetString(secDict, "img") : null
                                };


                                if (secDict.ContainsKey("paragraphs") && secDict["paragraphs"] is List<object> paragraphs)
                                {
                                    section.Paragraphs = paragraphs.OfType<string>().ToList();
                                }


                                if (secDict.ContainsKey("facts") && secDict["facts"] is List<object> facts)
                                {
                                    section.Facts = facts.Select(f => f?.ToString() ?? "").ToList();
                                }

                                if (secDict.ContainsKey("subsections") && secDict["subsections"] is List<object> subsections)
                                {
                                    var flagSubsections = new List<FlagSubsection>();
                                    section.Subsections = flagSubsections.Cast<IContentSubsection>().ToList();

                                    foreach (var sub in subsections)
                                    {
                                        if (sub is Dictionary<object, object> subDict)
                                        {
                                            var subsection = new FlagSubsection
                                            {
                                                Subtitle = GetString(subDict, "subtitle"),
                                                Text = GetString(subDict, "text")
                                            };

                                            if (subDict.ContainsKey("list") && subDict["list"] is List<object> list)
                                            {
                                                subsection.ListItems = list.Select(l => l?.ToString() ?? "").ToList();
                                            }

                                            flagSubsections.Add(subsection);
                                        }
                                    }
                                }


                                if (secDict.ContainsKey("versions") && secDict["versions"] is List<object> versions)
                                {
                                    foreach (var ver in versions)
                                    {
                                        if (ver is Dictionary<object, object> verDict)
                                        {
                                            section.Versions.Add(new FlagVersion
                                            {
                                                Name = GetString(verDict, "name"),
                                                Years = GetString(verDict, "years"),
                                                Image = GetString(verDict, "image"),
                                                Description = GetString(verDict, "description")
                                            });
                                        }
                                    }
                                }


                                if (secDict.ContainsKey("symbols") && secDict["symbols"] is List<object> symbols)
                                {
                                    foreach (var sym in symbols)
                                    {
                                        if (sym is Dictionary<object, object> symDict)
                                        {
                                            var symbol = new FlagSymbol
                                            {
                                                Id = GetString(symDict, "id"),
                                                Name = GetString(symDict, "name"),
                                                Image = GetString(symDict, "image")
                                            };

                                            if (symDict.ContainsKey("paragraphs") && symDict["paragraphs"] is List<object> symParagraphs)
                                            {
                                                symbol.Paragraphs = symParagraphs.Select(p => p?.ToString() ?? "").ToList();
                                            }

                                            section.Symbols.Add(symbol);
                                        }
                                    }
                                }


                                if (secDict.ContainsKey("colors") && secDict["colors"] is List<object> colorsList)
                                {
                                    foreach (var colorItem in colorsList)
                                    {
                                        if (colorItem is Dictionary<object, object> colDict)
                                        {
                                            section.Colors.Add(new FlagColor
                                            {
                                                Name = GetString(colDict, "name"),
                                                Hex = GetString(colDict, "hex"),
                                                Pantone = GetString(colDict, "pantone"),
                                                Cable = GetString(colDict, "cable")
                                            });
                                        }
                                    }
                                }

                                flagContent.Sections.Add(section);
                            }
                        }
                    }


                    if (data.ContainsKey("faq") && data["faq"] is List<object> faqList)
                    {
                        foreach (var faq in faqList)
                        {
                            if (faq is Dictionary<object, object> faqDict)
                            {
                                flagContent.Faq.Add(new FlagFaq
                                {
                                    Question = GetString(faqDict, "question"),
                                    Answer = GetString(faqDict, "answer")
                                });
                            }
                        }
                    }


                    if (data.ContainsKey("sources") && data["sources"] is List<object> sources)
                    {
                        foreach (var src in sources)
                        {
                            if (src is Dictionary<object, object> srcDict)
                            {
                                flagContent.Sources.Add(new FlagSource
                                {
                                    Name = GetString(srcDict, "name"),
                                    Url = GetString(srcDict, "url"),
                                    Description = GetString(srcDict, "description")
                                });
                            }
                        }
                    }

                    if (data.TryGetValue("big_stat", out var bigStatObj) && bigStatObj is Dictionary<object, object> bigStatDict)
                    {
                        flagContent.BigStat = new BigStatData
                        {
                            Number = GetString(bigStatDict, "number"),
                            Description = GetString(bigStatDict, "description")
                        };
                    }

                    if (data.TryGetValue("timeline", out var timelineObj) && timelineObj is List<object> timelineList)
                    {
                        foreach (var item in timelineList.OfType<Dictionary<object, object>>())
                        {
                            var year = GetString(item, "year");
                            var description = GetString(item, "description");

                            if (string.IsNullOrWhiteSpace(year) && string.IsNullOrWhiteSpace(description))
                                continue;

                            flagContent.Timeline.Add(new TimelineEvent
                            {
                                Year = year,
                                Description = description
                            });
                        }
                    }

                    if (data.TryGetValue("expert_quote", out var expertQuoteObj) && expertQuoteObj is Dictionary<object, object> expertQuoteDict)
                    {
                        flagContent.ExpertQuote = new ExpertQuoteData
                        {
                            Text = GetString(expertQuoteDict, "text"),
                            Source = GetString(expertQuoteDict, "source")
                        };
                    }


                    if (data.TryGetValue("quick_facts", out var quickFactsObj) && quickFactsObj is List<object> quickFactsList)
                    {
                        foreach (var fact in quickFactsList)
                        {
                            if (fact is not Dictionary<object, object> factDict) continue;

                            var label = GetString(factDict, "label");
                            var value = GetString(factDict, "value");

                            if (string.IsNullOrWhiteSpace(label) || string.IsNullOrWhiteSpace(value))
                                continue;

                            flagContent.QuickFacts.Add(new QuickFactItem
                            {
                                Label = label.Trim(),
                                Value = value.Trim(),
                                Url = GetString(factDict, "url"),
                                Italic = GetBool(factDict, "italic")
                            });
                        }
                    }


                    if (flagContent.QuickFacts.Count == 0)
                        flagContent.QuickFacts = BuildQuickFacts(flagContent);


                    flagContent.VisualAssets = YamlParse.VisualAssets(data);
                    flagContent.ComparisonCards = YamlParse.ComparisonCards(data);

                    if (flagContent.ComparisonCards != null &&
                        string.IsNullOrWhiteSpace(flagContent.ComparisonCardsAfterSectionId))
                    {
                        flagContent.ComparisonCardsAfterSectionId =
                            flagContent.Sections.FirstOrDefault()?.Id ?? string.Empty;
                    }

                    return flagContent;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing flag.yaml for {stateSlug}: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                    return null;
                }
            });
        }

        private string GetString(Dictionary<object, object> dict, string key)
        {
            return dict.ContainsKey(key) ? dict[key]?.ToString() ?? "" : "";
        }

        private int? GetInt(Dictionary<object, object> dict, string key)
        {
            if (dict.ContainsKey(key) && int.TryParse(dict[key]?.ToString(), out int result))
                return result;
            return null;
        }

        private bool GetBool(Dictionary<object, object> dict, string key)
        {
            if (dict.ContainsKey(key) && bool.TryParse(dict[key]?.ToString(), out bool result))
                return result;
            return false;
        }

        private DateTime? GetDate(Dictionary<object, object> dict, string key)
        {
            if (dict.ContainsKey(key) && DateTime.TryParse(dict[key]?.ToString(), out DateTime result))
                return result;
            return null;
        }

        private static List<QuickFactItem> BuildQuickFacts(FlagContent content)
        {
            var facts = new List<QuickFactItem>();

            void AddFact(string label, string? value, bool italic = false)
            {
                if (string.IsNullOrWhiteSpace(value))
                    return;

                facts.Add(new QuickFactItem
                {
                    Label = label,
                    Value = value.Trim(),
                    Italic = italic
                });
            }

            if (content.AdoptedYear.HasValue && content.AdoptedYear.Value > 0)
                AddFact("Adopted", content.AdoptedYear.Value.ToString());

            if (content.StandardizedYear.HasValue && content.StandardizedYear.Value > 0)
                AddFact("Standardized", content.StandardizedYear.Value.ToString());

            AddFact("Status", content.IsOfficial ? "Official flag" : "State flag");
            AddFact("Legislation", content.Legislation);

            return facts;
        }
    }
}
