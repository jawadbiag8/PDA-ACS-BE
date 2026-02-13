# Asset Control Panel – How Compliance Is Calculated

## Data source (current implementation)

- **Table used:** `KPIsResults` only. **`KPIsResultHistories` is not used** for the control panel.
- **Scope:** For each asset we load **all rows** in `KPIsResults` for that `AssetId` (no date filter, no “latest only”). For each KPI we then take all rows with that `KpiId`.
- So: if `KPIsResults` has one row per (AssetId, KpiId), we use that one row; if it has many (e.g. a log), we use **all** of them for the logic below.

---

## Per-KPI: how “current value” is computed

| KPI Id | KPI Name (from seed) | How current value is computed | Uses |
|--------|----------------------|-------------------------------|------|
| **1** | Website completely down (no response) | **Hit rate %** = (count of rows where outcome = hit) / (total rows) × 100. Hit = `Target` is "hit"/"pass" or `Result` is "hit"/"pass" (Target "miss"/"fail" = not hit). | All rows |
| **2** | DNS resolution failure | Same as KPI 1: **hit rate %** (hit/total × 100). | All rows |
| **3** | Hosting/network outage | **Count of misses** = number of rows where `Result` or `Target` is "miss"/"fail"/"false". | All rows |
| **4** | Partial outage (homepage loads, inner pages fail) | **Count of misses** (same rule as KPI 3). | All rows |
| **5** | Intermittent availability (flapping) | **Count of misses** (same rule as KPI 3). | All rows |
| **6** | Slow page load | **Average** of numeric `Result` values (e.g. seconds). Unit from target (e.g. "sec"). | All rows |
| **7** | Backend response time | **Average** of numeric `Result` values (seconds). | All rows |
| **8** | Heavy pages consuming excessive data | **Average** of numeric `Result` values (size). Unit from target (e.g. MB). | All rows |
| **9–14** | (SSL, browser security, mixed content, redirects, privacy, WCAG, etc.) | **Count of misses** (same as KPI 3). | All rows |
| **15** | (from seed: one of accessibility/download) | **Average** of numeric `Result` values in **%**. | All rows |
| **16, 17, 18** | (e.g. Missing form label, Images missing alt text, Poor color contrast) | **Hit rate %** (same as KPI 1: hit/total × 100). | All rows |
| **19, 20, 21, 22, 24** | (e.g. Download success, broken links, broken CSS/JS, search, internal links, circular nav) | **Count of misses** (same as KPI 3). Note: KPI 20 is “count of misses”, not “average %”. | All rows |
| **23** | (e.g. one of accessibility %) | **Average** of numeric `Result` values in **%**. | All rows |
| **25+** | Manual / other | Fallback: **average** from `CalculateAverageCurrentValue` (outcome type–dependent). | All rows |

---

## SLA status (all KPIs)

- **Inputs:** `currentValue` (from above) and `target` (from KpisLov by citizen impact: HIGH → TargetHigh, MEDIUM → TargetMedium, LOW → TargetLow).
- **Rules:**
  - **Higher is better** (KPI 1, 2, 15, 23): `current >= target` → COMPLIANT, else NON-COMPLIANT.
  - **Lower is better** (all other KPIs): `current <= target` → COMPLIANT, else NON-COMPLIANT.
  - If both current and target are 0 → COMPLIANT.
  - If current or target is N/A or not parseable → UNKNOWN.

---

## Summary table (quick reference)

| KPI Id | Current value = | SLA: higher or lower better? |
|--------|------------------|------------------------------|
| 1, 2 | Hit rate % (hit/total × 100) | Higher |
| 3, 4, 5, 9, 10, 11, 12, 13, 14, 19, 20, 21, 22, 24 | Count of misses | Lower |
| 6, 7 | Average (sec) | Lower |
| 8 | Average (MB) | Lower |
| 15, 23 | Average (%) | Higher |
| 16, 17, 18 | Hit rate % | Lower (comment in code: error % style) |

---

## What is *not* used today

- **KPIsResultHistories** is not used for control panel current value or SLA. Only **KPIsResults** is used.
- There is **no “latest only”** path for compliance: for each KPI we use **all** rows in `KPIsResults` for that asset + KpiId (no date filter or “last N” limit).

You can use this to say which KPIs should instead use **history**, or **latest value only**, or a **time window** (e.g. last 7 days), and we can adjust the implementation accordingly.
