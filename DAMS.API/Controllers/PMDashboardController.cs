using DAMS.Application.Interfaces;
using DAMS.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DAMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "PMO Executive")]
    public class PMDashboardController : ControllerBase
    {
        private readonly IPMDashboardService _pmDashboardService;

        public PMDashboardController(IPMDashboardService pmDashboardService)
        {
            _pmDashboardService = pmDashboardService;
        }

        [HttpGet]
        public ActionResult<APIResponse> GetDashboard()
        {
            return Ok(new APIResponse
            {
                IsSuccessful = true,
                Message = "PM Dashboard accessed successfully",
                Data = new { message = "PM Dashboard" }
            });
        }

        [HttpGet("header")]
        public async Task<ActionResult<APIResponse>> GetHeader()
        {
            var response = await _pmDashboardService.GetPMDashboardHeaderAsync();
            return Ok(response);
        }

        [HttpGet("indices")]
        public async Task<ActionResult<APIResponse>> GetIndices()
        {
            var response = await _pmDashboardService.GetPMDashboardIndicesAsync();
            return Ok(response);
        }

        [HttpGet("bottom-ministries")]
        public async Task<ActionResult<APIResponse>> GetBottomMinistriesByCitizenImpact([FromQuery] int count = 5)
        {
            var response = await _pmDashboardService.GetBottomMinistriesByCitizenImpactAsync(count);
            return Ok(response);
        }

        [HttpGet("top-ministries")]
        public async Task<ActionResult<APIResponse>> GetTopMinistriesByCompliance([FromQuery] int count = 5)
        {
            var response = await _pmDashboardService.GetTopMinistriesByComplianceAsync(count);
            return Ok(response);
        }
    }
}
