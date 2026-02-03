using System.Text.Json;
using System.Text.RegularExpressions;
using DAMS.Application.Interfaces;
using DAMS.Application.Models;
using DAMS.Application.DTOs;
using DAMS.Domain.Entities;
using DAMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DAMS.Infrastructure.Services
{
    public class KpisLovService : IKpisLovService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        public KpisLovService(ApplicationDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<APIResponse> GetKpisLovsForDropdownAsync()
        {
            var kpis = await _context.KpisLovs
                .Where(k => k.DeletedAt == null)
                .OrderBy(k => k.KpiName)
                .Select(k => new
                {
                    Id = k.Id,
                    Name = k.KpiName
                })
                .ToListAsync();

            return new APIResponse
            {
                IsSuccessful = true,
                Message = "KPIs retrieved successfully for dropdown",
                Data = kpis
            };
        }

        public async Task<APIResponse> GetKpisLovByIdAsync(int id)
        {
            var kpi = await _context.KpisLovs
                .FirstOrDefaultAsync(k => k.Id == id && k.DeletedAt == null);

            if (kpi == null)
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "KPI not found.",
                    Data = null
                };
            }

            var kpiDto = MapToKpisLovDto(kpi);
            return new APIResponse
            {
                IsSuccessful = true,
                Message = "KPI retrieved successfully",
                Data = kpiDto
            };
        }

        public async Task<APIResponse> GetAllKpisLovsAsync(KpisLovFilterDto filter)
        {
            var query = _context.KpisLovs
                .Where(k => k.DeletedAt == null)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                query = query.Where(k =>
                    k.KpiName.Contains(filter.SearchTerm) ||
                    k.KpiGroup.Contains(filter.SearchTerm));
            }

            if (!string.IsNullOrEmpty(filter.KpiGroup))
            {
                query = query.Where(k => k.KpiGroup == filter.KpiGroup);
            }

            // Apply sorting
            if (!string.IsNullOrEmpty(filter.SortBy))
            {
                query = filter.SortBy.ToLower() switch
                {
                    "id" => filter.SortDescending ? query.OrderByDescending(k => k.Id) : query.OrderBy(k => k.Id),
                    "kpiname" => filter.SortDescending ? query.OrderByDescending(k => k.KpiName) : query.OrderBy(k => k.KpiName),
                    "kpigroup" => filter.SortDescending ? query.OrderByDescending(k => k.KpiGroup) : query.OrderBy(k => k.KpiGroup),
                    "severityid" => filter.SortDescending ? query.OrderByDescending(k => k.SeverityId) : query.OrderBy(k => k.SeverityId),
                    "weight" => filter.SortDescending ? query.OrderByDescending(k => k.Weight) : query.OrderBy(k => k.Weight),
                    "createdat" => filter.SortDescending ? query.OrderByDescending(k => k.CreatedAt) : query.OrderBy(k => k.CreatedAt),
                    "updatedat" => filter.SortDescending ? query.OrderByDescending(k => k.UpdatedAt ?? k.CreatedAt) : query.OrderBy(k => k.UpdatedAt ?? k.CreatedAt),
                    _ => query.OrderByDescending(k => k.UpdatedAt ?? k.CreatedAt)
                };
            }
            else
            {
                query = query.OrderBy(k => k.KpiName);
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply pagination
            var kpis = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var kpiDtos = kpis.Select(MapToKpisLovDto).ToList();

            var pagedResponse = new PagedResponse<KpisLovDto>
            {
                Data = kpiDtos,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };

            return new APIResponse
            {
                IsSuccessful = true,
                Message = "KPIs retrieved successfully",
                Data = pagedResponse
            };
        }

        public async Task<APIResponse> CreateKpisLovAsync(CreateKpisLovDto dto, string createdBy)
        {
            // Validate mandatory fields
            if (string.IsNullOrWhiteSpace(dto.KpiName))
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "KPI Name is required.",
                    Data = null
                };
            }

            // Validate Severity Level exists
            var severityLevel = await _context.CommonLookups.FindAsync(dto.SeverityId);
            if (severityLevel == null || severityLevel.Type != "SeverityLevel")
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "Severity Level not found.",
                    Data = null
                };
            }

            var kpi = new KpisLov
            {
                KpiName = dto.KpiName.Trim(),
                KpiGroup = dto.KpiGroup ?? string.Empty,
                Manual = dto.Manual ?? string.Empty,
                Frequency = dto.Frequency ?? string.Empty,
                Outcome = dto.Outcome ?? string.Empty,
                PagesToCheck = dto.PagesToCheck ?? string.Empty,
                TargetType = dto.TargetType ?? string.Empty,
                TargetHigh = dto.TargetHigh ?? string.Empty,
                TargetMedium = dto.TargetMedium ?? string.Empty,
                TargetLow = dto.TargetLow ?? string.Empty,
                KpiType = dto.KpiType ?? string.Empty,
                SeverityId = dto.SeverityId,
                Weight = dto.Weight ?? 0,
                CreatedBy = createdBy,
                CreatedAt = DateTime.Now
            };

            _context.KpisLovs.Add(kpi);
            await _context.SaveChangesAsync();

            var kpiDto = MapToKpisLovDto(kpi);
            return new APIResponse
            {
                IsSuccessful = true,
                Message = "KPI created successfully",
                Data = kpiDto
            };
        }

        public async Task<APIResponse> UpdateKpisLovAsync(int id, UpdateKpisLovDto dto, string updatedBy)
        {
            var kpi = await _context.KpisLovs
                .FirstOrDefaultAsync(k => k.Id == id && k.DeletedAt == null);

            if (kpi == null)
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "KPI not found.",
                    Data = null
                };
            }

            if (!string.IsNullOrWhiteSpace(dto.KpiName))
                kpi.KpiName = dto.KpiName;

            if (dto.KpiGroup != null)
                kpi.KpiGroup = dto.KpiGroup;

            if (dto.Manual != null)
                kpi.Manual = dto.Manual;

            if (dto.Frequency != null)
                kpi.Frequency = dto.Frequency;

            if (dto.Outcome != null)
                kpi.Outcome = dto.Outcome;

            if (dto.PagesToCheck != null)
                kpi.PagesToCheck = dto.PagesToCheck;

            if (dto.TargetType != null)
                kpi.TargetType = dto.TargetType;

            if (dto.TargetHigh != null)
                kpi.TargetHigh = dto.TargetHigh;

            if (dto.TargetMedium != null)
                kpi.TargetMedium = dto.TargetMedium;

            if (dto.TargetLow != null)
                kpi.TargetLow = dto.TargetLow;

            if (dto.KpiType != null)
                kpi.KpiType = dto.KpiType;

            if (dto.SeverityId.HasValue)
            {
                var severityLevel = await _context.CommonLookups.FindAsync(dto.SeverityId.Value);
                if (severityLevel == null || severityLevel.Type != "SeverityLevel")
                {
                    return new APIResponse
                    {
                        IsSuccessful = false,
                        Message = "Severity Level not found.",
                        Data = null
                    };
                }
                kpi.SeverityId = dto.SeverityId.Value;
            }

            if (dto.Weight.HasValue)
                kpi.Weight = dto.Weight.Value;

            kpi.UpdatedAt = DateTime.Now;
            kpi.UpdatedBy = updatedBy;

            await _context.SaveChangesAsync();

            var kpiDto = MapToKpisLovDto(kpi);
            return new APIResponse
            {
                IsSuccessful = true,
                Message = "KPI updated successfully",
                Data = kpiDto
            };
        }

        public async Task<APIResponse> DeleteKpisLovAsync(int id, string deletedBy)
        {
            var kpi = await _context.KpisLovs
                .FirstOrDefaultAsync(k => k.Id == id && k.DeletedAt == null);

            if (kpi == null)
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "KPI not found.",
                    Data = null
                };
            }

            kpi.DeletedAt = DateTime.Now;
            kpi.DeletedBy = deletedBy;
            kpi.UpdatedAt = DateTime.Now;
            kpi.UpdatedBy = deletedBy;

            await _context.SaveChangesAsync();

            return new APIResponse
            {
                IsSuccessful = true,
                Message = "KPI deleted successfully",
                Data = null
            };
        }

        public async Task<APIResponse> GetKpisLovManualDataFromAssetUrlAsync(int assetId, int? kpiId = null)
        {
            var asset = await _context.Assets
                .AsNoTracking()
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

            if (string.IsNullOrWhiteSpace(asset.AssetUrl))
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "Asset has no URL configured.",
                    Data = null
                };
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(30);
                var response = await client.GetAsync(asset.AssetUrl);

                if (!response.IsSuccessStatusCode)
                {
                    return new APIResponse
                    {
                        IsSuccessful = false,
                        Message = $"Request to asset URL failed with status {response.StatusCode}.",
                        Data = new KpisLovManualDataResponseDto
                        {
                            AssetId = asset.Id,
                            AssetName = asset.AssetName,
                            AssetUrl = asset.AssetUrl,
                            ManualData = null,
                            ContentType = response.Content.Headers.ContentType?.ToString()
                        }
                    };
                }

                var manualData = await response.Content.ReadAsStringAsync();
                var contentType = response.Content.Headers.ContentType?.ToString();

                KpisLovManualAnalyticsDto? analytics = null;
                if (!string.IsNullOrWhiteSpace(manualData) &&
                    (contentType?.Contains("json", StringComparison.OrdinalIgnoreCase) == true ||
                     manualData.TrimStart().StartsWith("{")))
                {
                    try
                    {
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        };
                        analytics = JsonSerializer.Deserialize<KpisLovManualAnalyticsDto>(manualData, options);
                    }
                    catch
                    {
                        // Keep Analytics null; try HTML parsing below
                    }
                }

                // When response is HTML (e.g. ministry site), try to extract counter/visitor stats
                if (analytics == null && !string.IsNullOrWhiteSpace(manualData) &&
                    (contentType?.Contains("html", StringComparison.OrdinalIgnoreCase) == true ||
                     manualData.TrimStart().StartsWith("<!", StringComparison.OrdinalIgnoreCase)))
                {
                    analytics = TryParseAnalyticsFromHtml(manualData);
                }

                if (analytics != null)
                    NormalizeAnalytics(analytics);

                var result = new KpisLovManualDataResponseDto
                {
                    AssetId = asset.Id,
                    AssetName = asset.AssetName,
                    AssetUrl = asset.AssetUrl,
                    ManualData = null, // Do not send raw HTML/JSON to front
                    ContentType = contentType,
                    Analytics = analytics
                };

                // If kpiId is provided, save or update KPIsResult entry
                if (kpiId.HasValue)
                {
                    var kpi = await _context.KpisLovs
                        .AsNoTracking()
                        .FirstOrDefaultAsync(k => k.Id == kpiId.Value && k.DeletedAt == null);
                    if (kpi != null)
                    {
                        var (resultValue, hitOrMiss, metricName) = GetResultValueAndTarget(analytics, kpi);
                        var detailsJson = analytics != null
                            ? JsonSerializer.Serialize(analytics, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
                            : "{}";

                        // Check if record already exists for this AssetId + KpiId
                        var existingResult = await _context.KPIsResults
                            .FirstOrDefaultAsync(r => r.AssetId == assetId && r.KpiId == kpiId.Value);

                        if (existingResult != null)
                        {
                            // Update existing record
                            existingResult.Result = $"{resultValue}";
                            existingResult.Details = detailsJson;
                            existingResult.Target = hitOrMiss;
                            existingResult.UpdatedAt = DateTime.Now;
                        }
                        else
                        {
                            // Create new record
                            var kpiResult = new KPIsResult
                            {
                                AssetId = assetId,
                                KpiId = kpiId.Value,
                                Result = $"{resultValue}",
                                Details = detailsJson,
                                Target = hitOrMiss,
                                CreatedAt = DateTime.Now
                            };
                            _context.KPIsResults.Add(kpiResult);
                        }
                        await _context.SaveChangesAsync();
                    }
                }

                return new APIResponse
                {
                    IsSuccessful = true,
                    Message = kpiId.HasValue 
                        ? "KPI Lov manual data retrieved and KPIsResult entry saved successfully." 
                        : "KPI Lov manual data retrieved successfully from asset URL.",
                    Data = result
                };
            }
            catch (TaskCanceledException)

            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "Request to asset URL timed out.",
                    Data = null
                };
            }
            catch (HttpRequestException ex)
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = $"Request to asset URL failed: {ex.Message}",
                    Data = null
                };
            }
            catch (Exception ex)
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = $"An error occurred while fetching manual data: {ex.Message}",
                    Data = null
                };
            }
        }

        /// <summary>
        /// Maps the KPI (by KpiName) to the corresponding analytics metric and returns a single int result value.
        /// Target is "Hit" when result meets the KPI target (TargetHigh), else "Miss".
        /// Returns (resultValue, target, metricName) for meaningful Result description.
        /// </summary>
        private static (int resultValue, string target, string metricName) GetResultValueAndTarget(KpisLovManualAnalyticsDto? analytics, KpisLov kpi)
        {
            int value = 0;
            string metricName = "Manual Entry";
            bool lowerIsBetter = false; // Bounce rate: lower is better

            if (analytics != null)
            {
                var name = kpi.KpiName.Trim();
                if (name.Contains("Total visit", StringComparison.OrdinalIgnoreCase))
                {
                    value = (int)(analytics.TotalVisits ?? 0);
                    metricName = "Total Visits";
                }
                else if (name.Contains("Unique visitor", StringComparison.OrdinalIgnoreCase))
                {
                    value = (int)(analytics.UniqueVisitors ?? 0);
                    metricName = "Unique Visitors";
                }
                else if (name.Contains("Page view", StringComparison.OrdinalIgnoreCase))
                {
                    value = (int)(analytics.PageViews ?? 0);
                    metricName = "Page Views";
                }
                else if (name.Contains("Top accessed", StringComparison.OrdinalIgnoreCase))
                {
                    value = analytics.TopAccessedPages?.Count ?? 0;
                    metricName = "Top Accessed Pages";
                }
                else if (name.Contains("Entry page", StringComparison.OrdinalIgnoreCase))
                {
                    value = analytics.EntryPageDistribution?.Count ?? 0;
                    metricName = "Entry Page Distribution";
                }
                else if (name.Contains("Exit page", StringComparison.OrdinalIgnoreCase))
                {
                    value = analytics.ExitPageDistribution?.Count ?? 0;
                    metricName = "Exit Page Distribution";
                }
                else if (name.Contains("Average session", StringComparison.OrdinalIgnoreCase) || name.Contains("session duration", StringComparison.OrdinalIgnoreCase))
                {
                    value = (int)(analytics.AverageSessionDurationSeconds ?? 0);
                    metricName = "Average Session Duration (seconds)";
                }
                else if (name.Contains("Bounce rate", StringComparison.OrdinalIgnoreCase))
                {
                    value = (int)(analytics.BounceRate ?? 0);
                    metricName = "Bounce Rate (%)";
                    lowerIsBetter = true;
                }
                else if (name.Contains("Peak usage", StringComparison.OrdinalIgnoreCase))
                {
                    value = analytics.PeakUsageWindows?.Count ?? 0;
                    metricName = "Peak Usage Windows";
                }
            }

            double targetThreshold;
            var parsed = double.TryParse(kpi.TargetHigh?.Trim(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out targetThreshold);

            string hitOrMiss = "Miss";
            if (parsed)
            {
                if (lowerIsBetter)
                    hitOrMiss = value <= targetThreshold ? "Hit" : "Miss";
                else
                    hitOrMiss = value >= targetThreshold ? "Hit" : "Miss";
            }
            else
                hitOrMiss = value > 0 ? "Hit" : "Miss"; // No target defined: Hit if we have data

            return (value, hitOrMiss, metricName);
        }

        /// <summary>
        /// Tries to extract analytics (e.g. Total Visitors, Today Visitors, Active Users) from HTML
        /// that contains counter boxes with class "purecounterr".
        /// </summary>
        private static KpisLovManualAnalyticsDto? TryParseAnalyticsFromHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return null;

            // Match counter values: class="purecounterr">465,918</span> (order: Total Visitors, Today Visitors, Active Users)
            var valueMatches = Regex.Matches(html, @"purecounterr""?\s*>\s*([^<]+)\s*<", RegexOptions.IgnoreCase);
            if (valueMatches.Count == 0) return null;

            static long? ParseCounterValue(string raw)
            {
                if (string.IsNullOrWhiteSpace(raw)) return null;
                var digits = Regex.Replace(raw.Trim(), @"[^\d]", "");
                return long.TryParse(digits, out var n) ? n : null;
            }

            var values = valueMatches.Select(m => ParseCounterValue(m.Groups[1].Value)).ToList();
            if (values.All(v => v == null)) return null;

            return new KpisLovManualAnalyticsDto
            {
                TotalVisits = values.Count > 0 ? values[0] : null,
                TodayVisits = values.Count > 1 ? values[1] : null,
                ActiveUsers = values.Count > 2 ? values[2] : null,
                UniqueVisitors = values.Count > 2 ? values[2] : null,
                TopAccessedPages = new List<PageAccessDto>(),
                EntryPageDistribution = new List<PageDistributionItemDto>(),
                ExitPageDistribution = new List<PageDistributionItemDto>(),
                PeakUsageWindows = new List<UsageWindowDto>()
            };
        }

        /// <summary>
        /// Ensures analytics list properties are never null (empty array) and numeric nulls become 0 for a consistent API response.
        /// </summary>
        private static void NormalizeAnalytics(KpisLovManualAnalyticsDto analytics)
        {
            analytics.TopAccessedPages ??= new List<PageAccessDto>();
            analytics.EntryPageDistribution ??= new List<PageDistributionItemDto>();
            analytics.ExitPageDistribution ??= new List<PageDistributionItemDto>();
            analytics.PeakUsageWindows ??= new List<UsageWindowDto>();
            // Use 0 for numeric fields when not available so the response has no nulls
            if (!analytics.PageViews.HasValue) analytics.PageViews = 0;
            if (!analytics.AverageSessionDurationSeconds.HasValue) analytics.AverageSessionDurationSeconds = 0;
            if (!analytics.BounceRate.HasValue) analytics.BounceRate = 0;
        }

        private static KpisLovDto MapToKpisLovDto(KpisLov kpi)
        {
            return new KpisLovDto
            {
                Id = kpi.Id,
                KpiName = kpi.KpiName,
                KpiGroup = kpi.KpiGroup,
                Manual = kpi.Manual,
                Frequency = kpi.Frequency,
                Outcome = kpi.Outcome,
                PagesToCheck = kpi.PagesToCheck,
                TargetType = kpi.TargetType,
                TargetHigh = kpi.TargetHigh,
                TargetMedium = kpi.TargetMedium,
                TargetLow = kpi.TargetLow,
                KpiType = kpi.KpiType,
                SeverityId = kpi.SeverityId,
                Weight = kpi.Weight
            };
        }
    }
}
