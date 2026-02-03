namespace DAMS.Application.DTOs
{
    public class IncidentDto
    {
        public int Id { get; set; }
        public int AssetId { get; set; }
        public int KpiId { get; set; }
        public string IncidentTitle { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int SeverityId { get; set; }
        public int StatusId { get; set; }
        public string AssignedTo { get; set; } = string.Empty;
        public DateTime? UpdatedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class CreateIncidentDto
    {
        public int AssetId { get; set; }
        public int KpiId { get; set; }
        public string IncidentTitle { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Type { get; set; }
        public int SeverityId { get; set; }
        public int StatusId { get; set; }
    }

    public class UpdateIncidentDto
    {
        public int? KpiId { get; set; }
        public string? IncidentTitle { get; set; }
        public string? Description { get; set; }
        public string? Type { get; set; }
        public int? SeverityId { get; set; }
        public int? StatusId { get; set; }
        public string? AssignedTo { get; set; }
    }
}
