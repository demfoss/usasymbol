using Microsoft.Extensions.Caching.Memory;
using System.Collections;
using USASymbol.Models;
using USASymbol.Services.Interface;
using YamlDotNet.Serialization;

namespace USASymbol.Services
{
    public class ComparisonStatsService : IComparisonStatsService
    {
        private const string CacheKey = "comparison-state-stats";
        private readonly IMemoryCache _cache;
        private readonly IWebHostEnvironment _env;

        public ComparisonStatsService(IMemoryCache cache, IWebHostEnvironment env)
        {
            _cache = cache;
            _env = env;
        }

        public async Task<StateStats?> GetStatsAsync(string stateSlug)
        {
            var all = await GetAllStatsAsync();
            return all.TryGetValue(stateSlug, out var stats) ? stats : null;
        }

        public async Task<Dictionary<string, StateStats>> GetAllStatsAsync()
        {
            if (_cache.TryGetValue(CacheKey, out Dictionary<string, StateStats>? cached) && cached != null)
                return cached;

            var result = await LoadFromFileAsync();
            _cache.Set(CacheKey, result, new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromHours(24)
            });
            return result;
        }

        private async Task<Dictionary<string, StateStats>> LoadFromFileAsync()
        {
            var path = Path.Combine(_env.ContentRootPath, "Content", "compare", "state-stats.yaml");
            if (!File.Exists(path))
                return new Dictionary<string, StateStats>();

            var yaml = await File.ReadAllTextAsync(path);

            var deserializer = new DeserializerBuilder().Build();
            var raw = deserializer.Deserialize<Dictionary<string, object>>(yaml);

            var result = new Dictionary<string, StateStats>();
            foreach (var (slug, node) in raw)
            {
                var fields = FlattenFields(node);
                var stats = new StateStats { Slug = slug };

                if (fields.TryGetValue("land_area_sq_mi", out var area) && area != null)
                    stats.LandAreaSqMi = Convert.ToDouble(area);

                if (fields.TryGetValue("statehood_order", out var order) && order != null)
                    stats.StatehoodOrder = Convert.ToInt32(order);

                if (fields.TryGetValue("median_household_income", out var income) && income != null)
                    stats.MedianHouseholdIncome = Convert.ToInt32(income);

                if (fields.TryGetValue("college_educated_pct", out var collegeEducated) && collegeEducated != null)
                    stats.CollegeEducatedPct = Convert.ToDouble(collegeEducated);

                if (fields.TryGetValue("advanced_degree_pct", out var advancedDegree) && advancedDegree != null)
                    stats.AdvancedDegreePct = Convert.ToDouble(advancedDegree);

                if (fields.TryGetValue("regional_price_parity", out var rpp) && rpp != null)
                    stats.RegionalPriceParity = Convert.ToDouble(rpp);

                if (fields.TryGetValue("purchasing_power_100", out var purchasingPower) && purchasingPower != null)
                    stats.PurchasingPower100 = Convert.ToDouble(purchasingPower);

                if (fields.TryGetValue("poverty_rate_pct", out var poverty) && poverty != null)
                    stats.PovertyRatePct = Convert.ToDouble(poverty);

                if (fields.TryGetValue("employment_population_ratio_pct", out var employmentPopulationRatio) && employmentPopulationRatio != null)
                    stats.EmploymentPopulationRatioPct = Convert.ToDouble(employmentPopulationRatio);

                if (fields.TryGetValue("unemployment_rate_pct", out var unemployment) && unemployment != null)
                    stats.UnemploymentRatePct = Convert.ToDouble(unemployment);

                if (fields.TryGetValue("job_growth_pct", out var jobGrowth) && jobGrowth != null)
                    stats.JobGrowthPct = Convert.ToDouble(jobGrowth);

                if (fields.TryGetValue("minimum_wage_hourly", out var minimumWage) && minimumWage != null)
                    stats.MinimumWageHourly = Convert.ToDouble(minimumWage);

                if (fields.TryGetValue("cost_of_living_index", out var col) && col != null)
                    stats.CostOfLivingIndex = Convert.ToDouble(col);

                if (fields.TryGetValue("gas_price_regular", out var gasPrice) && gasPrice != null)
                    stats.GasPriceRegular = Convert.ToDouble(gasPrice);

                if (fields.TryGetValue("electricity_rate_cents_kwh", out var electricityRate) && electricityRate != null)
                    stats.ElectricityRateCentsKwh = Convert.ToDouble(electricityRate);

                if (fields.TryGetValue("income_tax_rate_pct", out var itax) && itax != null)
                    stats.IncomeTaxRatePct = Convert.ToDouble(itax);

                if (fields.TryGetValue("sales_tax_rate_pct", out var stax) && stax != null)
                    stats.SalesTaxRatePct = Convert.ToDouble(stax);

                if (fields.TryGetValue("property_tax_rate_pct", out var ptax) && ptax != null)
                    stats.PropertyTaxRatePct = Convert.ToDouble(ptax);

                if (fields.TryGetValue("gas_tax_cents", out var gasTax) && gasTax != null)
                    stats.GasTaxCents = Convert.ToDouble(gasTax);

                if (fields.TryGetValue("presidential_margin_pct", out var presidentialMargin) && presidentialMargin != null)
                    stats.PresidentialVotingMarginPct = Convert.ToDouble(presidentialMargin);

                if (fields.TryGetValue("political_lean", out var politicalLean) && politicalLean != null)
                    stats.PoliticalLean = Convert.ToString(politicalLean);

                if (fields.TryGetValue("swing_state", out var swingState) && swingState != null)
                    stats.SwingStateStatus = Convert.ToString(swingState);

                if (fields.TryGetValue("governor_party", out var governorParty) && governorParty != null)
                    stats.GovernorParty = Convert.ToString(governorParty);

                if (fields.TryGetValue("state_house_control", out var houseControl) && houseControl != null)
                    stats.StateHouseControl = Convert.ToString(houseControl);

                if (fields.TryGetValue("state_senate_control", out var senateControl) && senateControl != null)
                    stats.StateSenateControl = Convert.ToString(senateControl);

                if (fields.TryGetValue("trifecta", out var trifecta) && trifecta != null)
                    stats.Trifecta = Convert.ToString(trifecta);

                if (fields.TryGetValue("gun_laws_status", out var gunLawsStatus) && gunLawsStatus != null)
                    stats.GunLawsStatus = Convert.ToString(gunLawsStatus);

                if (fields.TryGetValue("alcohol_laws_status", out var alcoholLawsStatus) && alcoholLawsStatus != null)
                    stats.AlcoholLawsStatus = Convert.ToString(alcoholLawsStatus);

                if (fields.TryGetValue("marijuana_legalization_status", out var marijuanaLegalizationStatus) && marijuanaLegalizationStatus != null)
                    stats.MarijuanaLegalizationStatus = Convert.ToString(marijuanaLegalizationStatus);

                if (fields.TryGetValue("marriage_age_without_consent", out var marriageAgeWithoutConsent) && marriageAgeWithoutConsent != null)
                    stats.MarriageAgeWithoutConsent = Convert.ToInt32(marriageAgeWithoutConsent);

                if (fields.TryGetValue("marriage_min_age", out var marriageMinAge) && marriageMinAge != null)
                    stats.MarriageMinAge = Convert.ToInt32(marriageMinAge);

                if (fields.TryGetValue("marriage_min_age_label", out var marriageMinAgeLabel) && marriageMinAgeLabel != null)
                    stats.MarriageMinAgeLabel = Convert.ToString(marriageMinAgeLabel);

                if (fields.TryGetValue("median_home_value", out var home) && home != null)
                    stats.MedianHomeValue = Convert.ToInt32(home);

                if (fields.TryGetValue("median_gross_rent", out var rent) && rent != null)
                    stats.MedianGrossRent = Convert.ToInt32(rent);

                if (fields.TryGetValue("median_owner_costs_with_mortgage", out var withMortgage) && withMortgage != null)
                    stats.MedianOwnerCostsWithMortgage = Convert.ToInt32(withMortgage);

                if (fields.TryGetValue("median_owner_costs_without_mortgage", out var withoutMortgage) && withoutMortgage != null)
                    stats.MedianOwnerCostsWithoutMortgage = Convert.ToInt32(withoutMortgage);

                if (fields.TryGetValue("owner_costs_to_income_pct", out var ownerCostsToIncome) && ownerCostsToIncome != null)
                    stats.OwnerCostsToIncomePct = Convert.ToDouble(ownerCostsToIncome);

                if (fields.TryGetValue("homeownership_rate_pct", out var ownership) && ownership != null)
                    stats.HomeownershipRatePct = Convert.ToDouble(ownership);

                if (fields.TryGetValue("livability_score", out var livability) && livability != null)
                    stats.LivabilityScore = Convert.ToDouble(livability);

                if (fields.TryGetValue("mean_commute_minutes", out var commute) && commute != null)
                    stats.MeanCommuteMinutes = Convert.ToDouble(commute);

                if (fields.TryGetValue("average_temperature_f", out var avgTemp) && avgTemp != null)
                    stats.AverageTemperatureF = Convert.ToDouble(avgTemp);

                if (fields.TryGetValue("summer_temperature_f", out var summerTemp) && summerTemp != null)
                    stats.SummerTemperatureF = Convert.ToDouble(summerTemp);

                if (fields.TryGetValue("winter_temperature_f", out var winterTemp) && winterTemp != null)
                    stats.WinterTemperatureF = Convert.ToDouble(winterTemp);

                if (fields.TryGetValue("sunny_days_per_year", out var sunnyDays) && sunnyDays != null)
                    stats.SunnyDaysPerYear = Convert.ToInt32(sunnyDays);

                if (fields.TryGetValue("annual_precipitation_in", out var annualPrecip) && annualPrecip != null)
                    stats.AnnualPrecipitationIn = Convert.ToDouble(annualPrecip);

                if (fields.TryGetValue("life_expectancy_years", out var lifeExp) && lifeExp != null)
                    stats.LifeExpectancyYears = Convert.ToDouble(lifeExp);

                if (fields.TryGetValue("uninsured_rate_pct", out var uninsured) && uninsured != null)
                    stats.UninsuredRatePct = Convert.ToDouble(uninsured);

                if (fields.TryGetValue("obesity_rate_pct", out var obesity) && obesity != null)
                    stats.ObesityRatePct = Convert.ToDouble(obesity);

                if (fields.TryGetValue("violent_crime_rate_per_100k", out var violentCrime) && violentCrime != null)
                    stats.ViolentCrimeRatePer100k = Convert.ToDouble(violentCrime);

                if (fields.TryGetValue("property_crime_rate_per_100k", out var propertyCrime) && propertyCrime != null)
                    stats.PropertyCrimeRatePer100k = Convert.ToDouble(propertyCrime);

                if (fields.TryGetValue("k12_rank", out var k12Rank) && k12Rank != null)
                    stats.K12Rank = Convert.ToInt32(k12Rank);

                if (fields.TryGetValue("high_school_graduation_pct", out var hsGrad) && hsGrad != null)
                    stats.HighSchoolGraduationPct = Convert.ToDouble(hsGrad);

                if (fields.TryGetValue("student_teacher_ratio", out var strRatio) && strRatio != null)
                    stats.StudentTeacherRatio = Convert.ToDouble(strRatio);

                if (fields.TryGetValue("hurricane_risk", out var hurricaneRisk) && hurricaneRisk != null)
                    stats.HurricaneRisk = hurricaneRisk.ToString();

                if (fields.TryGetValue("tornado_risk", out var tornadoRisk) && tornadoRisk != null)
                    stats.TornadoRisk = tornadoRisk.ToString();

                if (fields.TryGetValue("earthquake_risk", out var earthquakeRisk) && earthquakeRisk != null)
                    stats.EarthquakeRisk = earthquakeRisk.ToString();

                if (fields.TryGetValue("wildfire_risk", out var wildfireRisk) && wildfireRisk != null)
                    stats.WildfireRisk = wildfireRisk.ToString();

                result[slug] = stats;
            }
            return result;
        }

        private static Dictionary<string, object?> FlattenFields(object? node)
        {
            var flattened = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            FlattenInto(flattened, node);
            return flattened;
        }

        private static void FlattenInto(IDictionary<string, object?> target, object? node)
        {
            if (node is IDictionary<object, object> genericMap)
            {
                foreach (var (key, value) in genericMap)
                {
                    if (key is not string stringKey)
                        continue;

                    if (value is IDictionary || value is IDictionary<object, object>)
                    {
                        FlattenInto(target, value);
                        continue;
                    }

                    target[stringKey] = value;
                }
                return;
            }

            if (node is not IDictionary map)
                return;

            foreach (DictionaryEntry entry in map)
            {
                if (entry.Key is not string key)
                    continue;

                if (entry.Value is IDictionary || entry.Value is IDictionary<object, object>)
                {
                    FlattenInto(target, entry.Value);
                    continue;
                }

                target[key] = entry.Value;
            }
        }
    }
}
