namespace DAMS.Application.Models
{
    public class APIResponse
    {
        public bool IsSuccessful { get; set; }
        public string? Message { get; set; }
        public object? Data { get; set; }
    }
}
