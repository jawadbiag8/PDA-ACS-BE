using DAMS.Application.Models;

namespace DAMS.Application.DTOs
{
    public class DepartmentFilterDto : PagedRequest
    {
        public int? MinistryId { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
    }
}
