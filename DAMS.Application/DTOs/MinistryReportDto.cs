namespace DAMS.Application.DTOs
{
    /// <summary>Data for the ministry PDF report.</summary>
    public class MinistryReportDto
    {
        public string MinistryName { get; set; } = string.Empty;
        public DateTime ReportGeneratedAt { get; set; }
        public int AssetsMonitored { get; set; }
        public int TotalIncidents { get; set; }
        public int ActiveIncidents { get; set; }
        public double ResolutionPerformance { get; set; }
        public List<MinistryReportAssetRowDto> Assets { get; set; } = new();
    }

    public class MinistryReportAssetRowDto
    {
        public string AssetName { get; set; } = string.Empty;
        public double ComplianceScore { get; set; }
        /// <summary>Open incidents count (incidents where status != Resolved).</summary>
        public int OpenIncidents { get; set; }
        /// <summary>Open incident rows for this asset: KPI, Value (Target), Incident Created At, Status.</summary>
        public List<MinistryReportIncidentRowDto> IncidentDetails { get; set; } = new();
    }

    /// <summary>One row in the incident details table (open incidents only).</summary>
    public class MinistryReportIncidentRowDto
    {
        public string KpiName { get; set; } = string.Empty;
        /// <summary>Display as "CurrentValue (Target)" from KPIsResult and KpisLov.</summary>
        public string ValueTargetDisplay { get; set; } = string.Empty;
        public DateTime IncidentCreatedAt { get; set; }
        public string StatusName { get; set; } = string.Empty;
    }
}
