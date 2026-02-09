namespace DAMS.Application.Models
{
    public class APIResponse
    {
        public bool IsSuccessful { get; set; }
        public string? Message { get; set; }
        public object? Data { get; set; }
    }

    /// <summary>Used only for GET incident details endpoint; includes KpiDetails.</summary>
    public class IncidentDetailsApiResponse : APIResponse
    {
        public object? KpiDetails { get; set; }
    }
}
