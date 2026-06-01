namespace USASymbol.Models
{
    public class StateStats
    {
        public string Slug { get; set; } = string.Empty;

        // Tier 0 - base (already in State model, but useful here for calculations)
        public double? LandAreaSqMi { get; set; }
        public int? StatehoodOrder { get; set; }

        // Tier 1 - income & economy
        public int? MedianHouseholdIncome { get; set; }
        public double? CollegeEducatedPct { get; set; }
        public double? AdvancedDegreePct { get; set; }
        public double? RegionalPriceParity { get; set; }
        public double? PurchasingPower100 { get; set; }
        public double? PovertyRatePct { get; set; }
        public double? EmploymentPopulationRatioPct { get; set; }
        public double? UnemploymentRatePct { get; set; }
        public double? JobGrowthPct { get; set; }
        public double? MinimumWageHourly { get; set; }
        public int? MedianHomeValue { get; set; }
        public int? MedianGrossRent { get; set; }
        public int? MedianOwnerCostsWithMortgage { get; set; }
        public int? MedianOwnerCostsWithoutMortgage { get; set; }
        public double? OwnerCostsToIncomePct { get; set; }
        public double? HomeownershipRatePct { get; set; }

        // Tier 1 - cost of living & taxes
        /// <summary>Composite index where 100 = national average (MERIC/C2ER).</summary>
        public double? CostOfLivingIndex { get; set; }
        public double? GasPriceRegular { get; set; }
        public double? ElectricityRateCentsKwh { get; set; }

        /// <summary>Top marginal state income tax rate (%). 0 = no income tax.</summary>
        public double? IncomeTaxRatePct { get; set; }

        /// <summary>State-level sales tax rate (%). 0 = no sales tax.</summary>
        public double? SalesTaxRatePct { get; set; }

        /// <summary>Effective real-estate property tax rate (% of home value, WalletHub 2026 using 2024 data).</summary>
        public double? PropertyTaxRatePct { get; set; }

        /// <summary>State gasoline excise tax in cents per gallon.</summary>
        public double? GasTaxCents { get; set; }

        /// <summary>Signed 2024 presidential margin in percentage points (positive = Democratic, negative = Republican).</summary>
        public double? PresidentialVotingMarginPct { get; set; }

        /// <summary>Political lean derived from the 2024 presidential margin.</summary>
        public string? PoliticalLean { get; set; }

        /// <summary>Whether the state was treated as a core battleground in the 2024 presidential cycle.</summary>
        public string? SwingStateStatus { get; set; }

        /// <summary>Current governor's party.</summary>
        public string? GovernorParty { get; set; }

        /// <summary>Current control of the lower legislative chamber.</summary>
        public string? StateHouseControl { get; set; }

        /// <summary>Current control of the upper legislative chamber.</summary>
        public string? StateSenateControl { get; set; }

        /// <summary>Whether one party controls the governorship and legislature.</summary>
        public string? Trifecta { get; set; }

        /// <summary>Binary gun-law label derived from current Giffords state scorecard grades.</summary>
        public string? GunLawsStatus { get; set; }

        /// <summary>Whether distilled spirits are sold under a NABCA control-state model.</summary>
        public string? AlcoholLawsStatus { get; set; }

        /// <summary>Current statewide marijuana legalization status.</summary>
        public string? MarijuanaLegalizationStatus { get; set; }

        /// <summary>Age at which a person may marry without parental consent.</summary>
        public int? MarriageAgeWithoutConsent { get; set; }

        /// <summary>Minimum marriage age with exceptions; 0 means no statutory minimum is set.</summary>
        public int? MarriageMinAge { get; set; }

        /// <summary>Human-readable minimum marriage age label from the state statute summary.</summary>
        public string? MarriageMinAgeLabel { get; set; }

        // Tier 2 - quality of life / livability
        /// <summary>WalletHub Best States to Live In total score.</summary>
        public double? LivabilityScore { get; set; }

        public double? MeanCommuteMinutes { get; set; }

        public double? AverageTemperatureF { get; set; }
        public double? SummerTemperatureF { get; set; }
        public double? WinterTemperatureF { get; set; }
        public int? SunnyDaysPerYear { get; set; }
        public double? AnnualPrecipitationIn { get; set; }

        public string? HighestPointName { get; set; }
        public double? HighestPointElevationFt { get; set; }

        // Tier 3 - health & safety
        /// <summary>Life expectancy at birth in years (CDC 2021).</summary>
        public double? LifeExpectancyYears { get; set; }

        /// <summary>Share of residents without health insurance coverage.</summary>
        public double? UninsuredRatePct { get; set; }

        /// <summary>Adult obesity prevalence.</summary>
        public double? ObesityRatePct { get; set; }

        /// <summary>Violent crime incidents per 100,000 residents (FBI UCR 2022).</summary>
        public double? ViolentCrimeRatePer100k { get; set; }

        /// <summary>Property crime incidents per 100,000 residents.</summary>
        public double? PropertyCrimeRatePer100k { get; set; }

        // Education
        /// <summary>US News K-12 education ranking (1 = best, 50 = worst).</summary>
        public int? K12Rank { get; set; }

        /// <summary>4-year adjusted cohort high school graduation rate (NCES).</summary>
        public double? HighSchoolGraduationPct { get; set; }

        /// <summary>Pupils per teacher in public K-12 schools (NCES).</summary>
        public double? StudentTeacherRatio { get; set; }

        // Natural disaster risk (FEMA NRI composite: None / Low / Moderate / High / Very High)
        public string? HurricaneRisk { get; set; }
        public string? TornadoRisk { get; set; }
        public string? EarthquakeRisk { get; set; }
        public string? WildfireRisk { get; set; }

        // Calculated
        public double? PopulationDensity(int? population)
        {
            if (population.HasValue && LandAreaSqMi.HasValue && LandAreaSqMi.Value > 0)
                return Math.Round(population.Value / LandAreaSqMi.Value, 1);
            return null;
        }

        public double? EstimatedMonthlyHouseholdIncome()
        {
            if (!MedianHouseholdIncome.HasValue || MedianHouseholdIncome.Value <= 0)
                return null;

            return MedianHouseholdIncome.Value / 12.0;
        }

        public double? HomeValueToIncomeRatio()
        {
            if (!MedianHomeValue.HasValue || !MedianHouseholdIncome.HasValue || MedianHouseholdIncome.Value <= 0)
                return null;

            return Math.Round((double)MedianHomeValue.Value / MedianHouseholdIncome.Value, 2);
        }

        public double? RentToIncomeRatioPct()
        {
            if (!MedianGrossRent.HasValue || !MedianHouseholdIncome.HasValue || MedianHouseholdIncome.Value <= 0)
                return null;

            return Math.Round((MedianGrossRent.Value * 12.0 / MedianHouseholdIncome.Value) * 100, 1);
        }

        public double? OwnerCostsWithMortgageToIncomeRatioPct()
        {
            if (!MedianOwnerCostsWithMortgage.HasValue)
                return null;

            var monthlyIncome = EstimatedMonthlyHouseholdIncome();
            if (!monthlyIncome.HasValue || monthlyIncome.Value <= 0)
                return null;

            return Math.Round((MedianOwnerCostsWithMortgage.Value / monthlyIncome.Value) * 100, 1);
        }

        public double? OwnerCostsWithoutMortgageToIncomeRatioPct()
        {
            if (!MedianOwnerCostsWithoutMortgage.HasValue)
                return null;

            var monthlyIncome = EstimatedMonthlyHouseholdIncome();
            if (!monthlyIncome.HasValue || monthlyIncome.Value <= 0)
                return null;

            return Math.Round((MedianOwnerCostsWithoutMortgage.Value / monthlyIncome.Value) * 100, 1);
        }

        public double? IncomeAfterMedianRentMonthly()
        {
            if (!MedianGrossRent.HasValue)
                return null;

            var monthlyIncome = EstimatedMonthlyHouseholdIncome();
            if (!monthlyIncome.HasValue)
                return null;

            return Math.Round(monthlyIncome.Value - MedianGrossRent.Value, 0);
        }

        public double? BestStateToLiveInScore()
        {
            return LivabilityScore;
        }

        public double? OlderAdultHealthScore()
        {
            var components = new List<double>();

            if (LifeExpectancyYears.HasValue)
                components.Add(Scale(LifeExpectancyYears.Value, 70, 82, higherIsBetter: true));

            if (UninsuredRatePct.HasValue)
                components.Add(Scale(UninsuredRatePct.Value, 3, 17, higherIsBetter: false));

            if (ObesityRatePct.HasValue)
                components.Add(Scale(ObesityRatePct.Value, 24, 42, higherIsBetter: false));

            return AverageScore(components);
        }

        public double? RetirementScore()
        {
            var components = new List<double>();

            if (CostOfLivingIndex.HasValue)
                components.Add(Scale(CostOfLivingIndex.Value, 84, 187, higherIsBetter: false));

            if (IncomeTaxRatePct.HasValue)
                components.Add(Scale(IncomeTaxRatePct.Value, 0, 13.3, higherIsBetter: false));

            if (PropertyTaxRatePct.HasValue)
                components.Add(Scale(PropertyTaxRatePct.Value, 0.25, 2.25, higherIsBetter: false));

            if (MedianHomeValue.HasValue)
                components.Add(Scale(MedianHomeValue.Value, 125000, 815000, higherIsBetter: false));

            if (LifeExpectancyYears.HasValue)
                components.Add(Scale(LifeExpectancyYears.Value, 70, 82, higherIsBetter: true));

            if (ViolentCrimeRatePer100k.HasValue)
                components.Add(Scale(ViolentCrimeRatePer100k.Value, 175, 840, higherIsBetter: false));

            if (WinterTemperatureF.HasValue)
                components.Add(Scale(WinterTemperatureF.Value, 15, 68, higherIsBetter: true));

            return AverageScore(components);
        }

        private static double? AverageScore(List<double> components)
        {
            return components.Count == 0 ? null : Math.Round(components.Average(), 1);
        }

        private static double Scale(double value, double min, double max, bool higherIsBetter)
        {
            if (max <= min)
                return 0;

            var normalized = (value - min) / (max - min);
            normalized = Math.Max(0, Math.Min(1, normalized));
            if (!higherIsBetter)
                normalized = 1 - normalized;

            return normalized * 100;
        }
    }
}
