namespace DAMS.Application.DTOs
{
    public class MinistryDetailsDto
    {
        public int MinistryId { get; set; }
        public string MinistryName { get; set; } = string.Empty;
        public int NumberOfAssets { get; set; }
        public List<MinistryDetailsAssetDto> Assets { get; set; } = new();
    }

    public class MinistryDetailsAssetDto
    {
        public int AssetId { get; set; }
        public string AssetName { get; set; } = string.Empty;
        public string CurrentStatus { get; set; } = "UNKNOWN";
        public int OpenIncidentCount { get; set; }
        public int ClosedIncidentCount { get; set; }
    }
}
