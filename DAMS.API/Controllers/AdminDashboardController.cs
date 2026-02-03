using DAMS.Application.Interfaces;
using DAMS.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DAMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "PDA Analyst,PMO Executive")]
    public class AdminDashboardController : ControllerBase
    {
        private readonly IAssetService _assetService;

        public AdminDashboardController(IAssetService assetService)
        {
            _assetService = assetService;
        }

        [HttpGet("summary")]
        public async Task<ActionResult<APIResponse>> GetDashboardSummary()
        {
            var response = await _assetService.GetDashboardSummaryAsync();
            return Ok(response);
        }

        [HttpGet]
        public IActionResult GetDashboard()
        {
            return Ok(new { message = "Admin Dashboard" });
        }
    }
}
