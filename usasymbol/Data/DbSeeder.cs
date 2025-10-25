using USASymbol.Models;

namespace USASymbol.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            // Проверяем есть ли уже данные
            if (context.States.Any())
                return;

            // Добавляем тестовые штаты
            var states = new List<State>
            {
                new State
                {
                    Name = "Illinois",
                    Slug = "illinois",
                    Abbreviation = "IL",
                    Capital = "Springfield",
                    Population = 12812508,
                    Region = "Midwest",
                    FlagImageUrl = "/images/states/illinois-flag.png",
                    StateHoodDate = new DateTime(1818, 12, 3)
                },
                new State
                {
                    Name = "California",
                    Slug = "california",
                    Abbreviation = "CA",
                    Capital = "Sacramento",
                    Population = 39538223,
                    Region = "West",
                    FlagImageUrl = "/images/states/california-flag.png",
                    StateHoodDate = new DateTime(1850, 9, 9)
                },
                new State
                {
                    Name = "Texas",
                    Slug = "texas",
                    Abbreviation = "TX",
                    Capital = "Austin",
                    Population = 29145505,
                    Region = "South",
                    FlagImageUrl = "/images/states/texas-flag.png",
                    StateHoodDate = new DateTime(1845, 12, 29)
                },
                new State
                {
                    Name = "New York",
                    Slug = "new-york",
                    Abbreviation = "NY",
                    Capital = "Albany",
                    Population = 20201249,
                    Region = "Northeast",
                    FlagImageUrl = "/images/states/new-york-flag.png",
                    StateHoodDate = new DateTime(1788, 7, 26)
                },
                new State
                {
                    Name = "Florida",
                    Slug = "florida",
                    Abbreviation = "FL",
                    Capital = "Tallahassee",
                    Population = 21538187,
                    Region = "South",
                    FlagImageUrl = "/images/states/florida-flag.png",
                    StateHoodDate = new DateTime(1845, 3, 3)
                }
            };

            context.States.AddRange(states);
            await context.SaveChangesAsync();

            // Добавляем символы для Illinois
            var illinois = states.First(s => s.Slug == "illinois");
            var illinoisSymbols = new List<Symbol>
            {
                new Symbol
                {
                    StateId = illinois.Id,
                    Type = "bird",
                    Name = "Northern Cardinal",
                    Slug = "northern-cardinal",
                    ScientificName = "Cardinalis cardinalis",
                    AdoptedYear = 1929,
                    ImageUrl = "/images/birds/northern-cardinal.jpg",
                    MarkdownPath = "Content/states/illinois/bird.md"
                },
                new Symbol
                {
                    StateId = illinois.Id,
                    Type = "flower",
                    Name = "Violet",
                    Slug = "violet",
                    ScientificName = "Viola",
                    AdoptedYear = 1908,
                    ImageUrl = "/images/flowers/violet.jpg",
                    MarkdownPath = "Content/states/illinois/flower.md"
                },
                new Symbol
                {
                    StateId = illinois.Id,
                    Type = "tree",
                    Name = "White Oak",
                    Slug = "white-oak",
                    ScientificName = "Quercus alba",
                    AdoptedYear = 1973,
                    ImageUrl = "/images/trees/white-oak.jpg",
                    MarkdownPath = "Content/states/illinois/tree.md"
                }
            };

            context.Symbols.AddRange(illinoisSymbols);
            await context.SaveChangesAsync();
        }
    }
}