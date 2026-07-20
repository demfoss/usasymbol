using USASymbol.Models;

namespace USASymbol.Services
{
    public static class ComparisonMetricsConfig
    {
        public static readonly List<ComparisonMetricDefinition> All = new()
        {
            new()
            {
                Slug = "population",
                Name = "Population",
                Description = "Total resident population (2020 Census).",
                GroupSlug = "demographics",
                GroupName = "Demographics",
                GroupOrder = 1,
                SortOrder = 1,
                Unit = "people",
                Icon = "fa-solid fa-people-group",
                Type = MetricType.Numeric,
                HigherIsBetter = true,
                GetNumericValue = (state, _) => state.Population.HasValue ? (double)state.Population.Value : null,
                GetDisplayValue = (state, _) => state.Population.HasValue ? state.Population.Value.ToString("N0") : null,
                GenerateSummary = (a, _, b, _) => CompareStates(
                    a, b,
                    a.Population.HasValue ? (double?)a.Population.Value : null,
                    b.Population.HasValue ? (double?)b.Population.Value : null,
                    true,
                    "Population data is not available for comparison.",
                    "have the same population",
                    "has a larger population than",
                    "people")
            },
            new()
            {
                Slug = "density",
                Name = "Population Density",
                Description = "Number of residents per square mile.",
                GroupSlug = "demographics",
                GroupName = "Demographics",
                GroupOrder = 1,
                SortOrder = 2,
                Unit = "per sq mi",
                Icon = "fa-solid fa-city",
                Type = MetricType.Numeric,
                HigherIsBetter = true,
                GetNumericValue = (state, stats) => stats?.PopulationDensity(state.Population),
                GetDisplayValue = (state, stats) => stats?.PopulationDensity(state.Population) is double d ? $"{d:N1} per sq mi" : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b, sa?.PopulationDensity(a.Population), sb?.PopulationDensity(b.Population), true,
                    "Density data is not available for comparison.",
                    "have the same population density",
                    "is more densely populated than")
            },
            new()
            {
                Slug = "college-educated",
                Name = "Bachelor's Degree",
                Description = "Adults age 25+ with a bachelor's degree or higher (ACS 2023).",
                GroupSlug = "demographics",
                GroupName = "Demographics",
                GroupOrder = 1,
                SortOrder = 3,
                Unit = "%",
                Icon = "fa-solid fa-graduation-cap",
                Type = MetricType.Numeric,
                HigherIsBetter = true,
                GetNumericValue = (_, stats) => stats?.CollegeEducatedPct,
                GetDisplayValue = (_, stats) => stats?.CollegeEducatedPct is double value ? $"{value:F1}%" : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b, sa?.CollegeEducatedPct, sb?.CollegeEducatedPct, true,
                    "College education data is not available for comparison.",
                    "have the same college-educated share",
                    "has a higher college-educated share than")
            },
            new()
            {
                Slug = "advanced-degree",
                Name = "Advanced Degree",
                Description = "Adults age 25+ with a graduate or professional degree (ACS 2023).",
                GroupSlug = "demographics",
                GroupName = "Demographics",
                GroupOrder = 1,
                SortOrder = 4,
                Unit = "%",
                Icon = "fa-solid fa-user-graduate",
                Type = MetricType.Numeric,
                HigherIsBetter = true,
                GetNumericValue = (_, stats) => stats?.AdvancedDegreePct,
                GetDisplayValue = (_, stats) => stats?.AdvancedDegreePct is double value ? $"{value:F1}%" : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b, sa?.AdvancedDegreePct, sb?.AdvancedDegreePct, true,
                    "Advanced degree data is not available for comparison.",
                    "have the same advanced-degree share",
                    "has a higher advanced-degree share than")
            },
            new()
            {
                Slug = "median-income",
                Name = "Median Income",
                Description = "Median household income in U.S. dollars.",
                GroupSlug = "economy",
                GroupName = "Income",
                GroupOrder = 2,
                SortOrder = 1,
                Unit = "USD",
                Icon = "fa-solid fa-dollar-sign",
                Type = MetricType.Numeric,
                HigherIsBetter = true,
                GetNumericValue = (_, stats) => stats?.MedianHouseholdIncome,
                GetDisplayValue = (_, stats) => stats?.MedianHouseholdIncome is int value ? $"${value:N0}" : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b, sa?.MedianHouseholdIncome, sb?.MedianHouseholdIncome, true,
                    "Income data is not available for comparison.",
                    "have the same median household income",
                    "has a higher median household income than",
                    "USD")
            },
            new()
            {
                Slug = "poverty-rate",
                Name = "Poverty Rate",
                Description = "Share of residents below the federal poverty line (ACS 2023).",
                GroupSlug = "economy",
                GroupName = "Income",
                GroupOrder = 2,
                SortOrder = 3,
                Unit = "%",
                Icon = "fa-solid fa-hand-holding-dollar",
                Type = MetricType.Numeric,
                HigherIsBetter = false,
                GetNumericValue = (_, stats) => stats?.PovertyRatePct,
                GetDisplayValue = (_, stats) => stats?.PovertyRatePct is double value ? $"{value:F1}%" : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b, sa?.PovertyRatePct, sb?.PovertyRatePct, false,
                    "Poverty rate data is not available for comparison.",
                    "have the same poverty rate",
                    "has a lower poverty rate than")
            },
            new()
            {
                Slug = "employment-population-ratio",
                Name = "Employment/Population Ratio",
                Description = "Employed civilian population as a share of the adult population (ACS 2023).",
                GroupSlug = "economy",
                GroupName = "Income",
                GroupOrder = 2,
                SortOrder = 4,
                Unit = "%",
                Icon = "fa-solid fa-briefcase",
                Type = MetricType.Numeric,
                HigherIsBetter = true,
                GetNumericValue = (_, stats) => stats?.EmploymentPopulationRatioPct,
                GetDisplayValue = (_, stats) => stats?.EmploymentPopulationRatioPct is double value ? $"{value:F1}%" : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b, sa?.EmploymentPopulationRatioPct, sb?.EmploymentPopulationRatioPct, true,
                    "Employment/population ratio data is not available for comparison.",
                    "have the same employment/population ratio",
                    "has a higher employment/population ratio than")
            },
            new()
            {
                Slug = "unemployment-rate",
                Name = "Unemployment Rate",
                Description = "Seasonally adjusted unemployment rate (BLS, December 2025).",
                GroupSlug = "economy",
                GroupName = "Income",
                GroupOrder = 2,
                SortOrder = 5,
                Unit = "%",
                Icon = "fa-solid fa-user-minus",
                Type = MetricType.Numeric,
                HigherIsBetter = false,
                GetNumericValue = (_, stats) => stats?.UnemploymentRatePct,
                GetDisplayValue = (_, stats) => stats?.UnemploymentRatePct is double value ? $"{value:F1}%" : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b, sa?.UnemploymentRatePct, sb?.UnemploymentRatePct, false,
                    "Unemployment rate data is not available for comparison.",
                    "have the same unemployment rate",
                    "has a lower unemployment rate than")
            },
            new()
            {
                Slug = "job-growth",
                Name = "Job Growth",
                Description = "Change in total nonfarm payroll employment from December 2024 to December 2025 (BLS).",
                GroupSlug = "economy",
                GroupName = "Income",
                GroupOrder = 2,
                SortOrder = 6,
                Unit = "%",
                Icon = "fa-solid fa-arrow-trend-up",
                Type = MetricType.Numeric,
                HigherIsBetter = true,
                GetNumericValue = (_, stats) => stats?.JobGrowthPct,
                GetDisplayValue = (_, stats) => stats?.JobGrowthPct is double value ? $"{value:F1}%" : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b, sa?.JobGrowthPct, sb?.JobGrowthPct, true,
                    "Job growth data is not available for comparison.",
                    "have the same job growth rate",
                    "has faster job growth than")
            },
            new()
            {
                Slug = "childcare-costs",
                Name = "Infant Childcare Cost",
                Description = "Average annual cost of full-time infant daycare.",
                GroupSlug = "economy",
                GroupName = "Income",
                GroupOrder = 2,
                SortOrder = 7,
                Unit = "USD",
                Icon = "fa-solid fa-baby",
                Type = MetricType.Numeric,
                HigherIsBetter = false,
                GetNumericValue = (_, stats) => stats?.ChildcareInfantAnnualCost.HasValue == true ? (double)stats.ChildcareInfantAnnualCost.Value : null,
                GetDisplayValue = (_, stats) => stats?.ChildcareInfantAnnualCost is int value ? $"${value:N0}/yr" : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b,
                    sa?.ChildcareInfantAnnualCost.HasValue == true ? (double?)sa.ChildcareInfantAnnualCost.Value : null,
                    sb?.ChildcareInfantAnnualCost.HasValue == true ? (double?)sb.ChildcareInfantAnnualCost.Value : null,
                    false,
                    "Childcare cost data is not available for comparison.",
                    "have the same average infant childcare cost",
                    "has cheaper average infant childcare than",
                    "USD")
            },
            new()
            {
                Slug = "regional-price-parity",
                Name = "Regional Price Parity",
                Description = "Official price level relative to the national average (100 = U.S. average).",
                GroupSlug = "economy",
                GroupName = "Income",
                GroupOrder = 2,
                SortOrder = 2,
                Unit = "index",
                Icon = "fa-solid fa-scale-balanced",
                Type = MetricType.Numeric,
                HigherIsBetter = false,
                GetNumericValue = (_, stats) => stats?.RegionalPriceParity,
                GetDisplayValue = (_, stats) => stats?.RegionalPriceParity is double value ? $"{value:F1}" : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b, sa?.RegionalPriceParity, sb?.RegionalPriceParity, false,
                    "Regional price parity data is not available for comparison.",
                    "have the same regional price parity",
                    "has a lower official price level than")
            },
            new()
            {
                Slug = "purchasing-power",
                Name = "Purchasing Power of $100",
                Description = "Real local value of $100 after adjusting for BEA Regional Price Parities.",
                GroupSlug = "economy",
                GroupName = "Income",
                GroupOrder = 2,
                SortOrder = 2,
                Unit = "USD",
                Icon = "fa-solid fa-money-bill-wave",
                Type = MetricType.Numeric,
                HigherIsBetter = true,
                GetNumericValue = (_, stats) => stats?.PurchasingPower100,
                GetDisplayValue = (_, stats) => stats?.PurchasingPower100 is double value ? $"${value:F2}" : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b, sa?.PurchasingPower100, sb?.PurchasingPower100, true,
                    "Purchasing power data is not available for comparison.",
                    "give the same real value for $100",
                    "makes $100 go further than",
                    "USD")
            },
            new()
            {
                Slug = "minimum-wage",
                Name = "Minimum Wage",
                Description = "State minimum wage in U.S. dollars per hour (U.S. Department of Labor, updated January 1, 2026).",
                GroupSlug = "jobs",
                GroupName = "Jobs",
                GroupOrder = 2,
                SortOrder = 1,
                Unit = "USD",
                Icon = "fa-solid fa-wallet",
                Type = MetricType.Numeric,
                HigherIsBetter = true,
                GetNumericValue = (_, stats) => stats?.MinimumWageHourly,
                GetDisplayValue = (_, stats) => stats?.MinimumWageHourly is double value ? $"${value:F2}/hr" : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b, sa?.MinimumWageHourly, sb?.MinimumWageHourly, true,
                    "Minimum wage data is not available for comparison.",
                    "have the same minimum wage",
                    "has a higher minimum wage than",
                    "USD")
            },
            new()
            {
                Slug = "unemployment-benefit",
                Name = "Unemployment Benefit",
                Description = "Maximum weekly unemployment insurance benefit (IndeedFlex 2026).",
                GroupSlug = "jobs",
                GroupName = "Jobs",
                GroupOrder = 2,
                SortOrder = 2,
                Unit = "USD",
                Icon = "fa-solid fa-hand-holding-dollar",
                Type = MetricType.Numeric,
                HigherIsBetter = true,
                GetNumericValue = (_, stats) => stats?.UnemploymentBenefitMaxWeekly.HasValue == true ? (double)stats.UnemploymentBenefitMaxWeekly.Value : null,
                GetDisplayValue = (_, stats) => stats?.UnemploymentBenefitMaxWeekly is int value ? $"${value:N0}/wk" : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b,
                    sa?.UnemploymentBenefitMaxWeekly.HasValue == true ? (double?)sa.UnemploymentBenefitMaxWeekly.Value : null,
                    sb?.UnemploymentBenefitMaxWeekly.HasValue == true ? (double?)sb.UnemploymentBenefitMaxWeekly.Value : null,
                    true,
                    "Unemployment benefit data is not available for comparison.",
                    "pay the same maximum weekly unemployment benefit",
                    "pays a higher maximum weekly unemployment benefit than",
                    "USD")
            },
            new()
            {
                Slug = "livability-score",
                Name = "Livability Score",
                Description = "Best States to Live In total score (August 11, 2025).",
                GroupSlug = "quality-of-life",
                GroupName = "Quality of Life",
                GroupOrder = 3,
                SortOrder = 1,
                Unit = "score",
                Icon = "fa-solid fa-star",
                Type = MetricType.Numeric,
                HigherIsBetter = true,
                GetNumericValue = (_, stats) => stats?.LivabilityScore,
                GetDisplayValue = (_, stats) => stats?.LivabilityScore is double value ? $"{value:F2}" : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b, sa?.LivabilityScore, sb?.LivabilityScore, true,
                    "Livability score data is not available for comparison.",
                    "have the same livability score",
                    "has a higher livability score than")
            },
            new()
            {
                Slug = "best-state-to-live-in",
                Name = "Best State to Live In",
                Description = "Composite living score for comparing states to live in, based on the existing livability score.",
                GroupSlug = "quality-of-life",
                GroupName = "Quality of Life",
                GroupOrder = 3,
                SortOrder = 2,
                Unit = "score",
                Icon = "fa-solid fa-location-dot",
                Type = MetricType.Numeric,
                HigherIsBetter = true,
                GetNumericValue = (_, stats) => stats?.BestStateToLiveInScore(),
                GetDisplayValue = (_, stats) => stats?.BestStateToLiveInScore() is double value ? $"{value:F2}" : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b, sa?.BestStateToLiveInScore(), sb?.BestStateToLiveInScore(), true,
                    "Best-state-to-live-in data is not available for comparison.",
                    "have the same overall living score",
                    "scores higher as a state to live in")
            },
            new()
            {
                Slug = "cost-of-living",
                Name = "Cost of Living",
                Description = "Composite cost of living index (100 = national average). Lower = more affordable.",
                GroupSlug = "quality-of-life",
                GroupName = "Quality of Life",
                GroupOrder = 3,
                SortOrder = 3,
                Unit = "index",
                Icon = "fa-solid fa-cart-shopping",
                Type = MetricType.Numeric,
                HigherIsBetter = false,
                GetNumericValue = (_, stats) => stats?.CostOfLivingIndex,
                GetDisplayValue = (_, stats) => stats?.CostOfLivingIndex is double value ? $"{value:F1}" : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b, sa?.CostOfLivingIndex, sb?.CostOfLivingIndex, false,
                    "Cost of living data is not available for comparison.",
                    "have the same cost of living index",
                    "is more affordable than")
            },
            new()
            {
                Slug = "commute-time",
                Name = "Commute Time",
                Description = "Average commute time in minutes.",
                GroupSlug = "quality-of-life",
                GroupName = "Quality of Life",
                GroupOrder = 3,
                SortOrder = 6,
                Unit = "minutes",
                Icon = "fa-solid fa-road",
                Type = MetricType.Numeric,
                HigherIsBetter = false,
                GetNumericValue = (_, stats) => stats?.MeanCommuteMinutes,
                GetDisplayValue = (_, stats) => stats?.MeanCommuteMinutes is double value ? $"{value:F1} min" : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b, sa?.MeanCommuteMinutes, sb?.MeanCommuteMinutes, false,
                    "Commute time data is not available for comparison.",
                    "have the same average commute time",
                    "has a shorter average commute than")
            },
            new()
            {
                Slug = "gas-price",
                Name = "Gas Price",
                Description = "Average regular gasoline price by state (AAA State Gas Price Averages, updated daily).",
                GroupSlug = "quality-of-life",
                GroupName = "Quality of Life",
                GroupOrder = 3,
                SortOrder = 4,
                Unit = "USD",
                Icon = "fa-solid fa-gas-pump",
                Type = MetricType.Numeric,
                HigherIsBetter = false,
                GetNumericValue = (_, stats) => stats?.GasPriceRegular,
                GetDisplayValue = (_, stats) => stats?.GasPriceRegular is double value ? $"${value:F3}/gal" : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b, sa?.GasPriceRegular, sb?.GasPriceRegular, false,
                    "Gas price data is not available for comparison.",
                    "have the same average regular gas price",
                    "has cheaper regular gas than",
                    "USD")
            },
            new()
            {
                Slug = "gas-tax",
                Name = "Gas Tax",
                Description = "State gasoline excise tax in cents per gallon. Lower = lower state fuel tax burden.",
                GroupSlug = "quality-of-life",
                GroupName = "Quality of Life",
                GroupOrder = 3,
                SortOrder = 4,
                Unit = "cents-gal",
                Icon = "fa-solid fa-gas-pump",
                Type = MetricType.Numeric,
                HigherIsBetter = false,
                GetNumericValue = (_, stats) => stats?.GasTaxCents,
                GetDisplayValue = (_, stats) => stats?.GasTaxCents is double value ? $"{value:F2} c/gal" : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b, sa?.GasTaxCents, sb?.GasTaxCents, false,
                    "Gas tax data is not available for comparison.",
                    "have the same state gas tax rate",
                    "has a lower state gas tax than")
            },
            new()
            {
                Slug = "electricity-rates",
                Name = "Electricity Rates",
                Description = "Average residential electricity price in cents per kWh (EIA monthly state data, January 2026 preliminary).",
                GroupSlug = "quality-of-life",
                GroupName = "Quality of Life",
                GroupOrder = 3,
                SortOrder = 5,
                Unit = "cents-kwh",
                Icon = "fa-solid fa-bolt",
                Type = MetricType.Numeric,
                HigherIsBetter = false,
                GetNumericValue = (_, stats) => stats?.ElectricityRateCentsKwh,
                GetDisplayValue = (_, stats) => stats?.ElectricityRateCentsKwh is double value ? $"{value:F2} c/kWh" : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b, sa?.ElectricityRateCentsKwh, sb?.ElectricityRateCentsKwh, false,
                    "Electricity price data is not available for comparison.",
                    "have the same average residential electricity rate",
                    "has cheaper residential electricity than")
            },
            new()
            {
                Slug = "car-insurance",
                Name = "Car Insurance Cost",
                Description = "Average annual full coverage car insurance premium (WalletHub 2026).",
                GroupSlug = "quality-of-life",
                GroupName = "Quality of Life",
                GroupOrder = 3,
                SortOrder = 7,
                Unit = "USD",
                Icon = "fa-solid fa-car",
                Type = MetricType.Numeric,
                HigherIsBetter = false,
                GetNumericValue = (_, stats) => stats?.CarInsuranceFullCoverageAnnual.HasValue == true ? (double)stats.CarInsuranceFullCoverageAnnual.Value : null,
                GetDisplayValue = (_, stats) => stats?.CarInsuranceFullCoverageAnnual is int value ? $"${value:N0}/yr" : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b,
                    sa?.CarInsuranceFullCoverageAnnual.HasValue == true ? (double?)sa.CarInsuranceFullCoverageAnnual.Value : null,
                    sb?.CarInsuranceFullCoverageAnnual.HasValue == true ? (double?)sb.CarInsuranceFullCoverageAnnual.Value : null,
                    false,
                    "Car insurance data is not available for comparison.",
                    "have the same average full coverage car insurance premium",
                    "has cheaper average full coverage car insurance than",
                    "USD")
            },
            new()
            {
                Slug = "average-temperature",
                Name = "Average Temperature",
                Description = "Average annual statewide temperature in degrees Fahrenheit.",
                GroupSlug = "climate",
                GroupName = "Climate",
                GroupOrder = 4,
                SortOrder = 1,
                Unit = "temp-f",
                Icon = "fa-solid fa-temperature-half",
                Type = MetricType.Numeric,
                HigherIsBetter = true,
                GetNumericValue = (_, stats) => stats?.AverageTemperatureF,
                GetDisplayValue = (_, stats) => stats?.AverageTemperatureF is double value ? $"{value:F1}\u00B0F" : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b, sa?.AverageTemperatureF, sb?.AverageTemperatureF, true,
                    "Average temperature data is not available for comparison.",
                    "have the same average annual temperature",
                    "is warmer overall than")
            },
            new()
            {
                Slug = "summer-temperature",
                Name = "Summer Temperature",
                Description = "Average statewide summer temperature across June, July, and August.",
                GroupSlug = "climate",
                GroupName = "Climate",
                GroupOrder = 4,
                SortOrder = 2,
                Unit = "temp-f",
                Icon = "fa-solid fa-sun",
                Type = MetricType.Numeric,
                HigherIsBetter = true,
                GetNumericValue = (_, stats) => stats?.SummerTemperatureF,
                GetDisplayValue = (_, stats) => stats?.SummerTemperatureF is double value ? $"{value:F1}\u00B0F" : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b, sa?.SummerTemperatureF, sb?.SummerTemperatureF, true,
                    "Summer temperature data is not available for comparison.",
                    "have the same average summer temperature",
                    "has hotter summers than")
            },
            new()
            {
                Slug = "winter-temperature",
                Name = "Winter Temperature",
                Description = "Average statewide winter temperature across December, January, and February.",
                GroupSlug = "climate",
                GroupName = "Climate",
                GroupOrder = 4,
                SortOrder = 3,
                Unit = "temp-f",
                Icon = "fa-regular fa-snowflake",
                Type = MetricType.Numeric,
                HigherIsBetter = true,
                GetNumericValue = (_, stats) => stats?.WinterTemperatureF,
                GetDisplayValue = (_, stats) => stats?.WinterTemperatureF is double value ? $"{value:F1}\u00B0F" : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b, sa?.WinterTemperatureF, sb?.WinterTemperatureF, true,
                    "Winter temperature data is not available for comparison.",
                    "have the same average winter temperature",
                    "has milder winters than")
            },
            new()
            {
                Slug = "sunny-days",
                Name = "Sunny Days",
                Description = "Average number of sunny or mostly sunny days per year.",
                GroupSlug = "climate",
                GroupName = "Climate",
                GroupOrder = 4,
                SortOrder = 4,
                Unit = "days",
                Icon = "fa-solid fa-cloud-sun",
                Type = MetricType.Numeric,
                HigherIsBetter = true,
                GetNumericValue = (_, stats) => stats?.SunnyDaysPerYear,
                GetDisplayValue = (_, stats) => stats?.SunnyDaysPerYear is int value ? $"{value:N0} days" : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b, sa?.SunnyDaysPerYear, sb?.SunnyDaysPerYear, true,
                    "Sunny day data is not available for comparison.",
                    "get the same number of sunny days",
                    "gets more sunny days than")
            },
            new()
            {
                Slug = "annual-precipitation",
                Name = "Annual Precipitation",
                Description = "Average annual rain and snowfall combined, measured in inches.",
                GroupSlug = "climate",
                GroupName = "Climate",
                GroupOrder = 4,
                SortOrder = 5,
                Unit = "inches",
                Icon = "fa-solid fa-cloud-rain",
                Type = MetricType.Numeric,
                HigherIsBetter = false,
                GetNumericValue = (_, stats) => stats?.AnnualPrecipitationIn,
                GetDisplayValue = (_, stats) => stats?.AnnualPrecipitationIn is double value ? $"{value:F1} in" : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b, sa?.AnnualPrecipitationIn, sb?.AnnualPrecipitationIn, false,
                    "Annual precipitation data is not available for comparison.",
                    "get the same annual precipitation",
                    "is drier overall than")
            },
            new()
            {
                Slug = "home-value",
                Name = "Median Housing Value",
                Description = "Median residential home value in U.S. dollars.",
                GroupSlug = "housing",
                GroupName = "Housing",
                GroupOrder = 5,
                SortOrder = 1,
                Unit = "USD",
                Icon = "fa-solid fa-house",
                Type = MetricType.Numeric,
                HigherIsBetter = false,
                GetNumericValue = (_, stats) => stats?.MedianHomeValue,
                GetDisplayValue = (_, stats) => stats?.MedianHomeValue is int value ? $"${value:N0}" : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b, sa?.MedianHomeValue, sb?.MedianHomeValue, false,
                    "Home value data is not available for comparison.",
                    "have the same median home value",
                    "has lower median home values than",
                    "USD")
            },
            new()
            {
                Slug = "median-rent",
                Name = "Median Gross Rent",
                Description = "Median gross monthly rent in U.S. dollars.",
                GroupSlug = "housing",
                GroupName = "Housing",
                GroupOrder = 5,
                SortOrder = 2,
                Unit = "USD",
                Icon = "fa-solid fa-key",
                Type = MetricType.Numeric,
                HigherIsBetter = false,
                GetNumericValue = (_, stats) => stats?.MedianGrossRent,
                GetDisplayValue = (_, stats) => stats?.MedianGrossRent is int value ? $"${value:N0}/mo" : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b, sa?.MedianGrossRent, sb?.MedianGrossRent, false,
                    "Median rent data is not available for comparison.",
                    "have the same median gross rent",
                    "has lower median rent than",
                    "USD")
            },
            new()
            {
                Slug = "owner-costs-with-mortgage",
                Name = "Median Monthly Owner Costs With Mortgage",
                Description = "Median selected monthly owner costs for households with a mortgage.",
                GroupSlug = "housing",
                GroupName = "Housing",
                GroupOrder = 5,
                SortOrder = 3,
                Unit = "USD",
                Icon = "fa-solid fa-house-circle-check",
                Type = MetricType.Numeric,
                HigherIsBetter = false,
                GetNumericValue = (_, stats) => stats?.MedianOwnerCostsWithMortgage,
                GetDisplayValue = (_, stats) => stats?.MedianOwnerCostsWithMortgage is int value ? $"${value:N0}/mo" : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b, sa?.MedianOwnerCostsWithMortgage, sb?.MedianOwnerCostsWithMortgage, false,
                    "Owner costs with mortgage data is not available for comparison.",
                    "have the same owner costs with a mortgage",
                    "has lower owner costs with a mortgage than",
                    "USD")
            },
            new()
            {
                Slug = "owner-costs-without-mortgage",
                Name = "Owner Costs Without Mortgage",
                Description = "Median selected monthly owner costs for households without a mortgage.",
                GroupSlug = "housing",
                GroupName = "Housing",
                GroupOrder = 5,
                SortOrder = 4,
                Unit = "USD",
                Icon = "fa-solid fa-house-circle-exclamation",
                Type = MetricType.Numeric,
                HigherIsBetter = false,
                GetNumericValue = (_, stats) => stats?.MedianOwnerCostsWithoutMortgage,
                GetDisplayValue = (_, stats) => stats?.MedianOwnerCostsWithoutMortgage is int value ? $"${value:N0}/mo" : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b, sa?.MedianOwnerCostsWithoutMortgage, sb?.MedianOwnerCostsWithoutMortgage, false,
                    "Owner costs without mortgage data is not available for comparison.",
                    "have the same owner costs without a mortgage",
                    "has lower owner costs without a mortgage than",
                    "USD")
            },
            new()
            {
                Slug = "homeownership-rate",
                Name = "Homeownership Rate",
                Description = "Share of occupied housing units that are owner-occupied.",
                GroupSlug = "housing",
                GroupName = "Housing",
                GroupOrder = 5,
                SortOrder = 5,
                Unit = "%",
                Icon = "fa-solid fa-house-user",
                Type = MetricType.Numeric,
                HigherIsBetter = true,
                GetNumericValue = (_, stats) => stats?.HomeownershipRatePct,
                GetDisplayValue = (_, stats) => stats?.HomeownershipRatePct is double value ? $"{value:F1}%" : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b, sa?.HomeownershipRatePct, sb?.HomeownershipRatePct, true,
                    "Homeownership rate data is not available for comparison.",
                    "have the same homeownership rate",
                    "has a higher homeownership rate than")
            },
            new()
            {
                Slug = "home-value-to-income",
                Name = "Home Value to Income Ratio",
                Description = "Median home value divided by median household income.",
                GroupSlug = "housing",
                GroupName = "Housing",
                GroupOrder = 5,
                SortOrder = 6,
                Unit = "ratio",
                Icon = "fa-solid fa-chart-line",
                Type = MetricType.Numeric,
                HigherIsBetter = false,
                GetNumericValue = (_, stats) => stats?.HomeValueToIncomeRatio(),
                GetDisplayValue = (_, stats) => stats?.HomeValueToIncomeRatio() is double value ? $"{value:F2}x" : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b, sa?.HomeValueToIncomeRatio(), sb?.HomeValueToIncomeRatio(), false,
                    "Home value to income ratio is not available for comparison.",
                    "have the same home value to income ratio",
                    "has a lower home value to income ratio than")
            },
            new()
            {
                Slug = "rent-to-income",
                Name = "Rent to Income Ratio",
                Description = "Annualized median gross rent as a share of median household income.",
                GroupSlug = "housing",
                GroupName = "Housing",
                GroupOrder = 5,
                SortOrder = 7,
                Unit = "%",
                Icon = "fa-solid fa-percent",
                Type = MetricType.Numeric,
                HigherIsBetter = false,
                GetNumericValue = (_, stats) => stats?.RentToIncomeRatioPct(),
                GetDisplayValue = (_, stats) => stats?.RentToIncomeRatioPct() is double value ? $"{value:F1}%" : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b, sa?.RentToIncomeRatioPct(), sb?.RentToIncomeRatioPct(), false,
                    "Rent to income ratio is not available for comparison.",
                    "have the same rent to income ratio",
                    "has a lower rent to income ratio than")
            },
            new()
            {
                Slug = "owner-costs-to-income",
                Name = "Owner Costs to Income",
                Description = "Median selected monthly owner costs as a percentage of household income (ACS 2023).",
                GroupSlug = "housing",
                GroupName = "Housing",
                GroupOrder = 5,
                SortOrder = 8,
                Unit = "%",
                Icon = "fa-solid fa-percent",
                Type = MetricType.Numeric,
                HigherIsBetter = false,
                GetNumericValue = (_, stats) => stats?.OwnerCostsToIncomePct,
                GetDisplayValue = (_, stats) => stats?.OwnerCostsToIncomePct is double value ? $"{value:F1}%" : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b, sa?.OwnerCostsToIncomePct, sb?.OwnerCostsToIncomePct, false,
                    "Owner costs to income ratio is not available for comparison.",
                    "have the same owner costs to income ratio",
                    "has a lower owner costs to income ratio than")
            },
            new()
            {
                Slug = "home-insurance",
                Name = "Home Insurance Cost",
                Description = "Average annual home insurance premium for $250,000 in dwelling coverage (MoneyGeek 2026).",
                GroupSlug = "housing",
                GroupName = "Housing",
                GroupOrder = 5,
                SortOrder = 9,
                Unit = "USD",
                Icon = "fa-solid fa-house-flood-water",
                Type = MetricType.Numeric,
                HigherIsBetter = false,
                GetNumericValue = (_, stats) => stats?.HomeInsuranceAnnualPremium.HasValue == true ? (double)stats.HomeInsuranceAnnualPremium.Value : null,
                GetDisplayValue = (_, stats) => stats?.HomeInsuranceAnnualPremium is int value ? $"${value:N0}/yr" : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b,
                    sa?.HomeInsuranceAnnualPremium.HasValue == true ? (double?)sa.HomeInsuranceAnnualPremium.Value : null,
                    sb?.HomeInsuranceAnnualPremium.HasValue == true ? (double?)sb.HomeInsuranceAnnualPremium.Value : null,
                    false,
                    "Home insurance data is not available for comparison.",
                    "have the same average home insurance premium",
                    "has cheaper average home insurance than",
                    "USD")
            },
            new()
            {
                Slug = "retirement-score",
                Name = "Retirement Score",
                Description = "Composite score for comparing states for retirement, combining affordability, taxes, housing, health, safety, and winter climate.",
                GroupSlug = "retirement",
                GroupName = "Retirement",
                GroupOrder = 6,
                SortOrder = 1,
                Unit = "score",
                Icon = "fa-solid fa-person-cane",
                Type = MetricType.Numeric,
                HigherIsBetter = true,
                GetNumericValue = (_, stats) => stats?.RetirementScore(),
                GetDisplayValue = (_, stats) => stats?.RetirementScore() is double value ? $"{value:F1}" : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b, sa?.RetirementScore(), sb?.RetirementScore(), true,
                    "Retirement score data is not available for comparison.",
                    "have the same retirement score",
                    "scores higher for retirement")
            },
            new()
            {
                Slug = "income-tax",
                Name = "State Income Tax",
                Description = "Top marginal state income tax rate. 0% = no state income tax.",
                GroupSlug = "taxes",
                GroupName = "Taxes",
                GroupOrder = 7,
                SortOrder = 1,
                Unit = "%",
                Icon = "fa-solid fa-file-invoice-dollar",
                Type = MetricType.Numeric,
                HigherIsBetter = false,
                GetNumericValue = (_, stats) => stats?.IncomeTaxRatePct,
                GetDisplayValue = (_, stats) => stats?.IncomeTaxRatePct is double value ? (value == 0 ? "None (0%)" : $"{value:F2}%") : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b, sa?.IncomeTaxRatePct, sb?.IncomeTaxRatePct, false,
                    "Income tax data is not available for comparison.",
                    "have the same income tax rate",
                    "has a lower state income tax rate than")
            },
            new()
            {
                Slug = "sales-tax",
                Name = "State Sales Tax",
                Description = "State-level sales tax rate. 0% = no state sales tax (local taxes may apply).",
                GroupSlug = "taxes",
                GroupName = "Taxes",
                GroupOrder = 7,
                SortOrder = 2,
                Unit = "%",
                Icon = "fa-solid fa-receipt",
                Type = MetricType.Numeric,
                HigherIsBetter = false,
                GetNumericValue = (_, stats) => stats?.SalesTaxRatePct,
                GetDisplayValue = (_, stats) => stats?.SalesTaxRatePct is double value ? (value == 0 ? "None (0%)" : $"{value:F2}%") : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b, sa?.SalesTaxRatePct, sb?.SalesTaxRatePct, false,
                    "Sales tax data is not available for comparison.",
                    "have the same sales tax rate",
                    "has a lower state sales tax rate than")
            },
            new()
            {
                Slug = "property-tax",
                Name = "Property Tax",
                Description = "Effective real-estate property tax rate (% of home value, WalletHub February 17, 2026 using 2024 data).",
                GroupSlug = "taxes",
                GroupName = "Taxes",
                GroupOrder = 7,
                SortOrder = 3,
                Unit = "%",
                Icon = "fa-solid fa-house-circle-check",
                Type = MetricType.Numeric,
                HigherIsBetter = false,
                GetNumericValue = (_, stats) => stats?.PropertyTaxRatePct,
                GetDisplayValue = (_, stats) => stats?.PropertyTaxRatePct is double value ? $"{value:F2}%" : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b, sa?.PropertyTaxRatePct, sb?.PropertyTaxRatePct, false,
                    "Property tax data is not available for comparison.",
                    "have the same effective property tax rate",
                    "has a lower effective property tax rate than")
            },
            new()
            {
                Slug = "tax-burden",
                Name = "Total Tax Burden",
                Description = "Total state and local tax burden (property, income, and sales and excise taxes) as a percentage of personal income (WalletHub 2026).",
                GroupSlug = "taxes",
                GroupName = "Taxes",
                GroupOrder = 7,
                SortOrder = 4,
                Unit = "%",
                Icon = "fa-solid fa-scale-balanced",
                Type = MetricType.Numeric,
                HigherIsBetter = false,
                GetNumericValue = (_, stats) => stats?.TotalTaxBurdenPct,
                GetDisplayValue = (_, stats) => stats?.TotalTaxBurdenPct is double value ? $"{value:F2}%" : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b, sa?.TotalTaxBurdenPct, sb?.TotalTaxBurdenPct, false,
                    "Tax burden data is not available for comparison.",
                    "have the same total tax burden",
                    "has a lower total tax burden than")
            },
            new()
            {
                Slug = "political-lean",
                Name = "State Color",
                Description = "Simple political color label based on the 2024 presidential result and battleground status.",
                GroupSlug = "politics",
                GroupName = "Politics",
                GroupOrder = 7,
                SortOrder = 1,
                Unit = "",
                Icon = "fa-solid fa-landmark-dome",
                Type = MetricType.Text,
                HigherIsBetter = false,
                GetNumericValue = (_, _) => null,
                GetDisplayValue = (_, stats) =>
                {
                    if (stats?.SwingStateStatus == "Battleground")
                        return "Swing State";

                    if (stats?.PresidentialVotingMarginPct is double margin)
                        return margin >= 0 ? "Solid Blue" : "Solid Red";

                    return stats?.PoliticalLean;
                },
                GenerateSummary = (a, sa, b, sb) =>
                {
                    string? Normalize(StateStats? stats)
                    {
                        if (stats?.SwingStateStatus == "Battleground")
                            return "Swing State";

                        if (stats?.PresidentialVotingMarginPct is double margin)
                            return margin >= 0 ? "Solid Blue" : "Solid Red";

                        return stats?.PoliticalLean;
                    }

                    var leanA = Normalize(sa);
                    var leanB = Normalize(sb);
                    if (string.IsNullOrWhiteSpace(leanA) || string.IsNullOrWhiteSpace(leanB))
                        return "Political lean data is not available for comparison.";

                    return leanA == leanB
                        ? $"{a.Name} and {b.Name} both rate as {leanA} based on the 2024 presidential result."
                        : $"{a.Name} rates as {leanA}, while {b.Name} rates as {leanB}.";
                }
            },
            new()
            {
                Slug = "presidential-voting-margin",
                Name = "Presidential Voting Margin",
                Description = "Signed 2024 presidential margin in percentage points (positive = Democratic, negative = Republican).",
                GroupSlug = "politics",
                GroupName = "Politics",
                GroupOrder = 7,
                SortOrder = 2,
                Unit = "%",
                Icon = "fa-solid fa-check-to-slot",
                Type = MetricType.Numeric,
                HigherIsBetter = true,
                GetNumericValue = (_, stats) => stats?.PresidentialVotingMarginPct,
                GetDisplayValue = (_, stats) => stats?.PresidentialVotingMarginPct is double value
                    ? (value > 0 ? $"Dem +{value:F2}" : value < 0 ? $"Rep +{Math.Abs(value):F2}" : "Even")
                    : null,
                GenerateSummary = (a, sa, b, sb) =>
                {
                    var marginA = sa?.PresidentialVotingMarginPct;
                    var marginB = sb?.PresidentialVotingMarginPct;
                    if (!marginA.HasValue || !marginB.HasValue)
                        return "Presidential voting margin data is not available for comparison.";

                    string Format(double value) => value > 0 ? $"Dem +{value:F2}" : value < 0 ? $"Rep +{Math.Abs(value):F2}" : "Even";

                    if (marginA.Value == marginB.Value)
                        return $"{a.Name} and {b.Name} had the same 2024 presidential margin at {Format(marginA.Value)}.";

                    var bluer = marginA.Value > marginB.Value ? a : b;
                    var redder = bluer == a ? b : a;
                    var bluerMargin = bluer == a ? marginA.Value : marginB.Value;
                    var redderMargin = bluer == a ? marginB.Value : marginA.Value;
                    return $"In the 2024 presidential vote, {bluer.Name} came in bluer than {redder.Name}: {Format(bluerMargin)} vs {Format(redderMargin)}.";
                }
            },
            new()
            {
                Slug = "swing-state",
                Name = "Swing State / Battleground",
                Description = "Whether the state was part of the core 2024 battleground map.",
                GroupSlug = "politics",
                GroupName = "Politics",
                GroupOrder = 7,
                SortOrder = 3,
                Unit = "",
                Icon = "fa-solid fa-map-location-dot",
                Type = MetricType.Text,
                HigherIsBetter = false,
                GetNumericValue = (_, _) => null,
                GetDisplayValue = (_, stats) => stats?.SwingStateStatus,
                GenerateSummary = (a, sa, b, sb) =>
                {
                    var swingA = sa?.SwingStateStatus;
                    var swingB = sb?.SwingStateStatus;
                    if (string.IsNullOrWhiteSpace(swingA) || string.IsNullOrWhiteSpace(swingB))
                        return "Battleground-state data is not available for comparison.";

                    if (swingA == swingB)
                        return swingA == "Battleground"
                            ? $"{a.Name} and {b.Name} were both core battleground states in the 2024 presidential cycle."
                            : $"{a.Name} and {b.Name} were both outside the core 2024 battleground map.";

                    var battleground = swingA == "Battleground" ? a : b;
                    var nonBattleground = battleground == a ? b : a;
                    return $"{battleground.Name} was treated as a core battleground in 2024, while {nonBattleground.Name} was not.";
                }
            },
            new()
            {
                Slug = "governor-party",
                Name = "Governor Party",
                Description = "Current governor's party (NCSL partisan composition, January 27, 2026).",
                GroupSlug = "politics",
                GroupName = "Politics",
                GroupOrder = 7,
                SortOrder = 4,
                Unit = "",
                Icon = "fa-solid fa-user-tie",
                Type = MetricType.Text,
                HigherIsBetter = false,
                GetNumericValue = (_, _) => null,
                GetDisplayValue = (_, stats) => stats?.GovernorParty,
                GenerateSummary = (a, sa, b, sb) =>
                {
                    var partyA = sa?.GovernorParty;
                    var partyB = sb?.GovernorParty;
                    if (string.IsNullOrWhiteSpace(partyA) || string.IsNullOrWhiteSpace(partyB))
                        return "Governor party data is not available for comparison.";

                    return partyA == partyB
                        ? $"{a.Name} and {b.Name} both currently have {partyA.ToLowerInvariant()} governors."
                        : $"{a.Name} currently has a {partyA.ToLowerInvariant()} governor, while {b.Name} has a {partyB.ToLowerInvariant()} governor.";
                }
            },
            new()
            {
                Slug = "state-house-control",
                Name = "State House Control",
                Description = "Current partisan control of the lower legislative chamber (NCSL, January 27, 2026).",
                GroupSlug = "politics",
                GroupName = "Politics",
                GroupOrder = 7,
                SortOrder = 5,
                Unit = "",
                Icon = "fa-solid fa-people-roof",
                Type = MetricType.Text,
                HigherIsBetter = false,
                GetNumericValue = (_, _) => null,
                GetDisplayValue = (_, stats) => stats?.StateHouseControl,
                GenerateSummary = (a, sa, b, sb) =>
                {
                    var controlA = sa?.StateHouseControl;
                    var controlB = sb?.StateHouseControl;
                    if (string.IsNullOrWhiteSpace(controlA) || string.IsNullOrWhiteSpace(controlB))
                        return "State House control data is not available for comparison.";

                    return controlA == controlB
                        ? $"{a.Name} and {b.Name} both have {controlA.ToLowerInvariant()} lower chambers."
                        : $"{a.Name}'s lower chamber is {controlA.ToLowerInvariant()}, while {b.Name}'s is {controlB.ToLowerInvariant()}.";
                }
            },
            new()
            {
                Slug = "state-senate-control",
                Name = "State Senate Control",
                Description = "Current partisan control of the upper legislative chamber (NCSL, January 27, 2026).",
                GroupSlug = "politics",
                GroupName = "Politics",
                GroupOrder = 7,
                SortOrder = 6,
                Unit = "",
                Icon = "fa-solid fa-scale-balanced",
                Type = MetricType.Text,
                HigherIsBetter = false,
                GetNumericValue = (_, _) => null,
                GetDisplayValue = (_, stats) => stats?.StateSenateControl,
                GenerateSummary = (a, sa, b, sb) =>
                {
                    var controlA = sa?.StateSenateControl;
                    var controlB = sb?.StateSenateControl;
                    if (string.IsNullOrWhiteSpace(controlA) || string.IsNullOrWhiteSpace(controlB))
                        return "State Senate control data is not available for comparison.";

                    return controlA == controlB
                        ? $"{a.Name} and {b.Name} both have {controlA.ToLowerInvariant()} upper chambers."
                        : $"{a.Name}'s upper chamber is {controlA.ToLowerInvariant()}, while {b.Name}'s is {controlB.ToLowerInvariant()}.";
                }
            },
            new()
            {
                Slug = "trifecta",
                Name = "Trifecta",
                Description = "Whether one party controls the governorship and both legislative chambers (NCSL, January 27, 2026).",
                GroupSlug = "politics",
                GroupName = "Politics",
                GroupOrder = 7,
                SortOrder = 7,
                Unit = "",
                Icon = "fa-solid fa-sitemap",
                Type = MetricType.Text,
                HigherIsBetter = false,
                GetNumericValue = (_, _) => null,
                GetDisplayValue = (_, stats) => stats?.Trifecta,
                GenerateSummary = (a, sa, b, sb) =>
                {
                    var trifectaA = sa?.Trifecta;
                    var trifectaB = sb?.Trifecta;
                    if (string.IsNullOrWhiteSpace(trifectaA) || string.IsNullOrWhiteSpace(trifectaB))
                        return "Trifecta data is not available for comparison.";

                    return trifectaA == trifectaB
                        ? $"{a.Name} and {b.Name} both currently have {trifectaA.ToLowerInvariant()}."
                        : $"{a.Name} currently has {trifectaA.ToLowerInvariant()}, while {b.Name} has {trifectaB.ToLowerInvariant()}.";
                }
            },
            new()
            {
                Slug = "electoral-votes",
                Name = "Electoral Votes",
                Description = "Number of electoral votes in the Electoral College.",
                GroupSlug = "politics",
                GroupName = "Politics",
                GroupOrder = 7,
                SortOrder = 8,
                Unit = "votes",
                Icon = "fa-solid fa-landmark-flag",
                Type = MetricType.Numeric,
                HigherIsBetter = true,
                GetNumericValue = (_, stats) => stats?.ElectoralVotes.HasValue == true ? (double)stats.ElectoralVotes.Value : null,
                GetDisplayValue = (_, stats) => stats?.ElectoralVotes is int value ? value.ToString("N0") : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b,
                    sa?.ElectoralVotes.HasValue == true ? (double?)sa.ElectoralVotes.Value : null,
                    sb?.ElectoralVotes.HasValue == true ? (double?)sb.ElectoralVotes.Value : null,
                    true,
                    "Electoral vote data is not available for comparison.",
                    "have the same number of electoral votes",
                    "has more electoral votes than")
            },
            new()
            {
                Slug = "gun-laws-status",
                Name = "Gun Laws Status",
                Description = "Simplified statewide gun-law label based on current Giffords grades: A to B = Restrictive, C to F = Permissive (scorecard modified February 20, 2026).",
                GroupSlug = "laws",
                GroupName = "Laws",
                GroupOrder = 8,
                SortOrder = 1,
                Unit = "",
                Icon = "fa-solid fa-gun",
                Type = MetricType.Text,
                HigherIsBetter = false,
                GetNumericValue = (_, _) => null,
                GetDisplayValue = (_, stats) => stats?.GunLawsStatus,
                GenerateSummary = (a, sa, b, sb) =>
                {
                    var statusA = sa?.GunLawsStatus;
                    var statusB = sb?.GunLawsStatus;
                    if (string.IsNullOrWhiteSpace(statusA) || string.IsNullOrWhiteSpace(statusB))
                        return "Gun-law status data is not available for comparison.";

                    return statusA == statusB
                        ? $"{a.Name} and {b.Name} both currently fall into the {statusA.ToLowerInvariant()} category on this simplified gun-law scale."
                        : $"{a.Name} currently falls into the {statusA.ToLowerInvariant()} category, while {b.Name} falls into the {statusB.ToLowerInvariant()} category.";
                }
            },
            new()
            {
                Slug = "alcohol-laws",
                Name = "Alcohol Laws",
                Description = "Whether the state uses a control-state or license-state model for distilled-spirit sales (NABCA directory accessed April 6, 2026).",
                GroupSlug = "laws",
                GroupName = "Laws",
                GroupOrder = 8,
                SortOrder = 2,
                Unit = "",
                Icon = "fa-solid fa-wine-bottle",
                Type = MetricType.Text,
                HigherIsBetter = false,
                GetNumericValue = (_, _) => null,
                GetDisplayValue = (_, stats) => stats?.AlcoholLawsStatus,
                GenerateSummary = (a, sa, b, sb) =>
                {
                    var statusA = sa?.AlcoholLawsStatus;
                    var statusB = sb?.AlcoholLawsStatus;
                    if (string.IsNullOrWhiteSpace(statusA) || string.IsNullOrWhiteSpace(statusB))
                        return "Alcohol-law status data is not available for comparison.";

                    return statusA == statusB
                        ? $"{a.Name} and {b.Name} both use a {statusA.ToLowerInvariant()} system for distilled-spirit sales."
                        : $"{a.Name} uses a {statusA.ToLowerInvariant()} system for distilled-spirit sales, while {b.Name} uses a {statusB.ToLowerInvariant()} system.";
                }
            },
            new()
            {
                Slug = "marijuana-legalization",
                Name = "Marijuana Legalization",
                Description = "Current statewide marijuana status: Legal, Medical, or Illegal (NCSL state cannabis laws page updated June 27, 2025).",
                GroupSlug = "laws",
                GroupName = "Laws",
                GroupOrder = 8,
                SortOrder = 3,
                Unit = "",
                Icon = "fa-solid fa-cannabis",
                Type = MetricType.Text,
                HigherIsBetter = false,
                GetNumericValue = (_, _) => null,
                GetDisplayValue = (_, stats) => stats?.MarijuanaLegalizationStatus,
                GenerateSummary = (a, sa, b, sb) =>
                {
                    var statusA = sa?.MarijuanaLegalizationStatus;
                    var statusB = sb?.MarijuanaLegalizationStatus;
                    if (string.IsNullOrWhiteSpace(statusA) || string.IsNullOrWhiteSpace(statusB))
                        return "Marijuana legalization data is not available for comparison.";

                    return statusA == statusB
                        ? $"{a.Name} and {b.Name} both currently rate as {statusA.ToLowerInvariant()} for statewide marijuana laws."
                        : $"{a.Name} currently rates as {statusA.ToLowerInvariant()}, while {b.Name} rates as {statusB.ToLowerInvariant()} for statewide marijuana laws.";
                }
            },
            new()
            {
                Slug = "marriage-age",
                Name = "Minimum Marriage Age",
                Description = "Minimum marriage age with statutory exceptions. California has no statutory minimum age.",
                GroupSlug = "laws",
                GroupName = "Laws",
                GroupOrder = 8,
                SortOrder = 4,
                Unit = "years",
                Icon = "fa-solid fa-ring",
                Type = MetricType.Numeric,
                HigherIsBetter = true,
                GetNumericValue = (_, stats) => stats?.MarriageMinAge,
                GetDisplayValue = (_, stats) =>
                {
                    if (stats?.MarriageMinAge is null)
                        return null;

                    if (stats.MarriageMinAge.Value == 0)
                        return "No statutory minimum";

                    return string.IsNullOrWhiteSpace(stats.MarriageMinAgeLabel)
                        ? $"{stats.MarriageMinAge.Value}"
                        : stats.MarriageMinAgeLabel;
                },
                GenerateSummary = (a, sa, b, sb) =>
                {
                    var ageA = sa?.MarriageMinAge;
                    var ageB = sb?.MarriageMinAge;
                    if (ageA is null || ageB is null)
                        return "Marriage age data is not available for comparison.";

                    if (ageA.Value == ageB.Value)
                    {
                        var label = ageA.Value == 0
                            ? "no statutory minimum marriage age"
                            : $"a minimum marriage age of {ageA.Value}";
                        return $"{a.Name} and {b.Name} both have {label}.";
                    }

                    var higherState = ageA.Value > ageB.Value ? a : b;
                    var lowerState = higherState == a ? b : a;
                    return $"{higherState.Name} has a higher minimum marriage age than {lowerState.Name}.";
                }
            },
            new()
            {
                Slug = "gun-ownership",
                Name = "Gun Ownership",
                Description = "Share of adults who personally own a gun (%).",
                GroupSlug = "laws",
                GroupName = "Laws",
                GroupOrder = 8,
                SortOrder = 5,
                Unit = "%",
                Icon = "fa-solid fa-gun",
                Type = MetricType.Numeric,
                HigherIsBetter = true,
                GetNumericValue = (_, stats) => stats?.GunOwnershipPct,
                GetDisplayValue = (_, stats) => stats?.GunOwnershipPct is double v ? $"{v:F1}%" : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b, sa?.GunOwnershipPct, sb?.GunOwnershipPct, true,
                    "Gun ownership data is not available for comparison.",
                    "have the same share of adults who own guns",
                    "has a higher share of adults who own guns than")
            },
            new()
            {
                Slug = "land-area",
                Name = "Land Area",
                Description = "Total land area in square miles.",
                GroupSlug = "geography",
                GroupName = "Geography",
                GroupOrder = 9,
                SortOrder = 1,
                Unit = "sq mi",
                Icon = "fa-solid fa-map",
                Type = MetricType.Numeric,
                HigherIsBetter = true,
                GetNumericValue = (_, stats) => stats?.LandAreaSqMi,
                GetDisplayValue = (_, stats) => stats?.LandAreaSqMi is double value ? $"{value:N0} sq mi" : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b, sa?.LandAreaSqMi, sb?.LandAreaSqMi, true,
                    "Land area data is not available for comparison.",
                    "have the same land area",
                    "is larger than")
            },
            new()
            {
                Slug = "highest-point",
                Name = "Highest Point",
                Description = "Highest natural point in the state, with summit elevation.",
                GroupSlug = "geography",
                GroupName = "Geography",
                GroupOrder = 9,
                SortOrder = 2,
                Unit = "ft",
                Icon = "fa-solid fa-mountain",
                Type = MetricType.Numeric,
                HigherIsBetter = true,
                GetNumericValue = (_, stats) => stats?.HighestPointElevationFt,
                GetDisplayValue = (_, stats) => stats?.HighestPointElevationFt is double value
                    ? $"{stats.HighestPointName} ({value:N0} ft)"
                    : stats?.HighestPointName,
                GenerateSummary = (a, sa, b, sb) =>
                {
                    var elevationA = sa?.HighestPointElevationFt;
                    var elevationB = sb?.HighestPointElevationFt;

                    if (elevationA is null || elevationB is null)
                        return "Highest point data is not available for comparison.";

                    if (elevationA.Value == elevationB.Value)
                        return $"{a.Name} and {b.Name} reach the same highest elevation.";

                    var higherState = elevationA > elevationB ? a : b;
                    var lowerState = higherState == a ? b : a;
                    var higherStats = higherState == a ? sa : sb;
                    return $"{higherState.Name}'s highest point is {higherStats?.HighestPointName} at {Math.Max(elevationA.Value, elevationB.Value):N0} ft, higher than {lowerState.Name}.";
                }
            },
            new()
            {
                Slug = "statehood",
                Name = "Statehood",
                Description = "When the state was admitted to the Union and its admission order.",
                GroupSlug = "geography",
                GroupName = "Geography",
                GroupOrder = 9,
                SortOrder = 3,
                Unit = "",
                Icon = "fa-solid fa-landmark-flag",
                Type = MetricType.Date,
                HigherIsBetter = false,
                GetNumericValue = (_, stats) => stats?.StatehoodOrder,
                GetDisplayValue = (state, stats) => state.StateHoodDate.HasValue && stats?.StatehoodOrder.HasValue == true
                    ? $"{state.StateHoodDate.Value:MMMM d, yyyy} (#{stats.StatehoodOrder})"
                    : state.StateHoodDate?.ToString("MMMM d, yyyy"),
                GenerateSummary = (a, _, b, _) =>
                {
                    if (!a.StateHoodDate.HasValue || !b.StateHoodDate.HasValue) return "Statehood data is not available for comparison.";
                    if (a.StateHoodDate.Value == b.StateHoodDate.Value) return $"{a.Name} and {b.Name} were admitted to the Union on the same date.";
                    var earlier = a.StateHoodDate.Value < b.StateHoodDate.Value ? a : b;
                    var later = earlier == a ? b : a;
                    return $"{earlier.Name} became a state before {later.Name}.";
                }
            },
            new()
            {
                Slug = "capital",
                Name = "Capital City",
                Description = "The official state capital.",
                GroupSlug = "geography",
                GroupName = "Geography",
                GroupOrder = 9,
                SortOrder = 4,
                Unit = "",
                Icon = "fa-solid fa-building-columns",
                Type = MetricType.Text,
                HigherIsBetter = false,
                GetNumericValue = (_, _) => null,
                GetDisplayValue = (state, _) => state.Capital,
                GenerateSummary = (a, _, b, _) => $"{a.Name}'s capital is {a.Capital} and {b.Name}'s capital is {b.Capital}."
            },
            new()
            {
                Slug = "region",
                Name = "Region",
                Description = "U.S. Census Bureau geographic region.",
                GroupSlug = "geography",
                GroupName = "Geography",
                GroupOrder = 9,
                SortOrder = 5,
                Unit = "",
                Icon = "fa-solid fa-compass",
                Type = MetricType.Text,
                HigherIsBetter = false,
                GetNumericValue = (_, _) => null,
                GetDisplayValue = (state, _) => state.Region,
                GenerateSummary = (a, _, b, _) => a.Region == b.Region
                    ? $"{a.Name} and {b.Name} are both in the {a.Region} region."
                    : $"{a.Name} is in the {a.Region} region, while {b.Name} is in the {b.Region} region."
            },
            new()
            {
                Slug = "life-expectancy",
                Name = "Life Expectancy",
                Description = "Life expectancy at birth in years (CDC 2021).",
                GroupSlug = "health",
                GroupName = "Health",
                GroupOrder = 10,
                SortOrder = 1,
                Unit = "years",
                Icon = "fa-solid fa-heart-pulse",
                Type = MetricType.Numeric,
                HigherIsBetter = true,
                GetNumericValue = (_, stats) => stats?.LifeExpectancyYears,
                GetDisplayValue = (_, stats) => stats?.LifeExpectancyYears is double v ? $"{v:F1} yrs" : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b, sa?.LifeExpectancyYears, sb?.LifeExpectancyYears, true,
                    "Life expectancy data is not available for comparison.",
                    "have the same life expectancy",
                    "has a higher life expectancy than")
            },
            new()
            {
                Slug = "older-adult-health-outcomes",
                Name = "Older Adult Health Outcomes",
                Description = "Composite score for comparing state health outcomes for older adults, using life expectancy, insurance coverage, and obesity prevalence.",
                GroupSlug = "health",
                GroupName = "Health",
                GroupOrder = 10,
                SortOrder = 2,
                Unit = "score",
                Icon = "fa-solid fa-user-doctor",
                Type = MetricType.Numeric,
                HigherIsBetter = true,
                GetNumericValue = (_, stats) => stats?.OlderAdultHealthScore(),
                GetDisplayValue = (_, stats) => stats?.OlderAdultHealthScore() is double value ? $"{value:F1}" : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b, sa?.OlderAdultHealthScore(), sb?.OlderAdultHealthScore(), true,
                    "Older-adult health outcome data is not available for comparison.",
                    "have the same older-adult health outcome score",
                    "scores higher on older-adult health outcomes")
            },
            new()
            {
                Slug = "uninsured-rate",
                Name = "Uninsured Rate",
                Description = "Share of residents without health insurance coverage.",
                GroupSlug = "health",
                GroupName = "Health",
                GroupOrder = 10,
                SortOrder = 3,
                Unit = "%",
                Icon = "fa-solid fa-notes-medical",
                Type = MetricType.Numeric,
                HigherIsBetter = false,
                GetNumericValue = (_, stats) => stats?.UninsuredRatePct,
                GetDisplayValue = (_, stats) => stats?.UninsuredRatePct is double v ? $"{v:F1}%" : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b, sa?.UninsuredRatePct, sb?.UninsuredRatePct, false,
                    "Uninsured rate data is not available for comparison.",
                    "have the same uninsured rate",
                    "has a lower uninsured rate than")
            },
            new()
            {
                Slug = "obesity-rate",
                Name = "Obesity Rate",
                Description = "Adult obesity prevalence.",
                GroupSlug = "health",
                GroupName = "Health",
                GroupOrder = 10,
                SortOrder = 4,
                Unit = "%",
                Icon = "fa-solid fa-weight-scale",
                Type = MetricType.Numeric,
                HigherIsBetter = false,
                GetNumericValue = (_, stats) => stats?.ObesityRatePct,
                GetDisplayValue = (_, stats) => stats?.ObesityRatePct is double v ? $"{v:F1}%" : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b, sa?.ObesityRatePct, sb?.ObesityRatePct, false,
                    "Obesity rate data is not available for comparison.",
                    "have the same obesity rate",
                    "has a lower obesity rate than")
            },
            new()
            {
                Slug = "best-healthcare",
                Name = "Healthcare Quality Score",
                Description = "Composite healthcare score combining outcomes, cost, and access.",
                GroupSlug = "health",
                GroupName = "Health",
                GroupOrder = 10,
                SortOrder = 5,
                Unit = "score",
                Icon = "fa-solid fa-briefcase-medical",
                Type = MetricType.Numeric,
                HigherIsBetter = true,
                GetNumericValue = (_, stats) => stats?.HealthcareScore,
                GetDisplayValue = (_, stats) => stats?.HealthcareScore is double v ? $"{v:F1}" : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b, sa?.HealthcareScore, sb?.HealthcareScore, true,
                    "Healthcare quality data is not available for comparison.",
                    "have the same healthcare quality score",
                    "scores higher for healthcare quality")
            },
            new()
            {
                Slug = "violent-crime",
                Name = "Violent Crime Rate",
                Description = "Violent crime incidents per 100,000 residents (FBI UCR 2022).",
                GroupSlug = "safety",
                GroupName = "Safety",
                GroupOrder = 11,
                SortOrder = 1,
                Unit = "per 100k",
                Icon = "fa-solid fa-shield-halved",
                Type = MetricType.Numeric,
                HigherIsBetter = false,
                GetNumericValue = (_, stats) => stats?.ViolentCrimeRatePer100k,
                GetDisplayValue = (_, stats) => stats?.ViolentCrimeRatePer100k is double v ? $"{v:F1}" : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b, sa?.ViolentCrimeRatePer100k, sb?.ViolentCrimeRatePer100k, false,
                    "Violent crime data is not available for comparison.",
                    "have the same violent crime rate",
                    "has a lower violent crime rate than")
            },
            new()
            {
                Slug = "property-crime",
                Name = "Property Crime Rate",
                Description = "Property crime incidents per 100,000 residents.",
                GroupSlug = "safety",
                GroupName = "Safety",
                GroupOrder = 11,
                SortOrder = 2,
                Unit = "per 100k",
                Icon = "fa-solid fa-house-lock",
                Type = MetricType.Numeric,
                HigherIsBetter = false,
                GetNumericValue = (_, stats) => stats?.PropertyCrimeRatePer100k,
                GetDisplayValue = (_, stats) => stats?.PropertyCrimeRatePer100k is double v ? $"{v:F1}" : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b, sa?.PropertyCrimeRatePer100k, sb?.PropertyCrimeRatePer100k, false,
                    "Property crime data is not available for comparison.",
                    "have the same property crime rate",
                    "has a lower property crime rate than")
            },

            // ── Education (GroupOrder 12) ────────────────────────────────────────
            new()
            {
                Slug = "k12-rank",
                Name = "K-12 Education Rank",
                Description = "US News Best States K-12 education sub-ranking (1 = best, 50 = worst).",
                GroupSlug = "education",
                GroupName = "Education",
                GroupOrder = 12,
                SortOrder = 1,
                Unit = "",
                Icon = "fa-solid fa-graduation-cap",
                Type = MetricType.Numeric,
                HigherIsBetter = false,
                GetNumericValue = (_, stats) => stats?.K12Rank.HasValue == true ? (double)stats.K12Rank.Value : null,
                GetDisplayValue = (_, stats) => stats?.K12Rank is int r ? $"#{r}" : null,
                GenerateSummary = (a, sa, b, sb) =>
                {
                    if (sa?.K12Rank is null || sb?.K12Rank is null) return "K-12 ranking data is not available.";
                    if (sa.K12Rank == sb.K12Rank) return $"{a.Name} and {b.Name} share the same K-12 education rank.";
                    var better = sa.K12Rank < sb.K12Rank ? a : b;
                    var worse  = better == a ? b : a;
                    var rankBetter = better == a ? sa.K12Rank!.Value : sb.K12Rank!.Value;
                    var rankWorse  = better == a ? sb.K12Rank!.Value : sa.K12Rank!.Value;
                    return $"{better.Name} ranks #{rankBetter} for K-12 education, higher than {worse.Name} at #{rankWorse} (US News).";
                }
            },
            new()
            {
                Slug = "hs-graduation-rate",
                Name = "High School Graduation Rate",
                Description = "4-year adjusted cohort graduation rate for public high schools (NCES).",
                GroupSlug = "education",
                GroupName = "Education",
                GroupOrder = 12,
                SortOrder = 2,
                Unit = "%",
                Icon = "fa-solid fa-user-graduate",
                Type = MetricType.Numeric,
                HigherIsBetter = true,
                GetNumericValue = (_, stats) => stats?.HighSchoolGraduationPct,
                GetDisplayValue = (_, stats) => stats?.HighSchoolGraduationPct is double v ? $"{v:F1}%" : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b, sa?.HighSchoolGraduationPct, sb?.HighSchoolGraduationPct, true,
                    "High school graduation data is not available.",
                    "have the same high school graduation rate",
                    "has a higher high school graduation rate than")
            },
            new()
            {
                Slug = "student-teacher-ratio",
                Name = "Student-Teacher Ratio",
                Description = "Average number of pupils per teacher in public K-12 schools (NCES).",
                GroupSlug = "education",
                GroupName = "Education",
                GroupOrder = 12,
                SortOrder = 3,
                Unit = ":1",
                Icon = "fa-solid fa-chalkboard-teacher",
                Type = MetricType.Numeric,
                HigherIsBetter = false,
                GetNumericValue = (_, stats) => stats?.StudentTeacherRatio,
                GetDisplayValue = (_, stats) => stats?.StudentTeacherRatio is double v ? $"{v:F1}:1" : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b, sa?.StudentTeacherRatio, sb?.StudentTeacherRatio, false,
                    "Student-teacher ratio data is not available.",
                    "have the same student-teacher ratio",
                    "has a lower student-teacher ratio than")
            },
            new()
            {
                Slug = "school-spending",
                Name = "School Spending",
                Description = "Per-pupil K-12 education spending (World Population Review 2025).",
                GroupSlug = "education",
                GroupName = "Education",
                GroupOrder = 12,
                SortOrder = 4,
                Unit = "USD",
                Icon = "fa-solid fa-school",
                Type = MetricType.Numeric,
                HigherIsBetter = true,
                GetNumericValue = (_, stats) => stats?.PerPupilSpending.HasValue == true ? (double)stats.PerPupilSpending.Value : null,
                GetDisplayValue = (_, stats) => stats?.PerPupilSpending is int value ? $"${value:N0}" : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b,
                    sa?.PerPupilSpending.HasValue == true ? (double?)sa.PerPupilSpending.Value : null,
                    sb?.PerPupilSpending.HasValue == true ? (double?)sb.PerPupilSpending.Value : null,
                    true,
                    "School spending data is not available for comparison.",
                    "spend the same per pupil",
                    "spends more per pupil than",
                    "USD")
            },
            new()
            {
                Slug = "teacher-salary",
                Name = "Teacher Salary",
                Description = "Average public school teacher salary, 2024-25 (NEA).",
                GroupSlug = "education",
                GroupName = "Education",
                GroupOrder = 12,
                SortOrder = 5,
                Unit = "USD",
                Icon = "fa-solid fa-chalkboard-user",
                Type = MetricType.Numeric,
                HigherIsBetter = true,
                GetNumericValue = (_, stats) => stats?.TeacherAverageSalary.HasValue == true ? (double)stats.TeacherAverageSalary.Value : null,
                GetDisplayValue = (_, stats) => stats?.TeacherAverageSalary is int value ? $"${value:N0}" : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b,
                    sa?.TeacherAverageSalary.HasValue == true ? (double?)sa.TeacherAverageSalary.Value : null,
                    sb?.TeacherAverageSalary.HasValue == true ? (double?)sb.TeacherAverageSalary.Value : null,
                    true,
                    "Teacher salary data is not available for comparison.",
                    "pay teachers the same average salary",
                    "pays teachers a higher average salary than",
                    "USD")
            },
            new()
            {
                Slug = "public-school-rank",
                Name = "Public School Ranking",
                Description = "Public school system ranking, 1 = best.",
                GroupSlug = "education",
                GroupName = "Education",
                GroupOrder = 12,
                SortOrder = 6,
                Unit = "rank",
                Icon = "fa-solid fa-ranking-star",
                Type = MetricType.Numeric,
                HigherIsBetter = false,
                GetNumericValue = (_, stats) => stats?.PublicSchoolRank.HasValue == true ? (double)stats.PublicSchoolRank.Value : null,
                GetDisplayValue = (_, stats) => stats?.PublicSchoolRank is int r ? $"#{r}" : null,
                GenerateSummary = (a, sa, b, sb) =>
                {
                    if (sa?.PublicSchoolRank is null || sb?.PublicSchoolRank is null) return "Public school ranking data is not available.";
                    if (sa.PublicSchoolRank == sb.PublicSchoolRank) return $"{a.Name} and {b.Name} share the same public school rank.";
                    var better = sa.PublicSchoolRank < sb.PublicSchoolRank ? a : b;
                    var worse  = better == a ? b : a;
                    var rankBetter = better == a ? sa.PublicSchoolRank!.Value : sb.PublicSchoolRank!.Value;
                    var rankWorse  = better == a ? sb.PublicSchoolRank!.Value : sa.PublicSchoolRank!.Value;
                    return $"{better.Name} ranks #{rankBetter} for public schools, higher than {worse.Name} at #{rankWorse}.";
                }
            },
            new()
            {
                Slug = "student-loan-debt",
                Name = "Student Loan Debt",
                Description = "Average federal student loan debt per borrower.",
                GroupSlug = "education",
                GroupName = "Education",
                GroupOrder = 12,
                SortOrder = 7,
                Unit = "USD",
                Icon = "fa-solid fa-hand-holding-dollar",
                Type = MetricType.Numeric,
                HigherIsBetter = false,
                GetNumericValue = (_, stats) => stats?.StudentLoanDebtAvg.HasValue == true ? (double)stats.StudentLoanDebtAvg.Value : null,
                GetDisplayValue = (_, stats) => stats?.StudentLoanDebtAvg is int value ? $"${value:N0}" : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b,
                    sa?.StudentLoanDebtAvg.HasValue == true ? (double?)sa.StudentLoanDebtAvg.Value : null,
                    sb?.StudentLoanDebtAvg.HasValue == true ? (double?)sb.StudentLoanDebtAvg.Value : null,
                    false,
                    "Student loan debt data is not available for comparison.",
                    "carry the same average student loan debt",
                    "has lower average student loan debt than",
                    "USD")
            },
            new()
            {
                Slug = "college-graduation-rate",
                Name = "College Graduation Rate",
                Description = "4-year college graduation rate (%).",
                GroupSlug = "education",
                GroupName = "Education",
                GroupOrder = 12,
                SortOrder = 8,
                Unit = "%",
                Icon = "fa-solid fa-graduation-cap",
                Type = MetricType.Numeric,
                HigherIsBetter = true,
                GetNumericValue = (_, stats) => stats?.CollegeGraduationRate4yrPct,
                GetDisplayValue = (_, stats) => stats?.CollegeGraduationRate4yrPct is double v ? $"{v:F1}%" : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b, sa?.CollegeGraduationRate4yrPct, sb?.CollegeGraduationRate4yrPct, true,
                    "College graduation rate data is not available for comparison.",
                    "have the same 4-year college graduation rate",
                    "has a higher 4-year college graduation rate than")
            },

            // ── Natural Disaster Risk (GroupOrder 13) ───────────────────────────
            new()
            {
                Slug = "hurricane-risk",
                Name = "Hurricane Risk",
                Description = "Relative hurricane exposure based on FEMA National Risk Index (None / Low / Moderate / High / Very High).",
                GroupSlug = "disasters",
                GroupName = "Natural Disasters",
                GroupOrder = 13,
                SortOrder = 1,
                Unit = "",
                Icon = "fa-solid fa-wind",
                Type = MetricType.Text,
                HigherIsBetter = false,
                GetNumericValue = (_, stats) => stats?.HurricaneRisk switch {
                    "None" => 0, "Low" => 1, "Moderate" => 2, "High" => 3, "Very High" => 4, _ => null
                },
                GetDisplayValue = (_, stats) => stats?.HurricaneRisk,
                GenerateSummary = (a, sa, b, sb) =>
                {
                    var rA = sa?.HurricaneRisk; var rB = sb?.HurricaneRisk;
                    if (rA is null || rB is null) return "Hurricane risk data is not available.";
                    return rA == rB
                        ? $"{a.Name} and {b.Name} share the same hurricane risk level ({rA.ToLowerInvariant()})."
                        : $"{a.Name} has {rA.ToLowerInvariant()} hurricane risk, while {b.Name} has {rB.ToLowerInvariant()} hurricane risk.";
                }
            },
            new()
            {
                Slug = "tornado-risk",
                Name = "Tornado Risk",
                Description = "Relative tornado exposure based on FEMA National Risk Index (None / Low / Moderate / High / Very High).",
                GroupSlug = "disasters",
                GroupName = "Natural Disasters",
                GroupOrder = 13,
                SortOrder = 2,
                Unit = "",
                Icon = "fa-solid fa-tornado",
                Type = MetricType.Text,
                HigherIsBetter = false,
                GetNumericValue = (_, stats) => stats?.TornadoRisk switch {
                    "None" => 0, "Low" => 1, "Moderate" => 2, "High" => 3, "Very High" => 4, _ => null
                },
                GetDisplayValue = (_, stats) => stats?.TornadoRisk,
                GenerateSummary = (a, sa, b, sb) =>
                {
                    var rA = sa?.TornadoRisk; var rB = sb?.TornadoRisk;
                    if (rA is null || rB is null) return "Tornado risk data is not available.";
                    return rA == rB
                        ? $"{a.Name} and {b.Name} share the same tornado risk level ({rA.ToLowerInvariant()})."
                        : $"{a.Name} has {rA.ToLowerInvariant()} tornado risk, while {b.Name} has {rB.ToLowerInvariant()} tornado risk.";
                }
            },
            new()
            {
                Slug = "earthquake-risk",
                Name = "Earthquake Risk",
                Description = "Relative seismic exposure based on FEMA National Risk Index (None / Low / Moderate / High / Very High).",
                GroupSlug = "disasters",
                GroupName = "Natural Disasters",
                GroupOrder = 13,
                SortOrder = 3,
                Unit = "",
                Icon = "fa-solid fa-house-crack",
                Type = MetricType.Text,
                HigherIsBetter = false,
                GetNumericValue = (_, stats) => stats?.EarthquakeRisk switch {
                    "None" => 0, "Low" => 1, "Moderate" => 2, "High" => 3, "Very High" => 4, _ => null
                },
                GetDisplayValue = (_, stats) => stats?.EarthquakeRisk,
                GenerateSummary = (a, sa, b, sb) =>
                {
                    var rA = sa?.EarthquakeRisk; var rB = sb?.EarthquakeRisk;
                    if (rA is null || rB is null) return "Earthquake risk data is not available.";
                    return rA == rB
                        ? $"{a.Name} and {b.Name} share the same earthquake risk level ({rA.ToLowerInvariant()})."
                        : $"{a.Name} has {rA.ToLowerInvariant()} earthquake risk, while {b.Name} has {rB.ToLowerInvariant()} earthquake risk.";
                }
            },
            new()
            {
                Slug = "wildfire-risk",
                Name = "Wildfire Risk",
                Description = "Relative wildfire exposure based on FEMA National Risk Index (None / Low / Moderate / High / Very High).",
                GroupSlug = "disasters",
                GroupName = "Natural Disasters",
                GroupOrder = 13,
                SortOrder = 4,
                Unit = "",
                Icon = "fa-solid fa-fire",
                Type = MetricType.Text,
                HigherIsBetter = false,
                GetNumericValue = (_, stats) => stats?.WildfireRisk switch {
                    "None" => 0, "Low" => 1, "Moderate" => 2, "High" => 3, "Very High" => 4, _ => null
                },
                GetDisplayValue = (_, stats) => stats?.WildfireRisk,
                GenerateSummary = (a, sa, b, sb) =>
                {
                    var rA = sa?.WildfireRisk; var rB = sb?.WildfireRisk;
                    if (rA is null || rB is null) return "Wildfire risk data is not available.";
                    return rA == rB
                        ? $"{a.Name} and {b.Name} share the same wildfire risk level ({rA.ToLowerInvariant()})."
                        : $"{a.Name} has {rA.ToLowerInvariant()} wildfire risk, while {b.Name} has {rB.ToLowerInvariant()} wildfire risk.";
                }
            },

            // ── Culture (GroupOrder 14) ──────────────────────────────────────────
            new()
            {
                Slug = "aza-zoos",
                Name = "AZA-Accredited Zoos",
                Description = "Number of zoos, aquariums, and related facilities accredited by the Association of Zoos and Aquariums (AZA institution status, 2026).",
                GroupSlug = "culture",
                GroupName = "Culture",
                GroupOrder = 14,
                SortOrder = 1,
                Unit = "count",
                Icon = "fa-solid fa-paw",
                Type = MetricType.Numeric,
                HigherIsBetter = true,
                GetNumericValue = (_, stats) => stats?.AzaAccreditedZoos.HasValue == true ? (double)stats.AzaAccreditedZoos.Value : null,
                GetDisplayValue = (_, stats) => stats?.AzaAccreditedZoos is int value ? value.ToString("N0") : null,
                GenerateSummary = (a, sa, b, sb) => CompareStates(
                    a, b,
                    sa?.AzaAccreditedZoos.HasValue == true ? (double?)sa.AzaAccreditedZoos.Value : null,
                    sb?.AzaAccreditedZoos.HasValue == true ? (double?)sb.AzaAccreditedZoos.Value : null,
                    true,
                    "AZA-accredited zoo data is not available for comparison.",
                    "have the same number of AZA-accredited zoos",
                    "has more AZA-accredited zoos than")
            }
        };

        public static readonly IReadOnlyList<(string Slug, string Name)> GroupOrder = All
            .OrderBy(m => m.GroupOrder)
            .ThenBy(m => m.SortOrder)
            .Select(m => (m.GroupSlug, m.GroupName))
            .Distinct()
            .ToList();

        public static ComparisonMetricDefinition? GetBySlug(string slug) => All.FirstOrDefault(m => m.Slug == slug);

        private static string CompareStates(State a, State b, double? valueA, double? valueB, bool higherIsBetter, string unavailable, string tieText, string winnerText, string? unit = null)
        {
            if (!valueA.HasValue || !valueB.HasValue) return unavailable;
            if (valueA.Value == valueB.Value) return $"{a.Name} and {b.Name} {tieText}.";

            var winner = higherIsBetter
                ? (valueA.Value > valueB.Value ? a : b)
                : (valueA.Value < valueB.Value ? a : b);
            var loser = winner == a ? b : a;
            var diff = Math.Abs(valueA.Value - valueB.Value);

            return unit switch
            {
                "people" => $"{winner.Name} {winnerText} {loser.Name} by {diff:N0} people.",
                "USD" => $"{winner.Name} {winnerText} {loser.Name} by ${diff:N0}.",
                _ => $"{winner.Name} {winnerText} {loser.Name}."
            };
        }
    }
}
