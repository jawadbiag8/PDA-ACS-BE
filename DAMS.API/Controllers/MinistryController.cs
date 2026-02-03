using DAMS.Application.Interfaces;
using DAMS.Application.Models;
using DAMS.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DAMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "PDA Analyst,PMO Executive")]
    public class MinistryController : ControllerBase
    {
        private readonly IMinistryService _ministryService;

        public MinistryController(IMinistryService ministryService)
        {
            _ministryService = ministryService;
        }

        private string GetCurrentUsername()
        {
            return User?.Identity?.Name ?? string.Empty;
        }

        [HttpGet]
        public async Task<ActionResult<APIResponse>> GetAllMinistries([FromQuery] MinistryFilterDto filter)
        {
            var response = await _ministryService.GetAllMinistriesAsync(filter);
            return Ok(response);
        }

        [HttpGet("getall")]
        public async Task<ActionResult<APIResponse>> GetAllMinistries()
        {
            var response = await _ministryService.GetAllMinistriesAsync();
            return Ok(response);
        }

        [HttpGet("ministrydetails")]
        public async Task<ActionResult<APIResponse>> GetMinistryDetails([FromQuery] PagedRequest filter)
        {
            var response = await _ministryService.GetMinistryDetailsAsync(filter ?? new PagedRequest());
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<APIResponse>> GetMinistryById(int id)
        {
            var response = await _ministryService.GetMinistryByIdAsync(id);
            if (!response.IsSuccessful)
            {
                return NotFound(response);
            }
            return Ok(response);
        }

        [HttpPost]
        public async Task<ActionResult<APIResponse>> CreateMinistry([FromBody] CreateMinistryDto dto)
        {
            var username = GetCurrentUsername();
            var response = await _ministryService.CreateMinistryAsync(dto, username);

            if (!response.IsSuccessful)
            {
                return BadRequest(response);
            }

            if (response.Data is MinistryDto ministryDto)
            {
                return CreatedAtAction(nameof(GetMinistryById), new { id = ministryDto.Id }, response);
            }

            return CreatedAtAction(nameof(GetMinistryById), new { id = 0 }, response);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<APIResponse>> UpdateMinistry(int id, [FromBody] UpdateMinistryDto dto)
        {
            var username = GetCurrentUsername();
            var response = await _ministryService.UpdateMinistryAsync(id, dto, username);

            return Ok(response);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<APIResponse>> DeleteMinistry(int id)
        {
            var username = GetCurrentUsername();
            var response = await _ministryService.DeleteMinistryAsync(id, username);
            return Ok(response);
        }
    }
}
