namespace DAMS.Application.DTOs
{
    public class KpisLovDto
    {
        public int Id { get; set; }
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
    }

    public class CreateKpisLovDto
    {
        public string KpiName { get; set; } = string.Empty;
        public string? KpiGroup { get; set; }
        public string? Manual { get; set; }
        public string? Frequency { get; set; }
        public string? Outcome { get; set; }
        public string? PagesToCheck { get; set; }
        public string? TargetType { get; set; }
        public string? TargetHigh { get; set; }
        public string? TargetMedium { get; set; }
        public string? TargetLow { get; set; }
        public string? KpiType { get; set; }
        public int SeverityId { get; set; }
        public int? Weight { get; set; }
    }

    public class UpdateKpisLovDto
    {
        public string? KpiName { get; set; }
        public string? KpiGroup { get; set; }
        public string? Manual { get; set; }
        public string? Frequency { get; set; }
        public string? Outcome { get; set; }
        public string? PagesToCheck { get; set; }
        public string? TargetType { get; set; }
        public string? TargetHigh { get; set; }
        public string? TargetMedium { get; set; }
        public string? TargetLow { get; set; }
        public string? KpiType { get; set; }
        public int? SeverityId { get; set; }
        public int? Weight { get; set; }
    }
}
