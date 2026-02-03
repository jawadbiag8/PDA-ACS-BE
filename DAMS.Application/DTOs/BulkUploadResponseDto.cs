namespace DAMS.Application.DTOs
{
    public class BulkUploadResponseDto
    {
        public int TotalRows { get; set; }
        public int SuccessfulCount { get; set; }
        public int FailedCount { get; set; }
        public List<BulkUploadErrorDto> Errors { get; set; } = new List<BulkUploadErrorDto>();
    }

    public class BulkUploadErrorDto
    {
        public int RowNumber { get; set; }
        public string AssetName { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
