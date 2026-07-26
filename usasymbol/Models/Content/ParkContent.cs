using USASymbol.Models.ViewModels;

namespace USASymbol.Models.Content
{
    public class ParkContent
    {
        public string Slug { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string NpsCode { get; set; } = string.Empty;

        public string? SeoTitle { get; set; }
        public string? SeoDescription { get; set; }
        public string? IntroText { get; set; }

        public string? Author { get; set; }
        public int? EstablishedYear { get; set; }
        public DateTime? DatePublished { get; set; }
        public DateTime? DateModified { get; set; }
        public DateTime LastModified { get; set; }

        public ParkLocation Location { get; set; } = new();
        public ParkMap Map { get; set; } = new();

        public List<QuickFactItem> QuickFacts { get; set; } = new();
        public List<ParkHighlightStat> HighlightStats { get; set; } = new();
        public List<ParkAttractionItem> BestThingsToSeeItems { get; set; } = new();
        public ParkFilters Filters { get; set; } = new();
        public ParkMedia Media { get; set; } = new();
        public ParkStats Stats { get; set; } = new();

        // Optional per-section accent images
        public string? SectionImageHiking { get; set; }
        public string? SectionImageHistory { get; set; }
        public string? SectionImageWildlife { get; set; }
        public string? SectionImageCamping { get; set; }

        // Structured section data (optional, fallback to text sections)
        public List<ParkTrail> HikingTrails { get; set; } = new();
        public List<ParkSeason> Seasons { get; set; } = new();
        public List<ParkCampground> Campgrounds { get; set; } = new();
        public List<ParkFeeItem> Fees { get; set; } = new();

        // Optional sections — only keys present in YAML are populated
        public string? SectionOverview { get; set; }
        public string? SectionKnownFor { get; set; }
        public string? SectionBestThingsToSee { get; set; }
        public string? SectionBestTimeToVisit { get; set; }
        public string? SectionHiking { get; set; }
        public string? SectionCamping { get; set; }
        public string? SectionFeesReservations { get; set; }
        public string? SectionGettingThere { get; set; }
        public string? SectionGeology { get; set; }
        public string? SectionWildlife { get; set; }
        public string? SectionHistory { get; set; }

        public ParkRankings Rankings { get; set; } = new();

        public List<ParkFaq> Faq { get; set; } = new();
        public List<ParkSource> Sources { get; set; } = new();
    }

    public class ParkLocation
    {
        public string State { get; set; } = string.Empty;
        public string StateCode { get; set; } = string.Empty;
        public List<string> States { get; set; } = new();
        public List<string> StateCodes { get; set; } = new();
        public string Region { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string NearestCity { get; set; } = string.Empty;
        public string NearestMajorAirport { get; set; } = string.Empty;
    }

    public class ParkMap
    {
        public int Zoom { get; set; } = 10;
        public string GoogleSearchUrl { get; set; } = string.Empty;
        public string GoogleDirectionsUrl { get; set; } = string.Empty;
    }

    public class ParkFilters
    {
        public List<string> Landscapes { get; set; } = new();
        public List<string> Activities { get; set; } = new();
        public bool HasEntranceFee { get; set; }
        public string ReservationStatus { get; set; } = string.Empty;
        public List<string> Seasons { get; set; } = new();
        // full | partial | none
        public string PetsAllowed { get; set; } = string.Empty;
        public bool DarkSky { get; set; }
    }

    public class ParkMedia
    {
        public string HeroImage { get; set; } = string.Empty;
        public string HeroAlt { get; set; } = string.Empty;
        public string HeroCredit { get; set; } = string.Empty;
        public List<ParkHighlight> Highlights { get; set; } = new();
    }

    public class ParkHighlight
    {
        public string Image { get; set; } = string.Empty;
        public string Alt { get; set; } = string.Empty;
        public string Credit { get; set; } = string.Empty;
    }

    public class ParkHighlightStat
    {
        public string Stat { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }

    public class ParkAttractionItem
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Image { get; set; }
        public string? Alt { get; set; }
        public string? Credit { get; set; }
    }

    public class ParkStats
    {
        public int AreaAcres { get; set; }
        public int VisitationRank { get; set; }
        public string EntranceFeeDisplay { get; set; } = string.Empty;
    }

    public class ParkFaq : IFaqItem
    {
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
    }

    public class ParkSource : ISource
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class ParkTrail
    {
        public string Name { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        public string Distance { get; set; } = string.Empty;
        public string Elevation { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
    }

    public class ParkSeason
    {
        public string Season { get; set; } = string.Empty;
        public string Months { get; set; } = string.Empty;
        public string TempRim { get; set; } = string.Empty;
        public string CrowdLevel { get; set; } = string.Empty;
        public string Verdict { get; set; } = string.Empty;
    }

    public class ParkCampground
    {
        public string Name { get; set; } = string.Empty;
        public int Sites { get; set; }
        public string Season { get; set; } = string.Empty;
        public string Reservations { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
    }

    public class ParkFeeItem
    {
        public string PassType { get; set; } = string.Empty;
        public string Cost { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
    }

    public class ParkRankings
    {
        public int OverallRank { get; set; }     // 1 = best among 63 parks

        // Personality block — /60
        public int Personality { get; set; }     // sum of below 5 (max 60)
        public int Beauty { get; set; }          // /15 — scenic/photogenic quality
        public int Recreation { get; set; }      // /15 — activity variety & quality
        public int Privacy { get; set; }         // /10 — higher = less crowded
        public int Weather { get; set; }         // /10 — pleasantness of climate
        public int Wildlife { get; set; }        // /10 — wildlife abundance & visibility

        // Practicality block — /40
        public int Practicality { get; set; }    // sum of below 5 (max 40)
        public int Accessibility { get; set; }   // /15 — ease of getting there & around
        public int Amenities { get; set; }       // /10 — facilities quality
        public int Lodging { get; set; }         // /5  — accommodation availability
        public int Frugality { get; set; }       // /5  — affordability
        public int Family { get; set; }          // /5  — child-friendliness

        public int OverallScore => Personality + Practicality;
    }
}
