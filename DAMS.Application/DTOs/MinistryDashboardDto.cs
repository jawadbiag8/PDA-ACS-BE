namespace DAMS.Application.DTOs
{
    public class MinistryDashboardDto
    {
        public int Id { get; set; }
        public string MinistryName { get; set; } = string.Empty;
        public int NumberOfDepartments { get; set; }
        public int NumberOfAssets { get; set; }
        public string ContactName { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
        public int OpenIncidents { get; set; }
        public int HighSeverityIncidents { get; set; }
    }
}
