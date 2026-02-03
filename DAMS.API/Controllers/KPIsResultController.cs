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
    public class KPIsResultController : ControllerBase
    {
        private readonly IKPIsResultService _kpisResultService;

        public KPIsResultController(IKPIsResultService kpisResultService)
        {
            _kpisResultService = kpisResultService;
        }

        private string GetCurrentUsername()
        {
            return User?.Identity?.Name ?? string.Empty;
        }

        [HttpGet]
        public async Task<ActionResult<APIResponse>> GetAllKPIsResults([FromQuery] KPIsResultFilterDto filter)
        {
            var response = await _kpisResultService.GetAllKPIsResultsAsync(filter);
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<APIResponse>> GetKPIsResultById(int id)
        {
            var response = await _kpisResultService.GetKPIsResultByIdAsync(id);
            if (!response.IsSuccessful)
            {
                return NotFound(response);
            }
            return Ok(response);
        }

        [HttpGet("asset/{assetId}")]
        public async Task<ActionResult<APIResponse>> GetKPIsResultsByAssetId(int assetId)
        {
            var response = await _kpisResultService.GetKPIsResultsByAssetIdAsync(assetId);
            return Ok(response);
        }

        [HttpPost]
        public async Task<ActionResult<APIResponse>> CreateKPIsResult([FromBody] CreateKPIsResultDto dto)
        {
            var username = GetCurrentUsername();
            var response = await _kpisResultService.CreateKPIsResultAsync(dto, username);

            if (!response.IsSuccessful)
            {
                return BadRequest(response);
            }

            if (response.Data is KPIsResultDto kpisResultDto)
            {
                return CreatedAtAction(nameof(GetKPIsResultById), new { id = kpisResultDto.Id }, response);
            }

            return CreatedAtAction(nameof(GetKPIsResultById), new { id = 0 }, response);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<APIResponse>> UpdateKPIsResult(int id, [FromBody] UpdateKPIsResultDto dto)
        {
            var username = GetCurrentUsername();
            var response = await _kpisResultService.UpdateKPIsResultAsync(id, dto, username);

            return Ok(response);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<APIResponse>> DeleteKPIsResult(int id)
        {
            var username = GetCurrentUsername();
            var response = await _kpisResultService.DeleteKPIsResultAsync(id, username);
            return Ok(response);
        }
    }
}
