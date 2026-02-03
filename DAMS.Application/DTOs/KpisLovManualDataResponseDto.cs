using System.Text.Json.Serialization;

namespace DAMS.Application.DTOs
{
    /// <summary>
    /// Response DTO for KPI Lov manual data fetched by hitting the asset URL.
    /// </summary>
    public class KpisLovManualDataResponseDto
    {
        public int AssetId { get; set; }
        public string AssetName { get; set; } = string.Empty;
        public string AssetUrl { get; set; } = string.Empty;
        /// <summary>
        /// Raw content (e.g. HTML or JSON) — not sent to front to avoid large payloads.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ManualData { get; set; }
        public string? ContentType { get; set; }
        /// <summary>
        /// Parsed analytics when the asset URL returns JSON with: Total visits, Unique visitors,
        /// Page views, Top accessed pages, Entry/Exit page distribution, Average session duration,
        /// Bounce rate, Peak usage windows.
        /// </summary>
        public KpisLovManualAnalyticsDto? Analytics { get; set; }
    }
}
