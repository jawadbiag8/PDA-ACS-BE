using DAMS.Application.DTOs;
using DAMS.Application.Interfaces;
using DAMS.Application.Models;
using DAMS.Domain.Entities;
using DAMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Collections.Generic;

namespace DAMS.Infrastructure.Services
{
    public class PMDashboardService : IPMDashboardService
    {
        private readonly ApplicationDbContext _context;

        public PMDashboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<APIResponse> GetPMDashboardHeaderAsync()
        {
            // Get all assets with related data
            var assets = await _context.Assets
                .Include(a => a.KPIsResults)
                    .ThenInclude(k => k.KpisLov)
                .Include(a => a.Incidents)
                    .ThenInclude(i => i.Severity)
                .Include(a => a.Incidents)
                    .ThenInclude(i => i.Status)
                .Include(a => a.Ministry)
                .Where(a => a.DeletedAt == null)
                .ToListAsync();

            var totalAssets = assets.Count;
            var assetIds = assets.Select(a => a.Id).ToList();

            // Get latest AssetMetrics for all assets
            var latestMetrics = await _context.AssetMetrics
                .Where(m => assetIds.Contains(m.AssetId))
                .GroupBy(m => m.AssetId)
                .Select(g => g.OrderByDescending(m => m.CalculatedAt).First())
                .ToListAsync();

            var metricsDict = latestMetrics.ToDictionary(m => m.AssetId);

            // Get metrics from 30 days ago for comparison (for Digital Experience Score change)
            var thirtyDaysAgo = DateTime.Now.AddDays(-30);
            var previousMetrics = await _context.AssetMetrics
                .Where(m => assetIds.Contains(m.AssetId) && m.CalculatedAt <= thirtyDaysAgo)
                .GroupBy(m => m.AssetId)
                .Select(g => g.OrderByDescending(m => m.CalculatedAt).First())
                .ToListAsync();

            var previousMetricsDict = previousMetrics.ToDictionary(m => m.AssetId);

            // Build asset metrics list
            var assetMetricsList = new List<dynamic>();
            foreach (var asset in assets)
            {
                metricsDict.TryGetValue(asset.Id, out var metrics);
                previousMetricsDict.TryGetValue(asset.Id, out var previousMetric);

                var healthIndex = metrics?.CurrentHealth ?? 0;
                var performanceIndex = metrics?.PerformanceIndex ?? 0.0;
                var complianceIndex = metrics?.OverallComplianceMetric ?? 0.0;
                var securityIndex = metrics?.SecurityIndex ?? 0.0;

                // Check if asset is online
                var latestHealthKpi = asset.KPIsResults
                    .Where(k => k.KpiId == 1)
                    .OrderByDescending(k => k.CreatedAt)
                    .FirstOrDefault();
                var isOnline = latestHealthKpi != null && IsAssetUp(latestHealthKpi);

                assetMetricsList.Add(new
                {
                    HealthIndex = (double)healthIndex,
                    PerformanceIndex = performanceIndex,
                    ComplianceIndex = complianceIndex,
                    SecurityIndex = securityIndex,
                    IsOnline = isOnline,
                    MinistryId = asset.MinistryId,
                    PreviousHealthIndex = (double)(previousMetric?.CurrentHealth ?? 0),
                    PreviousPerformanceIndex = previousMetric?.PerformanceIndex ?? 0.0,
                    PreviousComplianceIndex = previousMetric?.OverallComplianceMetric ?? 0.0
                });
            }

            // Calculate Digital Experience Score (average of Health, Performance, and Compliance indices)
            var assetsWithData = assetMetricsList.Where(m => 
                m.HealthIndex > 0 || m.PerformanceIndex > 0 || m.ComplianceIndex > 0).ToList();
            
            var currentDigitalExperienceScore = assetsWithData.Any()
                ? assetsWithData.Average(m => 
                    ((double)m.HealthIndex + (double)m.PerformanceIndex + (double)m.ComplianceIndex) / 3.0)
                : 0.0;

            // Calculate previous Digital Experience Score
            var previousAssetsWithData = assetMetricsList.Where(m => 
                m.PreviousHealthIndex > 0 || m.PreviousPerformanceIndex > 0 || m.PreviousComplianceIndex > 0).ToList();
            
            var previousDigitalExperienceScore = previousAssetsWithData.Any()
                ? previousAssetsWithData.Average(m => 
                    ((double)m.PreviousHealthIndex + (double)m.PreviousPerformanceIndex + (double)m.PreviousComplianceIndex) / 3.0)
                : 0.0;

            var digitalExperienceScoreChange = currentDigitalExperienceScore - previousDigitalExperienceScore;

            // Calculate offline assets
            var assetsOnline = assetMetricsList.Count(m => m.IsOnline);
            var digitalAssetsOffline = totalAssets - assetsOnline;

            // Get last checked time (most recent KPI result timestamp)
            var lastChecked = await _context.KPIsResults
                .OrderByDescending(k => k.UpdatedAt)
                .Where(x=>x.Target == "miss" && x.KpiId == 1)
                .Select(k => k.UpdatedAt)
                .FirstOrDefaultAsync();

            // Get all ministries and calculate compliance scores
            var ministries = await _context.Ministries
                .Where(m => m.DeletedAt == null)
                .ToListAsync();

            var totalMinistries = ministries.Count;
            var ministriesMeetComplianceStandards = 0;

            foreach (var ministry in ministries)
            {
                var ministryAssets = assetMetricsList
                    .Where(m => m.MinistryId == ministry.Id)
                    .ToList();

                if (ministryAssets.Any())
                {
                    var ministryAssetsWithCompliance = ministryAssets
                        .Where(m => m.ComplianceIndex > 0)
                        .ToList();

                    var ministryComplianceScore = ministryAssetsWithCompliance.Any()
                        ? ministryAssetsWithCompliance.Average(m => (double)m.ComplianceIndex)
                        : 0.0;

                    if (ministryComplianceScore >= 70.0)
                    {
                        ministriesMeetComplianceStandards++;
                    }
                }
            }

            // Get active incidents (open/unresolved)
            var activeIncidents = await _context.Incidents
                .Include(i => i.Status)
                .Where(i => i.DeletedAt == null &&
                           i.Status != null &&
                           i.Status.Name != null &&
                           i.Status.Name.ToLower() != "closed" &&
                           i.Status.Name.ToLower() != "resolved")
                .CountAsync();

            // Get resolved incidents in last 30 days
            var thirtyDaysAgoDate = DateTime.Now.AddDays(-30);
            var resolvedIncidentsLast30Days = await _context.Incidents
                .Include(i => i.Status)
                .Where(i => i.DeletedAt == null &&
                           i.Status != null &&
                           (i.Status.Name.ToLower() == "closed" || i.Status.Name.ToLower() == "resolved") &&
                           i.UpdatedAt >= thirtyDaysAgoDate)
                .CountAsync();

            // Count vulnerable assets: total number of assets with SecurityIndex < 70 (from AssetMetrics)
            var assetsAreVulnerable = assetMetricsList.Count(m => m.SecurityIndex < 70.0);

            var header = new PMDashboardHeaderDto
            {
                DigitalExperienceScore = Math.Round(currentDigitalExperienceScore, 2),
                DigitalExperienceScoreChange = Math.Round(digitalExperienceScoreChange, 2),
                TotalAssetsBeingMonitored = totalAssets,
                TotalMinistries = totalMinistries,
                DigitalAssetsOffline = digitalAssetsOffline,
                LastChecked = lastChecked,
                MinistriesMeetComplianceStandards = ministriesMeetComplianceStandards,
                ComplianceThreshold = 70.0,
                ActiveIncidents = activeIncidents,
                ResolvedIncidentsLast30Days = resolvedIncidentsLast30Days,
                AssetsAreVulnerable = assetsAreVulnerable,
                SecurityThreshold = 70.0
            };

            return new APIResponse
            {
                IsSuccessful = true,
                Message = "PM Dashboard header retrieved successfully",
                Data = header
            };
        }

