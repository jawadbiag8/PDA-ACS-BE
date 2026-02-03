namespace DAMS.Application.DTOs
{
    public class DashboardSummaryDto
    {
        public int TotalDigitalAssetsMonitored { get; set; }
        public int AssetsOnline { get; set; }
        public double AssetsOnlinePercentage { get; set; }
        public double HealthIndex { get; set; }
        public string HealthStatus { get; set; } = string.Empty;
        public double PerformanceIndex { get; set; }
        public string PerformanceStatus { get; set; } = string.Empty;
        public double ComplianceIndex { get; set; }
        public string ComplianceStatus { get; set; } = string.Empty;
        public int HighRiskAssets { get; set; }
        public string HighRiskAssetsStatus { get; set; } = string.Empty;
        public int OpenIncidents { get; set; }
        public int CriticalSeverityOpenIncidents { get; set; }
        public double CriticalSeverityOpenIncidentsPercentage { get; set; }
    }
}
