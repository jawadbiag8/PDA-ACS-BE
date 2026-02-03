using DAMS.Application.Models;

namespace DAMS.Application.DTOs
{
    public class KPIsResultFilterDto : PagedRequest
    {
        public int? AssetId { get; set; }
        public int? KpiId { get; set; }
    }
}
