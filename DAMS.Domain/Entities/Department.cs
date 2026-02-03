namespace DAMS.Domain.Entities
{
    public class Department : BaseEntity
    {
        public int MinistryId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string ContactName { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;

        // Navigation properties
        public Ministry Ministry { get; set; } = null!;
        public ICollection<Asset> Assets { get; set; } = new List<Asset>();
    }
}
