namespace DAMS.Domain.Entities
{
    public class Asset : BaseEntity
    {
        public int MinistryId { get; set; }
        public int? DepartmentId { get; set; }
        public string AssetName { get; set; } = string.Empty;
        public string AssetUrl { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int CitizenImpactLevelId { get; set; }
        public string PrimaryContactName { get; set; } = string.Empty;
        public string PrimaryContactEmail { get; set; } = string.Empty;
        public string PrimaryContactPhone { get; set; } = string.Empty;
        public string TechnicalContactName { get; set; } = string.Empty;
        public string TechnicalContactEmail { get; set; } = string.Empty;
        public string TechnicalContactPhone { get; set; } = string.Empty;

        // Navigation properties
        public Ministry Ministry { get; set; } = null!;
        public Department? Department { get; set; }
        public CommonLookup CitizenImpactLevel { get; set; } = null!;
        public ICollection<KPIsResult> KPIsResults { get; set; } = new List<KPIsResult>();
        public ICollection<KPIsResultHistory> KPIsResultHistories { get; set; } = new List<KPIsResultHistory>();
        public ICollection<Incident> Incidents { get; set; } = new List<Incident>();
        public ICollection<AssetMetrics> AssetMetrics { get; set; } = new List<AssetMetrics>();
    }
}
