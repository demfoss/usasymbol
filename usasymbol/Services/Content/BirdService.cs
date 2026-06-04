using Microsoft.Extensions.Caching.Memory;
using usasymbol.Services.Interface;
using USASymbol.Models.Content;
using USASymbol.Models.ViewModels;
using YamlDotNet.Serialization;

namespace USASymbol.Services
{
    public class BirdService : IBirdService
    {
        private readonly IMemoryCache _cache;
        private readonly IWebHostEnvironment _env;
        private readonly IDeserializer _yamlDeserializer;

        public BirdService(IMemoryCache cache, IWebHostEnvironment env)
        {
            _cache = cache;
            _env = env;
            _yamlDeserializer = new DeserializerBuilder().Build();
        }

        public async Task<BirdContent?> GetBirdContentAsync(string stateSlug)
        {
            var cacheKey = $"bird-{stateSlug}";

            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromHours(24);

                var path = Path.Combine(_env.ContentRootPath, "Content", "states", stateSlug, "bird.yaml");

                if (!File.Exists(path))
                    return null;

                var yaml = await File.ReadAllTextAsync(path);

                try
                {
                    var data = _yamlDeserializer.Deserialize<Dictionary<object, object>>(yaml);

                    var birdContent = new BirdContent
                    {
                        Title = GetString(data, "title"),
                        ScientificName = GetString(data, "scientific_name"),
                        AdoptedYear = GetInt(data, "adopted_year"),
                        WikidataId = string.Empty,
                        Legislation = GetString(data, "legislation"),
                        Meaning = GetString(data, "meaning"),
                        ConservationStatus = GetString(data, "conservation_status"),
                        Habitat = GetString(data, "habitat"),
                        Size = GetString(data, "size"),
                        Wingspan = GetString(data, "wingspan"),
                        Weight = GetString(data, "weight"),
                        SharedStatesText = GetString(data, "shared_states_text"),
                        DistinctiveSong = GetString(data, "distinctive_song"),
                        Coloration = GetString(data, "coloration"),
                        ColorationDetails = GetString(data, "coloration_details"),
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


                    if (data.ContainsKey("shared_states") && data["shared_states"] is List<object> sharedStates)
                    {
                        birdContent.SharedStates = sharedStates.Select(s => s?.ToString() ?? "").ToList();
                    }


                    if (data.ContainsKey("sections") && data["sections"] is List<object> sections)
                    {
                        foreach (var sec in sections)
                        {
                            if (sec is Dictionary<object, object> secDict)
                            {
                                var section = new BirdSection
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
                                    section.Subsections = new List<BirdSubsection>();
                                    foreach (var sub in subsections)
                                    {
                                        if (sub is Dictionary<object, object> subDict)
                                        {
                                            var subsection = new BirdSubsection
                                            {
                                                Subtitle = GetString(subDict, "subtitle"),
                                                Text = GetString(subDict, "text"),
                                                Link = ParseLinkData(subDict, stateSlug)
                                            };

                                            if (subDict.ContainsKey("list") && subDict["list"] is List<object> list)
                                            {
                                                subsection.ListItems = list.Select(l => l?.ToString() ?? "").ToList();
                                            }

                                            section.Subsections.Add(subsection);
                                        }
                                    }
                                }


                                if (secDict.ContainsKey("facts") && secDict["facts"] is List<object> facts)
                                {
                                    section.Facts = facts.Select(f => f?.ToString() ?? "").ToList();
                                }

                                birdContent.Sections.Add(section);
                            }
                        }
                    }


                    if (data.TryGetValue("big_stat", out var bigStatObj) && bigStatObj is Dictionary<object, object> bigStatDict)
                    {
                        birdContent.BigStat = new BigStatData
                        {
                            Number = GetString(bigStatDict, "number"),
                            Description = GetString(bigStatDict, "description")
                        };
                    }


                    if (data.TryGetValue("timeline", out var timelineObj) && timelineObj is List<object> timelineList)
                    {
                        foreach (var evt in timelineList)
                        {
                            if (evt is not Dictionary<object, object> evtDict) continue;

                            birdContent.Timeline.Add(new TimelineEvent
                            {
                                Year = GetString(evtDict, "year"),
                                Description = GetString(evtDict, "description")
                            });
                        }
                    }


                    if (data.TryGetValue("expert_quote", out var quoteObj) && quoteObj is Dictionary<object, object> quoteDict)
                    {
                        birdContent.ExpertQuote = new ExpertQuoteData
                        {
                            Text = GetString(quoteDict, "text"),
                            Source = GetString(quoteDict, "source")
                        };
                    }


                    if (data.ContainsKey("sources") && data["sources"] is List<object> sources)
                    {
                        foreach (var src in sources)
                        {
                            if (src is Dictionary<object, object> srcDict)
                            {
                                birdContent.Sources.Add(new BirdSource
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
                                birdContent.Faq.Add(new BirdFaq
                                {
                                    Question = GetString(faqDict, "question"),
                                    Answer = GetString(faqDict, "answer")
                                });
                            }
                        }
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

                            birdContent.QuickFacts.Add(new QuickFactItem
                            {
                                Label = label.Trim(),
                                Value = value.Trim(),
                                Url = GetString(factDict, "url"),
                                Italic = GetBool(factDict, "italic")
                            });
                        }
                    }


                    if (birdContent.QuickFacts.Count == 0)
                    {
                        void AddFact(string label, string? value, bool italic = false)
                        {
                            if (string.IsNullOrWhiteSpace(value)) return;
                            birdContent.QuickFacts.Add(new QuickFactItem
                            {
                                Label = label,
                                Value = value.Trim(),
                                Italic = italic
                            });
                        }

                        AddFact("Scientific name", birdContent.ScientificName, italic: true);
                        if (birdContent.AdoptedYear.HasValue && birdContent.AdoptedYear.Value > 0)
                            AddFact("Adopted", birdContent.AdoptedYear.Value.ToString());
                        AddFact("Legal reference", birdContent.Legislation);
                        AddFact("Recognition", birdContent.Coloration);
                    }

                    birdContent.VisualAssets = YamlParse.VisualAssets(data);

                    return birdContent;
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

        private bool GetBool(Dictionary<object, object> dict, string key)
        {
            if (!dict.ContainsKey(key) || dict[key] is null)
                return false;

            if (dict[key] is bool boolValue)
                return boolValue;

            return bool.TryParse(dict[key]?.ToString(), out var parsed) && parsed;
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

                var state = FirstNonEmpty(
                    GetString(linkDict, "state_slug"),
                    GetString(linkDict, "state"),
                    currentStateSlug);

                var bird = FirstNonEmpty(
                    GetString(linkDict, "bird_slug"),
                    GetString(linkDict, "bird"));

                if (!string.IsNullOrWhiteSpace(state) && !string.IsNullOrWhiteSpace(bird))
                {
                    return new LinkData
                    {
                        Url = $"/states/{state.Trim()}/bird/{bird.Trim()}",
                        Label = label
                    };
                }
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

            return $"/states/{currentStateSlug}/bird/{s}";
        }

        private string FirstNonEmpty(params string[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }

            return string.Empty;
        }
    }
}
