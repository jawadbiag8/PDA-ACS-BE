using DAMS.Application.Interfaces;
using DAMS.Application.Models;
using DAMS.Application.DTOs;
using DAMS.Domain.Entities;
using DAMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DAMS.Infrastructure.Services
{
    public class IncidentService : IIncidentService
    {
        private readonly ApplicationDbContext _context;

        public IncidentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<APIResponse> GetIncidentByIdAsync(int id)
        {
            var incident = await _context.Incidents
                .Include(i => i.Asset)
                .Include(i => i.KpisLov)
                .Include(i => i.Severity)
                .Include(i => i.Status)
                .FirstOrDefaultAsync(i => i.Id == id && i.DeletedAt == null);

            if (incident == null)
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "Incident not found.",
                    Data = null
                };
            }

            var incidentDto = MapToIncidentDto(incident);
            return new APIResponse
            {
                IsSuccessful = true,
                Message = "Incident retrieved successfully",
                Data = incidentDto
            };
        }

        public async Task<APIResponse> GetAllIncidentsAsync(IncidentFilterDto filter)
        {
            var query = _context.Incidents
                .Include(i => i.Asset)
                    .ThenInclude(a => a.Ministry)
                .Include(i => i.KpisLov)
                .Include(i => i.Severity)
                .Include(i => i.Status)
                .AsQueryable();

            // Apply status filter: StatusId = 14 means "all except closed (12)" i.e. StatusId != 12
            if (filter.StatusId != null && filter.StatusId > 0)
            {
                if (filter.StatusId == 14)
                    query = query.Where(i => i.StatusId != 12);
                else
                    query = query.Where(i => i.StatusId == filter.StatusId);
            }

            // Always exclude deleted incidents
            query = query.Where(i => i.DeletedAt == null);

            // Apply filters
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                query = query.Where(i => 
                    i.IncidentTitle.Contains(filter.SearchTerm) || 
                    i.Description.Contains(filter.SearchTerm) ||
                    (i.Severity != null && i.Severity.Name.Contains(filter.SearchTerm)) ||
                    (i.Asset != null && i.Asset.AssetName.Contains(filter.SearchTerm)) ||
                    (i.Asset != null && i.Asset.Ministry != null && i.Asset.Ministry.MinistryName.Contains(filter.SearchTerm)) ||
                    (i.KpisLov != null && i.KpisLov.KpiName.Contains(filter.SearchTerm)));
            }

            if (filter.MinistryId.HasValue)
            {
                query = query.Where(i => i.Asset != null && i.Asset.MinistryId == filter.MinistryId.Value);
            }

            if (filter.AssetId.HasValue)
            {
                query = query.Where(i => i.AssetId == filter.AssetId.Value);
            }

            if (filter.KpiId.HasValue)
            {
                query = query.Where(i => i.KpiId == filter.KpiId.Value);
            }

            if (filter.SeverityId.HasValue)
            {
                query = query.Where(i => i.SeverityId == filter.SeverityId.Value);
            }

            if (!string.IsNullOrEmpty(filter.CreatedBy))
            {
                query = query.Where(i => i.CreatedBy == filter.CreatedBy);
            }

            if (!string.IsNullOrEmpty(filter.AssignedTo))
            {
                query = query.Where(i => i.AssignedTo == filter.AssignedTo);
            }

            if (filter.CreatedFrom.HasValue)
            {
                query = query.Where(i => i.CreatedAt >= filter.CreatedFrom.Value);
            }

            if (filter.CreatedTo.HasValue)
            {
                query = query.Where(i => i.CreatedAt <= filter.CreatedTo.Value);
            }

            // Apply sorting
            if (!string.IsNullOrEmpty(filter.SortBy))
            {
                query = filter.SortBy.ToLower() switch
                {
                    "incidenttitle" => filter.SortDescending ? query.OrderByDescending(i => i.IncidentTitle) : query.OrderBy(i => i.IncidentTitle),
                    "assetid" => filter.SortDescending ? query.OrderByDescending(i => i.AssetId) : query.OrderBy(i => i.AssetId),
                    "kpiid" => filter.SortDescending ? query.OrderByDescending(i => i.KpiId) : query.OrderBy(i => i.KpiId),
                    "severityid" => filter.SortDescending ? query.OrderByDescending(i => i.SeverityId) : query.OrderBy(i => i.SeverityId),
                    "statusid" => filter.SortDescending ? query.OrderByDescending(i => i.StatusId) : query.OrderBy(i => i.StatusId),
                    "status" => filter.SortDescending ? query.OrderByDescending(i => i.StatusId) : query.OrderBy(i => i.StatusId), // Backward compatibility
                    "createdby" => filter.SortDescending ? query.OrderByDescending(i => i.CreatedBy) : query.OrderBy(i => i.CreatedBy),
                    "assignedto" => filter.SortDescending ? query.OrderByDescending(i => i.AssignedTo) : query.OrderBy(i => i.AssignedTo),
                    "createdat" => filter.SortDescending ? query.OrderByDescending(i => i.CreatedAt) : query.OrderBy(i => i.CreatedAt),
                    "updatedat" => filter.SortDescending ? query.OrderByDescending(i => i.UpdatedAt ?? i.CreatedAt) : query.OrderBy(i => i.UpdatedAt ?? i.CreatedAt),
                    _ => query.OrderByDescending(i => i.UpdatedAt ?? i.CreatedAt)
                };
            }
            else
            {
                query = query.OrderByDescending(i => i.UpdatedAt ?? i.CreatedAt);
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply pagination
            var incidents = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var dashboardDtos = incidents.Select(MapToIncidentDashboardDto).ToList();

            var pagedResponse = new PagedResponse<IncidentDashboardDto>
            {
                Data = dashboardDtos,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };

            return new APIResponse
            {
                IsSuccessful = true,
                Message = "Incidents retrieved successfully",
                Data = pagedResponse
            };
        }

        public async Task<APIResponse> GetIncidentsByAssetIdAsync(int assetId, IncidentFilterDto? filter = null)
        {
            // Use default filter if not provided
            filter ??= new IncidentFilterDto { PageNumber = 1, PageSize = 10 };

            var query = _context.Incidents
                .Include(i => i.Asset)
                    .ThenInclude(a => a.Ministry)
                .Include(i => i.KpisLov)
                .Include(i => i.Severity)
                .Include(i => i.Status)
                .AsQueryable();

            // Always filter by assetId
            query = query.Where(i => i.AssetId == assetId);

            // Apply status filter: StatusId = 14 means "all except closed (12)" i.e. StatusId != 12
            if (filter.StatusId != null && filter.StatusId > 0)
            {
                if (filter.StatusId == 14)
                    query = query.Where(i => i.StatusId != 12);
                else
                    query = query.Where(i => i.StatusId == filter.StatusId);
            }

            // Always exclude deleted incidents
            query = query.Where(i => i.DeletedAt == null);

            // Apply filters
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                query = query.Where(i => 
                    i.IncidentTitle.Contains(filter.SearchTerm) || 
                    i.Description.Contains(filter.SearchTerm) ||
                    (i.Severity != null && i.Severity.Name.Contains(filter.SearchTerm)) ||
                    (i.Asset != null && i.Asset.AssetName.Contains(filter.SearchTerm)) ||
                    (i.Asset != null && i.Asset.Ministry != null && i.Asset.Ministry.MinistryName.Contains(filter.SearchTerm)) ||
                    (i.KpisLov != null && i.KpisLov.KpiName.Contains(filter.SearchTerm)));
            }

            if (filter.MinistryId.HasValue)
            {
                query = query.Where(i => i.Asset != null && i.Asset.MinistryId == filter.MinistryId.Value);
            }

            // Note: AssetId filter is already applied above, but we can ignore filter.AssetId if provided
            // since we're already filtering by the required assetId parameter

            if (filter.KpiId.HasValue)
            {
                query = query.Where(i => i.KpiId == filter.KpiId.Value);
            }

            if (filter.SeverityId.HasValue)
            {
                query = query.Where(i => i.SeverityId == filter.SeverityId.Value);
            }

            if (!string.IsNullOrEmpty(filter.CreatedBy))
            {
                query = query.Where(i => i.CreatedBy == filter.CreatedBy);
            }

            if (!string.IsNullOrEmpty(filter.AssignedTo))
            {
                query = query.Where(i => i.AssignedTo == filter.AssignedTo);
            }

            if (filter.CreatedFrom.HasValue)
            {
                query = query.Where(i => i.CreatedAt >= filter.CreatedFrom.Value);
            }

            if (filter.CreatedTo.HasValue)
            {
                query = query.Where(i => i.CreatedAt <= filter.CreatedTo.Value);
            }

            // Apply sorting
            if (!string.IsNullOrEmpty(filter.SortBy))
            {
                query = filter.SortBy.ToLower() switch
                {
                    "incidenttitle" => filter.SortDescending ? query.OrderByDescending(i => i.IncidentTitle) : query.OrderBy(i => i.IncidentTitle),
                    "assetid" => filter.SortDescending ? query.OrderByDescending(i => i.AssetId) : query.OrderBy(i => i.AssetId),
                    "kpiid" => filter.SortDescending ? query.OrderByDescending(i => i.KpiId) : query.OrderBy(i => i.KpiId),
                    "severityid" => filter.SortDescending ? query.OrderByDescending(i => i.SeverityId) : query.OrderBy(i => i.SeverityId),
                    "statusid" => filter.SortDescending ? query.OrderByDescending(i => i.StatusId) : query.OrderBy(i => i.StatusId),
                    "status" => filter.SortDescending ? query.OrderByDescending(i => i.StatusId) : query.OrderBy(i => i.StatusId), // Backward compatibility
                    "createdby" => filter.SortDescending ? query.OrderByDescending(i => i.CreatedBy) : query.OrderBy(i => i.CreatedBy),
                    "assignedto" => filter.SortDescending ? query.OrderByDescending(i => i.AssignedTo) : query.OrderBy(i => i.AssignedTo),
                    "createdat" => filter.SortDescending ? query.OrderByDescending(i => i.CreatedAt) : query.OrderBy(i => i.CreatedAt),
                    "updatedat" => filter.SortDescending ? query.OrderByDescending(i => i.UpdatedAt ?? i.CreatedAt) : query.OrderBy(i => i.UpdatedAt ?? i.CreatedAt),
                    _ => query.OrderByDescending(i => i.UpdatedAt ?? i.CreatedAt)
                };
            }
            else
            {
                query = query.OrderByDescending(i => i.UpdatedAt ?? i.CreatedAt);
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply pagination
            var incidents = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var dashboardDtos = incidents.Select(MapToIncidentDashboardDto).ToList();

            var pagedResponse = new PagedResponse<IncidentDashboardDto>
            {
                Data = dashboardDtos,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };

            return new APIResponse
            {
                IsSuccessful = true,
                Message = "Incidents retrieved successfully",
                Data = pagedResponse
            };
        }

        public async Task<APIResponse> CreateIncidentAsync(CreateIncidentDto dto, string createdBy)
        {
            // Validate mandatory fields
            if (string.IsNullOrWhiteSpace(dto.IncidentTitle))
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "Incident Title is required.",
                    Data = null
                };
            }

            var asset = await _context.Assets.FindAsync(dto.AssetId);
            if (asset == null || asset.DeletedAt != null)
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "Asset not found.",
                    Data = null
                };
            }

            var kpi = await _context.KpisLovs.FindAsync(dto.KpiId);
            if (kpi == null)
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "KPI not found.",
                    Data = null
                };
            }

            var severity = await _context.CommonLookups.FindAsync(dto.SeverityId);
            if (severity == null)
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "Severity not found.",
                    Data = null
                };
            }

            // Check if incident already exists for this asset and KPI combination (title comparison in memory for EF translation)
            var trimmedTitle = dto.IncidentTitle?.Trim() ?? string.Empty;
            var titleLower = trimmedTitle.ToLowerInvariant();
            var candidates = await _context.Incidents
                .Where(i => i.AssetId == dto.AssetId && i.KpiId == dto.KpiId && i.DeletedAt == null)
                .Select(i => new { i.Id, i.IncidentTitle })
                .ToListAsync();
            var existingIncident = candidates.FirstOrDefault(c => (c.IncidentTitle ?? string.Empty).Trim().ToLowerInvariant() == titleLower);
            
            if (existingIncident != null)
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = $"An incident with title '{trimmedTitle}' already exists for this asset and KPI combination.",
                    Data = null
                };
            }

            var incident = new Incident
            {
                AssetId = dto.AssetId,
                KpiId = dto.KpiId,
                IncidentTitle = dto.IncidentTitle.Trim(),
                Description = dto.Description ?? string.Empty,
                Type = dto.Type ?? string.Empty,
                SeverityId = dto.SeverityId,
                StatusId = dto.StatusId > 0 ? dto.StatusId : await GetDefaultStatusIdAsync(),
                AssignedTo = createdBy, // Set AssignedTo same as CreatedBy
                CreatedBy = createdBy,
                CreatedAt = DateTime.Now
            };

            _context.Incidents.Add(incident);
            await _context.SaveChangesAsync();

            var incidentDto = MapToIncidentDto(incident);
            return new APIResponse
            {
                IsSuccessful = true,
                Message = "Incident created successfully",
                Data = incidentDto
            };
        }

        public async Task<APIResponse> UpdateIncidentAsync(int id, UpdateIncidentDto dto, string updatedBy)
        {
            var incident = await _context.Incidents
                .FirstOrDefaultAsync(i => i.Id == id && i.DeletedAt == null);

            if (incident == null)
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "Incident not found.",
                    Data = null
                };
            }

            if (dto.KpiId.HasValue)
            {
                var kpi = await _context.KpisLovs.FindAsync(dto.KpiId.Value);
                if (kpi == null)
                {
                    return new APIResponse
                    {
                        IsSuccessful = false,
                        Message = "KPI not found.",
                        Data = null
                    };
                }
                incident.KpiId = dto.KpiId.Value;
            }

            if (!string.IsNullOrEmpty(dto.IncidentTitle))
            {
                var trimmedTitle = dto.IncidentTitle.Trim();
                var titleLower = trimmedTitle.ToLowerInvariant();
                var kpiId = dto.KpiId ?? incident.KpiId;
                
                // Check if another incident with same AssetId + KpiId + Title already exists (title comparison in memory for EF translation)
                var candidates = await _context.Incidents
                    .Where(i => i.AssetId == incident.AssetId && i.KpiId == kpiId && i.Id != id && i.DeletedAt == null)
                    .Select(i => new { i.Id, i.IncidentTitle })
                    .ToListAsync();
                var existingIncident = candidates.FirstOrDefault(c => (c.IncidentTitle ?? string.Empty).Trim().ToLowerInvariant() == titleLower);
                
                if (existingIncident != null)
                {
                    return new APIResponse
                    {
                        IsSuccessful = false,
                        Message = $"An incident with title '{trimmedTitle}' already exists for this asset and KPI combination.",
                        Data = null
                    };
                }
                
                incident.IncidentTitle = trimmedTitle;
            }

            if (dto.Description != null)
                incident.Description = dto.Description;

            if (dto.Type != null)
                incident.Type = dto.Type;

            if (dto.SeverityId.HasValue)
            {
                var severity = await _context.CommonLookups.FindAsync(dto.SeverityId.Value);
                if (severity == null)
                {
                    return new APIResponse
                    {
                        IsSuccessful = false,
                        Message = "Severity not found.",
                        Data = null
                    };
                }
                incident.SeverityId = dto.SeverityId.Value;
            }

            if (dto.AssignedTo != null)
                incident.AssignedTo = dto.AssignedTo;

            // Update Status if status is being changed
            if (dto.StatusId.HasValue && dto.StatusId.Value > 0)
            {
                incident.StatusId = dto.StatusId.Value;
            }

            incident.UpdatedAt = DateTime.Now;
            incident.UpdatedBy = updatedBy;

            await _context.SaveChangesAsync();

            var incidentDto = MapToIncidentDto(incident);
            return new APIResponse
            {
                IsSuccessful = true,
                Message = "Incident updated successfully",
                Data = incidentDto
            };
        }

        public async Task<APIResponse> DeleteIncidentAsync(int id, string deletedBy)
        {
            var incident = await _context.Incidents
                .FirstOrDefaultAsync(i => i.Id == id && i.DeletedAt == null);

            if (incident == null)
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "Incident not found.",
                    Data = null
                };
            }

            incident.DeletedAt = DateTime.Now;
            incident.DeletedBy = deletedBy;
            incident.UpdatedAt = DateTime.Now;
            incident.UpdatedBy = deletedBy;

            await _context.SaveChangesAsync();

            return new APIResponse
            {
                IsSuccessful = true,
                Message = "Incident deleted successfully",
                Data = null
            };
        }

        private static IncidentDto MapToIncidentDto(Incident incident)
        {
            return new IncidentDto
            {
                Id = incident.Id,
                AssetId = incident.AssetId,
                KpiId = incident.KpiId,
                IncidentTitle = incident.IncidentTitle,
                Description = incident.Description,
                Type = incident.Type,
                SeverityId = incident.SeverityId,
                StatusId = incident.StatusId,
                AssignedTo = incident.AssignedTo,
                UpdatedAt = incident.UpdatedAt,
                CreatedAt = incident.CreatedAt,
                CreatedBy = incident.CreatedBy
            };
        }

        private static IncidentDashboardDto MapToIncidentDashboardDto(Incident incident)
        {
            var dashboardDto = new IncidentDashboardDto
            {
                Id = incident.Id,
                IncidentTitle = incident.IncidentTitle,
                CreatedBy = incident.CreatedBy
            };

            // Severity (from Severity CommonLookup)
            var severityName = incident.Severity?.Name ?? string.Empty;
            dashboardDto.Severity = GetSeverityCode(severityName);
            dashboardDto.SeverityDescription = GetSeverityDescription(severityName);

            // Status (from Status CommonLookup)
            dashboardDto.Status = incident.Status?.Name ?? "Open";
            
            // Status Since (time since status changed - use UpdatedAt if available, otherwise CreatedAt)
            var statusDate = incident.UpdatedAt ?? incident.CreatedAt;
            dashboardDto.StatusSince = GetTimeAgo(statusDate);

            // Created Ago
            dashboardDto.CreatedAgo = GetTimeAgo(incident.CreatedAt);

            // KPI Description (from KpisLov or Description)
            dashboardDto.Description = incident.Description;
            dashboardDto.Kpi = incident.KpisLov?.KpiName ?? string.Empty;

            // Asset Name
            dashboardDto.AssetName = incident.Asset?.AssetName ?? string.Empty;

            // Ministry Name
            dashboardDto.MinistryName = incident.Asset?.Ministry?.MinistryName ?? string.Empty;
            dashboardDto.AssetUrl = incident.Asset?.AssetUrl ?? string.Empty;

            return dashboardDto;
        }

        private static string GetSeverityCode(string securityLevel)
        {
            if (string.IsNullOrEmpty(securityLevel))
                return string.Empty;

            // Extract P1, P2, P3, P4 from SecurityLevel
            if (securityLevel.Contains("P1", StringComparison.OrdinalIgnoreCase) || 
                securityLevel.Contains("Critical", StringComparison.OrdinalIgnoreCase))
                return "P1";
            if (securityLevel.Contains("P2", StringComparison.OrdinalIgnoreCase) || 
                securityLevel.Contains("High", StringComparison.OrdinalIgnoreCase))
                return "P2";
            if (securityLevel.Contains("P3", StringComparison.OrdinalIgnoreCase) || 
                securityLevel.Contains("Medium", StringComparison.OrdinalIgnoreCase))
                return "P3";
            if (securityLevel.Contains("P4", StringComparison.OrdinalIgnoreCase) || 
                securityLevel.Contains("Low", StringComparison.OrdinalIgnoreCase))
                return "P4";

            return securityLevel;
        }

        private static string GetSeverityDescription(string securityLevel)
        {
            if (string.IsNullOrEmpty(securityLevel))
                return string.Empty;

            if (securityLevel.Contains("P1", StringComparison.OrdinalIgnoreCase) || 
                securityLevel.Contains("Critical", StringComparison.OrdinalIgnoreCase))
                return "Critical severity";
            if (securityLevel.Contains("P2", StringComparison.OrdinalIgnoreCase) || 
                securityLevel.Contains("High", StringComparison.OrdinalIgnoreCase))
                return "High";
            if (securityLevel.Contains("P3", StringComparison.OrdinalIgnoreCase) || 
                securityLevel.Contains("Medium", StringComparison.OrdinalIgnoreCase))
                return "Medium";
            if (securityLevel.Contains("P4", StringComparison.OrdinalIgnoreCase) || 
                securityLevel.Contains("Low", StringComparison.OrdinalIgnoreCase))
                return "Low";

            return securityLevel;
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

        public async Task<APIResponse> GetIncidentCommentsAsync(int incidentId)
        {
            var incident = await _context.Incidents
                .FirstOrDefaultAsync(i => i.Id == incidentId && i.DeletedAt == null);

            if (incident == null)
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "Incident not found.",
                    Data = null
                };
            }

            var comments = await _context.IncidentComments
                .Where(c => c.IncidentId == incidentId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            var commentDtos = comments.Select(MapToIncidentCommentDto).ToList();

            return new APIResponse
            {
                IsSuccessful = true,
                Message = "Incident comments retrieved successfully",
                Data = commentDtos
            };
        }

        public async Task<APIResponse> AddIncidentCommentAsync(CreateIncidentCommentDto dto, string createdBy)
        {
            // Validate incident exists
            var incident = await _context.Incidents
                .Include(i => i.Comments)
                .Include(i => i.Asset)
                .Include(i => i.KpisLov)
                .FirstOrDefaultAsync(i => i.Id == dto.IncidentId && i.DeletedAt == null);

            if (incident == null)
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "Incident not found.",
                    Data = null
                };
            }

            // Validate comment is not empty
            if (string.IsNullOrWhiteSpace(dto.Comment))
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "Comment is required.",
                    Data = null
                };
            }

            // Create comment
            var comment = new IncidentComment
            {
                IncidentId = dto.IncidentId,
                Comment = dto.Comment.Trim(),
                Status = dto.Status ?? string.Empty,
                CreatedBy = createdBy,
                CreatedAt = DateTime.Now
            };

            _context.IncidentComments.Add(comment);

            // Update incident status if status is provided
            if (!string.IsNullOrWhiteSpace(dto.Status))
            {
                // Look up StatusId from CommonLookup by name
                var statusLookup = await _context.CommonLookups
                    .FirstOrDefaultAsync(cl => cl.Type.Equals("Status", StringComparison.OrdinalIgnoreCase) &&
                                              cl.Name.Equals(dto.Status, StringComparison.OrdinalIgnoreCase) &&
                                              cl.DeletedAt == null);
                
                if (statusLookup != null)
                {
                    incident.StatusId = statusLookup.Id;
                    incident.UpdatedAt = DateTime.Now;
                    incident.UpdatedBy = createdBy;

                    // Create history entry for status change
                    var history = new IncidentHistory
                    {
                        IncidentId = incident.Id,
                        AssetId = incident.AssetId,
                        KpiId = incident.KpiId,
                        IncidentTitle = incident.IncidentTitle,
                        Description = dto.Comment.Trim(),
                        Type = incident.Type,
                        SeverityId = incident.SeverityId,
                        StatusId = statusLookup.Id, // Store StatusId in history
                        AssignedTo = incident.AssignedTo,
                        CreatedBy = createdBy,
                        CreatedAt = DateTime.Now
                    };

                _context.IncidentHistories.Add(history);
                }
            }

            await _context.SaveChangesAsync();

            var commentDto = MapToIncidentCommentDto(comment);
            return new APIResponse
            {
                IsSuccessful = true,
                Message = "Comment added successfully",
                Data = commentDto
            };
        }

        private static IncidentCommentDto MapToIncidentCommentDto(IncidentComment comment)
        {
            return new IncidentCommentDto
            {
                Id = comment.Id,
                IncidentId = comment.IncidentId,
                Comment = comment.Comment,
                Status = comment.Status,
                CreatedBy = comment.CreatedBy,
                CreatedAt = comment.CreatedAt
            };
        }

        public async Task<APIResponse> GetIncidentDetailsAsync(int id)
        {
            var incident = await _context.Incidents
                .Include(i => i.Asset)
                    .ThenInclude(a => a.Ministry)
                .Include(i => i.Asset)
                    .ThenInclude(a => a.Department)
                .Include(i => i.KpisLov)
                .Include(i => i.Severity)
                .Include(i => i.Status)
                .Include(i => i.History)
                    .ThenInclude(h => h.Severity)
                .Include(i => i.History)
                    .ThenInclude(h => h.Status)
                .Include(i => i.Comments)
                .FirstOrDefaultAsync(i => i.Id == id && i.DeletedAt == null);

            if (incident == null)
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "Incident not found.",
                    Data = null
                };
            }

            // Format severity (e.g., "P1 - CRITICAL")
            var severityName = incident.Severity?.Name ?? string.Empty;
            var severityDisplay = GetSeverityDisplay(severityName);

            // Get timeline entries (from IncidentHistory)
            var timelineEntries = incident.History
                .OrderByDescending(h => h.CreatedAt)
                .Select(h => new IncidentTimelineDto
                {
                    Id = h.Id,
                    Time = FormatTime(h.CreatedAt),
                    User = h.CreatedBy,
                    Description = h.Description,
                    Status = h.Status?.Name ?? string.Empty,
                    CreatedAt = h.CreatedAt
                })
                .ToList();

            // Get comments
            var comments = incident.Comments
                .OrderByDescending(c => c.CreatedAt)
                .Select(MapToIncidentCommentDto)
                .ToList();

            var detailsDto = new IncidentDetailsDto
            {
                Id = incident.Id,
                IncidentTitle = incident.IncidentTitle,
                AssetName = incident.Asset.AssetName,
                Severity = severityDisplay,
                Status = incident.Status?.Name ?? "Open",
                CreatedAt = incident.CreatedAt,
                AssignedTo = incident.AssignedTo,
                KpiName = incident.KpisLov?.KpiName ?? string.Empty,
                AssetId = incident.AssetId,
                AssetUrl = incident.Asset?.AssetUrl ?? string.Empty,
                CreatedBy = incident.CreatedBy,
                Ministry = incident.Asset?.Ministry?.MinistryName ?? string.Empty,
                Department = incident.Asset?.Department?.DepartmentName ?? string.Empty,
                Description = incident.Description,
                Timeline = timelineEntries,
                Comments = comments
            };

            return new APIResponse
            {
                IsSuccessful = true,
                Message = "Incident details retrieved successfully",
                Data = detailsDto
            };
        }

        private static string GetSeverityDisplay(string severityName)
        {
            if (string.IsNullOrWhiteSpace(severityName))
                return string.Empty;

            // Format as "P1 - CRITICAL" or similar
            if (severityName.StartsWith("P1", StringComparison.OrdinalIgnoreCase))
                return "P1 - CRITICAL";
            if (severityName.StartsWith("P2", StringComparison.OrdinalIgnoreCase))
                return "P2 - HIGH";
            if (severityName.StartsWith("P3", StringComparison.OrdinalIgnoreCase))
                return "P3 - MEDIUM";
            if (severityName.StartsWith("P4", StringComparison.OrdinalIgnoreCase))
                return "P4 - LOW";

            return severityName.ToUpper();
        }

        private static string FormatTime(DateTime dateTime)
        {
            // Format as "12:00 PM" or "3:45 AM"
            return dateTime.ToString("h:mm tt", System.Globalization.CultureInfo.InvariantCulture);
        }

        private async Task<int> GetDefaultStatusIdAsync()
        {
            // Get default "Open" status from CommonLookup
            var openStatus = await _context.CommonLookups
                .FirstOrDefaultAsync(cl => cl.Type.Equals("IncidentStatus", StringComparison.OrdinalIgnoreCase) &&
                                          cl.Name.Equals("Open", StringComparison.OrdinalIgnoreCase) &&
                                          cl.DeletedAt == null);
            
            if (openStatus != null)
                return openStatus.Id;
            
            // If not found, try to get any status with Type "IncidentStatus"
            var anyStatus = await _context.CommonLookups
                .FirstOrDefaultAsync(cl => cl.Type.Equals("IncidentStatus", StringComparison.OrdinalIgnoreCase) &&
                                          cl.DeletedAt == null);
            
            return anyStatus?.Id ?? 0; // Return 0 if no status found (will need to be handled)
        }
    }
}
