using USASymbol.Models;

namespace USASymbol.Services;

public sealed record ComparisonMetricSourceInfo(
    string Name,
    string Url,
    string DataPeriod,
    string ReviewedOn,
    string Note);

public static class ComparisonMetricSourceCatalog
{
    private const string ReviewedOn = "July 23, 2026";

    private static readonly HashSet<string> Acs2023Metrics = new(StringComparer.OrdinalIgnoreCase)
    {
        "college-educated", "advanced-degree", "median-income", "poverty-rate",
        "employment-population-ratio", "commute-time", "home-value", "median-rent",
        "homeownership-rate", "home-value-to-income", "rent-to-income",
        "owner-costs-to-income", "uninsured-rate"
    };

    private static readonly HashSet<string> NoaaClimateMetrics = new(StringComparer.OrdinalIgnoreCase)
    {
        "average-temperature", "summer-temperature", "winter-temperature",
        "sunny-days", "annual-precipitation"
    };

    private static readonly HashSet<string> NcslPoliticsMetrics = new(StringComparer.OrdinalIgnoreCase)
    {
        "governor-party", "state-house-control", "state-senate-control", "trifecta"
    };

    private static readonly HashSet<string> FemaRiskMetrics = new(StringComparer.OrdinalIgnoreCase)
    {
        "hurricane-risk", "tornado-risk", "earthquake-risk", "wildfire-risk"
    };

