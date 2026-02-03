namespace DAMS.Application.DTOs
{
    public class KPIsResultDto
    {
        public int Id { get; set; }
        public int AssetId { get; set; }
        public int KpiId { get; set; }
        public string Result { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateKPIsResultDto
    {
        public int AssetId { get; set; }
        public int KpiId { get; set; }
        public string Result { get; set; } = string.Empty;
        public string? Details { get; set; }
    }

    public class UpdateKPIsResultDto
    {
        public string? Result { get; set; }
        public string? Details { get; set; }
    }
}
