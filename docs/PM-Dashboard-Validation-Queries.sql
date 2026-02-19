-- =============================================================================
-- PM Executive Dashboard – Raw validation queries
-- Run these against your database to validate API results.
-- Table names: Assets, AssetMetrics, Ministries, Incidents, kpisResults, CommonLookup
-- Requires: MySQL 8+ (uses ROW_NUMBER() for "latest per asset").
--
-- Index:
--   1. TotalAssetsBeingMonitored
--   2. TotalMinistries
--   3. DigitalAssetsOffline (KpiId=1, Target='miss')
--   4. DigitalAssetsOnline (cross-check)
--   5. LastChecked (max UpdatedAt on kpisResults)
--   6. Latest AssetMetrics per asset (reference)
--   7. Current Digital Experience Score
--   8. Previous Digital Experience Score (~30 days ago)
--   9. Ministries meeting compliance (avg compliance >= 70)
--  10. Active incidents (status not closed/resolved)
--  11. Resolved incidents last 30 days
--  12. Assets vulnerable (has metrics, SecurityIndex < 70)
--  13. PM Dashboard Indices (averages)
--  14. Bottom 5 ministries by citizen impact
--  15. Top 5 ministries by compliance
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 1. Total assets being monitored (non-deleted)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS TotalAssetsBeingMonitored
FROM Assets
WHERE DeletedAt IS NULL;


-- -----------------------------------------------------------------------------
-- 2. Total ministries (non-deleted)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS TotalMinistries
FROM Ministries
WHERE DeletedAt IS NULL;


-- -----------------------------------------------------------------------------
-- 3. Digital assets OFFLINE (asset is offline when latest KpiId=1 result has Target = 'miss')
-- One row per asset: latest KPI 1 result; then count where Target = 'miss'
-- -----------------------------------------------------------------------------
WITH LatestKpi1 AS (
  SELECT k.AssetId,
         k.Target,
         ROW_NUMBER() OVER (PARTITION BY k.AssetId ORDER BY k.CreatedAt DESC) AS rn
  FROM kpisResults k
  INNER JOIN Assets a ON a.Id = k.AssetId AND a.DeletedAt IS NULL
  WHERE k.KpiId = 1
)
SELECT COUNT(*) AS DigitalAssetsOffline
FROM LatestKpi1
WHERE rn = 1 AND LOWER(TRIM(Target)) = 'miss';


-- -----------------------------------------------------------------------------
-- 4. Digital assets ONLINE (for cross-check: offline + online = total)
-- -----------------------------------------------------------------------------
WITH LatestKpi1 AS (
  SELECT k.AssetId,
         k.Target,
         ROW_NUMBER() OVER (PARTITION BY k.AssetId ORDER BY k.CreatedAt DESC) AS rn
  FROM kpisResults k
  INNER JOIN Assets a ON a.Id = k.AssetId AND a.DeletedAt IS NULL
  WHERE k.KpiId = 1
)
SELECT COUNT(*) AS DigitalAssetsOnline
FROM LatestKpi1
WHERE rn = 1 AND (LOWER(TRIM(Target)) <> 'miss' OR Target IS NULL);


-- -----------------------------------------------------------------------------
-- 5. Last checked (most recent UpdatedAt from kpisResults for monitored assets)
-- -----------------------------------------------------------------------------
SELECT MAX(k.UpdatedAt) AS LastChecked
FROM kpisResults k
INNER JOIN Assets a ON a.Id = k.AssetId AND a.DeletedAt IS NULL;


-- -----------------------------------------------------------------------------
-- 6. Latest AssetMetrics per asset (used in several calculations below)
-- -----------------------------------------------------------------------------
WITH LatestMetrics AS (
  SELECT m.*,
         ROW_NUMBER() OVER (PARTITION BY m.AssetId ORDER BY m.CalculatedAt DESC) AS rn
  FROM AssetMetrics m
  INNER JOIN Assets a ON a.Id = m.AssetId AND a.DeletedAt IS NULL
)
SELECT AssetId, CurrentHealth, PerformanceIndex, OverallComplianceMetric, SecurityIndex,
       AccessibilityIndex, AvailabilityIndex, NavigationIndex, UserExperienceIndex, CitizenHappinessMetric,
       CalculatedAt
FROM LatestMetrics
WHERE rn = 1;


-- -----------------------------------------------------------------------------
-- 7. Digital Experience Score (current) – average of (Health + Performance + Compliance)/3
--    only over assets where at least one of Health, Performance, Compliance > 0
-- -----------------------------------------------------------------------------
WITH LatestMetrics AS (
  SELECT m.AssetId,
         m.CurrentHealth AS HealthIndex,
         m.PerformanceIndex,
         m.OverallComplianceMetric AS ComplianceIndex,
         ROW_NUMBER() OVER (PARTITION BY m.AssetId ORDER BY m.CalculatedAt DESC) AS rn
  FROM AssetMetrics m
  INNER JOIN Assets a ON a.Id = m.AssetId AND a.DeletedAt IS NULL
),
WithScore AS (
  SELECT AssetId,
         (CAST(HealthIndex AS DOUBLE) + PerformanceIndex + ComplianceIndex) / 3.0 AS Score
  FROM LatestMetrics
  WHERE rn = 1
    AND (HealthIndex > 0 OR PerformanceIndex > 0 OR ComplianceIndex > 0)
)
SELECT COALESCE(AVG(Score), 0) AS CurrentDigitalExperienceScore
FROM WithScore;


