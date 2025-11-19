using Microsoft.Extensions.Caching.Memory;
using USASymbol.Models.Content;
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
                        LastModified = File.GetLastWriteTime(path)
                    };

                    // Parse shared_states
                    if (data.ContainsKey("shared_states") && data["shared_states"] is List<object> sharedStates)
                    {
                        birdContent.SharedStates = sharedStates.Select(s => s?.ToString() ?? "").ToList();
                    }

                    // Parse sections
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
                                    Style = GetString(secDict, "style")
                                };

                                // Paragraphs
                                if (secDict.ContainsKey("paragraphs") && secDict["paragraphs"] is List<object> paragraphs)
                                {
                                    section.Paragraphs = paragraphs.Select(p => p?.ToString() ?? "").ToList();
                                }

                                // Subsections
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

                                // Facts
                                if (secDict.ContainsKey("facts") && secDict["facts"] is List<object> facts)
                                {
                                    section.Facts = facts.Select(f => f?.ToString() ?? "").ToList();
                                }

                                birdContent.Sections.Add(section);
                            }
                        }
                    }

                    // Parse sources
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

                    // Parse FAQ
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

                    return birdContent;
                }
                catch
                {
                    return null;
                }
            });
        }

        // Helper methods
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