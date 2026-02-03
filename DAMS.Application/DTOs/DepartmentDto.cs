namespace DAMS.Application.DTOs
{
    public class DepartmentDto
    {
        public int Id { get; set; }
        public int MinistryId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string ContactName { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class CreateDepartmentDto
    {
        public int MinistryId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string? ContactName { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
    }

    public class UpdateDepartmentDto
    {
        public int? MinistryId { get; set; }
        public string? DepartmentName { get; set; }
        public string? ContactName { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
    }
}