-- -----------------------------------------------------------------------------
-- 8. Digital Experience Score (previous, ~30 days ago)
--    Latest metric per asset where CalculatedAt <= (NOW() - 30 days)
-- -----------------------------------------------------------------------------
WITH ThirtyDaysAgo AS (SELECT DATE_SUB(NOW(), INTERVAL 30 DAY) AS cutoff),
PreviousMetrics AS (
  SELECT m.AssetId,
         (CAST(m.CurrentHealth AS DOUBLE) + m.PerformanceIndex + m.OverallComplianceMetric) / 3.0 AS Score,
         ROW_NUMBER() OVER (PARTITION BY m.AssetId ORDER BY m.CalculatedAt DESC) AS rn
  FROM AssetMetrics m
  INNER JOIN Assets a ON a.Id = m.AssetId AND a.DeletedAt IS NULL
  CROSS JOIN ThirtyDaysAgo t
  WHERE m.CalculatedAt <= t.cutoff
),
WithPreviousScore AS (
  SELECT Score FROM PreviousMetrics WHERE rn = 1 AND (Score > 0)
)
SELECT COALESCE(AVG(Score), 0) AS PreviousDigitalExperienceScore
FROM WithPreviousScore;


-- -----------------------------------------------------------------------------
-- 9. Ministries meeting compliance standards (avg compliance per ministry >= 70)
--    Only ministries that have at least one asset with OverallComplianceMetric > 0
-- -----------------------------------------------------------------------------
WITH LatestMetrics AS (
  SELECT m.AssetId, m.OverallComplianceMetric, a.MinistryId,
         ROW_NUMBER() OVER (PARTITION BY m.AssetId ORDER BY m.CalculatedAt DESC) AS rn
  FROM AssetMetrics m
  INNER JOIN Assets a ON a.Id = m.AssetId AND a.DeletedAt IS NULL
  INNER JOIN Ministries mn ON mn.Id = a.MinistryId AND mn.DeletedAt IS NULL
),
MinistryCompliance AS (
  SELECT MinistryId,
         AVG(OverallComplianceMetric) AS AvgCompliance
  FROM LatestMetrics
  WHERE rn = 1 AND OverallComplianceMetric > 0
  GROUP BY MinistryId
)
SELECT COUNT(*) AS MinistriesMeetComplianceStandards
FROM MinistryCompliance
WHERE AvgCompliance >= 70;


-- -----------------------------------------------------------------------------
-- 10. Active incidents (status not 'closed' and not 'resolved')
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS ActiveIncidents
FROM Incidents i
INNER JOIN CommonLookup s ON s.Id = i.StatusId
WHERE i.DeletedAt IS NULL
  AND s.DeletedAt IS NULL
  AND LOWER(TRIM(s.Name)) NOT IN ('closed', 'resolved');


-- -----------------------------------------------------------------------------
-- 11. Resolved incidents in last 30 days (status closed or resolved, UpdatedAt >= 30 days ago)
-- -----------------------------------------------------------------------------
SELECT COUNT(*) AS ResolvedIncidentsLast30Days
FROM Incidents i
INNER JOIN CommonLookup s ON s.Id = i.StatusId
WHERE i.DeletedAt IS NULL
  AND s.DeletedAt IS NULL
  AND LOWER(TRIM(s.Name)) IN ('closed', 'resolved')
  AND (i.UpdatedAt >= DATE_SUB(NOW(), INTERVAL 30 DAY));


-- -----------------------------------------------------------------------------
-- 12. Assets vulnerable (dashboard: latest metric PER ASSET, SecurityIndex < 70 strictly)
--     Use this query to match the dashboard. Simple COUNT(*) counts ROWS and can differ
--     if you have multiple AssetMetrics rows per asset (or use <= 70 instead of < 70).
-- -----------------------------------------------------------------------------
WITH LatestMetrics AS (
  SELECT m.AssetId, m.SecurityIndex,
         ROW_NUMBER() OVER (PARTITION BY m.AssetId ORDER BY m.CalculatedAt DESC) AS rn
  FROM AssetMetrics m
  INNER JOIN Assets a ON a.Id = m.AssetId AND a.DeletedAt IS NULL
)
SELECT COUNT(*) AS AssetsAreVulnerable
FROM LatestMetrics
WHERE rn = 1 AND SecurityIndex < 70;

