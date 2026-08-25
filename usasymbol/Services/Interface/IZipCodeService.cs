using USASymbol.Models;

namespace usasymbol.Services.Interface
{
    public interface IZipCodeService
    {
        List<ZipCodeResult> GetRandom(int count, string? stateCode);
        List<ZipCodeResult> GetRandomCities(int count, string? stateCode);
        ZipCodeResult? GetByZip(string zip);
        List<ZipCodeResult> FindByCity(string city, string? stateCode);
        List<StateSummary> GetStateSummaries();
        DistanceResult? GetDistance(string fromZip, string toZip);
        (int? Population, List<ZipCodeResult> Zips)? GetCityPopulation(string city, string stateCode);
        FakeAddressResult GenerateRandomAddress(string? stateCode);
    }
}
