using Microsoft.Extensions.Caching.Memory;
using System.Collections;
using System.Linq;
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
            var statsDir = Path.Combine(_env.ContentRootPath, "Content", "compare", "stats");
            if (!Directory.Exists(statsDir))
                return new Dictionary<string, StateStats>();

            var deserializer = new DeserializerBuilder().Build();

            // Each file under Content/compare/stats/ holds one category (e.g. taxes.yaml, housing.yaml)
            // for all states. Merge every category's fields per state slug before building StateStats.
            var fieldsBySlug = new Dictionary<string, Dictionary<string, object?>>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in Directory.EnumerateFiles(statsDir, "*.yaml").OrderBy(f => f))
            {
                var yaml = await File.ReadAllTextAsync(file);
                var raw = deserializer.Deserialize<Dictionary<string, object>>(yaml);
                if (raw == null) continue;

                foreach (var (slug, node) in raw)
                {
                    if (!fieldsBySlug.TryGetValue(slug, out var target))
                    {
                        target = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                        fieldsBySlug[slug] = target;
                    }
                    FlattenInto(target, node);
                }
            }

            var result = new Dictionary<string, StateStats>();
            foreach (var (slug, fields) in fieldsBySlug)
            {
                var stats = new StateStats { Slug = slug };

                if (fields.TryGetValue("population_estimate_2025", out var populationEstimate) && populationEstimate != null)
                    stats.PopulationEstimate2025 = Convert.ToInt32(populationEstimate);

                if (fields.TryGetValue("population_change_since_2020_pct", out var populationChange) && populationChange != null)
                    stats.PopulationChangeSince2020Pct = Convert.ToDouble(populationChange);

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

                if (fields.TryGetValue("net_domestic_migration", out var netMigration) && netMigration != null)
                    stats.NetDomesticMigration = Convert.ToInt32(netMigration);

                if (fields.TryGetValue("domestic_migration_band", out var migrationBand) && migrationBand != null)
                    stats.DomesticMigrationBand = Convert.ToString(migrationBand);

                if (fields.TryGetValue("single_person_living_wage", out var livingWage) && livingWage != null)
                    stats.SinglePersonLivingWage = Convert.ToInt32(livingWage);

                if (fields.TryGetValue("single_person_living_wage_change_pct", out var livingWageChange) && livingWageChange != null)
                    stats.SinglePersonLivingWageChangePct = Convert.ToDouble(livingWageChange);

                if (fields.TryGetValue("average_credit_score", out var creditScore) && creditScore != null)
                    stats.AverageCreditScore = Convert.ToInt32(creditScore);

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

                if (fields.TryGetValue("grocery_tax_status", out var groceryTaxStatus) && groceryTaxStatus != null)
                    stats.GroceryTaxStatus = Convert.ToString(groceryTaxStatus);

                if (fields.TryGetValue("grocery_tax_rate_pct", out var groceryTaxRate) && groceryTaxRate != null)
                    stats.GroceryTaxRatePct = Convert.ToDouble(groceryTaxRate);

                if (fields.TryGetValue("death_tax_status", out var deathTaxStatus) && deathTaxStatus != null)
                    stats.DeathTaxStatus = Convert.ToString(deathTaxStatus);

                if (fields.TryGetValue("estate_tax_status", out var estateTaxStatus) && estateTaxStatus != null)
                    stats.EstateTaxStatus = Convert.ToString(estateTaxStatus);

                if (fields.TryGetValue("inheritance_tax_status", out var inheritanceTaxStatus) && inheritanceTaxStatus != null)
                    stats.InheritanceTaxStatus = Convert.ToString(inheritanceTaxStatus);

                if (fields.TryGetValue("vehicle_property_tax_status", out var vehiclePropertyTaxStatus) && vehiclePropertyTaxStatus != null)
                    stats.VehiclePropertyTaxStatus = Convert.ToString(vehiclePropertyTaxStatus);

                if (fields.TryGetValue("total_tax_burden_pct", out var taxBurden) && taxBurden != null)
                    stats.TotalTaxBurdenPct = Convert.ToDouble(taxBurden);

                if (fields.TryGetValue("home_insurance_annual_premium", out var homeIns) && homeIns != null)
                    stats.HomeInsuranceAnnualPremium = Convert.ToInt32(homeIns);

                if (fields.TryGetValue("car_insurance_full_coverage_annual", out var carIns) && carIns != null)
                    stats.CarInsuranceFullCoverageAnnual = Convert.ToInt32(carIns);

                if (fields.TryGetValue("unemployment_benefit_max_weekly", out var uiBenefit) && uiBenefit != null)
                    stats.UnemploymentBenefitMaxWeekly = Convert.ToInt32(uiBenefit);

                if (fields.TryGetValue("per_pupil_spending", out var spending) && spending != null)
                    stats.PerPupilSpending = Convert.ToInt32(spending);

                if (fields.TryGetValue("aza_accredited_zoos", out var zoos) && zoos != null)
                    stats.AzaAccreditedZoos = Convert.ToInt32(zoos);

                if (fields.TryGetValue("teacher_average_salary", out var teacherSalary) && teacherSalary != null)
                    stats.TeacherAverageSalary = Convert.ToInt32(teacherSalary);

                if (fields.TryGetValue("public_school_rank", out var schoolRank) && schoolRank != null)
                    stats.PublicSchoolRank = Convert.ToInt32(schoolRank);

                if (fields.TryGetValue("student_loan_debt_avg", out var loanDebt) && loanDebt != null)
                    stats.StudentLoanDebtAvg = Convert.ToInt32(loanDebt);

                if (fields.TryGetValue("college_graduation_rate_4yr_pct", out var gradRate) && gradRate != null)
                    stats.CollegeGraduationRate4yrPct = Convert.ToDouble(gradRate);

                if (fields.TryGetValue("childcare_infant_annual_cost", out var childcare) && childcare != null)
                    stats.ChildcareInfantAnnualCost = Convert.ToInt32(childcare);

                if (fields.TryGetValue("healthcare_score", out var healthcare) && healthcare != null)
                    stats.HealthcareScore = Convert.ToDouble(healthcare);

                if (fields.TryGetValue("gun_ownership_pct", out var gunOwnership) && gunOwnership != null)
                    stats.GunOwnershipPct = Convert.ToDouble(gunOwnership);

                if (fields.TryGetValue("electoral_votes", out var electoralVotes) && electoralVotes != null)
                    stats.ElectoralVotes = Convert.ToInt32(electoralVotes);

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

                if (fields.TryGetValue("abortion_law_status", out var abortionLawStatus) && abortionLawStatus != null)
                    stats.AbortionLawStatus = Convert.ToString(abortionLawStatus);

                if (fields.TryGetValue("abortion_gestational_limit", out var abortionGestationalLimit) && abortionGestationalLimit != null)
                    stats.AbortionGestationalLimit = Convert.ToString(abortionGestationalLimit);

                if (fields.TryGetValue("right_to_work_status", out var rightToWorkStatus) && rightToWorkStatus != null)
                    stats.RightToWorkStatus = Convert.ToString(rightToWorkStatus);

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

                if (fields.TryGetValue("average_wind_speed_mph", out var windSpeed) && windSpeed != null)
                    stats.AverageWindSpeedMph = Convert.ToDouble(windSpeed);

                if (fields.TryGetValue("lightning_density_per_sq_mile", out var lightningDensity) && lightningDensity != null)
                    stats.LightningDensityPerSqMile = Convert.ToDouble(lightningDensity);

                if (fields.TryGetValue("highest_point_name", out var highestPointName) && highestPointName != null)
                    stats.HighestPointName = Convert.ToString(highestPointName);

                if (fields.TryGetValue("highest_point_elevation_ft", out var highestPointElevation) && highestPointElevation != null)
                    stats.HighestPointElevationFt = Convert.ToDouble(highestPointElevation);

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

                if (fields.TryGetValue("infant_mortality_rate_per_1000", out var infantMortality) && infantMortality != null)
                    stats.InfantMortalityRatePer1000 = Convert.ToDouble(infantMortality);

                if (fields.TryGetValue("maternal_mortality_rate_per_100k", out var maternalMortality) && maternalMortality != null)
                    stats.MaternalMortalityRatePer100k = Convert.ToDouble(maternalMortality);

                if (fields.TryGetValue("overdose_death_rate_per_100k", out var overdoseDeathRate) && overdoseDeathRate != null)
                    stats.OverdoseDeathRatePer100k = Convert.ToDouble(overdoseDeathRate);

                if (fields.TryGetValue("water_quality_score", out var waterQualityScore) && waterQualityScore != null)
                    stats.WaterQualityScore = Convert.ToDouble(waterQualityScore);

                if (fields.TryGetValue("water_quality_grade", out var waterQualityGrade) && waterQualityGrade != null)
                    stats.WaterQualityGrade = Convert.ToString(waterQualityGrade);

                if (fields.TryGetValue("power_outage_hours_annual", out var outageHours) && outageHours != null)
                    stats.PowerOutageHoursAnnual = Convert.ToDouble(outageHours);

                if (fields.TryGetValue("road_quality_rank", out var roadQualityRank) && roadQualityRank != null)
                    stats.RoadQualityRank = Convert.ToInt32(roadQualityRank);

                if (fields.TryGetValue("renewable_electricity_pct", out var renewableElectricity) && renewableElectricity != null)
                    stats.RenewableElectricityPct = Convert.ToDouble(renewableElectricity);

                if (fields.TryGetValue("largest_airport_name", out var largestAirportName) && largestAirportName != null)
                    stats.LargestAirportName = Convert.ToString(largestAirportName);

                if (fields.TryGetValue("largest_airport_iata", out var largestAirportIata) && largestAirportIata != null)
                    stats.LargestAirportIata = Convert.ToString(largestAirportIata);

                if (fields.TryGetValue("largest_airport_passengers_millions", out var airportPassengers) && airportPassengers != null)
                    stats.LargestAirportPassengersMillions = Convert.ToDouble(airportPassengers);

                if (fields.TryGetValue("casino_count", out var casinoCount) && casinoCount != null)
                    stats.CasinoCount = Convert.ToInt32(casinoCount);

                if (fields.TryGetValue("best_known_casino", out var bestKnownCasino) && bestKnownCasino != null)
                    stats.BestKnownCasino = Convert.ToString(bestKnownCasino);

                if (fields.TryGetValue("ufo_sightings_total", out var ufoSightingsTotal) && ufoSightingsTotal != null)
                    stats.UfoSightingsTotal = Convert.ToInt32(ufoSightingsTotal);

                if (fields.TryGetValue("ufo_sightings_per_100k", out var ufoSightingsRate) && ufoSightingsRate != null)
                    stats.UfoSightingsPer100k = Convert.ToDouble(ufoSightingsRate);

                if (fields.TryGetValue("daily_screen_time_minutes", out var screenTimeMinutes) && screenTimeMinutes != null)
                    stats.DailyScreenTimeMinutes = Convert.ToInt32(screenTimeMinutes);

                if (fields.TryGetValue("daily_screen_time_label", out var screenTimeLabel) && screenTimeLabel != null)
                    stats.DailyScreenTimeLabel = Convert.ToString(screenTimeLabel);

                if (fields.TryGetValue("most_popular_car_brand", out var popularCarBrand) && popularCarBrand != null)
                    stats.MostPopularCarBrand = Convert.ToString(popularCarBrand);

                if (fields.TryGetValue("most_popular_car_model", out var popularCarModel) && popularCarModel != null)
                    stats.MostPopularCarModel = Convert.ToString(popularCarModel);

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
