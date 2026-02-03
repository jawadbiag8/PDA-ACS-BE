using DAMS.Application.Interfaces;
using DAMS.Application.Models;
using DAMS.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace DAMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IIdentityService _identityService;

        public AuthController(IIdentityService identityService)
        {
            _identityService = identityService;
        }

        [HttpPost("login")]
        public async Task<ActionResult<APIResponse>> Login([FromBody] LoginDto dto)
        {
            var response = await _identityService.LoginAsync(dto.Username, dto.Password);
            
            if (!response.IsSuccessful)
            {
                return Unauthorized(response);
            }

            return Ok(response);
        }

        [HttpPost("logout")]
        public async Task<ActionResult<APIResponse>> Logout()
        {
            var response = await _identityService.LogoutAsync();
            return Ok(response);
        }
    }
}
