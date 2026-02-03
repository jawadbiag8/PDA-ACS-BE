namespace DAMS.Application.DTOs
{
    public class AssetDashboardDto
    {
        public int Id { get; set; }
        public string MinistryDepartment { get; set; } = string.Empty;
        public int MinistryId { get; set; } = 0;
        public string Department { get; set; } = string.Empty;
        public string WebsiteApplication { get; set; } = string.Empty;
        public string AssetUrl { get; set; } = string.Empty;
        public string CurrentStatus { get; set; } = "UNKNOWN";
        public DateTime? LastChecked { get; set; }
        public string LastOutage { get; set; } = "N/A";
        public DateTime? LastOutageDate { get; set; }
        public string HealthStatus { get; set; } = "UNKNOWN";
        public int HealthIndex { get; set; }
        public string PerformanceStatus { get; set; } = "UNKNOWN";
        public int PerformanceIndex { get; set; }
        public string ComplianceStatus { get; set; } = "UNKNOWN";
        public int ComplianceIndex { get; set; }
        public string RiskExposureIndex { get; set; } = "UNKNOWN";
        public string CitizenImpactLevel { get; set; } = "UNKNOWN";
        public int OpenIncidents { get; set; }
        public int HighSeverityIncidents { get; set; }
    }
}
