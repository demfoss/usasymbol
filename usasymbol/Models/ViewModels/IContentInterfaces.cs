using USASymbol.Models.Content;

namespace USASymbol.Models.ViewModels
{

    public interface IContentSection
    {
        string Id { get; }
        string Icon { get; }
        string Title { get; }
        string? Style { get; }

        string? Img { get; }
        List<string>? Paragraphs { get; }
        List<IContentSubsection>? Subsections { get; }
        List<string>? Facts { get; }
        List<string>? ListItems { get; }
    }

    public interface IContentSubsection
    {
        string Subtitle { get; }
        string Text { get; }
        List<string>? ListItems { get; }
        LinkData? Link { get; set; }
        string? Image => null;
        string? ImageCaption => null;
    }

    public interface ISource
    {
        string Name { get; }
        string Url { get; }
        string Description { get; }
    }

    public interface IFaqItem
    {
        string Question { get; }
        string Answer { get; }
    }

    public sealed class LinkData
    {
        public string Label { get; set; } = "";
        public string Url { get; set; } = "";
    }
}