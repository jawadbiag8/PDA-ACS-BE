using DAMS.Application.Interfaces;
using DAMS.Application.Models;
using DAMS.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DAMS.Infrastructure.Services
{
    public class IdentityService : IIdentityService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IConfiguration _configuration;

        public IdentityService(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            SignInManager<ApplicationUser> signInManager,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _configuration = configuration;
        }

        public async Task<APIResponse> CreateUserAsync(string email, string password, string firstName, string lastName)
        {
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, password);

            if (!result.Succeeded)
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = string.Join(", ", result.Errors.Select(e => e.Description)),
                    Data = null
                };
            }

            return new APIResponse
            {
                IsSuccessful = true,
                Message = "User created successfully",
                Data = user.Id
            };
        }

        public async Task<APIResponse> CreateRoleAsync(string roleName, string description)
        {
            var role = new ApplicationRole
            {
                Name = roleName,
                Description = description
            };

            var result = await _roleManager.CreateAsync(role);

            if (!result.Succeeded)
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = string.Join(", ", result.Errors.Select(e => e.Description)),
                    Data = null
                };
            }

            return new APIResponse
            {
                IsSuccessful = true,
                Message = "Role created successfully",
                Data = role.Id
            };
        }

        public async Task<APIResponse> AssignRoleToUserAsync(string userId, string roleName)
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

            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "Role not found.",
                    Data = null
                };
            }

            var result = await _userManager.AddToRoleAsync(user, roleName);

            if (!result.Succeeded)
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = string.Join(", ", result.Errors.Select(e => e.Description)),
                    Data = null
                };
            }

            return new APIResponse
            {
                IsSuccessful = true,
                Message = "Role assigned successfully",
                Data = null
            };
        }

        public async Task<APIResponse> RemoveRoleFromUserAsync(string userId, string roleName)
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

            var result = await _userManager.RemoveFromRoleAsync(user, roleName);

            if (!result.Succeeded)
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = string.Join(", ", result.Errors.Select(e => e.Description)),
                    Data = null
                };
            }

            return new APIResponse
            {
                IsSuccessful = true,
                Message = "Role removed successfully",
                Data = null
            };
        }

        public async Task<APIResponse> ValidateUserAsync(string email, string password)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "Invalid credentials.",
                    Data = null
                };
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: false);

            return new APIResponse
            {
                IsSuccessful = true,
                Message = result.Succeeded ? "User validated successfully" : "Invalid credentials.",
                Data = result.Succeeded
            };
        }

        public async Task<APIResponse> DeleteUserAsync(string userId)
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

            var result = await _userManager.DeleteAsync(user);

            if (!result.Succeeded)
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = string.Join(", ", result.Errors.Select(e => e.Description)),
                    Data = null
                };
            }

            return new APIResponse
            {
                IsSuccessful = true,
                Message = "User deleted successfully",
                Data = null
            };
        }

        public async Task<APIResponse> UpdateUserAsync(string userId, string? firstName, string? lastName, string? email)
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

            if (!string.IsNullOrEmpty(firstName))
                user.FirstName = firstName;

            if (!string.IsNullOrEmpty(lastName))
                user.LastName = lastName;

            if (!string.IsNullOrEmpty(email))
            {
                user.Email = email;
                user.UserName = email;
            }

            user.UpdatedAt = DateTime.Now;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = string.Join(", ", result.Errors.Select(e => e.Description)),
                    Data = null
                };
            }

            return new APIResponse
            {
                IsSuccessful = true,
                Message = "User updated successfully",
                Data = null
            };
        }

        public async Task<APIResponse> LoginAsync(string username, string password)
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "Invalid username or password.",
                    Data = null
                };
            }

            if (!user.IsActive)
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "User account is inactive.",
                    Data = null
                };
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: false);

            if (!result.Succeeded)
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "Invalid username or password.",
                    Data = null
                };
            }

            // Generate JWT Token
            var roles = await _userManager.GetRolesAsync(user);
            var token = GenerateJwtToken(user, roles);

            return new APIResponse
            {
                IsSuccessful = true,
                Message = "Login successful",
                Data = new
                {
                    Token = token,
                    Name = user.FirstName+ " "+ user.LastName,
                    Roles = roles
                }
            };
        }

        public async Task<APIResponse> LogoutAsync()
        {
            // JWT tokens are stateless, so logout is handled client-side by removing the token
            // This endpoint is kept for consistency
            return new APIResponse
            {
                IsSuccessful = true,
                Message = "Logout successful",
                Data = null
            };
        }

        private string GenerateJwtToken(ApplicationUser user, IList<string> roles)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not found.");
            var issuer = jwtSettings["Issuer"] ?? "DAMS";
            var audience = jwtSettings["Audience"] ?? "DAMS";
            var expirationMinutes = int.Parse(jwtSettings["ExpirationInMinutes"] ?? "60");

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim("username", user.UserName ?? string.Empty),
                new Claim("Name", user.FirstName + user.LastName ?? string.Empty),
                new Claim("email", user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // Add roles to claims
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.Now.AddDays(expirationMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

