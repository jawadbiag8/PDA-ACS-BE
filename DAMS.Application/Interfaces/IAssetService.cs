using DAMS.Application.Models;
using DAMS.Application.DTOs;

namespace DAMS.Application.Interfaces
{
    public interface IAssetService
    {
        Task<APIResponse> GetAssetByIdAsync(int id);
        Task<APIResponse> GetAllAssetsAsync(AssetFilterDto filter);
        Task<APIResponse> GetAssetsByMinistryAsync(string? searchTerm = null);
        Task<APIResponse> GetAssetsByMinistryIdAsync(int ministryId, AssetFilterDto? filter = null);
        Task<APIResponse> GetMinistryAssetsSummaryAsync(int ministryId);
        Task<APIResponse> GetAssetsByDepartmentIdAsync(int departmentId);
        Task<APIResponse> GetAssetsForDropdownAsync();
        Task<APIResponse> CreateAssetAsync(CreateAssetDto dto, string createdBy);
        Task<APIResponse> UpdateAssetAsync(int id, UpdateAssetDto dto, string updatedBy);
        Task<APIResponse> DeleteAssetAsync(int id, string deletedBy);
        Task<APIResponse> BulkUploadAssetsAsync(Stream csvStream, string createdBy);
        /// <summary>Returns CSV template bytes for bulk asset upload. Headers match CreateAssetDto; one example row included.</summary>
        Task<byte[]> GetBulkUploadTemplateAsync();
        Task<APIResponse> GetDashboardSummaryAsync();
        Task<APIResponse> GetAssetDashboardHeaderAsync(int assetId);
        Task<APIResponse> GetAssetControlPanelAsync(int assetId);
    }
}