    public static ComparisonMetricSourceInfo Get(ComparisonMetricDefinition metric)
    {
        var slug = metric.Slug;

        if (Acs2023Metrics.Contains(slug))
        {
            return Source(
                "U.S. Census Bureau — 2023 American Community Survey",
                "https://www.census.gov/programs-surveys/acs/data.html",
                "ACS 2023 estimates",
                "State values or ratios calculated from ACS fields.");
        }

        if (NoaaClimateMetrics.Contains(slug))
        {
            return Source(
                "NOAA National Centers for Environmental Information — U.S. Climate Normals",
                "https://www.ncei.noaa.gov/products/land-based-station/us-climate-normals",
                "1991–2020 climate normals",
                "Statewide climate summaries; normals describe long-term conditions, not a forecast.");
        }

        if (NcslPoliticsMetrics.Contains(slug))
        {
            return Source(
                "National Conference of State Legislatures — State Partisan Composition",
                "https://www.ncsl.org/about-state-legislatures/state-partisan-composition",
                "January 27, 2026",
                "Current officeholder and chamber-control fields can change after elections or vacancies.");
        }

        if (FemaRiskMetrics.Contains(slug))
        {
            return Source(
                "FEMA — National Risk Index",
                "https://hazards.fema.gov/nri/",
                "Current National Risk Index release",
                "Relative state exposure labels summarized from FEMA hazard-risk data.");
        }

        return slug switch
        {
            "population" => Source(
                "U.S. Census Bureau — State Population Estimates",
                "https://www.census.gov/data/tables/time-series/demo/popest/2020s-state-total.html",
                "2025 estimate",
                "Latest annual state resident population copied from the maintained ranking."),
            "population-growth" => Source(
                "U.S. Census Bureau — 2020 Census and 2025 Population Estimates",
                "https://www.census.gov/data/tables/time-series/demo/popest/2020s-state-total.html",
                "2020 to 2025",
                "Percentage change from the 2020 Census baseline to the 2025 estimate."),
            "domestic-migration" => Source(
                "U.S. Census Bureau — State Population Totals and Components of Change",
                "https://www.census.gov/data/tables/time-series/demo/popest/2020s-state-total.html",
                "July 1, 2024 to July 1, 2025",
                "Net moves between U.S. states; international migration and natural change are excluded."),
            "density" => Source(
                "U.S. Census Bureau — 2020 Census and TIGER/Line",
                "https://www.census.gov/geographies/mapping-files/time-series/geo/tiger-line-file.html",
                "2020 population and land area",
                "Calculated as population divided by land area."),
            "unemployment-rate" => Source(
                "U.S. Bureau of Labor Statistics — Local Area Unemployment Statistics",
                "https://www.bls.gov/lau/",
                "December 2025",
                "Seasonally adjusted state unemployment rate."),
            "job-growth" => Source(
                "U.S. Bureau of Labor Statistics — State and Metro Area Employment",
                "https://www.bls.gov/sae/",
                "December 2024 to December 2025",
                "Percentage change in total nonfarm payroll employment."),
            "single-person-living-wage" => Source(
                "USA Symbol — Single Person Living Wage by State",
                "/rankings/economy/single-person-living-wage",
                "2025",
                "Annual income estimate for one adult, copied from the maintained ranking table."),
            "average-credit-score" => Source(
                "USA Symbol — Average Credit Score by State",
                "/rankings/economy/average-credit-score-by-state",
                "2025",
                "Average score copied from the maintained ranking table."),
            "childcare-costs" => Source(
                "USA Symbol — Average Childcare Cost by State",
                "/rankings/economy/average-childcare-costs",
                "2026 maintained table",
                "Annual infant-care cost copied from the maintained 50-state childcare table."),
            "livability-score" => Source(
                "WalletHub — Best States to Live In",
                "https://wallethub.com/edu/best-states-to-live-in/62617",
                "2025 comparison",
                "WalletHub total score incorporated into the comparison dataset."),
            "cost-of-living" => Source(
                "Missouri Economic Research and Information Center — Cost of Living Index",
                "https://meric.mo.gov/data/cost-living-data-series",
                "2026 incorporated quarterly release",
                "Composite index where 100 equals the national average."),
            "regional-price-parity" or "purchasing-power" => Source(
                "U.S. Bureau of Economic Analysis — Regional Price Parities",
                "https://www.bea.gov/data/prices-inflation/regional-price-parities-state-and-metro-area",
                "Latest incorporated BEA release",
                slug == "purchasing-power"
                    ? "Derived as 100 divided by the state price parity, multiplied by $100."
                    : "A value above 100 indicates prices above the national average."),
            "minimum-wage" => Source(
                "U.S. Department of Labor — State Minimum Wage Laws",
                "https://www.dol.gov/agencies/whd/minimum-wage/state",
                "January 1, 2026",
                "Statewide base minimum; local and occupation-specific rules may differ."),
            "unemployment-benefit" => Source(
                "Indeed Flex — Unemployment Benefits by State",
                "https://indeedflex.com/blog/unemployment-benefits-by-state/",
                "2026 dataset",
                "Maximum weekly benefit; eligibility and duration vary."),
            "gas-price" => Source(
                "AAA — State Gas Price Averages",
                "https://gasprices.aaa.com/",
                "Snapshot stored in the comparison dataset",
                "Gas prices change daily; the displayed value is not a live quote."),
            "electricity-rates" => Source(
                "U.S. Energy Information Administration — Electric Power Monthly",
                "https://www.eia.gov/electricity/monthly/",
                "January 2026 preliminary",
                "Average residential retail price by state."),
            "car-insurance" => Source(
                "WalletHub — Car Insurance by State",
                "https://wallethub.com/edu/ci/states-with-cheapest-car-insurance/14227",
                "2026 comparison",
                "Illustrative full-coverage premium; individual quotes vary."),
            "home-insurance" => Source(
                "MoneyGeek — Home Insurance by State",
                "https://www.moneygeek.com/insurance/homeowners/average-cost-home-insurance/",
                "2026 comparison",
                "Illustrative annual premium for the stated dwelling coverage; actual quotes vary."),
            "owner-costs-with-mortgage" or "owner-costs-without-mortgage" => Source(
                "U.S. Census Bureau — 2024 American Community Survey, table B25088",
                "https://data.census.gov/table/ACSDT1Y2024.B25088",
                "ACS 2024 1-year estimates",
                "Median selected monthly owner costs for owner-occupied housing units."),
            "gas-tax" => Source(
                "U.S. Energy Information Administration — State Motor Fuel Taxes",
                "https://www.eia.gov/petroleum/marketing/monthly/",
                "2026 incorporated table",
                "State gasoline excise tax in cents per gallon; local and environmental fees may differ."),
            "income-tax" => Source(
                "Tax Foundation — State Individual Income Tax Rates and Brackets",
                "https://taxfoundation.org/data/all/state/state-income-tax-rates/",
                "2026 comparison",
                "Top marginal state rate; brackets, deductions, local taxes, and effective rates differ."),
            "sales-tax" => Source(
                "Tax Foundation — State and Local Sales Tax Rates",
                "https://taxfoundation.org/data/all/state/state-and-local-sales-tax-rates/",
                "2026 comparison",
                "Statewide base sales tax rate; local additions and taxable bases differ."),
            "property-tax" or "tax-burden" => Source(
                "WalletHub — State tax comparisons",
                "https://wallethub.com/edu/states-with-the-highest-and-lowest-property-taxes/11585",
                slug == "property-tax" ? "Published February 17, 2026; underlying 2024 data" : "2026 comparison",
                "Comparison estimate; tax bills vary by income, property, locality, and deductions."),
            "grocery-tax" => Source(
                "USA Symbol — Grocery Tax by State",
                "/rankings/taxes/grocery-tax-by-state",
                "January 1, 2026",
                "State rate on ordinary groceries; local taxes and prepared-food rules can differ."),
            "death-tax" => Source(
                "Tax Foundation — Estate and Inheritance Taxes by State",
                "https://taxfoundation.org/data/all/state/estate-inheritance-taxes/",
                "2025",
                "State-level estate and inheritance tax status; exemptions and rates vary."),
            "vehicle-property-tax" => Source(
                "USA Symbol — Vehicle Property Tax by State",
                "/rankings/taxes/vehicle-property-tax-by-state",
                "Current incorporated ranking",
                "Recurring value-based vehicle tax status; registration and local fees may still apply."),
            "political-lean" or "presidential-voting-margin" or "swing-state" => Source(
                "Federal Election Commission — 2024 Presidential Election Results",
                "https://www.fec.gov/introduction-campaign-finance/election-results-and-voting-information/",
                "2024 general election",
                "Political labels are derived from the statewide presidential result and battleground classification."),
            "electoral-votes" => Source(
                "National Archives — Electoral College",
                "https://www.archives.gov/electoral-college/allocation",
                "2024–2028 allocation",
                "Allocation based on the 2020 Census apportionment."),
            "gun-laws-status" => Source(
                "Giffords Law Center — Annual Gun Law Scorecard",
                "https://giffords.org/lawcenter/resources/scorecard/",
                "Scorecard reviewed February 20, 2026",
                "USA Symbol groups A–B grades as Restrictive and C–F grades as Permissive."),
            "alcohol-laws" => Source(
                "National Alcohol Beverage Control Association",
                "https://www.nabca.org/control-state-directory-and-info",
                "Directory accessed April 6, 2026",
                "Statewide control-state or license-state model for distilled spirits."),
            "marijuana-legalization" => Source(
                "National Conference of State Legislatures — Cannabis Laws",
                "https://www.ncsl.org/health/state-medical-cannabis-laws",
                "Page updated June 27, 2025",
                "Simplified statewide status; possession, home-grow, and retail rules differ."),
            "abortion-laws" => Source(
                "USA Symbol — Abortion Laws by State",
                "/rankings/law/abortion-laws-by-state",
                "2026 review",
                "Simplified policy status only; laws and court orders can change quickly."),
            "right-to-work" => Source(
                "National Conference of State Legislatures — Right-to-Work Resources",
                "https://www.ncsl.org/labor-and-employment/right-to-work-resources",
                "Checked June 22, 2026",
                "Right-to-work status is distinct from at-will employment."),
            "marriage-age" => Source(
                "USA Symbol — Marriage Age by State",
                "/rankings/law/marriage-age-by-state",
                "State statutes reviewed January 2026",
                "Statewide minimum-age summary; exceptions and court procedures can change."),
            "gun-ownership" => Source(
                "RAND Corporation — State Firearm Ownership Estimates",
                "https://www.rand.org/pubs/tools/TLA243-2-v2.html",
                "Latest incorporated survey estimates",
                "Survey-derived adult ownership estimate; it is not a firearm registration count."),
            "land-area" or "region" => Source(
                "U.S. Census Bureau — Geographic reference files",
                "https://www.census.gov/geographies/reference-files.html",
                "Current incorporated reference data",
                slug == "land-area" ? "Land area in square miles." : "Census Bureau region classification."),
            "highest-point" => Source(
                "U.S. Geological Survey — Elevations and Distances",
                "https://www.usgs.gov/educational-resources/elevations-and-distances",
                "Reference data",
                "Highest named natural point and summit elevation."),
            "average-wind-speed" => Source(
                "USA Symbol — Windiest States in the U.S.",
                "/rankings/geography/windiest-states-in-the-us",
                "Current incorporated 50-state table",
                "Average statewide wind speed in miles per hour."),
            "lightning-density" => Source(
                "Vaisala — U.S. Lightning Data",
                "https://www.vaisala.com/en/digital-and-data-services/lightning",
                "2015–2019 state averages",
                "Average lightning events per square mile; this historical climate measure is not a forecast."),
            "statehood" or "capital" => Source(
                "National Archives and state government references",
                "https://www.archives.gov/",
                "Historical reference data",
                "Historical or civic reference field."),
            "life-expectancy" => Source(
                "CDC National Center for Health Statistics — State Life Expectancy",
                "https://www.cdc.gov/nchs/pressroom/sosmap/life_expectancy/life_expectancy.htm",
                "2021",
                "Life expectancy at birth."),
            "infant-mortality" => Source(
                "CDC National Center for Health Statistics — Infant Mortality",
                "https://www.cdc.gov/nchs/state-stats/deaths/infant-mortality.html",
                "2024",
                "Infant deaths per 1,000 live births."),
            "maternal-mortality" => Source(
                "USA Symbol — Maternal Mortality Rate by State",
                "/rankings/health/maternal-mortality-rate-by-state",
                "Latest incorporated CDC release",
                "Rates can be unstable in states with small numbers of births."),
            "overdose-death-rate" => Source(
                "USA Symbol — Drug Overdose Death Rate by State",
                "/rankings/health/drug-overdose-death-rate-by-state",
                "Latest incorporated CDC release",
                "Age-adjusted or reported rate as documented in the maintained ranking."),
            "obesity-rate" => Source(
                "CDC — Behavioral Risk Factor Surveillance System",
                "https://www.cdc.gov/brfss/",
                "Latest incorporated annual release",
                "Adult self-reported obesity prevalence."),
            "violent-crime" or "property-crime" => Source(
                "FBI — Crime Data Explorer",
                "https://cde.ucr.cjis.gov/",
                slug == "violent-crime" ? "2022 UCR data" : "Latest incorporated UCR data",
                "Reported offenses per 100,000 residents; reporting coverage can vary."),
            "hs-graduation-rate" or "student-teacher-ratio" or "college-graduation-rate" => Source(
                "National Center for Education Statistics",
                "https://nces.ed.gov/",
                "Latest incorporated NCES release",
                "State education statistic as defined in the metric description."),
            "teacher-salary" => Source(
                "National Education Association — Rankings and Estimates",
                "https://www.nea.org/resource-library/educator-pay-and-student-spending-how-does-your-state-rank",
                "2024–25",
                "Average public school teacher salary."),
            "k12-rank" => Source(
                "U.S. News & World Report — Best States for K-12 Education",
                "https://www.usnews.com/news/best-states/rankings/education/k-12-education",
                "2026 incorporated ranking",
                "Rank 1 is best; the ranking combines achievement and school-quality indicators."),
            "school-spending" => Source(
                "World Population Review — Per Pupil Spending by State",
                "https://worldpopulationreview.com/state-rankings/per-pupil-spending-by-state",
                "2025",
                "Nominal annual K-12 spending per student."),
            "public-school-rank" => Source(
                "World Population Review — Public School Rankings by State",
                "https://worldpopulationreview.com/state-rankings/public-school-rankings-by-state",
                "2025 edition",
                "Composite public-school rank; rank 1 is best."),
            "student-loan-debt" => Source(
                "Federal Student Aid — Data Center",
                "https://studentaid.gov/data-center",
                "Latest incorporated release",
                "Average federal student loan balance per borrower."),
            "aza-zoos" => Source(
                "Association of Zoos and Aquariums — Accredited Institutions",
                "https://www.aza.org/find-a-zoo-or-aquarium",
                "2026 institution status",
                "Count of AZA-accredited facilities assigned to each state."),
            "casinos" => Source(
                "USA Symbol — Casinos by State",
                "/rankings/culture/casinos-by-state",
                "Current incorporated ranking",
                "Casino count and best-known example copied from the maintained culture table."),
            "ufo-sightings" => Source(
                "USA Symbol — UFO Sightings by State",
                "/rankings/culture/states-by-ufo-sightings",
                "Current incorporated ranking",
                "Reported sightings are not verified events; rate is normalized per 100,000 residents."),
            "screen-time" => Source(
                "USA Symbol — Screen Time by State",
                "/rankings/culture/screen-time-by-state",
                "Current incorporated ranking",
                "Average daily estimate; survey definitions and device coverage can vary."),
            "most-popular-car" => Source(
                "USA Symbol — Most Popular Car by State",
                "/rankings/culture/most-popular-car-by-state",
                "Current incorporated ranking",
                "Most popular brand/model as defined by the source ranking."),
            "water-quality" => Source(
                "USA Symbol — Water Quality by State",
                "/rankings/infrastructure/water-quality-by-state",
                "2026 ranking",
                "TapWaterData.com composite score; it is not an EPA regulatory compliance grade."),
            "power-outages" => Source(
                "USA Symbol — Power Outages by State",
                "/rankings/infrastructure/power-outages-by-state",
                "Latest incorporated EIA reliability data",
                "Annual outage duration includes major event days."),
            "road-quality" => Source(
                "USA Symbol — Road Quality by State",
                "/rankings/infrastructure/road-quality-by-state",
                "Latest incorporated ranking",
                "Rank combines the road-condition fields documented in the ranking methodology."),
            "renewable-electricity" => Source(
                "USA Symbol — Renewable Energy by State",
                "/rankings/infrastructure/renewable-energy-by-state",
                "Latest incorporated EIA data",
                "Share of in-state electricity generation, not total energy consumption."),
            "largest-airport" => Source(
                "USA Symbol — Largest Airport by State",
                "/rankings/infrastructure/largest-airport-by-state",
                "Latest incorporated annual passenger data",
                "Largest commercial airport assigned by passenger volume."),
            "best-state-to-live-in" or "older-adult-health-outcomes" or "best-healthcare" or "retirement-score" => Source(
                "USA Symbol composite model",
                "/editorial-policy",
                "Inputs shown on this comparison page",
                "Derived score built from the component metrics described on the page; it is not a government statistic."),
            _ => Source(
                "USA Symbol comparison dataset",
                "/editorial-policy",
                "Latest incorporated source release",
                "Compiled comparison field. See the metric definition and editorial policy; values should be verified for legal, tax, or financial decisions.")
        };
    }

    private static ComparisonMetricSourceInfo Source(string name, string url, string period, string note) =>
        new(name, url, period, ReviewedOn, note);
}
