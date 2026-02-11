namespace DAMS.Application.Models
{
    /// <summary>Result of generating a ministry report PDF.</summary>
    public class MinistryReportPdfResult
    {
        public bool Success { get; set; }
        public byte[]? PdfBytes { get; set; }
        public string? FileName { get; set; }
        public string? Message { get; set; }
    }
}
