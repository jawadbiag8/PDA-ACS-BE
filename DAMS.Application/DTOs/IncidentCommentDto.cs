namespace DAMS.Application.DTOs
{
    public class IncidentCommentDto
    {
        public int Id { get; set; }
        public int IncidentId { get; set; }
        public string Comment { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class CreateIncidentCommentDto
    {
        public int IncidentId { get; set; }
        public string Comment { get; set; } = string.Empty;
        public string? Status { get; set; }
    }
}