        public async Task<APIResponse> GetPMDashboardIndicesAsync()
        {
            // Get all assets with related data
            var assets = await _context.Assets
                .Where(a => a.DeletedAt == null)
                .ToListAsync();

            var assetIds = assets.Select(a => a.Id).ToList();

            // Get latest AssetMetrics for all assets
            var latestMetrics = await _context.AssetMetrics
                .Where(m => assetIds.Contains(m.AssetId))
                .GroupBy(m => m.AssetId)
                .Select(g => g.OrderByDescending(m => m.CalculatedAt).First())
                .ToListAsync();

            // Calculate average indices across all assets (only for assets with non-zero values)
            var assetsWithOverallCompliance = latestMetrics.Where(m => m.OverallComplianceMetric > 0).ToList();
            var overallComplianceIndex = assetsWithOverallCompliance.Any()
                ? assetsWithOverallCompliance.Average(m => m.OverallComplianceMetric)
                : 0.0;

            var assetsWithAccessibility = latestMetrics.Where(m => m.AccessibilityIndex > 0).ToList();
            var accessibilityIndex = assetsWithAccessibility.Any()
                ? assetsWithAccessibility.Average(m => m.AccessibilityIndex)
                : 0.0;

            var assetsWithAvailability = latestMetrics.Where(m => m.AvailabilityIndex > 0).ToList();
            var availabilityIndex = assetsWithAvailability.Any()
                ? assetsWithAvailability.Average(m => m.AvailabilityIndex)
                : 0.0;

            var assetsWithNavigation = latestMetrics.Where(m => m.NavigationIndex > 0).ToList();
            var navigationIndex = assetsWithNavigation.Any()
                ? assetsWithNavigation.Average(m => m.NavigationIndex)
                : 0.0;

            var assetsWithPerformance = latestMetrics.Where(m => m.PerformanceIndex > 0).ToList();
            var performanceIndex = assetsWithPerformance.Any()
                ? assetsWithPerformance.Average(m => m.PerformanceIndex)
                : 0.0;

            var assetsWithSecurity = latestMetrics.Where(m => m.SecurityIndex > 0).ToList();
            var securityIndex = assetsWithSecurity.Any()
                ? assetsWithSecurity.Average(m => m.SecurityIndex)
                : 0.0;

            var assetsWithUserExperience = latestMetrics.Where(m => m.UserExperienceIndex > 0).ToList();
            var userExperienceIndex = assetsWithUserExperience.Any()
                ? assetsWithUserExperience.Average(m => m.UserExperienceIndex)
                : 0.0;

            // Traffic Overview - Currently not available, set to null
            // TODO: Implement traffic data collection when available
            long? totalVisits = null;
            long? uniqueVisitors = null;

            var indices = new PMDashboardIndicesDto
            {
                OverallComplianceIndex = Math.Round(overallComplianceIndex, 2),
                AccessibilityIndex = Math.Round(accessibilityIndex, 2),
                AvailabilityIndex = Math.Round(availabilityIndex, 2),
                NavigationIndex = Math.Round(navigationIndex, 2),
                PerformanceIndex = Math.Round(performanceIndex, 2),
                SecurityIndex = Math.Round(securityIndex, 2),
                UserExperienceIndex = Math.Round(userExperienceIndex, 2),
                TotalVisits = totalVisits,
                UniqueVisitors = uniqueVisitors
            };

            return new APIResponse
            {
                IsSuccessful = true,
                Message = "PM Dashboard indices retrieved successfully",
                Data = indices
            };
        }

