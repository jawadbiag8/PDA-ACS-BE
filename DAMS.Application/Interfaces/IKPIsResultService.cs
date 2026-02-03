using DAMS.Application.Models;
using DAMS.Application.DTOs;

namespace DAMS.Application.Interfaces
{
    public interface IKPIsResultService
    {
        Task<APIResponse> GetKPIsResultByIdAsync(int id);
        Task<APIResponse> GetAllKPIsResultsAsync(KPIsResultFilterDto filter);
        Task<APIResponse> GetKPIsResultsByAssetIdAsync(int assetId);
        Task<APIResponse> CreateKPIsResultAsync(CreateKPIsResultDto dto, string createdBy);
        Task<APIResponse> UpdateKPIsResultAsync(int id, UpdateKPIsResultDto dto, string updatedBy);
        Task<APIResponse> DeleteKPIsResultAsync(int id, string deletedBy);
    }
}
