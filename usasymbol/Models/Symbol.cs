namespace USASymbol.Models
{
    public class Symbol
    {
        public int Id { get; set; }
        public int StateId { get; set; }
        public string Type { get; set; } = string.Empty; // bird, flower, tree, motto, song...
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? ScientificName { get; set; }
        public int? AdoptedYear { get; set; }
        public string? ImageUrl { get; set; }
        public string MarkdownPath { get; set; } = string.Empty; // Content/states/illinois/bird.md

        // Navigation
        public State? State { get; set; }
    }
}