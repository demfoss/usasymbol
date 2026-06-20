using Microsoft.Extensions.Caching.Memory;
using USASymbol.Models.Content;
using USASymbol.Models.ViewModels;
using USASymbol.Services.Interface;
using USASymbol.Services.Yaml;

namespace USASymbol.Services
{
    public class FossilService : IFossilService
    {
        private readonly IMemoryCache _cache;
        private readonly IWebHostEnvironment _env;

        public FossilService(IMemoryCache cache, IWebHostEnvironment env)
        {
            _cache = cache;
            _env = env;
        }

        public async Task<FossilContent?> GetFossilContentAsync(string stateSlug, string contentFileName = "fossil.yaml")
        {
            var normalizedFileName = string.IsNullOrWhiteSpace(contentFileName) ? "fossil.yaml" : contentFileName.Trim();
            var stateDir = Path.Combine(_env.ContentRootPath, "Content", "states", stateSlug);
            var path = Path.Combine(stateDir, normalizedFileName);

            if (!File.Exists(path))
            {
                var discovered = Directory.Exists(stateDir)
                    ? Directory.GetFiles(stateDir, "fossil*.yaml").OrderBy(f => f).FirstOrDefault()
                    : null;
                if (discovered == null)
                    return null;
                path = discovered;
                normalizedFileName = Path.GetFileName(discovered);
            }

            var cacheKey = $"fossil-content-{stateSlug}-{normalizedFileName}-{File.GetLastWriteTimeUtc(path).Ticks}";

            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromHours(24);
                var yaml = await File.ReadAllTextAsync(path);

                try
                {
                    var deserializer = new YamlDotNet.Serialization.DeserializerBuilder().Build();
                    var data = deserializer.Deserialize<Dictionary<object, object>>(yaml);

                    var content = new FossilContent
                    {
                        Type = GetString(data, "type"),
                        State = GetString(data, "state"),
                        StateFips = GetString(data, "state_fips"),
                        Name = GetString(data, "name"),
                        CommonName = GetString(data, "common_name"),
                        BinomialName = GetString(data, "binomial_name"),
                        FossilCategory = GetString(data, "fossil_category"),
                        AdoptedYear = GetInt(data, "adopted_year"),
                        GeologicalAge = GetString(data, "geological_age"),
                        LivedWhen = GetString(data, "lived_when"),
                        ExtinctWhen = GetString(data, "extinct_when"),
                        Length = GetString(data, "length"),
                        Diet = GetString(data, "diet"),
                        Author = GetString(data, "author"),
                        DatePublished = GetDate(data, "date_published"),
                        DateModified = GetDate(data, "date_modified"),
                        LastModified = File.GetLastWriteTime(path),
                        SeoTitle = GetString(data, "seo_title"),
                        SeoDescription = GetString(data, "seo_description"),
                        HeroImage = ResolveHeroImage(GetString(data, "hero_image"), _env.WebRootPath),
                        HeroImageAlt = GetString(data, "hero_image_alt"),
                        HeroImageCaption = GetString(data, "hero_image_caption"),
                        IntroText = GetString(data, "intro_text")
                    };

                    if (data.ContainsKey("sections") && data["sections"] is List<object> sections)
                    {
                        foreach (var sec in sections)
                        {
                            if (sec is not Dictionary<object, object> secDict)
                                continue;

                            var section = new FossilSection
                            {
                                Id = GetString(secDict, "id"),
                                Icon = GetString(secDict, "icon"),
                                Title = GetString(secDict, "title"),
                                Style = GetString(secDict, "style"),
                                Img = secDict.ContainsKey("img") ? GetString(secDict, "img") : null
                            };

                            if (secDict.ContainsKey("paragraphs") && secDict["paragraphs"] is List<object> paragraphs)
                                section.Paragraphs = paragraphs.OfType<string>().ToList();

                            if (secDict.ContainsKey("facts") && secDict["facts"] is List<object> facts)
                                section.Facts = facts.Select(f => f?.ToString() ?? "").ToList();

                            if (secDict.ContainsKey("sites") && secDict["sites"] is List<object> siteList)
                            {
                                foreach (var site in siteList)
                                {
                                    if (site is not Dictionary<object, object> siteDict)
                                        continue;

                                    section.Sites.Add(new FossilSite
                                    {
                                        Name = GetString(siteDict, "name"),
                                        City = GetString(siteDict, "city"),
                                        Lat = GetDouble(siteDict, "lat"),
                                        Lng = GetDouble(siteDict, "lng"),
                                        Note = GetString(siteDict, "note"),
                                        Type = GetString(siteDict, "type") is { Length: > 0 } t ? t : "primary"
                                    });
                                }
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
                                content.Faq.Add(new FossilFaq
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
                                content.Sources.Add(new FossilSource
                                {
                                    Name = GetString(srcDict, "name"),
                                    Url = GetString(srcDict, "url"),
                                    Description = GetString(srcDict, "description")
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

        private static string? ResolveHeroImage(string? path, string webRootPath)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;
            var filePath = System.IO.Path.Combine(webRootPath, path.TrimStart('/').Replace('/', System.IO.Path.DirectorySeparatorChar));
            return File.Exists(filePath) ? path : null;
        }

        private static string GetString(Dictionary<object, object> dict, string key)
            => dict.ContainsKey(key) ? dict[key]?.ToString() ?? "" : "";

        private static int? GetInt(Dictionary<object, object> dict, string key)
        {
            if (dict.ContainsKey(key) && int.TryParse(dict[key]?.ToString(), out int result))
                return result;
            return null;
        }

        private static double GetDouble(Dictionary<object, object> dict, string key)
        {
            if (dict.ContainsKey(key) && double.TryParse(
                    dict[key]?.ToString(),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out double result))
                return result;
            return 0;
        }

        private static bool GetBool(Dictionary<object, object> dict, string key)
        {
            if (dict.ContainsKey(key) && bool.TryParse(dict[key]?.ToString(), out bool result))
                return result;
            return false;
        }

        private static DateTime? GetDate(Dictionary<object, object> dict, string key)
        {
            if (dict.ContainsKey(key) && DateTime.TryParse(dict[key]?.ToString(), out DateTime result))
                return result;
            return null;
        }

        private static List<QuickFactItem> BuildQuickFacts(FossilContent content)
        {
            var facts = new List<QuickFactItem>();

            void Add(string label, string? value, bool italic = false)
            {
                if (string.IsNullOrWhiteSpace(value)) return;
                facts.Add(new QuickFactItem { Label = label, Value = value.Trim(), Italic = italic });
            }

            Add("Scientific Name", content.BinomialName, italic: true);
            Add("Category", content.FossilCategory);
            Add("Geological Age", content.GeologicalAge);
            if (content.AdoptedYear.HasValue && content.AdoptedYear.Value > 0)
                Add("Adopted", content.AdoptedYear.Value.ToString());
            Add("Diet", content.Diet);
            Add("Length", content.Length);
            Add("Extinct", content.ExtinctWhen);

            return facts;
        }
    }
}
