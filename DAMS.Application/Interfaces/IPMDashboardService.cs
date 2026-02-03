using DAMS.Application.Models;

namespace DAMS.Application.Interfaces
{
    public interface IPMDashboardService
    {
        Task<APIResponse> GetPMDashboardHeaderAsync();
        Task<APIResponse> GetPMDashboardIndicesAsync();
        Task<APIResponse> GetBottomMinistriesByCitizenImpactAsync(int count = 5);
        Task<APIResponse> GetTopMinistriesByComplianceAsync(int count = 5);
    }
}
