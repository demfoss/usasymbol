namespace USASymbol.Services.Content
{
    /// <summary>
    /// Generates plausible-looking (not real, not deliverable) street lines for the Random
    /// Address Generator tool — pairs with a real city/state/ZIP from <see cref="ZipCodeService"/>.
    /// </summary>
    public static class FakeAddressGenerator
    {
        private static readonly string[] StreetNames =
        {
            "Maple", "Oak", "Pine", "Cedar", "Elm", "Washington", "Lincoln", "Jefferson",
            "Franklin", "Madison", "Main", "Park", "Hillcrest", "Sunset", "Highland",
            "Willow", "Birch", "Chestnut", "Spruce", "Meadow", "Ridge", "Lakeview",
            "River", "Forest", "Valley", "Prairie", "Orchard", "Magnolia", "Cypress", "Aspen"
        };

        private static readonly string[] StreetTypes =
        {
            "St", "Ave", "Blvd", "Dr", "Ln", "Rd", "Ct", "Way", "Pl", "Ter"
        };

        public static string GenerateStreetLine()
        {
            var rng = Random.Shared;
            var number = rng.Next(100, 9999);
            var name = StreetNames[rng.Next(StreetNames.Length)];
            var type = StreetTypes[rng.Next(StreetTypes.Length)];
            return $"{number} {name} {type}";
        }
    }
}
