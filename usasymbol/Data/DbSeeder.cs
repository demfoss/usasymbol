using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Globalization;
using System.Text;
using USASymbol.Models;
using YamlDotNet.Serialization;

namespace USASymbol.Data
{
    public static class DbSeeder
    {
        private static string GenerateSlug(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";


            string slug = text.ToLowerInvariant();


            slug = slug.Normalize(NormalizationForm.FormD);
            var chars = slug.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);
            slug = new string(chars.ToArray());


            slug = slug.Replace(" ", "-");


            slug = slug
                .Replace("'", "")
                .Replace("’", "")
                .Replace("ʻ", "")
                .Replace("`", "")
                .Replace(",", "")
                .Replace(".", "");


            slug = new string(slug.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());


            while (slug.Contains("--"))
                slug = slug.Replace("--", "-");

            return slug.Trim('-');
        }

        private static string ResolveFirearmImage(string slug)
        {
            try
            {
                var imagesDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "firearms");
                if (!Directory.Exists(imagesDir))
                    return $"/images/firearms/{slug}.webp";

                var files = Directory.EnumerateFiles(imagesDir)
                    .Select(f => Path.GetFileName(f))
                    .Where(f => f.StartsWith(slug, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (!files.Any())
                {
                    return $"/images/firearms/{slug}.webp";
                }

                var preferred = files.FirstOrDefault(f => f.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
                                ?? files.FirstOrDefault(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
                                ?? files.FirstOrDefault();

                return "/images/firearms/" + preferred;
            }
            catch
            {
                return $"/images/firearms/{slug}.webp";
            }
        }

        private static string ResolveDinosaurImage(string slug)
        {
            try
            {
                var imagesDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "dinosaurs");
                if (!Directory.Exists(imagesDir))
                    return $"/images/dinosaurs/{slug}.webp";

                var files = Directory.EnumerateFiles(imagesDir)
                    .Select(f => Path.GetFileName(f))
                    .Where(f => f.StartsWith(slug, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (!files.Any())
                    return $"/images/dinosaurs/{slug}.webp";

                var preferred = files.FirstOrDefault(f => f.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
                                ?? files.FirstOrDefault(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
                                ?? files.FirstOrDefault();

                return "/images/dinosaurs/" + preferred;
            }
            catch
            {
                return $"/images/dinosaurs/{slug}.webp";
            }
        }

        private static string ResolveBeverageImage(string stateSlug, string slug)
        {
            try
            {
                var imagesDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "beverages");
                if (!Directory.Exists(imagesDir))
                    return "";

                var stateDir = Path.Combine(imagesDir, stateSlug);
                if (Directory.Exists(stateDir))
                {
                    var stateFiles = Directory.EnumerateFiles(stateDir, $"{slug}.*", SearchOption.TopDirectoryOnly)
                        .Select(f => Path.GetFileName(f))
                        .Where(f => !string.IsNullOrWhiteSpace(f))
                        .ToList();

                    var preferredStateFile = stateFiles.FirstOrDefault(f => f.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
                                             ?? stateFiles.FirstOrDefault(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
                                             ?? stateFiles.FirstOrDefault();

                    if (!string.IsNullOrWhiteSpace(preferredStateFile))
                        return $"/images/beverages/{stateSlug}/{preferredStateFile}";
                }

                var files = Directory.EnumerateFiles(imagesDir, "*", SearchOption.AllDirectories)
                    .Select(f => new
                    {
                        RelativePath = Path.GetRelativePath(imagesDir, f).Replace("\\", "/"),
                        FileName = Path.GetFileName(f)
                    })
                    .Where(f => f.FileName.StartsWith(slug, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var preferred = files.FirstOrDefault(f => f.FileName.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
                                ?? files.FirstOrDefault(f => f.FileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
                                ?? files.FirstOrDefault();

                return preferred == null ? "" : "/images/beverages/" + preferred.RelativePath;
            }
            catch
            {
                return "";
            }
        }

        private static string ResolveStateSoilImage(string stateSlug, string heroImage)
        {
            if (!string.IsNullOrWhiteSpace(heroImage))
                return heroImage;

            return $"/images/soils/{stateSlug}/{stateSlug}-state-soil-hero.webp";
        }

        private static string ResolveStateGeologyImage(string categoryPlural, string designationSlug, string stateSlug, string heroImage)
        {
            if (!string.IsNullOrWhiteSpace(heroImage))
                return heroImage;

            return $"/images/{categoryPlural}/{stateSlug}/{stateSlug}-state-{designationSlug}-hero.webp";
        }


        public static async Task SeedAsync(AppDbContext context)
        {
            await context.Database.EnsureCreatedAsync();

            var states = new List<State>
            {

                new State { Name = "Connecticut", Slug = "connecticut", Abbreviation = "CT", Capital = "Hartford", Population = 3605944, Region = "Northeast", StateHoodDate = new DateTime(1788, 1, 9) },
                new State { Name = "Maine", Slug = "maine", Abbreviation = "ME", Capital = "Augusta", Population = 1362359, Region = "Northeast", StateHoodDate = new DateTime(1820, 3, 15) },
                new State { Name = "Massachusetts", Slug = "massachusetts", Abbreviation = "MA", Capital = "Boston", Population = 7029917, Region = "Northeast", StateHoodDate = new DateTime(1788, 2, 6) },
                new State { Name = "New Hampshire", Slug = "new-hampshire", Abbreviation = "NH", Capital = "Concord", Population = 1377529, Region = "Northeast", StateHoodDate = new DateTime(1788, 6, 21) },
                new State { Name = "New Jersey", Slug = "new-jersey", Abbreviation = "NJ", Capital = "Trenton", Population = 9288994, Region = "Northeast", StateHoodDate = new DateTime(1787, 12, 18) },
                new State { Name = "New York", Slug = "new-york", Abbreviation = "NY", Capital = "Albany", Population = 20201249, Region = "Northeast", StateHoodDate = new DateTime(1788, 7, 26) },
                new State { Name = "Pennsylvania", Slug = "pennsylvania", Abbreviation = "PA", Capital = "Harrisburg", Population = 13002700, Region = "Northeast", StateHoodDate = new DateTime(1787, 12, 12) },
                new State { Name = "Rhode Island", Slug = "rhode-island", Abbreviation = "RI", Capital = "Providence", Population = 1097379, Region = "Northeast", StateHoodDate = new DateTime(1790, 5, 29) },
                new State { Name = "Vermont", Slug = "vermont", Abbreviation = "VT", Capital = "Montpelier", Population = 643077, Region = "Northeast", StateHoodDate = new DateTime(1791, 3, 4) },


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


                new State { Name = "Alabama", Slug = "alabama", Abbreviation = "AL", Capital = "Montgomery", Population = 5024279, Region = "South", StateHoodDate = new DateTime(1819, 12, 14) },
                new State { Name = "Arkansas", Slug = "arkansas", Abbreviation = "AR", Capital = "Little Rock", Population = 3011524, Region = "South", StateHoodDate = new DateTime(1836, 6, 15) },
                new State { Name = "Delaware", Slug = "delaware", Abbreviation = "DE", Capital = "Dover", Population = 989948, Region = "South", StateHoodDate = new DateTime(1787, 12, 7) },
                new State { Name = "Florida", Slug = "florida", Abbreviation = "FL", Capital = "Tallahassee", Population = 21538187, Region = "South", StateHoodDate = new DateTime(1845, 3, 3) },
                new State { Name = "Georgia", Slug = "georgia", Abbreviation = "GA", Capital = "Atlanta", Population = 10711908, Region = "South", StateHoodDate = new DateTime(1788, 1, 2) },
                new State { Name = "Kentucky", Slug = "kentucky", Abbreviation = "KY", Capital = "Frankfort", Population = 4505836, Region = "South", StateHoodDate = new DateTime(1792, 6, 1) },
                new State { Name = "Louisiana", Slug = "louisiana", Abbreviation = "LA", Capital = "Baton Rouge", Population = 4657757, Region = "South", StateHoodDate = new DateTime(1812, 4, 30) },
                new State { Name = "Maryland", Slug = "maryland", Abbreviation = "MD", Capital = "Annapolis", Population = 6177224, Region = "South", StateHoodDate = new DateTime(1788, 4, 28) },
                new State { Name = "District of Columbia", Slug = "district-of-columbia", Abbreviation = "DC", Capital = "Washington, D.C.", Population = 689545, Region = "South", StateHoodDate = null },
                new State { Name = "Mississippi", Slug = "mississippi", Abbreviation = "MS", Capital = "Jackson", Population = 2961279, Region = "South", StateHoodDate = new DateTime(1817, 12, 10) },
                new State { Name = "North Carolina", Slug = "north-carolina", Abbreviation = "NC", Capital = "Raleigh", Population = 10439388, Region = "South", StateHoodDate = new DateTime(1789, 11, 21) },
                new State { Name = "Oklahoma", Slug = "oklahoma", Abbreviation = "OK", Capital = "Oklahoma City", Population = 3959353, Region = "South", StateHoodDate = new DateTime(1907, 11, 16) },
                new State { Name = "South Carolina", Slug = "south-carolina", Abbreviation = "SC", Capital = "Columbia", Population = 5118425, Region = "South", StateHoodDate = new DateTime(1788, 5, 23) },
                new State { Name = "Tennessee", Slug = "tennessee", Abbreviation = "TN", Capital = "Nashville", Population = 6910840, Region = "South", StateHoodDate = new DateTime(1796, 6, 1) },
                new State { Name = "Texas", Slug = "texas", Abbreviation = "TX", Capital = "Austin", Population = 29145505, Region = "South", StateHoodDate = new DateTime(1845, 12, 29) },
                new State { Name = "Virginia", Slug = "virginia", Abbreviation = "VA", Capital = "Richmond", Population = 8631393, Region = "South", StateHoodDate = new DateTime(1788, 6, 25) },
                new State { Name = "West Virginia", Slug = "west-virginia", Abbreviation = "WV", Capital = "Charleston", Population = 1793716, Region = "South", StateHoodDate = new DateTime(1863, 6, 20) },


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

            var existingStateSlugs = (await context.States
                .Select(s => s.Slug)
                .ToListAsync())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var missingStates = states
                .Where(s => !existingStateSlugs.Contains(s.Slug))
                .ToList();

            if (missingStates.Any())
            {
                foreach (var state in missingStates)
                {
                    state.FlagImageUrl = $"/images/states/flags/medium/{state.Abbreviation.ToLower()}.webp";
                }

                context.States.AddRange(missingStates);
                await context.SaveChangesAsync();
            }

            states = await context.States.ToListAsync();

            await SeedStateBirds(context, states);
            await SeedStateMottos(context, states);
            await SeedStateNicknames(context, states);
            await SeedStateFlowers(context, states);
            await SeedStateFlags(context, states);
            await SeedStateTrees(context, states);
            await SeedStateMammals(context, states);
            await SeedStateColors(context, states);
            await SeedStateFirearms(context, states);
            await SeedStateDinosaurs(context, states);
            await SeedStateBeverages(context, states);
            await SeedStateLicensePlates(context, states);
            await SeedStateSeals(context, states);
            await SeedStateCoatsOfArms(context, states);
            await SeedStateSoils(context, states);
            await SeedStateFossils(context, states);
            await SeedStateSports(context, states);
            await SeedStateDances(context, states);
            await SeedStateInsects(context, states);
            await SeedStateMinerals(context, states);
            await SeedStateRocks(context, states);
            await SeedStateGemstones(context, states);
            await SeedStateAmphibians(context, states);
            await SeedStateReptiles(context, states);
            await SeedStateFoods(context, states);


            {
                var categories = new List<SymbolCategory>
                {
                    new SymbolCategory
                    {
                        Type = "beverages",
                        Name = "State Beverages",
                        Description = "Discover official state beverages, state spirits, state cocktails, and other drink designations across the U.S.",
                        ImageUrl = "/images/symbol-categories/beverages.webp"
                    },
                    new SymbolCategory
                    {
                        Type = "birds",
                        Name = "State Birds",
                        Description = "Official state birds chosen to represent each state's wildlife and character.",
                        ImageUrl = "/images/symbol-categories/birds.webp"
                    },
                    new SymbolCategory
                    {
                        Type = "flowers",
                        Name = "State Flowers",
                        Description = "Beautiful blooms selected as each state's official floral emblem.",
                        ImageUrl = "/images/symbol-categories/flowers.webp"
                    },
                    new SymbolCategory
                    {
                        Type = "trees",
                        Name = "State Trees",
                        Description = "Trees that symbolize the natural heritage and forests of each state.",
                        ImageUrl = "/images/symbol-categories/trees.webp"
                    },
                    new SymbolCategory
                    {
                        Type = "flags",
                        Name = "State Flags",
                        Description = "Unique flags that showcase state history, identity, and pride.",
                        ImageUrl = "/images/symbol-categories/flags.webp"
                    },
                    new SymbolCategory
                    {
                        Type = "mottos",
                        Name = "State Mottos",
                        Description = "Inspiring words and phrases that capture each state's values and spirit.",
                        ImageUrl = "/images/symbol-categories/mottos.webp"
                    },
                    new SymbolCategory
                    {
                        Type = "mammals",
                        Name = "State Mammals",
                        Description = "Explore official state mammals and the unique animals that symbolize each U.S. state.",
                        ImageUrl = "/images/symbol-categories/mammals.webp"
                    },
                    new SymbolCategory
                    {
                        Type = "nicknames",
                        Name = "State Nicknames",
                        Description = "Common names and informal titles used to identify each state.",
                        ImageUrl = "/images/symbol-categories/nicknames.webp"
                    },
                    new SymbolCategory
                    {
                        Type = "dogs",
                        Name = "State Dogs",
                        Description = "Explore official state dogs and the breeds that represent each U.S. state.",
                        ImageUrl = "/images/symbol-categories/dogs.webp"
                    },
                    new SymbolCategory
                    {
                        Type = "marine-mammals",
                        Name = "State Marine Mammals",
                        Description = "Discover official state marine mammals, including whales, dolphins, and other coastal species.",
                        ImageUrl = "/images/symbol-categories/marine-mammals.webp"
                    },
                    new SymbolCategory
                    {
                        Type = "horses",
                        Name = "State Horses",
                        Description = "Learn about official state horses and the breeds recognized as symbols of U.S. states.",
                        ImageUrl = "/images/symbol-categories/horses.webp"
                    },
                    new SymbolCategory
                    {
                        Type = "colors",
                        Name = "State Colors",
                        Description = "Discover the official colors representing each U.S. state, from deep blues to vibrant reds.",
                        ImageUrl = "/images/symbol-categories/colors.webp"
                    },
                    new SymbolCategory
                    {
                        Type = "firearms",
                        Name = "State Firearms",
                        Description = "Learn about official Firearms and the breeds recognized as symbols of U.S. states.",
                        ImageUrl = "/images/symbol-categories/firearms.webp"
                    },
                    new SymbolCategory
                    {
                        Type = "dinosaurs",
                        Name = "State Dinosaurs",
                        Description = "Discover the official state dinosaurs and prehistoric symbols recognized by U.S. states.",
                        ImageUrl = "/images/symbol-categories/dinosaurs.webp"
                    },
                    new SymbolCategory
                    {
                        Type = "license-plate-slogans",
                        Name = "License Plate Slogans",
                        Description = "Explore the official license plate slogans of all 50 U.S. states — from 'Land of Lincoln' to 'Live Free or Die.'",
                        ImageUrl = "/images/symbol-categories/license-plate-slogans.webp"
                    },
                    new SymbolCategory
                    {
                        Type = "state-seals",
                        Name = "State Seals",
                        Description = "Explore the official great seals of all 50 U.S. states — the primary emblems used on government documents, flags, and official acts.",
                        ImageUrl = "/images/symbol-categories/state-seals.webp"
                    },
                    new SymbolCategory
                    {
                        Type = "coats-of-arms",
                        Name = "Coats of Arms",
                        Description = "Explore official state coats of arms and related heraldic emblems used by U.S. states.",
                        ImageUrl = "/images/symbol-categories/coats-of-arms.webp"
                    },
                    new SymbolCategory
                    {
                        Type = "soils",
                        Name = "State Soils",
                        Description = "Explore the official state soils of U.S. states — each one a distinct soil series that represents the land, agriculture, and geology of the state.",
                        ImageUrl = "/images/symbol-categories/soils.webp"
                    },
                    new SymbolCategory
                    {
                        Type = "cats",
                        Name = "State Cats",
                        Description = "Only three U.S. states have official domestic state cats: Maine (Maine Coon Cat), Maryland (Calico Cat), and Massachusetts (Tabby Cat).",
                        ImageUrl = "/images/symbol-categories/cats.webp"
                    },
                    new SymbolCategory
                    {
                        Type = "fossils",
                        Name = "State Fossils",
                        Description = "Discover the official state fossils of U.S. states — from ancient whales and mammoths to trilobites, dinosaurs, and prehistoric plants.",
                        ImageUrl = "/images/symbol-categories/fossils.webp"
                    },
                    new SymbolCategory
                    {
                        Type = "sports",
                        Name = "State Sports",
                        Description = "Explore official state sports and heritage sporting traditions recognized by U.S. states.",
                        ImageUrl = "/images/rankings/sports/most-popular-sport-by-state/most-popular-sport-by-state.webp"
                    },
                    new SymbolCategory
                    {
                        Type = "dances",
                        Name = "State Dances",
                        Description = "Explore official state dances, from square dance and clogging to hula and polka, adopted by U.S. states as folk and popular dance symbols.",
                        ImageUrl = "/images/symbol-categories/dances.webp"
                    },
                    new SymbolCategory
                    {
                        Type = "insects",
                        Name = "State Insects",
                        Description = "Discover official state insects, from monarch butterflies to honeybees, recognized by U.S. states.",
                        ImageUrl = "/images/symbol-categories/insects.webp"
                    },
                    new SymbolCategory
                    {
                        Type = "butterflies",
                        Name = "State Butterflies",
                        Description = "Explore official state butterflies, including monarchs, swallowtails, fritillaries, hairstreaks, and sulphurs.",
                        ImageUrl = "/images/insects/species/eastern-tiger-swallowtail-01.webp"
                    },
                    new SymbolCategory
                    {
                        Type = "minerals",
                        Name = "State Minerals",
                        Description = "Explore the official state minerals of U.S. states, from hematite to quartz, and the mining history behind each one.",
                        ImageUrl = "/images/symbol-categories/minerals.webp"
                    },
                    new SymbolCategory
                    {
                        Type = "rocks",
                        Name = "State Rocks & Stones",
                        Description = "Discover the official state rocks and stones of U.S. states, the building stones and geological formations chosen to represent them.",
                        ImageUrl = "/images/symbol-categories/rocks.webp"
                    },
                    new SymbolCategory
                    {
                        Type = "gemstones",
                        Name = "State Gemstones",
                        Description = "Explore the official state gemstones of U.S. states, from opals to sapphires, and the deposits where they are found.",
                        ImageUrl = "/images/symbol-categories/gemstones.webp"
                    },
                    new SymbolCategory
                    {
                        Type = "amphibians",
                        Name = "State Amphibians",
                        Description = "Discover official state amphibians, from salamanders to frogs and toads, recognized by U.S. states.",
                        ImageUrl = "/images/symbol-categories/amphibians.webp"
                    },
                    new SymbolCategory
                    {
                        Type = "reptiles",
                        Name = "State Reptiles",
                        Description = "Explore official state reptiles, from turtles to snakes and alligators, recognized by U.S. states.",
                        ImageUrl = "/images/symbol-categories/reptiles.webp"
                    },
                    new SymbolCategory
                    {
                        Type = "fruits",
                        Name = "State Fruits",
                        Description = "Explore official state fruits and berries, from peaches to blackberries, recognized by U.S. states.",
                        ImageUrl = "/images/symbol-categories/fruits.webp"
                    },
                    new SymbolCategory
                    {
                        Type = "vegetables",
                        Name = "State Vegetables",
                        Description = "Discover official state vegetables, from sweet potatoes to sweet onions, recognized by U.S. states.",
                        ImageUrl = "/images/symbol-categories/vegetables.webp"
                    },
                    new SymbolCategory
                    {
                        Type = "nuts",
                        Name = "State Nuts",
                        Description = "Explore official state nuts, from pecans to almonds, recognized by U.S. states.",
                        ImageUrl = "/images/symbol-categories/nuts.webp"
                    },
                    new SymbolCategory
                    {
                        Type = "desserts",
                        Name = "State Desserts & Sweets",
                        Description = "Discover official state cookies, cakes, pies, and other sweets recognized by U.S. states.",
                        ImageUrl = "/images/symbol-categories/desserts.webp"
                    },
                    new SymbolCategory
                    {
                        Type = "spirits",
                        Name = "State Spirits & Drinks",
                        Description = "Explore official state spirits, soft drinks, and other beverages recognized as food symbols by U.S. states.",
                        ImageUrl = "/images/symbol-categories/spirits.webp"
                    },
                    new SymbolCategory
                    {
                        Type = "dishes",
                        Name = "State Dishes & Snacks",
                        Description = "Discover official state meals, cuisines, and signature dishes recognized by U.S. states.",
                        ImageUrl = "/images/symbol-categories/dishes.webp"
                    },
                    new SymbolCategory
                    {
                        Type = "crops",
                        Name = "State Crops & Ingredients",
                        Description = "Explore official state grains, beans, honeys, and other crops recognized by U.S. states.",
                        ImageUrl = "/images/symbol-categories/crops.webp"
                    },

                };

                var staleTypes = new[] { "state-soils", "drinks" };
                var stale = await context.SymbolCategories
                    .Where(c => staleTypes.Contains(c.Type))
                    .ToListAsync();
                if (stale.Any())
                {
                    context.SymbolCategories.RemoveRange(stale);
                    await context.SaveChangesAsync();
                }

                var existingTypes = await context.SymbolCategories
                    .Select(c => c.Type)
                    .ToListAsync();

                var missingCategories = categories
                    .Where(c => !existingTypes.Contains(c.Type))
                    .ToList();

                if (missingCategories.Any())
                {
                    context.SymbolCategories.AddRange(missingCategories);
                    await context.SaveChangesAsync();
                }

                var existingCategories = await context.SymbolCategories.ToListAsync();
                var categoriesByType = categories.ToDictionary(c => c.Type);
                var didUpdateCategories = false;

                foreach (var existingCategory in existingCategories)
                {
                    if (!categoriesByType.TryGetValue(existingCategory.Type, out var seededCategory))
                    {
                        continue;
                    }

                    if (existingCategory.Name != seededCategory.Name)
                    {
                        existingCategory.Name = seededCategory.Name;
                        didUpdateCategories = true;
                    }

                    if (existingCategory.Description != seededCategory.Description)
                    {
                        existingCategory.Description = seededCategory.Description;
                        didUpdateCategories = true;
                    }

                    if (existingCategory.ImageUrl != seededCategory.ImageUrl)
                    {
                        existingCategory.ImageUrl = seededCategory.ImageUrl;
                        didUpdateCategories = true;
                    }
                }

                if (didUpdateCategories)
                {
                    await context.SaveChangesAsync();
                }
            }
        }
        private static async Task SeedStateBirds(AppDbContext context, List<State> states)
        {
            var old = await context.Symbols.Where(s => s.Type == "bird").ToListAsync();
            if (old.Count > 0)
            {
                context.Symbols.RemoveRange(old);
                await context.SaveChangesAsync();
            }

            var stateBirdsData = new Dictionary<string, (string Name, string ScientificName, int Year, string Legislation, string WikidataId, string Meaning)>
            {
                { "alabama", ("Yellowhammer", "Colaptes auratus", 1927, "Act 1927-54", "Q16819", "Named for the nickname of Alabama's Confederate soldiers, who wore yellow cloth on their uniforms.") },
                { "alaska", ("Willow Ptarmigan", "Lagopus lagopus", 1955, "Alaska Statutes § 44.09.060", "Q177573", "Chosen by schoolchildren before statehood; it symbolizes survival, changing its plumage to match the snowy landscape.") },
                { "arizona", ("Cactus Wren", "Campylorhynchus brunneicapillus", 1931, "Laws 1931, Ch. 68", "Q1025539", "Symbolizes the resilient spirit of Arizona, thriving and building nests in the harsh, thorny desert environment.") },
                { "arkansas", ("Northern Mockingbird", "Mimus polyglottos", 1929, "Senate Concurrent Resolution No. 22", "Q83724", "Recognized for its incredible vocal abilities and its protective benefit to state farmers' crops.") },
                { "california", ("California Quail", "Callipepla californica", 1931, "Chapter 777, Statutes of 1931", "Q495408", "A highly adaptable native game bird representing California's diverse coastal and valley habitats.") },
                { "colorado", ("Lark Bunting", "Calamospiza melanocorys", 1931, "House Bill 222", "Q1065011", "Noted for its acrobatic courtship flights over the vast Colorado plains.") },
                { "connecticut", ("American Robin", "Turdus migratorius", 1943, "General Statutes, Sec. 3-109", "Q461622", "A familiar, beloved sign of spring, celebrating the state's seasonal renewal.") },
                { "delaware", ("Delaware Blue Hen", "Gallus gallus domesticus", 1939, "Chapter 128, Volume 42", "Q1183863", "Traces back to the Revolutionary War, honoring the fierce combat reputation of Captain Caldwell's men.") },
                { "florida", ("Northern Mockingbird", "Mimus polyglottos", 1927, "Senate Concurrent Resolution No. 3", "Q83724", "A year-round resident famous for bringing beautiful melodies to Florida's landscapes.") },
                { "georgia", ("Brown Thrasher", "Toxostoma rufum", 1970, "House Resolution 540", "Q900891", "Replaced the Brown Pelican as a uniquely Georgian woodland songbird known for its fierce defense of its nest.") },
                { "hawaii", ("Nene", "Branta sandvicensis", 1957, "Act 52, Session Laws", "Q555628", "An endemic Hawaiian goose saved from the brink of extinction, symbolizing the state's conservation efforts.") },
                { "idaho", ("Mountain Bluebird", "Sialia currucoides", 1931, "1931 Session Laws, Chapter 64", "Q1210048", "Reflects the stunning, vivid blue skies of the Rocky Mountains.") },
                { "illinois", ("Northern Cardinal", "Cardinalis cardinalis", 1929, "Laws 1929, p. 757", "Q190663", "Voted by Illinois schoolchildren for its striking red plumage that brightens the winter months.") },
                { "indiana", ("Northern Cardinal", "Cardinalis cardinalis", 1933, "Acts of 1933, Chapter 223", "Q190663", "A vibrant, non-migratory symbol of the Midwest's winter landscapes.") },
                { "iowa", ("American Goldfinch", "Spinus tristis", 1933, "House Concurrent Resolution 22", "Q588938", "Often called the 'wild canary,' reflecting the golden hues of Iowa's agricultural prairie summers.") },
                { "kansas", ("Western Meadowlark", "Sturnella neglecta", 1937, "Laws 1937, Chapter 349", "Q1200236", "Voted by Kansas schoolchildren to represent the joyous, flute-like songs of the state's vast plains.") },
                { "kentucky", ("Northern Cardinal", "Cardinalis cardinalis", 1926, "Senate Resolution 17", "Q190663", "Chosen for its resilience and bright color that enlivens Kentucky winters.") },
                { "louisiana", ("Brown Pelican", "Pelecanus occidentalis", 1966, "Act 92 of 1966", "Q59654", "Featured on the state seal; legendary for tearing its own breast to feed its young, a symbol of charity.") },
                { "maine", ("Black-capped Chickadee", "Poecile atricapillus", 1927, "Public Laws 1927, Chapter 111", "Q848149", "A tough, cheerful little bird that fearlessly endures Maine's harshest winters.") },
                { "maryland", ("Baltimore Oriole", "Icterus galbula", 1947, "Chapter 54 of the Acts of 1947", "Q634125", "Its black and gold feathers perfectly match the colors of the historic Calvert family crest and the state flag.") },
                { "massachusetts", ("Black-capped Chickadee", "Poecile atricapillus", 1941, "Chapter 121", "Q848149", "A highly recognizable local bird, celebrated for its tireless consumption of woodland insects.") },
                { "michigan", ("American Robin", "Turdus migratorius", 1931, "House Concurrent Resolution 30", "Q461622", "Considered the best-known and best-loved bird in the state of Michigan.") },
                { "minnesota", ("Common Loon", "Gavia immer", 1961, "Laws 1961, Chapter 76", "Q193498", "Its haunting, iconic call is universally recognized as the sound of Minnesota's 10,000 lakes.") },
                { "mississippi", ("Northern Mockingbird", "Mimus polyglottos", 1944, "Chapter 326, Laws of 1944", "Q83724", "Selected by the Women's Federated Clubs for its sweet, nighttime serenades across the South.") },
                { "missouri", ("Eastern Bluebird", "Sialia sialis", 1927, "Laws of Missouri 1927, p. 121", "Q741496", "A universal symbol of happiness, bringing brilliant color to Missouri's orchards and fields.") },
                { "montana", ("Western Meadowlark", "Sturnella neglecta", 1931, "Laws 1931, Chapter 149", "Q1200236", "First recorded in Montana by Meriwether Lewis; a true symbol of the pioneer West.") },
                { "nebraska", ("Western Meadowlark", "Sturnella neglecta", 1929, "Laws 1929, Chapter 139", "Q1200236", "Selected by the state's women's clubs to represent Nebraska's agricultural heartland.") },
                { "nevada", ("Mountain Bluebird", "Sialia currucoides", 1967, "NRS 235.060", "Q1210048", "Lives high in Nevada's mountains, perfectly matching the state's clear, bright desert skies.") },
                { "new-hampshire", ("Purple Finch", "Haemorhous purpureus", 1957, "RSA 3:1", "Q1076326", "A hardy woodland bird representing the tough, independent spirit of New Hampshirites.") },
                { "new-jersey", ("American Goldfinch", "Spinus tristis", 1935, "Chapter 283, Laws of 1935", "Q588938", "Also known as the 'Eastern Goldfinch', a bright and cheerful resident of the Garden State.") },
                { "new-mexico", ("Greater Roadrunner", "Geococcyx californianus", 1949, "Laws 1949, Chapter 142", "Q633391", "A speedy, ground-dwelling bird deeply rooted in folklore and known for its bravery in killing rattlesnakes.") },
                { "new-york", ("Eastern Bluebird", "Sialia sialis", 1970, "Laws 1970, Chapter 824", "Q741496", "A gentle symbol of spring and hope returning to New York's vast forests and farms.") },
                { "north-carolina", ("Northern Cardinal", "Cardinalis cardinalis", 1943, "Session Laws 1943, c. 595", "Q190663", "A permanent resident providing a splash of brilliant red color year-round.") },
                { "north-dakota", ("Western Meadowlark", "Sturnella neglecta", 1947, "Session Laws 1947, Chapter 329", "Q1200236", "A faithful companion to the state's farmers, its song reliably signals the arrival of spring.") },
                { "ohio", ("Northern Cardinal", "Cardinalis cardinalis", 1933, "ORC Ann. 5.03 (1933)", "Q190663", "One of the most recognizable birds in Ohio, known for its loud, clear whistling.") },
                { "oklahoma", ("Scissor-tailed Flycatcher", "Tyrannus forficatus", 1951, "House Joint Resolution 21", "Q901007", "A stunning, deeply forked-tailed bird integral to Oklahoma's prairie ecology and pest control.") },
                { "oregon", ("Western Meadowlark", "Sturnella neglecta", 1927, "Gubernatorial Proclamation", "Q1200236", "Voted by schoolchildren for its beautiful song that echoes across Oregon's fields.") },
                { "pennsylvania", ("Ruffed Grouse", "Bonasa umbellus", 1931, "Act 234 of 1931", "Q808298", "A game bird symbolizing the state's deep roots in hunting and woodland conservation.") },
                { "rhode-island", ("Rhode Island Red", "Gallus gallus domesticus", 1954, "Public Laws 1954, Chapter 3249", "Q1051515", "A world-famous chicken breed developed in the state, symbolizing agricultural industry.") },
                { "south-carolina", ("Carolina Wren", "Thryothorus ludovicianus", 1948, "Act No. 693", "Q1063162", "Replaced the Mockingbird; uniquely known for its loud, distinctive 'tea-kettle' song in the woods.") },
                { "south-dakota", ("Ring-necked Pheasant", "Phasianus colchicus", 1943, "Laws 1943, Chapter 18", "Q125576", "An introduced species that thrived, becoming the absolute foundation of the state's hunting economy.") },
                { "tennessee", ("Northern Mockingbird", "Mimus polyglottos", 1933, "Senate Joint Resolution 51", "Q83724", "Voted on by the Tennessee Ornithological Society for its exceptional mimicking skills.") },
                { "texas", ("Northern Mockingbird", "Mimus polyglottos", 1927, "Senate Concurrent Resolution 8", "Q83724", "Described in the legal act as 'a fighter for the protection of his home', matching the Texas spirit.") },
                { "utah", ("California Gull", "Larus californicus", 1955, "House Bill 53", "Q1026857", "Honored for the legendary 'Miracle of the Gulls' in 1848, which saved Mormon pioneers' crops from crickets.") },
                { "vermont", ("Hermit Thrush", "Catharus guttatus", 1941, "No. 1 of the Acts of 1941", "Q578494", "Selected for its exquisite, ethereal song found echoing through Vermont's deep Green Mountains.") },
                { "virginia", ("Northern Cardinal", "Cardinalis cardinalis", 1950, "Acts of Assembly 1950, Chapter 54", "Q190663", "A bright, familiar bird that adorns Virginia's official state seal and gardens.") },
                { "washington", ("American Goldfinch", "Spinus tristis", 1951, "Laws 1951, Chapter 249", "Q588938", "Chosen twice by schoolchildren, famously known to locals as the Willow Goldfinch.") },
                { "west-virginia", ("Northern Cardinal", "Cardinalis cardinalis", 1949, "House Resolution 12", "Q190663", "Voted overwhelmingly by schoolchildren and civic clubs for its beauty and year-round presence.") },
                { "wisconsin", ("American Robin", "Turdus migratorius", 1949, "Laws 1949, Chapter 218", "Q461622", "The herald of Wisconsin's spring, chosen directly by the state's public school children.") },
                { "wyoming", ("Western Meadowlark", "Sturnella neglecta", 1927, "Laws 1927, Chapter 8", "Q1200236", "A lively singer that dots the fencelines and plains of the Cowboy State.") }
            };

            var birds = new List<Symbol>();

            foreach (var state in states)
            {
                if (stateBirdsData.TryGetValue(state.Slug, out var birdData))
                {
                    var slug = birdData.Name.ToLower().Replace(" ", "-").Replace("'", "");

                    birds.Add(new Symbol
                    {
                        StateId = state.Id,
                        Type = "bird",
                        Name = birdData.Name,
                        Slug = slug,
                        ScientificName = birdData.ScientificName,
                        AdoptedYear = birdData.Year,
                        Designation = "State bird",
                        Legislation = birdData.Legislation,
                        WikidataId = null,
                        Meaning = birdData.Meaning,
                        ImageUrl = $"/images/birds/{slug}.webp",
                        YamlPath = $"Content/states/{state.Slug}/bird.yml"
                    });
                }
            }

            context.Symbols.AddRange(birds);
            await context.SaveChangesAsync();
        }

        private static async Task SeedStateMottos(AppDbContext context, List<State> states)
        {

            var old = await context.Symbols.Where(s => s.Type == "motto").ToListAsync();
            if (old.Count > 0)
            {
                context.Symbols.RemoveRange(old);
                await context.SaveChangesAsync();
            }


            var stateMottoData = new Dictionary<string, (string Motto, string Language, int Year, string Legislation, string Meaning)>
          {

              { "connecticut", ("Qui Transtulit Sustinet", "Latin", 1662, "Royal Charter of 1662", "He who transplanted still sustains. Reflects the early settlers' faith that God would sustain them in a new land.") },
              { "maine", ("Dirigo", "Latin", 1820, "Resolves of 1820, Chapter 22", "I direct (or I lead). Inspired by the North Star, representing Maine's former status as the northernmost state.") },
              { "massachusetts", ("Ense Petit Placidam Sub Libertate Quietem", "Latin", 1775, "Adopted by the Provincial Congress", "By the sword we seek peace, but peace only under liberty. Attributed to English patriot Algernon Sidney.") },
              { "new-hampshire", ("Live Free or Die", "English", 1945, "Laws 1945, Chapter 152:1", "A fierce statement of independence from Revolutionary War General John Stark's 1809 toast.") },
              { "new-jersey", ("Liberty and Prosperity", "English", 1928, "Joint Resolution No. 8", "Reflects the core foundational values depicted on the state seal.") },
              { "new-york", ("Excelsior", "Latin", 1778, "Adopted with the State Coat of Arms", "Ever upward. A symbol of the state's constant striving for greatness.") },
              { "pennsylvania", ("Virtue, Liberty and Independence", "English", 1875, "Adopted with the State Coat of Arms", "The core principles of William Penn's commonwealth.") },
              { "rhode-island", ("Hope", "English", 1664, "Colonial General Assembly", "Inspired by the biblical phrase from Hebrews: 'hope we have as an anchor of the soul.'") },
              { "vermont", ("Freedom and Unity", "English", 1779, "Adopted with the State Seal", "Balancing personal liberty with the common good of the community.") },


              { "illinois", ("State Sovereignty, National Union", "English", 1868, "Act of March 7, 1867", "Reflects the state's post-Civil War political stance balancing states' rights with federal unity.") },
              { "indiana", ("The Crossroads of America", "English", 1937, "General Assembly Resolution", "Highlights the state's historic role as a national transportation hub.") },
              { "iowa", ("Our Liberties We Prize and Our Rights We Will Maintain", "English", 1847, "First General Assembly Act", "A strong declaration of frontier independence and civil rights.") },
              { "kansas", ("Ad Astra per Aspera", "Latin", 1861, "Joint Resolution No. 4", "To the stars through difficulties. Represents the pioneering struggles of early Kansans.") },
              { "michigan", ("Si Quaeris Peninsulam Amoenam Circumspice", "Latin", 1835, "Constitutional Convention", "If you seek a pleasant peninsula, look about you. A tribute to the state's natural beauty.") },
              { "minnesota", ("L'Étoile du Nord", "French", 1861, "Laws of Minnesota 1861", "The Star of the North. Chosen to reflect the state's geographic position.") },
              { "missouri", ("Salus Populi Suprema Lex Esto", "Latin", 1822, "Act of January 11, 1822", "Let the welfare of the people be the supreme law. From Cicero's 'De Legibus'.") },
              { "nebraska", ("Equality Before the Law", "English", 1867, "Act of June 15, 1867", "Emphasizes justice and equal rights for all citizens, adopted right after the Civil War.") },
              { "north-dakota", ("Liberty and Union Now and Forever, One and Inseparable", "English", 1889, "State Constitution, Article XI", "A famous quote by Daniel Webster advocating for national unity.") },
              { "ohio", ("With God All Things Are Possible", "English", 1959, "ORC Ann. 5.06", "A biblical quote from the Gospel of Matthew (19:26).") },
              { "south-dakota", ("Under God the People Rule", "English", 1889, "State Constitution, Article XXI", "Reflects the strong democratic and religious roots of the state's founders.") },
              { "wisconsin", ("Forward", "English", 1851, "Adopted with the State Seal", "Represents Wisconsin's continuous drive toward progress and leadership.") },


              { "alabama", ("Audemus Jura Nostra Defendere", "Latin", 1923, "Act 1923-420", "We dare defend our rights. Adopted to replace a Reconstruction-era motto.") },
              { "arkansas", ("Regnat Populus", "Latin", 1907, "Act 395 of 1907", "The people rule. Changed from 'Regnant Populi' to correct the Latin grammar.") },
              { "delaware", ("Liberty and Independence", "English", 1847, "Adopted with the State Seal", "Echoes the values of the First State to ratify the Constitution.") },
              { "florida", ("In God We Trust", "English", 2006, "Florida Statutes § 15.0301", "The national motto, officially adopted to reflect the state's deep faith.") },
              { "georgia", ("Wisdom, Justice, and Moderation", "English", 1799, "Adopted with the State Seal", "The three pillars supporting the arch (the Constitution) on the state seal.") },
              { "kentucky", ("United We Stand, Divided We Fall", "English", 1942, "Acts of Assembly 1942", "A classic patriotic phrase featured in 'The Liberty Song' of 1768.") },
              { "louisiana", ("Union, Justice and Confidence", "English", 1902, "Adopted with the State Seal", "The guiding principles of the state's governance.") },
              { "maryland", ("Fatti Maschii, Parole Femine", "Italian", 1874, "Joint Resolution No. 5", "Strong deeds, gentle words. Historically translated as 'Manly deeds, womanly words'.") },
              { "mississippi", ("Virtute et Armis", "Latin", 1894, "Adopted with the State Coat of Arms", "By valor and arms. Reflects the state's military history.") },
              { "north-carolina", ("Esse Quam Videri", "Latin", 1893, "Session Laws 1893, c. 145", "To be, rather than to seem. From Cicero's essay 'On Friendship'.") },
              { "oklahoma", ("Labor Omnia Vincit", "Latin", 1907, "State Constitution, Article VI", "Labor conquers all things. Represents the strong work ethic of the pioneers.") },
              { "south-carolina", ("Dum Spiro Spero", "Latin", 1776, "Adopted with the State Seal", "While I breathe, I hope. Represents optimism during the Revolutionary War.") },
              { "tennessee", ("Agriculture and Commerce", "English", 1987, "Public Chapter 402", "The two historic pillars of Tennessee's economy.") },
              { "texas", ("Friendship", "English", 1930, "House Concurrent Resolution 22", "Derived from the Caddo Indian word 'Tejas', meaning friends or allies.") },
              { "virginia", ("Sic Semper Tyrannis", "Latin", 1776, "Adopted with the State Seal", "Thus always to tyrants. A warning against oppressive governance.") },
              { "west-virginia", ("Montani Semper Liberi", "Latin", 1863, "Joint Resolution No. 9", "Mountaineers are always free. Reflects the state's independence from Virginia.") },


              { "alaska", ("North to the Future", "English", 1967, "Alaska Statutes § 44.09.045", "Represents Alaska as a land of promise and pioneering spirit.") },
              { "arizona", ("Ditat Deus", "Latin", 1911, "State Constitution, Article XXII", "God enriches. A reference to the state's abundant natural resources.") },
              { "california", ("Eureka", "Greek", 1963, "Government Code Section 420.5", "I have found it. A famous reference to the discovery of gold.") },
              { "colorado", ("Nil Sine Numine", "Latin", 1861, "Adopted by the First Territorial Assembly", "Nothing without Providence (or Deity).") },
              { "hawaii", ("Ua Mau ke Ea o ka ʻĀina i ka Pono", "Hawaiian", 1959, "Act 211, Session Laws of 1959", "The life of the land is perpetuated in righteousness. A famous quote by King Kamehameha III.") },
              { "idaho", ("Esto Perpetua", "Latin", 1890, "Adopted with the State Seal", "Let it be perpetual. A wish for the enduring existence of the state.") },
              { "montana", ("Oro y Plata", "Spanish", 1865, "Adopted by the Territorial Legislature", "Gold and silver. A direct reference to the state's immense mining wealth.") },
              { "nevada", ("All for Our Country", "English", 1866, "Adopted with the State Seal", "A patriotic statement adopted shortly after the Civil War.") },
              { "new-mexico", ("Crescit Eundo", "Latin", 1887, "Adopted by the Territorial Legislature", "It grows as it goes. Refers to the increasing prosperity of the territory.") },
              { "oregon", ("Alis Volat Propriis", "Latin", 1987, "Senate Joint Resolution 4", "She flies with her own wings. Emphasizes the state's independent spirit.") },
              { "utah", ("Industry", "English", 1959, "Laws of Utah 1959, Chapter 122", "Reflects the hard work of the early Mormon pioneers, represented by the beehive.") },
              { "washington", ("Al-ki", "Chinook", 1889, "Adopted with the State Seal", "By and by (or 'into the future'). Reflects the early settlers' hope for the future.") },
              { "wyoming", ("Equal Rights", "English", 1955, "Laws 1955, Chapter 102", "Honors Wyoming as the first territory and state to grant women the right to vote.") }
          };

            var mottos = new List<Symbol>();
            foreach (var state in states)
            {
                if (stateMottoData.TryGetValue(state.Slug, out var mottoData))
                {
                    var slug = GenerateSlug(mottoData.Motto);

                    mottos.Add(new Symbol
                    {
                        StateId = state.Id,
                        Type = "motto",
                        Name = mottoData.Motto,
                        Slug = slug,
                        ScientificName = mottoData.Language,
                        AdoptedYear = mottoData.Year,
                        Designation = "State motto",
                        Legislation = mottoData.Legislation,
                        Meaning = mottoData.Meaning,
                        ImageUrl = null,
                        YamlPath = $"Content/states/{state.Slug}/motto.yaml"
                    });
                }
            }

            context.Symbols.AddRange(mottos);
            await context.SaveChangesAsync();
        }

        private static async Task SeedStateNicknames(AppDbContext context, List<State> states)
        {
            var old = await context.Symbols.Where(s => s.Type == "nickname").ToListAsync();
            if (old.Count > 0)
            {
                context.Symbols.RemoveRange(old);
                await context.SaveChangesAsync();
            }


            var stateNicknameData = new Dictionary<string, (string MainNickname, string Status, int Year, string Legislation, string Meaning)>
            {

                { "connecticut", ("The Constitution State", "Official", 1959, "Public Act 59-121", "Refers to the Fundamental Orders of 1638-1639, considered by historians as the first written constitution in America.") },
                { "maine", ("The Pine Tree State", "Unofficial", 0, "", "Refers to the extensive pine forests covering the state and the white pine featured on the state seal.") },
                { "massachusetts", ("The Bay State", "Unofficial", 0, "", "Named after the original Massachusetts Bay Colony established by the Puritans.") },
                { "new-hampshire", ("The Granite State", "Unofficial", 0, "", "Refers to the state's extensive granite formations and historic, massive quarries.") },
                { "new-jersey", ("The Garden State", "Semi-official", 1954, "1954 License Plate Bill", "Promoted by Abraham Browning in 1876; it was added to license plates by the legislature in 1954 despite a gubernatorial veto.") },
                { "new-york", ("The Empire State", "Semi-official", 0, "State License Plates & Tourism", "Attributed to George Washington, who called New York the 'Seat of an Empire' due to its wealth and resources.") },
                { "pennsylvania", ("The Keystone State", "Unofficial", 0, "", "Represents Pennsylvania's central, crucial geographic and political role among the original 13 American colonies.") },
                { "rhode-island", ("The Ocean State", "Semi-official", 1972, "State Tourism Promotion", "Promotes the state's coastal tourism and the geographic fact that no resident is more than a 30-minute drive from the ocean.") },
                { "vermont", ("The Green Mountain State", "Unofficial", 0, "", "A direct translation of the French 'Verts Monts', referencing the state's signature, lush mountain range.") },


                { "illinois", ("The Prairie State", "Unofficial", 0, "", "Refers to the vast, flat, and wildly expansive grasslands that greeted early American settlers.") },
                { "indiana", ("The Hoosier State", "Unofficial", 0, "", "A historic term for an Indiana resident, with heavily debated origins dating back to the 1830s frontier.") },
                { "iowa", ("The Hawkeye State", "Unofficial", 0, "", "Named in honor of Chief Black Hawk, a prominent leader of the Sauk Native American tribe.") },
                { "kansas", ("The Sunflower State", "Unofficial", 0, "", "Celebrates the abundant wild sunflowers that natively blanket the massive Kansas plains.") },
                { "michigan", ("The Wolverine State", "Unofficial", 0, "", "Likely originated during the Toledo War boundary dispute or from the state's early French fur trading days.") },
                { "minnesota", ("The North Star State", "Unofficial", 0, "", "Derived directly from the state's official French motto 'L'Étoile du Nord'.") },
                { "missouri", ("The Show-Me State", "Unofficial", 1899, "", "Widely attributed to Congressman Willard Vandiver, stating that Missourians require proof and action over mere rhetoric.") },
                { "nebraska", ("The Cornhusker State", "Official", 1945, "Laws 1945, Chapter 319", "Replaced 'The Tree Planters' State' to honor the University of Nebraska athletic teams and the state's agricultural dominance.") },
                { "north-dakota", ("The Peace Garden State", "Official", 1957, "Session Laws 1957, Chapter 243", "Honors the International Peace Garden situated perfectly on the border of North Dakota and Manitoba, Canada.") },
                { "ohio", ("The Buckeye State", "Unofficial", 0, "", "Named for the native Ohio buckeye trees that once densely blanketed the region's forests.") },
                { "south-dakota", ("The Mount Rushmore State", "Official", 1992, "Laws 1992, Chapter 5", "Highlights the state's most globally famous landmark, replacing the former 'Sunshine State' nickname.") },
                { "wisconsin", ("The Badger State", "Unofficial", 0, "", "Named for the 1820s lead miners who dug hillside tunnels to live in during brutal winters, resembling burrowing badgers.") },


                { "alabama", ("The Yellowhammer State", "Unofficial", 0, "", "References the Civil War nickname for Alabama Confederate troops who wore bright yellow cloth on their cavalry uniforms.") },
                { "arkansas", ("The Natural State", "Official", 1995, "Act 1352 of 1995", "Promotes the state's pristine natural beauty and outdoor tourism, replacing the former 'Land of Opportunity' nickname.") },
                { "delaware", ("The First State", "Official", 2002, "Volume 73, Chapter 266", "Honors Delaware as the very first state to ratify the United States Constitution on December 7, 1787.") },
                { "florida", ("The Sunshine State", "Official", 1997, "Florida Statutes § 15.045", "A long-standing promotional and tourism phrase officially codified to celebrate Florida's warm, sunny climate.") },
                { "georgia", ("The Peach State", "Unofficial", 0, "", "Recognizes Georgia's deep historic reputation for producing exceptionally high-quality peaches.") },
                { "kentucky", ("The Bluegrass State", "Unofficial", 0, "", "Named for the fertile, bluish-flowering grass pastures in the central part of the state, famous for breeding racehorses.") },
                { "louisiana", ("The Pelican State", "Unofficial", 0, "", "A tribute to the state bird, which is deeply tied to Louisiana's coastal ecology and historical heraldry.") },
                { "maryland", ("The Old Line State", "Unofficial", 0, "", "Attributed to George Washington, who praised the extraordinarily brave 'Maryland Line' troops of the Revolutionary War.") },
                { "mississippi", ("The Magnolia State", "Unofficial", 0, "", "Honors the massive abundance of beautiful magnolia trees and fragrant flowers found throughout the state.") },
                { "north-carolina", ("The Tar Heel State", "Unofficial", 0, "", "Dates back to the state's early colonial history as a leading producer of tar, pitch, and turpentine for naval ships.") },
                { "oklahoma", ("The Sooner State", "Unofficial", 0, "", "Refers to the eager pioneers who entered the Unassigned Lands before the official start of the 1889 Land Rush.") },
                { "south-carolina", ("The Palmetto State", "Unofficial", 0, "", "Refers to the resilient native palmetto trees whose spongy wood helped defend a key fort during the Revolutionary War.") },
                { "tennessee", ("The Volunteer State", "Unofficial", 0, "", "Recognizes the remarkably massive turnout of volunteer soldiers from Tennessee during the War of 1812.") },
                { "texas", ("The Lone Star State", "Unofficial", 0, "", "A tribute to the single star on the state flag, representing Texas's unique history as an independent republic.") },
                { "virginia", ("The Old Dominion", "Unofficial", 0, "", "A historic title granted by King Charles II for Virginia's fierce loyalty to the Crown during the English Civil War.") },
                { "west-virginia", ("The Mountain State", "Unofficial", 0, "", "Refers to the state's completely mountainous and rugged terrain, nestled deeply within the Appalachians.") },


                { "alaska", ("The Last Frontier", "Official", 0, "Featured on State License Plates", "Reflects Alaska's vast, wild, and rugged, largely unsettled landscapes and massive wilderness.") },
                { "arizona", ("The Grand Canyon State", "Official", 1981, "Laws 1981, Chapter 243", "Officially honors Arizona's most globally renowned natural wonder and tourist destination.") },
                { "california", ("The Golden State", "Official", 1968, "Government Code Section 420.7", "A direct reference to the historic 1849 Gold Rush and the state's rolling, golden-hued summer hills.") },
                { "colorado", ("The Centennial State", "Unofficial", 0, "", "Celebrates Colorado becoming a state in 1876, exactly 100 years after the signing of the Declaration of Independence.") },
                { "hawaii", ("The Aloha State", "Official", 1959, "Act 1, Session Laws of Hawaii 1959", "Officially adopted just before statehood, representing the welcoming Hawaiian spirit of peace and affection.") },
                { "idaho", ("The Gem State", "Unofficial", 0, "", "A translation of the presumed Native American word 'Idaho', celebrating the state's abundant natural minerals.") },
                { "montana", ("The Treasure State", "Unofficial", 0, "", "A nod to the massive gold, silver, and copper mining wealth that originally drove Montana's early economy.") },
                { "nevada", ("The Silver State", "Unofficial", 0, "", "Refers to the historic, economy-defining silver-mining boom, particularly the famous Comstock Lode.") },
                { "new-mexico", ("The Land of Enchantment", "Official", 1999, "Laws 1999, Chapter 264", "Highlights the state's mesmerizing scenic beauty and its incredibly rich, diverse cultural heritage.") },
                { "oregon", ("The Beaver State", "Unofficial", 0, "", "A tribute to the animal that powered the early fur trade and exploration of the Pacific Northwest.") },
                { "utah", ("The Beehive State", "Unofficial", 0, "", "Symbolizes the intense industry and cooperative hard work of the early Mormon pioneers who settled the region.") },
                { "washington", ("The Evergreen State", "Semi-official", 0, "Featured on State License Plates", "Proposed by pioneer C.T. Conover to describe the state's lush, endlessly green coniferous forests.") },
                { "wyoming", ("The Equality State", "Official", 1990, "Wyoming Statutes § 8-3-118", "Honors Wyoming as the first government in the world to grant women full, unrestricted voting rights.") }
            };

            var nicknames = new List<Symbol>();
            foreach (var state in states)
            {
                if (stateNicknameData.TryGetValue(state.Slug, out var nicknameData))
                {
                    var slug = GenerateSlug(nicknameData.MainNickname);

                    nicknames.Add(new Symbol
                    {
                        StateId = state.Id,
                        Type = "nickname",
                        Name = nicknameData.MainNickname,
                        Slug = slug,
                        Status = nicknameData.Status,
                        Designation = "State nickname",
                        Legislation = nicknameData.Legislation,
                        Meaning = nicknameData.Meaning,
                        AdoptedYear = nicknameData.Year > 0 ? nicknameData.Year : null,
                        ImageUrl = $"/images/nicknames/{state.Slug}.webp",
                        YamlPath = $"Content/states/{state.Slug}/nickname.yaml"
                    });
                }
            }

            context.Symbols.AddRange(nicknames);
            await context.SaveChangesAsync();
        }

        private static async Task SeedStateFlowers(AppDbContext context, List<State> states)
        {
            var old = await context.Symbols.Where(s => s.Type == "flower").ToListAsync();
            if (old.Count > 0)
            {
                context.Symbols.RemoveRange(old);
                await context.SaveChangesAsync();
            }


            var stateFlowerData = new Dictionary<string, (string Name, string Scientific, bool IsOfficial, int Year, string Legislation, string WikidataId, string Meaning)>
            {
                { "alabama", ("Camellia", "Camellia japonica", true, 1959, "Act No. 124", "Q160121", "Replaced the goldenrod to honor a flower deeply rooted in Southern garden traditions.") },
                { "alaska", ("Forget-me-not", "Myosotis alpestris", true, 1949, "Territorial Legislature Act", "Q163620", "Chosen by the Grand Igloo of the Pioneers of Alaska; its blue color represents the Alaskan sky.") },
                { "arizona", ("Saguaro Cactus Blossom", "Carnegiea gigantea", true, 1931, "Laws 1931, Ch. 68", "Q275573", "Represents the majestic, towering saguaro cactus that uniquely dominates the Sonoran Desert.") },
                { "arkansas", ("Apple Blossom", "Malus domestica", true, 1901, "General Assembly Resolution", "Q158657", "Selected to highlight the state's booming apple-growing industry in the early 20th century.") },
                { "california", ("California Poppy", "Eschscholzia californica", true, 1903, "State Legislature Act", "Q158795", "Its vibrant orange-gold color symbolizes the 'Golden State' and the historic 1849 Gold Rush.") },
                { "colorado", ("Rocky Mountain Columbine", "Aquilegia coerulea", true, 1899, "General Assembly Act", "Q2561781", "Voted by schoolchildren; the blue represents the sky, white the snow, and yellow the gold mining history.") },
                { "connecticut", ("Mountain Laurel", "Kalmia latifolia", true, 1907, "General Statutes Sec. 3-108", "Q1235131", "A resilient, beautiful evergreen shrub that blooms abundantly across the rocky New England hills.") },
                { "delaware", ("Peach Blossom", "Prunus persica", true, 1895, "Volume 20, Chapter 210", "Q13189", "Adopted to celebrate Delaware's massive agricultural success as the 'Peach State' in the late 1800s.") },
                { "florida", ("Orange Blossom", "Citrus sinensis", true, 1909, "Legislative Resolution", "Q108906", "A universally recognized symbol of Florida's famous citrus industry and warm, fragrant climate.") },
                { "georgia", ("Cherokee Rose", "Rosa laevigata", true, 1916, "General Assembly Resolution", "Q1070309", "Legend says these white roses sprang from the tears of Native American mothers during the Trail of Tears.") },

                { "hawaii", ("Hibiscus", "Hibiscus brackenridgei", true, 1923, "Territorial Legislature Act", "Q13099321", "The native yellow hibiscus (Pua Aloalo) was officially specified in 1988 to represent Hawaii's tropical beauty.") },
                { "idaho", ("Syringa", "Philadelphus lewisii", true, 1931, "Session Laws 1931", "Q7182890", "A fragrant, white-flowering shrub first documented by the Lewis and Clark expedition in Idaho.") },
                { "illinois", ("Violet", "Viola sororia", true, 1908, "State Legislature Act", "Q1077055", "Voted overwhelmingly by Illinois schoolchildren as their favorite native woodland flower.") },
                { "indiana", ("Peony", "Paeonia officinalis", true, 1957, "Acts of 1957", "Q159738", "Replaced the zinnia; widely grown across Indiana, it blooms brilliantly just before Memorial Day.") },
                { "iowa", ("Wild Prairie Rose", "Rosa arkansana", true, 1897, "Concurrent Resolution", "Q145631", "Chosen for its resilience in the harsh prairie climate, blooming brightly across Iowa's summer fields.") },
                { "kansas", ("Sunflower", "Helianthus annuus", true, 1903, "Laws 1903, Chapter 479", "Q171497", "A towering native plant that follows the sun, representing the bright, expansive Kansas prairies.") },
                { "kentucky", ("Goldenrod", "Solidago gigantea", true, 1926, "Acts of Assembly 1926", "Q609627", "A vibrant native plant that blooms vividly across Kentucky's meadows in late summer and fall.") },
                { "louisiana", ("Magnolia", "Magnolia grandiflora", true, 1900, "Legislative Act", "Q161116", "A classic Southern symbol, renowned for its massive, fragrant white blossoms and deep green leaves.") },
                { "maine", ("White Pine Cone and Tassel", "Pinus strobus", true, 1895, "Legislative Resolve", "Q157230", "Unique among state flowers, it represents Maine's deep history in forestry and shipbuilding.") },
                { "maryland", ("Black-eyed Susan", "Rudbeckia hirta", true, 1918, "Chapter 458, Acts of 1918", "Q2532820", "Chosen because its black and gold colors perfectly match the historic Calvert family crest of Maryland.") },

                { "massachusetts", ("Mayflower", "Epigaea repens", true, 1918, "Chapter 181", "Q4532195", "A delicate, early-blooming trailing arbutus honoring the Pilgrims who arrived on the ship 'Mayflower'.") },
                { "michigan", ("Apple Blossom", "Malus domestica", true, 1897, "Joint Resolution 10", "Q158657", "Adopted to highlight Michigan's prominence as one of the top apple-producing states in the nation.") },
                { "minnesota", ("Pink and White Ladys Slipper", "Cypripedium reginae", true, 1902, "Legislative Act", "Q977605", "A rare, beautiful wild orchid found deep in Minnesota's bogs, strictly protected by state conservation laws.") },
                { "mississippi", ("Magnolia", "Magnolia grandiflora", true, 1952, "Legislative Act", "Q161116", "Voted on by schoolchildren in 1900, but not officially codified into law until over half a century later.") },
                { "missouri", ("White Hawthorn Blossom", "Crataegus punctata", true, 1923, "Laws of Missouri 1923", "Q4095586", "A highly prized, rugged shrub native to Missouri, offering beautiful white flowers in the spring.") },
                { "montana", ("Bitterroot", "Lewisia rediviva", true, 1895, "Legislative Act", "Q4113860", "A culturally significant plant with a nutritious root, historically vital to Native American diets in the Rockies.") },
                { "nebraska", ("Goldenrod", "Solidago gigantea", true, 1895, "Laws 1895", "Q609627", "Selected to represent the pioneer spirit, as it thrives and brings vibrant color to the rough prairie landscape.") },
                { "nevada", ("Sagebrush", "Artemisia tridentata", true, 1917, "State Legislature Act", "Q2117903", "The defining, incredibly resilient shrub of the Great Basin, known for its distinctive, sharp fragrance.") },
                { "new-hampshire", ("Purple Lilac", "Syringa vulgaris", true, 1919, "Laws 1919, Chapter 148", "Q6565319", "Reflects the tough character of the men and women of New Hampshire, enduring long, harsh winters.") },
                { "new-jersey", ("Violet", "Viola sororia", true, 1913, "Legislative Resolution", "Q1077055", "A familiar, resilient spring flower that blooms widely across the state's woodlands and gardens.") },

                { "new-mexico", ("Yucca Flower", "Yucca glauca", true, 1927, "Laws 1927, Chapter 102", "Q882878", "Known as 'Our Lord's Candles', these towering desert blooms were highly valued by early pioneers.") },
                { "new-york", ("Rose", "Rosa", true, 1955, "Laws 1955", "Q34687", "Voted by schoolchildren in 1891, the rose officially became the state flower to symbolize love and beauty.") },
                { "north-carolina", ("Dogwood", "Cornus florida", true, 1941, "Session Laws 1941", "Q887221", "A breathtaking spring-blooming tree found abundantly from North Carolina's mountains to its coast.") },
                { "north-dakota", ("Wild Prairie Rose", "Rosa arkansana", true, 1907, "Session Laws 1907", "Q145631", "A native, fiercely resilient flower that flourishes along roadsides and pastures throughout the state.") },
                { "ohio", ("Scarlet Carnation", "Dianthus caryophyllus", true, 1904, "General Code 29", "Q158984", "Adopted specifically to honor native Ohioan President William McKinley, who often wore one in his lapel.") },
                { "oklahoma", ("Oklahoma Rose", "Rosa oklahomensis", true, 2004, "House Bill 2004", "Q34687", "A hybrid tea rose specifically developed at Oklahoma State University, deeply fragrant and vibrant.") },
                { "oregon", ("Oregon Grape", "Mahonia aquifolium", true, 1899, "Senate Concurrent Resolution", "Q158303", "A resilient, native evergreen shrub bearing clusters of yellow flowers and tart, edible blue berries.") },
                { "pennsylvania", ("Mountain Laurel", "Kalmia latifolia", true, 1933, "Act of 1933", "Q1235131", "Selected by Governor Gifford Pinchot to honor the beautiful, blooming hills of the Appalachian mountains.") },
                { "rhode-island", ("Violet", "Viola sororia", true, 1968, "Public Laws 1968", "Q1077055", "Though chosen by schoolchildren in 1897, it wasn't officially codified until decades later.") },
                { "south-carolina", ("Yellow Jessamine", "Gelsemium sempervirens", true, 1924, "Legislative Act", "Q978130", "Its return in early spring signifies the enduring beauty and hospitality of the Palmetto State.") },

                { "south-dakota", ("American Pasqueflower", "Pulsatilla patens", true, 1903, "Laws 1903", "Q149341", "One of the very first wildflowers to bloom through the melting snow on the South Dakota prairie.") },
                { "tennessee", ("Iris", "Iris", true, 1933, "Public Chapter 51", "Q156150", "The purple iris was chosen after a heavily contested debate, replacing the previously designated passion flower.") },
                { "texas", ("Bluebonnet", "Lupinus texensis", true, 1901, "Legislative Act", "Q4115567", "These legendary blue wildflowers blanket Texas highways in the spring, deeply tied to state folklore.") },
                { "utah", ("Sego Lily", "Calochortus nuttallii", true, 1911, "Laws 1911", "Q2934343", "A sacred plant to Native Americans; its nutritious bulb saved early Mormon pioneers from starvation.") },
                { "vermont", ("Red Clover", "Trifolium pratense", true, 1895, "Acts of 1895", "Q156635", "Chosen to honor the state's deeply rooted agricultural heritage and its importance to dairy farming.") },
                { "virginia", ("Dogwood", "Cornus florida", true, 1918, "Acts of Assembly 1918", "Q887221", "Virginia is uniquely the only state to have the same plant as both its official state flower and state tree.") },
                { "washington", ("Coast Rhododendron", "Rhododendron macrophyllum", true, 1959, "Legislative Act", "Q2714739", "Chosen by Washington women in 1892 for the Chicago World's Fair, known for its massive, showy blooms.") },
                { "west-virginia", ("Rhododendron", "Rhododendron maximum", true, 1903, "Joint Resolution 8", "Q7061732", "Voted by schoolchildren, this striking 'Great Laurel' flourishes in the deep ravines of the Appalachians.") },
                { "wisconsin", ("Wood Violet", "Viola sororia", true, 1909, "Laws 1909", "Q1077055", "Selected on Arbor Day by state schoolchildren, symbolizing Wisconsin's beautiful, delicate spring landscapes.") },
                { "wyoming", ("Indian Paintbrush", "Castilleja linariifolia", true, 1917, "State Legislature Act", "Q265620", "A vividly bright, semi-parasitic desert flower that paints the rugged Wyoming landscape in brilliant reds.") }
            };

            var flowers = new List<Symbol>();

            foreach (var state in states)
            {
                if (stateFlowerData.TryGetValue(state.Slug, out var flower))
                {
                    var slug = GenerateSlug(flower.Name);

                    flowers.Add(new Symbol
                    {
                        StateId = state.Id,
                        Type = "flower",
                        Name = flower.Name,
                        ScientificName = flower.Scientific,
                        Slug = slug,
                        AdoptedYear = flower.Year > 0 ? flower.Year : null,
                        Designation = "State flower",
                        Legislation = flower.Legislation,
                        WikidataId = null,
                        Meaning = flower.Meaning,
                        ImageUrl = $"/images/flowers/{slug}.webp",
                        YamlPath = $"Content/states/{state.Slug}/flower.yaml"
                    });
                }
            }

            context.Symbols.AddRange(flowers);
            await context.SaveChangesAsync();
        }

        private static async Task SeedStateFlags(AppDbContext context, List<State> states)
        {
            var old = await context.Symbols.Where(s => s.Type == "flag").ToListAsync();
            if (old.Count > 0)
            {
                context.Symbols.RemoveRange(old);
                await context.SaveChangesAsync();
            }


            var stateFlagData = new Dictionary<string, (string Name, int AdoptedYear, int? StandardizedYear, string Legislation, string WikidataId, string Meaning)>
            {
                { "alabama", ("Alabama State Flag", 1895, 1967, "Act 95-383", "Q49544", "Features a crimson cross of St. Andrew on a white field, reminiscent of the Confederate battle flag.") },
                { "alaska", ("Alaska State Flag", 1927, 1959, "Laws 1927, Chapter 14", "Q131316", "Designed by 13-year-old Benny Benson, it features eight gold stars forming the Big Dipper and the North Star.") },
                { "arizona", ("Arizona State Flag", 1917, null, "Laws 1917, Chapter 7", "Q300431", "Features 13 rays of red and yellow representing the original colonies and western sunsets, centered by a copper star for mining.") },
                { "arkansas", ("Arkansas State Flag", 1913, 1924, "Senate Concurrent Resolution 11", "Q110398", "A diamond shape representing the state's diamond mines, with 25 stars honoring its admission as the 25th state.") },
                { "california", ("California State Flag", 1911, 1953, "Chapter 9, Statutes of 1911", "Q158485", "Known as the Bear Flag, it originated from the 1846 Bear Flag Revolt against Mexico.") },
                { "colorado", ("Colorado State Flag", 1911, 1964, "Senate Bill 118", "Q211113", "The colors represent the state's environmental features: blue skies, gold sunshine, white snow-capped mountains, and red earth.") },
                { "connecticut", ("Connecticut State Flag", 1897, 1957, "Chapter 227", "Q151249", "Features the state shield with three grapevines representing the three oldest settlements: Windsor, Wethersfield, and Hartford.") },
                { "delaware", ("Delaware State Flag", 1913, null, "Volume 27, Chapter 166", "Q18640", "Uses the buff and colonial blue colors of General George Washington's uniform, featuring the state coat of arms.") },
                { "florida", ("Florida State Flag", 1899, 1985, "Joint Resolution No. 4", "Q46522", "Displays a red saltire (cross of St. Andrew) with the state seal proudly superimposed in the center.") },
                { "georgia", ("Georgia State Flag", 2003, null, "House Bill 380", "Q119159", "Bears the state coat of arms and the motto 'In God We Trust', surrounded by 13 stars for the original colonies.") },

                { "hawaii", ("Hawaii State Flag", 1845, 1959, "Royal Decree of King Kamehameha III", "Q500437", "The 'Ka Hae Hawaii' uniquely features the British Union Jack and eight stripes representing the major Hawaiian islands.") },
                { "idaho", ("Idaho State Flag", 1907, 1957, "Session Laws 1907", "Q208003", "Features the state seal on a field of blue, uniquely depicting a miner and a woman representing equality, liberty, and justice.") },
                { "illinois", ("Illinois State Flag", 1915, 1970, "Senate Bill 446", "Q464227", "Features the state seal with a bald eagle holding a banner. The word 'ILLINOIS' was added in 1970 to ensure it was easily identifiable.") },
                { "indiana", ("Indiana State Flag", 1917, null, "Chapter 14", "Q488339", "Displays a golden torch representing liberty and enlightenment, surrounded by 19 stars honoring its entry as the 19th state.") },
                { "iowa", ("Iowa State Flag", 1921, null, "Chapter 282", "Q500366", "A vertical tricolor of blue, white, and red (reflecting its French colonial history) with an eagle carrying the state motto.") },
                { "kansas", ("Kansas State Flag", 1927, 1961, "Laws 1927, Chapter 281", "Q500057", "Features the state seal and a sunflower on a blue field. The word 'KANSAS' was officially added in 1961.") },
                { "kentucky", ("Kentucky State Flag", 1918, 1962, "Acts 1918, Chapter 40", "Q46559", "Shows the state seal on a navy blue field, beautifully surrounded by goldenrod sprigs, the official state flower.") },
                { "louisiana", ("Louisiana State Flag", 1912, 2006, "Act 39 of 1912", "Q301824", "Features the pelican in her piety, tearing her breast to feed her young, an ancient symbol of charity and protection.") },
                { "maine", ("Maine State Flag", 1909, null, "Public Laws 1909", "Q495610", "Displays the state coat of arms on a blue field, featuring a moose, a pine tree, a farmer, and a sailor.") },
                { "maryland", ("Maryland State Flag", 1904, null, "Chapter 48", "Q328608", "The only US state flag based strictly on English heraldry, combining the Calvert and Crossland family coats of arms.") },

                { "massachusetts", ("Massachusetts State Flag", 1908, 1971, "Chapter 229", "Q158499", "Features an Algonquian Native American holding a bow and arrow, with a white star representing the state itself.") },
                { "michigan", ("Michigan State Flag", 1911, null, "Public Act 209", "Q193630", "Showcases the state coat of arms depicting an elk, moose, and bald eagle, emphasizing the state's rich wildlife and peninsula.") },
                { "minnesota", ("Minnesota State Flag", 1893, 2023, "Laws 1893, Chapter 16", "Q190240", "Historically featured the state seal. It was officially redesigned in 2024 to feature an eight-pointed North Star and blue waters.") },
                { "mississippi", ("Mississippi State Flag", 2021, null, "House Bill 1", "Q104771457", "The 'New Magnolia' flag, overwhelmingly chosen by voters to feature a white magnolia blossom and the motto 'In God We Trust'.") },
                { "missouri", ("Missouri State Flag", 1913, null, "Laws 1913", "Q495287", "A red, white, and blue tricolor honoring its French heritage, centered with the state seal and 24 stars.") },
                { "montana", ("Montana State Flag", 1905, 1981, "Laws 1905", "Q488421", "Displays the state seal on a blue field, showcasing the Great Falls of the Missouri River and traditional mining tools.") },
                { "nebraska", ("Nebraska State Flag", 1925, 1963, "Laws 1925, Chapter 151", "Q301844", "Features the state seal in gold and silver on a national blue field, depicting the vital importance of agriculture and industry.") },
                { "nevada", ("Nevada State Flag", 1929, 1991, "Laws 1929", "Q495530", "Features a silver star and the words 'Battle Born', signifying its admission to the Union during the heat of the Civil War.") },
                { "new-hampshire", ("New Hampshire State Flag", 1909, 1931, "Chapter 16", "Q495449", "Centers the state seal depicting the frigate USS Raleigh, built in Portsmouth in 1776, surrounded by a wreath of laurel.") },
                { "new-jersey", ("New Jersey State Flag", 1896, null, "Joint Resolution 2", "Q301740", "The state coat of arms rests on a unique buff-colored background, chosen by George Washington for his continental regiments.") },

                { "new-mexico", ("New Mexico State Flag", 1925, null, "Laws 1925, Chapter 115", "Q300438", "Features the ancient Zia sun symbol in red on a field of Spanish yellow, honoring both Native American and Spanish roots.") },
                { "new-york", ("New York State Flag", 1901, null, "Laws 1901", "Q46540", "The state coat of arms featuring Liberty and Justice supporting a shield with a sun rising brilliantly over the Hudson River.") },
                { "north-carolina", ("North Carolina State Flag", 1885, null, "Chapter 291", "Q46513", "Bears the dates of the Mecklenburg Declaration and the Halifax Resolves, highlighting early defiance against Britain.") },
                { "north-dakota", ("North Dakota State Flag", 1911, 1943, "Chapter 283", "Q301764", "Based almost identically on the regimental flag carried by the First North Dakota Infantry during the Spanish-American War.") },
                { "ohio", ("Ohio State Flag", 1902, null, "Laws 1902", "Q495111", "The only non-rectangular US state flag; a unique swallowtail burgee designed by architect John Eisenmann.") },
                { "oklahoma", ("Oklahoma State Flag", 1925, 1941, "Laws 1925, Chapter 234", "Q500201", "An Osage Nation buffalo-skin shield overlaid with a ceremonial peace pipe and an olive branch, symbolizing deep-rooted peace.") },
                { "oregon", ("Oregon State Flag", 1925, null, "Chapter 227", "Q464205", "The only US state flag with different designs on each side: the state seal on the obverse and a beaver on the reverse.") },
                { "pennsylvania", ("Pennsylvania State Flag", 1907, null, "Act 233", "Q301815", "The state coat of arms flanked by draft horses, with a bald eagle crest, representing robust strength and independence.") },
                { "rhode-island", ("Rhode Island State Flag", 1897, null, "Chapter 460", "Q151241", "A golden anchor surrounded by 13 gold stars, representing the 13 original colonies and the state motto 'Hope'.") },
                { "south-carolina", ("South Carolina State Flag", 1861, null, "General Assembly Resolution", "Q463283", "Features a white palmetto tree and crescent on an indigo field, a design originating from Revolutionary War defenses.") },

                { "south-dakota", ("South Dakota State Flag", 1909, 1992, "Laws 1909", "Q301726", "The state seal surrounded by golden triangles representing the sun's rays and the state's abundantly sunny climate.") },
                { "tennessee", ("Tennessee State Flag", 1905, null, "Chapter 498", "Q500055", "Three white stars in a blue circle represent the three grand divisions of the state: East, Middle, and West Tennessee.") },
                { "texas", ("Texas State Flag", 1839, null, "Republic of Texas Congress", "Q200902", "The famous 'Lone Star Flag', symbolizing all of Texas standing defiantly and proudly united as one.") },
                { "utah", ("Utah State Flag", 1913, 2011, "Laws 1913", "Q202685", "Historically featured the state seal. Redesigned in 2024 to feature a modern beehive, a white mountain peak, and a gold star.") },
                { "vermont", ("Vermont State Flag", 1923, null, "Act No. 3", "Q301683", "The state coat of arms depicting a pine tree, a cow, and sheaves of wheat, heavily reflecting its agricultural heritage.") },
                { "virginia", ("Virginia State Flag", 1861, 1930, "Secession Convention", "Q495574", "Features the state seal depicting Virtus standing victorious over a fallen tyrant, embodying the motto 'Sic Semper Tyrannis'.") },
                { "washington", ("Washington State Flag", 1923, 1967, "Chapter 174", "Q466144", "The only US state flag with a green background, featuring the portrait of America's first president, George Washington.") },
                { "west-virginia", ("West Virginia State Flag", 1929, null, "Joint Resolution 18", "Q462446", "The state seal wreathed by rhododendrons, depicting a farmer and a miner to represent the state's foundational industries.") },
                { "wisconsin", ("Wisconsin State Flag", 1913, 1979, "Chapter 111", "Q301712", "The state coat of arms showing a sailor and a miner, symbolizing the state's intense labor on both water and land.") },
                { "wyoming", ("Wyoming State Flag", 1917, null, "Chapter 8", "Q220815", "A white bison silhouette bearing the state seal on a blue field bordered by white and red, representing native wildlife.") }
            };

            var flags = new List<Symbol>();

            foreach (var state in states)
            {
                if (stateFlagData.TryGetValue(state.Slug, out var flag))
                {
                    flags.Add(new Symbol
                    {
                        StateId = state.Id,
                        Type = "flag",
                        Name = flag.Name,
                        ScientificName = null,
                        Slug = GenerateSlug(flag.Name),
                        Status = "Official",
                        Designation = "State flag",
                        AdoptedYear = flag.AdoptedYear,

                        Legislation = flag.Legislation,
                        WikidataId = null,
                        Meaning = flag.Meaning,

                        ImageUrl = $"/images/flags/{state.Slug}/flag.webp",
                        YamlPath = $"Content/states/{state.Slug}/flag.yaml"
                    });
                }
            }

            context.Symbols.AddRange(flags);
            await context.SaveChangesAsync();
        }

        private static async Task SeedStateTrees(AppDbContext context, List<State> states)
        {
            var old = await context.Symbols.Where(s => s.Type == "tree").ToListAsync();
            if (old.Count > 0)
            {
                context.Symbols.RemoveRange(old);
                await context.SaveChangesAsync();
            }


            var stateTreeData = new Dictionary<string, (string Name, string Scientific, bool IsOfficial, int Year, string Legislation, string WikidataId, string Meaning)>
            {
                { "alabama", ("Southern Longleaf Pine", "Pinus palustris", true, 1949, "Act No. 143", "Q148542", "Vital to the state's early lumber industry and chosen to represent the vast southern pine forests.") },
                { "alaska", ("Sitka Spruce", "Picea sitchensis", true, 1962, "Alaska Statutes § 44.09.070", "Q147426", "A tall, majestic evergreen reflecting Alaska's immense and pristine temperate rainforests.") },
                { "arizona", ("Palo Verde", "Parkinsonia florida", true, 1954, "Laws 1954, Chapter 134", "Q2052735", "Its name means 'green stick' in Spanish, known for its bright yellow spring blooms in the desert.") },
                { "arkansas", ("Loblolly Pine", "Pinus taeda", true, 1939, "Act 53", "Q368248", "A major timber resource for Arkansas, representing the state's abundant and vital pine forests.") },
                { "california", ("California Redwood", "Sequoia sempervirens", true, 1937, "Chapter 112", "Q150129", "The tallest living things on Earth, symbolizing the massive scale and natural grandeur of California.") },
                { "colorado", ("Colorado Blue Spruce", "Picea pungens", true, 1939, "House Bill 130", "Q146025", "First discovered on Pikes Peak, its striking blue-silver needles represent Colorado's mountainous beauty.") },
                { "connecticut", ("White Oak", "Quercus alba", true, 1947, "Public Act 47-22", "Q469555", "Honors the legendary Charter Oak, which hid and saved the Connecticut colony's charter from the English in 1687.") },
                { "delaware", ("American Holly", "Ilex opaca", true, 1939, "Chapter 65, Volume 42", "Q2712730", "Delaware was once the leading exporter of holiday holly wreaths in the United States.") },
                { "florida", ("Sabal Palm", "Sabal palmetto", true, 1953, "Laws of Florida, Chapter 28126", "Q1088471", "Also known as the cabbage palm, it is a highly resilient native plant deeply tied to Florida's coastal history.") },
                { "georgia", ("Southern Live Oak", "Quercus virginiana", true, 1937, "General Assembly Resolution", "Q1758722", "Flourishes along the coastal plains, offering massive, moss-draped canopies symbolic of the deep South.") },

                { "hawaii", ("Kukui", "Aleurites moluccanus", true, 1959, "Act 14", "Q1160961", "Also known as the candlenut tree, its oil was used by ancient Hawaiians for light, symbolizing enlightenment.") },
                { "idaho", ("Western White Pine", "Pinus monticola", true, 1935, "Session Laws 1935", "Q261309", "Selected for its vital role in the state's timber industry and the history of loggers in the Idaho panhandle.") },
                { "illinois", ("White Oak", "Quercus alba", true, 1973, "Public Act 78-430", "Q469555", "Voted by schoolchildren, replacing the generic 'Oak', representing the state's strong prairie woodlands.") },
                { "indiana", ("Tulip Tree", "Liriodendron tulipifera", true, 1931, "Acts of 1931", "Q158783", "Also called the yellow poplar, it bears beautiful tulip-like flowers and was widely used by early pioneers for timber.") },
                { "iowa", ("Oak", "Quercus", true, 1961, "House Concurrent Resolution 15", "Q12004", "Designated generally to honor the abundant oak forests that provided shelter and wood for Iowa's early settlers.") },
                { "kansas", ("Cottonwood", "Populus deltoides", true, 1937, "Laws 1937, Chapter 349", "Q149319", "Provided crucial shade and building material for early pioneers navigating the flat, windswept Kansas plains.") },
                { "kentucky", ("Tulip Poplar", "Liriodendron tulipifera", true, 1994, "Ky. Acts ch. 248", "Q158783", "Replaced the Kentucky coffeetree; a majestic, towering tree that provided excellent lumber for early Kentuckians.") },
                { "louisiana", ("Bald Cypress", "Taxodium distichum", true, 1963, "Act 49 of 1963", "Q148950", "A swamp-dwelling giant with 'knees' protruding from the water, iconic to Louisiana's bayou ecosystems.") },
                { "maine", ("Eastern White Pine", "Pinus strobus", true, 1945, "Legislative Resolve", "Q157230", "Also featured on the state flag, honoring Maine's nickname 'The Pine Tree State' and its historic shipbuilding industry.") },
                { "maryland", ("White Oak", "Quercus alba", true, 1941, "Chapter 731, Acts of 1941", "Q469555", "Honors the legendary Wye Oak, a massive tree that stood in Talbot County for over 400 years.") },

                { "massachusetts", ("American Elm", "Ulmus americana", true, 1941, "Chapter 41", "Q469382", "Commemorates the 'Liberty Tree' in Boston, under which the Sons of Liberty gathered before the American Revolution.") },
                { "michigan", ("Eastern White Pine", "Pinus strobus", true, 1955, "Public Act 7", "Q157230", "Symbolizes the massive logging boom of the late 1800s that built Michigan's early economy.") },
                { "minnesota", ("Red Pine", "Pinus resinosa", true, 1953, "Laws 1953, Chapter 20", "Q2045958", "Also known as the Norway pine, a tall, sturdy tree standing perfectly straight in Minnesota's dense northern forests.") },
                { "mississippi", ("Southern Magnolia", "Magnolia grandiflora", true, 1938, "Laws 1938, Chapter 365", "Q161116", "Voted on by schoolchildren; cementing Mississippi's iconic identity as 'The Magnolia State'.") },
                { "missouri", ("Flowering Dogwood", "Cornus florida", true, 1955, "Laws of Missouri 1955", "Q887221", "A beautiful understory tree that spectacularly lights up Missouri's spring forests with white bracts.") },
                { "montana", ("Ponderosa Pine", "Pinus ponderosa", true, 1949, "Laws 1949, Chapter 150", "Q460523", "Selected by the state's schoolchildren; prized for its straight timber and distinctive vanilla-scented bark.") },
                { "nebraska", ("Cottonwood", "Populus deltoides", true, 1972, "Legislative Bill 1089", "Q149319", "Replaced the American Elm; it honors the resilient tree that provided life-saving shade to Great Plains pioneers.") },
                { "nevada", ("Single-leaf Pinyon", "Pinus monophylla", true, 1953, "NRS 235.040", "Q583885", "A small, incredibly drought-resistant pine whose nuts were a critical food source for Native Americans.") },
                { "new-hampshire", ("White Birch", "Betula papyrifera", true, 1947, "Laws 1947, Chapter 158", "Q76971", "Also known as the paper birch, its distinctive white bark is an iconic sight against the New Hampshire mountains.") },
                { "new-jersey", ("Northern Red Oak", "Quercus rubra", true, 1950, "Joint Resolution No. 5", "Q147525", "A sturdy, handsome shade tree offering brilliant red fall foliage, representing strength and endurance.") },

                { "new-mexico", ("Pinyon Pine", "Pinus edulis", true, 1949, "Laws 1949, Chapter 142", "Q133096", "A rugged, slow-growing desert pine whose edible nuts are a staple of New Mexican cuisine and culture.") },
                { "new-york", ("Sugar Maple", "Acer saccharum", true, 1956, "Laws 1956", "Q214733", "Voted by schoolchildren; famous for its brilliant autumn colors and sweet, historically important maple syrup.") },
                { "north-carolina", ("Pine", "Pinus", true, 1963, "Session Laws 1963, c. 41", "Q12024", "Represents the collective pine species that powered North Carolina's historical naval stores and timber industries.") },
                { "north-dakota", ("American Elm", "Ulmus americana", true, 1947, "Session Laws 1947", "Q469382", "A common, gracefully arching tree that provided a beautiful canopy for many of North Dakota's early towns.") },
                { "ohio", ("Ohio Buckeye", "Aesculus glabra", true, 1953, "ORC Ann. 5.05", "Q1813339", "Bears a nut resembling the eye of a deer, cementing Ohio's historical and beloved identity as 'The Buckeye State'.") },
                { "oklahoma", ("Redbud", "Cercis canadensis", true, 1937, "Senate Joint Resolution 5", "Q2452407", "One of the first trees to bloom in early spring, lining Oklahoma's valleys and ravines with vivid magenta flowers.") },
                { "oregon", ("Douglas Fir", "Pseudotsuga menziesii", true, 1939, "Senate Concurrent Resolution 5", "Q156687", "The backbone of Oregon's massive timber industry and the dominant conifer of the Pacific Northwest.") },
                { "pennsylvania", ("Eastern Hemlock", "Tsuga canadensis", true, 1931, "Act 233", "Q1137143", "A massive, long-lived evergreen that perfectly shaded the early, dense forests of Penn's Woods.") },
                { "rhode-island", ("Red Maple", "Acer rubrum", true, 1964, "Public Laws 1964", "Q161364", "Voted by schoolchildren; one of the most common trees in the state, offering spectacular scarlet autumn leaves.") },
                { "south-carolina", ("Palmetto", "Sabal palmetto", true, 1939, "Joint Resolution 63", "Q1088471", "Symbolizes the spongy palmetto log fort on Sullivan's Island that heroically repelled British cannonballs in 1776.") },

                { "south-dakota", ("Black Hills Spruce", "Picea glauca", true, 1947, "Laws 1947", "Q128116", "A dense, cone-shaped evergreen strictly native to the sacred and heavily forested Black Hills region.") },
                { "tennessee", ("Tulip Poplar", "Liriodendron tulipifera", true, 1947, "Public Chapter 204", "Q158783", "Chosen because it grew extensively across the state and was highly valued by early pioneers for constructing cabins.") },
                { "texas", ("Pecan", "Carya illinoinensis", true, 1919, "Senate Bill 317", "Q333877", "A native, nut-bearing tree deeply woven into Texan history; Governor James Hogg famously asked for one at his grave.") },
                { "utah", ("Quaking Aspen", "Populus tremuloides", true, 2014, "Senate Bill 41", "Q469576", "Replaced the Colorado Blue Spruce; known for its massive interconnected root systems, like the Pando clone.") },
                { "vermont", ("Sugar Maple", "Acer saccharum", true, 1949, "Act 1", "Q214733", "The cornerstone of Vermont's world-famous maple syrup industry and a major draw for autumn foliage tourism.") },
                { "virginia", ("Flowering Dogwood", "Cornus florida", true, 1956, "Acts of Assembly 1956", "Q887221", "Virginia is uniquely the only state to honor the beautiful dogwood as both its official state flower and state tree.") },
                { "washington", ("Western Hemlock", "Tsuga heterophylla", true, 1947, "Laws 1947, Chapter 191", "Q1144409", "Proposed by a Portland newspaper specifically to tease Oregon, it became a beloved symbol of Washington's rainy forests.") },
                { "west-virginia", ("Sugar Maple", "Acer saccharum", true, 1949, "House Concurrent Resolution 12", "Q214733", "Voted by students and civic groups for its beautiful wood, shade, and historic utility in producing sugar.") },
                { "wisconsin", ("Sugar Maple", "Acer saccharum", true, 1949, "Laws 1949, Chapter 218", "Q214733", "Voted by schoolchildren; celebrates the tree's brilliant fall colors and its importance to the state's hardwood lumber industry.") },
                { "wyoming", ("Plains Cottonwood", "Populus deltoides", true, 1947, "Laws 1947", "Q149319", "The largest broadleaf tree in Wyoming, historically serving as a critical landmark and shelter along prairie trails.") }
            };

            var trees = new List<Symbol>();

            foreach (var state in states)
            {
                if (stateTreeData.TryGetValue(state.Slug, out var tree))
                {
                    trees.Add(new Symbol
                    {
                        StateId = state.Id,
                        Type = "tree",
                        Name = tree.Name,
                        ScientificName = tree.Scientific,
                        Slug = GenerateSlug(tree.Name),
                        Status = tree.IsOfficial ? "Official" : "Unofficial",
                        AdoptedYear = tree.Year > 0 ? tree.Year : null,
                        Designation = "State tree",


                        Legislation = tree.Legislation,
                        WikidataId = null,
                        Meaning = tree.Meaning,

                        ImageUrl = $"/images/trees/{GenerateSlug(tree.Name)}.webp",
                        YamlPath = $"Content/states/{state.Slug}/tree.yaml"
                    });
                }
            }

            context.Symbols.AddRange(trees);
            await context.SaveChangesAsync();
        }

        private static async Task SeedStateMammals(AppDbContext context, List<State> states)
        {

            var old = await context.Symbols.Where(s => s.Type == "mammal").ToListAsync();
            if (old.Count > 0)
            {
                context.Symbols.RemoveRange(old);
                await context.SaveChangesAsync();
            }

            var rows = new List<(string StateSlug, string CommonName, string ScientificName, string Designation, int? Year, string? Legislation, string WikidataId, string Meaning)>
{

    ("alabama", "Black bear", "Ursus americanus", "State mammal", 2006, "Act 2006-245", "Q122783", "Symbolizes the state's rich forested wildlife and ongoing conservation efforts."),
    ("alabama", "Racking Horse", "Equus caballus", "State horse", 1975, "Acts 1975, No. 1153", "Q10758650", "A breed celebrated for its smooth gait, uniquely developed on Southern plantations."),
    ("alabama", "West Indian manatee", "Trichechus manatus", "State marine mammal", 2009, "Act 2009-488", "Q40261", "Highlights the importance of protecting Alabama's coastal and river ecosystems."),


    ("alaska", "Moose", "Alces alces", "State land mammal", 1998, "Senate Bill 265", "Q35517", "Represents the strength, resilience, and iconic wilderness of the Last Frontier."),
    ("alaska", "Bowhead whale", "Balaena mysticetus", "State marine mammal", 1983, "AS §44.09.075", "Q174652", "Honors the vital cultural and historic role this whale plays for Alaska Native communities."),
    ("alaska", "Alaskan Malamute", "Canis lupus familiaris", "Official state dog", 2010, "House Bill 14", "Q26972265", "Recognizes the indispensable role of sled dogs in Alaskan exploration and transportation."),


    ("arizona", "Ringtail", "Bassariscus astutus", "State mammal", 1986, "A.R.S. §41-859", "Q632701", "Known as the 'miner's cat,' it reflects Arizona's pioneering and mining history."),


    ("arkansas", "White-tailed deer", "Odocoileus virginianus", "State mammal", 1993, "Act 892", "Q215887", "Celebrates a highly valued natural resource and the state's outdoor heritage."),
    ("arkansas", "Labrador Retriever", "Canis lupus familiaris", "State dog", 2025, "House Bill 1487", "Q26972265", "Recognized as the official state dog in 2025 to honor its popularity and versatile role as a family and working companion."),


    ("california", "Grizzly bear", "Ursus arctos californicus", "State animal", 1953, "Assembly Bill 1014", "Q2565286", "A powerful symbol of independence originating from the 1846 Bear Flag Revolt."),
    ("california", "Gray whale", "Eschrichtius robustus", "State marine mammal", 1975, "Assembly Bill 258", "Q179154", "Commemorates the majestic marine life migrating along the California coastline."),


    ("colorado", "Rocky Mountain bighorn sheep", "Ovis canadensis canadensis", "State animal", 1961, "Senate Bill 61-294", "Q20908572", "Embodiment of Colorado's rugged Rocky Mountain terrain and resilient wildlife."),
    ("colorado", "Shelter dogs and cats", "Canis lupus familiaris & Felis catus", "State pets", 2013, "Senate Bill 13-201", "", "Promotes animal welfare and encourages the adoption of rescue pets statewide."),



    ("connecticut", "Sperm whale", "Physeter macrocephalus", "State animal", 1975, "Public Act 75-165", "Q81214", "A nod to Connecticut's historic 19th-century whaling industry and maritime roots."),
    ("connecticut", "Siberian Husky", "Canis lupus familiaris", "State dog", 2024, "House Bill 5354", "Q26972265", "Adopted in 2024, honoring the breed's historical connection to the state and its role as the long-standing mascot of UConn."),



    ("delaware", "Gray fox", "Urocyon cinereoargenteus", "State wildlife animal", 2010, "House Bill 354", "Q215250", "Recognized as a uniquely adaptable native predator of the Delaware woodlands."),
    ("delaware", "Rescue dog", "", "State dog", 2023, "Senate Bill 37", "", "Adopted to raise awareness for animal rescue and shelter welfare."),


    ("florida", "Florida panther", "Puma concolor coryi", "State animal", 1982, "Chapter 82-44, Laws of Florida", "Q776670", "Chosen by students to highlight the urgent need to protect this endemic, endangered feline."),
    ("florida", "Manatee", "Trichechus manatus latirostris", "State marine mammal", 1975, "Chapter 75-16, Laws of Florida", "Q28823693", "A beloved, gentle marine giant representing Florida's unique aquatic ecosystems."),
    ("florida", "Dolphin", "Tursiops truncatus", "State saltwater mammal", 1975, "Chapter 75-16, Laws of Florida", "Q174199", "Symbolizes the vibrant marine life and coastal economy of the Sunshine State."),
    ("florida", "Florida cracker horse", "Equus caballus", "State horse", 2008, "Chapter 2008-151, Laws of Florida", "Q10758650", "Honors the state's rich cattle-ranching heritage and the early Florida 'Crackers'."),


    ("georgia", "White-tailed deer", "Odocoileus virginianus", "State mammal", 2015, "House Bill 70", "Q215887", "Represents the state's dedication to wildlife conservation and hunting traditions."),
    ("georgia", "Right whale", "Eubalaena glacialis", "State marine mammal", 1985, "House Resolution 118", "Q193683", "Chosen to protect the only known calving grounds for this endangered species off Georgia's coast."),
    ("georgia", "Adoptable dog", "", "State dog", 2016, "House Resolution 1033", "", "Designated to promote the adoption of pets from animal shelters and rescues."),


    ("hawaii", "Hawaiian monk seal", "Neomonachus schauinslandi", "State mammal", 2008, "Act 146 (2008)", "Q28173851", "An endangered native species emphasizing Hawaii's commitment to marine conservation."),
    ("hawaii", "Humpback whale", "Megaptera novaeangliae", "State marine mammal", 1979, "Act 246 (1979)", "Q132905", "Celebrates the whales that return to Hawaiian waters each winter to mate and give birth."),
    ("hawaii", "Hawaiian hoary bat", "Lasiurus cinereus semotus", "State land mammal", 2015, "Act 126 (2015)", "Q1830296", "The only endemic land mammal of Hawaii, holding special cultural significance."),


    ("idaho", "Appaloosa horse", "Equus caballus", "State horse", 1975, "1975 Idaho Sess. Laws ch. 134", "Q10758650", "Ties to the state's history, developed specifically by the Nez Perce tribe of Idaho."),


    ("illinois", "White-tailed deer", "Odocoileus virginianus", "State animal", 1982, "Public Act 82-0866", "Q215887", "Selected by a vote of Illinois schoolchildren to represent the state's natural beauty."),


    ("kansas", "American bison", "Bison bison", "State animal", 1955, "Kansas Statute Chapter 73, Article 14", "Q82728", "A historic icon of the Great Plains and the early frontier days of Kansas."),


    ("kentucky", "Gray squirrel", "Sciurus carolinensis", "State wild game animal", 1968, "1968 Ky. Acts ch. 219, sec. 1", "Q468500", "An abundant native species historically significant to early Kentucky pioneers."),
    ("kentucky", "Thoroughbred horse", "Equus caballus", "State horse", 1996, "1996 Ky. Acts ch. 361, sec. 1", "Q10758650", "A global symbol of Kentucky's world-famous equine and horse racing industry."),


    ("louisiana", "Black bear", "Ursus americanus", "State mammal", 1992, "Acts 1992, No. 1022", "Q122783", "Represents a conservation success story in the swamp and woodland habitats of Louisiana."),
    ("louisiana", "Catahoula leopard dog", "Canis lupus familiaris", "State dog", 1979, "Acts 1979, No. 239", "Q26972265", "The only dog breed native to Louisiana, historically used for herding wild hogs."),


    ("maine", "Moose", "Alces alces", "State animal", 1979, "PL 1979, c. 234", "Q35517", "A majestic symbol of Maine's deep northern woods and outdoor sporting heritage."),
    ("maine", "Maine Coon cat", "Felis catus", "State cat", 1985, "PL 1985, c. 737", "Q20980826", "One of the oldest natural breeds in North America, native to the state of Maine."),


    ("maryland", "Calico cat", "Felis catus", "State cat", 2001, "Chapter 100", "Q20980826", "Chosen because its orange, black, and white colors match the Calvert family coat of arms."),
    ("maryland", "Chesapeake Bay retriever", "Canis lupus familiaris", "State dog", 1964, "Chapter 68", "Q26972265", "A rugged breed developed specifically for hunting waterfowl in the Chesapeake Bay."),
    ("maryland", "Thoroughbred horse", "Equus caballus", "State horse", 2003, "Chapter 374", "Q10758650", "Reflects the state's rich history in horse breeding and the famous Preakness Stakes."),


    ("massachusetts", "Right whale", "Eubalaena glacialis", "State marine mammal", 1980, "Chapter 698", "Q193683", "Acknowledges the state's whaling past and its modern commitment to marine protection."),
    ("massachusetts", "Boston terrier", "Canis lupus familiaris", "State dog", 1979, "Chapter 731", "Q26972265", "The first purebred dog developed in America, originating in the state's capital."),
    ("massachusetts", "Morgan horse", "Equus caballus", "State horse", 1970, "Chapter 781", "Q10758650", "A versatile American breed closely tied to New England's agricultural history."),
    ("massachusetts", "Tabby cat", "Felis catus", "State cat", 1988, "Chapter 267", "Q20980826", "Designated by schoolchildren in honor of the popular, affectionate feline companions."),


    ("michigan", "White-tailed deer", "Odocoileus virginianus", "State game mammal", 1997, "Public Act 15", "Q215887", "Highlights the animal's massive economic and cultural importance to Michigan's hunters."),


    ("mississippi", "White-tailed deer", "Odocoileus virginianus", "State land mammal", 1974, "Chapter 551, Laws of Mississippi", "Q215887", "A highly respected species representing the state's commitment to wildlife management."),
    ("mississippi", "Bottlenose dolphin", "Tursiops truncatus", "State water mammal", 1974, "Chapter 551, Laws of Mississippi", "Q174199", "Symbolizes the ecological richness of the Mississippi Gulf Coast."),
    ("mississippi", "Red fox", "Vulpes vulpes", "State land mammal", 1997, "Chapter 411, Laws of Mississippi", "Q8332", "Recognized for its intelligence and prevalence in Mississippi's rural landscapes."),


    ("missouri", "Missouri mule", "Equus asinus caballus", "State animal", 1995, "House Bill 384", "Q1939106", "Crucial to the agricultural and pioneer history, known for pulling pioneer wagons west."),
    ("missouri", "Missouri fox trotter horse", "Equus caballus", "State horse", 2002, "House Bill 1810", "Q10758650", "A breed developed in the Ozarks, famous for its comfortable gait over rough terrain."),


    ("montana", "Grizzly bear", "Ursus arctos horribilis", "State animal", 1983, "Chapter 325", "Q171004", "A symbol of the wild, untamed nature of Montana's mountainous regions."),


    ("nebraska", "White-tailed deer", "Odocoileus virginianus", "State mammal", 1981, "Legislative Bill 58", "Q215887", "An abundant species reflecting Nebraska's prairieland and woodland edges."),


    ("nevada", "Desert bighorn sheep", "Ovis canadensis nelsoni", "State animal", 1973, "Chapter 187", "Q1107889", "Perfectly adapted to survive in Nevada's harsh, arid desert environments."),


    ("new-hampshire", "White-tailed deer", "Odocoileus virginianus", "State animal", 1983, "1983, 190:2", "Q215887", "A tribute to New Hampshire's rich forestry and traditional hunting culture."),
    ("new-hampshire", "Bobcat", "Lynx rufus", "State wildcat", 2015, "2015, 90:1", "Q131907", "Chosen by students to represent the state's rugged, resilient, and elusive wildlife."),
    ("new-hampshire", "Chinook", "Canis lupus familiaris", "State dog", 2009, "House Bill 157", "Q26972265", "Developed in New Hampshire in the early 20th century, this rare breed is a tribute to the state's sled-dog racing heritage."),


    ("new-jersey", "Horse", "Equus caballus", "State animal", 1977, "P.L. 1977, c. 132", "Q10758650", "Represents the state's significant equestrian heritage and widespread horse farming."),
    ("new-jersey", "Seeing Eye Dog", "Canis lupus familiaris", "State dog", 2020, "A-711", "Q26972265", "Honors the legacy of The Seeing Eye, the first guide dog school in the U.S., founded in Nashville and relocated to Morristown, NJ."),
  

    ("new-mexico", "Black bear", "Ursus americanus", "State animal", 1963, "Laws 1963, ch. 134", "Q122783", "The animal that inspired 'Smokey Bear,' tied deeply to New Mexico's forest conservation."),


    ("new-york", "Beaver", "Castor canadensis", "State animal", 1975, "Chapter 170", "Q81056", "The driving force of the early fur trade that helped establish New York's economy."),
    ("new-york", "Service dog", "Canis lupus familiaris", "State dog", 2015, "Chapter 423", "Q26972265", "Honors the dedication of dogs trained to assist New Yorkers with disabilities."),


    ("north-carolina", "Gray squirrel", "Sciurus carolinensis", "State mammal", 1969, "Session Laws 1969, c. 1209", "Q468500", "A common and beloved woodland resident familiar to all North Carolinians."),
    ("north-carolina", "Plott hound", "Canis lupus familiaris", "State dog", 1989, "Session Laws 1989, c. 383", "Q26972265", "The only recognized dog breed native to the state, originally bred for hunting bears."),
    ("north-carolina", "Colonial Spanish mustang", "Equus caballus", "State horse", 2010, "Session Laws 2010-20", "Q10758650", "Descendants of horses brought by Spanish explorers, roaming the Outer Banks for centuries."),
    ("north-carolina", "Virginia opossum", "Didelphis virginiana", "State marsupial", 2013, "Session Laws 2013-189", "Q147267", "North America's only native marsupial, celebrated for its unique biological traits."),


    ("north-dakota", "Nokota horse", "Equus caballus", "Honorary equine", 1993, "1993 Legislative Act", "Q10758650", "A resilient breed descended from wild horses that roamed the badlands of North Dakota."),


    ("ohio", "White-tailed deer", "Odocoileus virginianus", "State animal", 1988, "Act of 1988", "Q215887", "Acknowledges the historical significance of the animal to Native Americans and early settlers."),


    ("oklahoma", "American bison", "Bison bison", "State animal", 1972, "Chapter 258", "Q82728", "A symbol of the great plains and the state's deeply rooted Native American history."),
    ("oklahoma", "Raccoon", "Procyon lotor", "State furbearer", 1989, "Chapter 320", "Q121439", "Recognized for its historical importance in the state's early trapping and fur trade."),
    ("oklahoma", "White-tailed deer", "Odocoileus virginianus", "State game animal", 1990, "Chapter 246", "Q215887", "The most popular game animal in Oklahoma, vital to the state's hunting economy."),
    ("oklahoma", "Mexican free-tailed bat", "Tadarida brasiliensis", "State flying mammal", 2006, "Chapter 148", "Q913930", "Provides essential natural pest control for Oklahoma's agricultural crops."),


    ("oregon", "Beaver", "Castor canadensis", "State animal", 1969, "Chapter 647", "Q81056", "Oregon is the 'Beaver State,' recognizing the animal's central role in early settlement and trade."),


    ("pennsylvania", "White-tailed deer", "Odocoileus virginianus", "State animal", 1959, "Act 130", "Q215887", "A fundamental part of Pennsylvania's wildlife and a key species for outdoor enthusiasts."),
    ("pennsylvania", "Great Dane", "Canis lupus familiaris", "State dog", 1965, "Act 7", "Q26972265", "Chosen for its strength and historical use by early Pennsylvania settlers for hunting and protection."),


    ("rhode-island", "Harbor seal", "Phoca vitulina", "State marine mammal", 2016, "Chapter 129", "Q26913", "Reflects the state's deep connection to the ocean and narragansett bay marine life."),


    ("south-carolina", "White-tailed deer", "Odocoileus virginianus", "State animal", 1972, "Act 1253", "Q215887", "A ubiquitous species integral to the state's natural ecosystem and hunting heritage."),
    ("south-carolina", "Bottlenose dolphin", "Tursiops truncatus", "State marine mammal", 2009, "Act 58", "Q174199", "A highly intelligent marine mammal commonly seen along South Carolina's coastline."),
    ("south-carolina", "Northern right whale", "Eubalaena glacialis", "Migratory marine mammal", 2009, "Act 58", "Q193683", "Recognizes the coastal waters as a critical migratory route for this endangered species."),
    ("south-carolina", "Mule", "Equus asinus caballus", "Heritage work animal", 2010, "Act 240", "Q15879", "Honors the immense agricultural labor provided by mules in the state's history."),
    ("south-carolina", "Marsh tacky", "Equus caballus", "Heritage horse", 2010, "Act 240", "Q10758650", "A rare, sturdy breed of colonial Spanish horse uniquely adapted to the state's swamps."),
    ("south-carolina", "Boykin spaniel", "Canis lupus familiaris", "State dog", 1985, "Act 31", "Q26972265", "A versatile hunting dog specifically bred in South Carolina for retrieving wild turkeys and ducks."),


    ("south-dakota", "Coyote", "Canis latrans", "State animal", 1949, "Chapter 219", "Q41551", "A highly adaptable predator echoing the wild, expansive prairies of South Dakota."),


    ("tennessee", "Raccoon", "Procyon lotor", "State wild animal", 1971, "Public Chapter 55", "Q121439", "A nod to the pioneer era and the historical significance of the frontiersman's coonskin cap."),
    ("tennessee", "Tennessee walking horse", "Equus caballus", "State horse", 2000, "Public Chapter 874", "Q10758650", "A world-renowned breed developed in the state, famous for its unique running walk."),
    ("tennessee", "Shelter dogs and cats", "", "State pet", 2014, "Public Chapter 968", "", "Brings attention to animal rescue efforts and the value of adopting shelter pets."),


    ("texas", "Nine-banded armadillo", "Dasypus novemcinctus", "State small mammal", 1995, "HCR 78", "Q649549", "A resilient and unique creature reflecting the tough, adaptable spirit of Texas."),
    ("texas", "Texas longhorn", "Bos taurus", "State large mammal", 1995, "HCR 78", "Q19610691", "A living icon of the legendary Texas cattle drives and ranching legacy."),
    ("texas", "Mexican free-tailed bat", "Tadarida brasiliensis", "State flying mammal", 1995, "HCR 78", "Q913930", "Honors the massive bat colonies that provide crucial pest control for Texas agriculture."),
    ("texas", "Blue Lacy", "Canis lupus familiaris", "State dog", 2005, "HCR 108", "Q26972265", "The only dog breed originating in Texas, specifically developed for working ranches."),
    ("texas", "American Quarter Horse", "Equus caballus", "State horse", 2009, "HCR 181", "Q10758650", "Celebrates the breed's foundational role in the state's agricultural and rodeo history."),


    ("utah", "Rocky Mountain elk", "Cervus canadensis nelsoni", "State animal", 1971, "HJR 8", "Q742914", "A majestic symbol of Utah's wild mountain ranges and wildlife conservation success."),


    ("vermont", "Morgan horse", "Equus caballus", "State animal", 1961, "Act 162", "Q10758650", "One of the earliest American horse breeds, tracing its foundational sire back to Vermont."),
    ("vermont", "Randall Lineback", "Bos taurus", "Heritage breed", 2005, "Act 72", "Q19610691", "A rare, historic breed of cattle originally developed in Vermont for dairy and draft work."),


    ("virginia", "Virginia big-eared bat", "Corynorhinus townsendii virginianus", "State bat", 2005, "HB 2637", "Q16993443", "Highlights the state's efforts to protect this endangered, native cave-dwelling species."),
    ("virginia", "American foxhound", "Canis lupus familiaris", "State dog", 1966, "Chapter 373", "Q26972265", "A breed famously cultivated by George Washington, deeply tied to Virginian history."),


    ("washington", "Orca", "Orcinus orca", "State marine mammal", 2005, "House Bill 1995", "Q26843", "A cultural and ecological icon of the Pacific Northwest and Puget Sound."),
    ("washington", "Olympic marmot", "Marmota olympus", "Endemic mammal", 2009, "House Bill 1026", "Q1242811", "A highly social species found only in the alpine regions of Washington's Olympic Peninsula."),


    ("west-virginia", "Black bear", "Ursus americanus", "State animal", 1973, "House Joint Resolution 25", "Q122783", "Voted by students, teachers, and sportsmen to represent the state's dense, rugged forests."),


    ("wisconsin", "Badger", "Taxidea taxus", "State wildlife animal", 1957, "Chapter 326", "Q232129", "Honors the early lead miners who dug hillside tunnels, earning Wisconsin the 'Badger State' nickname."),
    ("wisconsin", "White-tailed deer", "Odocoileus virginianus", "State wildlife animal", 1957, "Chapter 326", "Q215887", "A vital part of the state's ecology and highly prized by Wisconsin's sporting community."),
    ("wisconsin", "Dairy cow", "Bos taurus", "State domestic animal", 1971, "Chapter 201", "Q19610691", "A tribute to 'America's Dairyland' and the massive economic impact of Wisconsin's dairy industry."),
    ("wisconsin", "American water spaniel", "Canis lupus familiaris", "State dog", 1985, "Chapter 90", "Q26972265", "A rare breed developed in Wisconsin specifically for hunting in the state's icy waters and marshes."),


    ("wyoming", "American bison", "Bison bison", "State mammal", 1985, "Chapter 166", "Q82728", "An iconic symbol of the western frontier, heavily featured on the state's flag and seal.")
};
            static string Slugify(string value)
            {
                if (string.IsNullOrWhiteSpace(value)) return "";

                var s = value.Trim().ToLowerInvariant()
                    .Replace("’", "")
                    .Replace("'", "")
                    .Replace(".", "")
                    .Replace(",", "")
                    .Replace("(", "")
                    .Replace(")", "");


                var chars = s.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
                s = new string(chars);

                while (s.Contains("--")) s = s.Replace("--", "-");
                return s.Trim('-');
            }

            static string EnsureUniqueSlug(int stateId, string slug, List<Symbol> pending)
            {
                var used = pending.Where(x => x.StateId == stateId).Select(x => x.Slug).ToHashSet();
                if (!used.Contains(slug)) return slug;

                var i = 2;
                while (used.Contains($"{slug}-{i}")) i++;
                return $"{slug}-{i}";
            }

            var stateBySlug = states.ToDictionary(s => s.Slug, s => s);

            var mammals = new List<Symbol>();

            foreach (var r in rows)
            {
                if (!stateBySlug.TryGetValue(r.StateSlug, out var state))
                    continue;

                var baseSlug = Slugify(r.CommonName);
                var slug = EnsureUniqueSlug(state.Id, baseSlug, mammals);

                mammals.Add(new Symbol
                {
                    StateId = state.Id,
                    Type = "mammal",
                    Name = r.CommonName,
                    Slug = slug,
                    ScientificName = r.ScientificName,
                    AdoptedYear = r.Year,
                    Designation = r.Designation,
                    Legislation = r.Legislation,
                    ImageUrl = $"/images/mammals/{state.Slug}/{slug}.jpg",
                    YamlPath = $"Content/states/{state.Slug}/mammals/{slug}.yml",
                    WikidataId = null,
                    Meaning = r.Meaning,
                });
            }

            context.Symbols.AddRange(mammals);
            await context.SaveChangesAsync();
        }

        private static async Task SeedStateColors(AppDbContext context, List<State> states)
        {
            var old = await context.Symbols.Where(s => s.Type == "color").ToListAsync();
            if (old.Count > 0)
            {
                context.Symbols.RemoveRange(old);
                await context.SaveChangesAsync();
            }

            var stateColorData = new Dictionary<string, (string Name, string Status, int Year, string Legislation, string WikidataId, string Meaning)>
{
    { "alabama", ("Red and White", "Traditional", 0, "Traditional / State Flag", "Q173", "Based on the crimson cross of St. Andrew on the Alabama state flag.") },
    { "alaska", ("Blue and Gold", "Traditional", 0, "Traditional / State Flag", "Q797", "Blue for the sky and forget-me-not flower, gold for the North Star and state's mineral wealth.") },
    { "arizona", ("Blue and Old Gold", "Official", 1915, "Laws 1915, Chapter 30", "Q816", "Matches the blue of the US flag and the old gold of the state's historical mining era.") },
    { "arkansas", ("Red, White, and Blue", "Traditional", 0, "Traditional / State Flag", "Q1612", "Derived from the state flag, honoring its history and the United States.") },
    { "california", ("Blue and Gold", "Official", 1951, "Gov. Code § 424", "Q99", "Blue represents the sky and sea; Gold represents the precious metal of the 49ers and the California poppy.") },
    { "colorado", ("Blue, White, Gold, and Red", "Traditional", 0, "Traditional / State Flag", "Q1261", "Represents the blue sky, white snow, gold sunshine, and red soil of the Rocky Mountains.") },
    { "connecticut", ("Primary Blue", "Traditional", 0, "Traditional / State Flag", "Q779", "The dominant background color of the state flag and the historical colonial seal.") },
    { "delaware", ("Colonial Blue and Buff", "Official", 1913, "Title 29, Chapter 3", "Q1735", "Matches the uniform worn by General George Washington and his Continental Army.") },
    { "florida", ("Orange, Red, and White", "Associated", 0, "Cultural Association", "Q812", "Heavily associated with the state's citrus industry and the saltire on the state flag.") },
    { "georgia", ("Red, White, and Blue", "Traditional", 0, "Traditional / State Flag", "Q1428", "Based on the state flag, which shares colors with the United States flag.") },

    { "hawaii", ("Eight Island Colors", "Traditional", 0, "Traditional / Cultural", "Q782", "Each major island has an individual color (e.g., Maui is Pink); collectively they represent the state.") },
    { "idaho", ("Green, Gold, and Red", "Traditional", 0, "Traditional / State Seal", "Q1221", "Dominant colors found within the Idaho state seal and its agricultural landscape.") },
    { "illinois", ("Blue and Orange", "Associated", 0, "University of Illinois", "Q1204", "Widely recognized state colors popularized by the state's flagship university.") },
    { "indiana", ("Blue and Gold", "Traditional", 0, "Traditional / State Flag", "Q1415", "Derived from the dark blue field and gold stars of the state flag.") },
    { "iowa", ("Red, White, and Blue", "Traditional", 0, "Traditional / State Flag", "Q1546", "Mirrors the French tricolor, reflecting Iowa's history as part of the Louisiana Purchase.") },
    { "kansas", ("Blue and Gold", "Traditional", 0, "Traditional / State Flag", "Q1558", "The prominent colors of the state flag and the state flower, the wild native sunflower.") },
    { "kentucky", ("Blue and Gold", "Traditional", 0, "Traditional / State Flag", "Q1603", "Features the navy blue of the state flag and the gold of goldenrod, the state flower.") },
    { "louisiana", ("Blue, White, and Gold", "Official", 1972, "Act 603", "Q1588", "Reflects the historic colors of the state flag featuring the pelican emblem.") },
    { "maine", ("Blue and Green", "Traditional", 0, "Traditional / Natural", "Q724", "Represents the deep blue of the Atlantic ocean and the dark green of the vast pine forests.") },
    { "maryland", ("Red, White, Black, and Gold", "Official", 2004, "State Gov't Code § 13-302", "Q1391", "Officially designated in 2004; colors of the Calvert and Crossland coats of arms.") },

    { "massachusetts", ("Blue, Green, and Cranberry", "Official", 2005, "Chapter 13, Section 53", "Q771", "Blue for the ocean, green for the forests, and cranberry for the state's native berry.") },
    { "michigan", ("Maize and Blue", "Associated", 0, "University of Michigan", "Q1166", "Deeply ingrained in state culture via the University; widely accepted as de facto colors.") },
    { "minnesota", ("Cyan and Dark Blue", "Traditional", 2024, "2024 State Flag Adoption", "Q1527", "Colors of the 2024 flag representing 'The Land of 10,000 Lakes' and the North Star.") },
    { "mississippi", ("Red, Blue, and Gold", "Traditional", 2021, "2021 State Flag Adoption", "Q1494", "The prominent colors of the 'New Magnolia' flag adopted by voters in 2021.") },
    { "missouri", ("Red, White, and Blue", "Traditional", 0, "Traditional / State Flag", "Q1581", "Reflects the state's French heritage and its enduring American patriotism.") },
    { "montana", ("Copper, Silver, and Gold", "Traditional", 0, "Traditional / Mining", "Q1212", "Represents the state's nickname 'The Treasure State' and its rich mining history.") },
    { "nebraska", ("Scarlet and Cream", "Associated", 0, "University of Nebraska", "Q1553", "Highly popular colors of the state's flagship university 'Huskers' celebrated statewide.") },
    { "nevada", ("Silver and Blue", "Official", 1983, "NRS 235.025", "Q1227", "Silver for the state's mining industry and blue for the waters of Lake Tahoe.") },
    { "new-hampshire", ("Green and White", "Traditional", 0, "Traditional / Natural", "Q759", "Represents the majestic White Mountains and the state's dense forests.") },
    { "new-jersey", ("Jersey Blue and Buff", "Official", 1965, "Title 52:9A-1", "Q1408", "Chosen by George Washington in 1779 for the uniforms of the NJ Continental Line.") },

    { "new-mexico", ("Red and Yellow", "Official", 1999, "NMSA § 12-3-14", "Q1522", "Colors of Old Spain, featured on the Zia sun symbol flag adopted in 1925.") },
    { "new-york", ("Blue and Gold", "Traditional", 2015, "NYS Brand Guidelines", "Q1384", "Formalized in 2015 branding; colors derived from the historical 1889 state flag.") },
    { "north-carolina", ("Red and Blue", "Official", 1945, "G.S. § 145-3", "Q1454", "Specifically Old Glory Blue and Old Glory Red, matching the United States flag.") },
    { "north-dakota", ("Green and Yellow", "Associated", 0, "North Dakota State University", "Q1207", "Agricultural colors associated with the state's wheat and sunflower heritage.") },
    { "ohio", ("Scarlet and Gray", "Associated", 0, "Ohio State University", "Q1397", "Dominant cultural colors; state brand guide (2023) uses Buckeye Blue and Cardinal Red.") },
    { "oklahoma", ("Green and White", "Official", 1915, "25 O.S. § 93", "Q1649", "Selected to represent mistletoe, the state's original floral emblem.") },
    { "oregon", ("Navy Blue and Gold", "Official", 1959, "ORS § 186.010", "Q824", "Codified in state law; used on the state's unique double-sided flag.") },
    { "pennsylvania", ("Blue and Gold", "Traditional", 0, "Traditional / State Flag", "Q1400", "Historical colors derived from the 1799 state flag design.") },
    { "rhode-island", ("Blue, White, and Gold", "Traditional", 0, "Traditional / State Flag", "Q1387", "Colors of the 1897 flag: gold anchor and stars on a white field.") },
    { "south-carolina", ("Indigo Blue", "Official", 2008, "SC Code § 1-1-703", "Q1456", "Only Indigo Blue is official; confirmed via analysis of Revolutionary War artifacts.") },

    { "south-dakota", ("Blue and Gold", "Official", 1909, "SDCL § 1-6-16", "Q1211", "Official colors of the state seal and the original state flag design.") },
    { "tennessee", ("Orange and White", "Associated", 0, "University of Tennessee", "Q1509", "Extremely popular throughout the state due to the 'Volunteers' sports traditions.") },
    { "texas", ("Blue, White, and Red", "Official", 1933, "Gov. Code § 3101.002", "Q164", "Exactly matches the Lone Star Flag: loyalty (blue), purity (white), and bravery (red).") },
    { "utah", ("Black and Gold", "Traditional", 2024, "New State Flag Adoption", "Q829", "Featured on the new 2024 flag; representing the beehive and golden history.") },
    { "vermont", ("Green and Gold", "Traditional", 0, "Traditional / Natural", "Q16551", "Represents the Green Mountains and the state's agricultural roots.") },
    { "virginia", ("Red, White, and Blue", "Traditional", 0, "Traditional / History", "Q1370", "Linked to colonial history, the national colors, and the state flag.") },
    { "washington", ("Green and Gold", "Traditional", 0, "Traditional / State Flag", "Q1223", "Reflects the Evergreen State's forests and the gold of the state seal.") },
    { "west-virginia", ("Old Gold and Blue", "Official", 1963, "W. Va. SCR 18", "Q1371", "Officially adopted in 1963; originally popularized by the state university in 1895.") },
    { "wisconsin", ("Red and White", "Associated", 0, "University of Wisconsin", "Q1537", "Traditional colors of the UW-Madison Badgers, embraced statewide.") },
    { "wyoming", ("Brown and Gold", "Associated", 0, "University of Wyoming", "Q1214", "Distinct colors of the state's only 4-year university, recognized across the state.") }
};

            var colors = new List<Symbol>();

            foreach (var state in states)
            {
                if (stateColorData.TryGetValue(state.Slug, out var color))
                {
                    colors.Add(new Symbol
                    {
                        StateId = state.Id,
                        Type = "color",
                        Name = color.Name,
                        ScientificName = null, 
                        Slug = GenerateSlug(color.Name),
                        Status = color.Status,
                        AdoptedYear = color.Year > 0 ? color.Year : null,
                        Designation = "State colors",

                        Legislation = color.Legislation,
                        WikidataId = null,
                        Meaning = color.Meaning,

                        ImageUrl = $"/images/colors/{state.Slug}.svg",
                        YamlPath = $"Content/states/{state.Slug}/color.yaml"
                    });
                }
            }

            context.Symbols.AddRange(colors);
            await context.SaveChangesAsync();
        }

        private static async Task SeedStateFirearms(AppDbContext context, List<State> states)
        {
            var old = await context.Symbols.Where(s => s.Type == "firearm").ToListAsync();
            if (old.Count > 0)
            {
                context.Symbols.RemoveRange(old);
                await context.SaveChangesAsync();
            }

            var stateFirearmData = new Dictionary<string, (string Name, string Status, int Year, string Legislation, string WikidataId, string Meaning)>
        {
            { "alaska", ("Pre-1964 Winchester Model 70", "Official", 2014, "Senate Bill 175", "Q2814565", "Recognized as the 'Rifleman's Rifle', heavily used by Alaskans for survival and hunting in extreme conditions.") },
            { "arizona", ("Colt Single Action Army Revolver", "Official", 2011, "Senate Bill 1610", "Q1112183", "The iconic 'Peacemaker' that played a significant role in the history of the American West and Arizona's early statehood.") },
            { "indiana", ("Grouseland Rifle", "Official", 2012, "Senate Enrolled Act 209", "", "Crafted by John Small in Vincennes, it represents Indiana's frontier history and William Henry Harrison's era.") },
            { "kentucky", ("Kentucky Long Rifle", "Official", 2013, "House Bill 239", "", "The primary firearm of the American frontier, vital for hunting and defense during the early settlement of Kentucky.") },
            { "missouri", ("Hawken Rifle", "Official", 2023, "Senate Bill 139", "", "A heavy muzzleloading rifle famously used by fur trappers and explorers on the Santa Fe and Oregon trails originating in Missouri.") },
            { "pennsylvania", ("Pennsylvania Long Rifle", "Official", 2014, "House Bill 1989", "", "The original American frontier rifle, developed by German immigrant gunsmiths in Pennsylvania in the 1700s.") },
            { "tennessee", ("Barrett M82 / M107", "Official", 2016, "HJR 0231", "", "A .50 caliber semi-automatic sniper rifle invented by Tennessee native Ronnie Barrett and produced entirely within the state.") },
            { "texas", ("1847 Colt Walker Pistol", "Official", 2021, "SCR 8", "", "Co-invented by Texas Ranger Captain Samuel Hamilton Walker and Samuel Colt, instrumental in the survival of the early Texas Rangers.") },
            { "utah", ("Browning M1911 Pistol", "Official", 2011, "HB 219", "", "Designed by Utah native John M. Browning, serving as the standard U.S. military sidearm for over 70 years.") },
            { "west-virginia", ("Hall Model 1819 Flintlock Rifle", "Official", 2013, "SCR 7", "", "Manufactured at the Harpers Ferry Armory in present-day West Virginia, it was the first breech-loading rifle adopted by the U.S. military.") }
        };

            var firearms = new List<Symbol>();

            foreach (var state in states)
            {
                if (stateFirearmData.TryGetValue(state.Slug, out var firearm))
                {
                    firearms.Add(new Symbol
                    {
                        StateId = state.Id,
                        Type = "firearm",
                        Name = firearm.Name,
                        ScientificName = null,
                        Slug = GenerateSlug(firearm.Name),
                        Status = firearm.Status,
                        AdoptedYear = firearm.Year > 0 ? firearm.Year : null,
                        Designation = "State firearm",

                        Legislation = firearm.Legislation,
                        WikidataId = null,
                        Meaning = firearm.Meaning,

                        ImageUrl = ResolveFirearmImage(GenerateSlug(firearm.Name)),
                        YamlPath = $"Content/states/{state.Slug}/firearm.yaml"
                    });
                }
            }

            context.Symbols.AddRange(firearms);
            await context.SaveChangesAsync();
        }

        private static async Task SeedStateDinosaurs(AppDbContext context, List<State> states)
        {
            var old = await context.Symbols.Where(s => s.Type == "dinosaur").ToListAsync();
            if (old.Count > 0)
            {
                context.Symbols.RemoveRange(old);
                await context.SaveChangesAsync();
            }

            var stateDinosaurData = new Dictionary<string, (string Name, string ScientificName, int Year, string Legislation, string WikidataId, string Meaning)>
            {
                { "alabama", ("Lophorhothon", "Lophorhothon atopus", 1984, "Act No. 84-264", "", "Recognized for Alabama's important dinosaur discoveries in the Black Belt region and its role in the state's prehistoric heritage.") },
                { "arizona", ("Sonorasaurus", "Sonorasaurus thompsoni", 1998, "House Bill 2464", "", "Chosen as a dinosaur discovered in Arizona's Sonoran Desert and a symbol of the state's Cretaceous fossil record.") },
                { "arkansas", ("Arkansaurus", "Arkansaurus fridayi", 2017, "Act 689", "", "Celebrates one of Arkansas's best-known dinosaur discoveries and the state's growing paleontological identity.") },
                { "california", ("Augustynolophus", "Augustynolophus morrisi", 2017, "Assembly Bill 1540", "", "Selected as a dinosaur known only from California fossils, reflecting the state's unique prehistoric past.") },
                { "colorado", ("Stegosaurus", "Stegosaurus armatus", 1982, "House Bill 1005", "Q207172", "The first official state dinosaur in the U.S., representing Colorado's world-famous Morrison Formation fossil beds.") },
                { "maryland", ("Astrodon", "Astrodon johnstoni", 1998, "Senate Bill 656", "Q4816907", "Honors the first dinosaur species described from North American material and Maryland's deep paleontological history.") },
                { "missouri", ("Hypsibema", "Hypsibema missouriensis", 2004, "House Bill 1547", "Q5952735", "Recognized as Missouri's own dinosaur species and a symbol of the state's fossil discoveries.") },
                { "montana", ("Maiasaura", "Maiasaura peeblesorum", 1985, "House Bill 382", "Q629447", "Represents Montana's internationally significant dinosaur nesting discoveries and evidence of dinosaur parental care.") },
                { "new-jersey", ("Hadrosaurus", "Hadrosaurus foulkii", 1991, "Assembly Concurrent Resolution No. 32", "Q131410", "Commemorates the first nearly complete dinosaur skeleton found in North America, discovered in Haddonfield, New Jersey.") },
                { "oklahoma", ("Acrocanthosaurus", "Acrocanthosaurus atokensis", 2006, "Senate Concurrent Resolution 17", "Q26914", "Chosen for one of Oklahoma's most famous dinosaur finds and the state's important Cretaceous fossil record.") },
                { "texas", ("Paluxysaurus", "Paluxysaurus jonesi", 2009, "House Concurrent Resolution 16", "Q1963557", "Represents the giant sauropod fossils and dinosaur trackways of the Paluxy River region in Texas.") },
                { "utah", ("Utahraptor", "Utahraptor ostrommaysorum", 2018, "House Bill 14", "Q270916", "Honors Utah's globally recognized dinosaur discoveries and one of the largest known dromaeosaurs.") }
            };

            var dinosaurs = new List<Symbol>();

            foreach (var state in states)
            {
                if (!stateDinosaurData.TryGetValue(state.Slug, out var dinosaur))
                    continue;

                var slug = GenerateSlug(dinosaur.Name);

                dinosaurs.Add(new Symbol
                {
                    StateId = state.Id,
                    Type = "dinosaur",
                    Name = dinosaur.Name,
                    ScientificName = dinosaur.ScientificName,
                    Slug = slug,
                    AdoptedYear = dinosaur.Year > 0 ? dinosaur.Year : null,
                    Designation = "State dinosaur",
                    Legislation = dinosaur.Legislation,
                    WikidataId = null,
                    Meaning = dinosaur.Meaning,
                    ImageUrl = ResolveDinosaurImage(GenerateSlug(dinosaur.Name)),
                    YamlPath = $"Content/states/{state.Slug}/dinosaur.yaml"
                });
            }

            context.Symbols.AddRange(dinosaurs);
            await context.SaveChangesAsync();
        }

        private static async Task SeedStateBeverages(AppDbContext context, List<State> states)
        {
            var old = await context.Symbols.Where(s => s.Type == "beverage").ToListAsync();
            if (old.Count > 0)
            {
                context.Symbols.RemoveRange(old);
                await context.SaveChangesAsync();
            }

            var stateBeverageData = new (string StateSlug, string Name, string Slug, int? Year, string Designation, string Legislation, string Meaning)[]
            {
                ("alabama", "Clyde May's Whiskey", "clyde-mays-whiskey", 2004, "State spirit", "Official Alabama state spirit designation", "Named for Clyde May, a Bullock County farmer jailed for illegal distilling whose apple-aged Christmas whiskey became Conecuh Ridge; the legislature overrode Governor Bob Riley's veto to make it official in 2004."),
                ("arizona", "Lemonade", "lemonade", 2019, "State drink", "Arizona state drink designation", "Designated in 2019 after a fourth-grade civics project — a young student petitioned the Arizona legislature directly, making lemonade one of the few state symbols that originated as a child's legislative campaign."),
                ("arkansas", "Milk", "milk", 1985, "State drink", "Arkansas state drink designation", "Designated in 1985 as part of the national dairy industry push to place milk on state symbol lists across the South; reflects Arkansas's active but lower-profile dairy sector."),
                ("delaware", "Milk", "milk", 1983, "State drink", "Delaware state drink designation", "One of the earlier Mid-Atlantic milk designations; reflects the dairy farming communities of Delaware's southern counties, where agriculture anchors a state otherwise dominated by corporate and financial industry."),
                ("delaware", "Orange Crush", "orange-crush", 2024, "State cocktail", "Delaware state cocktail designation", "Designated in 2024 citing its popularity along Rehoboth Beach and the Delmarva coast — the move prompted Maryland to claim it in 2025 on stronger grounds: the drink was invented in Ocean City, Maryland."),
                ("florida", "Orange Juice", "orange-juice", 1967, "State beverage", "Florida state beverage designation", "Designated in 1967 at the peak of Florida citrus dominance, when the state produced the majority of American orange juice; citrus greening disease has since devastated the industry, and the symbol now outlasts it."),
                ("hawaii", "ʻAwa", "awa", 2018, "State drink", "Hawaii state drink designation", "ʻAwa (kava), brewed from the root of Piper methysticum, has been central to Native Hawaiian ceremony for centuries — the 2018 designation made Hawaii the only state with an indigenous ceremonial drink as its official symbol."),
                ("indiana", "Water", "water", 2007, "State beverage", "Indiana state beverage designation", "Designated in 2007, making Indiana one of the few states to choose a non-commercial beverage; framed as a conservation and public health statement rather than an agricultural or cultural designation."),
                ("kentucky", "Milk", "milk", 2005, "State drink", "Kentucky state drink designation", "Notably, the state most associated with bourbon chose milk as its official drink in 2005 — reflecting the dairy farming communities of south-central Kentucky that coexist with, and largely get overlooked beside, the whiskey industry."),
                ("kentucky", "Ale-8-One", "ale-8-one", 2013, "State soft drink", "Kentucky Revised Statutes § 2.085", "The only soft drink invented in Kentucky still in production; bottled in Winchester since 1926, the name is a phonetic pun on 'a late one' — it entered a regional flavor contest last and won anyway."),
                ("louisiana", "Milk", "milk", 1983, "State drink", "Louisiana state drink designation", "Designated in 1983 during the national dairy advocacy push that put milk on most Southern state symbol lists within a few years; reflects Louisiana's active but low-profile dairy sector alongside its far more famous sugarcane and crawfish industries."),
                ("maine", "Moxie", "moxie", 2005, "State soft drink", "Maine state soft drink designation", "Created in the 1870s by Union, Maine native Dr. Augustin Thompson; the gentian root bitterness defines it, the annual Lisbon Falls festival keeps it alive, and the brand name became a common English word for nerve and resilience."),
                ("maryland", "Milk", "milk", 1998, "State beverage", "Maryland state beverage designation", "Reflects the dairy farming belt of Frederick, Carroll, and Washington counties — the western Piedmont interior that Maryland's Chesapeake coastal reputation tends to overshadow."),
                ("maryland", "Rye Whiskey", "rye-whiskey", 2023, "State spirit", "Maryland state spirit designation", "Maryland rye has roots to the 1700s; Prohibition erased the industry in 1920, and Governor Wes Moore's 2023 designation formally reclaimed a tradition rebuilt by craft distillers over the prior decade."),
                ("maryland", "Orange Crush", "orange-crush", 2025, "State cocktail", "Maryland state cocktail designation", "Invented at Harborside Bar & Grill in Ocean City, Maryland in the mid-1990s; designated state cocktail in 2025 after Delaware claimed it first in 2024 — Maryland's origin argument was the stronger case."),
                ("massachusetts", "Cranberry Juice", "cranberry-juice", 1970, "State beverage", "Massachusetts state beverage designation", "Chosen to reflect the Commonwealth's cranberry bogs and the importance of cranberries to Massachusetts agriculture."),
                ("minnesota", "Milk", "milk", 1984, "State drink", "Minnesota state drink designation", "Recognized as a symbol of Minnesota's dairy sector and rural food economy."),
                ("mississippi", "Milk", "milk", 1984, "State beverage", "Mississippi state beverage designation", "Chosen to reflect agriculture, nutrition, and the role of dairy in Mississippi communities."),
                ("nebraska", "Milk", "milk", 1998, "State beverage", "Nebraska state beverage designation", "Recognized as a traditional farm product linked to Nebraska agriculture and food production."),
                ("nebraska", "Kool-Aid", "kool-aid", 1998, "State soft drink", "Nebraska state soft drink designation", "Chosen because Kool-Aid was invented in Nebraska and remains one of the state's most famous homegrown products."),
                ("nevada", "Picon Punch", "picon-punch", 2025, "State cocktail", "Nevada state cocktail designation", "Recognized for its deep ties to Nevada's Basque communities and old-West bar culture."),
                ("new-hampshire", "Apple Cider", "apple-cider", 2010, "State beverage", "New Hampshire state beverage designation", "Chosen to reflect New Hampshire orchards, fall culture, and the long history of apple growing in the state."),
                ("new-jersey", "Cranberry Juice", "cranberry-juice", 2023, "State juice", "New Jersey state juice designation", "Recognized for New Jersey's cranberry industry and the importance of the Pine Barrens growing region."),
                ("new-york", "Milk", "milk", 1981, "State beverage", "New York state beverage designation", "Chosen to reflect New York's major dairy industry and the central role of milk in the state's agriculture."),
                ("north-carolina", "Milk", "milk", 1987, "State beverage", "North Carolina state beverage designation", "Recognized as a symbol of North Carolina agriculture and statewide dairy production."),
                ("north-dakota", "Milk", "milk", 1983, "State beverage", "North Dakota state beverage designation", "Chosen to reflect North Dakota dairying and the importance of milk as a basic farm product."),
                ("ohio", "Tomato Juice", "tomato-juice", 1965, "State beverage", "Ohio state beverage designation", "Selected as the first-known state beverage in the country, tied to Ohio tomato growing and processing."),
                ("oklahoma", "Milk", "milk", 2002, "State beverage", "Oklahoma state beverage designation", "Recognized to represent Oklahoma dairy farming and agricultural heritage."),
                ("oregon", "Milk", "milk", 1997, "State beverage", "Oregon state beverage designation", "Chosen to reflect Oregon's dairy sector and the role of milk in the state's farm economy."),
                ("pennsylvania", "Milk", "milk", 1982, "State beverage", "Pennsylvania state beverage designation", "Recognized as a symbol of Pennsylvania agriculture and the state's strong dairy tradition."),
                ("rhode-island", "Coffee Milk", "coffee-milk", 1993, "State drink", "Rhode Island state drink designation", "Chosen because coffee milk is a distinctive local specialty closely associated with Rhode Island food culture."),
                ("south-carolina", "Milk", "milk", 1984, "State beverage", "South Carolina state beverage designation", "Recognized to reflect dairy farming and the place of milk in South Carolina agriculture."),
                ("south-carolina", "South Carolina-grown Tea", "tea", 1995, "State hospitality beverage", "South Carolina state hospitality beverage designation", "Chosen to reflect the state's tea-growing tradition and its role in welcoming visitors."),
                ("south-dakota", "Milk", "milk", 1986, "State beverage", "South Dakota state beverage designation", "Recognized as a standard agricultural symbol tied to South Dakota dairy production."),
                ("tennessee", "Milk", "milk", 2009, "State beverage", "Tennessee state beverage designation", "Chosen to represent Tennessee agriculture, family farms, and everyday nutrition."),
                ("vermont", "Milk", "milk", 1983, "State beverage", "Vermont state beverage designation", "Recognized as a direct symbol of Vermont's dairy identity and its nationally known milk and cheese production."),
                ("virginia", "Milk", "milk", 1982, "State beverage", "Virginia state beverage designation", "Chosen to reflect Virginia agriculture and the role of dairy farming across the Commonwealth."),
                ("virginia", "George Washington's Rye Whiskey", "george-washingtons-rye-whiskey", 2017, "State spirit", "Virginia state spirit designation", "Recognized for Mount Vernon's whiskey distilling history and George Washington's connection to Virginia heritage."),
                ("washington", "Coffee", "coffee", 2011, "State beverage", "Washington state beverage designation", "Chosen to reflect Washington's coffee culture and the global prominence of Seattle-area coffee companies."),
                ("wisconsin", "Milk", "milk", 1987, "State beverage", "Wisconsin state beverage designation", "Recognized as the clearest drink symbol of Wisconsin's dairy industry and statewide identity."),
                ("wisconsin", "Brandy Old Fashioned", "brandy-old-fashioned", 2023, "State cocktail", "Wisconsin state cocktail designation", "Chosen because the brandy old fashioned is one of Wisconsin's best-known supper club and bar traditions.")
            };

            var beverages = new List<Symbol>();

            foreach (var entry in stateBeverageData)
            {
                var state = states.FirstOrDefault(s => s.Slug == entry.StateSlug);
                if (state == null)
                    continue;

                beverages.Add(new Symbol
                {
                    StateId = state.Id,
                    Type = "beverage",
                    Name = entry.Name,
                    Slug = entry.Slug,
                    ScientificName = null,
                    AdoptedYear = entry.Year,
                    Designation = entry.Designation,
                    Legislation = entry.Legislation,
                    Meaning = entry.Meaning,
                    ImageUrl = ResolveBeverageImage(state.Slug, entry.Slug),
                    YamlPath = $"Content/states/{state.Slug}/beverage/{entry.Slug}.yaml"
                });
            }

            context.Symbols.AddRange(beverages);
            await context.SaveChangesAsync();
        }

        private static async Task SeedStateLicensePlates(AppDbContext context, List<State> states)
        {
            var old = await context.Symbols.Where(s => s.Type == "license-plate").ToListAsync();
            if (old.Count > 0)
            {
                context.Symbols.RemoveRange(old);
                await context.SaveChangesAsync();
            }

            var data = new (string StateSlug, string Slogan, string Slug, int? Year, string Meaning)[]
            {
                ("alabama", "Heart of Dixie", "heart-of-dixie", 1955, "One of the South's oldest regional identity phrases; 'Dixie' refers to the Southern United States and reflects Alabama's place at the heart of the Deep South."),
                ("alaska", "The Last Frontier", "the-last-frontier", 1959, "Adopted at statehood; reflects Alaska's vast wilderness, remote landscapes, and its status as the final major territorial expansion of the United States."),
                ("arizona", "Grand Canyon State", "grand-canyon-state", 1940, "The Grand Canyon, one of the world's most visited natural landmarks, defines Arizona's public identity more than any other single feature."),
                ("arkansas", "The Natural State", "the-natural-state", 1975, "Replaced 'Land of Opportunity' to emphasize Arkansas's forests, rivers, and outdoor recreation rather than economic development."),
                ("california", "The Golden State", "the-golden-state", 1968, "Refers simultaneously to the Gold Rush of 1848, the golden poppy wildflower, the golden hills of summer, and the state's year-round sunshine."),
                ("colorado", "Colorful Colorado", "colorful-colorado", 1950, "The state name comes from the Spanish word for color; the slogan plays on that etymology while evoking Colorado's striking mountain, canyon, and plateau landscapes."),
                ("connecticut", "The Constitution State", "the-constitution-state", 1959, "Connecticut's 1638 Fundamental Orders is considered the world's first written constitution adopted by a representative assembly."),
                ("delaware", "The First State", "the-first-state", 1974, "Delaware was the first state to ratify the U.S. Constitution on December 7, 1787, and has maintained that distinction on its plates ever since."),
                ("florida", "The Sunshine State", "the-sunshine-state", 1949, "One of the most recognized state slogans in the country; Florida averages more than 230 sunny days per year and built its entire tourism economy around that climate identity."),
                ("georgia", "Peach State", "peach-state", 1957, "Georgia's association with peaches is so deeply embedded that the fruit appears on the state quarter even though South Carolina now produces more peaches annually."),
                ("hawaii", "Aloha State", "aloha-state", 1959, "Adopted at statehood; 'aloha' carries multiple meanings in Hawaiian — love, peace, compassion, and a greeting — making the phrase do exceptional cultural work in two words."),
                ("idaho", "Famous Potatoes", "famous-potatoes", 1928, "One of the oldest plate slogans still in active use; Idaho chose agricultural identity over scenery and has kept the phrase for nearly a century."),
                ("illinois", "Land of Lincoln", "land-of-lincoln", 1954, "Abraham Lincoln lived in Illinois from 1830 until his election as president in 1860; Springfield remains the center of Lincoln heritage tourism."),
                ("indiana", "Crossroads of America", "crossroads-of-america", 1937, "Indiana sits at the intersection of major national highways and is one of the few states whose plate slogan is also its official state motto."),
                ("iowa", "The Hawkeye State", "the-hawkeye-state", 1954, "Named after Black Hawk, a Sauk leader whose resistance during the Black Hawk War became part of the region's founding mythology before Iowa became a state."),
                ("kansas", "The Sunflower State", "the-sunflower-state", 1953, "Kansas is one of the country's top sunflower producers; the sunflower is also the state flower, making the plate slogan and the floral emblem reinforce each other."),
                ("kentucky", "The Bluegrass State", "the-bluegrass-state", 1954, "Named for Poa pratensis, a grass that grows blue-green in Kentucky's limestone-rich soil and gave rise to the state's legendary thoroughbred horse breeding industry."),
                ("louisiana", "Sportsman's Paradise", "sportsmans-paradise", 1958, "Louisiana's coastal marshes, rivers, and bayous support some of the richest hunting and fishing in North America; the slogan accurately describes a genuine outdoor recreation identity."),
                ("maine", "Vacationland", "vacationland", 1936, "One of the oldest surviving plate phrases in the country; Maine's coast, lakes, and mountains have drawn summer visitors since the railroad era of the 19th century."),
                ("maryland", "The Old Line State", "the-old-line-state", 1974, "Honors the Maryland Line, Continental Army soldiers who fought with distinction at the Battle of Brooklyn in 1776 and earned the state its reputation for military courage."),
                ("massachusetts", "The Spirit of America", "the-spirit-of-america", 1971, "Adopted during the lead-up to the bicentennial; Massachusetts is home to more Revolutionary-era landmarks than any other state, including Lexington, Concord, and Boston."),
                ("michigan", "Pure Michigan", "pure-michigan", 2013, "Pure Michigan is the current slogan-bearing standard plate option; earlier Michigan plate slogans included Water Wonderland, Water-Winter Wonderland, Great Lake State, and Great Lakes."),
                ("minnesota", "Land of 10,000 Lakes", "land-of-10000-lakes", 1950, "There are actually more than 11,800 lakes in Minnesota; the round figure has undersold the state since the phrase was coined."),
                ("mississippi", "No current standard slogan", "no-current-standard-slogan", 2024, "The current 2024-2029 magnolia standard plate carries no text slogan; earlier Mississippi slogans included The Hospitality State in 1977 and Birthplace of American Music in 2012."),
                ("missouri", "Bicentennial", "bicentennial", 2018, "Missouri's current standard plate is the Bicentennial design introduced on October 15, 2018, ahead of the state's 200th anniversary of statehood in 2021."),
                ("montana", "Big Sky Country", "big-sky-country", 1967, "The phrase comes from the title of A.B. Guthrie Jr.'s 1947 novel set in Montana; the state adopted it because it captures the experience of the high plains and mountain landscapes more precisely than any geographic description."),
                ("nebraska", "The Good Life", "the-good-life", 1967, "Replaced 'The Beef State' as Nebraska sought to promote quality of life and livability rather than relying solely on agricultural identity."),
                ("nevada", "Battle Born", "battle-born", 1983, "Nevada was admitted to the Union in October 1864, during the Civil War — the only state admitted in wartime specifically to support the Union cause."),
                ("new-hampshire", "Live Free or Die", "live-free-or-die", 1971, "From General John Stark's 1809 toast: 'Live free or die: Death is not the worst of evils.' A 1977 Supreme Court case established that residents cannot be compelled to display it."),
                ("new-jersey", "Garden State", "garden-state", 1954, "The phrase predates the plate — it appears in an 1876 speech by Abraham Browning of Camden, who called New Jersey 'the Garden State' for its role feeding the cities of New York and Philadelphia."),
                ("new-mexico", "Land of Enchantment", "land-of-enchantment", 1941, "Coined by early 20th-century travel writers; the phrase captures the convergence of desert landscape, Indigenous cultures, and Spanish colonial history that distinguishes New Mexico from other Western states."),
                ("new-york", "Empire State", "empire-state", 1951, "The phrase is attributed to George Washington and has been part of New York's identity since the early republic; it appears on the state's official buildings, sports venues, and most recognizably the Empire State Building."),
                ("north-carolina", "First in Flight", "first-in-flight", 1982, "The Wright Brothers made the world's first powered airplane flight at Kitty Hawk, North Carolina on December 17, 1903; Ohio's competing 'Birthplace of Aviation' claim refers to where the brothers were born and built their plane, not where it flew."),
                ("north-dakota", "Peace Garden State", "peace-garden-state", 1956, "Named for the International Peace Garden on the North Dakota–Manitoba border, established in 1932 as a living monument to the friendship between the United States and Canada."),
                ("ohio", "The Heart of It All", "the-heart-of-it-all", 1984, "A marketing phrase that works on two levels: Ohio's central geographic position in the Midwest and its role as a presidential election bellwether state."),
                ("oklahoma", "Imagine That", "imagine-that", 2024, "Imagine That is the current standard plate slogan on the Iconic Oklahoma plate introduced September 1, 2024; Native America was Oklahoma's major historical plate slogan from 1994 through 2016."),
                ("oregon", "Pacific Wonderland", "pacific-wonderland", 1959, "Pacific Wonderland is Oregon's classic centennial-era plate slogan, created for the 1959 statehood centennial and later reissued as a special plate."),
                ("pennsylvania", "Let Freedom Ring", "let-freedom-ring", 2025, "Let Freedom Ring is Pennsylvania's current Liberty Bell plate slogan, introduced in 2025 ahead of America's 250th anniversary in 2026."),
                ("rhode-island", "Ocean State", "ocean-state", 1972, "Ocean State has appeared on Rhode Island standard passenger plates since 1972 and continues on the current Ocean plate introduced in 2023."),
                ("south-carolina", "Smiling Faces Beautiful Places", "smiling-faces-beautiful-places", 1969, "An early hospitality-focused slogan that puts people alongside landscape; relatively unusual among plate phrases, which more commonly emphasize geography alone."),
                ("south-dakota", "Great Faces Great Places", "great-faces-great-places", 1992, "A deliberate play on Mount Rushmore — 'great faces' refers directly to the four presidential carvings in the Black Hills."),
                ("tennessee", "The Volunteer State", "the-volunteer-state", 1954, "Tennessee supplied an unusually large number of volunteer soldiers in the War of 1812 and Mexican–American War; the tradition of volunteering became central to Tennessee's self-image."),
                ("texas", "The Lone Star State", "the-lone-star-state", 1951, "The single star on the Texas flag represents Texas's period as an independent republic (1836–1845); the Lone Star identity predates U.S. statehood and remains the strongest national brand of any state."),
                ("utah", "Life Elevated", "life-elevated", 2006, "Replaced 'The Greatest Snow on Earth' to broaden Utah's identity beyond ski tourism; works on two levels — Utah's high elevation and the concept of an elevated quality of life."),
                ("vermont", "Green Mountain State", "green-mountain-state", 1937, "'Vermont' derives from the French vert mont, meaning green mountain — the plate slogan is essentially a translation of the state's own name."),
                ("virginia", "Virginia Is For Lovers", "virginia-is-for-lovers", 1969, "Created in 1969 by the Martin Agency for the Virginia State Travel Service; it survived internal debate over whether to target specific traveler types ('history lovers,' 'beach lovers') and became one of the most successful and enduring tourism taglines ever produced."),
                ("washington", "The Evergreen State", "the-evergreen-state", 1923, "One of the oldest plate slogans in continuous use; Washington's Douglas fir and western red cedar forests were already defining the state's image before the automobile era."),
                ("west-virginia", "Wild, Wonderful", "wild-wonderful", 1969, "Replaced 'The Mountain State' to emphasize outdoor recreation and natural scenery; the two-adjective format has proven more memorable than a straightforward geographic label."),
                ("wisconsin", "America's Dairyland", "americas-dairyland", 1940, "Wisconsin adopted the phrase when it was the country's leading milk producer; the slogan has remained through decades of changes in the national dairy industry."),
                ("wyoming", "The Equality State", "the-equality-state", 1966, "Wyoming became the first U.S. territory to grant women the right to vote in 1869 — 51 years before the 19th Amendment — and the first to seat women on juries and elect a female governor.")
            };

            var symbols = new List<Symbol>();

            foreach (var entry in data)
            {
                var state = states.FirstOrDefault(s => s.Slug == entry.StateSlug);
                if (state == null)
                    continue;

                symbols.Add(new Symbol
                {
                    StateId = state.Id,
                    Type = "license-plate",
                    Name = entry.Slogan,
                    Slug = entry.Slug,
                    ScientificName = null,
                    AdoptedYear = entry.Year,
                    Designation = "License plate slogan",
                    Legislation = null,
                    Meaning = entry.Meaning,
                    ImageUrl = null,
                    YamlPath = $"Content/states/{state.Slug}/license-plate.yaml"
                });
            }

            context.Symbols.AddRange(symbols);
            await context.SaveChangesAsync();
        }

        private static async Task SeedStateSeals(AppDbContext context, List<State> states)
        {
            var old = await context.Symbols.Where(s => s.Type == "state-seal").ToListAsync();
            if (old.Count > 0)
            {
                context.Symbols.RemoveRange(old);
                await context.SaveChangesAsync();
            }

            var stateSealData = new Dictionary<string, (string Name, int AdoptedYear, int? RevisedYear, string Legislation, string Meaning)>
            {
                { "alabama", ("Great Seal of Alabama", 1819, 1939, "Alabama Code § 1-2-1", "Features a central map of Alabama with major rivers and a shield displaying symbolic elements representing the state's history and resources.") },
                { "alaska", ("Great Seal of Alaska", 1910, 1960, "Alaska Statutes § 44.09.040", "Features the northern lights, mountains, forests, a train, and ships — representing the natural wealth and industries of the Last Frontier.") },
                { "arizona", ("Great Seal of Arizona", 1912, null, "A.R.S. § 41-851", "A sun rising over mountains, a copper star, a dam, cattle, and a miner — the industries and landscape that define Arizona.") },
                { "arkansas", ("Great Seal of Arkansas", 1820, 1907, "Arkansas Code Ann. § 1-4-101", "The Goddess of Liberty on top, a sword and shield at center, and an eagle at the bottom — representing sovereignty and justice.") },
                { "california", ("Great Seal of California", 1849, 1937, "California Government Code § 400", "Minerva — the Roman goddess of wisdom, born an adult — stands as the central figure because California was admitted without a territorial period. Includes a grizzly bear, gold miner, 31 stars, and the motto Eureka.") },
                { "colorado", ("Great Seal of Colorado", 1877, 1964, "Colorado Revised Statutes § 24-80-901", "The Eye of Providence surmounts a heraldic shield with mountains, a mining pick, and a fasces — representing the union of Roman republican ideals with Colorado's frontier identity.") },
                { "connecticut", ("Great Seal of Connecticut", 1784, null, "Connecticut General Statutes § 3-105", "Three grapevines on a white field descend from the 1647 colonial seal — making Connecticut's design one of the oldest continuously used state seal compositions in the country.") },
                { "delaware", ("Great Seal of Delaware", 1777, 1971, "Delaware Code Title 29 § 301", "A farmer and a soldier flank a shield bearing wheat, corn, and an ox — the agricultural foundation of the First State.") },
                { "florida", ("Great Seal of Florida", 1868, 1985, "Florida Statutes § 15.03", "A Seminole woman scattering flowers, a steamboat on the horizon, a Sabal palmetto, and a rising sun — updated in 1985 to more accurately depict the Seminole woman.") },
                { "georgia", ("Great Seal of Georgia", 1799, 1914, "Official Code of Georgia § 50-3-30", "An arch supported by three columns representing Wisdom, Justice, and Moderation — the three principles in the state motto 'Wisdom, Justice, Moderation.'") },
                { "hawaii", ("Great Seal of Hawaii", 1959, null, "Hawaii Revised Statutes § 5-7", "The rising sun, the Hawaiian state flag, and the Phoenix above the state motto — 'Ua Mau ke Ea o ka ʻĀina i ka Pono' (The life of the land is perpetuated in righteousness).") },
                { "idaho", ("Great Seal of Idaho", 1891, null, "Idaho Code § 59-1001", "A woman representing liberty and equality stands alongside a miner — Idaho was among the first states to grant women suffrage.") },
                { "illinois", ("Great Seal of Illinois", 1818, 1868, "5 Illinois Compiled Statutes 460/1", "A bald eagle holds a shield and banner with 'State Sovereignty, National Union' — the motto's word order was controversially reversed after the Civil War.") },
                { "indiana", ("Great Seal of Indiana", 1816, 1963, "Indiana Code § 1-2-3-1", "A woodsman fells a tree while a bison flees and the sun rises over the hills — the clearing of the frontier for settlement.") },
                { "iowa", ("Great Seal of Iowa", 1847, null, "Iowa Code § 1A.1", "A citizen soldier holding an American flag and a plow stands in a landscape of farming and industrial scenes — agriculture and the frontier.") },
                { "kansas", ("Great Seal of Kansas", 1861, 1992, "Kansas Statutes § 73-701", "A prairie sunrise, a river with wagons crossing, a farmer plowing, and bison being chased — the frontier landscape of the Great Plains.") },
                { "kentucky", ("Great Seal of Kentucky", 1792, 1962, "Kentucky Revised Statutes § 2.020", "Two figures — a frontiersman and a statesman — embrace under the motto 'United We Stand, Divided We Fall.'") },
                { "louisiana", ("Great Seal of Louisiana", 1812, 2006, "Louisiana Revised Statutes § 49:151", "A pelican in her piety — feeding three chicks from her own breast — an ancient Christian symbol of charity and self-sacrifice.") },
                { "maine", ("Great Seal of Maine", 1820, 1919, "Maine Revised Statutes Title 1 § 201", "A moose beneath a pine tree, flanked by a farmer and a sailor, with the North Star above and the motto 'Dirigo' (I Direct or I Lead).") },
                { "maryland", ("Great Seal of Maryland", 1648, 1969, "Maryland Code, State Government § 13-201", "The Calvert and Crossland family coats of arms — the oldest heraldic state seal design in the United States, rooted in English heraldry.") },
                { "massachusetts", ("Great Seal of Massachusetts", 1780, 2022, "Massachusetts General Laws Chapter 2 § 1", "An Algonquian Native American holding a bow — revised in 2022 to remove a sword held over the figure and update the motto from the Latin phrase to a new design.") },
                { "michigan", ("Great Seal of Michigan", 1835, null, "Michigan Compiled Laws § 2.21", "An elk and a moose support a shield with an eagle crest and the motto 'Si Quaeris Peninsulam Amoenam, Circumspice' (If you seek a pleasant peninsula, look about you).") },
                { "minnesota", ("Great Seal of Minnesota", 1858, 1983, "Minnesota Statutes § 1.135", "A farmer, a waterfall, and St. Anthony Falls — revised in 1983 to remove a Native American on horseback receding into the distance, deemed demeaning.") },
                { "mississippi", ("Great Seal of Mississippi", 1817, 2014, "Mississippi Code § 3-3-3", "An eagle with the motto 'Virtute et Armis' (By valor and arms) — one of the few state seals to use a purely Latin motto emphasizing martial valor.") },
                { "missouri", ("Great Seal of Missouri", 1822, null, "Missouri Revised Statutes § 10.020", "Two grizzly bears support a shield divided between the U.S. coat of arms and a crescent moon with a grizzly — Missouri's position between East and West.") },
                { "montana", ("Great Seal of Montana", 1865, 1985, "Montana Code § 1-1-501", "The Great Falls of the Missouri River with mining and plow tools — 'Oro y Plata' (Gold and Silver) — the mining identity of the Treasure State.") },
                { "nebraska", ("Great Seal of Nebraska", 1867, 1966, "Nebraska Revised Statutes § 90-101", "A blacksmith, a settler's cabin, a train, and a steamboat — the industries of the Great Plains frontier at the moment of statehood.") },
                { "nevada", ("Great Seal of Nevada", 1866, 1915, "Nevada Revised Statutes § 235.010", "A silver star on blue, with mountains, a mine, and a train — 'Battle Born' for admission during the Civil War.") },
                { "new-hampshire", ("Great Seal of New Hampshire", 1784, 1931, "New Hampshire Revised Statutes § 3:1", "The frigate USS Raleigh, built in Portsmouth in 1776 — one of the first warships of the Continental Navy — surrounded by a laurel wreath.") },
                { "new-jersey", ("Great Seal of New Jersey", 1777, null, "N.J.S.A. 52:1-1", "Two figures, Ceres and Liberty, flank a shield with three ploughs — representing New Jersey's agricultural character and abundance.") },
                { "new-mexico", ("Great Seal of New Mexico", 1912, null, "New Mexico Statutes § 12-3-1", "A small American eagle covering a larger Mexican eagle — visually representing the transfer of sovereignty over New Mexico from Mexico to the United States.") },
                { "new-york", ("Great Seal of New York", 1778, 1882, "New York State Law § 70", "Liberty and Justice flank a shield with the sun rising over the Hudson River — the foundational imagery of the state that calls itself the gateway to America.") },
                { "north-carolina", ("Great Seal of North Carolina", 1971, null, "North Carolina General Statutes § 144-1", "Liberty and Plenty stand together under the motto 'Esse Quam Videri' (To be rather than to seem) — adopted from Cicero.") },
                { "north-dakota", ("Great Seal of North Dakota", 1889, null, "North Dakota Century Code § 54-02-01", "A tree stump, a plow, and an Indian on horseback — frontier settlement and indigenous heritage on the Northern Plains.") },
                { "ohio", ("Great Seal of Ohio", 1803, 1967, "Ohio Revised Code § 5.10", "A rising sun over Mount Logan, the Scioto River, and a bundle of 17 arrows and 17 laurel leaves — Ohio was the 17th state admitted to the Union.") },
                { "oklahoma", ("Great Seal of Oklahoma", 1907, null, "Oklahoma Statutes § 80-1", "A central star representing Oklahoma; five smaller stars around it for the five major tribes of Indian Territory: Chickasaw, Choctaw, Cherokee, Creek, and Seminole.") },
                { "oregon", ("Great Seal of Oregon", 1859, 1903, "Oregon Revised Statutes § 186.010", "A covered wagon, a departing British ship, and an arriving American ship — the transfer of Pacific Northwest sovereignty from Britain to the United States.") },
                { "pennsylvania", ("Great Seal of Pennsylvania", 1778, 1809, "Pennsylvania Statutes Title 71 § 1801", "An eagle, a ship, and a plough on a shield — agriculture and commerce, flanked by corn and olive branches representing peace and prosperity.") },
                { "rhode-island", ("Great Seal of Rhode Island", 1647, 1875, "Rhode Island General Laws § 42-4-1", "A golden anchor surrounded by 13 stars for the original colonies, with the motto 'Hope' — Rhode Island's anchor and hope have appeared on its seal since the colonial era.") },
                { "south-carolina", ("Great Seal of South Carolina", 1777, null, "South Carolina Code § 1-1-630", "Two ovals: a palmetto tree over a fallen oak (the palmetto fort that defeated a British naval attack in 1776) and a woman with the motto 'Dum Spiro Spero' (While I breathe, I hope).") },
                { "south-dakota", ("Great Seal of South Dakota", 1889, 1961, "South Dakota Codified Laws § 1-6-1", "A steamboat, a smelting furnace, corn, and a farmer — the industries of the Dakota Territory at the moment of statehood.") },
                { "tennessee", ("Great Seal of Tennessee", 1796, 1987, "Tennessee Code § 4-1-301", "A plow, a sheaf of wheat, and a riverboat — the words 'Agriculture' and 'Commerce' on the two sides of the seal identify Tennessee's founding economic identity.") },
                { "texas", ("Great Seal of Texas", 1839, 1961, "Texas Government Code § 3101.001", "A lone star encircled by olive and live oak branches — the emblem of the Republic of Texas, carried directly into statehood in 1845.") },
                { "utah", ("Great Seal of Utah", 1896, 2011, "Utah Code § 67-1-3", "A beehive — Deseret, symbol of industry — flanked by Utah lilies and an American eagle, with the motto 'Industry.'") },
                { "vermont", ("Great Seal of Vermont", 1779, 1937, "Vermont Statutes Title 1 § 491", "A pine tree, a cow, and sheaves of wheat under the motto 'Freedom and Unity' — Vermont's agricultural and independent character.") },
                { "virginia", ("Great Seal of Virginia", 1776, 1930, "Code of Virginia § 7.1-26", "Virtus, the goddess of virtue, stands victorious over a fallen tyrant — embodying the state motto 'Sic Semper Tyrannis' (Thus always to tyrants).") },
                { "washington", ("Great Seal of Washington", 1889, 1967, "Revised Code of Washington § 1.20.100", "A portrait of George Washington — the only U.S. state seal to feature the face of a named historical person rather than an allegorical figure.") },
                { "west-virginia", ("Great Seal of West Virginia", 1863, null, "West Virginia Code § 2-2-1", "A farmer and a miner flank a rock with crossed rifles — the state was carved from Virginia during the Civil War; the rifles honor the Union soldiers who made that possible.") },
                { "wisconsin", ("Great Seal of Wisconsin", 1851, 1881, "Wisconsin Statutes § 14.47", "A sailor and a miner flank a shield with state industries; a badger crest sits above and a cornucopia and lead below — industry by both land and water.") },
                { "wyoming", ("Great Seal of Wyoming", 1893, null, "Wyoming Statutes § 8-3-101", "A central pillar with the motto 'Equal Rights,' flanked by a miner, a cowboy, and a woman — Wyoming was the first U.S. territory to grant women's suffrage, in 1869.") }
            };

            var seals = new List<Symbol>();

            foreach (var state in states)
            {
                if (stateSealData.TryGetValue(state.Slug, out var sealData))
                {
                    seals.Add(new Symbol
                    {
                        StateId = state.Id,
                        Type = "state-seal",
                        Name = sealData.Name,
                        Slug = GenerateSlug(sealData.Name),
                        ScientificName = null,
                        AdoptedYear = sealData.AdoptedYear,
                        Status = "Official",
                        Designation = "State seal",
                        Legislation = sealData.Legislation,
                        WikidataId = null,
                        Meaning = sealData.Meaning,
                        ImageUrl = $"/images/seals/{state.Slug}/seal.webp",
                        YamlPath = $"Content/states/{state.Slug}/state-seal.yaml"
                    });
                }
            }

            context.Symbols.AddRange(seals);
            await context.SaveChangesAsync();
        }

        private static async Task SeedStateCoatsOfArms(AppDbContext context, List<State> states)
        {
            var old = await context.Symbols.Where(s => s.Type == "coat-of-arms").ToListAsync();
            if (old.Count > 0)
            {
                context.Symbols.RemoveRange(old);
                await context.SaveChangesAsync();
            }

            var contentRoot = Path.Combine(Directory.GetCurrentDirectory(), "Content", "states");
            if (!Directory.Exists(contentRoot))
            {
                return;
            }

            var deserializer = new DeserializerBuilder().Build();
            var symbols = new List<Symbol>();

            foreach (var file in Directory.EnumerateFiles(contentRoot, "coat-of-arms.yaml", SearchOption.AllDirectories))
            {
                var stateSlug = new DirectoryInfo(Path.GetDirectoryName(file) ?? string.Empty).Name;
                if (string.IsNullOrWhiteSpace(stateSlug))
                {
                    continue;
                }

                var state = states.FirstOrDefault(s => string.Equals(s.Slug, stateSlug, StringComparison.OrdinalIgnoreCase));
                if (state == null)
                {
                    continue;
                }

                Dictionary<object, object>? data;
                try
                {
                    data = deserializer.Deserialize<Dictionary<object, object>>(File.ReadAllText(file));
                }
                catch
                {
                    continue;
                }

                if (data == null)
                {
                    continue;
                }

                var name = GetYamlString(data, "name");
                if (string.IsNullOrWhiteSpace(name))
                {
                    name = GetYamlString(data, "title");
                }

                if (string.IsNullOrWhiteSpace(name))
                {
                    name = $"Coat of Arms of {state.Name}";
                }

                var heroImage = GetYamlString(data, "hero_image");
                if (string.IsNullOrWhiteSpace(heroImage))
                {
                    heroImage = $"/images/coats-of-arms/{state.Slug}/coat-of-arms.webp";
                }

                symbols.Add(new Symbol
                {
                    StateId = state.Id,
                    Type = "coat-of-arms",
                    Name = name,
                    Slug = GenerateSlug(name),
                    ScientificName = null,
                    AdoptedYear = GetYamlInt(data, "adopted_year"),
                    Status = GetYamlBool(data, "is_official") ? "Official" : null,
                    Designation = "Coat of arms",
                    Legislation = GetYamlString(data, "legislation"),
                    WikidataId = null,
                    Meaning = GetYamlString(data, "meaning"),
                    ImageUrl = heroImage,
                    YamlPath = $"Content/states/{state.Slug}/coat-of-arms.yaml"
                });
            }

            if (symbols.Count == 0)
            {
                return;
            }

            context.Symbols.AddRange(symbols);
            await context.SaveChangesAsync();
        }

        private static async Task SeedStateSoils(AppDbContext context, List<State> states)
        {
            var old = await context.Symbols.Where(s => s.Type == "soil" || s.Type == "state-soil").ToListAsync();
            if (old.Count > 0)
            {
                context.Symbols.RemoveRange(old);
                await context.SaveChangesAsync();
            }

            var contentRoot = Path.Combine(Directory.GetCurrentDirectory(), "Content", "states");
            if (!Directory.Exists(contentRoot))
                return;

            var deserializer = new DeserializerBuilder().Build();
            var symbols = new List<Symbol>();

            foreach (var file in Directory.EnumerateFiles(contentRoot, "soil.yaml", SearchOption.AllDirectories))
            {
                var stateSlug = new DirectoryInfo(Path.GetDirectoryName(file) ?? string.Empty).Name;
                if (string.IsNullOrWhiteSpace(stateSlug))
                    continue;

                var state = states.FirstOrDefault(s => string.Equals(s.Slug, stateSlug, StringComparison.OrdinalIgnoreCase));
                if (state == null)
                    continue;

                Dictionary<object, object>? data;
                try { data = deserializer.Deserialize<Dictionary<object, object>>(File.ReadAllText(file)); }
                catch { continue; }

                if (data == null)
                    continue;

                var name = GetYamlString(data, "name");
                if (string.IsNullOrWhiteSpace(name))
                    name = GetYamlString(data, "title");
                if (string.IsNullOrWhiteSpace(name))
                    name = $"State Soil of {state.Name}";

                var heroImage = ResolveStateSoilImage(state.Slug, GetYamlString(data, "hero_image"));

                symbols.Add(new Symbol
                {
                    StateId = state.Id,
                    Type = "soil",
                    Name = name,
                    Slug = GenerateSlug(name),
                    ScientificName = null,
                    AdoptedYear = GetYamlInt(data, "adopted_year"),
                    Status = GetYamlBool(data, "is_official") ? "Official" : null,
                    Designation = "State soil",
                    Legislation = GetYamlString(data, "legislation"),
                    WikidataId = null,
                    Meaning = GetYamlString(data, "meaning"),
                    ImageUrl = heroImage,
                    YamlPath = $"Content/states/{state.Slug}/soil.yaml"
                });
            }

            if (symbols.Count == 0)
                return;

            context.Symbols.AddRange(symbols);
            await context.SaveChangesAsync();
        }

        private static async Task SeedStateInsects(AppDbContext context, List<State> states)
        {
            var old = await context.Symbols.Where(s => s.Type == "insect").ToListAsync();
            if (old.Count > 0)
            {
                context.Symbols.RemoveRange(old);
                await context.SaveChangesAsync();
            }

            var contentRoot = Path.Combine(Directory.GetCurrentDirectory(), "Content", "states");
            if (!Directory.Exists(contentRoot))
                return;

            var deserializer = new DeserializerBuilder().Build();
            var symbols = new List<Symbol>();

            // Wildcard match: states can have several insect-related designations
            // (state insect, state butterfly, state agricultural insect, state bug, etc.),
            // one YAML file per designation - e.g. insect.yaml, insect-butterfly.yaml, insect-agricultural.yaml.
            foreach (var file in Directory.EnumerateFiles(contentRoot, "insect*.yaml", SearchOption.AllDirectories))
            {
                var stateSlug = new DirectoryInfo(Path.GetDirectoryName(file) ?? string.Empty).Name;
                if (string.IsNullOrWhiteSpace(stateSlug))
                    continue;

                var state = states.FirstOrDefault(s => string.Equals(s.Slug, stateSlug, StringComparison.OrdinalIgnoreCase));
                if (state == null)
                    continue;

                Dictionary<object, object>? data;
                try { data = deserializer.Deserialize<Dictionary<object, object>>(File.ReadAllText(file)); }
                catch { continue; }

                if (data == null)
                    continue;

                var name = GetYamlString(data, "name");
                if (string.IsNullOrWhiteSpace(name))
                    name = GetYamlString(data, "title");
                if (string.IsNullOrWhiteSpace(name))
                    name = $"State Insect of {state.Name}";

                var designation = GetYamlString(data, "designation");
                if (string.IsNullOrWhiteSpace(designation))
                    designation = "State insect";

                symbols.Add(new Symbol
                {
                    StateId = state.Id,
                    Type = "insect",
                    Name = name,
                    Slug = GenerateSlug(name),
                    ScientificName = GetYamlString(data, "binomial_name"),
                    AdoptedYear = GetYamlInt(data, "adopted_year"),
                    Status = GetYamlBool(data, "is_official") ? "Official" : null,
                    Designation = designation,
                    Legislation = GetYamlString(data, "legislation"),
                    WikidataId = null,
                    Meaning = GetYamlString(data, "meaning"),
                    ImageUrl = GetYamlString(data, "hero_image"),
                    YamlPath = $"Content/states/{state.Slug}/{Path.GetFileName(file)}"
                });
            }

            if (symbols.Count == 0)
                return;

            context.Symbols.AddRange(symbols);
            await context.SaveChangesAsync();
        }

        // Shared loader for State Mineral / State Rock (or Stone) / State Gemstone, one
        // content file per designation (mineral.yaml / rock.yaml / gemstone.yaml), the same
        // reuse pattern SeedStateSoils uses: read straight from YAML, no hardcoded data array.
        private static async Task SeedStateGeologySymbols(
            AppDbContext context,
            List<State> states,
            string symbolType,
            string yamlFileName,
            string defaultDesignation,
            string categoryPlural)
        {
            var old = await context.Symbols.Where(s => s.Type == symbolType).ToListAsync();
            if (old.Count > 0)
            {
                context.Symbols.RemoveRange(old);
                await context.SaveChangesAsync();
            }

            var contentRoot = Path.Combine(Directory.GetCurrentDirectory(), "Content", "states");
            if (!Directory.Exists(contentRoot))
                return;

            var deserializer = new DeserializerBuilder().Build();
            var symbols = new List<Symbol>();

            foreach (var file in Directory.EnumerateFiles(contentRoot, yamlFileName, SearchOption.AllDirectories))
            {
                var stateSlug = new DirectoryInfo(Path.GetDirectoryName(file) ?? string.Empty).Name;
                if (string.IsNullOrWhiteSpace(stateSlug))
                    continue;

                var state = states.FirstOrDefault(s => string.Equals(s.Slug, stateSlug, StringComparison.OrdinalIgnoreCase));
                if (state == null)
                    continue;

                Dictionary<object, object>? data;
                try { data = deserializer.Deserialize<Dictionary<object, object>>(File.ReadAllText(file)); }
                catch { continue; }

                if (data == null)
                    continue;

                var name = GetYamlString(data, "name");
                if (string.IsNullOrWhiteSpace(name))
                    name = $"{defaultDesignation} of {state.Name}";

                var designation = GetYamlString(data, "designation_label");
                if (string.IsNullOrWhiteSpace(designation))
                    designation = defaultDesignation;

                var heroImage = ResolveStateGeologyImage(categoryPlural, symbolType, state.Slug, GetYamlString(data, "hero_image"));

                symbols.Add(new Symbol
                {
                    StateId = state.Id,
                    Type = symbolType,
                    Name = name,
                    Slug = GenerateSlug(name),
                    ScientificName = GetYamlString(data, "chemical_formula"),
                    AdoptedYear = GetYamlInt(data, "adopted_year"),
                    Status = GetYamlBool(data, "is_official") ? "Official" : null,
                    Designation = designation,
                    Legislation = GetYamlString(data, "legislation"),
                    WikidataId = null,
                    Meaning = GetYamlString(data, "meaning"),
                    ImageUrl = heroImage,
                    YamlPath = $"Content/states/{state.Slug}/{yamlFileName}"
                });
            }

            if (symbols.Count == 0)
                return;

            context.Symbols.AddRange(symbols);
            await context.SaveChangesAsync();
        }

        private static Task SeedStateMinerals(AppDbContext context, List<State> states)
            => SeedStateGeologySymbols(context, states, "mineral", "mineral.yaml", "State Mineral", "minerals");

        private static Task SeedStateRocks(AppDbContext context, List<State> states)
            => SeedStateGeologySymbols(context, states, "rock", "rock.yaml", "State Rock", "rocks");

        private static Task SeedStateGemstones(AppDbContext context, List<State> states)
            => SeedStateGeologySymbols(context, states, "gemstone", "gemstone.yaml", "State Gemstone", "gemstones");

        // Shared loader for State Amphibian / State Reptile, one or more content files per
        // designation (amphibian*.yaml / reptile*.yaml, states can have more than one), the
        // same wildcard-discovery pattern SeedStateInsects uses.
        private static async Task SeedStateCreatureSymbols(
            AppDbContext context,
            List<State> states,
            string symbolType,
            string yamlFilePattern,
            string defaultDesignation)
        {
            var old = await context.Symbols.Where(s => s.Type == symbolType).ToListAsync();
            if (old.Count > 0)
            {
                context.Symbols.RemoveRange(old);
                await context.SaveChangesAsync();
            }

            var contentRoot = Path.Combine(Directory.GetCurrentDirectory(), "Content", "states");
            if (!Directory.Exists(contentRoot))
                return;

            var deserializer = new DeserializerBuilder().Build();
            var symbols = new List<Symbol>();

            foreach (var file in Directory.EnumerateFiles(contentRoot, yamlFilePattern, SearchOption.AllDirectories))
            {
                var stateSlug = new DirectoryInfo(Path.GetDirectoryName(file) ?? string.Empty).Name;
                if (string.IsNullOrWhiteSpace(stateSlug))
                    continue;

                var state = states.FirstOrDefault(s => string.Equals(s.Slug, stateSlug, StringComparison.OrdinalIgnoreCase));
                if (state == null)
                    continue;

                Dictionary<object, object>? data;
                try { data = deserializer.Deserialize<Dictionary<object, object>>(File.ReadAllText(file)); }
                catch { continue; }

                if (data == null)
                    continue;

                var name = GetYamlString(data, "name");
                if (string.IsNullOrWhiteSpace(name))
                    name = $"{defaultDesignation} of {state.Name}";

                var designation = GetYamlString(data, "designation");
                if (string.IsNullOrWhiteSpace(designation))
                    designation = defaultDesignation;

                symbols.Add(new Symbol
                {
                    StateId = state.Id,
                    Type = symbolType,
                    Name = name,
                    Slug = GenerateSlug(name),
                    ScientificName = GetYamlString(data, "binomial_name"),
                    AdoptedYear = GetYamlInt(data, "adopted_year"),
                    Status = GetYamlBool(data, "is_official") ? "Official" : null,
                    Designation = designation,
                    Legislation = GetYamlString(data, "legislation"),
                    WikidataId = null,
                    Meaning = GetYamlString(data, "meaning"),
                    ImageUrl = GetYamlString(data, "hero_image"),
                    YamlPath = $"Content/states/{state.Slug}/{Path.GetFileName(file)}"
                });
            }

            if (symbols.Count == 0)
                return;

            context.Symbols.AddRange(symbols);
            await context.SaveChangesAsync();
        }

        private static Task SeedStateAmphibians(AppDbContext context, List<State> states)
            => SeedStateCreatureSymbols(context, states, "amphibian", "amphibian*.yaml", "State Amphibian");

        private static Task SeedStateReptiles(AppDbContext context, List<State> states)
            => SeedStateCreatureSymbols(context, states, "reptile", "reptile*.yaml", "State Reptile");

        // States commonly have many food designations (State Cookie, State Nut, State Fruit,
        // State Legume...), one YAML file per designation, e.g. food-cookie.yaml, food-nut.yaml.
        private static Task SeedStateFoods(AppDbContext context, List<State> states)
            => SeedStateCreatureSymbols(context, states, "food", "food*.yaml", "State Food");

        private static async Task SeedStateFossils(AppDbContext context, List<State> states)
        {
            var old = await context.Symbols.Where(s => s.Type == "fossil").ToListAsync();
            if (old.Count > 0)
            {
                context.Symbols.RemoveRange(old);
                await context.SaveChangesAsync();
            }

            var fossilEntries = new (string StateSlug, string Name, string ScientificName, string Age, int? Year, string Legislation, string Meaning, string YamlFile)[]
            {
                ("alabama",       "Basilosaurus Whale",       "Basilosaurus cetoides",                        "Eocene",                   1984, "Act 84-108",                     "An ancient whale up to 60 feet long that lived 40–34 million years ago. Fossils cannot be removed from Alabama without the governor's written approval.",                                                                    "fossil.yaml"),
                ("alaska",        "Woolly Mammoth",           "Mammuthus primigenius",                        "Pleistocene",              1986, "Alaska Statutes § 44.09.063",    "Frequently unearthed by gold miners as stream banks erode — one of the most commonly found Pleistocene mammals in Alaska.",                                                                                                 "fossil.yaml"),
                ("arizona",       "Petrified Wood",           "Araucarioxylon arizonicum",                    "Triassic",                 1988, "A.R.S. § 41-859",                "The most abundant fossil tree in Arizona's Petrified Forest National Park, deposited approximately 225 million years ago.",                                                                                                   "fossil.yaml"),
                ("california",    "Saber-Toothed Cat",        "Smilodon fatalis",                             "Pleistocene",              1974, "California Government Code § 425","Thousands of specimens recovered from the La Brea Tar Pits — the richest single source of Pleistocene mammals in the world.",                                                                                                   "fossil.yaml"),
                ("colorado",      "Stegosaurus",              "Stegosaurus armatus",                          "Jurassic",                 1982, "C.R.S. § 24-80-914",             "One of the most recognizable dinosaurs, found in Colorado's Jurassic Morrison Formation. Despite weighing up to 10 tons, its brain was roughly the size of a walnut.",                                                        "fossil.yaml"),
                ("connecticut",   "Dinosaur Tracks",          "Eubrontes giganteus",                          "Jurassic",                 1991, "Conn. Gen. Stat. § 3-110e",      "Three-toed tracks in the Connecticut Valley's sandstone were the first dinosaur fossils discovered in North America. No skeleton of the trackmaker has ever been found.",                                                        "fossil.yaml"),
                ("delaware",      "Belemnite",                "Belemnitella americana",                       "Cretaceous",               1996, "29 Del. C. § 304",               "Extinct squid-like cephalopods found in abundance along the Chesapeake and Delaware Canal in the Late Cretaceous Mount Laurel Formation.",                                                                                   "fossil.yaml"),
                ("georgia",       "Shark Tooth",              "Carcharocles megalodon",                       "Cretaceous–Miocene",       1976, "O.C.G.A. § 50-3-61",            "Fossil shark teeth up to 7 inches long, found in Georgia's Cretaceous through Miocene deposits. Sharks shed thousands of teeth per lifetime, making shark teeth the most common Georgia fossil.",                             "fossil.yaml"),
                ("idaho",         "Hagerman Horse",           "Equus simplicidens",                           "Pliocene",                 1988, "Idaho Code § 67-4508",           "One of the oldest Equus species, resembling the modern African zebra. Nearly 200 skeletons recovered from Hagerman Fossil Beds National Monument.",                                                                            "fossil.yaml"),
                ("illinois",      "Tully Monster",            "Tullimonstrum gregarium",                      "Pennsylvanian",            1989, "5 ILCS 460/20",                  "The most enigmatic state fossil — a 300-million-year-old creature that does not fit any known animal phylum. Found only in Illinois's Mazon Creek fossil beds.",                                                              "fossil.yaml"),
                ("indiana",       "American Mastodon",        "Mammut americanum",                            "Holocene",                 2022, "IC 1-2-10",                      "Designated in 2022 after a campaign by elementary school students in Greensburg. Mastodon remains are found throughout Indiana in glacial lake deposits.",                                                                        "fossil.yaml"),
                ("kansas",        "Pteranodon",               "Pteranodon longiceps",                         "Cretaceous",               2014, "K.S.A. 73-2003",                 "Kansas's state flying fossil — a large pterosaur from the Western Interior Seaway that covered Kansas 85 million years ago. Designated in 2014 alongside Tylosaurus.",                                                        "fossil-flying.yaml"),
                ("kansas",        "Tylosaurus",               "Tylosaurus kansasensis",                       "Cretaceous",               2014, "K.S.A. 73-2003",                 "Kansas's state marine fossil — a mosasaur up to 45 feet long that hunted in the shallow sea covering Kansas 85 million years ago. Designated in 2014 alongside Pteranodon.",                                                "fossil-marine.yaml"),
                ("kentucky",      "Brachiopod",               "Undetermined species",                         "Ordovician–Pennsylvanian", 1986, "KRS 2.095",                      "Kentucky designated the entire brachiopod group rather than any single species — their shells are so common in Kentucky's Paleozoic rocks that picking one would exclude hundreds of others.",                                   "fossil.yaml"),
                ("louisiana",     "Petrified Palmwood",       "Palmoxylon sp.",                               "Oligocene",                1976, "La. R.S. 49:173",                "Found in Louisiana's Catahoula Formation — coastal plain deposits from about 30 million years ago. Distinguished by distinctive rod-like structures visible in cross-section.",                                                "fossil.yaml"),
                ("maine",         "Pertica Plant",            "Pertica quadrifaria",                          "Devonian",                 1976, "1 M.R.S.A. § 209",               "An extinct vascular plant from around 390 million years ago, first described from compression fossils found in northern Maine's Trout Valley Formation in 1972.",                                                              "fossil.yaml"),
                ("maryland",      "Ecphora Shell",            "Ecphora gardnerae gardnerae",                  "Miocene",                  1984, "Md. Code, State Gov't § 13-316", "An extinct carnivorous sea snail, one of the first New World fossils illustrated in a European scientific publication, in 1687. Adopted 1984; species name revised in 1994. Named after paleontologist Julia Gardner.",       "fossil.yaml"),
                ("massachusetts", "Dinosaur Tracks",          "Eubrontes giganteus",                          "Jurassic",                 1980, "M.G.L. c. 2 § 28",              "The Connecticut River Valley of western Massachusetts is one of the world's richest dinosaur track sites. First discovered in the early 1800s, the prints were initially thought to be ancient bird tracks.",               "fossil.yaml"),
                ("michigan",      "American Mastodon",        "Mammut americanum",                            "Holocene",                 2002, "M.C.L. § 2.55",                  "Mastodon remains are found across Michigan in glacial lake beds and peat deposits. Michigan's Petoskey Stone (state stone) is also a fossil — polished Devonian coral fragments.",                                             "fossil.yaml"),
                ("minnesota",     "Giant Beaver",             "Castoroides ohioensis",                        "Pleistocene",              2025, "Minn. Stat. § 1.1495",           "The largest rodent in North American history, up to 8 feet long — the size of a black bear. Designated as state fossil in 2025 after a student-led campaign.",                                                              "fossil.yaml"),
                ("mississippi",   "Prehistoric Whale",        "Zygorhiza kochii",                             "Eocene",                   1981, "Miss. Code § 3-3-37",            "An ancient basilosaurid whale from 40–34 million years ago, from the warm shallow sea that covered the Gulf Coast during the Eocene.",                                                                                       "fossil.yaml"),
                ("missouri",      "Sea Lily",                 "Delocrinus missouriensis",                     "Pennsylvanian",            1989, "Mo. Rev. Stat. § 10.070",        "Crinoids — called sea lilies — are animals, not plants. Related to starfish and sea urchins, their fossils are common in Missouri's Pennsylvanian limestone formations.",                                                        "fossil.yaml"),
                ("montana",       "Maiasaura",                "Maiasaura peeblesorum",                        "Cretaceous",               1985, "Mont. Code § 1-1-514",           "Montana chose Maiasaura because paleontologists Jack Horner and Bob Makela discovered the first known dinosaur nesting colony there in 1978. Maiasaura means 'good mother lizard.'",                                          "fossil.yaml"),
                ("nebraska",      "Mammoth",                  "Mammuthus primigenius / columbi / imperator",  "Pleistocene",              1967, "Neb. Rev. Stat. § 90-106",       "Nebraska was the first state to designate a state fossil in 1967, naming three mammoth species. 'Archie,' discovered in Lincoln County, is the largest mammoth skeleton ever found — 15 feet tall and estimated at 7 tons.", "fossil.yaml"),
                ("nevada",        "Ichthyosaur",              "Shonisaurus popularis",                        "Triassic",                 1977, "NRS 235.070",                    "A marine reptile up to 50 feet long. A bone bed of 37 Shonisaurus individuals was discovered near Berlin, Nevada — now preserved at Berlin-Ichthyosaur State Park.",                                                          "fossil.yaml"),
                ("new-jersey",    "Hadrosaurus",              "Hadrosaurus foulkii",                          "Cretaceous",               1991, "N.J.S.A. 52:9P-1",               "Found in Haddonfield, NJ in 1858, Hadrosaurus foulkii was the first nearly complete dinosaur skeleton in North America. When mounted in 1868, it became the first mounted dinosaur skeleton in the world.",                   "fossil.yaml"),
                ("new-mexico",    "Coelophysis",              "Coelophysis bauri",                            "Triassic",                 1981, "N.M. Stat. § 12-3-5",            "Hundreds of Coelophysis skeletons were found at Ghost Ranch in the 1940s, making it perhaps the best-known Triassic dinosaur in the world. A small carnivore — about 6 feet long and 50 lbs.",                             "fossil.yaml"),
                ("new-york",      "Sea Scorpion",             "Eurypterus remipes",                           "Silurian",                 1984, "N.Y. State Law § 75",            "An extinct arthropod from 432–418 million years ago with large paddles for swimming. Found in 1818 and initially misidentified as a catfish. It lived in the shallow sea that covered upstate New York during the Silurian.",  "fossil.yaml"),
                ("north-carolina","Megalodon Tooth",          "Otodus megalodon",                             "Miocene–Pliocene",         2013, "N.C. Gen. Stat. § 145-40",       "Fossilized teeth from the largest predatory shark ever known, estimated at up to 60 feet long with 7.5-inch serrated teeth. Teeth erode from the Hawthorn Formation and are popular with divers.",                         "fossil.yaml"),
                ("north-dakota",  "Petrified Wood (Teredo)",  "Teredo petrified wood",                        "Paleocene",                1967, "N.D. Cent. Code § 54-02-21",     "Wood bored into by marine shipworm mollusks while drifting as sea flotsam 60 million years ago, then fossilized. Found in the Cannonball Formation of south-central North Dakota.",                                          "fossil.yaml"),
                ("ohio",          "Trilobite",                "Isotelus maximus",                             "Ordovician",               1985, "Ohio Rev. Code § 5.039",         "Ohio's state fossil invertebrate, proposed by Dayton schoolchildren in 1985. Isotelus maximus is the largest trilobite ever found in North America, some specimens over 18 inches long.",                                    "fossil-invertebrate.yaml"),
                ("ohio",          "Dunkleosteus",             "Dunkleosteus terrelli",                        "Devonian",                 2021, "Ohio Rev. Code § 5.039.1",       "Ohio's state fossil fish, designated in 2021. A 20-foot armored apex predator with bony shearing plates instead of teeth and the strongest bite force of any fish ever measured. Found in the Cleveland Shale.",             "fossil-fish.yaml"),
                ("oklahoma",      "Saurophaganax",            "Saurophaganax maximus",                        "Jurassic",                 2000, "Okla. Stat. § 25-98.7",          "A massive allosaurid predator estimated at 34–43 feet in length, from the Late Jurassic Morrison Formation of Oklahoma. First found near Kenton, Oklahoma in the early 1930s.",                                              "fossil.yaml"),
                ("oregon",        "Dawn Redwood",             "Metasequoia sp.",                              "Eocene",                   2005, "ORS 186.060",                    "Designated in 2005 after a fossil enthusiast gave every Oregon legislator a Metasequoia fossil. The dawn redwood is a deciduous conifer that flourished 34–5 million years ago.",                                                 "fossil.yaml"),
                ("pennsylvania",  "Trilobite",                "Phacops rana",                                 "Devonian",                 1988, "71 Pa. C.S. § 1001",             "Phacops may be the world's most recognizable trilobite. Proposed by an elementary school science class and designated in 1988. Found in Pennsylvania's Devonian-age rocks.",                                                 "fossil.yaml"),
                ("rhode-island",  "Trilobite",                "Genus and species not specified",              "Paleozoic",                2023, "R.I. Gen. Laws § 42-4-18",       "Rhode Island designated the trilobite in 2023 without naming a specific genus or species — the least specific state fossil designation. One of the most recently adopted.",                                                  "fossil.yaml"),
                ("south-carolina","Columbian Mammoth",        "Mammuthus columbi",                            "Pleistocene",              2014, "S.C. Code § 1-1-710",            "The bill nearly included Genesis language from creationist legislators before passing without it. Columbian mammoths were larger than woolly mammoths and roamed the southern U.S.",                                             "fossil.yaml"),
                ("south-dakota",  "Triceratops",              "Triceratops horridus",                         "Cretaceous",               1988, "S.D.C.L. § 1-6-18",              "The Hell Creek Formation across South Dakota is one of the world's richest sources of Triceratops fossils. South Dakota replaced a cycad (fossil plant) with Triceratops in 1988.",                                          "fossil.yaml"),
                ("tennessee",     "Pterotrigonia Bivalve",    "Pterotrigonia thoracica",                      "Cretaceous",               1998, "T.C.A. § 4-1-329",               "An extinct bivalve mollusk from when much of western Tennessee was covered by a shallow sea about 70 million years ago. Common in the Coon Creek Formation of McNairy County.",                                               "fossil.yaml"),
                ("utah",          "Allosaurus",               "Allosaurus fragilis",                          "Jurassic",                 1988, "Utah Code § 63G-1-601",          "The most common large predatory dinosaur in Utah's Jurassic Morrison Formation. Over 60 Allosaurus skeletons were found in a single Utah quarry. It may have used its upper jaw like a hatchet.",                            "fossil.yaml"),
                ("vermont",       "Beluga Whale",             "Delphinapterus leucas",                        "Pleistocene",              1993, "1 V.S.A. § 498",                 "Vermont's state marine fossil — the only state fossil from a species still living today. A beluga skeleton was found in 1849 near Charlotte in glacial lake sediments laid down when the Champlain Sea covered the valley.",   "fossil-marine.yaml"),
                ("vermont",       "Woolly Mammoth",           "Mammuthus primigenius",                        "Pleistocene",              2014, "1 V.S.A. § 498",                 "Vermont's state terrestrial fossil, designated in 2014. Woolly mammoth bones have been found at several sites in the Champlain Valley, deposited when glaciers retreated about 12,000 years ago.",                           "fossil-terrestrial.yaml"),
                ("virginia",      "Chesapecten Scallop",      "Chesapecten jeffersonius",                     "Pliocene",                 1993, "Code of Va. § 7.1-40",           "The first fossil from the New World illustrated in a European scientific publication — in 1687. Named after Thomas Jefferson for his interest in natural history. Common in streams of southeastern Virginia.",                 "fossil.yaml"),
                ("washington",    "Columbian Mammoth",        "Mammuthus columbi",                            "Pleistocene",              1998, "RCW 1.20.110",                   "Fossilized remains found on the Olympic Peninsula. Washington also designates petrified wood as its state stone, making it one of several states with two fossil-related official symbols.",                                      "fossil.yaml"),
                ("west-virginia", "Jefferson's Ground Sloth", "Megalonyx jeffersonii",                        "Pleistocene",              2008, "W. Va. Code § 2-2-9",            "Named after Thomas Jefferson, who described Megalonyx to the American Philosophical Society in 1797 — the paper that launched vertebrate paleontology in North America. Stood nearly 10 feet tall.",                         "fossil.yaml"),
                ("wisconsin",     "Trilobite",                "Calymene celebra",                             "Silurian",                 1985, "Wis. Stat. § 1.10(6)",           "Calymene celebra lived when warm shallow seas covered Wisconsin during the Silurian. Found in the Niagara dolomite outcroppings across the state.",                                                                          "fossil.yaml"),
                ("wyoming",       "Knightia",                 "Knightia spp.",                                "Eocene",                   1987, "Wyo. Stat. § 8-3-112",           "A genus of fossil herring preserved in massive numbers in Wyoming's Green River Formation — ancient lakes where mass die-offs created extraordinary fish preservation. Knightia is the most commonly found vertebrate fossil in the world.", "fossil.yaml"),
            };

            var stateIndex = states.ToDictionary(s => s.Slug, s => s);
            var fossils = new List<Symbol>();

            foreach (var entry in fossilEntries)
            {
                if (!stateIndex.TryGetValue(entry.StateSlug, out var state)) continue;
                fossils.Add(new Symbol
                {
                    StateId = state.Id,
                    Type = "fossil",
                    Name = entry.Name,
                    Slug = GenerateSlug(entry.Name),
                    ScientificName = entry.ScientificName,
                    AdoptedYear = entry.Year,
                    Status = "Official",
                    Designation = "State fossil",
                    Legislation = entry.Legislation,
                    WikidataId = null,
                    Meaning = entry.Meaning,
                    ImageUrl = $"/images/fossils/{entry.StateSlug}.webp",
                    YamlPath = $"Content/states/{entry.StateSlug}/{entry.YamlFile}"
                });
            }

            context.Symbols.AddRange(fossils);
            await context.SaveChangesAsync();
        }

        private static async Task SeedStateSports(AppDbContext context, List<State> states)
        {
            var old = await context.Symbols.Where(s => s.Type == "sport").ToListAsync();
            if (old.Count > 0)
            {
                context.Symbols.RemoveRange(old);
                await context.SaveChangesAsync();
            }

            var contentRoot = Path.Combine(Directory.GetCurrentDirectory(), "Content", "states");
            if (!Directory.Exists(contentRoot))
                return;

            var deserializer = new DeserializerBuilder().Build();
            var symbols = new List<Symbol>();

            var sportDirs = Directory.EnumerateDirectories(contentRoot, "sport", SearchOption.AllDirectories);

            foreach (var sportDir in sportDirs)
            {
                var stateSlug = new DirectoryInfo(Path.GetDirectoryName(sportDir) ?? string.Empty).Name;
                if (string.IsNullOrWhiteSpace(stateSlug))
                    continue;

                var state = states.FirstOrDefault(s => string.Equals(s.Slug, stateSlug, StringComparison.OrdinalIgnoreCase));
                if (state == null)
                    continue;

                foreach (var file in Directory.EnumerateFiles(sportDir, "*.yaml", SearchOption.TopDirectoryOnly))
                {
                    var slug = Path.GetFileNameWithoutExtension(file);
                    if (string.IsNullOrWhiteSpace(slug))
                        continue;

                    Dictionary<object, object>? data;
                    try
                    {
                        data = deserializer.Deserialize<Dictionary<object, object>>(File.ReadAllText(file));
                    }
                    catch
                    {
                        continue;
                    }

                    if (data == null)
                        continue;

                    var name = GetYamlString(data, "name");
                    if (string.IsNullOrWhiteSpace(name))
                        name = GetYamlString(data, "title");
                    if (string.IsNullOrWhiteSpace(name))
                        name = $"State Sport of {state.Name}";

                    symbols.Add(new Symbol
                    {
                        StateId = state.Id,
                        Type = "sport",
                        Name = name,
                        Slug = slug,
                        ScientificName = null,
                        AdoptedYear = GetYamlInt(data, "adopted_year"),
                        Status = GetYamlBool(data, "is_official") ? "Official" : null,
                        Designation = "State sport",
                        Legislation = GetYamlString(data, "legislation"),
                        WikidataId = null,
                        Meaning = GetYamlString(data, "meaning"),
                        ImageUrl = GetYamlString(data, "hero_image"),
                        YamlPath = $"Content/states/{state.Slug}/sport/{slug}.yaml"
                    });
                }
            }

            if (symbols.Count == 0)
                return;

            context.Symbols.AddRange(symbols);
            await context.SaveChangesAsync();
        }

        private static async Task SeedStateDances(AppDbContext context, List<State> states)
        {
            var old = await context.Symbols.Where(s => s.Type == "dance").ToListAsync();
            if (old.Count > 0)
            {
                context.Symbols.RemoveRange(old);
                await context.SaveChangesAsync();
            }

            var contentRoot = Path.Combine(Directory.GetCurrentDirectory(), "Content", "states");
            if (!Directory.Exists(contentRoot))
                return;

            var deserializer = new DeserializerBuilder().Build();
            var symbols = new List<Symbol>();

            var danceDirs = Directory.EnumerateDirectories(contentRoot, "dance", SearchOption.AllDirectories);

            foreach (var danceDir in danceDirs)
            {
                var stateSlug = new DirectoryInfo(Path.GetDirectoryName(danceDir) ?? string.Empty).Name;
                if (string.IsNullOrWhiteSpace(stateSlug))
                    continue;

                var state = states.FirstOrDefault(s => string.Equals(s.Slug, stateSlug, StringComparison.OrdinalIgnoreCase));
                if (state == null)
                    continue;

                foreach (var file in Directory.EnumerateFiles(danceDir, "*.yaml", SearchOption.TopDirectoryOnly))
                {
                    var slug = Path.GetFileNameWithoutExtension(file);
                    if (string.IsNullOrWhiteSpace(slug))
                        continue;

                    Dictionary<object, object>? data;
                    try
                    {
                        data = deserializer.Deserialize<Dictionary<object, object>>(File.ReadAllText(file));
                    }
                    catch
                    {
                        continue;
                    }

                    if (data == null)
                        continue;

                    var name = GetYamlString(data, "name");
                    if (string.IsNullOrWhiteSpace(name))
                        name = GetYamlString(data, "title");
                    if (string.IsNullOrWhiteSpace(name))
                        name = $"State Dance of {state.Name}";

                    symbols.Add(new Symbol
                    {
                        StateId = state.Id,
                        Type = "dance",
                        Name = name,
                        Slug = slug,
                        ScientificName = null,
                        AdoptedYear = GetYamlInt(data, "adopted_year"),
                        Status = GetYamlBool(data, "is_official") ? "Official" : null,
                        Designation = "State dance",
                        Legislation = GetYamlString(data, "legislation"),
                        WikidataId = null,
                        Meaning = GetYamlString(data, "meaning"),
                        ImageUrl = GetYamlString(data, "hero_image"),
                        YamlPath = $"Content/states/{state.Slug}/dance/{slug}.yaml"
                    });
                }
            }

            if (symbols.Count == 0)
                return;

            context.Symbols.AddRange(symbols);
            await context.SaveChangesAsync();
        }

        private static string GetYamlString(Dictionary<object, object> dict, string key)
            => dict.TryGetValue(key, out var value) ? value?.ToString() ?? string.Empty : string.Empty;

        private static int? GetYamlInt(Dictionary<object, object> dict, string key)
            => dict.TryGetValue(key, out var value) && int.TryParse(value?.ToString(), out var result) ? result : null;

        private static bool GetYamlBool(Dictionary<object, object> dict, string key)
            => dict.TryGetValue(key, out var value) && bool.TryParse(value?.ToString(), out var result) && result;

    }


}
