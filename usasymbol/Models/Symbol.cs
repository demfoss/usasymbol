namespace USASymbol.Models
{
    public class Symbol
    {
        public int Id { get; set; }
        public int StateId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? ScientificName { get; set; }
        public int? AdoptedYear { get; set; }
        public string? ImageUrl { get; set; }
        public string YamlPath { get; set; } = string.Empty;

        public string? Designation { get; set; }
        public string? Legislation { get; set; }
        public string? Status { get; set; }






        public string? WikidataId { get; set; }




        public string? Meaning { get; set; }


        public State? State { get; set; }
    }
}