using DAMS.Application.Models;
using DAMS.Application.DTOs;

namespace DAMS.Application.Interfaces
{
    public interface ICommonLookupService
    {
        Task<APIResponse> GetCommonLookupByIdAsync(int id);
        Task<APIResponse> GetAllCommonLookupsAsync(PagedRequest filter);
        Task<APIResponse> GetCommonLookupsByTypeAsync(string type);
        Task<APIResponse> CreateCommonLookupAsync(CreateCommonLookupDto dto, string createdBy);
        Task<APIResponse> UpdateCommonLookupAsync(int id, UpdateCommonLookupDto dto, string updatedBy);
        Task<APIResponse> DeleteCommonLookupAsync(int id, string deletedBy);
    }
}
