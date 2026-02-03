using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAMS.Domain.Entities
{
    public class AssetMetrics
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int AssetId { get; set; }

        public double AccessibilityIndex { get; set; }
        public double AvailabilityIndex { get; set; }
        public double NavigationIndex { get; set; }
        public double PerformanceIndex { get; set; }
        public double SecurityIndex { get; set; }
        public double UserExperienceIndex { get; set; }
        public double CitizenHappinessMetric { get; set; }
        public double OverallComplianceMetric { get; set; }
        public double DigitalRiskExposureIndex { get; set; }
        public int CurrentHealth { get; set; }

        public DateTime PeriodStartDate { get; set; }
        public DateTime PeriodEndDate { get; set; }
        public DateTime CalculatedAt { get; set; }

        // Navigation property
        public Asset Asset { get; set; } = null!;
    }
}
