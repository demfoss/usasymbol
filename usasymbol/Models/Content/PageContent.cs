using System;
using System.Collections.Generic;
using USASymbol.Models.ViewModels;

namespace USASymbol.Models.Content
{





    public class PageTable
    {
        public string? Title { get; set; }
        public List<TableColumn> Columns { get; set; } = new();
        public List<TableRow> Rows { get; set; } = new();
        public List<string> ToggleableColumns { get; set; } = new();
        public List<string> HiddenColumns { get; set; } = new();
        public string? DefaultColumn { get; set; }
        public bool Searchable { get; set; } = true;
        public bool Sortable { get; set; } = true;

        /// <summary>
        /// Optional alternate renderer for this table. When set to "insignia-grid", the page
        /// renders a multi-image gallery grid (see _InsigniaGrid.cshtml) instead of the standard
        /// sortable/searchable data table. Set via YAML field <c>table.display_mode</c>.
        /// </summary>
        public string? DisplayMode { get; set; }
    }

    public class TableColumn
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;


        public string Type { get; set; } = "text";

        public string? Format { get; set; }
        public string? CssClass { get; set; }
        public bool Sortable { get; set; } = true;
        public bool Toggleable { get; set; } = false;
        public string? Width { get; set; }
    }

    public class TableRow
    {
        public Dictionary<string, object?> Data { get; set; } = new();

        public T? Get<T>(string key)
        {
            if (Data.TryGetValue(key, out var value) && value != null)
            {
                try
                {
                    if (typeof(T) == typeof(string))
                        return (T)(object)value.ToString()!;
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch { return default; }
            }
            return default;
        }

        public string GetString(string key)
        {
            var direct = Get<string>(key);
            if (!string.IsNullOrWhiteSpace(direct))
                return direct;

            if (string.Equals(key, "note", StringComparison.OrdinalIgnoreCase))
                return Get<string>("notes") ?? "";

            if (string.Equals(key, "notes", StringComparison.OrdinalIgnoreCase))
                return Get<string>("note") ?? "";

            return "";
        }
        public long GetNumber(string key) => Get<long>(key);

        public bool Has(string key) =>
            Data.ContainsKey(key) && Data[key] != null &&
            !string.IsNullOrEmpty(Data[key]?.ToString());
    }








    public class PageContent
    {
        public string Type { get; set; } = "";
        public string Slug { get; set; } = "";
        public string Category { get; set; } = "";
        public string Url { get; set; } = "";





        public string? DetailType { get; set; }
        public string? Subcategory { get; set; }
        public string? HeroVariant { get; set; }

        public PageSeo Seo { get; set; } = new();
        public PageBody Page { get; set; } = new();
        public List<PageSection> Sections { get; set; } = new();


        public List<PageTable> Tables { get; set; } = new();

        public List<PageFaq> Faq { get; set; } = new();
        public List<PageRelated> Related { get; set; } = new();
        public string? HeroImage { get; set; }
        public string? HeroImageAlt { get; set; }
        public string? HeroImageCaption { get; set; }
        public PageMap? Map { get; set; }
        public PageHeatmap? Heatmap { get; set; }
        public List<VisualAsset> VisualAssets { get; set; } = new();
        public RankingCompareData? Compare { get; set; }
        public QuizPromoData? QuizPromo { get; set; }
        public ComputedRankingConfig? ComputedData { get; set; }


        /// <summary>
        /// Controls how the RankingExtremesBlock renders on this page.
        /// Values: "both" (default), "topOnly", "bottomOnly", "none".
        /// Set via YAML field <c>extremes_mode</c>.
        /// </summary>
        public string? ExtremesMode { get; set; }

        /// <summary>
        /// Optional SEO-friendly heading for the RankingExtremesBlock.
        /// If omitted, the block auto-generates a title from the metric column label.
        /// Set via YAML field <c>extremes_title</c>.
        /// </summary>
        public string? ExtremesTitle { get; set; }

        public BigStatData? BigStat { get; set; }
        public List<TimelineEvent> Timeline { get; set; } = new();
        public ExpertQuoteData? ExpertQuote { get; set; }

        public string? Author { get; set; }
        public DateTime? DatePublished { get; set; }
        public DateTime? DateModified { get; set; }
        public DateTime LastModified { get; set; }


        public PageTable? Table => Tables.Count > 0 ? Tables[0] : null;
    }

    public class PageSeo
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
    }

    public class PageBody
    {
        public string H1 { get; set; } = "";

        public string? IntroTitle { get; set; }
        public string? QuickAnswerTitle { get; set; }
        public List<string> QuickAnswer { get; set; } = new();
        public List<string> IntroParagraphs { get; set; } = new();
        public List<string> Insights { get; set; } = new();
        public string Methodology { get; set; } = "";
        public List<PageSource> Sources { get; set; } = new();
    }

    public class PageSection
    {
        public string Id { get; set; } = "";
        public string Icon { get; set; } = "";
        public string Title { get; set; } = "";
        public string? Style { get; set; }
        public string? Img { get; set; }
        public string? ImgRight { get; set; }
        public List<string> Paragraphs { get; set; } = new();
        public List<PageSubsection>? Subsections { get; set; }
        public List<string>? Facts { get; set; }
        public PageSectionTable? Table { get; set; }
        public List<PageHighlight>? Highlights { get; set; }
    }

    public class PageHighlight
    {
        public string Name { get; set; } = "";
        public string State { get; set; } = "";
        public string Status { get; set; } = "";
        public string Image { get; set; } = "";
        public string Description { get; set; } = "";
        public List<string>? AnchorPhrases { get; set; }
    }

    public class PageSectionTable
    {
        public List<string> Headers { get; set; } = new();
        public List<PageSectionTableRow> Rows { get; set; } = new();
        public string? Caption { get; set; }
        public string? Note { get; set; }
        public bool FirstColumnIsHeader { get; set; } = true;
        public bool ShowRowNumbers { get; set; } = false;
    }

    public class PageSectionTableRow
    {
        public List<string> Cells { get; set; } = new();
    }

    public class PageSubsection
    {
        public string Subtitle { get; set; } = "";
        public string Status { get; set; } = "";
        public string Text { get; set; } = "";
        public LinkData? Link { get; set; }
        public List<string>? AnchorPhrases { get; set; }
        public string? Image { get; set; }
        public string? ImageCaption { get; set; }
    }

    public class PageSource : ISource
    {
        public string Name { get; set; } = "";
        public string Url { get; set; } = "";
        public string Description { get; set; } = "";
    }

    public class PageFaq
    {
        public string Question { get; set; } = "";
        public string Answer { get; set; } = "";
    }

    public class PageRelated
    {
        public string Title { get; set; } = "";
        public string Url { get; set; } = "";
    }

    public class PageMap
    {
        public string Title { get; set; } = "";
        public string Image { get; set; } = "";
        public string ImageAlt { get; set; } = "";
        public string? Caption { get; set; }
        public string? MetricKey { get; set; }
        public string? MetricLabel { get; set; }
        public string? NameKey { get; set; }
        public string? ImageKey { get; set; }
        public List<string> DetailKeys { get; set; } = new();
        public string? FillColor { get; set; }
        public string ColorScheme { get; set; } = "blue";
        public string ColorScale { get; set; } = "linear";
        public bool ShowLabels { get; set; } = false;
        public bool TextOverlay { get; set; } = false;
        public string? SummaryKey { get; set; }
        public List<PageMapFilter> Filters { get; set; } = new();
        public Dictionary<string, string> ColorMap { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public class PageMapFilter
    {
        public string Key { get; set; } = "";
        public string Label { get; set; } = "";
        public string Style { get; set; } = "chips";
        public string? AccentColor { get; set; }
        public bool UseColors { get; set; } = false;
    }

    public class PageHeatmap
    {
        public string Title { get; set; } = "";
        public string? Caption { get; set; }
        public string DataUrl { get; set; } = "";
        public string Gradient { get; set; } = "hot";
        public int Radius { get; set; } = 18;
        public int Blur { get; set; } = 22;
        public int PointsPerCluster { get; set; } = 1;
    }

    public class QuizPromoData
    {
        public string Slug { get; set; } = "";
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Icon { get; set; }
        public string? ButtonText { get; set; }
    }

    public class RankingCompareData
    {
        public string MetricSlug { get; set; } = "";
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? ButtonText { get; set; }
        public string? Icon { get; set; }
        public string? DefaultStateA { get; set; }
        public string? DefaultStateB { get; set; }
    }

    public class ComputedRankingConfig
    {
        /// <summary>snake_case field name matching StateStats property (e.g. median_household_income).</summary>
        public string Field { get; set; } = "";

        /// <summary>"asc" or "desc". Default is "desc" (highest first).</summary>
        public string Sort { get; set; } = "desc";

        /// <summary>.NET format string for the metric column (e.g. "C0", "F1", "N0").</summary>
        public string Format { get; set; } = "N2";

        /// <summary>Column header displayed in the table.</summary>
        public string Label { get; set; } = "";

        /// <summary>Short key used as the column key in the table row data.</summary>
        public string MetricKey { get; set; } = "value";
    }






    public class PageCategory
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Icon { get; set; } = "";
        public string? Image { get; set; }
        public List<PageCategoryItem> Items { get; set; } = new();
    }

    public class PageCategoryItem
    {
        public string Title { get; set; } = "";
        public string Url { get; set; } = "";
        public string? Description { get; set; }
        public string? Image { get; set; }
        public string? ImageAlt { get; set; }
        public string? Subcategory { get; set; }
        public DateTime? DatePublished { get; set; }
        public DateTime? DateModified { get; set; }
    }
}
