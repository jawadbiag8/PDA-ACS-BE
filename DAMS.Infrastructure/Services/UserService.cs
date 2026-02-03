using DAMS.Application.Interfaces;
using DAMS.Application.Models;
using DAMS.Application.DTOs;
using DAMS.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DAMS.Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;

        public UserService(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<APIResponse> GetUserByIdAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "User not found.",
                    Data = null
                };
            }

            var roles = await _userManager.GetRolesAsync(user);
            var userDto = MapToUserDto(user, roles);

            return new APIResponse
            {
                IsSuccessful = true,
                Message = "User retrieved successfully",
                Data = userDto
            };
        }

        public async Task<APIResponse> GetAllUsersAsync()
        {
            var users = await _userManager.Users.ToListAsync();
            var userDtos = new List<UserDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userDtos.Add(MapToUserDto(user, roles));
            }

            return new APIResponse
            {
                IsSuccessful = true,
                Message = "Users retrieved successfully",
                Data = userDtos
            };
        }

        public async Task<APIResponse> GetUsersByRoleAsync(string roleName)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role == null)
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "Role not found.",
                    Data = null
                };
            }

            var users = await _userManager.GetUsersInRoleAsync(roleName);
            var userDtos = new List<UserDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userDtos.Add(MapToUserDto(user, roles));
            }

            return new APIResponse
            {
                IsSuccessful = true,
                Message = "Users retrieved successfully",
                Data = userDtos
            };
        }

        public async Task<APIResponse> GetUserByEmailAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "User not found.",
                    Data = null
                };
            }

            var roles = await _userManager.GetRolesAsync(user);
            var userDto = MapToUserDto(user, roles);

            return new APIResponse
            {
                IsSuccessful = true,
                Message = "User retrieved successfully",
                Data = userDto
            };
        }

        public async Task<APIResponse> GetUsersForDropdownAsync()
        {
            var users = await _userManager.Users
                .Where(u => u.IsActive && !string.IsNullOrEmpty(u.Email))
                .OrderBy(u => u.Email)
                .Select(u => new
                {
                    Id = u.Id,
                    Email = u.Email ?? string.Empty
                })
                .ToListAsync();

            return new APIResponse
            {
                IsSuccessful = true,
                Message = "Users retrieved successfully for dropdown",
                Data = users
            };
        }

        private static UserDto MapToUserDto(ApplicationUser user, IList<string> roles)
        {
            return new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                UserName = user.UserName ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = roles.ToList(),
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            };
        }
    }
}

