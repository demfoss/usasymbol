using USASymbol.Models.Content;
using USASymbol.Services.Interface;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace USASymbol.Services
{
    public class SurnamesService : ISurnamesService
    {
        private readonly ILogger<SurnamesService> _logger;
        private readonly string _contentBasePath;

        public SurnamesService(ILogger<SurnamesService> logger, IWebHostEnvironment env)
        {
            _logger = logger;
            _contentBasePath = Path.Combine(env.ContentRootPath, "Content", "states");
        }

        public IReadOnlyList<string> GetAvailableSlugs()
        {
            if (!Directory.Exists(_contentBasePath)) return [];
            return Directory
                .GetDirectories(_contentBasePath)
                .Where(d => File.Exists(Path.Combine(d, "surnames.yaml")))
                .Select(Path.GetFileName)
                .OfType<string>()
                .OrderBy(s => s)
                .ToList();
        }

        public async Task<SurnamesContent?> GetSurnamesContentAsync(string stateSlug)
        {
            try
            {
                var filePath = Path.Combine(_contentBasePath, stateSlug, "surnames.yaml");
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("Surnames file not found: {Path}", filePath);
                    return null;
                }

                var yaml = await File.ReadAllTextAsync(filePath);
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .IgnoreUnmatchedProperties()
                    .Build();

                var raw = deserializer.Deserialize<Dictionary<string, object>>(yaml);
                return Map(raw);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading surnames for {StateSlug}", stateSlug);
                return null;
            }
        }

        private static SurnamesContent Map(Dictionary<string, object> raw)
        {
            var content = new SurnamesContent
            {
                State = GetString(raw, "state"),
                StateSlug = GetString(raw, "state_slug"),
                Population = GetLong(raw, "population"),
                DataYear = GetInt(raw, "data_year"),
            };

            if (raw.TryGetValue("seo", out var seoObj) && seoObj is Dictionary<object, object> seo)
            {
                content.SeoTitle = GetObjString(seo, "title");
                content.SeoDescription = GetObjString(seo, "description");
            }

            if (raw.TryGetValue("page", out var pageObj) && pageObj is Dictionary<object, object> page)
            {
                content.H1 = GetObjString(page, "h1");
                content.Intro = GetObjString(page, "intro");
                content.HeritageTitle = GetObjString(page, "heritage_title");
                content.HeritageBody = GetObjString(page, "heritage_body");
                content.FunFact = GetObjString(page, "fun_fact");
            }

            if (raw.TryGetValue("surnames", out var surnamesObj) && surnamesObj is List<object> surnameList)
            {
                foreach (var item in surnameList)
                {
                    if (item is not Dictionary<object, object> s) continue;
                    content.Surnames.Add(new SurnameEntry
                    {
                        Rank = GetObjInt(s, "rank"),
                        Name = GetObjString(s, "name"),
                        Count = GetObjLong(s, "count"),
                        Ratio = GetObjInt(s, "ratio"),
                        Origin = GetObjString(s, "origin", "english"),
                        Type = GetObjString(s, "type"),
                        Meaning = GetObjString(s, "meaning"),
                    });
                }
            }

            if (raw.TryGetValue("unique_surnames", out var uObj) && uObj is List<object> uList)
            {
                foreach (var item in uList)
                {
                    if (item is not Dictionary<object, object> u) continue;
                    content.UniqueSurnames.Add(new UniqueSurname
                    {
                        Name = GetObjString(u, "name"),
                        StateRank = GetObjInt(u, "state_rank"),
                        NationalRank = GetObjInt(u, "national_rank"),
                        Origin = GetObjString(u, "origin", "english"),
                        Why = GetObjString(u, "why"),
                    });
                }
            }

            if (raw.TryGetValue("etymology_groups", out var egObj) && egObj is List<object> egList)
            {
                foreach (var item in egList)
                {
                    if (item is not Dictionary<object, object> eg) continue;
                    var examples = new List<string>();
                    if (eg.TryGetValue("examples", out var exObj) && exObj is List<object> exList)
                        examples = exList.Select(e => e?.ToString() ?? "").Where(e => e != "").ToList();
                    content.EtymologyGroups.Add(new EtymologyGroup
                    {
                        Type = GetObjString(eg, "type"),
                        Icon = GetObjString(eg, "icon", "fa-regular fa-bookmark"),
                        Description = GetObjString(eg, "description"),
                        Examples = examples,
                    });
                }
            }

            if (raw.TryGetValue("faq", out var faqObj) && faqObj is List<object> faqList)
            {
                foreach (var item in faqList)
                {
                    if (item is not Dictionary<object, object> f) continue;
                    content.Faq.Add(new SurnamesFaq
                    {
                        Question = GetObjString(f, "question"),
                        Answer = GetObjString(f, "answer"),
                    });
                }
            }

            if (raw.TryGetValue("sources", out var srcObj) && srcObj is List<object> srcList)
            {
                foreach (var item in srcList)
                {
                    if (item is not Dictionary<object, object> s) continue;
                    content.Sources.Add(new SurnamesSource
                    {
                        Name = GetObjString(s, "name"),
                        Url = GetObjString(s, "url"),
                        Description = GetObjString(s, "description"),
                    });
                }
            }

            return content;
        }

        private static string GetString(Dictionary<string, object> d, string key)
            => d.TryGetValue(key, out var v) ? v?.ToString() ?? "" : "";

        private static long GetLong(Dictionary<string, object> d, string key)
            => d.TryGetValue(key, out var v) && long.TryParse(v?.ToString(), out var n) ? n : 0;

        private static int GetInt(Dictionary<string, object> d, string key)
            => d.TryGetValue(key, out var v) && int.TryParse(v?.ToString(), out var n) ? n : 0;

        private static string GetObjString(Dictionary<object, object> d, string key, string fallback = "")
            => d.TryGetValue(key, out var v) ? v?.ToString() ?? fallback : fallback;

        private static int GetObjInt(Dictionary<object, object> d, string key)
            => d.TryGetValue(key, out var v) && int.TryParse(v?.ToString(), out var n) ? n : 0;

        private static long GetObjLong(Dictionary<object, object> d, string key)
            => d.TryGetValue(key, out var v) && long.TryParse(v?.ToString(), out var n) ? n : 0;
    }
}
