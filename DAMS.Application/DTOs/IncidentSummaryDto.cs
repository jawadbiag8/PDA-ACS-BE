using System.Text.Json.Serialization;

namespace DAMS.Application.DTOs
{
    /// <summary>Global incident counts for the system. Open = StatusId 8, Closed = StatusId 12.</summary>
    public class IncidentSummaryDto
    {
        [JsonPropertyName("totalIncidents")]
        public int TotalIncidents { get; set; }
        [JsonPropertyName("openIncidents")]
        public int OpenIncidents { get; set; }
        [JsonPropertyName("closedIncidents")]
        public int ClosedIncidents { get; set; }
    }
}
