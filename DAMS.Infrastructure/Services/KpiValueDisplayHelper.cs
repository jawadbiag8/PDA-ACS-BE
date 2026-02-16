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

    /// <summary>Appends the correct metric unit for calculated KPIs (same as incident creation): 6,7 = sec; 8 = MB; 15,16,17,18,23 = %.
    /// Values that are whole numbers (0, 100, 5, etc.) are shown without decimals (e.g. "0%", "100%", "0 MB", "5 sec").</summary>
    public static string FormatCalculatedValueWithUnit(int kpiId, string rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue)) return rawValue;
        var v = rawValue.Trim();
        var valuePart = StripUnit(v);
        var numStr = NormalizeNumericDisplay(valuePart);
        // If not numeric (e.g. "N/A"), return original without forcing a unit
        if (!double.TryParse(valuePart, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out _))
            return v;
        if (kpiId == 6 || kpiId == 7)
            return numStr + " sec";
        if (kpiId == 8)
            return numStr + " MB";
        if (kpiId == 15 || kpiId == 16 || kpiId == 17 || kpiId == 18 || kpiId == 23)
            return numStr + "%";
        return v;
    }

    /// <summary>Strip trailing unit (% or MB or sec) for parsing; returns the rest as-is.</summary>
    private static string StripUnit(string v)
    {
        if (v.EndsWith("%")) return v[..^1].Trim();
        if (v.EndsWith("MB", StringComparison.OrdinalIgnoreCase)) return v[..^2].Trim();
        if (v.EndsWith("sec", StringComparison.OrdinalIgnoreCase)) return v[..^3].Trim();
        if (v.EndsWith("seconds", StringComparison.OrdinalIgnoreCase)) return v[..^7].Trim();
        return v;
    }

    /// <summary>Format numeric string for display: whole numbers (0, 100, 5, etc.) without decimals; otherwise two decimals.</summary>
    private static string NormalizeNumericDisplay(string valuePart)
    {
        if (string.IsNullOrWhiteSpace(valuePart)) return valuePart;
        if (!double.TryParse(valuePart, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var num))
            return valuePart;
        if (Math.Abs(num - Math.Round(num)) < 1e-9)
            return ((int)Math.Round(num)).ToString(System.Globalization.CultureInfo.InvariantCulture);
        return num.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
    }
}
