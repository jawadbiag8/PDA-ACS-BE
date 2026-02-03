namespace DAMS.Application.DTOs
{
    public class AssetDto
    {
        public int Id { get; set; }
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
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class CreateAssetDto
    {
        public int MinistryId { get; set; }
        public int? DepartmentId { get; set; }
        public string AssetName { get; set; } = string.Empty;
        public string AssetUrl { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int CitizenImpactLevelId { get; set; }
        public string? PrimaryContactName { get; set; }
        public string? PrimaryContactEmail { get; set; }
        public string? PrimaryContactPhone { get; set; }
        public string? TechnicalContactName { get; set; }
        public string? TechnicalContactEmail { get; set; }
        public string? TechnicalContactPhone { get; set; }
    }

    public class UpdateAssetDto
    {
        public int? MinistryId { get; set; }
        public int? DepartmentId { get; set; }
        public string? AssetName { get; set; }
        public string? AssetUrl { get; set; }
        public string? Description { get; set; }
        public int? CitizenImpactLevelId { get; set; }
        public string? PrimaryContactName { get; set; }
        public string? PrimaryContactEmail { get; set; }
        public string? PrimaryContactPhone { get; set; }
        public string? TechnicalContactName { get; set; }
        public string? TechnicalContactEmail { get; set; }
        public string? TechnicalContactPhone { get; set; }
    }
}
