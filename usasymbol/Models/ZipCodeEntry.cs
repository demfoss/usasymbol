namespace USASymbol.Models
{
    public class ZipCodeEntry
    {
        public string Zip { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string County { get; set; } = string.Empty;
        public double? Lat { get; set; }
        public double? Lng { get; set; }
        public int? Population { get; set; }
    }

    public class ZipCodeResult
    {
        public string Zip { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string County { get; set; } = string.Empty;
        public string StateCode { get; set; } = string.Empty;
        public string StateName { get; set; } = string.Empty;
        public string StateSlug { get; set; } = string.Empty;
        public string FlagUrl { get; set; } = string.Empty;
        public double? Lat { get; set; }
        public double? Lng { get; set; }
        public int? Population { get; set; }
    }

    public class StateSummary
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string FlagUrl { get; set; } = string.Empty;
        public int ZipCount { get; set; }
    }

    public class DistanceResult
    {
        public ZipCodeResult From { get; set; } = new();
        public ZipCodeResult To { get; set; } = new();
        public double? Miles { get; set; }
        public double? Kilometers { get; set; }
    }

    public class AreaCodeEntry
    {
        public string AreaCode { get; set; } = string.Empty;
        public string PrimaryState { get; set; } = string.Empty;
        public List<string> States { get; set; } = new();
        public List<string> Cities { get; set; } = new();
    }

    public class AreaCodeResult
    {
        public string AreaCode { get; set; } = string.Empty;
        public string PrimaryStateCode { get; set; } = string.Empty;
        public string PrimaryStateName { get; set; } = string.Empty;
        public string PrimaryStateSlug { get; set; } = string.Empty;
        public string FlagUrl { get; set; } = string.Empty;
        public List<string> Cities { get; set; } = new();
        public bool SpansMultipleStates { get; set; }
        public List<string> OtherStateNames { get; set; } = new();
    }

    public class FakeAddressResult
    {
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string StateCode { get; set; } = string.Empty;
        public string StateName { get; set; } = string.Empty;
        public string StateSlug { get; set; } = string.Empty;
        public string Zip { get; set; } = string.Empty;
        public string FlagUrl { get; set; } = string.Empty;
    }

    public class NearestParkResult
    {
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string StateCode { get; set; } = string.Empty;
        public string NearestCity { get; set; } = string.Empty;
        public string GoogleMapsUrl { get; set; } = string.Empty;
        public double DistanceMiles { get; set; }
    }
}
