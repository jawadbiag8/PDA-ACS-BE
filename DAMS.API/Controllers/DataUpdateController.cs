using DAMS.Application;
using DAMS.Application.Interfaces;
using DAMS.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DAMS.API.Controllers
{
    /// <summary>Endpoints for external systems (e.g. KPI scheduler) to notify that data has changed so SignalR clients can refetch. Call after writing to the database.</summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "PDA Analyst,PMO Executive")]
    public class DataUpdateController : ControllerBase
    {
        private readonly IDataUpdateNotifier _dataUpdateNotifier;

        public DataUpdateController(IDataUpdateNotifier dataUpdateNotifier)
        {
            _dataUpdateNotifier = dataUpdateNotifier;
        }

        /// <summary>Notify that dashboard metrics/calculations have been updated (e.g. AssetMetrics, KPIsResults recalculated). Clients refetch Admin Dashboard summary and PM Dashboard header.</summary>
        [HttpPost("notify-dashboards")]
        public async Task<ActionResult<APIResponse>> NotifyDashboardsUpdated()
        {
            await _dataUpdateNotifier.NotifyTopicAsync(DataUpdateTopics.AdminDashboardSummary);
            await _dataUpdateNotifier.NotifyTopicAsync(DataUpdateTopics.PMDashboardHeader);
            return Ok(new APIResponse
            {
                IsSuccessful = true,
                Message = "Dashboard and PM Dashboard update notified.",
                Data = null
            });
        }

        /// <summary>Notify that incidents have been created or closed/updated (e.g. by external scheduler). Clients refetch dashboards and optionally specific incident details. Pass incidentIds to refresh those incident pages.</summary>
        [HttpPost("notify-incidents")]
        public async Task<ActionResult<APIResponse>> NotifyIncidentsUpdated([FromBody] NotifyIncidentsRequest? request = null)
        {
            await _dataUpdateNotifier.NotifyTopicAsync(DataUpdateTopics.AdminDashboardSummary);
            await _dataUpdateNotifier.NotifyTopicAsync(DataUpdateTopics.PMDashboardHeader);
            if (request?.IncidentIds != null)
            {
                foreach (var id in request.IncidentIds)
                    await _dataUpdateNotifier.NotifyTopicAsync(DataUpdateTopics.Incident(id));
            }
            return Ok(new APIResponse
            {
                IsSuccessful = true,
                Message = "Incident update notified.",
                Data = null
            });
        }
    }

    /// <summary>Optional body for POST api/DataUpdate/notify-incidents. If IncidentIds is provided, clients subscribed to those incidents will refetch.</summary>
    public class NotifyIncidentsRequest
    {
        public IReadOnlyList<int>? IncidentIds { get; set; }
    }
}
