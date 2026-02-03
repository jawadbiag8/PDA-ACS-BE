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
    public class CommonLookupController : ControllerBase
    {
        private readonly ICommonLookupService _commonLookupService;

        public CommonLookupController(ICommonLookupService commonLookupService)
        {
            _commonLookupService = commonLookupService;
        }

        private string GetCurrentUsername()
        {
            return User?.Identity?.Name ?? string.Empty;
        }

        [HttpGet]
        public async Task<ActionResult<APIResponse>> GetAllCommonLookups([FromQuery] PagedRequest filter)
        {
            var response = await _commonLookupService.GetAllCommonLookupsAsync(filter);
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<APIResponse>> GetCommonLookupById(int id)
        {
            var response = await _commonLookupService.GetCommonLookupByIdAsync(id);
            if (!response.IsSuccessful)
            {
                return NotFound(response);
            }
            return Ok(response);
        }

        [HttpGet("type/{type}")]
        public async Task<ActionResult<APIResponse>> GetCommonLookupsByType(string type)
        {
            var response = await _commonLookupService.GetCommonLookupsByTypeAsync(type);
            return Ok(response);
        }

        [HttpPost]
        public async Task<ActionResult<APIResponse>> CreateCommonLookup([FromBody] CreateCommonLookupDto dto)
        {
            var username = GetCurrentUsername();
            var response = await _commonLookupService.CreateCommonLookupAsync(dto, username);

            if (!response.IsSuccessful)
            {
                return BadRequest(response);
            }

            if (response.Data is CommonLookupDto commonLookupDto)
            {
                return CreatedAtAction(nameof(GetCommonLookupById), new { id = commonLookupDto.Id }, response);
            }

            return CreatedAtAction(nameof(GetCommonLookupById), new { id = 0 }, response);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<APIResponse>> UpdateCommonLookup(int id, [FromBody] UpdateCommonLookupDto dto)
        {
            var username = GetCurrentUsername();
            var response = await _commonLookupService.UpdateCommonLookupAsync(id, dto, username);

            return Ok(response);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<APIResponse>> DeleteCommonLookup(int id)
        {
            var username = GetCurrentUsername();
            var response = await _commonLookupService.DeleteCommonLookupAsync(id, username);
            return Ok(response);
        }
    }
}
