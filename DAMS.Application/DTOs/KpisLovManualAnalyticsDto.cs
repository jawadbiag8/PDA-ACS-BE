namespace DAMS.Application.DTOs
{
    /// <summary>
    /// Analytics metrics from KPI Lov manual data (e.g. from asset URL).
    /// </summary>
    public class KpisLovManualAnalyticsDto
    {
        public long? TotalVisits { get; set; }
        public long? UniqueVisitors { get; set; }
        public long? PageViews { get; set; }
        public List<PageAccessDto>? TopAccessedPages { get; set; }
        public List<PageDistributionItemDto>? EntryPageDistribution { get; set; }
        public List<PageDistributionItemDto>? ExitPageDistribution { get; set; }
        /// <summary>Average session duration in seconds.</summary>
        public double? AverageSessionDurationSeconds { get; set; }
        /// <summary>Bounce rate as percentage (0–100).</summary>
        public double? BounceRate { get; set; }
        public List<UsageWindowDto>? PeakUsageWindows { get; set; }
        /// <summary>Today's visitors (e.g. parsed from HTML counter).</summary>
        public long? TodayVisits { get; set; }
        /// <summary>Active users (e.g. parsed from HTML counter).</summary>
        public long? ActiveUsers { get; set; }
    }

    public class PageAccessDto
    {
        public string Page { get; set; } = string.Empty;
        public string? Url { get; set; }
        public long Count { get; set; }
    }

    public class PageDistributionItemDto
    {
        public string Page { get; set; } = string.Empty;
        public string? Url { get; set; }
        public double Percentage { get; set; }
    }

    public class UsageWindowDto
    {
        public string Window { get; set; } = string.Empty;
        public string? StartTime { get; set; }
        public string? EndTime { get; set; }
        public long? Visits { get; set; }
        public double? Percentage { get; set; }
    }
}
