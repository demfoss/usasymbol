using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using USASymbol.Models.Content;
using USASymbol.Services.Interface;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Markdig;

namespace USASymbol.Services
{
    public class BorderService : IBorderService
    {
        private readonly ILogger<BorderService> _logger;
        private readonly string _contentBasePath;

        public BorderService(ILogger<BorderService> logger, IWebHostEnvironment env)
        {
            _logger = logger;
            _contentBasePath = Path.Combine(env.ContentRootPath, "Content", "borders");
        }

        public async Task<BorderContent?> GetBorderContentAsync(string stateSlug)
        {
            try
            {
                var filePath = Path.Combine(_contentBasePath, $"{stateSlug}.yml");

                if (!File.Exists(filePath))
                {
                    _logger.LogWarning($"Border content file not found: {filePath}");
                    return null;
                }

                var fileContent = await File.ReadAllTextAsync(filePath);
                var parts = fileContent.Split(new[] { "---" }, StringSplitOptions.RemoveEmptyEntries);


                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .IgnoreUnmatchedProperties()
                    .Build();


                var rawData = deserializer.Deserialize<Dictionary<string, object>>(parts[0]);
                var borderContent = MapToBorderContent(rawData);


                if (parts.Length > 1)
                {
                    var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
                    borderContent.HtmlContent = Markdown.ToHtml(parts[1].Trim(), pipeline);
                }


                var fileInfo = new FileInfo(filePath);
                borderContent.LastModified = fileInfo.LastWriteTime;

                return borderContent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading border content for {stateSlug}");
                return null;
            }
        }

        private BorderContent MapToBorderContent(Dictionary<string, object> rawData)
        {
            var content = new BorderContent();


            content.Type = GetString(rawData, "type");
            content.State = GetString(rawData, "state");
            content.IsOfficial = GetBool(rawData, "is_official");
            content.Author = GetString(rawData, "author");
            content.SeoTitle = GetString(rawData, "seo_title");
            content.SeoDescription = GetString(rawData, "seo_description");
            content.IntroText = GetString(rawData, "intro_text");


            content.DatePublished = GetDateTime(rawData, "date_published");
            content.DateModified = GetDateTime(rawData, "date_modified");


            if (rawData.ContainsKey("border_summary") && rawData["border_summary"] is Dictionary<object, object> summaryDict)
            {
                content.BorderSummary = new BorderSummary
                {
                    BorderingStatesCount = GetInt(summaryDict, "bordering_states_count"),
                    BorderingStatesList = GetString(summaryDict, "bordering_states_list"),
                    CountryBorders = GetString(summaryDict, "country_borders"),
                    OceanBorders = GetString(summaryDict, "ocean_borders"),
                    GreatLakeBorders = GetString(summaryDict, "great_lake_borders"),
                    MajorRiverBorders = GetString(summaryDict, "major_river_borders"),
                    Landlocked = GetBool(summaryDict, "landlocked")
                };
            }


            if (rawData.ContainsKey("map") && rawData["map"] is Dictionary<object, object> mapDict)
            {
                content.Map = new BorderMap
                {
                    Title = GetString(mapDict, "title"),
                    Image = GetString(mapDict, "image"),
                    ImageAlt = GetString(mapDict, "image_alt"),
                    Caption = GetString(mapDict, "caption")
                };
            }


            if (rawData.ContainsKey("cards") && rawData["cards"] is List<object> cardsList)
            {
                foreach (var cardObj in cardsList)
                {
                    if (cardObj is Dictionary<object, object> cardDict)
                    {
                        content.Cards.Add(new BorderCard
                        {
                            Name = GetString(cardDict, "name"),
                            Badge = GetString(cardDict, "badge"),
                            NeighborKind = GetString(cardDict, "neighbor_kind"),
                            BorderType = GetString(cardDict, "border_type"),
                            Features = GetString(cardDict, "features"),
                            Anchor = GetString(cardDict, "anchor"),
                            Image = string.IsNullOrEmpty(GetString(cardDict, "image")) ? null : GetString(cardDict, "image"),
                            ParkName = string.IsNullOrEmpty(GetString(cardDict, "park_name")) ? null : GetString(cardDict, "park_name")
                        });
                    }
                }
            }


            if (rawData.ContainsKey("sections") && rawData["sections"] is List<object> sectionsList)
            {
                foreach (var sectionObj in sectionsList)
                {
                    if (sectionObj is Dictionary<object, object> sectionDict)
                    {
                        var section = new BorderSection
                        {
                            Id = GetString(sectionDict, "id"),
                            Icon = GetString(sectionDict, "icon"),
                            Title = GetString(sectionDict, "title"),
                            Style = GetString(sectionDict, "style")
                        };


                        if (sectionDict.ContainsKey("paragraphs") && sectionDict["paragraphs"] is List<object> paraList)
                        {
                            section.Paragraphs = paraList.Select(p => p?.ToString() ?? "").ToList();
                        }


                        if (sectionDict.ContainsKey("bullets") && sectionDict["bullets"] is List<object> bulletList)
                        {
                            section.Bullets = bulletList.Select(b => b?.ToString() ?? "").ToList();
                        }


                        if (sectionDict.ContainsKey("table") && sectionDict["table"] is Dictionary<object, object> tableDict)
                        {
                            section.Table = ParseTable(tableDict);
                        }


                        if (sectionDict.ContainsKey("facts") && sectionDict["facts"] is List<object> factsList)
                        {
                            section.Facts = factsList.Select(f => f?.ToString() ?? "").ToList();
                        }


                        if (sectionDict.ContainsKey("subsections") && sectionDict["subsections"] is List<object> subsectionsList)
                        {
                            section.Subsections = new List<BorderSubsection>();
                            foreach (var subsectionObj in subsectionsList)
                            {
                                if (subsectionObj is Dictionary<object, object> subsectionDict)
                                {
                                    var subsection = new BorderSubsection
                                    {
                                        Id = GetString(subsectionDict, "id"),
                                        Subtitle = GetString(subsectionDict, "subtitle"),
                                        Text = GetString(subsectionDict, "text")
                                    };


                                    if (subsectionDict.ContainsKey("paragraphs") && subsectionDict["paragraphs"] is List<object> subParaList)
                                    {
                                        subsection.Paragraphs = subParaList.Select(p => p?.ToString() ?? "").ToList();
                                    }


                                    if (subsectionDict.ContainsKey("bullets") && subsectionDict["bullets"] is List<object> subBulletList)
                                    {
                                        subsection.Bullets = subBulletList.Select(b => b?.ToString() ?? "").ToList();
                                    }

                                    section.Subsections.Add(subsection);
                                }
                            }
                        }

                        content.Sections.Add(section);
                    }
                }
            }


            if (rawData.ContainsKey("faq") && rawData["faq"] is List<object> faqList)
            {
                foreach (var faqObj in faqList)
                {
                    if (faqObj is Dictionary<object, object> faqDict)
                    {
                        content.Faq.Add(new BorderFaq
                        {
                            Question = GetString(faqDict, "question"),
                            Answer = GetString(faqDict, "answer")
                        });
                    }
                }
            }


            if (rawData.ContainsKey("sources") && rawData["sources"] is List<object> sourcesList)
            {
                foreach (var sourceObj in sourcesList)
                {
                    if (sourceObj is Dictionary<object, object> sourceDict)
                    {
                        content.Sources.Add(new BorderSource
                        {
                            Name = GetString(sourceDict, "name"),
                            Url = GetString(sourceDict, "url"),
                            Description = GetString(sourceDict, "description")
                        });
                    }
                }
            }

            return content;
        }

