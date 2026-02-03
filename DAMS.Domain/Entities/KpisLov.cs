namespace DAMS.Domain.Entities
{
    public class KpisLov : BaseEntity
    {
        public string KpiName { get; set; } = string.Empty;
        public string KpiGroup { get; set; } = string.Empty;
        public string Manual { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public string Outcome { get; set; } = string.Empty;
        public string PagesToCheck { get; set; } = string.Empty;
        public string TargetType { get; set; } = string.Empty;
        public string TargetHigh { get; set; } = string.Empty;
        public string TargetMedium { get; set; } = string.Empty;
        public string TargetLow { get; set; } = string.Empty;
        public string KpiType { get; set; } = string.Empty;
        public int SeverityId { get; set; }
        public int Weight { get; set; }

        // Navigation properties
        public CommonLookup Severity { get; set; } = null!;
        public ICollection<KPIsResult> KPIsResults { get; set; } = new List<KPIsResult>();
    }
}
