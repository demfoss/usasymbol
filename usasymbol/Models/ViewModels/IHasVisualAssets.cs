namespace USASymbol.Models.ViewModels
{







    public interface IHasVisualAssets
    {
        IReadOnlyList<VisualAsset>? VisualAssets { get; }
    }






    public sealed class VisualAsset
    {

        public string Id { get; init; } = "";


        public string Src { get; init; } = "";


        public string Alt { get; init; } = "";


        public string Caption { get; init; } = "";








        public string Layout { get; init; } = "";

        public string Section { get; init; } = "";
    }
}
