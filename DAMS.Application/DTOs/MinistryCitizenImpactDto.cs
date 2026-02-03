namespace DAMS.Application.DTOs
{
    public class MinistryCitizenImpactDto
    {
        public int MinistryId { get; set; }
        public string MinistryName { get; set; } = string.Empty;
        public int Assets { get; set; }
        public double CitizenHappinessIndex { get; set; }
    }
}
