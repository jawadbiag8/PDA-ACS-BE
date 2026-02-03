using DAMS.Application.Models;

namespace DAMS.Application.DTOs
{
    public class MinistryFilterDto : PagedRequest
    {
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
    }
}
