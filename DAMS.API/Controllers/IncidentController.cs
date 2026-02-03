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
    public class IncidentController : ControllerBase
    {
        private readonly IIncidentService _incidentService;

        public IncidentController(IIncidentService incidentService)
        {
            _incidentService = incidentService;
        }

        private string GetCurrentUsername()
        {
            return User?.Identity?.Name ?? string.Empty;
        }

        [HttpGet]
        public async Task<ActionResult<APIResponse>> GetAllIncidents([FromQuery] IncidentFilterDto filter)
        {
            var response = await _incidentService.GetAllIncidentsAsync(filter);
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<APIResponse>> GetIncidentById(int id)
        {
            var response = await _incidentService.GetIncidentByIdAsync(id);
            if (!response.IsSuccessful)
            {
                return NotFound(response);
            }
            return Ok(response);
        }

        [HttpGet("{id}/details")]
        public async Task<ActionResult<APIResponse>> GetIncidentDetails(int id)
        {
            var response = await _incidentService.GetIncidentDetailsAsync(id);
            if (!response.IsSuccessful)
            {
                return NotFound(response);
            }
            return Ok(response);
        }

        [HttpGet("asset/{assetId}")]
        public async Task<ActionResult<APIResponse>> GetIncidentsByAsset(int assetId, [FromQuery] IncidentFilterDto? filter = null)
        {
            var response = await _incidentService.GetIncidentsByAssetIdAsync(assetId, filter);
            return Ok(response);
        }

        [HttpPost]
        public async Task<ActionResult<APIResponse>> CreateIncident([FromBody] CreateIncidentDto dto)
        {
            var username = GetCurrentUsername();
            var response = await _incidentService.CreateIncidentAsync(dto, username);

            if (!response.IsSuccessful)
            {
                return BadRequest(response);
            }

            if (response.Data is IncidentDto incidentDto)
            {
                return CreatedAtAction(nameof(GetIncidentById), new { id = incidentDto.Id }, response);
            }

            return CreatedAtAction(nameof(GetIncidentById), new { id = 0 }, response);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<APIResponse>> UpdateIncident(int id, [FromBody] UpdateIncidentDto dto)
        {
            var username = GetCurrentUsername();
            var response = await _incidentService.UpdateIncidentAsync(id, dto, username);

            return Ok(response);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<APIResponse>> DeleteIncident(int id)
        {
            var username = GetCurrentUsername();
            var response = await _incidentService.DeleteIncidentAsync(id, username);
            return Ok(response);
        }

        [HttpGet("{id}/comments")]
        public async Task<ActionResult<APIResponse>> GetIncidentComments(int id)
        {
            var response = await _incidentService.GetIncidentCommentsAsync(id);
            if (!response.IsSuccessful)
            {
                return NotFound(response);
            }
            return Ok(response);
        }

        [HttpPost("{id}/comments")]
        public async Task<ActionResult<APIResponse>> AddIncidentComment(int id, [FromBody] CreateIncidentCommentDto dto)
        {
            // Ensure the incident ID in the DTO matches the route parameter
            dto.IncidentId = id;

            var username = GetCurrentUsername();
            var response = await _incidentService.AddIncidentCommentAsync(dto, username);

            if (!response.IsSuccessful)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
    }
}
