using DAMS.Application.Interfaces;
using DAMS.Application.Models;
using DAMS.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace DAMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "PDA Analyst,PMO Executive")]
    public class MinistryController : ControllerBase
    {
        private readonly IMinistryService _ministryService;
        private readonly IMinistryReportService _ministryReportService;
        private readonly IServiceProvider _serviceProvider;

        public MinistryController(IMinistryService ministryService, IMinistryReportService ministryReportService, IServiceProvider serviceProvider)
        {
            _ministryService = ministryService;
            _ministryReportService = ministryReportService;
            _serviceProvider = serviceProvider;
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

        /// <summary>Ministry details with optional search, pagination, and filters. Use filterType + filterValue for one filter at a time (Status: UP/DOWN/ALL; metrics: High/Average/Poor/Unknown). When filterType is not provided, all ministries are returned.</summary>
        [HttpGet("ministrydetails")]
        public async Task<ActionResult<APIResponse>> GetMinistryDetails(
            [FromQuery] PagedRequest filter,
            [FromQuery] string? filterType = null,
            [FromQuery] string? filterValue = null)
        {
            var requestFilter = filter ?? new PagedRequest();
            var response = await _ministryService.GetMinistryDetailsAsync(requestFilter, filterType, filterValue);
            if (!response.IsSuccessful)
                return BadRequest(response);
            if (!string.IsNullOrWhiteSpace(requestFilter.SearchTerm))
                Response.Headers.CacheControl = "no-store";
            return Ok(response);
        }

        /// <summary>Generate and download a PDF report for the ministry (ministry summary + assets with compliance score).</summary>
        [HttpGet("{id}/report")]
        public async Task<IActionResult> GetMinistryReportPdf(int id)
        {
            try
            {
                byte[] pdfBytes;
                string? fileName;
                using (var scope = _serviceProvider.CreateScope())
                {
                    var reportService = scope.ServiceProvider.GetRequiredService<IMinistryReportService>();
                    var result = await reportService.GenerateReportPdfAsync(id);
                    if (!result.Success)
                        return BadRequest(new APIResponse { IsSuccessful = false, Message = result.Message, Data = null });
                    var raw = result.PdfBytes ?? Array.Empty<byte>();
                    pdfBytes = new byte[raw.Length];
                    Array.Copy(raw, pdfBytes, raw.Length);
                    fileName = result.FileName ?? "Ministry-Report.pdf";
                }
                GC.WaitForPendingFinalizers();
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new APIResponse { IsSuccessful = false, Message = "Report generation failed: " + ex.Message, Data = null });
            }
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
