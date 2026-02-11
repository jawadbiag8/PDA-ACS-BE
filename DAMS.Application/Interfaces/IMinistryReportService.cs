using DAMS.Application.Models;

namespace DAMS.Application.Interfaces
{
    public interface IMinistryReportService
    {
        Task<MinistryReportPdfResult> GenerateReportPdfAsync(int ministryId);
    }
}
