using System.Collections.Generic;

namespace USASymbol.Models.Content
{
    public class BirdContent
    {
        // Basic info
        public string Title { get; set; } = string.Empty;
        public string ScientificName { get; set; } = string.Empty;
        public int? AdoptedYear { get; set; }

        // Dates
        public DateTime? DatePublished { get; set; }
        public DateTime? DateModified { get; set; }
        public string Author { get; set; } = string.Empty;

        // Shared states
        public List<string> SharedStates { get; set; } = new();

        // Habitat & Features
        public string Habitat { get; set; } = string.Empty;
        public string DistinctiveFeature { get; set; } = string.Empty;
        public string ConservationStatus { get; set; } = string.Empty;

        public string? SharedStatesText { get; set; } = string.Empty;
        public string? DistinctiveSong { get; set; } = string.Empty;
        public string? Coloration { get; set; } = string.Empty;
        public string? ColorationDetails { get; set; } = string.Empty;

        // Physical characteristics
        public string Size { get; set; } = string.Empty;
        public string Wingspan { get; set; } = string.Empty;
        public string Weight { get; set; } = string.Empty;

        // Behavior
        public string Diet { get; set; } = string.Empty;
        public string Nesting { get; set; } = string.Empty;
        public string Range { get; set; } = string.Empty;

        // Content Sections (для красивого отображения)
        public List<BirdSection> Sections { get; set; } = new();

        // Sources
        public List<BirdSource> Sources { get; set; } = new();

        public List<BirdFaq> Faq { get; set; } = new();

        // Markdown content (after YAML)
        public string HtmlContent { get; set; } = string.Empty;

        // File metadata
        public DateTime LastModified { get; set; }
    }

    public class BirdSection
    {
        public string Id { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Style { get; set; } // "gradient" для Fun Facts

        // Простые параграфы
        public List<string>? Paragraphs { get; set; }

        // Подсекции (Male Cardinals, Female Cardinals)
        public List<BirdSubsection>? Subsections { get; set; }

        // Список фактов
        public List<string>? Facts { get; set; }
    }

    public class BirdSubsection
    {
        public string Subtitle { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public List<string>? ListItems { get; set; }
    }

    public class BirdSource
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class BirdFaq
    {
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
    }
}