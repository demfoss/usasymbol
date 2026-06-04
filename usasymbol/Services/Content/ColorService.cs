using Microsoft.Extensions.Caching.Memory;
using usasymbol.Services.Interface;
using USASymbol.Models.Content;
using USASymbol.Models.ViewModels;
using USASymbol.Services.Yaml;
using YamlDotNet.Serialization;

namespace USASymbol.Services
{
    public class ColorService : IColorService
    {
        private readonly IMemoryCache _cache;
        private readonly IWebHostEnvironment _env;
        private readonly IDeserializer _yamlDeserializer;

        public ColorService(IMemoryCache cache, IWebHostEnvironment env)
        {
            _cache = cache;
            _env = env;
            _yamlDeserializer = new DeserializerBuilder().Build();
        }

        public async Task<ColorContent?> GetColorContentAsync(string stateSlug, string symbolSlug)
        {
            var cacheKey = $"color-{stateSlug}-{symbolSlug}";

            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromHours(24);




                var path = Path.Combine(_env.ContentRootPath, "Content", "states", stateSlug, "color.yaml");

                if (!File.Exists(path))
                    return null;

                var yaml = await File.ReadAllTextAsync(path);

                try
                {
                    var data = _yamlDeserializer.Deserialize<Dictionary<object, object>>(yaml);

                    var colorContent = new ColorContent
                    {

                        Title = GetString(data, "title"),
                        AdoptedYear = GetInt(data, "adopted_year"),


                        OfficialColors = GetString(data, "official_colors"),
                        OfficialSince = GetString(data, "official_since"),
                        PrimaryUse = GetString(data, "primary_use"),
                        KnownFor = GetString(data, "known_for"),


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




                    if (data.ContainsKey("color_data") && data["color_data"] is List<object> colorDataList)
                    {
                        foreach (var colorItem in colorDataList)
                        {
                            if (colorItem is Dictionary<object, object> colorDict)
                            {
                                colorContent.ColorData.Add(new ColorSpecification
                                {
                                    Name = GetString(colorDict, "name"),
                                    Hex = GetString(colorDict, "hex"),
                                    Rgb = GetString(colorDict, "rgb"),
                                    Cmyk = GetString(colorDict, "cmyk"),
                                    Pantone = GetString(colorDict, "pantone"),
                                    Symbolism = GetString(colorDict, "symbolism")
                                });
                            }
                        }
                    }






                    if (data.ContainsKey("quick_summary") && data["quick_summary"] is List<object> quickSummary)
                    {
                        colorContent.QuickSummary = quickSummary.Select(q => q?.ToString() ?? "").ToList();
                    }


                    colorContent.DidYouKnow = GetString(data, "did_you_know");


                    if (data.ContainsKey("big_stat") && data["big_stat"] is Dictionary<object, object> bigStatDict)
                    {
                        colorContent.BigStat = new BigStatData
                        {
                            Number = GetString(bigStatDict, "number"),
                            Description = GetString(bigStatDict, "description")
                        };
                    }


                    if (data.ContainsKey("timeline") && data["timeline"] is List<object> timeline)
                    {
                        foreach (var evt in timeline)
                        {
                            if (evt is Dictionary<object, object> evtDict)
                            {
                                colorContent.Timeline.Add(new TimelineEvent
                                {
                                    Year = GetString(evtDict, "year"),
                                    Description = GetString(evtDict, "description")
                                });
                            }
                        }
                    }


                    if (data.ContainsKey("expert_quote") && data["expert_quote"] is Dictionary<object, object> quoteDict)
                    {
                        colorContent.ExpertQuote = new ExpertQuoteData
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
                                var section = new ColorSection
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
                                    foreach (var sub in subsections)
                                    {
                                        if (sub is Dictionary<object, object> subDict)
                                        {
                                            var subsection = new ColorSubsection
                                            {
                                                Subtitle = GetString(subDict, "subtitle"),
                                                Text = GetString(subDict, "text")
                                            };

                                            if (subDict.ContainsKey("list_items") && subDict["list_items"] is List<object> subListItems)
                                            {
                                                subsection.ListItems = subListItems.Select(l => l?.ToString() ?? "").ToList();
                                            }

                                            section.Subsections.Add(subsection);
                                        }
                                    }
                                }

                                colorContent.Sections.Add(section);
                            }
                        }
                    }




                    if (data.ContainsKey("sources") && data["sources"] is List<object> sources)
                    {
                        foreach (var src in sources)
                        {
                            if (src is Dictionary<object, object> srcDict)
                            {
                                colorContent.Sources.Add(new ColorSource
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
                                colorContent.Faq.Add(new ColorFaq
                                {
                                    Question = GetString(faqDict, "question"),
                                    Answer = GetString(faqDict, "answer")
                                });
                            }
                        }
                    }




                    void AddFact(string label, string? value, bool italic = false)
                    {
                        if (string.IsNullOrWhiteSpace(value)) return;

                        colorContent.QuickFacts.Add(new QuickFactItem
                        {
                            Label = label,
                            Value = value.Trim(),
                            Italic = italic
                        });
                    }

                    AddFact("Official colors", colorContent.OfficialColors);
                    AddFact("Official since", colorContent.OfficialSince);
                    AddFact("Primary use", colorContent.PrimaryUse);
                    AddFact("Known for", colorContent.KnownFor);

                    colorContent.VisualAssets = YamlParse.VisualAssets(data);

                    return colorContent;
                }
                catch (Exception)
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
