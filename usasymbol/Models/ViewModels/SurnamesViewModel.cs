using USASymbol.Models.Content;

namespace USASymbol.Models.ViewModels
{
    public class SurnamesViewModel : IStateScopedViewModel
    {
        public required State State { get; set; }
        public required SurnamesContent Content { get; set; }

        public SurnameEntry? Top1 => Content.Surnames.FirstOrDefault(s => s.Rank == 1);
        public SurnameEntry? Top2 => Content.Surnames.FirstOrDefault(s => s.Rank == 2);
        public SurnameEntry? Top3 => Content.Surnames.FirstOrDefault(s => s.Rank == 3);
        public IEnumerable<SurnameEntry> RestOfList => Content.Surnames.Where(s => s.Rank > 3).OrderBy(s => s.Rank);
    }
}
