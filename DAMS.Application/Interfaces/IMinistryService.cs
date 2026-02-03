using DAMS.Application.Models;
using DAMS.Application.DTOs;

namespace DAMS.Application.Interfaces
{
    public interface IMinistryService
    {
        Task<APIResponse> GetMinistryByIdAsync(int id);
        Task<APIResponse> GetMinistryDetailsAsync(PagedRequest filter);
        Task<APIResponse> GetAllMinistriesAsync();
        Task<APIResponse> GetAllMinistriesAsync(MinistryFilterDto filter);
        Task<APIResponse> CreateMinistryAsync(CreateMinistryDto dto, string createdBy);
        Task<APIResponse> UpdateMinistryAsync(int id, UpdateMinistryDto dto, string updatedBy);
        Task<APIResponse> DeleteMinistryAsync(int id, string deletedBy);
    }
}