-- Simple row count (only matches dashboard if one row per asset AND you use < 70):
-- SELECT COUNT(*) FROM AssetMetrics WHERE SecurityIndex < 70;


-- -----------------------------------------------------------------------------
-- 13. PM Dashboard Indices – averages (only over assets with that metric > 0)
-- -----------------------------------------------------------------------------
WITH LatestMetrics AS (
  SELECT m.*,
         ROW_NUMBER() OVER (PARTITION BY m.AssetId ORDER BY m.CalculatedAt DESC) AS rn
  FROM AssetMetrics m
  INNER JOIN Assets a ON a.Id = m.AssetId AND a.DeletedAt IS NULL
)
SELECT
  ROUND(AVG(CASE WHEN OverallComplianceMetric > 0 THEN OverallComplianceMetric END), 2) AS OverallComplianceIndex,
  ROUND(AVG(CASE WHEN AccessibilityIndex > 0 THEN AccessibilityIndex END), 2) AS AccessibilityIndex,
  ROUND(AVG(CASE WHEN AvailabilityIndex > 0 THEN AvailabilityIndex END), 2) AS AvailabilityIndex,
  ROUND(AVG(CASE WHEN NavigationIndex > 0 THEN NavigationIndex END), 2) AS NavigationIndex,
  ROUND(AVG(CASE WHEN PerformanceIndex > 0 THEN PerformanceIndex END), 2) AS PerformanceIndex,
  ROUND(AVG(CASE WHEN SecurityIndex > 0 THEN SecurityIndex END), 2) AS SecurityIndex,
  ROUND(AVG(CASE WHEN UserExperienceIndex > 0 THEN UserExperienceIndex END), 2) AS UserExperienceIndex
FROM LatestMetrics
WHERE rn = 1;


-- -----------------------------------------------------------------------------
-- 14. Bottom N ministries by citizen impact (lowest CitizenHappinessMetric), e.g. N=5
-- -----------------------------------------------------------------------------
WITH LatestMetrics AS (
  SELECT m.AssetId, m.CitizenHappinessMetric, a.MinistryId,
         ROW_NUMBER() OVER (PARTITION BY m.AssetId ORDER BY m.CalculatedAt DESC) AS rn
  FROM AssetMetrics m
  INNER JOIN Assets a ON a.Id = m.AssetId AND a.DeletedAt IS NULL
),
MinistryHappiness AS (
  SELECT mn.Id AS MinistryId,
         mn.MinistryName,
         COUNT(DISTINCT a.Id) AS Assets,
         ROUND(AVG(CASE WHEN lm.CitizenHappinessMetric > 0 THEN lm.CitizenHappinessMetric END), 2) AS CitizenHappinessIndex
  FROM Ministries mn
  INNER JOIN Assets a ON a.MinistryId = mn.Id AND a.DeletedAt IS NULL
  LEFT JOIN LatestMetrics lm ON lm.AssetId = a.Id AND lm.rn = 1
  WHERE mn.DeletedAt IS NULL
  GROUP BY mn.Id, mn.MinistryName
  HAVING COUNT(DISTINCT a.Id) > 0
)
SELECT MinistryId, MinistryName, Assets, COALESCE(CitizenHappinessIndex, 0) AS CitizenHappinessIndex
FROM MinistryHappiness
ORDER BY COALESCE(CitizenHappinessIndex, 0) ASC
LIMIT 5;


-- -----------------------------------------------------------------------------
-- 15. Top N ministries by compliance (highest OverallComplianceMetric), e.g. N=5
-- -----------------------------------------------------------------------------
WITH LatestMetrics AS (
  SELECT m.AssetId, m.OverallComplianceMetric, a.MinistryId,
         ROW_NUMBER() OVER (PARTITION BY m.AssetId ORDER BY m.CalculatedAt DESC) AS rn
  FROM AssetMetrics m
  INNER JOIN Assets a ON a.Id = m.AssetId AND a.DeletedAt IS NULL
),
MinistryCompliance AS (
  SELECT mn.Id AS MinistryId,
         mn.MinistryName,
         COUNT(DISTINCT a.Id) AS Assets,
         ROUND(AVG(CASE WHEN lm.OverallComplianceMetric > 0 THEN lm.OverallComplianceMetric END), 2) AS ComplianceIndex
  FROM Ministries mn
  INNER JOIN Assets a ON a.MinistryId = mn.Id AND a.DeletedAt IS NULL
  LEFT JOIN LatestMetrics lm ON lm.AssetId = a.Id AND lm.rn = 1
  WHERE mn.DeletedAt IS NULL
  GROUP BY mn.Id, mn.MinistryName
  HAVING COUNT(DISTINCT a.Id) > 0
)
SELECT MinistryId, MinistryName, Assets, COALESCE(ComplianceIndex, 0) AS ComplianceIndex
FROM MinistryCompliance
ORDER BY COALESCE(ComplianceIndex, 0) DESC
LIMIT 5;
