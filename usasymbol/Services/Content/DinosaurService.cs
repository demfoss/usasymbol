using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using usasymbol.Services.Interface;
using USASymbol.Models.Content;
using USASymbol.Models.ViewModels;
using YamlDotNet.Serialization;

namespace USASymbol.Services
{
    public class DinosaurService : IDinosaurService
    {
        private readonly IMemoryCache _cache;
        private readonly IWebHostEnvironment _env;
        private readonly IDeserializer _yamlDeserializer;

        public DinosaurService(IMemoryCache cache, IWebHostEnvironment env)
        {
            _cache = cache;
            _env = env;
            _yamlDeserializer = new DeserializerBuilder().Build();
        }

        public async Task<DinosaurContent?> GetDinosaurContentAsync(string stateSlug)
        {
            var cacheKey = $"dinosaur-{stateSlug}";

            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromHours(24);

                var path = Path.Combine(_env.ContentRootPath, "Content", "states", stateSlug, "dinosaur.yaml");
                if (!File.Exists(path))
                    return null;

                var yaml = await File.ReadAllTextAsync(path);

                try
                {
                    var data = _yamlDeserializer.Deserialize<Dictionary<object, object>>(yaml);

                    var content = new DinosaurContent
                    {
                        Title = GetString(data, "title"),
                        AdoptedYear = GetInt(data, "adopted_year"),
                        WikidataId = string.Empty,
                        Legislation = GetString(data, "legislation"),
                        Meaning = GetString(data, "meaning"),
                        ScientificName = GetString(data, "scientific_name"),
                        Period = GetString(data, "period"),
                        DiscoveredIn = GetString(data, "discovered_in"),
                        Diet = GetString(data, "diet"),
                        Length = GetString(data, "length"),
                        Weight = GetString(data, "weight"),
                        NamedBy = GetString(data, "named_by"),
                        FossilSites = GetString(data, "fossil_sites"),
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

                    if (data.TryGetValue("sections", out var sectionsObj) && sectionsObj is List<object> sections)
                    {
                        foreach (var sec in sections)
                        {
                            if (sec is not Dictionary<object, object> secDict) continue;

                            var section = new DinosaurSection
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
                                section.Subsections = new List<DinosaurSubsection>();

                                foreach (var sub in subsections)
                                {
                                    if (sub is not Dictionary<object, object> subDict) continue;

                                    var subsection = new DinosaurSubsection
                                    {
                                        Subtitle = GetString(subDict, "subtitle"),
                                        Text = GetString(subDict, "text"),
                                        Link = ParseLinkData(subDict, stateSlug)
                                    };

                                    if (subDict.TryGetValue("list", out var listObj) && listObj is List<object> list)
                                        subsection.ListItems = list.Select(l => l?.ToString() ?? "").ToList();

                                    section.Subsections.Add(subsection);
                                }
                            }

                            if (secDict.TryGetValue("facts", out var factsObj) && factsObj is List<object> facts)
                                section.Facts = facts.Select(f => f?.ToString() ?? "").ToList();

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
                            content.Sources.Add(new DinosaurSource
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
                            content.Faq.Add(new DinosaurFaq
                            {
                                Question = GetString(faq, "question"),
                                Answer = GetString(faq, "answer")
                            });
                        }
                    }

                    void AddFact(string label, string? value, bool italic = false)
                    {
                        if (string.IsNullOrWhiteSpace(value)) return;
                        content.QuickFacts.Add(new QuickFactItem { Label = label, Value = value.Trim(), Italic = italic });
                    }

                    AddFact("Scientific name", content.ScientificName, true);
                    AddFact("Period", content.Period);
                    AddFact("Diet", content.Diet);
                    AddFact("Length", content.Length);
                    AddFact("Weight", content.Weight);
                    AddFact("Discovered in", content.DiscoveredIn);
                    AddFact("Named by", content.NamedBy);
                    AddFact("Fossil sites", content.FossilSites);
                    AddFact("Legislation", content.Legislation);
                    if (content.AdoptedYear.HasValue && content.AdoptedYear.Value > 0)
                        AddFact("Adopted", content.AdoptedYear.Value.ToString());

                    content.VisualAssets = YamlParse.VisualAssets(data);

                    return content;
                }
                catch
                {
                    return null;
                }
            });
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
                var dinosaur = GetString(linkDict, "dinosaur_slug") ?? GetString(linkDict, "dinosaur");

                if (!string.IsNullOrWhiteSpace(state) && !string.IsNullOrWhiteSpace(dinosaur))
                    return new LinkData { Url = $"/states/{state.Trim()}/dinosaur/{dinosaur.Trim()}", Label = label };
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

            return $"/states/{currentStateSlug}/dinosaur/{s}";
        }

        private string GetString(Dictionary<object, object> dict, string key)
            => dict.ContainsKey(key) ? dict[key]?.ToString() ?? "" : "";

        private int? GetInt(Dictionary<object, object> dict, string key)
        {
            if (dict.ContainsKey(key) && int.TryParse(dict[key]?.ToString(), out var result))
                return result;
            return null;
        }

        private DateTime? GetDate(Dictionary<object, object> dict, string key)
        {
            if (dict.ContainsKey(key) && DateTime.TryParse(dict[key]?.ToString(), out var result))
                return result;
            return null;
        }
    }
}
