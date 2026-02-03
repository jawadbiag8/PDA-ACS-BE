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
    public class KpisLovController : ControllerBase
    {
        private readonly IKpisLovService _kpisLovService;

        public KpisLovController(IKpisLovService kpisLovService)
        {
            _kpisLovService = kpisLovService;
        }

        private string GetCurrentUsername()
        {
            return User?.Identity?.Name ?? string.Empty;
        }

        [HttpGet]
        public async Task<ActionResult<APIResponse>> GetAllKpisLovs([FromQuery] KpisLovFilterDto filter)
        {
            var response = await _kpisLovService.GetAllKpisLovsAsync(filter);
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<APIResponse>> GetKpisLovById(int id)
        {
            var response = await _kpisLovService.GetKpisLovByIdAsync(id);
            if (!response.IsSuccessful)
            {
                return NotFound(response);
            }
            return Ok(response);
        }

        [HttpGet("dropdown")]
        public async Task<ActionResult<APIResponse>> GetKpisLovsForDropdown()
        {
            var response = await _kpisLovService.GetKpisLovsForDropdownAsync();
            return Ok(response);
        }

        /// <summary>
        /// Gets KPI Lov manual data by calling the given asset's URL (AssetUrl).
        /// If kpiId is provided as query parameter, also creates a KPIsResult entry after fetching.
        /// </summary>
        [HttpGet("manual-from-asset/{assetId}")]
        public async Task<ActionResult<APIResponse>> GetKpisLovManualDataFromAsset(int assetId, [FromQuery] int? kpiId = null)
        {
            var response = await _kpisLovService.GetKpisLovManualDataFromAssetUrlAsync(assetId, kpiId);
            if (!response.IsSuccessful)
            {
                if (response.Message?.Contains("not found") == true)
                    return NotFound(response);
                return BadRequest(response);
            }
            return Ok(response);
        }

        [HttpPost]
        public async Task<ActionResult<APIResponse>> CreateKpisLov([FromBody] CreateKpisLovDto dto)
        {
            var username = GetCurrentUsername();
            var response = await _kpisLovService.CreateKpisLovAsync(dto, username);

            if (!response.IsSuccessful)
            {
                return BadRequest(response);
            }

            if (response.Data is KpisLovDto kpisLovDto)
            {
                return CreatedAtAction(nameof(GetKpisLovById), new { id = kpisLovDto.Id }, response);
            }

            return CreatedAtAction(nameof(GetKpisLovById), new { id = 0 }, response);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<APIResponse>> UpdateKpisLov(int id, [FromBody] UpdateKpisLovDto dto)
        {
            var username = GetCurrentUsername();
            var response = await _kpisLovService.UpdateKpisLovAsync(id, dto, username);

            return Ok(response);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<APIResponse>> DeleteKpisLov(int id)
        {
            var username = GetCurrentUsername();
            var response = await _kpisLovService.DeleteKpisLovAsync(id, username);
            return Ok(response);
        }
    }
}
