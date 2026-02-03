namespace DAMS.Domain.Entities
{
    public class Ministry : BaseEntity
    {
        public string MinistryName { get; set; } = string.Empty;
        public string ContactName { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;

        // Navigation properties
        public ICollection<Department> Departments { get; set; } = new List<Department>();
        public ICollection<Asset> Assets { get; set; } = new List<Asset>();
    }
}
