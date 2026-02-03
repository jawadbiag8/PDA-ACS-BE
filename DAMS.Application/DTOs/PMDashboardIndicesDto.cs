namespace DAMS.Application.DTOs
{
    public class PMDashboardIndicesDto
    {
        // Total Assets Monitored Section
        public double OverallComplianceIndex { get; set; }
        public double AccessibilityIndex { get; set; }
        public double AvailabilityIndex { get; set; }
        public double NavigationIndex { get; set; }
        public double PerformanceIndex { get; set; }
        public double SecurityIndex { get; set; }
        public double UserExperienceIndex { get; set; }
        
        // Traffic Overview Section
        public long? TotalVisits { get; set; }
        public long? UniqueVisitors { get; set; }
    }
}
