using DAMS.Application.Models;

namespace DAMS.Application.DTOs
{
    public class AssetFilterDto : PagedRequest
    {
        public int? MinistryId { get; set; }
        public int? DepartmentId { get; set; }
        public int? CitizenImpactLevelId { get; set; }
        public string? CurrentStatus { get; set; }
        public string? Health { get; set; }
        public string? Performance { get; set; }
        public string? Compliance { get; set; }
        public string? RiskIndex { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
    }
}
