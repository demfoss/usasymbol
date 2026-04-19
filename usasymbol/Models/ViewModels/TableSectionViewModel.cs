using System.Collections.Generic;
using System.Linq;
using USASymbol.Models.Content;

namespace USASymbol.Models.ViewModels
{





    public class TableSectionViewModel
    {

        public List<PageTable> Tables { get; set; } = new();


        public string DetailType { get; set; } = "";


        public bool ShowMetricToggle { get; set; }


        public List<TableColumn> ToggleableColumnsList { get; set; } = new();


        public int TotalRowsCount { get; set; }


        public bool Searchable { get; set; } = true;


        public List<string> ToggleableColumnKeys =>
            Tables.FirstOrDefault()?.ToggleableColumns ?? new List<string>();

        public string? DefaultColumnKey =>
            Tables.FirstOrDefault()?.DefaultColumn
            ?? ToggleableColumnKeys.FirstOrDefault();
    }
}
