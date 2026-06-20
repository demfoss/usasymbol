using System.Collections.Generic;
using System.Linq;
using USASymbol.Models.Content;

namespace USASymbol.Models.ViewModels
{




    public class PageDetailViewModel : IHasVisualAssets
    {
        public PageContent Content { get; set; } = null!;


        public IReadOnlyList<VisualAsset>? VisualAssets => Content?.VisualAssets;


        public List<PageTable> Tables => Content?.Tables ?? new List<PageTable>();
        public bool HasRows => Tables.Any(t => t.Rows.Any());
        public bool HasColumns => Tables.Any(t => t.Columns.Any());
        public int TotalRowsCount => Tables.Sum(t => t.Rows.Count);


        public bool ShowMetricToggle => Content?.Table?.ToggleableColumns?.Count > 1;

        public List<TableColumn> ToggleableColumnsList =>
            Content?.Table?.Columns?
                .Where(c => Content.Table.ToggleableColumns.Contains(c.Key))
                .ToList() ?? new List<TableColumn>();


        public bool HasQuickAnswer      => Content?.Page?.QuickAnswer?.Any()      == true;
        public bool HasIntroParagraphs  => Content?.Page?.IntroParagraphs?.Any()  == true;
        public bool HasInsights         => Content?.Page?.Insights?.Any()         == true;
        public bool HasSources          => Content?.Page?.Sources?.Any()          == true;
        public bool HasRelated          => Content?.Related?.Any()                == true;
        public bool HasFaq              => Content?.Faq?.Any()                    == true;
        public bool HasSections         => Content?.Sections?.Any()               == true;



        public bool IsRanking => Content?.Type == "ranking";


        public bool IsCollection => Content?.Type == "collection";


        public AuthorBox AuthorBox => new AuthorBox
        {
            DateModified = Content?.DateModified,
            Author       = Content?.Author,
        };


        public TableSectionViewModel ToTableSection() => new TableSectionViewModel
        {
            Tables                = Tables,
            DetailType            = (Content?.DetailType ?? "").ToLowerInvariant(),
            ShowMetricToggle      = ShowMetricToggle,
            ToggleableColumnsList = ToggleableColumnsList,
            TotalRowsCount        = TotalRowsCount,
            Searchable            = Content?.Table?.Searchable ?? true,
        };


        public string CategoryTitle => FormatCategoryTitle(Content?.Category ?? "");
        public string CategoryIcon  => GetCategoryIcon(Content?.Category ?? "");

        private static string FormatCategoryTitle(string id) => id switch
        {
            "geography"    => "Geography",
            "demographics" => "Demographics",
            "government"   => "Government & Politics",
            "history"      => "History",
            "economy"      => "Economy",
            "symbols"      => "Symbols & Culture",
            "culture"      => "Culture",

            "flags"        => "Flags",
            "animals"      => "Animals",
            "birds"        => "Birds",
            "flowers"      => "Flowers",
            "trees"        => "Trees",
            "beverages"    => "Beverages",
            "firearms"     => "Firearms",
            "dinosaurs"    => "Dinosaurs",
            "weird"        => "Weird & Unusual",
            "records"      => "State Records",
            "firsts"       => "Historical Firsts",
            _ => System.Globalization.CultureInfo.CurrentCulture.TextInfo
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

            "flags"        => "fa-solid fa-flag",
            "animals"      => "fa-solid fa-paw",
            "birds"        => "fa-solid fa-dove",
            "flowers"      => "fa-solid fa-seedling",
            "trees"        => "fa-solid fa-tree",
            "beverages"    => "fa-solid fa-glass-water",
            "firearms"     => "fa-solid fa-gun",
            "dinosaurs"    => "fa-solid fa-bone",
            "weird"        => "fa-solid fa-wand-magic-sparkles",
            "records"      => "fa-solid fa-trophy",
            "firsts"       => "fa-solid fa-star",
            _              => "fa-solid fa-layer-group",
        };
    }





    public class PageHubViewModel
    {
        public List<PageCategory> Categories { get; set; } = new();
        public bool HasCategories => Categories?.Any() == true;
    }

    public class SubcategoryFilterOption
    {
        public string Value { get; set; } = "";
        public string Label { get; set; } = "";
        public int Count { get; set; }
    }

    public class PageCategoryViewModel
    {
        public PageCategory Category { get; set; } = new();
        public List<PageCategoryItem> MostPopular { get; set; } = new();
        public List<SubcategoryFilterOption> SubcategoryFilters { get; set; } = new();
    }
}
