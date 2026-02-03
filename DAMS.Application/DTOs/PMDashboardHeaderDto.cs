namespace DAMS.Application.DTOs
{
    public class PMDashboardHeaderDto
    {
        // Digital Experience Score
        public double DigitalExperienceScore { get; set; }
        public double DigitalExperienceScoreChange { get; set; } 
        
        // Total Assets Being Monitored
        public int TotalAssetsBeingMonitored { get; set; }
        public int TotalMinistries { get; set; }
        
        // Digital Assets Are Offline
        public int DigitalAssetsOffline { get; set; }
        public DateTime? LastChecked { get; set; }
        
        // Ministries meet Compliance standards
        public int MinistriesMeetComplianceStandards { get; set; }
        public double ComplianceThreshold { get; set; } = 70.0; // Threshold for compliance (70%)
        
        // Active Incidents
        public int ActiveIncidents { get; set; }
        public int ResolvedIncidentsLast30Days { get; set; }
        
        // Assets are vulnerable
        public int AssetsAreVulnerable { get; set; }
        public double SecurityThreshold { get; set; } = 70.0; // Security Index threshold (< 70 is vulnerable)
    }
}
