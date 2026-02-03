namespace DAMS.Domain.Entities
{
    public class IncidentComment
    {
        public int Id { get; set; }
        public int IncidentId { get; set; }
        public string Comment { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation properties
        public Incident Incident { get; set; } = null!;
    }
}
