namespace USASymbol.Models.ViewModels
{
    public class StateMapPageViewModel
    {
        public State State { get; set; } = new();
        public List<CountyItem> Counties { get; set; } = new();
        public CountySummary? CountySummary { get; set; }
        public string AllStatesJson { get; set; } = "[]";
        public string StateSlugsJson { get; set; } = "[]";
    }

    public class CountyItem
    {
        public string DisplayName { get; set; } = string.Empty;
        public string StateCode { get; set; } = string.Empty;
        public string FipsCode { get; set; } = string.Empty;
        public int? Population { get; set; }
    }

    public class CountySummary
    {
        public int CountyCount { get; set; }
        public CountyItem? LargestByPopulation { get; set; }
        public CountyItem? SmallestByPopulation { get; set; }
    }
}
