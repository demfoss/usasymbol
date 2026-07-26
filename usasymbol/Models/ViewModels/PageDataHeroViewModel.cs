using System;
using System.Collections.Generic;

namespace USASymbol.Models.ViewModels
{
    public enum PageDataHeroVariant
    {
        Compact,
        TopChart,
        TopStrip,
        TileMap
    }

    public sealed class PageDataHeroItem
    {
        public int Rank { get; set; }
        public string State { get; set; } = string.Empty;
        public string StateSlug { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public double? NumericValue { get; set; }

        /// <summary>
        /// Resolved detail-page URL for this row (e.g. /states/alaska/sport/dog-mushing),
        /// when the table has enough to build one. Empty when only the state page applies.
        /// </summary>
        public string Url { get; set; } = string.Empty;
    }

    public sealed class PageDataHeroStateTile
    {
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public int Column { get; set; }
        public int Row { get; set; }
        public PageDataHeroItem? Item { get; set; }
    }

    public sealed class PageDataHeroViewModel
    {
        public PageDetailViewModel PageModel { get; set; } = null!;
        public PageDataHeroVariant Variant { get; set; } = PageDataHeroVariant.Compact;
        public string KindLabel { get; set; } = "Guide";
        public string HubUrl { get; set; } = "/";
        public string CategoryLabel { get; set; } = string.Empty;
        public string CategoryUrl { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string SourceName { get; set; } = string.Empty;
        public DateTime? DateModified { get; set; }
        public int EntryCount { get; set; }
        public int StatesRepresented { get; set; }
        public string MetricLabel { get; set; } = string.Empty;
        public string TableAnchor { get; set; } = "#ranking-table";
        public string MethodologyAnchor { get; set; } = "#methodology";
        public bool HasMethodology { get; set; }
        public List<PageDataHeroItem> Items { get; set; } = new();
        public List<PageDataHeroStateTile> StateTiles { get; set; } = new();
    }
}
