namespace DAMS.Application.DTOs
{
    public class AssetControlPanelDto
    {
        public AssetDashboardHeaderDto Header { get; set; } = new AssetDashboardHeaderDto();
        public List<KpiCategoryDto> KpiCategories { get; set; } = new List<KpiCategoryDto>();
    }

    public class KpiCategoryDto
    {
        public string CategoryName { get; set; } = string.Empty;
        public List<KpiItemDto> Kpis { get; set; } = new List<KpiItemDto>();
    }

    public class KpiItemDto
    {
        public int KpiId { get; set; }
        public string KpiName { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public string Manual { get; set; } = string.Empty;
        public string CurrentValue { get; set; } = "N/A";
        public string SlaStatus { get; set; } = "UNKNOWN";
        public string LastChecked { get; set; } = "N/A";
        public string DataSource { get; set; } = string.Empty;
    }
}
