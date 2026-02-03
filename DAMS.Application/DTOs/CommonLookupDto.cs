namespace DAMS.Application.DTOs
{
    public class CommonLookupDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }

    public class CreateCommonLookupDto
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }

    public class UpdateCommonLookupDto
    {
        public string? Name { get; set; }
        public string? Type { get; set; }
    }
}
