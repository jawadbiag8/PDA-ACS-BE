using DAMS.Application.Models;

namespace DAMS.Application.Interfaces
{
    public interface IIdentityService
    {
        Task<APIResponse> CreateUserAsync(string email, string password, string firstName, string lastName);
        Task<APIResponse> CreateRoleAsync(string roleName, string description);
        Task<APIResponse> AssignRoleToUserAsync(string userId, string roleName);
        Task<APIResponse> RemoveRoleFromUserAsync(string userId, string roleName);
        Task<APIResponse> ValidateUserAsync(string email, string password);
        Task<APIResponse> DeleteUserAsync(string userId);
        Task<APIResponse> UpdateUserAsync(string userId, string? firstName, string? lastName, string? email);
        Task<APIResponse> LoginAsync(string username, string password);
        Task<APIResponse> LogoutAsync();
    }
}
