using Microsoft.Extensions.Caching.Memory;
using USASymbol.Models.Content;
using USASymbol.Models.ViewModels;
using USASymbol.Services.Interface;
using YamlDotNet.Serialization;

namespace USASymbol.Services
{
    public class SoilService : ISoilService
    {
        private readonly IMemoryCache _cache;
        private readonly IWebHostEnvironment _env;
        private readonly IDeserializer _yamlDeserializer;

        public SoilService(IMemoryCache cache, IWebHostEnvironment env)
        {
            _cache = cache;
            _env = env;
            _yamlDeserializer = new DeserializerBuilder().Build();
        }

        public async Task<SoilContent?> GetSoilContentAsync(string stateSlug, string contentFileName = "soil.yaml")
        {
            var normalizedFileName = string.IsNullOrWhiteSpace(contentFileName) ? "soil.yaml" : contentFileName.Trim();
            var path = Path.Combine(_env.ContentRootPath, "Content", "states", stateSlug, normalizedFileName);

            if (!File.Exists(path))
                return null;

            var cacheKey = $"soil-content-{stateSlug}-{normalizedFileName}-{File.GetLastWriteTimeUtc(path).Ticks}";

            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromHours(24);
                var yaml = await File.ReadAllTextAsync(path);

                try
                {
                    var data = _yamlDeserializer.Deserialize<Dictionary<object, object>>(yaml);

                    var content = new SoilContent
                    {
                        Type = GetString(data, "type"),
                        State = GetString(data, "state"),
                        StateFips = GetString(data, "state_fips"),
                        Name = !string.IsNullOrWhiteSpace(GetString(data, "name")) ? GetString(data, "name") : GetString(data, "title"),
                        AdoptedYear = GetInt(data, "adopted_year"),
                        IsOfficial = GetBool(data, "is_official"),
                        Legislation = GetString(data, "legislation"),
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
                        ExpertQuoteAfterSectionId = GetString(data, "expert_quote_after_section")
                    };

                    if (data.ContainsKey("sections") && data["sections"] is List<object> sections)
                    {
                        foreach (var sec in sections)
                        {
                            if (sec is not Dictionary<object, object> secDict)
                                continue;

                            var section = new SoilSection
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
                                var soilSubsections = new List<SoilSubsection>();

                                foreach (var sub in subsections)
                                {
                                    if (sub is not Dictionary<object, object> subDict)
                                        continue;

                                    var subsection = new SoilSubsection
                                    {
                                        Subtitle = GetString(subDict, "subtitle"),
                                        Text = GetString(subDict, "text")
                                    };

                                    if (subDict.ContainsKey("list") && subDict["list"] is List<object> list)
                                        subsection.ListItems = list.Select(l => l?.ToString() ?? "").ToList();

                                    soilSubsections.Add(subsection);
                                }

                                section.Subsections = soilSubsections.Cast<IContentSubsection>().ToList();
                            }

                            // Soil horizon layers
                            if (secDict.ContainsKey("layers") && secDict["layers"] is List<object> layerList)
                            {
                                foreach (var layer in layerList)
                                {
                                    if (layer is not Dictionary<object, object> layerDict)
                                        continue;
                                    section.Layers.Add(new SoilLayer
                                    {
                                        Horizon = GetString(layerDict, "horizon"),
                                        Name = GetString(layerDict, "name"),
                                        DepthIn = GetString(layerDict, "depth_in"),
                                        ColorHex = GetString(layerDict, "color_hex"),
                                        ColorName = GetString(layerDict, "color_name"),
                                        Texture = GetString(layerDict, "texture"),
                                        Note = GetString(layerDict, "note")
                                    });
                                }
                            }

                            // County distribution
                            if (secDict.ContainsKey("counties") && secDict["counties"] is List<object> countyList)
                            {
                                section.Counties = countyList
                                    .Select(c => c?.ToString() ?? "")
                                    .Where(c => !string.IsNullOrWhiteSpace(c))
                                    .ToList();
                            }

                            content.Sections.Add(section);
                        }
                    }

                    if (data.ContainsKey("faq") && data["faq"] is List<object> faqList)
                    {
                        foreach (var faq in faqList)
                        {
                            if (faq is Dictionary<object, object> faqDict)
                            {
                                content.Faq.Add(new SoilFaq
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
                                content.Sources.Add(new SoilSource
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
                        content.BigStat = new BigStatData
                        {
                            Number = GetString(bigStatDict, "number"),
                            Description = GetString(bigStatDict, "description")
                        };
                    }

                    if (data.TryGetValue("expert_quote", out var expertQuoteObj) && expertQuoteObj is Dictionary<object, object> expertQuoteDict)
                    {
                        content.ExpertQuote = new ExpertQuoteData
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
                            if (string.IsNullOrWhiteSpace(label) || string.IsNullOrWhiteSpace(value)) continue;
                            content.QuickFacts.Add(new QuickFactItem
                            {
                                Label = label.Trim(),
                                Value = value.Trim(),
                                Url = GetString(factDict, "url"),
                                Italic = GetBool(factDict, "italic")
                            });
                        }
                    }

                    if (content.QuickFacts.Count == 0)
                        content.QuickFacts = BuildQuickFacts(content);

                    content.VisualAssets = YamlParse.VisualAssets(data);

                    return content;
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

        private static List<QuickFactItem> BuildQuickFacts(SoilContent content)
        {
            var facts = new List<QuickFactItem>();

            void AddFact(string label, string? value, bool italic = false)
            {
                if (string.IsNullOrWhiteSpace(value)) return;
                facts.Add(new QuickFactItem { Label = label, Value = value.Trim(), Italic = italic });
            }

            if (content.AdoptedYear.HasValue && content.AdoptedYear.Value > 0)
                AddFact("Adopted", content.AdoptedYear.Value.ToString());

            AddFact("Status", content.IsOfficial ? "Official state soil" : "state soil");
            AddFact("Legislation", content.Legislation);

            return facts;
        }
    }
}
