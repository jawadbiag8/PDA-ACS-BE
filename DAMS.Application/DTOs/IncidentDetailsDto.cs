namespace DAMS.Application.DTOs
{
    public class IncidentDetailsDto
    {
        // Incident Details
        public int Id { get; set; }
        public string IncidentTitle { get; set; } = string.Empty;
        public string AssetName { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string AssignedTo { get; set; } = string.Empty;
        public string KpiName { get; set; } = string.Empty;
        public int AssetId { get; set; }
        public string AssetUrl { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public string Ministry { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // Timeline entries
        public List<IncidentTimelineDto> Timeline { get; set; } = new List<IncidentTimelineDto>();

        // Comments
        public List<IncidentCommentDto> Comments { get; set; } = new List<IncidentCommentDto>();
    }

    public class IncidentTimelineDto
    {
        public int Id { get; set; }
        public string Time { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
