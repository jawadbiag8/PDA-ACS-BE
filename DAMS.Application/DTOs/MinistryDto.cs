namespace DAMS.Application.DTOs
{
    public class MinistryDto
    {
        public int Id { get; set; }
        public string MinistryName { get; set; } = string.Empty;
        public string ContactName { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class CreateMinistryDto
    {
        public string MinistryName { get; set; } = string.Empty;
        public string? ContactName { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
    }

    public class UpdateMinistryDto
    {
        public string? MinistryName { get; set; }
        public string? ContactName { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
    }
}
