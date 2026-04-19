using Microsoft.Extensions.Caching.Memory;
using usasymbol.Services.Interface;
using USASymbol.Models.Content;
using USASymbol.Models.ViewModels;
using YamlDotNet.Serialization;

namespace USASymbol.Services
{
    public class MammalService : IMammalService
    {
        private readonly IMemoryCache _cache;
        private readonly IWebHostEnvironment _env;
        private readonly IDeserializer _yamlDeserializer;

        public MammalService(IMemoryCache cache, IWebHostEnvironment _env)
        {
            _cache = cache;
            this._env = _env;
            _yamlDeserializer = new DeserializerBuilder().Build();
        }

        public async Task<MammalContent?> GetMammalContentAsync(string stateSlug, string symbolSlug)
        {
            var cacheKey = $"mammal-{stateSlug}-{symbolSlug}";

            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromHours(24);

                var path = Path.Combine(_env.ContentRootPath, "Content", "states", stateSlug, "mammal", $"{symbolSlug}.yaml");

                if (!File.Exists(path))
                    return null;

                var yaml = await File.ReadAllTextAsync(path);

                try
                {
                    var data = _yamlDeserializer.Deserialize<Dictionary<object, object>>(yaml);

                    var mammalContent = new MammalContent
                    {
                        Title = GetString(data, "title"),
                        ScientificName = GetString(data, "scientific_name"),
                        AdoptedYear = GetInt(data, "adopted_year"),


                        CommonName = GetString(data, "common_name"),
                        OfficialSince = GetString(data, "official_since"),
                        Status = GetString(data, "status"),
                        HabitatInState = GetString(data, "habitat_in_state"),
                        KnownFor = GetString(data, "known_for"),


                        Size = GetString(data, "size"),
                        Weight = GetString(data, "weight"),
                        Coloration = GetString(data, "coloration"),
                        DistinctiveTraits = GetString(data, "distinctive_traits"),


                        Habitat = GetString(data, "habitat"),
                        RangeInState = GetString(data, "range_in_state"),


                        ConservationStatus = GetString(data, "conservation_status"),
                        WhereToFind = GetString(data, "where_to_find"),
                        SymbolConnections = GetString(data, "symbol_connections"),


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
                    };




                    if (data.ContainsKey("quick_summary") && data["quick_summary"] is List<object> quickSummary)
                    {
                        mammalContent.QuickSummary = quickSummary.Select(q => q?.ToString() ?? "").ToList();
                    }


                    mammalContent.DidYouKnow = GetString(data, "did_you_know");


                    if (data.ContainsKey("big_stat") && data["big_stat"] is Dictionary<object, object> bigStatDict)
                    {
                        mammalContent.BigStat = new BigStatData
                        {
                            Number = GetString(bigStatDict, "number"),
                            Description = GetString(bigStatDict, "description")
                        };
                    }


                    if (data.ContainsKey("timeline") && data["timeline"] is List<object> timeline)
                    {
                        mammalContent.Timeline = new List<TimelineEvent>();
                        foreach (var evt in timeline)
                        {
                            if (evt is Dictionary<object, object> evtDict)
                            {
                                mammalContent.Timeline.Add(new TimelineEvent
                                {
                                    Year = GetString(evtDict, "year"),
                                    Description = GetString(evtDict, "description")
                                });
                            }
                        }
                    }


                    if (data.ContainsKey("expert_quote") && data["expert_quote"] is Dictionary<object, object> quoteDict)
                    {
                        mammalContent.ExpertQuote = new ExpertQuoteData
                        {
                            Text = GetString(quoteDict, "text"),
                            Source = GetString(quoteDict, "source")
                        };
                    }


                    if (data.ContainsKey("sections") && data["sections"] is List<object> sections)
                    {
                        foreach (var sec in sections)
                        {
                            if (sec is Dictionary<object, object> secDict)
                            {
                                var section = new MammalSection
                                {
                                    Id = GetString(secDict, "id"),
                                    Icon = GetString(secDict, "icon"),
                                    Title = GetString(secDict, "title"),
                                    Style = GetString(secDict, "style"),
                                    Img   = secDict.ContainsKey("img") ? GetString(secDict, "img") : null
                                };


                                if (secDict.ContainsKey("paragraphs") && secDict["paragraphs"] is List<object> paragraphs)
                                {
                                    section.Paragraphs = paragraphs.Select(p => p?.ToString() ?? "").ToList();
                                }


                                if (secDict.ContainsKey("subsections") && secDict["subsections"] is List<object> subsections)
                                {
                                    section.Subsections = new List<IContentSubsection>();
                                    foreach (var sub in subsections)
                                    {
                                        if (sub is Dictionary<object, object> subDict)
                                        {
                                            var subsection = new MammalSubsection
                                            {
                                                Subtitle = GetString(subDict, "subtitle"),
                                                Text = GetString(subDict, "text")
                                            };

                                            if (subDict.ContainsKey("list_items") && subDict["list_items"] is List<object> subListItems)
                                            {
                                                subsection.ListItems = subListItems.Select(l => l?.ToString() ?? "").ToList();
                                            }

                                            if (subDict.ContainsKey("link") && subDict["link"] is Dictionary<object, object> linkDict)
                                            {
                                                subsection.Link = new LinkData
                                                {
                                                    Label = GetString(linkDict, "label"),
                                                    Url = GetString(linkDict, "url")
                                                };
                                            }

                                            section.Subsections.Add(subsection);
                                        }
                                    }
                                }


                                if (secDict.ContainsKey("facts") && secDict["facts"] is List<object> facts)
                                {
                                    section.Facts = facts.Select(f => f?.ToString() ?? "").ToList();
                                }


                                if (secDict.ContainsKey("list_items") && secDict["list_items"] is List<object> listItems)
                                {
                                    section.ListItems = listItems.Select(l => l?.ToString() ?? "").ToList();
                                }

                                mammalContent.Sections.Add(section);
                            }
                        }
                    }


                    if (data.ContainsKey("sources") && data["sources"] is List<object> sources)
                    {
                        foreach (var src in sources)
                        {
                            if (src is Dictionary<object, object> srcDict)
                            {
                                mammalContent.Sources.Add(new MammalSource
                                {
                                    Name = GetString(srcDict, "name"),
                                    Url = GetString(srcDict, "url"),
                                    Description = GetString(srcDict, "description")
                                });
                            }
                        }
                    }


                    if (data.ContainsKey("faq") && data["faq"] is List<object> faqList)
                    {
                        foreach (var faq in faqList)
                        {
                            if (faq is Dictionary<object, object> faqDict)
                            {
                                mammalContent.Faq.Add(new MammalFaq
                                {
                                    Question = GetString(faqDict, "question"),
                                    Answer = GetString(faqDict, "answer")
                                });
                            }
                        }
                    }




                    mammalContent.QuickFacts = new List<QuickFactItem>();

                    void AddFact(string label, string? value, bool italic = false)
                    {
                        if (string.IsNullOrWhiteSpace(value)) return;

                        mammalContent.QuickFacts.Add(new QuickFactItem
                        {
                            Label = label,
                            Value = value.Trim(),
                            Italic = italic
                        });
                    }

                    AddFact("Common name", mammalContent.CommonName);
                    AddFact("Scientific name", mammalContent.ScientificName, italic: true);
                    AddFact("Official since", mammalContent.OfficialSince);
                    AddFact("Status", mammalContent.Status);
                    AddFact("Habitat in state", mammalContent.HabitatInState);
                    AddFact("Known for", mammalContent.KnownFor);

                    if (mammalContent.AdoptedYear.HasValue && mammalContent.AdoptedYear.Value > 0)
                        AddFact("Designated", mammalContent.AdoptedYear.Value.ToString());

                    mammalContent.VisualAssets = YamlParse.VisualAssets(data);

                    return mammalContent;
                }
                catch
                {
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

        private DateTime? GetDate(Dictionary<object, object> dict, string key)
        {
            if (dict.ContainsKey(key) && DateTime.TryParse(dict[key]?.ToString(), out DateTime result))
                return result;
            return null;
        }
    }
}
