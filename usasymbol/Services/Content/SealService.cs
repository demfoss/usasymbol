using Microsoft.Extensions.Caching.Memory;
using usasymbol.Services.Interface;
using USASymbol.Models.Content;
using USASymbol.Models.ViewModels;
using YamlDotNet.Serialization;

namespace USASymbol.Services
{
    public class SealService : ISealService
    {
        private readonly IMemoryCache _cache;
        private readonly IWebHostEnvironment _env;
        private readonly IDeserializer _yamlDeserializer;

        public SealService(IMemoryCache cache, IWebHostEnvironment env)
        {
            _cache = cache;
            _env = env;
            _yamlDeserializer = new DeserializerBuilder().Build();
        }

        public async Task<SealContent?> GetSealContentAsync(string stateSlug, string contentFileName = "state-seal.yaml")
        {
            var normalizedFileName = string.IsNullOrWhiteSpace(contentFileName)
                ? "state-seal.yaml"
                : contentFileName.Trim();
            var path = Path.Combine(_env.ContentRootPath, "Content", "states", stateSlug, normalizedFileName);

            if (!File.Exists(path))
                return null;

            var cacheKey = $"seal-content-{stateSlug}-{normalizedFileName}-{File.GetLastWriteTimeUtc(path).Ticks}";

            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromHours(24);

                var yaml = await File.ReadAllTextAsync(path);

                try
                {
                    var data = _yamlDeserializer.Deserialize<Dictionary<object, object>>(yaml);

                    var sealContent = new SealContent
                    {
                        Type = GetString(data, "type"),
                        State = GetString(data, "state"),
                        Name = !string.IsNullOrWhiteSpace(GetString(data, "name")) ? GetString(data, "name") : GetString(data, "title"),
                        AdoptedYear = GetInt(data, "adopted_year"),
                        RevisedYear = GetInt(data, "revised_year"),
                        IsOfficial = GetBool(data, "is_official"),
                        WikidataId = string.Empty,
                        Legislation = GetString(data, "legislation"),
                        Meaning = GetString(data, "meaning"),
                        MeaningAfterSectionId = GetString(data, "meaning_after_section"),
                        MeaningTitle = GetString(data, "meaning_title"),
                        Author = GetString(data, "author"),
                        DatePublished = GetDate(data, "date_published"),
                        DateModified = GetDate(data, "date_modified"),
                        LastModified = File.GetLastWriteTime(path),
                        SeoTitle = GetString(data, "seo_title"),
                        SeoDescription = GetString(data, "seo_description"),
                        HeroImage = GetString(data, "hero_image"),
                        HeroImageAlt = GetString(data, "hero_image_alt"),
                        HeroImageCaption = GetString(data, "hero_image_caption"),
                        IntroText = GetString(data, "intro_text"),
                        BigStatAfterSectionId = GetString(data, "big_stat_after_section"),
                        TimelineAfterSectionId = GetString(data, "timeline_after_section"),
                        ExpertQuoteAfterSectionId = GetString(data, "expert_quote_after_section")
                    };

                    if (data.ContainsKey("sections") && data["sections"] is List<object> sections)
                    {
                        foreach (var sec in sections)
                        {
                            if (sec is not Dictionary<object, object> secDict)
                                continue;

                            var section = new SealSection
                            {
                                Id = GetString(secDict, "id"),
                                Icon = GetString(secDict, "icon"),
                                Style = GetString(secDict, "style"),
                                Title = GetString(secDict, "title"),
                                Img = secDict.ContainsKey("img") ? GetString(secDict, "img") : null
                            };

                            if (secDict.ContainsKey("paragraphs") && secDict["paragraphs"] is List<object> paragraphs)
                                section.Paragraphs = paragraphs.OfType<string>().ToList();

                            if (secDict.ContainsKey("facts") && secDict["facts"] is List<object> facts)
                                section.Facts = facts.Select(f => f?.ToString() ?? "").ToList();

                            if (secDict.ContainsKey("subsections") && secDict["subsections"] is List<object> subsections)
                            {
                                var sealSubsections = new List<SealSubsection>();
                                section.Subsections = sealSubsections.Cast<IContentSubsection>().ToList();

                                foreach (var sub in subsections)
                                {
                                    if (sub is not Dictionary<object, object> subDict)
                                        continue;

                                    var subsection = new SealSubsection
                                    {
                                        Subtitle = GetString(subDict, "subtitle"),
                                        Text = GetString(subDict, "text")
                                    };

                                    if (subDict.ContainsKey("list") && subDict["list"] is List<object> list)
                                        subsection.ListItems = list.Select(l => l?.ToString() ?? "").ToList();

                                    sealSubsections.Add(subsection);
                                }
                            }

                            if (secDict.ContainsKey("versions") && secDict["versions"] is List<object> versions)
                            {
                                foreach (var ver in versions)
                                {
                                    if (ver is Dictionary<object, object> verDict)
                                    {
                                        section.Versions.Add(new SealVersion
                                        {
                                            Name = GetString(verDict, "name"),
                                            Years = GetString(verDict, "years"),
                                            Image = GetString(verDict, "image"),
                                            Description = GetString(verDict, "description")
                                        });
                                    }
                                }
                            }

                            if (TryGetList(secDict, "elements", out var elements) || TryGetList(secDict, "symbols", out elements))
                            {
                                foreach (var el in elements)
                                {
                                    if (el is not Dictionary<object, object> elDict)
                                        continue;

                                    var element = new SealElement
                                    {
                                        Id = GetString(elDict, "id"),
                                        Name = GetString(elDict, "name"),
                                        Image = GetString(elDict, "image")
                                    };

                                    if (elDict.ContainsKey("paragraphs") && elDict["paragraphs"] is List<object> elParagraphs)
                                        element.Paragraphs = elParagraphs.Select(p => p?.ToString() ?? "").ToList();

                                    section.Elements.Add(element);
                                }
                            }

                            sealContent.Sections.Add(section);
                        }
                    }

                    if (data.ContainsKey("faq") && data["faq"] is List<object> faqList)
                    {
                        foreach (var faq in faqList)
                        {
                            if (faq is Dictionary<object, object> faqDict)
                            {
                                sealContent.Faq.Add(new SealFaq
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
                                sealContent.Sources.Add(new SealSource
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
                        sealContent.BigStat = new BigStatData
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

                            sealContent.Timeline.Add(new TimelineEvent
                            {
                                Year = year,
                                Description = description
                            });
                        }

                        if (sealContent.Timeline.Count > 0 && string.IsNullOrWhiteSpace(sealContent.TimelineAfterSectionId))
                        {
                            sealContent.TimelineAfterSectionId =
                                sealContent.Sections.FirstOrDefault(section => string.Equals(section.Id, "history", StringComparison.OrdinalIgnoreCase))?.Id
                                ?? sealContent.Sections.FirstOrDefault()?.Id
                                ?? string.Empty;
                        }
                    }

                    if (data.TryGetValue("expert_quote", out var expertQuoteObj) && expertQuoteObj is Dictionary<object, object> expertQuoteDict)
                    {
                        sealContent.ExpertQuote = new ExpertQuoteData
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

                            sealContent.QuickFacts.Add(new QuickFactItem
                            {
                                Label = label.Trim(),
                                Value = value.Trim(),
                                Url = GetString(factDict, "url"),
                                Italic = GetBool(factDict, "italic")
                            });
                        }
                    }

                    if (sealContent.QuickFacts.Count == 0)
                        sealContent.QuickFacts = BuildQuickFacts(sealContent);

                    if (string.IsNullOrWhiteSpace(sealContent.MeaningTitle) && !string.IsNullOrWhiteSpace(sealContent.Meaning))
                    {
                        var title = !string.IsNullOrWhiteSpace(sealContent.Name)
                            ? sealContent.Name
                            : sealContent.Type;
                        sealContent.MeaningTitle = string.IsNullOrWhiteSpace(title) ? "Meaning" : $"{title} Meaning";
                    }

                    if (string.IsNullOrWhiteSpace(sealContent.MeaningAfterSectionId))
                    {
                        sealContent.MeaningAfterSectionId =
                            sealContent.Sections.FirstOrDefault(section => string.Equals(section.Id, "history", StringComparison.OrdinalIgnoreCase))?.Id
                            ?? sealContent.Sections.FirstOrDefault()?.Id
                            ?? string.Empty;
                    }

                    sealContent.VisualAssets = YamlParse.VisualAssets(data);

                    return sealContent;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing {normalizedFileName} for {stateSlug}: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                    return null;
                }
            });
        }

        private string GetString(Dictionary<object, object> dict, string key)
            => dict.ContainsKey(key) ? dict[key]?.ToString() ?? "" : "";

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

        private static List<QuickFactItem> BuildQuickFacts(SealContent content)
        {
            var facts = new List<QuickFactItem>();

            void AddFact(string label, string? value, bool italic = false)
            {
                if (string.IsNullOrWhiteSpace(value)) return;
                facts.Add(new QuickFactItem { Label = label, Value = value.Trim(), Italic = italic });
            }

            if (content.AdoptedYear.HasValue && content.AdoptedYear.Value > 0)
                AddFact("Adopted", content.AdoptedYear.Value.ToString());

            if (content.RevisedYear.HasValue && content.RevisedYear.Value > 0)
                AddFact("Last revised", content.RevisedYear.Value.ToString());

            var typeLabel = string.IsNullOrWhiteSpace(content.Type)
                ? "seal"
                : content.Type.Trim().ToLowerInvariant();
            AddFact("Status", content.IsOfficial ? $"Official {typeLabel}" : typeLabel);
            AddFact("Legislation", content.Legislation);

            return facts;
        }
    }
}
