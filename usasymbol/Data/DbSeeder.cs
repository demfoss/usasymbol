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

            // Все 50 штатов США
            var states = new List<State>
            {
                // Northeast Region
                new State { Name = "Connecticut", Slug = "connecticut", Abbreviation = "CT", Capital = "Hartford", Population = 3605944, Region = "Northeast", StateHoodDate = new DateTime(1788, 1, 9) },
                new State { Name = "Maine", Slug = "maine", Abbreviation = "ME", Capital = "Augusta", Population = 1362359, Region = "Northeast", StateHoodDate = new DateTime(1820, 3, 15) },
                new State { Name = "Massachusetts", Slug = "massachusetts", Abbreviation = "MA", Capital = "Boston", Population = 7029917, Region = "Northeast", StateHoodDate = new DateTime(1788, 2, 6) },
                new State { Name = "New Hampshire", Slug = "new-hampshire", Abbreviation = "NH", Capital = "Concord", Population = 1377529, Region = "Northeast", StateHoodDate = new DateTime(1788, 6, 21) },
                new State { Name = "New Jersey", Slug = "new-jersey", Abbreviation = "NJ", Capital = "Trenton", Population = 9288994, Region = "Northeast", StateHoodDate = new DateTime(1787, 12, 18) },
                new State { Name = "New York", Slug = "new-york", Abbreviation = "NY", Capital = "Albany", Population = 20201249, Region = "Northeast", StateHoodDate = new DateTime(1788, 7, 26) },
                new State { Name = "Pennsylvania", Slug = "pennsylvania", Abbreviation = "PA", Capital = "Harrisburg", Population = 13002700, Region = "Northeast", StateHoodDate = new DateTime(1787, 12, 12) },
                new State { Name = "Rhode Island", Slug = "rhode-island", Abbreviation = "RI", Capital = "Providence", Population = 1097379, Region = "Northeast", StateHoodDate = new DateTime(1790, 5, 29) },
                new State { Name = "Vermont", Slug = "vermont", Abbreviation = "VT", Capital = "Montpelier", Population = 643077, Region = "Northeast", StateHoodDate = new DateTime(1791, 3, 4) },

                // Midwest Region
                new State { Name = "Illinois", Slug = "illinois", Abbreviation = "IL", Capital = "Springfield", Population = 12812508, Region = "Midwest", StateHoodDate = new DateTime(1818, 12, 3) },
                new State { Name = "Indiana", Slug = "indiana", Abbreviation = "IN", Capital = "Indianapolis", Population = 6785528, Region = "Midwest", StateHoodDate = new DateTime(1816, 12, 11) },
                new State { Name = "Iowa", Slug = "iowa", Abbreviation = "IA", Capital = "Des Moines", Population = 3190369, Region = "Midwest", StateHoodDate = new DateTime(1846, 12, 28) },
                new State { Name = "Kansas", Slug = "kansas", Abbreviation = "KS", Capital = "Topeka", Population = 2937880, Region = "Midwest", StateHoodDate = new DateTime(1861, 1, 29) },
                new State { Name = "Michigan", Slug = "michigan", Abbreviation = "MI", Capital = "Lansing", Population = 10077331, Region = "Midwest", StateHoodDate = new DateTime(1837, 1, 26) },
                new State { Name = "Minnesota", Slug = "minnesota", Abbreviation = "MN", Capital = "Saint Paul", Population = 5706494, Region = "Midwest", StateHoodDate = new DateTime(1858, 5, 11) },
                new State { Name = "Missouri", Slug = "missouri", Abbreviation = "MO", Capital = "Jefferson City", Population = 6154913, Region = "Midwest", StateHoodDate = new DateTime(1821, 8, 10) },
                new State { Name = "Nebraska", Slug = "nebraska", Abbreviation = "NE", Capital = "Lincoln", Population = 1961504, Region = "Midwest", StateHoodDate = new DateTime(1867, 3, 1) },
                new State { Name = "North Dakota", Slug = "north-dakota", Abbreviation = "ND", Capital = "Bismarck", Population = 779094, Region = "Midwest", StateHoodDate = new DateTime(1889, 11, 2) },
                new State { Name = "Ohio", Slug = "ohio", Abbreviation = "OH", Capital = "Columbus", Population = 11799448, Region = "Midwest", StateHoodDate = new DateTime(1803, 3, 1) },
                new State { Name = "South Dakota", Slug = "south-dakota", Abbreviation = "SD", Capital = "Pierre", Population = 886667, Region = "Midwest", StateHoodDate = new DateTime(1889, 11, 2) },
                new State { Name = "Wisconsin", Slug = "wisconsin", Abbreviation = "WI", Capital = "Madison", Population = 5893718, Region = "Midwest", StateHoodDate = new DateTime(1848, 5, 29) },

                // South Region
                new State { Name = "Alabama", Slug = "alabama", Abbreviation = "AL", Capital = "Montgomery", Population = 5024279, Region = "South", StateHoodDate = new DateTime(1819, 12, 14) },
                new State { Name = "Arkansas", Slug = "arkansas", Abbreviation = "AR", Capital = "Little Rock", Population = 3011524, Region = "South", StateHoodDate = new DateTime(1836, 6, 15) },
                new State { Name = "Delaware", Slug = "delaware", Abbreviation = "DE", Capital = "Dover", Population = 989948, Region = "South", StateHoodDate = new DateTime(1787, 12, 7) },
                new State { Name = "Florida", Slug = "florida", Abbreviation = "FL", Capital = "Tallahassee", Population = 21538187, Region = "South", StateHoodDate = new DateTime(1845, 3, 3) },
                new State { Name = "Georgia", Slug = "georgia", Abbreviation = "GA", Capital = "Atlanta", Population = 10711908, Region = "South", StateHoodDate = new DateTime(1788, 1, 2) },
                new State { Name = "Kentucky", Slug = "kentucky", Abbreviation = "KY", Capital = "Frankfort", Population = 4505836, Region = "South", StateHoodDate = new DateTime(1792, 6, 1) },
                new State { Name = "Louisiana", Slug = "louisiana", Abbreviation = "LA", Capital = "Baton Rouge", Population = 4657757, Region = "South", StateHoodDate = new DateTime(1812, 4, 30) },
                new State { Name = "Maryland", Slug = "maryland", Abbreviation = "MD", Capital = "Annapolis", Population = 6177224, Region = "South", StateHoodDate = new DateTime(1788, 4, 28) },
                new State { Name = "Mississippi", Slug = "mississippi", Abbreviation = "MS", Capital = "Jackson", Population = 2961279, Region = "South", StateHoodDate = new DateTime(1817, 12, 10) },
                new State { Name = "North Carolina", Slug = "north-carolina", Abbreviation = "NC", Capital = "Raleigh", Population = 10439388, Region = "South", StateHoodDate = new DateTime(1789, 11, 21) },
                new State { Name = "Oklahoma", Slug = "oklahoma", Abbreviation = "OK", Capital = "Oklahoma City", Population = 3959353, Region = "South", StateHoodDate = new DateTime(1907, 11, 16) },
                new State { Name = "South Carolina", Slug = "south-carolina", Abbreviation = "SC", Capital = "Columbia", Population = 5118425, Region = "South", StateHoodDate = new DateTime(1788, 5, 23) },
                new State { Name = "Tennessee", Slug = "tennessee", Abbreviation = "TN", Capital = "Nashville", Population = 6910840, Region = "South", StateHoodDate = new DateTime(1796, 6, 1) },
                new State { Name = "Texas", Slug = "texas", Abbreviation = "TX", Capital = "Austin", Population = 29145505, Region = "South", StateHoodDate = new DateTime(1845, 12, 29) },
                new State { Name = "Virginia", Slug = "virginia", Abbreviation = "VA", Capital = "Richmond", Population = 8631393, Region = "South", StateHoodDate = new DateTime(1788, 6, 25) },
                new State { Name = "West Virginia", Slug = "west-virginia", Abbreviation = "WV", Capital = "Charleston", Population = 1793716, Region = "South", StateHoodDate = new DateTime(1863, 6, 20) },

                // West Region
                new State { Name = "Alaska", Slug = "alaska", Abbreviation = "AK", Capital = "Juneau", Population = 733391, Region = "West", StateHoodDate = new DateTime(1959, 1, 3) },
                new State { Name = "Arizona", Slug = "arizona", Abbreviation = "AZ", Capital = "Phoenix", Population = 7151502, Region = "West", StateHoodDate = new DateTime(1912, 2, 14) },
                new State { Name = "California", Slug = "california", Abbreviation = "CA", Capital = "Sacramento", Population = 39538223, Region = "West", StateHoodDate = new DateTime(1850, 9, 9) },
                new State { Name = "Colorado", Slug = "colorado", Abbreviation = "CO", Capital = "Denver", Population = 5773714, Region = "West", StateHoodDate = new DateTime(1876, 8, 1) },
                new State { Name = "Hawaii", Slug = "hawaii", Abbreviation = "HI", Capital = "Honolulu", Population = 1455271, Region = "West", StateHoodDate = new DateTime(1959, 8, 21) },
                new State { Name = "Idaho", Slug = "idaho", Abbreviation = "ID", Capital = "Boise", Population = 1839106, Region = "West", StateHoodDate = new DateTime(1890, 7, 3) },
                new State { Name = "Montana", Slug = "montana", Abbreviation = "MT", Capital = "Helena", Population = 1084225, Region = "West", StateHoodDate = new DateTime(1889, 11, 8) },
                new State { Name = "Nevada", Slug = "nevada", Abbreviation = "NV", Capital = "Carson City", Population = 3104614, Region = "West", StateHoodDate = new DateTime(1864, 10, 31) },
                new State { Name = "New Mexico", Slug = "new-mexico", Abbreviation = "NM", Capital = "Santa Fe", Population = 2117522, Region = "West", StateHoodDate = new DateTime(1912, 1, 6) },
                new State { Name = "Oregon", Slug = "oregon", Abbreviation = "OR", Capital = "Salem", Population = 4237256, Region = "West", StateHoodDate = new DateTime(1859, 2, 14) },
                new State { Name = "Utah", Slug = "utah", Abbreviation = "UT", Capital = "Salt Lake City", Population = 3271616, Region = "West", StateHoodDate = new DateTime(1896, 1, 4) },
                new State { Name = "Washington", Slug = "washington", Abbreviation = "WA", Capital = "Olympia", Population = 7705281, Region = "West", StateHoodDate = new DateTime(1889, 11, 11) },
                new State { Name = "Wyoming", Slug = "wyoming", Abbreviation = "WY", Capital = "Cheyenne", Population = 576851, Region = "West", StateHoodDate = new DateTime(1890, 7, 10) }
            };

            // После создания всех штатов - автоматически добавляем пути к флагам
            foreach (var state in states)
            {
                state.FlagImageUrl = $"/images/states/flags/small/{state.Abbreviation.ToLower()}.webp";
            }

            context.States.AddRange(states);
            await context.SaveChangesAsync();

            // Добавляем символы для Illinois (для примера)
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