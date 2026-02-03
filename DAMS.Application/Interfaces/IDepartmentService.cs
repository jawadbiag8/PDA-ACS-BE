using DAMS.Application.Models;
using DAMS.Application.DTOs;

namespace DAMS.Application.Interfaces
{
    public interface IDepartmentService
    {
        Task<APIResponse> GetDepartmentByIdAsync(int id);
        Task<APIResponse> GetAllDepartmentsAsync(DepartmentFilterDto filter);
        Task<APIResponse> GetAllDepartmentsAsync(int ministryId);
        Task<APIResponse> GetDepartmentsByMinistryIdAsync(int ministryId);
        Task<APIResponse> CreateDepartmentAsync(CreateDepartmentDto dto, string createdBy);
        Task<APIResponse> UpdateDepartmentAsync(int id, UpdateDepartmentDto dto, string updatedBy);
        Task<APIResponse> DeleteDepartmentAsync(int id, string deletedBy);
    }
}
