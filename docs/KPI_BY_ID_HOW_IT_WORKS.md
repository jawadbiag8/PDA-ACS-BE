# How Each KPI Works (Control Panel)

For each KPI: data source, how **current value** is calculated, and how **SLA status** is decided.  
**Skipped** records are excluded from all counts. **N/A** or no data → SLA = UNKNOWN.

---

## Hit/miss KPIs (20)

**Data source:** `KPIsResultHistories` — last 30 days for the asset (or all available in that window).  
**Current value:** `(hits / total) × 100` → shown as **"X.XX%"**.  
- **Total** = rows where `Target` is not `"skipped"`.  
- **Hits** = rows where `Target` is `"hit"` or `"pass"`.  

**SLA rule:** **Higher is better** — COMPLIANT when current % ≥ target %; 0% → always NON-COMPLIANT.

| Id | KPI Name | Category | Current value | SLA |
|----|----------|----------|---------------|-----|
| **1** | Website completely down (no response) | Availability & Reliability | Hit rate % (hits / total × 100) over last 30 days; skipped excluded | Higher is better; 0% → NON-COMPLIANT |
| **2** | DNS resolution failure | Availability & Reliability | Same as above | Same |
| **3** | Hosting/network outage | Availability & Reliability | Same as above | Same |
| **4** | Partial outage (homepage loads, inner pages fail) | Availability & Reliability | Same as above | Same |
| **5** | Intermittent availability (flapping) | Availability & Reliability | Same as above | Same |
| **9** | Website not using HTTPS | Security, Trust & Privacy | Same as above | Same |
| **10** | SSL certificate expired | Security, Trust & Privacy | Same as above | Same |
| **11** | Browser security warning | Security, Trust & Privacy | Same as above | Same |
| **12** | Mixed content warnings | Security, Trust & Privacy | Same as above | Same |
| **13** | Suspicious redirects | Security, Trust & Privacy | Same as above | Same |
| **14** | Privacy policy availability | Security, Trust & Privacy | Same as above | Same |
| **16** | Missing form label | Accessibility & Inclusivity | **% of misses** (misses/total×100); skipped excluded | **Lower is better** — COMPLIANT when current ≤ target |
| **17** | Images missing alt text | Accessibility & Inclusivity | Same (miss-based) | Same |
| **18** | Poor color contrast | Accessibility & Inclusivity | Same (miss-based) | Same |
| **19** | Download success rate | User Experience & Journey Quality | **Count of misses** (integer); skipped excluded | **Lower is better** — COMPLIANT when current ≤ target |
| **20** | Download links broken | User Experience & Journey Quality | Same (count of misses) | Same |
| **21** | Page loads but assets don't (broken CSS/JS) | User Experience & Journey Quality | **Count of misses** (integer); skipped excluded | **Lower is better** — COMPLIANT when current ≤ target |
| **22** | Search not available | Navigation & Discoverability | **% of misses** (misses/total×100); skipped excluded | **Lower is better** — COMPLIANT when current ≤ target |
| **24** | Circular navigation | Navigation & Discoverability | Hit rate % (same as 1, 2, …) | Higher is better; 0% → NON-COMPLIANT |

---

## Numeric KPIs (4)

**Data source:** Currently **KPIsResults** (all rows for the asset); numeric KPIs not yet switched to 30-day history.  
**Current value:** From **Result** column — average (or derived value) per existing logic.  
**Skipped:** Excluded before computing totals/averages where applicable.

### Lower is better (current ≤ target → COMPLIANT)

| Id | KPI Name | Category | Current value | SLA |
|----|----------|----------|---------------|-----|
| **6** | Slow page load | Performance & Efficiency | Average of numeric Result (sec) | Lower is better; current ≤ target → COMPLIANT |
| **7** | Backend response time | Performance & Efficiency | Average of numeric Result (sec) | Same |
| **8** | Heavy pages consuming excessive data | Performance & Efficiency | Average of numeric Result (MB) | Same |

### Higher is better (current ≥ target → COMPLIANT)

| Id | KPI Name | Category | Current value | SLA |
|----|----------|----------|---------------|-----|
| **15** | WCAG compliance score | Accessibility & Inclusivity | Average of numeric Result (%) | Higher is better; current ≥ target → COMPLIANT |

---

## Summary table (all 24)

| Id | KPI Name | Type | Current value logic | SLA rule |
|----|----------|------|---------------------|----------|
| 1 | Website completely down (no response) | Hit/miss | Hit rate % (last 30 days, no skipped) | Higher is better; 0% → NON-COMPLIANT |
| 2 | DNS resolution failure | Hit/miss | Same | Same |
| 3 | Hosting/network outage | Hit/miss | Same | Same |
| 4 | Partial outage (homepage loads, inner pages fail) | Hit/miss | Same | Same |
| 5 | Intermittent availability (flapping) | Hit/miss | Same | Same |
| 6 | Slow page load | Numeric | Avg Result (sec) | Lower is better |
| 7 | Backend response time | Numeric | Avg Result (sec) | Lower is better |
| 8 | Heavy pages consuming excessive data | Numeric | Avg Result (MB) | Lower is better |
| 9 | Website not using HTTPS | Hit/miss | Hit rate % | Higher is better; 0% → NON-COMPLIANT |
| 10 | SSL certificate expired | Hit/miss | Same | Same |
| 11 | Browser security warning | Hit/miss | Same | Same |
| 12 | Mixed content warnings | Hit/miss | Same | Same |
| 13 | Suspicious redirects | Hit/miss | Same | Same |
| 14 | Privacy policy availability | Hit/miss | Same | Same |
| 15 | WCAG compliance score | Numeric | Avg Result (%) | Higher is better |
| 16 | Missing form label | Hit/miss | % of misses (last 30 days) | Lower is better (current ≤ target → COMPLIANT) |
| 17 | Images missing alt text | Hit/miss | % of misses (last 30 days) | Lower is better (current ≤ target → COMPLIANT) |
| 18 | Poor color contrast | Hit/miss | % of misses (last 30 days) | Lower is better (current ≤ target → COMPLIANT) |
| 19 | Download success rate | Hit/miss | Count of misses (last 30 days) | Lower is better (current ≤ target → COMPLIANT) |
| 20 | Download links broken | Hit/miss | Count of misses (last 30 days) | Lower is better (current ≤ target → COMPLIANT) |
| 21 | Page loads but assets don't (broken CSS/JS) | Hit/miss | Count of misses | Lower is better (current ≤ target → COMPLIANT) |
| 22 | Search not available | Hit/miss | % of misses | Lower is better (current ≤ target → COMPLIANT) |
| 23 | Broken internal links | Hit/miss | % of misses | Lower is better (current ≤ target → COMPLIANT) |
| 24 | Circular navigation | Hit/miss | Hit rate % | Higher is better; 0% → NON-COMPLIANT |

---

## General rules (all KPIs)

- **Skipped:** Rows with `Target = "skipped"` are excluded from total and from hit/miss/numeric calculations. If all rows are skipped → effective total = 0 → current value **N/A**, SLA **UNKNOWN**.
- **N/A:** When current value is N/A (no data or all skipped), SLA = **UNKNOWN**.
- **Target:** Comes from KpisLov by citizen impact level (HIGH → TargetHigh, MEDIUM → TargetMedium, LOW → TargetLow).
