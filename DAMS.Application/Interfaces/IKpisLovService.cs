using DAMS.Application.Models;
using DAMS.Application.DTOs;

namespace DAMS.Application.Interfaces
{
    public interface IKpisLovService
    {
        Task<APIResponse> GetKpisLovByIdAsync(int id);
        Task<APIResponse> GetAllKpisLovsAsync(KpisLovFilterDto filter);
        Task<APIResponse> GetKpisLovsForDropdownAsync();
        Task<APIResponse> CreateKpisLovAsync(CreateKpisLovDto dto, string createdBy);
        Task<APIResponse> UpdateKpisLovAsync(int id, UpdateKpisLovDto dto, string updatedBy);
        Task<APIResponse> DeleteKpisLovAsync(int id, string deletedBy);
        /// <summary>
        /// Calls the manual-check API for the given asset and KPI. Both assetId and kpiId are required.
        /// </summary>
        Task<APIResponse> GetKpisLovManualDataFromAssetUrlAsync(int assetId, int kpiId);
    }
}
