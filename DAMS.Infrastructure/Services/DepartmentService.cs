using DAMS.Application.Interfaces;
using DAMS.Application.Models;
using DAMS.Application.DTOs;
using DAMS.Domain.Entities;
using DAMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace DAMS.Infrastructure.Services
{
    public class DepartmentService : IDepartmentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DepartmentService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        private string GetCurrentUsername()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user?.Identity?.Name 
                ?? user?.FindFirst(ClaimTypes.Name)?.Value 
                ?? user?.FindFirst(ClaimTypes.Email)?.Value 
                ?? string.Empty;
        }

        public async Task<APIResponse> GetDepartmentByIdAsync(int id)
        {
            var department = await _context.Departments
                .Include(d => d.Ministry)
                .FirstOrDefaultAsync(d => d.Id == id && d.DeletedAt == null);

            if (department == null)
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "Department not found.",
                    Data = null
                };
            }

            var departmentDto = MapToDepartmentDto(department);
            return new APIResponse
            {
                IsSuccessful = true,
                Message = "Department retrieved successfully",
                Data = departmentDto
            };
        }

        public async Task<APIResponse> GetAllDepartmentsAsync(DepartmentFilterDto filter)
        {
            var query = _context.Departments
                .Include(d => d.Ministry)
                .Where(d => d.DeletedAt == null)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                query = query.Where(d => 
                    d.DepartmentName.Contains(filter.SearchTerm) || 
                    d.ContactName.Contains(filter.SearchTerm) ||
                    d.ContactEmail.Contains(filter.SearchTerm));
            }

            if (filter.MinistryId.HasValue)
            {
                query = query.Where(d => d.MinistryId == filter.MinistryId.Value);
            }

            if (filter.CreatedFrom.HasValue)
            {
                query = query.Where(d => d.CreatedAt >= filter.CreatedFrom.Value);
            }

            if (filter.CreatedTo.HasValue)
            {
                query = query.Where(d => d.CreatedAt <= filter.CreatedTo.Value);
            }

            // Apply sorting
            if (!string.IsNullOrEmpty(filter.SortBy))
            {
                query = filter.SortBy.ToLower() switch
                {
                    "id" => filter.SortDescending ? query.OrderByDescending(d => d.Id) : query.OrderBy(d => d.Id),
                    "departmentname" => filter.SortDescending ? query.OrderByDescending(d => d.DepartmentName) : query.OrderBy(d => d.DepartmentName),
                    "ministryid" => filter.SortDescending ? query.OrderByDescending(d => d.MinistryId) : query.OrderBy(d => d.MinistryId),
                    "ministryname" => filter.SortDescending ? query.OrderByDescending(d => d.Ministry != null ? d.Ministry.MinistryName : "") : query.OrderBy(d => d.Ministry != null ? d.Ministry.MinistryName : ""),
                    "contactname" => filter.SortDescending ? query.OrderByDescending(d => d.ContactName) : query.OrderBy(d => d.ContactName),
                    "contactemail" => filter.SortDescending ? query.OrderByDescending(d => d.ContactEmail) : query.OrderBy(d => d.ContactEmail),
                    "contactphone" => filter.SortDescending ? query.OrderByDescending(d => d.ContactPhone) : query.OrderBy(d => d.ContactPhone),
                    "createdat" => filter.SortDescending ? query.OrderByDescending(d => d.CreatedAt) : query.OrderBy(d => d.CreatedAt),
                    "updatedat" => filter.SortDescending ? query.OrderByDescending(d => d.UpdatedAt ?? d.CreatedAt) : query.OrderBy(d => d.UpdatedAt ?? d.CreatedAt),
                    _ => query.OrderByDescending(d => d.UpdatedAt ?? d.CreatedAt)
                };
            }
            else
            {
                query = query.OrderByDescending(d => d.UpdatedAt ?? d.CreatedAt);
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply pagination
            var departments = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var departmentDtos = departments.Select(MapToDepartmentDto).ToList();

            var pagedResponse = new PagedResponse<DepartmentDto>
            {
                Data = departmentDtos,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };

            return new APIResponse
            {
                IsSuccessful = true,
                Message = "Departments retrieved successfully",
                Data = pagedResponse
            };
        }

        public async Task<APIResponse> GetAllDepartmentsAsync(int ministryId)
        {
            var query = await _context.Departments
                 .Where(d => d.DeletedAt == null && d.MinistryId == ministryId)
                 .Select(x => new
                 {
                     x.Id,
                     x.DepartmentName
                 })
                 .ToListAsync();

            return new APIResponse
            {
                IsSuccessful = true,
                Message = "Departments retrieved successfully",
                Data = query
            };
        }

        public async Task<APIResponse> GetDepartmentsByMinistryIdAsync(int ministryId)
        {
            var departments = await _context.Departments
                .Include(d => d.Ministry)
                .Where(d => d.MinistryId == ministryId && d.DeletedAt == null)
                .ToListAsync();

            var departmentDtos = departments.Select(MapToDepartmentDto).ToList();

            return new APIResponse
            {
                IsSuccessful = true,
                Message = "Departments retrieved successfully",
                Data = departmentDtos
            };
        }

        public async Task<APIResponse> CreateDepartmentAsync(CreateDepartmentDto dto, string createdBy)
        {
            // Validate mandatory fields
            if (string.IsNullOrWhiteSpace(dto.DepartmentName))
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "Department Name is required.",
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

            // Check if department name already exists within the ministry
            var existingDepartment = await _context.Departments
                .FirstOrDefaultAsync(d => d.DepartmentName.ToLower() == dto.DepartmentName.ToLower() && 
                                         d.MinistryId == dto.MinistryId && 
                                         d.DeletedAt == null);
            if (existingDepartment != null)
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "Department Name must be unique within the ministry.",
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

            var department = new Department
            {
                MinistryId = dto.MinistryId,
                DepartmentName = dto.DepartmentName.Trim(),
                ContactName = dto.ContactName ?? string.Empty,
                ContactEmail = dto.ContactEmail ?? string.Empty,
                ContactPhone = dto.ContactPhone ?? string.Empty,
                CreatedBy = createdBy,
                CreatedAt = DateTime.Now
            };

            _context.Departments.Add(department);
            await _context.SaveChangesAsync();

            var departmentDto = MapToDepartmentDto(department);
            return new APIResponse
            {
                IsSuccessful = true,
                Message = "Department created successfully",
                Data = departmentDto
            };
        }

        public async Task<APIResponse> UpdateDepartmentAsync(int id, UpdateDepartmentDto dto, string updatedBy)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null || department.DeletedAt != null)
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "Department not found.",
                    Data = null
                };
            }

            var ministryId = dto.MinistryId ?? department.MinistryId;

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
                department.MinistryId = dto.MinistryId.Value;
            }

            // Validate and update name if provided
            if (!string.IsNullOrWhiteSpace(dto.DepartmentName))
            {
                // Check if name already exists within the ministry (excluding current department)
                var existingDepartment = await _context.Departments
                    .FirstOrDefaultAsync(d => d.DepartmentName.ToLower() == dto.DepartmentName.ToLower() && 
                                             d.MinistryId == ministryId && 
                                             d.Id != id && 
                                             d.DeletedAt == null);
                if (existingDepartment != null)
                {
                    return new APIResponse
                    {
                        IsSuccessful = false,
                        Message = "Department Name must be unique within the ministry.",
                        Data = null
                    };
                }
                department.DepartmentName = dto.DepartmentName.Trim();
            }

            if (dto.ContactName != null)
                department.ContactName = dto.ContactName;

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
                department.ContactEmail = dto.ContactEmail;
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
                department.ContactPhone = dto.ContactPhone;
            }

            department.UpdatedAt = DateTime.Now;
            department.UpdatedBy = updatedBy;

            await _context.SaveChangesAsync();

            var departmentDto = MapToDepartmentDto(department);
            return new APIResponse
            {
                IsSuccessful = true,
                Message = "Department updated successfully",
                Data = departmentDto
            };
        }

        public async Task<APIResponse> DeleteDepartmentAsync(int id, string deletedBy)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null || department.DeletedAt != null)
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "Department not found.",
                    Data = null
                };
            }

            department.DeletedAt = DateTime.Now;
            department.DeletedBy = deletedBy;
            department.UpdatedAt = DateTime.Now;
            department.UpdatedBy = deletedBy;

            await _context.SaveChangesAsync();

            return new APIResponse
            {
                IsSuccessful = true,
                Message = "Department deleted successfully",
                Data = null
            };
        }

        private static DepartmentDto MapToDepartmentDto(Department department)
        {
            return new DepartmentDto
            {
                Id = department.Id,
                MinistryId = department.MinistryId,
                DepartmentName = department.DepartmentName,
                ContactName = department.ContactName,
                ContactEmail = department.ContactEmail,
                ContactPhone = department.ContactPhone,
                CreatedAt = department.CreatedAt,
                CreatedBy = department.CreatedBy
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
    }
}
