using DAMS.Application.Models;

namespace DAMS.Application.Interfaces
{
    public interface IUserService
    {
        Task<APIResponse> GetUserByIdAsync(string userId);
        Task<APIResponse> GetAllUsersAsync();
        Task<APIResponse> GetUsersByRoleAsync(string roleName);
        Task<APIResponse> GetUserByEmailAsync(string email);
        Task<APIResponse> GetUsersForDropdownAsync();
    }
}
