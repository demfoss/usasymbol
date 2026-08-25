using Microsoft.Extensions.Caching.Memory;
using USASymbol.Models.Content;
using USASymbol.Models.ViewModels;
using USASymbol.Services.Interface;
using USASymbol.Services.Yaml;
using YamlDotNet.Serialization;

namespace USASymbol.Services.Content
{
    public class SongService : ISongService
    {
        private readonly IMemoryCache _cache;
        private readonly IWebHostEnvironment _env;
        private readonly IDeserializer _yamlDeserializer;

        public SongService(IMemoryCache cache, IWebHostEnvironment env)
        {
            _cache = cache;
            _env = env;
            _yamlDeserializer = new DeserializerBuilder().Build();
        }

        public async Task<SongContent?> GetSongContentAsync(string stateSlug, string symbolSlug)
        {
            var path = Path.Combine(_env.ContentRootPath, "Content", "states", stateSlug, "song", $"{symbolSlug}.yaml");

            if (!File.Exists(path))
                return null;

            var cacheKey = $"song-content-{stateSlug}-{symbolSlug}-{File.GetLastWriteTimeUtc(path).Ticks}";

            return await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromHours(24);
                var yaml = await File.ReadAllTextAsync(path);

                try
                {
                    var data = _yamlDeserializer.Deserialize<Dictionary<object, object>>(yaml);

                    var content = new SongContent
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
                        Composer = GetString(data, "composer"),
                        Lyricist = GetString(data, "lyricist"),
                        PublicationYear = GetInt(data, "publication_year"),
                        YoutubeUrl = GetString(data, "youtube_url"),
                        YoutubeTitle = GetString(data, "youtube_title"),
                        YoutubeCaption = GetString(data, "youtube_caption"),
                        SpotifyUrl = GetString(data, "spotify_url"),
                        SpotifyCaption = GetString(data, "spotify_caption"),
                        LyricsIsPublicDomain = GetBool(data, "lyrics_is_public_domain"),
                        LyricsIsUserProvided = GetBool(data, "lyrics_is_user_provided"),
                        LyricsNote = GetString(data, "lyrics_note"),
                        LyricsSourceUrl = GetString(data, "lyrics_source_url"),
                        LyricsSourceName = GetString(data, "lyrics_source_name")
                    };

                    if (data.ContainsKey("lyrics_verses") && data["lyrics_verses"] is List<object> verses)
                        content.LyricsVerses = verses.Select(v => v?.ToString() ?? string.Empty).ToList();

                    if (data.ContainsKey("secondary_songs") && data["secondary_songs"] is List<object> secondarySongs)
                    {
                        foreach (var s in secondarySongs)
                        {
                            if (s is not Dictionary<object, object> sDict)
                                continue;

                            content.SecondarySongs.Add(new SecondarySong
                            {
                                Name = GetString(sDict, "name"),
                                Designation = GetString(sDict, "designation"),
                                Composer = GetString(sDict, "composer"),
                                Lyricist = GetString(sDict, "lyricist"),
                                Year = GetString(sDict, "year")
                            });
                        }
                    }

                    if (data.ContainsKey("sections") && data["sections"] is List<object> sections)
                    {
                        foreach (var sec in sections)
                        {
                            if (sec is not Dictionary<object, object> secDict)
                                continue;

                            var section = new SongSection
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
                                section.Facts = facts.Select(f => f?.ToString() ?? string.Empty).ToList();

                            content.Sections.Add(section);
                        }
                    }

                    if (data.ContainsKey("faq") && data["faq"] is List<object> faqList)
                    {
                        foreach (var faq in faqList)
                        {
                            if (faq is not Dictionary<object, object> faqDict)
                                continue;

                            content.Faq.Add(new SongFaq
                            {
                                Question = GetString(faqDict, "question"),
                                Answer = GetString(faqDict, "answer")
                            });
                        }
                    }

                    if (data.ContainsKey("sources") && data["sources"] is List<object> sources)
                    {
                        foreach (var src in sources)
                        {
                            if (src is not Dictionary<object, object> srcDict)
                                continue;

                            content.Sources.Add(new SongSource
                            {
                                Name = GetString(srcDict, "name"),
                                Url = GetString(srcDict, "url"),
                                Description = GetString(srcDict, "description")
                            });
                        }
                    }

                    if (data.TryGetValue("quick_facts", out var quickFactsObj) && quickFactsObj is List<object> quickFactsList)
                    {
                        foreach (var fact in quickFactsList)
                        {
                            if (fact is not Dictionary<object, object> factDict)
                                continue;

                            var label = GetString(factDict, "label");
                            var value = GetString(factDict, "value");
                            if (string.IsNullOrWhiteSpace(label) || string.IsNullOrWhiteSpace(value))
                                continue;

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
                    Console.WriteLine($"Error parsing {symbolSlug}.yaml for {stateSlug}: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                    return null;
                }
            });
        }

        private static string GetString(Dictionary<object, object> dict, string key)
            => dict.ContainsKey(key) ? dict[key]?.ToString() ?? string.Empty : string.Empty;

        private static int? GetInt(Dictionary<object, object> dict, string key)
        {
            if (dict.ContainsKey(key) && int.TryParse(dict[key]?.ToString(), out var result))
                return result;
            return null;
        }

        private static bool GetBool(Dictionary<object, object> dict, string key)
        {
            if (dict.ContainsKey(key) && bool.TryParse(dict[key]?.ToString(), out var result))
                return result;
            return false;
        }

        private static DateTime? GetDate(Dictionary<object, object> dict, string key)
        {
            if (dict.ContainsKey(key) && DateTime.TryParse(dict[key]?.ToString(), out var result))
                return result;
            return null;
        }

        private static List<QuickFactItem> BuildQuickFacts(SongContent content)
        {
            var facts = new List<QuickFactItem>();

            void AddFact(string label, string? value)
            {
                if (string.IsNullOrWhiteSpace(value))
                    return;

                facts.Add(new QuickFactItem
                {
                    Label = label,
                    Value = value.Trim()
                });
            }

            if (content.AdoptedYear.HasValue && content.AdoptedYear.Value > 0)
                AddFact("Adopted", content.AdoptedYear.Value.ToString());

            AddFact("Composer", content.Composer);
            AddFact("Lyricist", content.Lyricist);
            AddFact("Status", content.IsOfficial ? "Official state song" : "State song");

            return facts;
        }
    }
}
