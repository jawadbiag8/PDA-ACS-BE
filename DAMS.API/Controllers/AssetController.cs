using DAMS.Application;
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
    public class AssetController : ControllerBase
    {
        private readonly IAssetService _assetService;
        private readonly IDataUpdateNotifier _dataUpdateNotifier;

        public AssetController(IAssetService assetService, IDataUpdateNotifier dataUpdateNotifier)
        {
            _assetService = assetService;
            _dataUpdateNotifier = dataUpdateNotifier;
        }

        private string GetCurrentUsername()
        {
            return User?.Identity?.Name ?? string.Empty;
        }

        [HttpGet]
        public async Task<ActionResult<APIResponse>> GetAllAssets([FromQuery] AssetFilterDto filter)
        {
            var response = await _assetService.GetAllAssetsAsync(filter);
            return Ok(response);
        }

        [HttpGet("byministry")]
        public async Task<ActionResult<APIResponse>> GetAssetsByMinistry([FromQuery] string? search = null)
        {
            var response = await _assetService.GetAssetsByMinistryAsync(search);
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<APIResponse>> GetAssetById(int id)
        {
            var response = await _assetService.GetAssetByIdAsync(id);
            if (!response.IsSuccessful)
            {
                return NotFound(response);
            }
            return Ok(response);
        }

        [HttpGet("{id}/dashboard/header")]
        public async Task<ActionResult<APIResponse>> GetAssetDashboardHeader(int id)
        {
            var response = await _assetService.GetAssetDashboardHeaderAsync(id);
            if (!response.IsSuccessful)
            {
                return NotFound(response);
            }
            return Ok(response);
        }

        [HttpGet("{id}/controlpanel")]
        public async Task<ActionResult<APIResponse>> GetAssetControlPanel(int id)
        {
            var response = await _assetService.GetAssetControlPanelAsync(id);
            if (!response.IsSuccessful)
            {
                return NotFound(response);
            }
            return Ok(response);
        }

        /// <summary>Notify connected clients that this asset's control panel data has changed (e.g. after manual KPI check or scheduler update). Refreshes control panel, KpisLov, and both dashboards. Call after updating KPIsResult or KPIsResultHistory for the asset.</summary>
        [HttpPost("{id}/controlpanel/notify")]
        public async Task<ActionResult<APIResponse>> NotifyControlPanelUpdated(int id)
        {
            await _dataUpdateNotifier.NotifyTopicAsync(DataUpdateTopics.AssetControlPanel(id));
            await _dataUpdateNotifier.NotifyTopicAsync(DataUpdateTopics.AssetKpisLov(id));
            await _dataUpdateNotifier.NotifyTopicAsync(DataUpdateTopics.AdminDashboardSummary);
            await _dataUpdateNotifier.NotifyTopicAsync(DataUpdateTopics.PMDashboardHeader);
            return Ok(new APIResponse { IsSuccessful = true, Message = "Control panel and dashboards update notified.", Data = null });
        }

        [HttpGet("ministry/{ministryId}")]
        public async Task<ActionResult<APIResponse>> GetAssetsByMinistry(int ministryId, [FromQuery] AssetFilterDto filter)
        {
            var response = await _assetService.GetAssetsByMinistryIdAsync(ministryId, filter);
            return Ok(response);
        }

        [HttpGet("ministry/{ministryId}/summary")]
        public async Task<ActionResult<APIResponse>> GetMinistryAssetsSummary(int ministryId)
        {
            var response = await _assetService.GetMinistryAssetsSummaryAsync(ministryId);
            return Ok(response);
        }

        [HttpGet("department/{departmentId}")]
        public async Task<ActionResult<APIResponse>> GetAssetsByDepartment(int departmentId)
        {
            var response = await _assetService.GetAssetsByDepartmentIdAsync(departmentId);
            return Ok(response);
        }

        [HttpGet("dropdown")]
        public async Task<ActionResult<APIResponse>> GetAssetsForDropdown()
        {
            var response = await _assetService.GetAssetsForDropdownAsync();
            return Ok(response);
        }

        [HttpPost]
        public async Task<ActionResult<APIResponse>> CreateAsset([FromBody] CreateAssetDto dto)
        {
            var username = GetCurrentUsername();
            var response = await _assetService.CreateAssetAsync(dto, username);

            if (!response.IsSuccessful)
            {
                return BadRequest(response);
            }

            if (response.Data is AssetDto assetDto)
            {
                return CreatedAtAction(nameof(GetAssetById), new { id = assetDto.Id }, response);
            }

            return CreatedAtAction(nameof(GetAssetById), new { id = 0 }, response);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<APIResponse>> UpdateAsset(int id, [FromBody] UpdateAssetDto dto)
        {
            var username = GetCurrentUsername();
            var response = await _assetService.UpdateAssetAsync(id, dto, username);

            return Ok(response);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<APIResponse>> DeleteAsset(int id)
        {
            var username = GetCurrentUsername();
            var response = await _assetService.DeleteAssetAsync(id, username);
            return Ok(response);
        }

        /// <summary>Download CSV template for bulk asset upload. Use the same file format with POST /api/Asset/bulk-upload.</summary>
        [HttpGet("bulk-upload/template")]
        public async Task<IActionResult> DownloadBulkUploadTemplate()
        {
            var bytes = await _assetService.GetBulkUploadTemplateAsync();
            const string fileName = "asset-bulk-upload-template.csv";
            return File(bytes, "text/csv", fileName);
        }

        [HttpPost("bulk-upload")]
        public async Task<ActionResult<APIResponse>> BulkUploadAssets(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new APIResponse
                {
                    IsSuccessful = false,
                    Message = "No file uploaded or file is empty.",
                    Data = null
                });
            }

            // Validate file extension
            var allowedExtensions = new[] { ".csv" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                return BadRequest(new APIResponse
                {
                    IsSuccessful = false,
                    Message = "Invalid file type. Only CSV files are allowed.",
                    Data = null
                });
            }

            // Validate file size (e.g., max 10MB)
            const long maxFileSize = 10 * 1024 * 1024; // 10MB
            if (file.Length > maxFileSize)
            {
                return BadRequest(new APIResponse
                {
                    IsSuccessful = false,
                    Message = "File size exceeds the maximum allowed size of 10MB.",
                    Data = null
                });
            }

            var username = GetCurrentUsername();
            using var stream = file.OpenReadStream();
            var response = await _assetService.BulkUploadAssetsAsync(stream, username);

            if (!response.IsSuccessful)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
    }
}
