namespace DAMS.Application.DTOs
{
    public class MinistryComplianceDto
    {
        public int MinistryId { get; set; }
        public string MinistryName { get; set; } = string.Empty;
        public int Assets { get; set; }
        public double ComplianceIndex { get; set; }
    }
}