        private BorderTable ParseTable(Dictionary<object, object> tableDict)
        {
            var table = new BorderTable();


            if (tableDict.ContainsKey("columns") && tableDict["columns"] is List<object> columnsList)
            {
                table.Columns = columnsList.Select(c => c?.ToString() ?? "").ToList();
            }


            if (tableDict.ContainsKey("rows") && tableDict["rows"] is List<object> rowsList)
            {
                foreach (var rowObj in rowsList)
                {
                    if (rowObj is Dictionary<object, object> rowDict)
                    {
                        var row = new BorderTableRow();


                        foreach (var kvp in rowDict)
                        {
                            var key = kvp.Key?.ToString()?.ToLowerInvariant() ?? "";
                            var value = kvp.Value?.ToString() ?? "";
                            row.Data[key] = value;
                        }

                        table.Rows.Add(row);
                    }
                }
            }

            return table;
        }


        private string GetString(Dictionary<object, object> dict, string key)
        {
            return dict.ContainsKey(key) ? dict[key]?.ToString() ?? "" : "";
        }

        private string GetString(Dictionary<string, object> dict, string key)
        {
            return dict.ContainsKey(key) ? dict[key]?.ToString() ?? "" : "";
        }

        private int GetInt(Dictionary<object, object> dict, string key)
        {
            if (dict.ContainsKey(key) && dict[key] != null)
            {
                if (int.TryParse(dict[key].ToString(), out var result))
                    return result;
            }
            return 0;
        }

        private bool GetBool(Dictionary<object, object> dict, string key)
        {
            if (dict.ContainsKey(key) && dict[key] != null)
            {
                if (bool.TryParse(dict[key].ToString(), out var result))
                    return result;
            }
            return false;
        }

        private bool GetBool(Dictionary<string, object> dict, string key)
        {
            if (dict.ContainsKey(key) && dict[key] != null)
            {
                if (bool.TryParse(dict[key].ToString(), out var result))
                    return result;
            }
            return false;
        }

        private DateTime? GetDateTime(Dictionary<string, object> dict, string key)
        {
            if (dict.ContainsKey(key) && dict[key] != null)
            {
                if (DateTime.TryParse(dict[key].ToString(), out var result))
                    return result;
            }
            return null;
        }
    }
}