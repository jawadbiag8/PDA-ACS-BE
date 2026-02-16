namespace DAMS.Application;

/// <summary>Topic names for SignalR data-update notifications. Client joins a topic then refetches the matching REST endpoint when notified.</summary>
public static class DataUpdateTopics
{
    /// <summary>GET /api/AdminDashboard/summary</summary>
    public const string AdminDashboardSummary = "AdminDashboard.Summary";

    /// <summary>GET /api/Incident/{id}</summary>
    public static string Incident(int id) => $"Incident.{id}";

    /// <summary>GET /api/Asset/{id}/controlpanel</summary>
    public static string AssetControlPanel(int assetId) => $"Asset.{assetId}.ControlPanel";

    /// <summary>Any KPI-related data for this asset (e.g. manual-from-asset). Notify when asset or KPI results for this asset change; client refetches GET /api/KpisLov/manual-from-asset/{assetId}?kpiId=X for each KPI it needs.</summary>
    public static string AssetKpisLov(int assetId) => $"Asset.{assetId}.KpisLov";
}
