namespace DAMS.Infrastructure.Services;

/// <summary>Shared logic for KPI value display (incident reasoning / report). Returns only the current value string.</summary>
internal static class KpiValueDisplayHelper
{
    private static readonly HashSet<int> CalculationKpiIds = new() { 6, 7, 8, 15, 16, 17, 18, 23 };

    /// <summary>Returns the display string for the current value. Static KPIs use hardcoded strings; calculation KPIs use historyResult with unit suffix.</summary>
    public static string GetCurrentValueDisplay(int kpiId, bool isFailure, string? historyResult)
    {
        switch (kpiId)
        {
            case 1: return isFailure ? "Down" : "Up";
            case 2: return isFailure ? "DNS failed" : "No DNS failure";
            case 3: return isFailure ? "Hosting outage detected" : "No hosting outage";
            case 4: return isFailure ? "Partial outage detected" : "No partial outage";
            case 5: return isFailure ? "Flapping detected" : "No flapping";
            case 9: return isFailure ? "Not using HTTPS" : "Using HTTPS";
            case 10: return isFailure ? "Expired/Missing certificate" : "Valid certificate";
            case 11: return isFailure ? "Warnings detected" : "No warnings";
            case 12: return isFailure ? "Warnings detected" : "No warnings";
            case 13: return isFailure ? "Suspicious redirects detected" : "No suspicious redirects";
            case 14: return isFailure ? "Not available" : "Available";
            case 19: return isFailure ? "Failed" : "Successful";
            case 20: return isFailure ? "Broken" : "Working";
            case 21: return isFailure ? "Broken" : "Working";
            case 22: return isFailure ? "Not available" : "Available";
            case 24: return isFailure ? "Circular navigation detected" : "No circular navigation";

            default:
                if (CalculationKpiIds.Contains(kpiId))
                    return FormatCalculatedValueWithUnit(kpiId, historyResult ?? string.Empty);
                return string.Empty;
        }
    }

    /// <summary>Appends the correct metric unit for calculated KPIs (same as incident creation): 6,7 = sec; 8 = MB; 15,16,17,18,23 = %.</summary>
    public static string FormatCalculatedValueWithUnit(int kpiId, string rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue)) return rawValue;
        var v = rawValue.Trim();
        if (kpiId == 6 || kpiId == 7)
        {
            if (v.EndsWith("sec", StringComparison.OrdinalIgnoreCase) || v.EndsWith("seconds", StringComparison.OrdinalIgnoreCase)) return v;
            return v + " sec";
        }
        if (kpiId == 8)
        {
            if (v.EndsWith("MB", StringComparison.OrdinalIgnoreCase)) return v;
            return v + " MB";
        }
        if (kpiId == 15 || kpiId == 16 || kpiId == 17 || kpiId == 18 || kpiId == 23)
        {
            if (v.EndsWith("%")) return v;
            return v + "%";
        }
        return v;
    }
}
