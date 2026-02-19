using System;
using System.Text.Json.Serialization;

namespace DAMS.Application.DTOs
{
    /// <summary>Response for GET /api/Incident: summary counts plus paged list. Existing 'data' shape unchanged.</summary>
    public class IncidentListResponseDto
    {
        [JsonPropertyName("summary")]
        public IncidentSummaryDto Summary { get; set; } = new IncidentSummaryDto();
        [JsonPropertyName("data")]
        public List<IncidentDashboardDto> Data { get; set; } = new List<IncidentDashboardDto>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}
