using DAMS.Application.Models;

namespace DAMS.Application.DTOs
{
    public class IncidentFilterDto : PagedRequest
    {
        public int? MinistryId { get; set; }
        public int? AssetId { get; set; }
        public int? KpiId { get; set; }
        public int? SeverityId { get; set; }
        public int? StatusId { get; set; } // CommonLookup ID for status (e.g., "Open" or "Closed")
        public string? CreatedBy { get; set; }
        public string? AssignedTo { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
    }
}
