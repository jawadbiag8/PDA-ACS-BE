namespace DAMS.Application.DTOs
{
    /// <summary>
    /// Request to fetch manual data from asset URL and save a KPIsResult entry for the given KPI.
    /// </summary>
    public class SaveManualDataRequestDto
    {
        public int AssetId { get; set; }
        public int KpiId { get; set; }
    }
}
