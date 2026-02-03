using DAMS.Application.Models;

namespace DAMS.Application.DTOs
{
    public class KpisLovFilterDto : PagedRequest
    {
        public string? KpiGroup { get; set; }
    }
}
