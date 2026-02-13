# KPI calculation reference (control panel)

**Target** for all KPIs: from KpisLov by asset citizen impact level (HIGH → TargetHigh, MEDIUM → TargetMedium, LOW → TargetLow).  
**Skipped** rows are excluded from totals. **N/A** or no data → SLA = UNKNOWN.

| KPI Id | KPI name | Current value calculation | Data source | SLA rule |
|--------|----------|---------------------------|-------------|----------|
| **1** | Website completely down (no response) | (hits / total) × 100 → "X.XX%" | History, last 30 days | Target ≤ calculated → COMPLIANT |
| **2** | DNS resolution failure | (hits / total) × 100 → "X.XX%" | History, last 30 days | Target ≤ calculated → COMPLIANT |
| **3** | Hosting/network outage | (misses / total) × 100 → "X.XX%" | History, last 30 days | Target ≥ calculated → COMPLIANT |
| **4** | Partial outage (homepage loads, inner pages fail) | (misses / total) × 100 → "X.XX%" | History, last 30 days | Target ≥ calculated → COMPLIANT |
| **5** | Intermittent availability (flapping) | (misses / total) × 100 → "X.XX%" | History, last 30 days | Target ≥ calculated → COMPLIANT |
| **6** | Slow page load | AVG(Result), unit sec | KPIsResults | Target ≥ calculated → COMPLIANT (lower better) |
| **7** | Backend response time | AVG(Result), unit sec | KPIsResults | Target ≥ calculated → COMPLIANT (lower better) |
| **8** | Heavy pages consuming excessive data | AVG(Result), unit MB | KPIsResults | Target ≥ calculated → COMPLIANT (lower better) |
| **9** | Website not using HTTPS | (misses / total) × 100 → "X.XX%" | History, last 30 days | Target ≥ calculated → COMPLIANT |
| **10** | SSL certificate expired | (misses / total) × 100 → "X.XX%" | History, last 30 days | Target ≥ calculated → COMPLIANT |
| **11** | Browser security warning | (misses / total) × 100 → "X.XX%" | History, last 30 days | Target ≥ calculated → COMPLIANT |
| **12** | Mixed content warnings | (misses / total) × 100 → "X.XX%" | History, last 30 days | Target ≥ calculated → COMPLIANT |
| **13** | Suspicious redirects | (misses / total) × 100 → "X.XX%" | History, last 30 days | Target ≥ calculated → COMPLIANT |
| **14** | Privacy policy availability | (misses / total) × 100 → "X.XX%" | History, last 30 days | Target ≥ calculated → COMPLIANT |
| **15** | WCAG compliance score | AVG(Result) → "X.XX%" | History, last 30 days | Target ≤ calculated → COMPLIANT |
| **16** | Missing form label | (misses / total) × 100 → "X.XX%" | History, last 30 days | Target ≥ calculated → COMPLIANT |
| **17** | Images missing alt text | (misses / total) × 100 → "X.XX%" | History, last 30 days | Target ≥ calculated → COMPLIANT |
| **18** | Poor color contrast | (misses / total) × 100 → "X.XX%" | History, last 30 days | Target ≥ calculated → COMPLIANT |
| **19** | Download success rate | (misses / total) × 100 → "X.XX%" | History, last 30 days | Target ≥ calculated → COMPLIANT |
| **20** | Download links broken | (misses / total) × 100 → "X.XX%" | History, last 30 days | Target ≥ calculated → COMPLIANT |
| **21** | Page loads but assets don't (broken CSS/JS) | (misses / total) × 100 → "X.XX%" | History, last 30 days | Target ≥ calculated → COMPLIANT |
| **22** | Search not available | (misses / total) × 100 → "X.XX%" | History, last 30 days | Target ≥ calculated → COMPLIANT |
| **23** | Broken internal links | (misses / total) × 100 → "X.XX%" | History, last 30 days | Target ≥ calculated → COMPLIANT |
| **24** | Circular navigation | (misses / total) × 100 → "X.XX%" | History, last 30 days | Target ≥ calculated → COMPLIANT |

**Definitions**
- **total** = number of history rows (last 30 days) for that asset + KPI, excluding rows where `Target = "skipped"`.
- **hits** = rows where `Target` is "hit" or "pass".
- **misses** = total − hits.
- **History** = table `KPIsResultHistories` (last 30 days by `CreatedAt`).
