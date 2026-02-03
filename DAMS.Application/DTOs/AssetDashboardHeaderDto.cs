namespace DAMS.Application.DTOs
{
    public class AssetDashboardHeaderDto
    {
        public string AssetUrl { get; set; } = string.Empty;
        public string AssetName { get; set; } = string.Empty;
        public string Ministry { get; set; } = string.Empty;
        public int MinistryId { get; set; } = 0;
        public string Department { get; set; } = string.Empty;
        public string CitizenImpactLevel { get; set; } = string.Empty;
        public string CurrentHealth { get; set; } = string.Empty;
        public string RiskExposureIndex { get; set; } = string.Empty;
        public string CurrentStatus { get; set; } = string.Empty;
        public string LastOutage { get; set; } = string.Empty;
        
        // Ownership & Accountability
        public string OwnerName { get; set; } = string.Empty;
        public string OwnerEmail { get; set; } = string.Empty;
        public string OwnerContact { get; set; } = string.Empty;
        public string TechnicalOwnerName { get; set; } = string.Empty;
        public string TechnicalOwnerEmail { get; set; } = string.Empty;
        public string TechnicalOwnerContact { get; set; } = string.Empty;
        
        // Compliance Overview Categories
        public string AccessibilityInclusivityStatus { get; set; } = string.Empty;
        public string AvailabilityReliabilityStatus { get; set; } = string.Empty;
        public string NavigationDiscoverabilityStatus { get; set; } = string.Empty;
        public string PerformanceEfficiencyStatus { get; set; } = string.Empty;
        public string SecurityTrustPrivacyStatus { get; set; } = string.Empty;
        public string UserExperienceJourneyQualityStatus { get; set; } = string.Empty;
        
        // Additional Metrics
        public double CitizenHappinessMetric { get; set; }
        public double OverallComplianceMetric { get; set; }
        public int OpenIncidents { get; set; }
        public int HighSeverityOpenIncidents { get; set; }
    }
}
