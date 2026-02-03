using DAMS.Application.Interfaces;
using DAMS.Application.Models;
using DAMS.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DAMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "PDA Analyst,PMO Executive")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IIdentityService _identityService;

        public UserController(IUserService userService, IIdentityService identityService)
        {
            _userService = userService;
            _identityService = identityService;
        }

        [HttpGet]
        public async Task<ActionResult<APIResponse>> GetAllUsers()
        {
            var response = await _userService.GetAllUsersAsync();
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<APIResponse>> GetUserById(string id)
        {
            var response = await _userService.GetUserByIdAsync(id);
            if (!response.IsSuccessful)
            {
                return NotFound(response);
            }
            return Ok(response);
        }

        [HttpGet("email/{email}")]
        public async Task<ActionResult<APIResponse>> GetUserByEmail(string email)
        {
            var response = await _userService.GetUserByEmailAsync(email);
            if (!response.IsSuccessful)
            {
                return NotFound(response);
            }
            return Ok(response);
        }

        [HttpGet("role/{roleName}")]
        public async Task<ActionResult<APIResponse>> GetUsersByRole(string roleName)
        {
            var response = await _userService.GetUsersByRoleAsync(roleName);
            return Ok(response);
        }

        [HttpGet("dropdown")]
        public async Task<ActionResult<APIResponse>> GetUsersForDropdown()
        {
            var response = await _userService.GetUsersForDropdownAsync();
            return Ok(response);
        }

        [HttpPost]
        public async Task<ActionResult<APIResponse>> CreateUser([FromBody] CreateUserDto dto)
        {
            var response = await _identityService.CreateUserAsync(
                dto.Email,
                dto.Password,
                dto.FirstName,
                dto.LastName);

            if (!response.IsSuccessful)
            {
                return BadRequest(response);
            }

            var userId = response.Data?.ToString() ?? string.Empty;
            var userResponse = await _userService.GetUserByIdAsync(userId);
            return CreatedAtAction(nameof(GetUserById), new { id = userId }, userResponse);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<APIResponse>> UpdateUser(string id, [FromBody] UpdateUserDto dto)
        {
            var response = await _identityService.UpdateUserAsync(id, dto.FirstName, dto.LastName, dto.Email);
            return Ok(response);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<APIResponse>> DeleteUser(string id)
        {
            var response = await _identityService.DeleteUserAsync(id);
            return Ok(response);
        }

        [HttpPost("role")]
        public async Task<ActionResult<APIResponse>> CreateRole([FromBody] CreateRoleDto dto)
        {
            var response = await _identityService.CreateRoleAsync(dto.Name, dto.Description);
            if (!response.IsSuccessful)
            {
                return BadRequest(response);
            }
            return CreatedAtAction(nameof(CreateRole), new { id = response.Data }, response);
        }

        [HttpPost("assign-role")]
        public async Task<ActionResult<APIResponse>> AssignRoleToUser([FromBody] AssignRoleDto dto)
        {
            var response = await _identityService.AssignRoleToUserAsync(dto.UserId, dto.RoleName);
            return Ok(response);
        }

        [HttpPost("remove-role")]
        public async Task<ActionResult<APIResponse>> RemoveRoleFromUser([FromBody] AssignRoleDto dto)
        {
            var response = await _identityService.RemoveRoleFromUserAsync(dto.UserId, dto.RoleName);
            return Ok(response);
        }
    }
}
