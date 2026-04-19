using Microsoft.Extensions.Caching.Memory;
using usasymbol.Services.Interface;
using USASymbol.Models.Content;
using USASymbol.Models.ViewModels;
using YamlDotNet.Serialization;

namespace USASymbol.Services
{
    public class TreeService : ITreeService
    {
        private readonly IMemoryCache _cache;
        private readonly IWebHostEnvironment _env;
        private readonly IDeserializer _yamlDeserializer;

        public TreeService(IMemoryCache cache, IWebHostEnvironment env)
        {
            _cache = cache;
            _env = env;
            _yamlDeserializer = new DeserializerBuilder().Build();
        }

        public async Task<TreeContent?> GetTreeContentAsync(string stateSlug)
        {
            var path = Path.Combine(_env.ContentRootPath, "Content", "states", stateSlug, "tree.yaml");

            if (!File.Exists(path))
                return null;

            var cacheKey = $"tree-{stateSlug}-{File.GetLastWriteTimeUtc(path).Ticks}";

            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromHours(24);

                var yaml = await File.ReadAllTextAsync(path);

                try
                {
                    var data = _yamlDeserializer.Deserialize<Dictionary<object, object>>(yaml);

                    var treeContent = new TreeContent
                    {
                        Type = GetString(data, "type"),
                        State = GetString(data, "state"),
                        Name = GetString(data, "name"),
                        ScientificName = GetString(data, "scientific_name"),
                        AdoptedYear = GetInt(data, "adopted_year"),
                        IsOfficial = GetBool(data, "is_official"),
                        WikidataId = string.Empty,
                        Legislation = GetString(data, "legislation"),
                        Meaning = GetString(data, "meaning"),
                        Author = GetString(data, "author"),
                        DatePublished = GetDate(data, "date_published"),
                        DateModified = GetDate(data, "date_modified"),
                        LastModified = File.GetLastWriteTime(path),
                        IntroText = GetString(data, "intro_text"),
                        BigStatAfterSectionId = GetString(data, "big_stat_after_section"),
                        TimelineAfterSectionId = GetString(data, "timeline_after_section"),
                        ExpertQuoteAfterSectionId = GetString(data, "expert_quote_after_section")
                    };


                    if (data.ContainsKey("seo") && data["seo"] is Dictionary<object, object> seo)
                    {
                        treeContent.SeoTitle = GetString(seo, "title");
                        treeContent.SeoDescription = GetString(seo, "description");
                    }
                    else
                    {
                        treeContent.SeoTitle = GetString(data, "seo_title");
                        treeContent.SeoDescription = GetString(data, "seo_description");
                    }


                    if (data.ContainsKey("sections") && data["sections"] is List<object> sections)
                    {
                        foreach (var sec in sections)
                        {
                            if (sec is Dictionary<object, object> secDict)
                            {
                                var section = new TreeSection
                                {
                                    Id = GetString(secDict, "id"),
                                    Icon = GetString(secDict, "icon"),
                                    Title = GetString(secDict, "title"),
                                    Style = GetString(secDict, "style"),
                                    Content = BuildSectionContent(secDict),
                                    Img   = secDict.ContainsKey("img") ? GetString(secDict, "img") : null
                                };


                                if (secDict.ContainsKey("paragraphs") && secDict["paragraphs"] is List<object> paragraphs)
                                {
                                    section.Paragraphs = paragraphs.Select(p => p?.ToString() ?? "").ToList();
                                }


                                if (secDict.ContainsKey("facts") && secDict["facts"] is List<object> facts)
                                {
                                    section.Facts = facts.Select(f => f?.ToString() ?? "").ToList();
                                }


                                if (secDict.ContainsKey("subsections") && secDict["subsections"] is List<object> subsections)
                                {
                                    var treeSubsections = new List<TreeSubsection>();

                                    section.Subsections = treeSubsections.Cast<IContentSubsection>().ToList();

                                    foreach (var sub in subsections)
                                    {
                                        if (sub is Dictionary<object, object> subDict)
                                        {
                                            var subsection = new TreeSubsection
                                            {
                                                Subtitle = GetString(subDict, "subtitle"),
                                                Text = GetString(subDict, "text")
                                            };

                                            if (subDict.ContainsKey("list") && subDict["list"] is List<object> list)
                                            {
                                                subsection.ListItems = list.Select(l => l?.ToString() ?? "").ToList();
                                            }

                                            section.Subsections.Add(subsection);
                                        }
                                    }
                                }

                                treeContent.Sections.Add(section);
                            }
                        }
                    }


                    if (data.ContainsKey("faq") && data["faq"] is List<object> faqList)
                    {
                        foreach (var faq in faqList)
                        {
                            if (faq is Dictionary<object, object> faqDict)
                            {
                                treeContent.Faq.Add(new TreeFaq
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
                                treeContent.Sources.Add(new TreeSource
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
                        treeContent.BigStat = new BigStatData
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

                            treeContent.Timeline.Add(new TimelineEvent
                            {
                                Year = year,
                                Description = description
                            });
                        }
                    }

                    if (data.TryGetValue("expert_quote", out var expertQuoteObj) && expertQuoteObj is Dictionary<object, object> expertQuoteDict)
                    {
                        treeContent.ExpertQuote = new ExpertQuoteData
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

                            treeContent.QuickFacts.Add(new QuickFactItem
                            {
                                Label = label.Trim(),
                                Value = value.Trim(),
                                Url = GetString(factDict, "url"),
                                Italic = GetBool(factDict, "italic")
                            });
                        }
                    }

                    if (treeContent.QuickFacts.Count == 0)
                    {
                        treeContent.QuickFacts = BuildQuickFacts(treeContent);
                    }

                    treeContent.VisualAssets = YamlParse.VisualAssets(data);
                 
                    return treeContent;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing tree.yaml for {stateSlug}: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                    return null;
                }
            });
        }

        private string BuildSectionContent(Dictionary<object, object> secDict)
        {
            var contentParts = new List<string>();

            var directContent = GetString(secDict, "content");
            if (!string.IsNullOrEmpty(directContent))
            {
                contentParts.Add(directContent);
            }

            if (secDict.ContainsKey("paragraphs") && secDict["paragraphs"] is List<object> paragraphs)
            {
                foreach (var p in paragraphs)
                {
                    if (p != null && !string.IsNullOrWhiteSpace(p.ToString()))
                    {
                        contentParts.Add($"<p>{p.ToString()}</p>");
                    }
                }
            }

            if (secDict.ContainsKey("subsections") && secDict["subsections"] is List<object> subsections)
            {
                foreach (var sub in subsections)
                {
                    if (sub is Dictionary<object, object> subDict)
                    {
                        var subtitle = GetString(subDict, "subtitle");
                        var text = GetString(subDict, "text");

                        if (!string.IsNullOrEmpty(subtitle))
                        {
                            contentParts.Add($"<h3 class='text-xl font-bold mt-6 mb-3'>{subtitle}</h3>");
                        }
                        if (!string.IsNullOrEmpty(text))
                        {
                            contentParts.Add($"<p>{text}</p>");
                        }
                    }
                }
            }

            return string.Join("\n", contentParts);
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

        private static List<QuickFactItem> BuildQuickFacts(TreeContent content)
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

            AddFact("Scientific name", content.ScientificName, italic: true);

            if (content.AdoptedYear.HasValue && content.AdoptedYear.Value > 0)
                AddFact("Adopted", content.AdoptedYear.Value.ToString());

            AddFact("Status", content.IsOfficial ? "Official symbol" : "State tree");
            AddFact("Legislation", content.Legislation);

            return facts;
        }
    }
}
