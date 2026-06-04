using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using USASymbol.Models.Content;
using USASymbol.Models.ViewModels;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace USASymbol.Services
{




    public interface IPageContentService
    {
        Task<PageContent?> GetContentAsync(string category, string slug);
        Task<List<PageCategory>> GetAllCategoriesAsync();
    }






    public interface IRankingsContentService : IPageContentService { }


    public interface IListingsContentService : IPageContentService { }


    public interface ICollectionsContentService : IPageContentService { }





    public class PageContentService : IPageContentService
    {
        private readonly ILogger<PageContentService> _logger;
        private readonly string _contentBasePath;

        private static readonly Regex MultiDash     = new(@"-+",   RegexOptions.Compiled);
        private static readonly Regex AnyWhitespace = new(@"\s+",  RegexOptions.Compiled);

        public PageContentService(ILogger<PageContentService> logger, string contentBasePath)
        {
            _logger          = logger;
            _contentBasePath = contentBasePath;
        }



        public async Task<PageContent?> GetContentAsync(string category, string slug)
        {
            try
            {
                var filePath = Path.Combine(_contentBasePath, category, $"{slug}.yml");
                if (!File.Exists(filePath))
                {
                    filePath = Path.Combine(_contentBasePath, category, $"{slug}.yaml");
                }

                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("Content file not found: {Path}", filePath);
                    return null;
                }

                var yaml = await File.ReadAllTextAsync(filePath);

                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .IgnoreUnmatchedProperties()
                    .Build();

                var raw     = deserializer.Deserialize<Dictionary<string, object>>(yaml);
                var content = MapToPageContent(raw);
                content.LastModified = new FileInfo(filePath).LastWriteTime;

                return content;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading content for {Category}/{Slug}", category, slug);
                return null;
            }
        }

        public async Task<List<PageCategory>> GetAllCategoriesAsync()
        {
            var categories = new List<PageCategory>();

            try
            {
                if (!Directory.Exists(_contentBasePath))
                {
                    _logger.LogWarning("Content directory not found: {Path}", _contentBasePath);
                    return categories;
                }

                foreach (var categoryDir in Directory.GetDirectories(_contentBasePath))
                {
                    var categoryName = Path.GetFileName(categoryDir);
                    var category     = new PageCategory
                    {
                        Id    = categoryName,
                        Title = FormatCategoryTitle(categoryName),
                        Icon  = GetCategoryIcon(categoryName),
                    };

                    foreach (var file in Directory.GetFiles(categoryDir, "*.yml").Concat(Directory.GetFiles(categoryDir, "*.yaml")))
                    {
                        var content = await GetContentAsync(categoryName, Path.GetFileNameWithoutExtension(file));
                        if (content != null)
                        {
                            category.Items.Add(new PageCategoryItem
                            {
                                Title       = content.Page.H1,
                                Url         = content.Url,
                                Description = content.Seo.Description,
                                Image       = content.HeroImage,
                                ImageAlt    = content.HeroImageAlt,
                            });
                        }
                    }

                    if (category.Items.Any())
                        categories.Add(category);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading categories from {Path}", _contentBasePath);
            }

            return categories;
        }



        private PageContent MapToPageContent(Dictionary<string, object> raw)
        {
            var content = new PageContent
            {
                Type          = Str(raw, "type"),
                Slug          = Str(raw, "slug"),
                Category      = Str(raw, "category"),
                Url           = Str(raw, "url"),
                HeroImage     = Str(raw, "hero_image"),
                HeroImageAlt  = Str(raw, "hero_image_alt"),
                HeroImageCaption = Str(raw, "hero_image_caption"),
                Author        = Str(raw, "author"),
                DatePublished = Date(raw, "date_published"),
                DateModified  = Date(raw, "date_modified"),
                DetailType    = Str(raw, "detail_type"),
            };


            if (raw.TryGetValue("seo", out var seoObj) && seoObj is Dictionary<object, object> seoD)
                content.Seo = new PageSeo { Title = Str(seoD, "title"), Description = Str(seoD, "description") };


            if (raw.TryGetValue("page", out var pageObj) && pageObj is Dictionary<object, object> pageD)
            {
                content.Page = new PageBody
                {
                    H1          = Str(pageD, "h1"),
                    Methodology = Str(pageD, "methodology"),
                    IntroTitle  = pageD.ContainsKey("intro_title") ? Str(pageD, "intro_title") : null,
                };

                if (pageD.TryGetValue("quick_answer",     out var qa)      && qa      is List<object> qaL)
                    content.Page.QuickAnswer = qaL.Select(x => x?.ToString() ?? "").ToList();

                if (pageD.TryGetValue("intro_paragraphs", out var intro)   && intro   is List<object> introL)
                    content.Page.IntroParagraphs = introL.Select(x => x?.ToString() ?? "").ToList();

                if (pageD.TryGetValue("insights",         out var ins)     && ins     is List<object> insL)
                    content.Page.Insights = insL.Select(x => x?.ToString() ?? "").ToList();

                if (pageD.TryGetValue("sources",          out var srcObj)  && srcObj  is List<object> srcL)
                    foreach (var s in srcL.OfType<Dictionary<object, object>>())
                        content.Page.Sources.Add(new PageSource
                        {
                            Name        = Str(s, "name"),
                            Url         = Str(s, "url"),
                            Description = Str(s, "description"),
                        });

                if (pageD.TryGetValue("sections", out var pageSects) && pageSects is List<object> pageSectsList)
                    ParseSectionsInto(content, pageSectsList);
            }


            if (raw.TryGetValue("map", out var mapObj) && mapObj is Dictionary<object, object> mapD)
                content.Map = new PageMap
                {
                    Title       = Str(mapD, "title"),
                    Image       = Str(mapD, "image"),
                    ImageAlt    = Str(mapD, "image_alt"),
                    Caption     = Str(mapD, "caption"),
                    MetricKey   = mapD.ContainsKey("metric_key")   ? Str(mapD, "metric_key")   : null,
                    MetricLabel = mapD.ContainsKey("metric_label") ? Str(mapD, "metric_label") : null,
                    NameKey     = mapD.ContainsKey("name_key")     ? Str(mapD, "name_key")     : null,
                    ImageKey    = mapD.ContainsKey("image_key")    ? Str(mapD, "image_key")    : null,
                    DetailKeys  = mapD.TryGetValue("detail_keys", out var detailKeys) && detailKeys is List<object> detailKeyList
                        ? detailKeyList.Select(x => x?.ToString() ?? "").Where(x => !string.IsNullOrWhiteSpace(x)).ToList()
                        : new List<string>(),
                    FillColor   = mapD.ContainsKey("fill_color")   ? Str(mapD, "fill_color")   : null,
                    ColorScheme = mapD.ContainsKey("color_scheme") ? Str(mapD, "color_scheme") : "blue",
                    ColorScale  = mapD.ContainsKey("color_scale")  ? Str(mapD, "color_scale")  : "linear",
                    ShowLabels  = mapD.ContainsKey("show_labels")  && Str(mapD, "show_labels") == "true",
                };

            if (raw.TryGetValue("extremes_mode", out var emObj))
                content.ExtremesMode = emObj?.ToString();

            if (raw.TryGetValue("extremes_title", out var etObj))
                content.ExtremesTitle = etObj?.ToString();

            if (raw.TryGetValue("compare", out var compareObj) && compareObj is Dictionary<object, object> compareD)
                content.Compare = new RankingCompareData
                {
                    MetricSlug    = Str(compareD, "metric_slug"),
                    Title         = compareD.ContainsKey("title") ? Str(compareD, "title") : null,
                    Description   = compareD.ContainsKey("description") ? Str(compareD, "description") : null,
                    ButtonText    = compareD.ContainsKey("button_text") ? Str(compareD, "button_text") : null,
                    Icon          = compareD.ContainsKey("icon") ? Str(compareD, "icon") : null,
                    DefaultStateA = compareD.ContainsKey("default_state_a") ? Str(compareD, "default_state_a") : null,
                    DefaultStateB = compareD.ContainsKey("default_state_b") ? Str(compareD, "default_state_b") : null,
                };

            if (raw.TryGetValue("computed_data", out var cdObj) && cdObj is Dictionary<object, object> cdD)
                content.ComputedData = new ComputedRankingConfig
                {
                    Field     = Str(cdD, "field"),
                    Sort      = cdD.ContainsKey("sort")       ? Str(cdD, "sort")       : "desc",
                    Format    = cdD.ContainsKey("format")     ? Str(cdD, "format")     : "N2",
                    Label     = cdD.ContainsKey("label")      ? Str(cdD, "label")      : "",
                    MetricKey = cdD.ContainsKey("metric_key") ? Str(cdD, "metric_key") : "value",
                };


            if (raw.TryGetValue("sections", out var sectsObj) && sectsObj is List<object> sectsL)
                ParseSectionsInto(content, sectsL);


            if (raw.TryGetValue("table", out var tblObj) && tblObj is Dictionary<object, object> tblD)
            {
                var table = ParseTable(tblD);
                AutoFillSlugs(table, content.DetailType, content.Slug);
                content.Tables.Add(table);
            }


            if (raw.TryGetValue("tables", out var tbls) && tbls is List<object> tblsL)
                foreach (var t in tblsL.OfType<Dictionary<object, object>>())
                {
                    var table = ParseTable(t);

                    if (t.TryGetValue("title", out var titleObj))
                        table.Title = titleObj?.ToString();

                    AutoFillSlugs(table, content.DetailType, content.Slug);
                    content.Tables.Add(table);
                }


            if (raw.TryGetValue("faq", out var faqObj) && faqObj is List<object> faqL)
                foreach (var f in faqL.OfType<Dictionary<object, object>>())
                    content.Faq.Add(new PageFaq { Question = Str(f, "question"), Answer = Str(f, "answer") });


            if (raw.TryGetValue("related", out var relObj) && relObj is List<object> relL)
                foreach (var r in relL.OfType<Dictionary<object, object>>())
                    content.Related.Add(new PageRelated { Title = Str(r, "title"), Url = Str(r, "url") });


            if (raw.TryGetValue("visual_assets", out var vaObj) && vaObj is List<object> vaL)
                foreach (var va in vaL.OfType<Dictionary<object, object>>())
                    content.VisualAssets.Add(new VisualAsset
                    {
                        Id      = Str(va, "id"),
                        Src     = Str(va, "src"),
                        Alt     = Str(va, "alt"),
                        Caption = Str(va, "caption"),
                        Layout  = Str(va, "layout"),
                        Section = Str(va, "section"),
                    });


            if (raw.TryGetValue("big_stat", out var bsObj) && bsObj is Dictionary<object, object> bsD)
                content.BigStat = new BigStatData { Number = Str(bsD, "number"), Description = Str(bsD, "description") };

            if (raw.TryGetValue("timeline", out var tlObj) && tlObj is List<object> tlL)
                foreach (var t in tlL.OfType<Dictionary<object, object>>())
                    content.Timeline.Add(new TimelineEvent { Year = Str(t, "year"), Description = Str(t, "description") });

            if (raw.TryGetValue("expert_quote", out var eqObj) && eqObj is Dictionary<object, object> eqD)
                content.ExpertQuote = new ExpertQuoteData { Text = Str(eqD, "text"), Source = Str(eqD, "source") };

            return content;
        }



        private PageTable ParseTable(Dictionary<object, object> d)
        {
            var table = new PageTable();

            if (d.TryGetValue("searchable",   out var s)) table.Searchable    = s?.ToString()?.ToLower() == "true";
            if (d.TryGetValue("sortable",     out var o)) table.Sortable      = o?.ToString()?.ToLower() == "true";
            if (d.TryGetValue("default_column", out var dc)) table.DefaultColumn = dc?.ToString();


            if (table.DefaultColumn == null && d.TryGetValue("metric_default", out var md))
                table.DefaultColumn = md?.ToString();

            if (d.TryGetValue("metrics_available", out var ma) && ma is List<object> maL)
                table.ToggleableColumns = maL.Select(x => x?.ToString() ?? "").Where(x => x != "").ToList();

            if (d.TryGetValue("toggleable_columns", out var tc) && tc is List<object> tcL)
                table.ToggleableColumns = tcL.Select(x => x?.ToString() ?? "").Where(x => x != "").ToList();

            if (d.TryGetValue("hidden_columns", out var hc) && hc is List<object> hcL)
                table.HiddenColumns = hcL.Select(x => x?.ToString() ?? "").Where(x => x != "").ToList();


            if (d.TryGetValue("columns", out var colsObj) && colsObj is Dictionary<object, object> colsD)
                foreach (var kvp in colsD)
                {
                    var key = kvp.Key?.ToString() ?? "";
                    if (key == "") continue;

                    var col = new TableColumn
                    {
                        Key        = key,
                        Label      = kvp.Value?.ToString() ?? key,
                        Type       = InferColumnType(key),
                        Sortable   = true,
                        Toggleable = table.ToggleableColumns.Contains(key),
                    };

                    if (col.Type == "number")
                        col.Format = key.ToLower().Contains("year") ? "0" : "N0";

                    table.Columns.Add(col);
                }


            if (d.TryGetValue("rows", out var rowsObj) && rowsObj is List<object> rowsL)
                foreach (var rowObj in rowsL.OfType<Dictionary<object, object>>())
                {
                    var row = new TableRow();
                    foreach (var kvp in rowObj)
                    {
                        var key = kvp.Key?.ToString() ?? "";
                        if (key != "") row.Data[key] = kvp.Value;
                    }


                    if (!row.Data.ContainsKey("state_slug") && row.Data.ContainsKey("state"))
                        row.Data["state_slug"] = GenerateSlug(row.GetString("state"));

                    table.Rows.Add(row);
                }

            return table;
        }

        private static void ParseSectionsInto(PageContent content, List<object> sectsL)
        {
            foreach (var s in sectsL.OfType<Dictionary<object, object>>())
            {
                var sect = new PageSection
                {
                    Id       = Str(s, "id"),
                    Icon     = Str(s, "icon"),
                    Title    = Str(s, "title"),
                };

                if (s.TryGetValue("style", out var style))
                    sect.Style = style?.ToString();

                if (s.TryGetValue("paragraphs", out var parObj) && parObj is List<object> parL)
                    sect.Paragraphs = parL.Select(x => x?.ToString() ?? "").ToList();

                if (s.TryGetValue("subsections", out var subObj) && subObj is List<object> subL)
                {
                    sect.Subsections = new List<PageSubsection>();
                    foreach (var sub in subL.OfType<Dictionary<object, object>>())
                    {
                        var pageSub = new PageSubsection { Subtitle = Str(sub, "subtitle"), Text = Str(sub, "text") };
                        if (sub.TryGetValue("link", out var linkObj) && linkObj is Dictionary<object, object> linkD)
                            pageSub.Link = new LinkData { Label = Str(linkD, "label"), Url = Str(linkD, "url") };
                        sect.Subsections.Add(pageSub);
                    }
                }

                if (s.TryGetValue("facts", out var factsObj) && factsObj is List<object> factsL)
                    sect.Facts = factsL.Select(x => x?.ToString() ?? "").ToList();

                if (s.TryGetValue("table", out var tableObj) && tableObj is Dictionary<object, object> tableD)
                    sect.Table = ParseSectionTable(tableD);

                if (s.TryGetValue("highlights", out var hlObj) && hlObj is List<object> hlL)
                {
                    sect.Highlights = new List<PageHighlight>();
                    foreach (var h in hlL.OfType<Dictionary<object, object>>())
                        sect.Highlights.Add(new PageHighlight
                        {
                            Name        = Str(h, "name"),
                            State       = h.ContainsKey("state") ? Str(h, "state") : "",
                            Image       = h.ContainsKey("image") ? Str(h, "image") : "",
                            Description = h.ContainsKey("description") ? Str(h, "description") : "",
                        });
                }

                content.Sections.Add(sect);
            }
        }

        private static PageSectionTable ParseSectionTable(Dictionary<object, object> d)
        {
            var table = new PageSectionTable
            {
                Caption = d.TryGetValue("caption", out var caption) ? caption?.ToString() : null,
                Note = d.TryGetValue("note", out var note) ? note?.ToString() : null,
            };

            if (d.TryGetValue("first_column_is_header", out var firstColumn))
            {
                table.FirstColumnIsHeader = !string.Equals(
                    firstColumn?.ToString(),
                    "false",
                    StringComparison.OrdinalIgnoreCase);
            }

            // columns: dict → keys for data lookup, values as display headers
            List<string>? columnKeys = null;
            if (d.TryGetValue("columns", out var colsObj) && colsObj is Dictionary<object, object> colsD)
            {
                columnKeys = new List<string>();
                var labels = new List<string>();
                foreach (var kvp in colsD)
                {
                    var k = kvp.Key?.ToString() ?? "";
                    if (string.IsNullOrWhiteSpace(k)) continue;
                    columnKeys.Add(k);
                    labels.Add(kvp.Value?.ToString() ?? k);
                }
                table.Headers = labels;
            }
            else if (d.TryGetValue("headers", out var headersObj) && headersObj is List<object> headersL)
            {
                table.Headers = headersL.Select(x => x?.ToString() ?? "").ToList();
            }

            if (d.TryGetValue("rows", out var rowsObj) && rowsObj is List<object> rowsL)
            {
                foreach (var rowObj in rowsL)
                {
                    switch (rowObj)
                    {
                        case List<object> cells:
                            table.Rows.Add(new PageSectionTableRow
                            {
                                Cells = cells.Select(x => x?.ToString() ?? "").ToList()
                            });
                            break;

                        case Dictionary<object, object> rowDict:
                        {
                            List<string> keys;
                            if (columnKeys != null)
                            {
                                keys = columnKeys;
                            }
                            else if (table.Headers.Any())
                            {
                                keys = table.Headers;
                            }
                            else
                            {
                                keys = rowDict.Keys.Select(k => k?.ToString() ?? "").Where(k => !string.IsNullOrWhiteSpace(k)).ToList();
                                table.Headers = keys;
                            }

                            table.Rows.Add(new PageSectionTableRow
                            {
                                Cells = keys.Select(key => rowDict.TryGetValue(key, out var value) ? value?.ToString() ?? "" : "").ToList()
                            });

                            break;
                        }
                    }
                }
            }

            return table;
        }





        private void AutoFillSlugs(PageTable table, string? detailType, string? contentSlug)
        {
            var nameKey = (detailType ?? "").Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(nameKey)) return;
            var imageFolder = (contentSlug ?? "").Trim().Trim('/').ToLowerInvariant();
            var alternateSlugKey = nameKey switch
            {
                "license-plate" => "slogan_slug",
                "state-seal" => "seal_slug",
                "coat-of-arms" => "coat_of_arms_slug",
                "soil" => "soil_slug",
                _ => string.Empty
            };

            foreach (var row in table.Rows)
            {
                var explicitSlugKey = $"{nameKey}_slug";
                var explicitSlug    = row.GetString(explicitSlugKey);
                if (string.IsNullOrWhiteSpace(explicitSlug) && !string.IsNullOrWhiteSpace(alternateSlugKey))
                    explicitSlug = row.GetString(alternateSlugKey);

                if (!row.Data.ContainsKey("symbol_slug"))
                {
                    if (!string.IsNullOrWhiteSpace(explicitSlug))
                        row.Data["symbol_slug"] = explicitSlug;
                    else if (row.Data.ContainsKey(nameKey))
                        row.Data["symbol_slug"] = GenerateSlug(row.GetString(nameKey));
                    else if (row.Data.ContainsKey("symbol"))
                        row.Data["symbol_slug"] = GenerateSlug(row.GetString("symbol"));
                }

                if (!row.Data.ContainsKey("symbol_image"))
                {
                    var stateSlug = row.GetString("state_slug");
                    var symbolSlug = row.GetString("symbol_slug");

                    var licensePlateHero = nameKey == "license-plate"
                        ? GetLicensePlateHeroImage(stateSlug)
                        : string.Empty;

                    if (!string.IsNullOrWhiteSpace(licensePlateHero))
                    {
                        row.Data["symbol_image"] = licensePlateHero;
                    }
                    else if (!string.IsNullOrWhiteSpace(stateSlug) &&
                        !string.IsNullOrWhiteSpace(symbolSlug) &&
                        !string.IsNullOrWhiteSpace(imageFolder))
                    {
                        row.Data["symbol_image"] = $"/images/{imageFolder}/{stateSlug}/{symbolSlug}.webp";
                    }
                }

                if (!row.Data.ContainsKey("symbol_url"))
                {
                    var stateSlug = row.GetString("state_slug");
                    var symbolSlug = row.GetString("symbol_slug");
                    if (!string.IsNullOrWhiteSpace(stateSlug) &&
                        !string.IsNullOrWhiteSpace(symbolSlug) &&
                        !string.IsNullOrWhiteSpace(nameKey))
                    {
                        row.Data["symbol_url"] = $"/states/{stateSlug}/{nameKey}/{symbolSlug}";
                    }
                }
            }

            if (nameKey == "license-plate" &&
                table.Rows.Any(row => row.Has("symbol_image")) &&
                table.Columns.All(col => !string.Equals(col.Key, "symbol_image", StringComparison.OrdinalIgnoreCase)))
            {
                table.Columns.Insert(0, new TableColumn
                {
                    Key = "symbol_image",
                    Label = "Plate",
                    Type = "image",
                    Sortable = false
                });
            }
        }

        private string GetLicensePlateHeroImage(string stateSlug)
        {
            if (string.IsNullOrWhiteSpace(stateSlug))
                return string.Empty;

            var path = Path.Combine(_contentBasePath, "states", stateSlug, "license-plate.yaml");
            if (!File.Exists(path))
                return string.Empty;

            var yaml = File.ReadAllText(path);
            var match = Regex.Match(yaml, @"(?m)^hero_image:\s*(.+?)\s*$");
            return match.Success
                ? match.Groups[1].Value.Trim().Trim('"', '\'')
                : string.Empty;
        }



        private static string InferColumnType(string key)
        {
            var k = key.ToLower();

            if (k == "state")                      return "state-link";
            if (k is "rank" or "order" or "#")     return "rank";

            if (k.Contains("population") || k.Contains("area")   || k.Contains("land")   ||
                k.Contains("water")      || k.Contains("total")   || k.Contains("count")  ||
                k.Contains("number")     || k.Contains("amount")  || k.Contains("size")   ||
                k == "year" || k == "value" || k.Contains("year_"))
                return "number";

            if (k.Contains("date") || k is "established" or "founded" or "admitted")
                return "date";

            if (k.Contains("image") || k.Contains("flag") || k.Contains("seal") ||
                k.Contains("photo")  || k.Contains("icon") || k.Contains("logo"))
                return "image";

            if (k.Contains("url") || k.Contains("link") || k.Contains("website"))
                return "link";

            return "text";
        }



        private static string GenerateSlug(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";

            var slug = System.Net.WebUtility.HtmlDecode(text).ToLowerInvariant().Trim();

            var parenIndex = slug.IndexOf('(');
            if (parenIndex > 0) slug = slug[..parenIndex].Trim();

            slug = slug.Normalize(NormalizationForm.FormD);
            slug = new string(slug.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());

            slug = slug.Replace("&", " and ");
            slug = AnyWhitespace.Replace(slug, "-");
            slug = slug.Replace("'", "").Replace("\u2019", "").Replace("\u02BB", "")
                       .Replace("`",  "").Replace(",",     "").Replace(".",      "");

            slug = new string(slug.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());
            slug = MultiDash.Replace(slug, "-").Trim('-');

            return slug;
        }



        private static string FormatCategoryTitle(string id) => id switch
        {
            "geography"    => "Geography",
            "demographics" => "Demographics",
            "government"   => "Government & Politics",
            "history"      => "History",
            "economy"      => "Economy",
            "symbols"      => "Symbols & Culture",
            "culture"      => "Culture",
            "laws"         => "Laws & Statutes",
            _ => CultureInfo.CurrentCulture.TextInfo
                     .ToTitleCase(id.Replace("-", " ").Replace("_", " "))
        };

        private static string GetCategoryIcon(string id) => id switch
        {
            "geography"    => "fa-solid fa-globe",
            "demographics" => "fa-solid fa-users",
            "government"   => "fa-solid fa-landmark",
            "history"      => "fa-solid fa-clock-rotate-left",
            "economy"      => "fa-solid fa-chart-line",
            "symbols"      => "fa-solid fa-star",
            "culture"      => "fa-solid fa-masks-theater",
            "laws"         => "fa-solid fa-scale-balanced",
            _              => "fa-solid fa-list",
        };



        private static string Str(Dictionary<object, object> d, string key)
            => d.TryGetValue(key, out var v) ? v?.ToString() ?? "" : "";

        private static string Str(Dictionary<string, object> d, string key)
            => d.TryGetValue(key, out var v) ? v?.ToString() ?? "" : "";

        private static DateTime? Date(Dictionary<string, object> d, string key)
        {
            if (d.TryGetValue(key, out var v) && v != null && DateTime.TryParse(v.ToString(), out var r)) return r;
            return null;
        }
    }





    public class RankingsContentService : PageContentService, IRankingsContentService
    {
        public RankingsContentService(ILogger<PageContentService> logger, IWebHostEnvironment env)
            : base(logger, Path.Combine(env.ContentRootPath, "Content", "rankings")) { }
    }

    public class ListingsContentService : PageContentService, IListingsContentService
    {
        public ListingsContentService(ILogger<PageContentService> logger, IWebHostEnvironment env)
            : base(logger, Path.Combine(env.ContentRootPath, "Content")) { }
    }

    public class CollectionsContentService : PageContentService, ICollectionsContentService
    {
        public CollectionsContentService(ILogger<PageContentService> logger, IWebHostEnvironment env)
            : base(logger, Path.Combine(env.ContentRootPath, "Content", "collections")) { }
    }
}
