using Microsoft.Extensions.Caching.Memory;
using usasymbol.Services.Interface;
using USASymbol.Models.Content;
using USASymbol.Models.ViewModels;
using YamlDotNet.Serialization;

namespace USASymbol.Services
{
    public class NicknameService : INicknameService
    {
        private readonly IMemoryCache _cache;
        private readonly IWebHostEnvironment _env;
        private readonly IDeserializer _yamlDeserializer;

        public NicknameService(IMemoryCache cache, IWebHostEnvironment env)
        {
            _cache = cache;
            _env = env;
            _yamlDeserializer = new DeserializerBuilder().Build();
        }

        public async Task<NicknameContent?> GetNicknameContentAsync(string stateSlug)
        {
            var cacheKey = $"nickname-{stateSlug}";

            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromHours(24);

                var path = Path.Combine(_env.ContentRootPath, "Content", "states", stateSlug, "nickname.yaml");

                if (!File.Exists(path))
                    return null;

                var yaml = await File.ReadAllTextAsync(path);

                try
                {
                    var data = _yamlDeserializer.Deserialize<Dictionary<object, object>>(yaml);

                    var nicknameContent = new NicknameContent
                    {
                        Title = GetString(data, "title"),
                        State = GetString(data, "state"),
                        Type = GetString(data, "symbol_type"),
                        IsOfficial = GetBool(data, "is_official"),
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
                        ExpertQuoteAfterSectionId = GetString(data, "expert_quote_after_section"),
                        ComparisonCardsAfterSectionId = GetString(data, "comparison_cards_after_section")
                    };

                    if (data.ContainsKey("big_stat") && data["big_stat"] is Dictionary<object, object> bigStatDict)
                    {
                        nicknameContent.BigStat = new BigStatData
                        {
                            Number = GetString(bigStatDict, "number"),
                            Description = GetString(bigStatDict, "description")
                        };
                    }

                    if (data.ContainsKey("timeline") && data["timeline"] is List<object> timelineList)
                    {
                        foreach (var timeline in timelineList)
                        {
                            if (timeline is Dictionary<object, object> timelineDict)
                            {
                                nicknameContent.Timeline.Add(new TimelineEvent
                                {
                                    Year = GetString(timelineDict, "year"),
                                    Description = GetString(timelineDict, "description")
                                });
                            }
                        }
                    }

                    if (data.ContainsKey("expert_quote") && data["expert_quote"] is Dictionary<object, object> quoteDict)
                    {
                        nicknameContent.ExpertQuote = new ExpertQuoteData
                        {
                            Text = GetString(quoteDict, "text"),
                            Source = GetString(quoteDict, "source")
                        };
                    }


                    if (data.ContainsKey("other_nicknames") && data["other_nicknames"] is List<object> otherNicknames)
                    {
                        nicknameContent.OtherNicknames = otherNicknames.Select(n => n?.ToString() ?? "").ToList();
                    }


                    if (data.ContainsKey("sections") && data["sections"] is List<object> sections)
                    {
                        foreach (var sec in sections)
                        {
                            if (sec is Dictionary<object, object> secDict)
                            {
                                var section = new NicknameSection
                                {
                                    Id = GetString(secDict, "id"),
                                    Icon = GetString(secDict, "icon"),
                                    Title = GetString(secDict, "title"),
                                    Style = GetString(secDict, "style"),
                                    Img   = secDict.ContainsKey("img") ? GetString(secDict, "img") : null
                                };


                                if (secDict.ContainsKey("paragraphs") && secDict["paragraphs"] is List<object> paragraphs)
                                {
                                    section.Paragraphs = paragraphs.OfType<string>().ToList();
                                }


                                if (secDict.ContainsKey("subsections") && secDict["subsections"] is List<object> subsections)
                                {
                                    var nicknameSubsections = new List<NicknameSubsection>();

                                    section.Subsections = nicknameSubsections.Cast<IContentSubsection>().ToList();

                                    foreach (var sub in subsections)
                                    {
                                        if (sub is Dictionary<object, object> subDict)
                                        {
                                            var subsection = new NicknameSubsection
                                            {
                                                Subtitle = GetString(subDict, "subtitle"),
                                                Text = GetString(subDict, "text")
                                            };

                                            if (subDict.ContainsKey("list") && subDict["list"] is List<object> list)
                                            {
                                                subsection.ListItems = list.Select(l => l?.ToString() ?? "").ToList();
                                            }

                                            if (subDict.ContainsKey("list_items") && subDict["list_items"] is List<object> listItems)
                                            {
                                                subsection.ListItems = listItems.Select(l => l?.ToString() ?? "").ToList();
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

                                nicknameContent.Sections.Add(section);
                            }
                        }
                    }

                    if (data.ContainsKey("quick_facts") && data["quick_facts"] is List<object> quickFacts)
                    {
                        foreach (var fact in quickFacts)
                        {
                            if (fact is Dictionary<object, object> factDict)
                            {
                                nicknameContent.QuickFacts.Add(new QuickFactItem
                                {
                                    Label = GetString(factDict, "label"),
                                    Value = GetString(factDict, "value"),
                                    Italic = GetBool(factDict, "italic")
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
                                nicknameContent.Sources.Add(new NicknameSource
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
                                nicknameContent.Faq.Add(new NicknameFaq
                                {
                                    Question = GetString(faqDict, "question"),
                                    Answer = GetString(faqDict, "answer")
                                });
                            }
                        }
                    }



                    nicknameContent.VisualAssets = YamlParse.VisualAssets(data);
                    nicknameContent.ComparisonCards = YamlParse.ComparisonCards(data);

                    if (nicknameContent.ComparisonCards != null &&
                        nicknameContent.ComparisonCards.Cards.Any() &&
                        string.IsNullOrWhiteSpace(nicknameContent.ComparisonCardsAfterSectionId))
                    {
                        nicknameContent.ComparisonCardsAfterSectionId = nicknameContent.Sections.LastOrDefault()?.Id ?? string.Empty;
                    }

                    return nicknameContent;
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
    }
}
