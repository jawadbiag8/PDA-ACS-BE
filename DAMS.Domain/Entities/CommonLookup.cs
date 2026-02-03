namespace DAMS.Domain.Entities
{
    public class CommonLookup : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;

        // Navigation properties
        public ICollection<Asset> Assets { get; set; } = new List<Asset>();
    }
}
