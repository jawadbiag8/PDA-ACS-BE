namespace DAMS.Domain.Entities
{
    public class MetricWeights : BaseEntity
    {
        public string Category { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Weight { get; set; }
    }
}
