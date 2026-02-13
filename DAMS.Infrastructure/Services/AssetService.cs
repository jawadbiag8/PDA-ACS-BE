using DAMS.Application.Interfaces;
using DAMS.Application.Models;
using DAMS.Application.DTOs;
using DAMS.Domain.Entities;
using DAMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace DAMS.Infrastructure.Services
{
    public class AssetService : IAssetService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AssetService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<APIResponse> GetAssetByIdAsync(int id)
        {
            var asset = await _context.Assets
                .Include(a => a.Ministry)
                .Include(a => a.Department)
                .Include(a => a.CitizenImpactLevel)
                .FirstOrDefaultAsync(a => a.Id == id && a.DeletedAt == null);

            if (asset == null)
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "Asset not found.",
                    Data = null
                };
            }

            var assetDto = MapToAssetDto(asset);
            return new APIResponse
            {
                IsSuccessful = true,
                Message = "Asset retrieved successfully",
                Data = assetDto
            };
        }

        public async Task<APIResponse> GetAllAssetsAsync(AssetFilterDto filter)
        {
            var query = _context.Assets
                .Include(a => a.Ministry)
                .Include(a => a.Department)
                .Include(a => a.CitizenImpactLevel)
                .Include(a => a.KPIsResults)
                    .ThenInclude(k => k.KpisLov)
                .Include(a => a.Incidents)
                    .ThenInclude(i => i.Severity)
                .Include(a => a.Incidents)
                    .ThenInclude(i => i.Status)
                .Where(a => a.DeletedAt == null)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                query = query.Where(a => 
                    a.AssetName.Contains(filter.SearchTerm) || 
                    a.AssetUrl.Contains(filter.SearchTerm) ||
                    a.Ministry.MinistryName.Contains(filter.SearchTerm) ||
                    (a.Department != null && a.Department.DepartmentName.Contains(filter.SearchTerm)) ||
                    a.TechnicalContactName.Contains(filter.SearchTerm) ||
                    a.PrimaryContactName.Contains(filter.SearchTerm));
            }

            if (filter.MinistryId.HasValue)
            {
                query = query.Where(a => a.MinistryId == filter.MinistryId.Value);
            }

            if (filter.DepartmentId.HasValue)
            {
                query = query.Where(a => a.DepartmentId == filter.DepartmentId.Value);
            }

            if (filter.CitizenImpactLevelId.HasValue)
            {
                query = query.Where(a => a.CitizenImpactLevelId == filter.CitizenImpactLevelId.Value);
            }

            if (filter.CreatedFrom.HasValue)
            {
                query = query.Where(a => a.CreatedAt >= filter.CreatedFrom.Value);
            }

            if (filter.CreatedTo.HasValue)
            {
                query = query.Where(a => a.CreatedAt <= filter.CreatedTo.Value);
            }

            // Apply CurrentStatus filter (Up or Down based on latest KPI result where KpiId = 1)
            if (!string.IsNullOrEmpty(filter.CurrentStatus))
            {
                var statusFilter = filter.CurrentStatus.Trim();
                if (statusFilter.Equals("Up", StringComparison.OrdinalIgnoreCase) || 
                    statusFilter.Equals("Down", StringComparison.OrdinalIgnoreCase))
                {
                    var isUp = statusFilter.Equals("Up", StringComparison.OrdinalIgnoreCase);
                    
                    // Filter assets based on their latest KPIsResult where KpiId = 1
                    // We need to filter after loading because IsAssetUp is a static method
                    // So we'll filter the assets list after loading
                }
            }

            // Apply DB sorting for entity-backed fields only (computed fields sorted in memory later)
            var sortByLower = filter.SortBy?.Trim().ToLowerInvariant() ?? "";
            var isComputedSort = sortByLower is "currentstatus" or "lastchecked" or "lastoutage" or "lastoutagedate"
                or "healthstatus" or "healthindex" or "performancestatus" or "performanceindex"
                or "compliancestatus" or "complianceindex" or "riskexposureindex" or "citizenimpactlevel"
                or "openincidents" or "highseverityincidents";

            if (!string.IsNullOrEmpty(filter.SortBy) && !isComputedSort)
            {
                query = sortByLower switch
                {
                    "id" => filter.SortDescending ? query.OrderByDescending(a => a.Id) : query.OrderBy(a => a.Id),
                    "websiteapplication" => filter.SortDescending ? query.OrderByDescending(a => a.AssetName) : query.OrderBy(a => a.AssetName),
                    "asseturl" => filter.SortDescending ? query.OrderByDescending(a => a.AssetUrl) : query.OrderBy(a => a.AssetUrl),
                    "ministrydepartment" or "ministryname" => filter.SortDescending ? query.OrderByDescending(a => a.Ministry.MinistryName) : query.OrderBy(a => a.Ministry.MinistryName),
                    "ministryid" => filter.SortDescending ? query.OrderByDescending(a => a.MinistryId) : query.OrderBy(a => a.MinistryId),
                    "departmentname" or "department" => filter.SortDescending ? query.OrderByDescending(a => a.Department != null ? a.Department.DepartmentName : "") : query.OrderBy(a => a.Department != null ? a.Department.DepartmentName : ""),
                    "departmentid" => filter.SortDescending ? query.OrderByDescending(a => a.DepartmentId ?? 0) : query.OrderBy(a => a.DepartmentId ?? 0),
                    "citizenimpactlevelid" => filter.SortDescending ? query.OrderByDescending(a => a.CitizenImpactLevelId) : query.OrderBy(a => a.CitizenImpactLevelId),
                    "createdat" => filter.SortDescending ? query.OrderByDescending(a => a.CreatedAt) : query.OrderBy(a => a.CreatedAt),
                    "updatedat" => filter.SortDescending ? query.OrderByDescending(a => a.UpdatedAt ?? a.CreatedAt) : query.OrderBy(a => a.UpdatedAt ?? a.CreatedAt),
                    _ => query.OrderByDescending(a => a.UpdatedAt ?? a.CreatedAt)
                };
            }
            else if (!isComputedSort)
            {
                query = query.OrderByDescending(a => a.UpdatedAt ?? a.CreatedAt);
            }

            // Load all assets matching the query (before CurrentStatus filter)
            // We need to load all to filter by CurrentStatus in memory, then paginate
            var allMatchingAssets = await query.ToListAsync();

            // Get AssetMetrics for all assets to filter by Health, Performance, Compliance, RiskIndex
            var assetIds = allMatchingAssets.Select(a => a.Id).ToList();
            var latestMetrics = await _context.AssetMetrics
                .Where(m => assetIds.Contains(m.AssetId))
                .GroupBy(m => m.AssetId)
                .Select(g => g.OrderByDescending(m => m.CalculatedAt).First())
                .ToListAsync();
            var metricsDict = latestMetrics.ToDictionary(m => m.AssetId);

            // Apply CurrentStatus filter after loading (since IsAssetUp is not translatable to SQL)
            if (!string.IsNullOrEmpty(filter.CurrentStatus))
            {
                var statusFilter = filter.CurrentStatus.Trim();
                if (statusFilter.Equals("Up", StringComparison.OrdinalIgnoreCase) || 
                    statusFilter.Equals("Down", StringComparison.OrdinalIgnoreCase))
                {
                    var isUp = statusFilter.Equals("Up", StringComparison.OrdinalIgnoreCase);
                    allMatchingAssets = allMatchingAssets.Where(asset =>
                    {
                        var latestHealthKpi = asset.KPIsResults
                            .Where(k => k.KpiId == 1)
                            .OrderByDescending(k => k.CreatedAt)
                            .FirstOrDefault();
                        
                        if (latestHealthKpi == null)
                            return false; // No KPI result means doesn't match Up or Down
                        
                        var assetIsUp = IsAssetUp(latestHealthKpi);
                        return isUp ? assetIsUp : !assetIsUp;
                    }).ToList();
                }
            }

            // Apply Health filter
            if (!string.IsNullOrEmpty(filter.Health))
            {
                var healthFilter = filter.Health.Trim();
                allMatchingAssets = allMatchingAssets.Where(asset =>
                {
                    metricsDict.TryGetValue(asset.Id, out var metrics);
                    if (metrics == null) return false;
                    var healthStatus = GetHealthStatus(metrics.CurrentHealth);
                    return healthStatus.Equals(healthFilter, StringComparison.OrdinalIgnoreCase);
                }).ToList();
            }

            // Apply Performance filter
            if (!string.IsNullOrEmpty(filter.Performance))
            {
                var performanceFilter = filter.Performance.Trim();
                allMatchingAssets = allMatchingAssets.Where(asset =>
                {
                    metricsDict.TryGetValue(asset.Id, out var metrics);
                    if (metrics == null) return false;
                    var performanceStatus = GetPerformanceStatus((int)Math.Round(metrics.PerformanceIndex));
                    return performanceStatus.Equals(performanceFilter, StringComparison.OrdinalIgnoreCase);
                }).ToList();
            }

            // Apply Compliance filter
            if (!string.IsNullOrEmpty(filter.Compliance))
            {
                var complianceFilter = filter.Compliance.Trim();
                allMatchingAssets = allMatchingAssets.Where(asset =>
                {
                    metricsDict.TryGetValue(asset.Id, out var metrics);
                    if (metrics == null) return false;
                    var complianceStatus = GetComplianceStatus((int)Math.Round(metrics.OverallComplianceMetric));
                    return complianceStatus.Equals(complianceFilter, StringComparison.OrdinalIgnoreCase);
                }).ToList();
            }

            // Apply RiskIndex filter
            if (!string.IsNullOrEmpty(filter.RiskIndex))
            {
                var riskFilter = filter.RiskIndex.Trim();
                allMatchingAssets = allMatchingAssets.Where(asset =>
                {
                    metricsDict.TryGetValue(asset.Id, out var metrics);
                    if (metrics == null) return false;
                    var riskStatus = GetIndexStatus((metrics.CurrentHealth + metrics.SecurityIndex) / 2.0);
                    string riskExposureIndex;
                    if (riskStatus == "UNKNOWN")
                        riskExposureIndex = "UNKNOWN";
                    else if (riskStatus == "HIGH")
                        riskExposureIndex = "LOW RISK";  // High index = Low risk
                    else if (riskStatus == "MEDIUM")
                        riskExposureIndex = "MEDIUM RISK";
                    else
                        riskExposureIndex = "HIGH RISK";  // Low index = High risk
                    
                    return riskExposureIndex.Equals(riskFilter, StringComparison.OrdinalIgnoreCase);
                }).ToList();
            }

            // In-memory sort for computed / dashboard fields
            if (isComputedSort && !string.IsNullOrEmpty(filter.SortBy))
            {
                Dictionary<int, DateTime>? lastOutageDict = null;
                if (sortByLower == "lastoutage" || sortByLower == "lastoutagedate")
                {
                    var outageRows = await _context.KPIsResultHistories
                        .AsNoTracking()
                        .Where(k => assetIds.Contains(k.AssetId))
                        .Where(k => (!string.IsNullOrWhiteSpace(k.Result) && k.Result.ToLower() == "miss") ||
                                    (!string.IsNullOrWhiteSpace(k.Target) && k.Target.ToLower() == "miss"))
                        .GroupBy(k => k.AssetId)
                        .Select(g => new { AssetId = g.Key, LastOutage = g.Max(x => x.CreatedAt) })
                        .ToListAsync();
                    lastOutageDict = outageRows.ToDictionary(x => x.AssetId, x => x.LastOutage);
                }

                var desc = filter.SortDescending;
                allMatchingAssets = sortByLower switch
                {
                    "currentstatus" => desc
                        ? allMatchingAssets.OrderByDescending(a => GetCurrentStatusSortKey(a)).ToList()
                        : allMatchingAssets.OrderBy(a => GetCurrentStatusSortKey(a)).ToList(),
                    "lastchecked" => desc
                        ? allMatchingAssets.OrderByDescending(a => GetLastCheckedSortKey(a)).ToList()
                        : allMatchingAssets.OrderBy(a => GetLastCheckedSortKey(a)).ToList(),
                    "lastoutage" or "lastoutagedate" => desc
                        ? allMatchingAssets.OrderByDescending(a => lastOutageDict != null && lastOutageDict.TryGetValue(a.Id, out var d) ? (DateTime?)d : null).ToList()
                        : allMatchingAssets.OrderBy(a => lastOutageDict != null && lastOutageDict.TryGetValue(a.Id, out var d) ? (DateTime?)d : null).ToList(),
                    "healthstatus" => desc
                        ? allMatchingAssets.OrderBy(a => GetHealthStatusSortRank(metricsDict.GetValueOrDefault(a.Id)?.CurrentHealth ?? 0)).ToList() // worst first (POOR, FAIR, HEALTHY)
                        : allMatchingAssets.OrderByDescending(a => GetHealthStatusSortRank(metricsDict.GetValueOrDefault(a.Id)?.CurrentHealth ?? 0)).ToList(), // best first
                    "healthindex" => desc
                        ? allMatchingAssets.OrderByDescending(a => metricsDict.GetValueOrDefault(a.Id)?.CurrentHealth ?? 0).ToList()
                        : allMatchingAssets.OrderBy(a => metricsDict.GetValueOrDefault(a.Id)?.CurrentHealth ?? 0).ToList(),
                    "performancestatus" => desc
                        ? allMatchingAssets.OrderByDescending(a => GetPerformanceStatus((int)Math.Round(metricsDict.GetValueOrDefault(a.Id)?.PerformanceIndex ?? 0))).ToList()
                        : allMatchingAssets.OrderBy(a => GetPerformanceStatus((int)Math.Round(metricsDict.GetValueOrDefault(a.Id)?.PerformanceIndex ?? 0))).ToList(),
                    "performanceindex" => desc
                        ? allMatchingAssets.OrderByDescending(a => (int)Math.Round(metricsDict.GetValueOrDefault(a.Id)?.PerformanceIndex ?? 0)).ToList()
                        : allMatchingAssets.OrderBy(a => (int)Math.Round(metricsDict.GetValueOrDefault(a.Id)?.PerformanceIndex ?? 0)).ToList(),
                    "compliancestatus" => desc
                        ? allMatchingAssets.OrderByDescending(a => GetComplianceStatus((int)Math.Round(metricsDict.GetValueOrDefault(a.Id)?.OverallComplianceMetric ?? 0))).ToList()
                        : allMatchingAssets.OrderBy(a => GetComplianceStatus((int)Math.Round(metricsDict.GetValueOrDefault(a.Id)?.OverallComplianceMetric ?? 0))).ToList(),
                    "complianceindex" => desc
                        ? allMatchingAssets.OrderByDescending(a => (int)Math.Round(metricsDict.GetValueOrDefault(a.Id)?.OverallComplianceMetric ?? 0)).ToList()
                        : allMatchingAssets.OrderBy(a => (int)Math.Round(metricsDict.GetValueOrDefault(a.Id)?.OverallComplianceMetric ?? 0)).ToList(),
                    "riskexposureindex" => desc
                        ? allMatchingAssets.OrderByDescending(a => GetRiskExposureIndexSortKey(metricsDict.GetValueOrDefault(a.Id))).ToList()
                        : allMatchingAssets.OrderBy(a => GetRiskExposureIndexSortKey(metricsDict.GetValueOrDefault(a.Id))).ToList(),
                    "citizenimpactlevel" => desc
                        ? allMatchingAssets.OrderByDescending(a => a.CitizenImpactLevel?.Name ?? "").ToList()
                        : allMatchingAssets.OrderBy(a => a.CitizenImpactLevel?.Name ?? "").ToList(),
                    "openincidents" => desc
                        ? allMatchingAssets.OrderByDescending(a => a.Incidents?.Count(i => i.DeletedAt == null) ?? 0).ToList()
                        : allMatchingAssets.OrderBy(a => a.Incidents?.Count(i => i.DeletedAt == null) ?? 0).ToList(),
                    "highseverityincidents" => desc
                        ? allMatchingAssets.OrderByDescending(a => GetHighSeverityIncidentsCount(a)).ToList()
                        : allMatchingAssets.OrderBy(a => GetHighSeverityIncidentsCount(a)).ToList(),
                    _ => allMatchingAssets
                };
            }

            // Get total count after filtering
            var totalCount = allMatchingAssets.Count;
            
            // Apply pagination after filtering
            var assets = allMatchingAssets
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToList();

            var dashboardDtos = new List<AssetDashboardDto>();
            foreach (var asset in assets)
            {
                var dto = await MapToAssetDashboardDtoAsync(asset);
                dashboardDtos.Add(dto);
            }

            var pagedResponse = new PagedResponse<AssetDashboardDto>
            {
                Data = dashboardDtos,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };

            return new APIResponse
            {
                IsSuccessful = true,
                Message = "Assets retrieved successfully",
                Data = pagedResponse
            };
        }

        public async Task<APIResponse> GetAssetsByMinistryAsync(string? searchTerm = null)
        {
            var query = _context.Ministries
                .Where(m => m.DeletedAt == null);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                query = query.Where(m =>
                    (m.MinistryName != null && m.MinistryName.Contains(term)) ||
                    (m.ContactName != null && m.ContactName.Contains(term)) ||
                    (m.ContactPhone != null && m.ContactPhone.Contains(term)));
            }

            var result = await query
                .Select(m => new
                {
                    MinistryId = m.Id,
                    MinistryName = m.MinistryName,
                    AssetCount = _context.Assets.Count(a => a.MinistryId == m.Id && a.DeletedAt == null)
                })
                .OrderByDescending(m => m.AssetCount)
                .ToListAsync();

            return new APIResponse
            {
                IsSuccessful = true,
                Message = "Assets by ministry retrieved successfully",
                Data = result
            };
        }

        public async Task<APIResponse> GetAssetsByMinistryIdAsync(int ministryId, AssetFilterDto? filter = null)
        {
            // Use default filter if not provided
            filter ??= new AssetFilterDto { PageNumber = 1, PageSize = 10 };

            var query = _context.Assets
                .Include(a => a.Ministry)
                .Include(a => a.Department)
                .Include(a => a.CitizenImpactLevel)
                .Include(a => a.KPIsResults)
                    .ThenInclude(k => k.KpisLov)
                .Include(a => a.Incidents)
                    .ThenInclude(i => i.Severity)
                .Include(a => a.Incidents)
                    .ThenInclude(i => i.Status)
                .Where(a => a.MinistryId == ministryId && a.DeletedAt == null)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                query = query.Where(a => 
                    a.AssetName.Contains(filter.SearchTerm) || 
                    a.AssetUrl.Contains(filter.SearchTerm) ||
                    (a.Department != null && a.Department.DepartmentName.Contains(filter.SearchTerm)) ||
                    a.TechnicalContactName.Contains(filter.SearchTerm) ||
                    a.PrimaryContactName.Contains(filter.SearchTerm));
            }

            if (filter.DepartmentId.HasValue)
            {
                query = query.Where(a => a.DepartmentId == filter.DepartmentId.Value);
            }

            if (filter.CitizenImpactLevelId.HasValue)
            {
                query = query.Where(a => a.CitizenImpactLevelId == filter.CitizenImpactLevelId.Value);
            }

            if (filter.CreatedFrom.HasValue)
            {
                query = query.Where(a => a.CreatedAt >= filter.CreatedFrom.Value);
            }

            if (filter.CreatedTo.HasValue)
            {
                query = query.Where(a => a.CreatedAt <= filter.CreatedTo.Value);
            }

            // Apply DB sorting for entity-backed fields only (computed fields sorted in memory later)
            var sortByLower = filter.SortBy?.Trim().ToLowerInvariant() ?? "";
            var isComputedSort = sortByLower is "currentstatus" or "lastchecked" or "lastoutage" or "lastoutagedate"
                or "healthstatus" or "healthindex" or "performancestatus" or "performanceindex"
                or "compliancestatus" or "complianceindex" or "riskexposureindex" or "citizenimpactlevel"
                or "openincidents" or "highseverityincidents";

            if (!string.IsNullOrEmpty(filter.SortBy) && !isComputedSort)
            {
                query = sortByLower switch
                {
                    "id" => filter.SortDescending ? query.OrderByDescending(a => a.Id) : query.OrderBy(a => a.Id),
                    "websiteapplication" or "assetname" => filter.SortDescending ? query.OrderByDescending(a => a.AssetName) : query.OrderBy(a => a.AssetName),
                    "asseturl" => filter.SortDescending ? query.OrderByDescending(a => a.AssetUrl) : query.OrderBy(a => a.AssetUrl),
                    "ministrydepartment" or "ministryname" => filter.SortDescending ? query.OrderByDescending(a => a.Ministry != null ? a.Ministry.MinistryName : "") : query.OrderBy(a => a.Ministry != null ? a.Ministry.MinistryName : ""),
                    "ministryid" => filter.SortDescending ? query.OrderByDescending(a => a.MinistryId) : query.OrderBy(a => a.MinistryId),
                    "departmentname" or "department" => filter.SortDescending ? query.OrderByDescending(a => a.Department != null ? a.Department.DepartmentName : "") : query.OrderBy(a => a.Department != null ? a.Department.DepartmentName : ""),
                    "departmentid" => filter.SortDescending ? query.OrderByDescending(a => a.DepartmentId ?? 0) : query.OrderBy(a => a.DepartmentId ?? 0),
                    "citizenimpactlevelid" => filter.SortDescending ? query.OrderByDescending(a => a.CitizenImpactLevelId) : query.OrderBy(a => a.CitizenImpactLevelId),
                    "createdat" => filter.SortDescending ? query.OrderByDescending(a => a.CreatedAt) : query.OrderBy(a => a.CreatedAt),
                    "updatedat" => filter.SortDescending ? query.OrderByDescending(a => a.UpdatedAt ?? a.CreatedAt) : query.OrderBy(a => a.UpdatedAt ?? a.CreatedAt),
                    _ => query.OrderByDescending(a => a.UpdatedAt ?? a.CreatedAt)
                };
            }
            else if (!isComputedSort)
            {
                query = query.OrderByDescending(a => a.UpdatedAt ?? a.CreatedAt);
            }

            // Load all assets matching the query (before CurrentStatus and other filters)
            // We need to load all to filter by CurrentStatus, Health, Performance, Compliance, RiskIndex in memory, then paginate
            var allMatchingAssets = await query.ToListAsync();

            // Get AssetMetrics for all assets to filter by Health, Performance, Compliance, RiskIndex
            var assetIds = allMatchingAssets.Select(a => a.Id).ToList();
            var latestMetrics = await _context.AssetMetrics
                .Where(m => assetIds.Contains(m.AssetId))
                .GroupBy(m => m.AssetId)
                .Select(g => g.OrderByDescending(m => m.CalculatedAt).First())
                .ToListAsync();
            var metricsDict = latestMetrics.ToDictionary(m => m.AssetId);

            // Apply CurrentStatus filter after loading (since IsAssetUp is not translatable to SQL)
            if (!string.IsNullOrEmpty(filter.CurrentStatus))
            {
                var statusFilter = filter.CurrentStatus.Trim();
                if (statusFilter.Equals("Up", StringComparison.OrdinalIgnoreCase) || 
                    statusFilter.Equals("Down", StringComparison.OrdinalIgnoreCase))
                {
                    var isUp = statusFilter.Equals("Up", StringComparison.OrdinalIgnoreCase);
                    allMatchingAssets = allMatchingAssets.Where(asset =>
                    {
                        var latestHealthKpi = asset.KPIsResults
                            .Where(k => k.KpiId == 1)
                            .OrderByDescending(k => k.CreatedAt)
                            .FirstOrDefault();
                        
                        if (latestHealthKpi == null)
                            return false; // No KPI result means doesn't match Up or Down
                        
                        var assetIsUp = IsAssetUp(latestHealthKpi);
                        return isUp ? assetIsUp : !assetIsUp;
                    }).ToList();
                }
            }

            // Apply Health filter
            if (!string.IsNullOrEmpty(filter.Health))
            {
                var healthFilter = filter.Health.Trim();
                allMatchingAssets = allMatchingAssets.Where(asset =>
                {
                    metricsDict.TryGetValue(asset.Id, out var metrics);
                    if (metrics == null) return false;
                    var healthStatus = GetHealthStatus(metrics.CurrentHealth);
                    return healthStatus.Equals(healthFilter, StringComparison.OrdinalIgnoreCase);
                }).ToList();
            }

            // Apply Performance filter
            if (!string.IsNullOrEmpty(filter.Performance))
            {
                var performanceFilter = filter.Performance.Trim();
                allMatchingAssets = allMatchingAssets.Where(asset =>
                {
                    metricsDict.TryGetValue(asset.Id, out var metrics);
                    if (metrics == null) return false;
                    var performanceStatus = GetPerformanceStatus((int)Math.Round(metrics.PerformanceIndex));
                    return performanceStatus.Equals(performanceFilter, StringComparison.OrdinalIgnoreCase);
                }).ToList();
            }

            // Apply Compliance filter
            if (!string.IsNullOrEmpty(filter.Compliance))
            {
                var complianceFilter = filter.Compliance.Trim();
                allMatchingAssets = allMatchingAssets.Where(asset =>
                {
                    metricsDict.TryGetValue(asset.Id, out var metrics);
                    if (metrics == null) return false;
                    var complianceStatus = GetComplianceStatus((int)Math.Round(metrics.OverallComplianceMetric));
                    return complianceStatus.Equals(complianceFilter, StringComparison.OrdinalIgnoreCase);
                }).ToList();
            }

            // Apply RiskIndex filter
            if (!string.IsNullOrEmpty(filter.RiskIndex))
            {
                var riskFilter = filter.RiskIndex.Trim();
                allMatchingAssets = allMatchingAssets.Where(asset =>
                {
                    metricsDict.TryGetValue(asset.Id, out var metrics);
                    if (metrics == null) return false;
                    var riskStatus = GetIndexStatus((metrics.CurrentHealth + metrics.SecurityIndex) / 2.0);
                    string riskExposureIndex;
                    if (riskStatus == "UNKNOWN")
                        riskExposureIndex = "UNKNOWN";
                    else if (riskStatus == "HIGH")
                        riskExposureIndex = "LOW RISK";  // High index = Low risk
                    else if (riskStatus == "MEDIUM")
                        riskExposureIndex = "MEDIUM RISK";
                    else
                        riskExposureIndex = "HIGH RISK";  // Low index = High risk
                    
                    return riskExposureIndex.Equals(riskFilter, StringComparison.OrdinalIgnoreCase);
                }).ToList();
            }

            // In-memory sort for computed / dashboard fields (same response keys as GetAllAssets)
            if (isComputedSort && !string.IsNullOrEmpty(filter.SortBy))
            {
                Dictionary<int, DateTime>? lastOutageDict = null;
                if (sortByLower == "lastoutage" || sortByLower == "lastoutagedate")
                {
                    var outageRows = await _context.KPIsResultHistories
                        .AsNoTracking()
                        .Where(k => assetIds.Contains(k.AssetId))
                        .Where(k => (!string.IsNullOrWhiteSpace(k.Result) && k.Result.ToLower() == "miss") ||
                                    (!string.IsNullOrWhiteSpace(k.Target) && k.Target.ToLower() == "miss"))
                        .GroupBy(k => k.AssetId)
                        .Select(g => new { AssetId = g.Key, LastOutage = g.Max(x => x.CreatedAt) })
                        .ToListAsync();
                    lastOutageDict = outageRows.ToDictionary(x => x.AssetId, x => x.LastOutage);
                }

                var desc = filter.SortDescending;
                allMatchingAssets = sortByLower switch
                {
                    "currentstatus" => desc
                        ? allMatchingAssets.OrderByDescending(a => GetCurrentStatusSortKey(a)).ToList()
                        : allMatchingAssets.OrderBy(a => GetCurrentStatusSortKey(a)).ToList(),
                    "lastchecked" => desc
                        ? allMatchingAssets.OrderByDescending(a => GetLastCheckedSortKey(a)).ToList()
                        : allMatchingAssets.OrderBy(a => GetLastCheckedSortKey(a)).ToList(),
                    "lastoutage" or "lastoutagedate" => desc
                        ? allMatchingAssets.OrderByDescending(a => lastOutageDict != null && lastOutageDict.TryGetValue(a.Id, out var d) ? (DateTime?)d : null).ToList()
                        : allMatchingAssets.OrderBy(a => lastOutageDict != null && lastOutageDict.TryGetValue(a.Id, out var d) ? (DateTime?)d : null).ToList(),
                    "healthstatus" => desc
                        ? allMatchingAssets.OrderBy(a => GetHealthStatusSortRank(metricsDict.GetValueOrDefault(a.Id)?.CurrentHealth ?? 0)).ToList() // worst first (POOR, FAIR, HEALTHY)
                        : allMatchingAssets.OrderByDescending(a => GetHealthStatusSortRank(metricsDict.GetValueOrDefault(a.Id)?.CurrentHealth ?? 0)).ToList(), // best first
                    "healthindex" => desc
                        ? allMatchingAssets.OrderByDescending(a => metricsDict.GetValueOrDefault(a.Id)?.CurrentHealth ?? 0).ToList()
                        : allMatchingAssets.OrderBy(a => metricsDict.GetValueOrDefault(a.Id)?.CurrentHealth ?? 0).ToList(),
                    "performancestatus" => desc
                        ? allMatchingAssets.OrderByDescending(a => GetPerformanceStatus((int)Math.Round(metricsDict.GetValueOrDefault(a.Id)?.PerformanceIndex ?? 0))).ToList()
                        : allMatchingAssets.OrderBy(a => GetPerformanceStatus((int)Math.Round(metricsDict.GetValueOrDefault(a.Id)?.PerformanceIndex ?? 0))).ToList(),
                    "performanceindex" => desc
                        ? allMatchingAssets.OrderByDescending(a => (int)Math.Round(metricsDict.GetValueOrDefault(a.Id)?.PerformanceIndex ?? 0)).ToList()
                        : allMatchingAssets.OrderBy(a => (int)Math.Round(metricsDict.GetValueOrDefault(a.Id)?.PerformanceIndex ?? 0)).ToList(),
                    "compliancestatus" => desc
                        ? allMatchingAssets.OrderByDescending(a => GetComplianceStatus((int)Math.Round(metricsDict.GetValueOrDefault(a.Id)?.OverallComplianceMetric ?? 0))).ToList()
                        : allMatchingAssets.OrderBy(a => GetComplianceStatus((int)Math.Round(metricsDict.GetValueOrDefault(a.Id)?.OverallComplianceMetric ?? 0))).ToList(),
                    "complianceindex" => desc
                        ? allMatchingAssets.OrderByDescending(a => (int)Math.Round(metricsDict.GetValueOrDefault(a.Id)?.OverallComplianceMetric ?? 0)).ToList()
                        : allMatchingAssets.OrderBy(a => (int)Math.Round(metricsDict.GetValueOrDefault(a.Id)?.OverallComplianceMetric ?? 0)).ToList(),
                    "riskexposureindex" => desc
                        ? allMatchingAssets.OrderByDescending(a => GetRiskExposureIndexSortKey(metricsDict.GetValueOrDefault(a.Id))).ToList()
                        : allMatchingAssets.OrderBy(a => GetRiskExposureIndexSortKey(metricsDict.GetValueOrDefault(a.Id))).ToList(),
                    "citizenimpactlevel" => desc
                        ? allMatchingAssets.OrderByDescending(a => a.CitizenImpactLevel?.Name ?? "").ToList()
                        : allMatchingAssets.OrderBy(a => a.CitizenImpactLevel?.Name ?? "").ToList(),
                    "openincidents" => desc
                        ? allMatchingAssets.OrderByDescending(a => a.Incidents?.Count(i => i.DeletedAt == null) ?? 0).ToList()
                        : allMatchingAssets.OrderBy(a => a.Incidents?.Count(i => i.DeletedAt == null) ?? 0).ToList(),
                    "highseverityincidents" => desc
                        ? allMatchingAssets.OrderByDescending(a => GetHighSeverityIncidentsCount(a)).ToList()
                        : allMatchingAssets.OrderBy(a => GetHighSeverityIncidentsCount(a)).ToList(),
                    _ => allMatchingAssets
                };
            }

            // Get total count after filtering
            var totalCount = allMatchingAssets.Count;
            
            // Apply pagination after filtering
            var assets = allMatchingAssets
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToList();

            var dashboardDtos = new List<AssetDashboardDto>();
            foreach (var asset in assets)
            {
                var dto = await MapToAssetDashboardDtoAsync(asset);
                dashboardDtos.Add(dto);
            }

            var pagedResponse = new PagedResponse<AssetDashboardDto>
            {
                Data = dashboardDtos,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };

            return new APIResponse
            {
                IsSuccessful = true,
                Message = "Assets retrieved successfully",
                Data = pagedResponse
            };
        }

        public async Task<APIResponse> GetMinistryAssetsSummaryAsync(int ministryId)
        {
            // Verify ministry exists
            var ministry = await _context.Ministries
                .FirstOrDefaultAsync(m => m.Id == ministryId && m.DeletedAt == null);

            if (ministry == null)
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "Ministry not found.",
                    Data = null
                };
            }

            // Get all assets for this ministry (excluding deleted)
            var assets = await _context.Assets
                .Include(a => a.Incidents)
                    .ThenInclude(i => i.Severity)
                .Include(a => a.Incidents)
                    .ThenInclude(i => i.Status)
                .Where(a => a.MinistryId == ministryId && a.DeletedAt == null)
                .ToListAsync();

            // Total Assets
            var totalAssets = assets.Count;

            // Get all incidents for these assets
            var allIncidents = assets
                .SelectMany(a => a.Incidents ?? new List<Incident>())
                .Where(i => i.DeletedAt == null)
                .ToList();

            // Total Incidents
            var totalIncidents = allIncidents.Count;

            // Open Incidents (not deleted) - with null check for Status
            var openIncidents = allIncidents
                .Where(i => i.DeletedAt == null && i.StatusId != 12)
                .ToList();

            // High Severity Open Incidents (P1 - Critical)
            var highSeverityIncidents = openIncidents
                .Count(i => (i.Severity?.Name.Contains("P2", StringComparison.OrdinalIgnoreCase) ?? false));

            var summary = new MinistryAssetsSummaryDto
            {
                TotalAssets = totalAssets,
                TotalIncidents = totalIncidents,
                OpenIncidents = openIncidents.Count,
                HighSeverityOpenIncidents = highSeverityIncidents
            };

            return new APIResponse
            {
                IsSuccessful = true,
                Message = "Ministry assets summary retrieved successfully",
                Data = summary
            };
        }

        public async Task<APIResponse> GetAssetsByDepartmentIdAsync(int departmentId)
        {
            var assets = await _context.Assets
                .Include(a => a.Ministry)
                .Include(a => a.Department)
                .Include(a => a.CitizenImpactLevel)
                .Where(a => a.DepartmentId.HasValue && a.DepartmentId.Value == departmentId && a.DeletedAt == null)
                .ToListAsync();

            var assetDtos = assets.Select(MapToAssetDto).ToList();

            return new APIResponse
            {
                IsSuccessful = true,
                Message = "Assets retrieved successfully",
                Data = assetDtos
            };
        }

        public async Task<APIResponse> GetAssetsForDropdownAsync()
        {
            var assets = await _context.Assets
                .Where(a => a.DeletedAt == null)
                .OrderBy(a => a.AssetName)
                .Select(a => new
                {
                    Id = a.Id,
                    Name = a.AssetName
                })
                .ToListAsync();

            return new APIResponse
            {
                IsSuccessful = true,
                Message = "Assets retrieved successfully for dropdown",
                Data = assets
            };
        }

        public async Task<APIResponse> CreateAssetAsync(CreateAssetDto dto, string createdBy)
        {
            // Validate mandatory fields
            if (string.IsNullOrWhiteSpace(dto.AssetName))
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "Asset Name is required.",
                    Data = null
                };
            }

            if (string.IsNullOrWhiteSpace(dto.AssetUrl))
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "Asset URL is required.",
                    Data = null
                };
            }

            if (!IsValidUrl(dto.AssetUrl))
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "Asset URL must be a valid URL format.",
                    Data = null
                };
            }

            // Check if asset with same URL already exists (case-insensitive; URL comparison in memory for EF translation)
            var trimmedUrl = dto.AssetUrl.Trim();
            var urlLower = trimmedUrl.ToLowerInvariant();
            var existingUrls = await _context.Assets
                .Where(a => a.DeletedAt == null)
                .Select(a => a.AssetUrl)
                .ToListAsync();
            var existingAsset = existingUrls.FirstOrDefault(u => (u ?? string.Empty).Trim().ToLowerInvariant() == urlLower) != null;
            
            if (existingAsset)
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = $"An asset with URL '{trimmedUrl}' already exists.",
                    Data = null
                };
            }

            var ministry = await _context.Ministries.FindAsync(dto.MinistryId);
            if (ministry == null)
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "Ministry not found.",
                    Data = null
                };
            }

            if (dto.DepartmentId.HasValue)
            {
                var department = await _context.Departments.FindAsync(dto.DepartmentId.Value);
                if (department == null)
                {
                    return new APIResponse
                    {
                        IsSuccessful = false,
                        Message = "Department not found.",
                        Data = null
                    };
                }
            }

            var citizenImpactLevel = await _context.CommonLookups.FindAsync(dto.CitizenImpactLevelId);
            if (citizenImpactLevel == null)
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "Citizen Impact Level not found.",
                    Data = null
                };
            }

            //// Validate email formats if provided
            //if (!string.IsNullOrWhiteSpace(dto.PrimaryContactEmail) && !IsValidEmail(dto.PrimaryContactEmail))
            //{
            //    return new APIResponse
            //    {
            //        IsSuccessful = false,
            //        Message = "Primary Contact Email must be a valid email format.",
            //        Data = null
            //    };
            //}

            //if (!string.IsNullOrWhiteSpace(dto.TechnicalContactEmail) && !IsValidEmail(dto.TechnicalContactEmail))
            //{
            //    return new APIResponse
            //    {
            //        IsSuccessful = false,
            //        Message = "Technical Contact Email must be a valid email format.",
            //        Data = null
            //    };
            //}

            //// Validate phone formats if provided
            //if (!string.IsNullOrWhiteSpace(dto.PrimaryContactPhone) && !IsValidPhone(dto.PrimaryContactPhone))
            //{
            //    return new APIResponse
            //    {
            //        IsSuccessful = false,
            //        Message = "Primary Contact Phone must contain valid phone characters.",
            //        Data = null
            //    };
            //}

            //if (!string.IsNullOrWhiteSpace(dto.TechnicalContactPhone) && !IsValidPhone(dto.TechnicalContactPhone))
            //{
            //    return new APIResponse
            //    {
            //        IsSuccessful = false,
            //        Message = "Technical Contact Phone must contain valid phone characters.",
            //        Data = null
            //    };
            //}

            var asset = new Asset
            {
                MinistryId = dto.MinistryId,
                DepartmentId = dto.DepartmentId,
                AssetName = dto.AssetName.Trim(),
                AssetUrl = dto.AssetUrl.Trim(),
                Description = dto.Description ?? string.Empty,
                CitizenImpactLevelId = dto.CitizenImpactLevelId,
                PrimaryContactName = dto.PrimaryContactName ?? string.Empty,
                PrimaryContactEmail = dto.PrimaryContactEmail ?? string.Empty,
                PrimaryContactPhone = dto.PrimaryContactPhone ?? string.Empty,
                TechnicalContactName = dto.TechnicalContactName ?? string.Empty,
                TechnicalContactEmail = dto.TechnicalContactEmail ?? string.Empty,
                TechnicalContactPhone = dto.TechnicalContactPhone ?? string.Empty,
                CreatedBy = createdBy,
                CreatedAt = DateTime.Now
            };

            _context.Assets.Add(asset);
            await _context.SaveChangesAsync();

            var assetDto = MapToAssetDto(asset);
            return new APIResponse
            {
                IsSuccessful = true,
                Message = "Asset created successfully",
                Data = assetDto
            };
        }

        public async Task<APIResponse> UpdateAssetAsync(int id, UpdateAssetDto dto, string updatedBy)
        {
            var asset = await _context.Assets.FindAsync(id);
            if (asset == null || asset.DeletedAt != null)
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "Asset not found.",
                    Data = null
                };
            }

            if (dto.MinistryId.HasValue)
            {
                var ministry = await _context.Ministries.FindAsync(dto.MinistryId.Value);
                if (ministry == null)
                {
                    return new APIResponse
                    {
                        IsSuccessful = false,
                        Message = "Ministry not found.",
                        Data = null
                    };
                }
                asset.MinistryId = dto.MinistryId.Value;
            }

            if (dto.DepartmentId.HasValue)
            {
                var department = await _context.Departments.FindAsync(dto.DepartmentId.Value);
                if (department == null)
                {
                    return new APIResponse
                    {
                        IsSuccessful = false,
                        Message = "Department not found.",
                        Data = null
                    };
                }
                asset.DepartmentId = dto.DepartmentId.Value;
            }
            else if (dto.DepartmentId == null)
            {
                asset.DepartmentId = null;
            }

            if (!string.IsNullOrEmpty(dto.AssetName))
                asset.AssetName = dto.AssetName.Trim();

            if (!string.IsNullOrEmpty(dto.AssetUrl))
            {
                if (!IsValidUrl(dto.AssetUrl))
                {
                    return new APIResponse
                    {
                        IsSuccessful = false,
                        Message = "Asset URL must be a valid URL format.",
                        Data = null
                    };
                }
                
                var trimmedUrl = dto.AssetUrl.Trim();
                var urlLower = trimmedUrl.ToLowerInvariant();
                // Check if URL already exists for a different asset (case-insensitive; URL comparison in memory for EF translation)
                var existingUrls = await _context.Assets
                    .Where(a => a.Id != id && a.DeletedAt == null)
                    .Select(a => a.AssetUrl)
                    .ToListAsync();
                var duplicateExists = existingUrls.Any(u => (u ?? string.Empty).Trim().ToLowerInvariant() == urlLower);
                
                if (duplicateExists)
                {
                    return new APIResponse
                    {
                        IsSuccessful = false,
                        Message = $"An asset with URL '{trimmedUrl}' already exists.",
                        Data = null
                    };
                }
                
                asset.AssetUrl = trimmedUrl;
            }

            if (dto.CitizenImpactLevelId.HasValue)
            {
                var citizenImpactLevel = await _context.CommonLookups.FindAsync(dto.CitizenImpactLevelId.Value);
                if (citizenImpactLevel == null)
                {
                    return new APIResponse
                    {
                        IsSuccessful = false,
                        Message = "Citizen Impact Level not found.",
                        Data = null
                    };
                }
                asset.CitizenImpactLevelId = dto.CitizenImpactLevelId.Value;
            }

            if (dto.Description != null)
                asset.Description = dto.Description;

            if (dto.PrimaryContactName != null)
                asset.PrimaryContactName = dto.PrimaryContactName;

            if (dto.PrimaryContactEmail != null)
                asset.PrimaryContactEmail = dto.PrimaryContactEmail;
            //{
            //    if (!string.IsNullOrWhiteSpace(dto.PrimaryContactEmail) && !IsValidEmail(dto.PrimaryContactEmail))
            //    {
            //        return new APIResponse
            //        {
            //            IsSuccessful = false,
            //            Message = "Primary Contact Email must be a valid email format.",
            //            Data = null
            //        };
            //    }
            //    asset.PrimaryContactEmail = dto.PrimaryContactEmail;
            //}

            if (dto.PrimaryContactPhone != null)
                asset.PrimaryContactPhone = dto.PrimaryContactPhone;
            //{
            //    if (!string.IsNullOrWhiteSpace(dto.PrimaryContactPhone) && !IsValidPhone(dto.PrimaryContactPhone))
            //    {
            //        return new APIResponse
            //        {
            //            IsSuccessful = false,
            //            Message = "Primary Contact Phone must contain valid phone characters.",
            //            Data = null
            //        };
            //    }
            //    asset.PrimaryContactPhone = dto.PrimaryContactPhone;
            //}

            if (dto.TechnicalContactName != null)
                asset.TechnicalContactName = dto.TechnicalContactName;

            if (dto.TechnicalContactEmail != null)
                asset.TechnicalContactEmail = dto.TechnicalContactEmail;
            //{
            //    if (!string.IsNullOrWhiteSpace(dto.TechnicalContactEmail) && !IsValidEmail(dto.TechnicalContactEmail))
            //    {
            //        return new APIResponse
            //        {
            //            IsSuccessful = false,
            //            Message = "Technical Contact Email must be a valid email format.",
            //            Data = null
            //        };
            //    }
            //    asset.TechnicalContactEmail = dto.TechnicalContactEmail;
            //}

            if (dto.TechnicalContactPhone != null)
                asset.TechnicalContactPhone = dto.TechnicalContactPhone;
            //{
            //    if (!string.IsNullOrWhiteSpace(dto.TechnicalContactPhone) && !IsValidPhone(dto.TechnicalContactPhone))
            //    {
            //        return new APIResponse
            //        {
            //            IsSuccessful = false,
            //            Message = "Technical Contact Phone must contain valid phone characters.",
            //            Data = null
            //        };
            //    }
            //    asset.TechnicalContactPhone = dto.TechnicalContactPhone;
            //}

            asset.UpdatedAt = DateTime.Now;
            asset.UpdatedBy = updatedBy;

            await _context.SaveChangesAsync();

            var assetDto = MapToAssetDto(asset);
            return new APIResponse
            {
                IsSuccessful = true,
                Message = "Asset updated successfully",
                Data = assetDto
            };
        }

        public async Task<APIResponse> DeleteAssetAsync(int id, string deletedBy)
        {
            var asset = await _context.Assets.FindAsync(id);
            if (asset == null || asset.DeletedAt != null)
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "Asset not found.",
                    Data = null
                };
            }

            asset.DeletedAt = DateTime.Now;
            asset.DeletedBy = deletedBy;
            asset.UpdatedAt = DateTime.Now;
            asset.UpdatedBy = deletedBy;

            await _context.SaveChangesAsync();

            return new APIResponse
            {
                IsSuccessful = true,
                Message = "Asset deleted successfully",
                Data = null
            };
        }

        public async Task<APIResponse> BulkUploadAssetsAsync(Stream csvStream, string createdBy)
        {
            var response = new BulkUploadResponseDto();
            var errors = new List<BulkUploadErrorDto>();
            var validAssets = new List<Asset>();

            try
            {
                using var reader = new StreamReader(csvStream);
                using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HeaderValidated = null,
                    MissingFieldFound = null,
                    TrimOptions = TrimOptions.Trim
                });

                // Read all records from CSV
                var records = csv.GetRecords<CreateAssetDto>().ToList();
                response.TotalRows = records.Count;

                // Pre-fetch lookup data for validation
                var ministries = await _context.Ministries.Where(m => m.DeletedAt == null).ToListAsync();
                var departments = await _context.Departments.Where(d => d.DeletedAt == null).ToListAsync();
                var citizenImpactLevels = await _context.CommonLookups
                    .Where(c => c.Type == "CitizenImpactLevel" && c.DeletedAt == null)
                    .ToListAsync();
                
                // Pre-fetch existing asset URLs to check for duplicates (normalize in memory for EF translation)
                var existingAssetUrlList = await _context.Assets
                    .Where(a => a.DeletedAt == null)
                    .Select(a => a.AssetUrl)
                    .ToListAsync();
                var existingAssetUrls = new HashSet<string>(
                    existingAssetUrlList.Select(u => (u ?? string.Empty).Trim().ToLowerInvariant()),
                    StringComparer.OrdinalIgnoreCase);

                var rowNumber = 1; // Start at 1 (header is row 0)
                var processedUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase); // Track URLs in current batch
                
                foreach (var dto in records)
                {
                    rowNumber++; // Increment to account for header row (first data row is row 2)
                    var validationError = ValidateAssetDto(dto, ministries, departments, citizenImpactLevels);

                    if (!string.IsNullOrEmpty(validationError))
                    {
                        errors.Add(new BulkUploadErrorDto
                        {
                            RowNumber = rowNumber,
                            AssetName = dto.AssetName ?? "N/A",
                            ErrorMessage = validationError
                        });
                        response.FailedCount++;
                        continue;
                    }

                    // Check for duplicate URL (in database or within current batch)
                    var trimmedUrl = dto.AssetUrl?.Trim() ?? string.Empty;
                    var urlLower = trimmedUrl.ToLowerInvariant();
                    
                    if (existingAssetUrls.Contains(urlLower) || processedUrls.Contains(urlLower))
                    {
                        errors.Add(new BulkUploadErrorDto
                        {
                            RowNumber = rowNumber,
                            AssetName = dto.AssetName ?? "N/A",
                            ErrorMessage = $"An asset with URL '{trimmedUrl}' already exists."
                        });
                        response.FailedCount++;
                        continue;
                    }
                    
                    processedUrls.Add(urlLower);

                    // Create asset entity
                    var asset = new Asset
                    {
                        MinistryId = dto.MinistryId,
                        DepartmentId = dto.DepartmentId,
                        AssetName = dto.AssetName.Trim(),
                        AssetUrl = dto.AssetUrl.Trim(),
                        Description = dto.Description ?? string.Empty,
                        CitizenImpactLevelId = dto.CitizenImpactLevelId,
                        PrimaryContactName = dto.PrimaryContactName ?? string.Empty,
                        PrimaryContactEmail = dto.PrimaryContactEmail ?? string.Empty,
                        PrimaryContactPhone = dto.PrimaryContactPhone ?? string.Empty,
                        TechnicalContactName = dto.TechnicalContactName ?? string.Empty,
                        TechnicalContactEmail = dto.TechnicalContactEmail ?? string.Empty,
                        TechnicalContactPhone = dto.TechnicalContactPhone ?? string.Empty,
                        CreatedBy = createdBy,
                        CreatedAt = DateTime.Now
                    };

                    validAssets.Add(asset);
                }

                // Bulk insert valid assets
                if (validAssets.Any())
                {
                    await _context.Assets.AddRangeAsync(validAssets);
                    await _context.SaveChangesAsync();
                    response.SuccessfulCount = validAssets.Count;
                }

                response.Errors = errors;
                response.FailedCount = errors.Count;

                var message = response.SuccessfulCount > 0
                    ? $"Bulk upload completed. {response.SuccessfulCount} asset(s) created successfully."
                    : "Bulk upload failed. No assets were created.";

                if (response.FailedCount > 0)
                {
                    message += $" {response.FailedCount} row(s) failed validation.";
                }

                return new APIResponse
                {
                    IsSuccessful = response.SuccessfulCount > 0,
                    Message = message,
                    Data = response
                };
            }
            catch (Exception ex)
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = $"Error processing CSV file: {ex.Message}",
                    Data = new BulkUploadResponseDto
                    {
                        TotalRows = response.TotalRows,
                        SuccessfulCount = response.SuccessfulCount,
                        FailedCount = response.FailedCount,
                        Errors = errors
                    }
                };
            }
        }

        private string ValidateAssetDto(CreateAssetDto dto, List<Ministry> ministries, List<Department> departments, List<CommonLookup> citizenImpactLevels)
        {
            // Validate mandatory fields
            if (string.IsNullOrWhiteSpace(dto.AssetName))
            {
                return "Asset Name is required.";
            }

            if (string.IsNullOrWhiteSpace(dto.AssetUrl))
            {
                return "Asset URL is required.";
            }

            if (!IsValidUrl(dto.AssetUrl))
            {
                return "Asset URL must be a valid URL format.";
            }

            // Validate Ministry exists
            if (!ministries.Any(m => m.Id == dto.MinistryId))
            {
                return $"Ministry with ID {dto.MinistryId} not found.";
            }

            // Validate Department exists if provided
            if (dto.DepartmentId.HasValue)
            {
                if (!departments.Any(d => d.Id == dto.DepartmentId.Value))
                {
                    return $"Department with ID {dto.DepartmentId.Value} not found.";
                }
            }

            // Validate Citizen Impact Level exists
            if (!citizenImpactLevels.Any(c => c.Id == dto.CitizenImpactLevelId))
            {
                return $"Citizen Impact Level with ID {dto.CitizenImpactLevelId} not found.";
            }

            // Validate email formats if provided
            if (!string.IsNullOrWhiteSpace(dto.PrimaryContactEmail) && !IsValidEmail(dto.PrimaryContactEmail))
            {
                return "Primary Contact Email must be a valid email format.";
            }

            if (!string.IsNullOrWhiteSpace(dto.TechnicalContactEmail) && !IsValidEmail(dto.TechnicalContactEmail))
            {
                return "Technical Contact Email must be a valid email format.";
            }

            // Validate phone formats if provided
            if (!string.IsNullOrWhiteSpace(dto.PrimaryContactPhone) && !IsValidPhone(dto.PrimaryContactPhone))
            {
                return "Primary Contact Phone must contain valid phone characters.";
            }

            if (!string.IsNullOrWhiteSpace(dto.TechnicalContactPhone) && !IsValidPhone(dto.TechnicalContactPhone))
            {
                return "Technical Contact Phone must contain valid phone characters.";
            }

            return string.Empty;
        }

        private static AssetDto MapToAssetDto(Asset asset)
        {
            return new AssetDto
            {
                Id = asset.Id,
                MinistryId = asset.MinistryId,
                AssetName = asset.AssetName,
                DepartmentId = asset.DepartmentId,
                AssetUrl = asset.AssetUrl,
                Description = asset.Description,
                CitizenImpactLevelId = asset.CitizenImpactLevelId,
                PrimaryContactName = asset.PrimaryContactName,
                PrimaryContactEmail = asset.PrimaryContactEmail,
                PrimaryContactPhone = asset.PrimaryContactPhone,
                TechnicalContactName = asset.TechnicalContactName,
                TechnicalContactEmail = asset.TechnicalContactEmail,
                TechnicalContactPhone = asset.TechnicalContactPhone,
                CreatedAt = asset.CreatedAt,
                CreatedBy = asset.CreatedBy
            };
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsValidPhone(string phone)
        {
            // Allow digits, spaces, dashes, parentheses, plus sign
            return System.Text.RegularExpressions.Regex.IsMatch(phone, @"^[\d\s\-\(\)\+]+$");
        }

        private static bool IsValidUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var result) &&
                   (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
        }

        private async Task<AssetDashboardDto> MapToAssetDashboardDtoAsync(Asset asset)
        {
            var dashboardDto = new AssetDashboardDto
            {
                MinistryId = asset.MinistryId,
                Id = asset.Id,
                AssetUrl = asset.AssetUrl,
                CitizenImpactLevel = asset.CitizenImpactLevel?.Name ?? "UNKNOWN"
            };

            // Ministry / Department
            var ministryName = asset.Ministry?.MinistryName ?? "UNKNOWN MINISTRY";
            var departmentName = asset.Department?.DepartmentName;
            dashboardDto.MinistryDepartment = ministryName ?? "";
            dashboardDto.Department = departmentName ?? string.Empty;

            // Website / Application
            dashboardDto.WebsiteApplication = string.IsNullOrEmpty(asset.AssetName)
                ? "Ministry Website"
                : asset.AssetName;

            // Get AssetMetrics directly from database (no calculations)
            var metrics = await _context.AssetMetrics
                .Where(m => m.AssetId == asset.Id)
                .OrderByDescending(m => m.CalculatedAt)
                .FirstOrDefaultAsync();

            // Use default values if metrics not found
            if (metrics == null)
            {
                metrics = new AssetMetrics
                {
                    CurrentHealth = 0,
                    PerformanceIndex = 0,
                    SecurityIndex = 0,
                    CalculatedAt = DateTime.Now
                };
            }

            // Use metrics for indices (from stored AssetMetrics)
            dashboardDto.HealthIndex = metrics.CurrentHealth;
            dashboardDto.HealthStatus = GetHealthStatus(dashboardDto.HealthIndex);

            dashboardDto.PerformanceIndex = (int)Math.Round(metrics.PerformanceIndex);
            dashboardDto.PerformanceStatus = GetPerformanceStatus(dashboardDto.PerformanceIndex);

            dashboardDto.ComplianceIndex = (int)Math.Round(metrics.OverallComplianceMetric);
            dashboardDto.ComplianceStatus = GetComplianceStatus(dashboardDto.ComplianceIndex);

            // Current Status (based on latest KPI result where KpiId = 1)
            var latestHealthKpi = asset.KPIsResults
                .Where(k => k.KpiId == 1)
                .OrderByDescending(k => k.UpdatedAt)
                .FirstOrDefault();
            if (latestHealthKpi != null)
            {
                dashboardDto.CurrentStatus = IsAssetUp(latestHealthKpi) ? "UP" : "DOWN";
                dashboardDto.LastChecked = latestHealthKpi.UpdatedAt;
            }
            else
            {
                dashboardDto.CurrentStatus = "UNKNOWN";
                dashboardDto.LastChecked = null;
            }

            // Last Outage (find when status was Down from KPIsResultHistory)
            var assetId = asset.Id;
            var downKpisCreatedAt = await _context.KPIsResultHistories
                .AsNoTracking()
                .Where(k => k.AssetId == assetId && k.KpiId == 1)
                .Where(k => (!string.IsNullOrWhiteSpace(k.Result) && k.Result.ToLower() == "miss") ||
                           (!string.IsNullOrWhiteSpace(k.Target) && k.Target.ToLower() == "miss"))
                .OrderByDescending(k => k.CreatedAt)
                .Select(k => k.CreatedAt)
                .FirstOrDefaultAsync();
            if (downKpisCreatedAt != default(DateTime))
            {
                dashboardDto.LastOutageDate = downKpisCreatedAt;
                dashboardDto.LastOutage = GetTimeAgo(downKpisCreatedAt);
            }
            else
            {
                dashboardDto.LastOutage = "NO OUTAGES";
                dashboardDto.LastOutageDate = null;
            }

            // Risk Exposure Index (based on Health and Compliance from metrics)
            // Use standardized status format
            var riskStatus = GetIndexStatus((metrics.CurrentHealth + metrics.SecurityIndex) / 2.0);
            if (riskStatus == "UNKNOWN")
                dashboardDto.RiskExposureIndex = "UNKNOWN";
            else if (riskStatus == "HIGH")
                dashboardDto.RiskExposureIndex = "LOW RISK";  // High index = Low risk
            else if (riskStatus == "MEDIUM")
                dashboardDto.RiskExposureIndex = "MEDIUM RISK";
            else
                dashboardDto.RiskExposureIndex = "HIGH RISK";  // Low index = High risk

            // Open Incidents
            var openIncidents = asset.Incidents
                .Where(i => i.DeletedAt == null && i.StatusId != 12)
                .ToList();
            dashboardDto.OpenIncidents = openIncidents.Count;
            
            // High Severity Incidents (P1 - Critical)
            dashboardDto.HighSeverityIncidents = openIncidents
                .Count(i => (i.Severity?.Name.Contains("P1", StringComparison.OrdinalIgnoreCase) ?? false) || 
                           (i.Severity?.Name.Contains("Critical", StringComparison.OrdinalIgnoreCase) ?? false));

            return dashboardDto;
        }

        /// <summary>
        /// Gets status based on index value using standardized thresholds:
        /// <30% = LOW, 30-70% = MEDIUM, >70% = HIGH
        /// </summary>
        private static string GetIndexStatus(double index)
        {
            if (index == 0) return "UNKNOWN";
            if (index < 30) return "LOW";
            if (index <= 70) return "MEDIUM";
            return "HIGH";
        }

        /// <summary>
        /// Gets status based on index value using standardized thresholds (int overload)
        /// </summary>
        private static string GetIndexStatus(int index)
        {
            return GetIndexStatus((double)index);
        }

        private static string GetHealthStatus(int healthIndex)
        {
            // Use standardized thresholds: <30% = LOW, 30-70% = MEDIUM, >70% = HIGH
            var status = GetIndexStatus(healthIndex);
            if (status == "UNKNOWN") return "UNKNOWN";
            if (status == "HIGH") return "HEALTHY";
            if (status == "MEDIUM") return "FAIR";
            return "POOR"; // LOW
        }

        /// <summary>Sort rank for health status: POOR=0, FAIR=1, HEALTHY=2, UNKNOWN=3. Ascending = worst first; descending = best first.</summary>
        private static int GetHealthStatusSortRank(int healthIndex)
        {
            var status = GetHealthStatus(healthIndex);
            return status switch
            {
                "POOR" => 0,
                "FAIR" => 1,
                "HEALTHY" => 2,
                _ => 3 // UNKNOWN
            };
        }

        private static string GetPerformanceStatus(int performanceIndex)
        {
            // Use standardized thresholds: <30% = LOW, 30-70% = MEDIUM, >70% = HIGH
            var status = GetIndexStatus(performanceIndex);
            if (status == "UNKNOWN") return "UNKNOWN";
            if (status == "HIGH") return "GOOD";
            if (status == "MEDIUM") return "AVERAGE";
            return "BELOW AVERAGE"; // LOW
        }

        private static string GetComplianceStatus(int complianceIndex)
        {
            // Use standardized thresholds: <30% = LOW, 30-70% = MEDIUM, >70% = HIGH
            var status = GetIndexStatus(complianceIndex);
            if (status == "UNKNOWN") return "UNKNOWN";
            return status; // HIGH, MEDIUM, or LOW
        }

        private static string GetRiskStatus(int index)
        {
            // Use standardized thresholds: <30% = LOW, 30-70% = MEDIUM, >70% = HIGH
            // For risk: HIGH index = LOW risk (good), LOW index = HIGH risk (bad)
            var status = GetIndexStatus(index);
            if (status == "UNKNOWN") return "UNKNOWN";
            if (status == "HIGH") return "LOW";      // High index = Low risk (good)
            if (status == "MEDIUM") return "MEDIUM";  // Medium index = Medium risk
            return "HIGH";                            // Low index = High risk (bad)
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

        /// <summary>Sort key for currentStatus: Down=0, Unknown=1, Up=2 so ascending order is Down, Unknown, Up.</summary>
        private static int GetCurrentStatusSortKey(Asset asset)
        {
            var latestHealthKpi = asset.KPIsResults?
                .Where(k => k.KpiId == 1)
                .OrderByDescending(k => k.CreatedAt)
                .FirstOrDefault();
            if (latestHealthKpi == null) return 1; // Unknown
            return IsAssetUp(latestHealthKpi) ? 2 : 0; // Up=2, Down=0
        }

        /// <summary>Returns last checked DateTime for sorting; null sorts last.</summary>
        private static DateTime? GetLastCheckedSortKey(Asset asset)
        {
            return asset.KPIsResults?
                .Where(k => k.KpiId == 1)
                .OrderByDescending(k => k.CreatedAt)
                .Select(k => (DateTime?)k.CreatedAt)
                .FirstOrDefault();
        }

        /// <summary>Sort key for risk exposure: HIGH RISK=0, MEDIUM=1, LOW RISK=2, UNKNOWN=3 for ascending.</summary>
        private static int GetRiskExposureIndexSortKey(AssetMetrics? metrics)
        {
            if (metrics == null) return 3;
            var riskStatus = GetIndexStatus((metrics.CurrentHealth + metrics.SecurityIndex) / 2.0);
            if (riskStatus == "UNKNOWN") return 3;
            if (riskStatus == "HIGH") return 2;   // LOW RISK
            if (riskStatus == "MEDIUM") return 1; // MEDIUM RISK
            return 0; // LOW index = HIGH RISK
        }

        private static int GetHighSeverityIncidentsCount(Asset asset)
        {
            var openIncidents = asset.Incidents?.Where(i => i.DeletedAt == null).ToList() ?? new List<Incident>();
            return openIncidents.Count(i => (i.Severity?.Name.Contains("P1", StringComparison.OrdinalIgnoreCase) ?? false) ||
                                            (i.Severity?.Name.Contains("Critical", StringComparison.OrdinalIgnoreCase) ?? false));
        }

        private static string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.Now - dateTime;
            
            if (timeSpan.TotalMinutes < 1)
                return "Just now";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} minute{(timeSpan.TotalMinutes >= 2 ? "s" : "")} ago";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} hour{(timeSpan.TotalHours >= 2 ? "s" : "")} ago";
            if (timeSpan.TotalDays < 30)
                return $"{(int)timeSpan.TotalDays} day{(timeSpan.TotalDays >= 2 ? "s" : "")} ago";
            if (timeSpan.TotalDays < 365)
                return $"{(int)(timeSpan.TotalDays / 30)} month{(timeSpan.TotalDays / 30 >= 2 ? "s" : "")} ago";
            
            return $"{(int)(timeSpan.TotalDays / 365)} year{((int)(timeSpan.TotalDays / 365) >= 2 ? "s" : "")} ago";
        }

        private static string CalculateRiskExposure(int healthIndex, int complianceIndex)
        {
            // Calculate risk based on health and compliance
            var avgIndex = (healthIndex + complianceIndex) / 2;
            
            if (avgIndex == 0) return "UNKNOWN";
            if (avgIndex >= 70) return "LOW RISK";
            if (avgIndex >= 40) return "MEDIUM RISK";
            return "HIGH RISK";
        }

        /// <summary>
        /// Gets asset criticality multiplier based on Citizen Impact Level
        /// High = 100%, Medium = 60%, Low = 30%
        /// </summary>
        private static double GetAssetCriticalityMultiplier(string citizenImpactLevel)
        {
            if (string.IsNullOrWhiteSpace(citizenImpactLevel))
                return 0.3; // Default to Low

            var level = citizenImpactLevel.ToUpper();
            if (level.Contains("HIGH", StringComparison.OrdinalIgnoreCase))
                return 1.0; // 100%
            if (level.Contains("MEDIUM", StringComparison.OrdinalIgnoreCase) ||
                level.Contains("MED", StringComparison.OrdinalIgnoreCase))
                return 0.6; // 60%
            return 0.3; // Low = 30%
        }

        public async Task<APIResponse> GetDashboardSummaryAsync()
        {
            // Get all assets with related data
            var assets = await _context.Assets
                .Include(a => a.KPIsResults)
                    .ThenInclude(k => k.KpisLov)
                .Include(a => a.Incidents)
                    .ThenInclude(i => i.Severity)
                .Include(a => a.Incidents)
                    .ThenInclude(i => i.Status)
                .Include(a => a.CitizenImpactLevel)
                .Where(a => a.DeletedAt == null)
                .ToListAsync();

            var totalAssets = assets.Count;
            var assetIds = assets.Select(a => a.Id).ToList();

            // Get latest AssetMetrics for all assets directly from database (no calculations)
            var latestMetrics = await _context.AssetMetrics
                .Where(m => assetIds.Contains(m.AssetId))
                .GroupBy(m => m.AssetId)
                .Select(g => g.OrderByDescending(m => m.CalculatedAt).First())
                .ToListAsync();

            var metricsDict = latestMetrics.ToDictionary(m => m.AssetId);

            // Build dashboard metrics from stored AssetMetrics
            var assetMetricsList = new List<dynamic>();
            foreach (var asset in assets)
            {
                // Get AssetMetrics from database (no calculation)
                metricsDict.TryGetValue(asset.Id, out var metrics);

                // Use stored metrics or default to 0 if not found
                var healthIndex = metrics?.CurrentHealth ?? 0;
                var performanceIndex = metrics?.PerformanceIndex ?? 0.0;
                var complianceIndex = metrics?.SecurityIndex ?? 0.0;

                var riskIndex = metrics?.DigitalRiskExposureIndex??0.0;

                // Check if asset is online (based on latest KPI result where KpiId = 1)
                var latestHealthKpi = asset.KPIsResults
                    .Where(k => k.KpiId == 1)
                    .OrderByDescending(k => k.CreatedAt)
                    .FirstOrDefault();
                var isOnline = latestHealthKpi != null && IsAssetUp(latestHealthKpi);

                // Open incidents
                var openIncidents = asset.Incidents
                    .Where(i => i.DeletedAt == null && i.StatusId != 12)
                    .ToList();

                assetMetricsList.Add(new
                {
                    HealthIndex = (double)healthIndex,
                    PerformanceIndex = performanceIndex,
                    ComplianceIndex = complianceIndex,
                    RiskIndex = riskIndex,
                    IsOnline = isOnline,
                    OpenIncidents = openIncidents.Count,
                    CriticalSeverityIncidents = openIncidents.Count(i =>
                        (i.Severity?.Name.Contains("P1", StringComparison.OrdinalIgnoreCase) ?? false) ||
                        (i.Severity?.Name.Contains("Critical", StringComparison.OrdinalIgnoreCase) ?? false))
                });
            }
            var assetMetrics = assetMetricsList;

            // Calculate summary metrics
            // Assets online: count assets with latest KPI result (KpiId = 1) showing they're up
            var assetsOnline = assetMetrics.Count(m => m.IsOnline);
            var assetsOnlinePercentage = totalAssets > 0 
                ? (double)assetsOnline / totalAssets * 100 
                : 0;

            // Calculate indices as average of assets with non-zero values only
            // Use double precision for weighted calculations
            var assetsWithHealthData = assetMetrics.Where(m => m.HealthIndex > 0).ToList();
            var overallHealthIndex = assetsWithHealthData.Any() 
                ? assetsWithHealthData.Average(m => (double)m.HealthIndex) 
                : 0.0;

            var assetsWithPerformanceData = assetMetrics.Where(m => m.PerformanceIndex > 0).ToList();
            var overallPerformanceIndex = assetsWithPerformanceData.Any() 
                ? assetsWithPerformanceData.Average(m => (double)m.PerformanceIndex) 
                : 0.0;

            var assetsWithComplianceData = assetMetrics.Where(m => m.ComplianceIndex > 0).ToList();
            var overallComplianceIndex = assetsWithComplianceData.Any() 
                ? assetsWithComplianceData.Average(m => (double)m.ComplianceIndex) 
                : 0.0;

            var highRiskAssets = assetMetrics.Count(m => m.RiskIndex > 70);
            var highRiskAssetsStatus = highRiskAssets == 0 ? "LOW" :
                                      highRiskAssets <= 20 ? "MEDIUM" : "HIGH";

            var totalOpenIncidents = assetMetrics.Sum(m => m.OpenIncidents);
            var totalCriticalSeverityIncidents = assetMetrics.Sum(m => m.CriticalSeverityIncidents);
            var criticalSeverityPercentage = totalOpenIncidents > 0 
                ? (double)totalCriticalSeverityIncidents / totalOpenIncidents * 100 
                : 0;

            var summary = new DashboardSummaryDto
            {
                TotalDigitalAssetsMonitored = totalAssets,
                AssetsOnline = assetsOnline,
                AssetsOnlinePercentage = Math.Round(assetsOnlinePercentage, 2),
                HealthIndex = Math.Round(overallHealthIndex, 2),
                HealthStatus = GetHealthStatus((int)overallHealthIndex),
                PerformanceIndex = Math.Round(overallPerformanceIndex, 2),
                PerformanceStatus = GetPerformanceStatus((int)overallPerformanceIndex),
                ComplianceIndex = Math.Round(overallComplianceIndex, 2),
                ComplianceStatus = GetComplianceStatus((int)overallComplianceIndex),
                HighRiskAssets = highRiskAssets,
                HighRiskAssetsStatus = highRiskAssetsStatus,
                OpenIncidents = totalOpenIncidents,
                CriticalSeverityOpenIncidents = totalCriticalSeverityIncidents,
                CriticalSeverityOpenIncidentsPercentage = Math.Round(criticalSeverityPercentage, 2)
            };

            return new APIResponse
            {
                IsSuccessful = true,
                Message = "Dashboard summary retrieved successfully",
                Data = summary
            };
        }

        public async Task<APIResponse> GetAssetDashboardHeaderAsync(int assetId)
        {
            var asset = await _context.Assets
                .Include(a => a.Ministry)
                .Include(a => a.Department)
                .Include(a => a.CitizenImpactLevel)
                .Include(a => a.KPIsResults)
                    .ThenInclude(k => k.KpisLov)
                .Include(a => a.Incidents)
                    .ThenInclude(i => i.Severity)
                .Include(a => a.Incidents)
                    .ThenInclude(i => i.Status)
                .FirstOrDefaultAsync(a => a.Id == assetId && a.DeletedAt == null);

            if (asset == null)
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "Asset not found.",
                    Data = null
                };
            }

            // Get AssetMetrics directly from database (no calculations)
            var metrics = await _context.AssetMetrics
                .Where(m => m.AssetId == assetId)
                .OrderByDescending(m => m.CalculatedAt)
                .FirstOrDefaultAsync();

            // Use default values if metrics not found
            if (metrics == null)
            {
                metrics = new AssetMetrics
                {
                    CurrentHealth = 0,
                    PerformanceIndex = 0,
                    SecurityIndex = 0,
                    AccessibilityIndex = 0,
                    AvailabilityIndex = 0,
                    NavigationIndex = 0,
                    UserExperienceIndex = 0,
                    CitizenHappinessMetric = 0,
                    OverallComplianceMetric = 0,
                    DigitalRiskExposureIndex = 0,
                    CalculatedAt = DateTime.Now
                };
            }

            // Use metrics for all calculated values
            var accessibilityIndex = metrics.AccessibilityIndex;
            var availabilityIndex = metrics.AvailabilityIndex;
            var navigationIndex = metrics.NavigationIndex;
            var performanceIndex = metrics.PerformanceIndex;
            var securityIndex = metrics.SecurityIndex;
            var userExperienceIndex = metrics.UserExperienceIndex;
            var citizenHappinessMetric = metrics.CitizenHappinessMetric;
            var overallComplianceMetric = metrics.OverallComplianceMetric;
            var drei = metrics.DigitalRiskExposureIndex;

            // Risk Exposure Index – send status (LOW, MEDIUM, HIGH, UNKNOWN) instead of numeric value
            var riskExposureIndexStatus = GetRiskStatus((int)Math.Round(drei));

            // Current Health Status
            var currentHealthStatus = GetIndexStatus(metrics.CurrentHealth);

            // Calculate Compliance Overview Category Statuses from metrics
            var accessibilityStatus = GetIndexStatus(accessibilityIndex);
            var availabilityReliabilityStatus = GetIndexStatus(availabilityIndex);
            var navigationStatus = GetIndexStatus(navigationIndex);
            var performanceStatus = GetIndexStatus(performanceIndex);
            var securityStatus = GetIndexStatus(securityIndex);
            var userExperienceStatus = GetIndexStatus(userExperienceIndex);

            // Current Status (based on CurrentHealth from AssetMetrics)
            var currentStatus = "UNKNOWN";
            var lastOutage = "N/A";

            // Current Status (based on latest KPI result where KpiId = 1)
            var latestHealthKpi = asset.KPIsResults
                .Where(k => k.KpiId == 1)
                .OrderByDescending(k => k.CreatedAt)
                .FirstOrDefault();
            
            if (latestHealthKpi != null)
            {
                currentStatus = IsAssetUp(latestHealthKpi) ? "UP" : "DOWN";
                
                // Last Outage (find when status was Down from KPIsResultHistory)
                var assetIdForQuery = asset.Id;
                var downKpisCreatedAt = await _context.KPIsResultHistories
                    .AsNoTracking()
                    .Where(k => k.AssetId == assetIdForQuery && 
                               k.KpiId == 1 &&
                               ((!string.IsNullOrWhiteSpace(k.Result) && k.Result.ToLower() == "miss") ||
                                (!string.IsNullOrWhiteSpace(k.Target) && k.Target.ToLower() == "miss")))
                    .Select(k => k.CreatedAt)
                    .FirstOrDefaultAsync();
                
                if (downKpisCreatedAt != default(DateTime))
                {
                    lastOutage = GetTimeAgo(downKpisCreatedAt);
                }
                else
                {
                    lastOutage = "NO OUTAGES";
                }
            }
            else
            {
                currentStatus = "UNKNOWN";
                lastOutage = "N/A";
            }

            // Extract domain from URL for display
            var assetUrl = asset.AssetUrl;
            try
            {
                if (Uri.TryCreate(asset.AssetUrl, UriKind.Absolute, out var uri))
                {
                    assetUrl = uri.Host;
                }
            }
            catch
            {
                // Keep original URL if parsing fails
            }

            // Get open incidents for counts
            var allIncidents = asset.Incidents.Where(i => i.DeletedAt == null).ToList();
            var openIncidents = allIncidents
                .Where(i => i.StatusId != 12)
                .ToList();

            var openCriticalIncidents = openIncidents.Count(i =>
                (i.Severity?.Name.Contains("P1", StringComparison.OrdinalIgnoreCase) ?? false) ||
                (i.Severity?.Name.Contains("Critical", StringComparison.OrdinalIgnoreCase) ?? false));

            var openIncidentsCount = openIncidents.Count;
            var highSeverityOpenIncidents = openCriticalIncidents;

            // Ownership & Accountability fields
            var ownerName = string.IsNullOrWhiteSpace(asset.PrimaryContactName) ? "Not Assigned" : asset.PrimaryContactName;
            var ownerEmail = string.IsNullOrWhiteSpace(asset.PrimaryContactEmail) ? "NA" : asset.PrimaryContactEmail;
            var ownerContact = string.IsNullOrWhiteSpace(asset.PrimaryContactPhone) ? "NA" : asset.PrimaryContactPhone;
            var technicalOwnerName = string.IsNullOrWhiteSpace(asset.TechnicalContactName) ? "Not Assigned" : asset.TechnicalContactName;
            var technicalOwnerEmail = string.IsNullOrWhiteSpace(asset.TechnicalContactEmail) ? "NA" : asset.TechnicalContactEmail;
            var technicalOwnerContact = string.IsNullOrWhiteSpace(asset.TechnicalContactPhone) ? "NA" : asset.TechnicalContactPhone;

            var headerDto = new AssetDashboardHeaderDto
            {
                AssetUrl = assetUrl,
                AssetName = asset.AssetName,
                Ministry = asset.Ministry?.MinistryName ?? "UNKNOWN MINISTRY",
                Department = asset.Department?.DepartmentName ?? string.Empty,
                CitizenImpactLevel = asset.CitizenImpactLevel?.Name ?? "UNKNOWN",
                CurrentHealth = currentHealthStatus,
                RiskExposureIndex = riskExposureIndexStatus,
                CurrentStatus = currentStatus,
                LastOutage = lastOutage,
                OwnerName = ownerName,
                OwnerEmail = ownerEmail,
                OwnerContact = ownerContact,
                TechnicalOwnerName = technicalOwnerName,
                TechnicalOwnerEmail = technicalOwnerEmail,
                TechnicalOwnerContact = technicalOwnerContact,
                AccessibilityInclusivityStatus = accessibilityStatus,
                AvailabilityReliabilityStatus = availabilityReliabilityStatus,
                NavigationDiscoverabilityStatus = navigationStatus,
                PerformanceEfficiencyStatus = performanceStatus,
                SecurityTrustPrivacyStatus = securityStatus,
                UserExperienceJourneyQualityStatus = userExperienceStatus,
                CitizenHappinessMetric = Math.Round(citizenHappinessMetric, 2),
                OverallComplianceMetric = Math.Round(overallComplianceMetric, 2),
                OpenIncidents = openIncidentsCount,
                HighSeverityOpenIncidents = highSeverityOpenIncidents
            };

            return new APIResponse
            {
                IsSuccessful = true,
                Message = "Asset dashboard header retrieved successfully",
                Data = headerDto
            };
        }

        public async Task<APIResponse> GetAssetControlPanelAsync(int assetId)
        {
            var asset = await _context.Assets
                .Include(a => a.Ministry)
                .Include(a => a.Department)
                .Include(a => a.CitizenImpactLevel)
                .Include(a => a.KPIsResults)
                    .ThenInclude(k => k.KpisLov)
                .Include(a => a.Incidents)
                    .ThenInclude(i => i.Severity)
                .Include(a => a.Incidents)
                    .ThenInclude(i => i.Status)
                .FirstOrDefaultAsync(a => a.Id == assetId && a.DeletedAt == null);

            if (asset == null)
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "Asset not found.",
                    Data = null
                };
            }

            // Get AssetMetrics directly from database (no calculations)
            var metrics = await _context.AssetMetrics
                .Where(m => m.AssetId == assetId)
                .OrderByDescending(m => m.CalculatedAt)
                .FirstOrDefaultAsync();

            // Use default values if metrics not found
            if (metrics == null)
            {
                metrics = new AssetMetrics
                {
                    CurrentHealth = 0,
                    PerformanceIndex = 0,
                    SecurityIndex = 0,
                    AccessibilityIndex = 0,
                    AvailabilityIndex = 0,
                    NavigationIndex = 0,
                    UserExperienceIndex = 0,
                    CitizenHappinessMetric = 0,
                    OverallComplianceMetric = 0,
                    DigitalRiskExposureIndex = 0,
                    CalculatedAt = DateTime.Now
                };
            }

            // Use metrics for all calculated values
            var headerAccessibilityIndex = metrics.AccessibilityIndex;
            var headerAvailabilityIndex = metrics.AvailabilityIndex;
            var headerNavigationIndex = metrics.NavigationIndex;
            var headerPerformanceIndex = metrics.PerformanceIndex;
            var headerSecurityIndex = metrics.SecurityIndex;
            var headerUserExperienceIndex = metrics.UserExperienceIndex;
            var headerCitizenHappinessMetric = metrics.CitizenHappinessMetric;
            var headerOverallComplianceMetric = metrics.OverallComplianceMetric;
            var headerDrei = metrics.DigitalRiskExposureIndex;

            // Risk Exposure Index – send status (LOW, MEDIUM, HIGH, UNKNOWN) instead of numeric value
            var headerRiskExposureIndexStatus = GetRiskStatus((int)Math.Round(headerDrei));

            // Current Health Status
            var headerCurrentHealthStatus = GetIndexStatus(metrics.CurrentHealth);

            // Calculate Compliance Overview Category Statuses from metrics
            var headerAccessibilityStatus = GetIndexStatus(headerAccessibilityIndex);
            var headerAvailabilityReliabilityStatus = GetIndexStatus(headerAvailabilityIndex);
            var headerNavigationStatus = GetIndexStatus(headerNavigationIndex);
            var headerPerformanceStatus = GetIndexStatus(headerPerformanceIndex);
            var headerSecurityStatus = GetIndexStatus(headerSecurityIndex);
            var headerUserExperienceStatus = GetIndexStatus(headerUserExperienceIndex);

            // Get open incidents for counts
            var headerAllIncidents = asset.Incidents.Where(i => i.DeletedAt == null).ToList();
            var headerOpenIncidents = headerAllIncidents
                .Where(i => i.StatusId != 12)
                .ToList();

            var headerOpenCriticalIncidents = headerOpenIncidents.Count(i =>
                (i.Severity?.Name.Contains("P1", StringComparison.OrdinalIgnoreCase) ?? false) ||
                (i.Severity?.Name.Contains("Critical", StringComparison.OrdinalIgnoreCase) ?? false));

            // Current Status (based on CurrentHealth from AssetMetrics)
            var headerCurrentStatus = "UNKNOWN";
            var headerLastOutage = "N/A";

            // Current Status (based on latest KPI result where KpiId = 1)
            var headerLatestHealthKpi = asset.KPIsResults
                .Where(k => k.KpiId == 1)
                .OrderByDescending(k => k.CreatedAt)
                .FirstOrDefault();
            
            if (headerLatestHealthKpi != null)
            {
                headerCurrentStatus = IsAssetUp(headerLatestHealthKpi) ? "UP" : "DOWN";
                
                // Last Outage (find when status was Down from KPIsResultHistory)
                var headerAssetIdForQuery = asset.Id;
                var headerDownKpisCreatedAt = await _context.KPIsResultHistories
                    .AsNoTracking()
                    .Where(k => k.AssetId == headerAssetIdForQuery && 
                               k.KpiId == 1 &&
                               ((!string.IsNullOrWhiteSpace(k.Result) && k.Result.ToLower() == "miss") ||
                                (!string.IsNullOrWhiteSpace(k.Target) && k.Target.ToLower() == "miss")))
                    .OrderByDescending(k => k.CreatedAt)
                    .Select(k => k.CreatedAt)
                    .FirstOrDefaultAsync();
                headerLastOutage = headerDownKpisCreatedAt != default(DateTime) ? GetTimeAgo(headerDownKpisCreatedAt) : "NO OUTAGES";
            }
            else
            {
                headerCurrentStatus = "UNKNOWN";
                headerLastOutage = "N/A";
            }

            // Extract domain from URL
            var headerAssetUrl = asset.AssetUrl;
            try
            {
                if (Uri.TryCreate(asset.AssetUrl, UriKind.Absolute, out var uri))
                {
                    headerAssetUrl = uri.Host;
                }
            }
            catch { }

            // Ownership fields
            var ownerName = string.IsNullOrWhiteSpace(asset.PrimaryContactName) ? "Not Assigned" : asset.PrimaryContactName;
            var ownerEmail = string.IsNullOrWhiteSpace(asset.PrimaryContactEmail) ? "NA" : asset.PrimaryContactEmail;
            var ownerContact = string.IsNullOrWhiteSpace(asset.PrimaryContactPhone) ? "NA" : asset.PrimaryContactPhone;
            var technicalOwnerName = string.IsNullOrWhiteSpace(asset.TechnicalContactName) ? "Not Assigned" : asset.TechnicalContactName;
            var technicalOwnerEmail = string.IsNullOrWhiteSpace(asset.TechnicalContactEmail) ? "NA" : asset.TechnicalContactEmail;
            var technicalOwnerContact = string.IsNullOrWhiteSpace(asset.TechnicalContactPhone) ? "NA" : asset.TechnicalContactPhone;

            var header = new AssetDashboardHeaderDto
            {
                AssetUrl = headerAssetUrl,
                AssetName = asset.AssetName,
                MinistryId = asset.MinistryId,
                Ministry = asset.Ministry?.MinistryName ?? "UNKNOWN MINISTRY",
                Department = asset.Department?.DepartmentName ?? string.Empty,
                CitizenImpactLevel = asset.CitizenImpactLevel?.Name ?? "UNKNOWN",
                CurrentHealth = headerCurrentHealthStatus,
                RiskExposureIndex = headerRiskExposureIndexStatus,
                CurrentStatus = headerCurrentStatus,
                LastOutage = headerLastOutage,
                OwnerName = ownerName,
                OwnerEmail = ownerEmail,
                OwnerContact = ownerContact,
                TechnicalOwnerName = technicalOwnerName,
                TechnicalOwnerEmail = technicalOwnerEmail,
                TechnicalOwnerContact = technicalOwnerContact,
                AccessibilityInclusivityStatus = headerAccessibilityStatus,
                AvailabilityReliabilityStatus = headerAvailabilityReliabilityStatus,
                NavigationDiscoverabilityStatus = headerNavigationStatus,
                PerformanceEfficiencyStatus = headerPerformanceStatus,
                SecurityTrustPrivacyStatus = headerSecurityStatus,
                UserExperienceJourneyQualityStatus = headerUserExperienceStatus,
                CitizenHappinessMetric = Math.Round(headerCitizenHappinessMetric, 2),
                OverallComplianceMetric = Math.Round(headerOverallComplianceMetric, 2),
                OpenIncidents = headerOpenIncidents.Count,
                HighSeverityOpenIncidents = headerOpenCriticalIncidents
            };

            // Get all KPIs for this asset: History (last 30 days) for hit/miss KPIs, Results for numeric/manual
            var assetIdForKpiQuery = asset.Id;
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

            // Load KPI history for last 30 days (used for hit/miss KPIs: current value = hit rate % over window)
            var historyRows = await _context.KPIsResultHistories
                .AsNoTracking()
                .Where(h => h.AssetId == assetIdForKpiQuery && h.CreatedAt >= thirtyDaysAgo)
                .Select(h => new KpiHistoryRowDto { KpiId = h.KpiId, Result = h.Result ?? "", Target = h.Target ?? "", CreatedAt = h.CreatedAt })
                .ToListAsync();

            // Load KPIsResults (latest per asset; used for numeric and manual KPIs only)
            var allKpisResults = await _context.KPIsResults
                .AsNoTracking()
                .Where(k => k.AssetId == assetIdForKpiQuery)
                .Select(k => new
                {
                    k.KpiId,
                    k.Result,
                    k.Target,
                    k.CreatedAt,
                    k.UpdatedAt
                })
                .ToListAsync();

            // Hit/miss KPIs: use Target column, current value = (hits / total) * 100 over last 30 days
            var hitMissKpiIds = new HashSet<int> { 1, 2, 3, 4, 5, 9, 10, 11, 12, 13, 14, 16, 17, 18, 19, 20, 21, 22, 23, 24 };

            // Get all KPIs from KpisLov, grouped by category
            var allKpis = await _context.KpisLovs
                .Where(k => k.DeletedAt == null)
                .OrderBy(k => k.Id)
                .ThenBy(k => k.Id)
                .ToListAsync();

            // Group KPIs by category
            var kpiCategories = allKpis
                .GroupBy(k => k.KpiGroup)
                .Select(g => new KpiCategoryDto
                {
                    CategoryName = g.Key,
                    Kpis = g.Select(kpi =>
                    {
                        if (hitMissKpiIds.Contains(kpi.Id))
                        {
                            return MapToKpiItemDtoFromHistoryHitMiss(kpi, asset.CitizenImpactLevel?.Name, historyRows);
                        }
                        if (kpi.Id == 15)
                        {
                            return MapToKpiItemDtoFromHistoryAvgResult(kpi, asset.CitizenImpactLevel?.Name, historyRows);
                        }
                        var allKpiResults = allKpisResults.Where(r => r.KpiId == kpi.Id).ToList();
                        return MapToKpiItemDtoFromHistory(kpi, asset.CitizenImpactLevel?.Name, allKpiResults);
                    }).ToList()
                })
                .Where(c => c.Kpis.Any())
                .ToList();

            var controlPanelDto = new AssetControlPanelDto
            {
                Header = header,
                KpiCategories = kpiCategories
            };

            return new APIResponse
            {
                IsSuccessful = true,
                Message = "Asset control panel retrieved successfully",
                Data = controlPanelDto
            };
        }

        private KpiItemDto MapToKpiItemDto(KpisLov kpi, KPIsResult? latestResult)
        {
            // Format target (use TargetHigh as primary target)
            var target = string.IsNullOrWhiteSpace(kpi.TargetHigh) 
                ? (string.IsNullOrWhiteSpace(kpi.TargetMedium) ? kpi.TargetLow : kpi.TargetMedium)
                : kpi.TargetHigh;

            if (string.IsNullOrWhiteSpace(target))
                target = "N/A";

            // Get current value
            var currentValue = latestResult != null && !string.IsNullOrWhiteSpace(latestResult.Result)
                ? latestResult.Result
                : "N/A";

            // Calculate SLA status
            var slaStatus = "N/A"; // CalculateSlaStatus(kpi, currentValue, target);

            // Get last checked time
            var lastChecked = latestResult != null
                ? GetTimeAgo(latestResult.CreatedAt)
                : "N/A";

            // Get data source
            var dataSource = string.IsNullOrWhiteSpace(kpi.Manual) || kpi.Manual.Equals("Auto", StringComparison.OrdinalIgnoreCase)
                ? "auto"
                : "Manual";

            return new KpiItemDto
            {
                KpiId = kpi.Id,
                KpiName = kpi.KpiName,
                Manual = kpi.Manual,
                Target = target,
                CurrentValue = currentValue,
                SlaStatus = slaStatus,
                LastChecked = lastChecked,
                DataSource = dataSource
            };
        }

        /// <summary>
        /// Ensures target has unit suffix when stored as bare number (e.g. "3" -> "3 sec" for KPI 6,7; "3" -> "3 MB" for KPI 8; "95" -> "95%" for % KPIs).
        /// </summary>
        private static string EnsureTargetHasUnit(int kpiId, string target)
        {
            if (string.IsNullOrWhiteSpace(target) || target == "N/A") return target ?? "N/A";
            var t = target.Trim();
            // Already has unit if GetUnitFromTarget finds one
            if (!string.IsNullOrEmpty(GetUnitFromTarget(t))) return target;
            // Bare number: append unit by KPI
            if (kpiId == 6 || kpiId == 7) return t + " sec";
            if (kpiId == 8) return t + " MB";
            if (new[] { 1, 2, 15, 16, 17, 18, 20, 23 }.Contains(kpiId)) return t + "%";
            return target;
        }

        /// <summary>
        /// Extracts the unit from target string as it appears (e.g. "5 sec" -> "sec", "10 MB" -> "MB", "95%" -> "%") so current value uses the same unit.
        /// </summary>
        private static string? GetUnitFromTarget(string? target)
        {
            if (string.IsNullOrWhiteSpace(target)) return null;
            var t = target.Trim();
            int i = 0;
            while (i < t.Length && (char.IsDigit(t[i]) || t[i] == '.' || t[i] == ',' || t[i] == ' '))
                i++;
            var unit = t.Substring(i).Trim();
            return string.IsNullOrEmpty(unit) ? null : unit;
        }

        /// <summary>
        /// Gets current value for Asset Control Panel per spec; current value unit is taken from target (e.g. target "5 sec" -> current "3 sec", target "10 MB" -> current "3 MB").
        /// </summary>
        private static string GetCurrentValueForControlPanel(int kpiId, IEnumerable<dynamic>? allKpiResults, string? target = null)
        {
            if (allKpiResults == null || !allKpiResults.Any())
                return "N/A";

            var list = allKpiResults.Where(r => r != null).ToList();
            if (list.Count == 0)
                return "N/A";

            static string R(dynamic r) => r.Result?.ToString()?.Trim() ?? "";
            static string T(dynamic r) => r.Target?.ToString()?.Trim() ?? "";

            // Exclude "skipped" from all counts. If all entries are skipped, list is empty → N/A (and caller sets SLA to UNKNOWN).
            list = list.Where(r => !string.Equals(T(r), "skipped", StringComparison.OrdinalIgnoreCase)).ToList();
            if (list.Count == 0)
                return "N/A";

            // Percentage: hit/total * 100 (KPI 1, 2, 16, 17, 18) – “All the hit Record/Total Number of records”, value in %
            if (kpiId == 1 || kpiId == 2 || kpiId == 16 || kpiId == 17 || kpiId == 18)
            {
                int total = list.Count;
                int hit = list.Count(r =>
                {
                    var res = R(r).ToLowerInvariant();
                    var tar = T(r).ToLowerInvariant();
                    // Target is authoritative for outcome (hit/miss). Do not treat Result "true" as hit when Target is "miss".
                    if (tar == "miss" || tar == "fail") return false;
                    return tar == "hit" || tar == "pass" || res == "hit" || res == "pass";
                });
                if (total == 0) return "N/A";
                var pctUnit = GetUnitFromTarget(target) ?? "%";
                var sep = (pctUnit?.StartsWith("%") ?? true) ? "" : " ";
                return $"{Math.Round((double)hit / total * 100.0, 2)}{sep}{pctUnit}";
            }

            // Count of misses (KPI 3–14, 19, 20, 21, 22, 24 – not 23; 23 is avg %)
            if (new[] { 3, 4, 5, 9, 10, 11, 12, 13, 14, 19, 20, 21, 22, 24 }.Contains(kpiId))
            {
                int misses = list.Count(r =>
                {
                    var res = R(r).ToLowerInvariant();
                    var tar = T(r).ToLowerInvariant();
                    return res == "miss" || res == "false" || res == "fail" || tar == "miss" || tar == "fail";
                });
                return misses.ToString();
            }

            // Average in seconds (KPI 6, 7) – current value unit from target (e.g. "3 sec")
            if (kpiId == 6 || kpiId == 7)
            {
                var values = new List<double>();
                foreach (var r in list)
                {
                    var s = R(r);
                    if (double.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double v))
                        values.Add(v);
                    else
                    {
                        var extracted = ExtractNumericValue(s);
                        if (extracted.HasValue) values.Add(extracted.Value);
                    }
                }
                if (values.Count == 0) return "N/A";
                var unit = GetUnitFromTarget(target) ?? "sec";
                return $"{Math.Round(values.Average(), 2)} {unit}";
            }

            // Average page load size (KPI 8) – current value unit from target (e.g. "3 MB")
            if (kpiId == 8)
            {
                var values = new List<double>();
                string? unit = GetUnitFromTarget(target);
                if (unit == null)
                {
                    foreach (var r in list)
                    {
                        var s = R(r);
                        if (string.IsNullOrWhiteSpace(s)) continue;
                        var lower = s.ToLowerInvariant();
                        if (lower.Contains("gb")) { unit = "GB"; break; }
                        if (lower.Contains("mb")) { unit = "MB"; break; }
                        if (lower.Contains("kb")) { unit = "KB"; break; }
                    }
                }
                foreach (var r in list)
                {
                    var s = R(r);
                    if (string.IsNullOrWhiteSpace(s)) continue;
                    var extracted = ExtractNumericValue(s);
                    if (extracted.HasValue) values.Add(extracted.Value);
                    else if (double.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double v))
                        values.Add(v);
                }
                if (values.Count == 0) return "N/A";
                unit ??= "MB";
                return $"{Math.Round(values.Average(), 2)} {unit}";
            }

            // KPI 15 (WCAG compliance score): AVG(Result) as current value; target <= calculated → COMPLIANT. 20, 23 use history/miss % so only 15 hits this path.
            if (new[] { 15, 20, 23 }.Contains(kpiId))
            {
                var values = new List<double>();
                foreach (var r in list)
                {
                    var s = R(r);
                    if (double.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double v))
                        values.Add(v);
                    else
                    {
                        var extracted = ExtractNumericValue(s);
                        if (extracted.HasValue) values.Add(extracted.Value);
                    }
                }
                if (values.Count == 0) return "N/A";
                var pctUnit = GetUnitFromTarget(target) ?? "%";
                var sep = (pctUnit?.StartsWith("%") ?? true) ? "" : " ";
                return $"{Math.Round(values.Average(), 2)}{sep}{pctUnit}";
            }

            // Fallback: use existing average logic by outcome
            return CalculateAverageCurrentValue("", allKpiResults);
        }

        /// <summary>
        /// Calculates average current value from all KPI results based on outcome type
        /// For boolean results (true/false), returns percentage of false values
        /// </summary>
        private static string CalculateAverageCurrentValue(string outcome, IEnumerable<dynamic>? allKpiResults)
        {
            if (allKpiResults == null || !allKpiResults.Any())
                return "N/A";

            var results = allKpiResults
                .Where(r => r != null && r.Result != null)
                .Select(r => r.Result?.ToString()?.Trim())
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .ToList();

            if (!results.Any())
                return "N/A";

            var outcomeType = outcome?.Trim().ToUpper() ?? "";
            
            // Check if all results are boolean (true/false)
            var allBoolean = results.All(r => 
            {
                var lower = r?.ToLower();
                return lower == "true" || lower == "false";
            });

            // If all results are boolean, calculate percentage of false
            if (allBoolean)
            {
                var falseCount = results.Count(r => r?.ToLower() == "false");
                var totalCount = results.Count;
                var falsePercentage = totalCount > 0 ? (double)falseCount / totalCount * 100.0 : 0.0;
                return $"{Math.Round(falsePercentage, 2)}%";
            }

            var numericValues = new List<double>();

            foreach (var result in results)
            {
                if (string.IsNullOrWhiteSpace(result))
                    continue;

                double? numericValue = null;

                switch (outcomeType)
                {
                    case "FLAG":
                        // For Flag type, handle boolean strings and numeric values
                        var flagLower = result.ToLower();
                        if (flagLower == "true" || flagLower == "1" || flagLower == "pass" || flagLower == "hit" || flagLower == "yes")
                            numericValue = 1.0;
                        else if (flagLower == "false" || flagLower == "0" || flagLower == "fail" || flagLower == "miss" || flagLower == "no")
                            numericValue = 0.0;
                        else if (double.TryParse(result, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double flagNum))
                            numericValue = flagNum;
                        break;

                    case "SEC":
                    case "SECONDS":
                        // Handle numeric values (e.g., "2.14", "0.53", "0") and boolean strings
                        var secLower = result.ToLower();
                        if (secLower == "true")
                            numericValue = 1.0;
                        else if (secLower == "false")
                            numericValue = 0.0;
                        else if (double.TryParse(result, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double secNum))
                            numericValue = secNum;
                        else
                        {
                            // Try extracting from strings like "2.14 sec"
                            double? extracted = ExtractNumericValue(result);
                            if (extracted.HasValue)
                                numericValue = extracted.Value;
                        }
                        break;

                    case "MB":
                    case "MEGABYTES":
                        // Handle numeric values (e.g., "0.09", "0.16") and boolean strings
                        var mbLower = result.ToLower();
                        if (mbLower == "true")
                            numericValue = 1.0;
                        else if (mbLower == "false")
                            numericValue = 0.0;
                        else if (double.TryParse(result, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double mbNum))
                            numericValue = mbNum;
                        else
                        {
                            // Try extracting from strings like "0.09 MB"
                            double? extracted = ExtractNumericValue(result);
                            if (extracted.HasValue)
                                numericValue = extracted.Value;
                        }
                        break;

                    case "%":
                    case "PERCENT":
                    case "PERCENTAGE":
                        // Handle percentage values (e.g., "58.3%", "0%", "0.0%") and boolean strings
                        var percentLower = result.ToLower();
                        if (percentLower == "true")
                            numericValue = 100.0;
                        else if (percentLower == "false")
                            numericValue = 0.0;
                        else
                        {
                            // Extract numeric value from percentage strings
                            double? extracted = ExtractNumericValue(result);
                            if (extracted.HasValue)
                                numericValue = extracted.Value;
                        }
                        break;

                    default:
                        // For unknown types, try to parse as boolean first, then numeric
                        var defaultLower = result.ToLower();
                        if (defaultLower == "true" || defaultLower == "1")
                            numericValue = 1.0;
                        else if (defaultLower == "false" || defaultLower == "0")
                            numericValue = 0.0;
                        else if (double.TryParse(result, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double defaultNum))
                            numericValue = defaultNum;
                        break;
                }

                if (numericValue.HasValue)
                    numericValues.Add(numericValue.Value);
            }

            if (!numericValues.Any())
                return "N/A";

            var average = numericValues.Average();
            var formattedAverage = Math.Round(average, 2);

            // Format with appropriate unit
            return outcomeType switch
            {
                "SEC" or "SECONDS" => $"{formattedAverage} Sec",
                "MB" or "MEGABYTES" => $"{formattedAverage} MB",
                "%" or "PERCENT" or "PERCENTAGE" => $"{formattedAverage}%",
                "FLAG" => formattedAverage.ToString("F2"),
                _ => formattedAverage.ToString("F2")
            };
        }

        /// <summary>
        /// Extracts numeric value from a string, removing units (%, sec, MB, etc.) for SLA comparison.
        /// Handles "99.50%", "100%", "3", "0", "5 sec", "10 MB", etc.
        /// </summary>
        private static double? ExtractNumericValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var trimmed = value.Trim();

            // Direct parse for pure numbers (e.g. "2.14", "0", "3")
            if (double.TryParse(trimmed, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double directResult))
                return directResult;

            // Strip units then parse (e.g. "99.50%" -> 99.5, "5 sec" -> 5, "10 MB" -> 10)
            var cleaned = trimmed
                .Replace("%", "", StringComparison.OrdinalIgnoreCase)
                .Replace("percent", "", StringComparison.OrdinalIgnoreCase)
                .Replace("percentage", "", StringComparison.OrdinalIgnoreCase)
                .Replace("sec", "", StringComparison.OrdinalIgnoreCase)
                .Replace("seconds", "", StringComparison.OrdinalIgnoreCase)
                .Replace("s", "", StringComparison.OrdinalIgnoreCase)
                .Replace("mb", "", StringComparison.OrdinalIgnoreCase)
                .Replace("megabytes", "", StringComparison.OrdinalIgnoreCase)
                .Trim();

            if (double.TryParse(cleaned, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double result))
                return result;

            return null;
        }

        /// <summary>
        /// Calculates SLA status for Asset Control Panel: COMPLIANT, NON-COMPLIANT, or UNKNOWN.
        /// SLA rules:
        /// - Hit/miss (current = hit rate %): higher is better — current >= target % → COMPLIANT (KPI 1, 2, 3, 4, 5, 9–14, 19, 20, 21, 22, 24).
        /// - Numeric 6, 7, 8 (time/size): lower is better — current <= target → COMPLIANT.
        /// - Numeric 15, 16, 17, 18, 23 (scores/%): higher is better — current >= target → COMPLIANT.
        /// </summary>
        private static string CalculateSlaStatus(string currentValue, string target, int kpiId = 0)
        {
            if (currentValue == "N/A" || target == "N/A" || string.IsNullOrWhiteSpace(currentValue) || string.IsNullOrWhiteSpace(target))
                return "UNKNOWN";

            double? currentNum = ExtractNumericValue(currentValue);
            double? targetNum = ExtractNumericValue(target);

            if (!currentNum.HasValue || !targetNum.HasValue)
                return "UNKNOWN";

            // Target <= calculated → COMPLIANT (higher is better): KPI 1, 2 only (hit rate %). Target >= calculated → COMPLIANT (lower is better): 3–5, 9–14, 16, 17, 19–22, 24 (miss %). Numeric 15: higher better.
            var higherIsBetter = new[] { 1, 2, 15 }.Contains(kpiId);

            if (higherIsBetter)
            {
                // Hit rate % / uptime: 0% is never COMPLIANT (even if target is 0 from KpisLov)
                if (currentNum.Value == 0)
                    return "NON-COMPLIANT";
                return currentNum.Value >= targetNum.Value ? "COMPLIANT" : "NON-COMPLIANT";
            }

            // Lower is better: 0 vs 0 is COMPLIANT (e.g. zero incidents vs target 0); else current <= target
            if (currentNum.Value == 0 && targetNum.Value == 0)
                return "COMPLIANT";
            return currentNum.Value <= targetNum.Value ? "COMPLIANT" : "NON-COMPLIANT";
        }

        /// <summary>
        /// KPI 15 (WCAG compliance score): current value = AVG(Result) from KPIsResultHistories (last 30 days). Target &lt;= calculated → COMPLIANT.
        /// </summary>
        private KpiItemDto MapToKpiItemDtoFromHistoryAvgResult(KpisLov kpi, string? citizenImpactLevelName, List<KpiHistoryRowDto> allHistoryRows)
        {
            var rows = allHistoryRows.Where(r => r.KpiId == kpi.Id).ToList();
            var values = new List<double>();
            foreach (var r in rows)
            {
                var parsed = ExtractNumericValue(r.Result ?? "");
                if (parsed.HasValue) values.Add(parsed.Value);
            }
            string currentValue = values.Count > 0 ? $"{Math.Round(values.Average(), 2)}%" : "N/A";

            string target = GetTargetForKpi(kpi, citizenImpactLevelName);
            target = EnsureTargetHasUnit(kpi.Id, target);
            var slaStatus = (string.IsNullOrWhiteSpace(currentValue) || currentValue == "N/A")
                ? "UNKNOWN"
                : CalculateSlaStatus(currentValue, target, kpi.Id);

            DateTime lastCheckedAt = rows.Count > 0 ? rows.Max(r => r.CreatedAt) : default;
            var lastChecked = lastCheckedAt != default ? GetTimeAgo(lastCheckedAt) : "N/A";

            var dataSource = string.IsNullOrWhiteSpace(kpi.Manual) || kpi.Manual.Equals("Auto", StringComparison.OrdinalIgnoreCase)
                ? "Auto"
                : "Manual";

            return new KpiItemDto
            {
                KpiId = kpi.Id,
                KpiName = kpi.KpiName,
                Manual = kpi.Manual,
                Target = target,
                CurrentValue = currentValue,
                SlaStatus = slaStatus,
                LastChecked = lastChecked,
                DataSource = dataSource
            };
        }

        /// <summary>
        /// Builds KpiItemDto for hit/miss KPIs using KPIsResultHistories (last 30 days).
        /// Current value = (hits / total records) * 100; Target column used for hit/miss.
        /// </summary>
        private KpiItemDto MapToKpiItemDtoFromHistoryHitMiss(KpisLov kpi, string? citizenImpactLevelName, List<KpiHistoryRowDto> allHistoryRows)
        {
            // Exclude "skipped" from all counts: only hit and miss count. If all entries are skipped, total = 0 → N/A and UNKNOWN.
            var rows = allHistoryRows
                .Where(r => r.KpiId == kpi.Id)
                .Where(r => !string.Equals((r.Target ?? "").Trim(), "skipped", StringComparison.OrdinalIgnoreCase))
                .ToList();
            int total = rows.Count;
            int hits = 0;
            if (total > 0)
            {
                foreach (var r in rows)
                {
                    var tar = (r.Target ?? "").Trim().ToLowerInvariant();
                    if (tar == "hit" || tar == "pass") hits++;
                }
            }
            int misses = total - hits;

            // KPI 1, 2: hit/total % (target <= calculated → COMPLIANT). All others: misses/total % (target >= calculated → COMPLIANT).
            string currentValue;
            if (kpi.Id == 1 || kpi.Id == 2)
                currentValue = total > 0 ? $"{Math.Round(hits / (double)total * 100.0, 2)}%" : "N/A"; // Hit rate %
            else
                currentValue = total > 0 ? $"{Math.Round(misses / (double)total * 100.0, 2)}%" : "N/A"; // Miss %

            string target = GetTargetForKpi(kpi, citizenImpactLevelName);
            target = EnsureTargetHasUnit(kpi.Id, target);
            // UNKNOWN only when there is no data (current value N/A). When we have data and 0% (all misses), use normal SLA → NON-COMPLIANT.
            var slaStatus = total == 0 ? "UNKNOWN" : CalculateSlaStatus(currentValue, target, kpi.Id);

            DateTime lastCheckedAt = default;
            if (rows.Count > 0)
            {
                var maxCreated = rows.Max(r => r.CreatedAt);
                lastCheckedAt = maxCreated;
            }
            var lastChecked = lastCheckedAt != default ? GetTimeAgo(lastCheckedAt) : "N/A";

            var dataSource = string.IsNullOrWhiteSpace(kpi.Manual) || kpi.Manual.Equals("Auto", StringComparison.OrdinalIgnoreCase)
                ? "Auto"
                : "Manual";

            return new KpiItemDto
            {
                KpiId = kpi.Id,
                KpiName = kpi.KpiName,
                Manual = kpi.Manual,
                Target = target,
                CurrentValue = currentValue,
                SlaStatus = slaStatus,
                LastChecked = lastChecked,
                DataSource = dataSource
            };
        }

        private static string GetTargetForKpi(KpisLov kpi, string? citizenImpactLevelName)
        {
            if (!string.IsNullOrWhiteSpace(citizenImpactLevelName))
            {
                if (citizenImpactLevelName.Equals("HIGH - Critical Public Services", StringComparison.OrdinalIgnoreCase))
                    return kpi.TargetHigh ?? kpi.TargetMedium ?? kpi.TargetLow ?? "N/A";
                if (citizenImpactLevelName.Equals("MEDIUM - Important Services", StringComparison.OrdinalIgnoreCase))
                    return kpi.TargetMedium ?? kpi.TargetHigh ?? kpi.TargetLow ?? "N/A";
                if (citizenImpactLevelName.Equals("LOW - Supporting Services", StringComparison.OrdinalIgnoreCase))
                    return kpi.TargetLow ?? kpi.TargetMedium ?? kpi.TargetHigh ?? "N/A";
                return "N/A";
            }
            return kpi.TargetHigh ?? kpi.TargetMedium ?? kpi.TargetLow ?? "N/A";
        }

        private KpiItemDto MapToKpiItemDtoFromHistory(KpisLov kpi, string? citizenImpactLevelName = null, IEnumerable<dynamic>? allKpiResults = null)
        {
            // Helper function to get latest result when needed
            dynamic? GetLatestResult() => allKpiResults?.OrderByDescending(r => r.CreatedAt).FirstOrDefault();

            string target = GetTargetForKpi(kpi, citizenImpactLevelName);

            // Ensure target has unit suffix when stored as bare number (e.g. "3" -> "3 sec", "95" -> "95%")
            target = EnsureTargetHasUnit(kpi.Id, target);
            
            // Current value: unit taken from target so it matches (e.g. target "5 sec" -> current "3 sec", target "10 MB" -> current "3 MB")
            string currentValue = GetCurrentValueForControlPanel(kpi.Id, allKpiResults, target);
            // Same as hit/miss: UNKNOWN only when current value is N/A (no data); otherwise compute SLA from actual value
            var slaStatus = (string.IsNullOrWhiteSpace(currentValue) || currentValue == "N/A")
                ? "UNKNOWN"
                : CalculateSlaStatus(currentValue, target, kpi.Id);

            // Last checked: UpdatedAt from KpiResult, fallback to CreatedAt
            var latestResult = GetLatestResult();
            DateTime lastCheckedAt = default;
            if (latestResult != null)
            {
                try { if (latestResult.UpdatedAt != null) lastCheckedAt = (DateTime)latestResult.UpdatedAt; } catch { }
                if (lastCheckedAt == default && latestResult.CreatedAt != null)
                    lastCheckedAt = (DateTime)latestResult.CreatedAt;
            }
            var lastChecked = lastCheckedAt != default ? GetTimeAgo(lastCheckedAt) : "N/A";

            // Data source: Auto or Manual per KpisLov
            var dataSource = string.IsNullOrWhiteSpace(kpi.Manual) || kpi.Manual.Equals("Auto", StringComparison.OrdinalIgnoreCase)
                ? "Auto"
                : "Manual";

            return new KpiItemDto
            {
                KpiId = kpi.Id,
                KpiName = kpi.KpiName,
                Manual = kpi.Manual,
                Target = target,
                CurrentValue = currentValue,
                SlaStatus = slaStatus,
                LastChecked = lastChecked,
                DataSource = dataSource
            };
        }

        private sealed class KpiHistoryRowDto
        {
            public int KpiId { get; set; }
            public string Result { get; set; } = "";
            public string Target { get; set; } = "";
            public DateTime CreatedAt { get; set; }
        }
    }
}
