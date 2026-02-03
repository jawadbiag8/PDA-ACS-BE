namespace DAMS.Application.DTOs
{
    public class IncidentDashboardDto
    {
        public int Id { get; set; }
        public string IncidentTitle { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string SeverityDescription { get; set; } = string.Empty;
        public string Status { get; set; } = "Open";
        public string StatusSince { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public string CreatedAgo { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Kpi { get; set; } = string.Empty;
        public string AssetName { get; set; } = string.Empty;
        public string MinistryName { get; set; } = string.Empty;
        public string AssetUrl { get; set; } = string.Empty;
    }
}