        public async Task<APIResponse> GetBottomMinistriesByCitizenImpactAsync(int count = 5)
        {
            // Get all ministries
            var ministries = await _context.Ministries
                .Where(m => m.DeletedAt == null)
                .ToListAsync();

            var ministryResults = new List<MinistryCitizenImpactDto>();

            foreach (var ministry in ministries)
            {
                // Get all assets for this ministry
                var ministryAssets = await _context.Assets
                    .Where(a => a.MinistryId == ministry.Id && a.DeletedAt == null)
                    .ToListAsync();

                var assetCount = ministryAssets.Count;

                if (assetCount == 0)
                    continue;

                var assetIds = ministryAssets.Select(a => a.Id).ToList();

                // Get latest AssetMetrics for all assets in this ministry
                var latestMetrics = await _context.AssetMetrics
                    .Where(m => assetIds.Contains(m.AssetId))
                    .GroupBy(m => m.AssetId)
                    .Select(g => g.OrderByDescending(m => m.CalculatedAt).First())
                    .ToListAsync();

                // Calculate average Citizen Happiness Metric for this ministry
                var assetsWithHappinessData = latestMetrics
                    .Where(m => m.CitizenHappinessMetric > 0)
                    .ToList();

                var citizenHappinessIndex = assetsWithHappinessData.Any()
                    ? assetsWithHappinessData.Average(m => m.CitizenHappinessMetric)
                    : 0.0;

                ministryResults.Add(new MinistryCitizenImpactDto
                {
                    MinistryId = ministry.Id,
                    MinistryName = ministry.MinistryName,
                    Assets = assetCount,
                    CitizenHappinessIndex = Math.Round(citizenHappinessIndex, 2)
                });
            }

            // Sort by Citizen Happiness Index (ascending - lowest first) and take bottom N
            var bottomMinistries = ministryResults
                .OrderBy(m => m.CitizenHappinessIndex)
                .Take(count)
                .ToList();

            return new APIResponse
            {
                IsSuccessful = true,
                Message = $"Bottom {count} ministries by citizen impact retrieved successfully",
                Data = bottomMinistries
            };
        }

