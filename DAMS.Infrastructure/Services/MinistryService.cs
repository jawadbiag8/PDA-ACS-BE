using DAMS.Application.Interfaces;
using DAMS.Application.Models;
using DAMS.Application.DTOs;
using DAMS.Domain.Entities;
using DAMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace DAMS.Infrastructure.Services
{
    public class MinistryService : IMinistryService
    {
        private readonly ApplicationDbContext _context;

        public MinistryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<APIResponse> GetMinistryByIdAsync(int id)
        {
            var ministry = await _context.Ministries
                .FirstOrDefaultAsync(m => m.Id == id && m.DeletedAt == null);

            if (ministry == null)
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "Ministry not found.",
                    Data = null
                };
            }

            var ministryDto = MapToMinistryDto(ministry);
            return new APIResponse
            {
                IsSuccessful = true,
                Message = "Ministry retrieved successfully",
                Data = ministryDto
            };
        }

        public async Task<APIResponse> GetAllMinistriesAsync(MinistryFilterDto filter)
        {
            var query = _context.Ministries
                .Include(m => m.Departments)
                .Include(m => m.Assets)
                    .ThenInclude(a => a.Incidents)
                        .ThenInclude(i => i.Severity)
                .Where(m => m.DeletedAt == null)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                query = query.Where(m =>
                    m.MinistryName.Contains(filter.SearchTerm) ||
                    m.ContactName.Contains(filter.SearchTerm) ||
                    m.ContactEmail.Contains(filter.SearchTerm));
            }

            if (filter.CreatedFrom.HasValue)
            {
                query = query.Where(m => m.CreatedAt >= filter.CreatedFrom.Value);
            }

            if (filter.CreatedTo.HasValue)
            {
                query = query.Where(m => m.CreatedAt <= filter.CreatedTo.Value);
            }

            // Apply sorting
            if (!string.IsNullOrEmpty(filter.SortBy))
            {
                query = filter.SortBy.ToLower() switch
                {
                    "id" => filter.SortDescending ? query.OrderByDescending(m => m.Id) : query.OrderBy(m => m.Id),
                    "ministryname" => filter.SortDescending ? query.OrderByDescending(m => m.MinistryName) : query.OrderBy(m => m.MinistryName),
                    "contactname" => filter.SortDescending ? query.OrderByDescending(m => m.ContactName) : query.OrderBy(m => m.ContactName),
                    "contactemail" => filter.SortDescending ? query.OrderByDescending(m => m.ContactEmail) : query.OrderBy(m => m.ContactEmail),
                    "contactphone" => filter.SortDescending ? query.OrderByDescending(m => m.ContactPhone) : query.OrderBy(m => m.ContactPhone),
                    "numberofdepartments" => filter.SortDescending ? query.OrderByDescending(m => m.Departments.Count(d => d.DeletedAt == null)) : query.OrderBy(m => m.Departments.Count(d => d.DeletedAt == null)),
                    "numberofassets" => filter.SortDescending ? query.OrderByDescending(m => m.Assets.Count(a => a.DeletedAt == null)) : query.OrderBy(m => m.Assets.Count(a => a.DeletedAt == null)),
                    "incidentcount" or "openincidents" => filter.SortDescending ? query.OrderByDescending(m => m.Assets.Where(a => a.DeletedAt == null).Sum(a => a.Incidents.Count(i => i.DeletedAt == null))) : query.OrderBy(m => m.Assets.Where(a => a.DeletedAt == null).Sum(a => a.Incidents.Count(i => i.DeletedAt == null))),
                    "createdat" => filter.SortDescending ? query.OrderByDescending(m => m.CreatedAt) : query.OrderBy(m => m.CreatedAt),
                    "updatedat" => filter.SortDescending ? query.OrderByDescending(m => m.UpdatedAt ?? m.CreatedAt) : query.OrderBy(m => m.UpdatedAt ?? m.CreatedAt),
                    _ => query.OrderByDescending(m => m.UpdatedAt ?? m.CreatedAt)
                };
            }
            else
            {
                query = query.OrderByDescending(m => m.UpdatedAt ?? m.CreatedAt);
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply pagination
            var ministries = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var dashboardDtos = ministries.Select(MapToMinistryDashboardDto).ToList();

            var pagedResponse = new PagedResponse<MinistryDashboardDto>
            {
                Data = dashboardDtos,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };

            return new APIResponse
            {
                IsSuccessful = true,
                Message = "Ministries retrieved successfully",
                Data = pagedResponse
            };
        }

        public async Task<APIResponse> GetAllMinistriesAsync()
        {
            var query = await _context.Ministries
                .Where(m => m.DeletedAt == null)
                .Select(x => new
                {
                    Id = x.Id,
                    MinistryName = x.MinistryName
                }
                ).ToListAsync();

            return new APIResponse
            {
                IsSuccessful = true,
                Message = "Ministries retrieved successfully",
                Data = query
            };
        }

        public async Task<APIResponse> CreateMinistryAsync(CreateMinistryDto dto, string createdBy)
        {
            // Validate mandatory fields
            if (string.IsNullOrWhiteSpace(dto.MinistryName))
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "Ministry Name is required.",
                    Data = null
                };
            }

            // Check if ministry name already exists
            var existingMinistry = await _context.Ministries
                .FirstOrDefaultAsync(m => m.MinistryName.ToLower() == dto.MinistryName.ToLower() && m.DeletedAt == null);
            if (existingMinistry != null)
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "Ministry Name must be unique.",
                    Data = null
                };
            }

            // Validate email format if provided
            if (!string.IsNullOrWhiteSpace(dto.ContactEmail) && !IsValidEmail(dto.ContactEmail))
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "Contact Email must be a valid email format.",
                    Data = null
                };
            }

            // Validate phone format if provided
            if (!string.IsNullOrWhiteSpace(dto.ContactPhone) && !IsValidPhone(dto.ContactPhone))
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "Contact Phone must contain valid phone characters.",
                    Data = null
                };
            }

            var ministry = new Ministry
            {
                MinistryName = dto.MinistryName.Trim(),
                ContactName = dto.ContactName ?? string.Empty,
                ContactEmail = dto.ContactEmail ?? string.Empty,
                ContactPhone = dto.ContactPhone ?? string.Empty,
                CreatedBy = createdBy,
                CreatedAt = DateTime.Now
            };

            _context.Ministries.Add(ministry);
            await _context.SaveChangesAsync();

            var ministryDto = MapToMinistryDto(ministry);
            return new APIResponse
            {
                IsSuccessful = true,
                Message = "Ministry created successfully",
                Data = ministryDto
            };
        }

        public async Task<APIResponse> UpdateMinistryAsync(int id, UpdateMinistryDto dto, string updatedBy)
        {
            var ministry = await _context.Ministries
                .FirstOrDefaultAsync(m => m.Id == id && m.DeletedAt == null);

            if (ministry == null)
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "Ministry not found.",
                    Data = null
                };
            }

            // Validate and update name if provided
            if (!string.IsNullOrWhiteSpace(dto.MinistryName))
            {
                // Check if name already exists (excluding current ministry)
                var existingMinistry = await _context.Ministries
                    .FirstOrDefaultAsync(m => m.MinistryName.ToLower() == dto.MinistryName.ToLower() && m.Id != id && m.DeletedAt == null);
                if (existingMinistry != null)
                {
                    return new APIResponse
                    {
                        IsSuccessful = false,
                        Message = "Ministry Name must be unique.",
                        Data = null
                    };
                }
                ministry.MinistryName = dto.MinistryName.Trim();
            }

            if (dto.ContactName != null)
                ministry.ContactName = dto.ContactName;

            // Validate and update email if provided
            if (dto.ContactEmail != null)
            {
                if (!string.IsNullOrWhiteSpace(dto.ContactEmail) && !IsValidEmail(dto.ContactEmail))
                {
                    return new APIResponse
                    {
                        IsSuccessful = false,
                        Message = "Contact Email must be a valid email format.",
                        Data = null
                    };
                }
                ministry.ContactEmail = dto.ContactEmail;
            }

            // Validate and update phone if provided
            if (dto.ContactPhone != null)
            {
                if (!string.IsNullOrWhiteSpace(dto.ContactPhone) && !IsValidPhone(dto.ContactPhone))
                {
                    return new APIResponse
                    {
                        IsSuccessful = false,
                        Message = "Contact Phone must contain valid phone characters.",
                        Data = null
                    };
                }
                ministry.ContactPhone = dto.ContactPhone;
            }

            ministry.UpdatedAt = DateTime.Now;
            ministry.UpdatedBy = updatedBy;

            await _context.SaveChangesAsync();

            var ministryDto = MapToMinistryDto(ministry);
            return new APIResponse
            {
                IsSuccessful = true,
                Message = "Ministry updated successfully",
                Data = ministryDto
            };
        }

        public async Task<APIResponse> DeleteMinistryAsync(int id, string deletedBy)
        {
            var ministry = await _context.Ministries
                .FirstOrDefaultAsync(m => m.Id == id && m.DeletedAt == null);

            if (ministry == null)
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "Ministry not found.",
                    Data = null
                };
            }

            ministry.DeletedAt = DateTime.Now;
            ministry.DeletedBy = deletedBy;
            ministry.UpdatedAt = DateTime.Now;
            ministry.UpdatedBy = deletedBy;

            await _context.SaveChangesAsync();

            return new APIResponse
            {
                IsSuccessful = true,
                Message = "Ministry deleted successfully",
                Data = null
            };
        }

        private static MinistryDto MapToMinistryDto(Ministry ministry)
        {
            return new MinistryDto
            {
                Id = ministry.Id,
                MinistryName = ministry.MinistryName,
                ContactName = ministry.ContactName,
                ContactEmail = ministry.ContactEmail,
                ContactPhone = ministry.ContactPhone,
                CreatedAt = ministry.CreatedAt,
                CreatedBy = ministry.CreatedBy
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

        private static MinistryDashboardDto MapToMinistryDashboardDto(Ministry ministry)
        {
            // Count departments (excluding deleted)
            var departmentCount = ministry.Departments?.Count(d => d.DeletedAt == null) ?? 0;

            // Count assets (excluding deleted)
            var assets = ministry.Assets?.Where(a => a.DeletedAt == null).ToList() ?? new List<Asset>();
            var assetCount = assets.Count;

            // Count open incidents across all assets
            var openIncidents = assets
                .SelectMany(a => a.Incidents ?? new List<Incident>())
                .Where(i => i.DeletedAt == null && i.StatusId != 12)
                .ToList();

            var openIncidentsCount = openIncidents.Count;

            // Count high severity incidents (P1 - Critical)
            var highSeverityIncidents = openIncidents
                .Count(i => (i.Severity?.Name.Contains("P1", StringComparison.OrdinalIgnoreCase) ?? false) ||
                           (i.Severity?.Name.Contains("Critical", StringComparison.OrdinalIgnoreCase) ?? false));

            return new MinistryDashboardDto
            {
                Id = ministry.Id,
                MinistryName = ministry.MinistryName,
                NumberOfDepartments = departmentCount,
                NumberOfAssets = assetCount,
                ContactName = ministry.ContactName,
                ContactPhone = ministry.ContactPhone,
                OpenIncidents = openIncidentsCount,
                HighSeverityIncidents = highSeverityIncidents
            };
        }
    }
}
