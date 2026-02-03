namespace DAMS.Application.DTOs
{
    public class MinistryAssetsSummaryDto
    {
        public int TotalAssets { get; set; }
        public int TotalIncidents { get; set; }
        public int OpenIncidents { get; set; }
        public int HighSeverityOpenIncidents { get; set; }
    }
}