        public async Task<APIResponse> GetTopMinistriesByComplianceAsync(int count = 5)
        {
            // Get all ministries
            var ministries = await _context.Ministries
                .Where(m => m.DeletedAt == null)
                .ToListAsync();

            var ministryResults = new List<MinistryComplianceDto>();

            foreach (var ministry in ministries)
            {
                // Get all assets for this ministry
                var ministryAssets = await _context.Assets
                    .Where(a => a.MinistryId == ministry.Id && a.DeletedAt == null)
                    .ToListAsync();

                var assetCount = ministryAssets.Count;

                if (assetCount == 0)
                    continue;

                var assetIds = ministryAssets.Select(a => a.Id).ToList();

                // Get latest AssetMetrics for all assets in this ministry
                var latestMetrics = await _context.AssetMetrics
                    .Where(m => assetIds.Contains(m.AssetId))
                    .GroupBy(m => m.AssetId)
                    .Select(g => g.OrderByDescending(m => m.CalculatedAt).First())
                    .ToListAsync();

                // Calculate average Overall Compliance Metric for this ministry
                var assetsWithComplianceData = latestMetrics
                    .Where(m => m.OverallComplianceMetric > 0)
                    .ToList();

                var complianceIndex = assetsWithComplianceData.Any()
                    ? assetsWithComplianceData.Average(m => m.OverallComplianceMetric)
                    : 0.0;

                ministryResults.Add(new MinistryComplianceDto
                {
                    MinistryId = ministry.Id,
                    MinistryName = ministry.MinistryName,
                    Assets = assetCount,
                    ComplianceIndex = Math.Round(complianceIndex, 2)
                });
            }

            // Sort by Compliance Index (descending - highest first) and take top N
            var topMinistries = ministryResults
                .OrderByDescending(m => m.ComplianceIndex)
                .Take(count)
                .ToList();

            return new APIResponse
            {
                IsSuccessful = true,
                Message = $"Top {count} ministries by compliance retrieved successfully",
                Data = topMinistries
            };
        }

        private static bool IsAssetUp(KPIsResult kpiResult)
        {
            if (string.IsNullOrEmpty(kpiResult.Target))
                return false;

            var resultLower = kpiResult.Target.ToLower();
            if (resultLower == "miss")
                return false;

            return true;
        }
    }
}
