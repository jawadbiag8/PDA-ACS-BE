namespace DAMS.Domain.Entities
{
    public class Incident : BaseEntity
    {
        public int AssetId { get; set; }
        public int KpiId { get; set; }
        public string IncidentTitle { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int SeverityId { get; set; }
        public int StatusId { get; set; }
        public string AssignedTo { get; set; } = string.Empty;
        
        // Navigation properties
        public CommonLookup Severity { get; set; } = null!;
        public CommonLookup Status { get; set; } = null!;
        public Asset Asset { get; set; } = null!;
        public KpisLov KpisLov { get; set; } = null!;
        public ICollection<IncidentComment> Comments { get; set; } = new List<IncidentComment>();
        public ICollection<IncidentHistory> History { get; set; } = new List<IncidentHistory>();
    }
}
