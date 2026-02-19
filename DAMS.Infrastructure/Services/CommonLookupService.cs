using DAMS.Application.Interfaces;
using DAMS.Application.Models;
using DAMS.Application.DTOs;
using DAMS.Domain.Entities;
using DAMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DAMS.Infrastructure.Services
{
    public class CommonLookupService : ICommonLookupService
    {
        private readonly ApplicationDbContext _context;

        public CommonLookupService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<APIResponse> GetCommonLookupByIdAsync(int id)
        {
            var commonLookup = await _context.CommonLookups
                .FirstOrDefaultAsync(cl => cl.Id == id && cl.DeletedAt == null);

            if (commonLookup == null)
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "Common Lookup not found.",
                    Data = null
                };
            }

            var commonLookupDto = MapToCommonLookupDto(commonLookup);
            return new APIResponse
            {
                IsSuccessful = true,
                Message = "Common Lookup retrieved successfully",
                Data = commonLookupDto
            };
        }

        public async Task<APIResponse> GetAllCommonLookupsAsync(PagedRequest filter)
        {
            var query = _context.CommonLookups.Where(cl => cl.DeletedAt == null).AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                query = query.Where(cl =>
                    cl.Name.Contains(filter.SearchTerm) ||
                    cl.Type.Contains(filter.SearchTerm));
            }

            // Apply sorting
            if (!string.IsNullOrEmpty(filter.SortBy))
            {
                query = filter.SortBy.ToLower() switch
                {
                    "id" => filter.SortDescending ? query.OrderByDescending(cl => cl.Id) : query.OrderBy(cl => cl.Id),
                    "name" => filter.SortDescending ? query.OrderByDescending(cl => cl.Name) : query.OrderBy(cl => cl.Name),
                    "type" => filter.SortDescending ? query.OrderByDescending(cl => cl.Type) : query.OrderBy(cl => cl.Type),
                    "createdat" => filter.SortDescending ? query.OrderByDescending(cl => cl.CreatedAt) : query.OrderBy(cl => cl.CreatedAt),
                    "updatedat" => filter.SortDescending ? query.OrderByDescending(cl => cl.UpdatedAt ?? cl.CreatedAt) : query.OrderBy(cl => cl.UpdatedAt ?? cl.CreatedAt),
                    _ => query.OrderByDescending(cl => cl.UpdatedAt ?? cl.CreatedAt)
                };
            }
            else
            {
                query = query.OrderByDescending(cl => cl.UpdatedAt ?? cl.CreatedAt);
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply pagination
            var commonLookups = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var commonLookupDtos = commonLookups.Select(MapToCommonLookupDto).ToList();

            var pagedResponse = new PagedResponse<CommonLookupDto>
            {
                Data = commonLookupDtos,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };

            return new APIResponse
            {
                IsSuccessful = true,
                Message = "Common Lookups retrieved successfully",
                Data = pagedResponse
            };
        }

        public async Task<APIResponse> GetCommonLookupsByTypeAsync(string type)
        {
            var commonLookups = await _context.CommonLookups
                .Where(cl => cl.Type == type && cl.DeletedAt == null)
                .OrderByDescending(cl => cl.Id)
                .ToListAsync();

            var commonLookupDtos = commonLookups.Select(MapToCommonLookupDto).ToList();

            return new APIResponse
            {
                IsSuccessful = true,
                Message = "Common Lookups retrieved successfully",
                Data = commonLookupDtos
            };
        }

        public async Task<APIResponse> CreateCommonLookupAsync(CreateCommonLookupDto dto, string createdBy)
        {
            // Validate mandatory fields
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "Name is required.",
                    Data = null
                };
            }

            if (string.IsNullOrWhiteSpace(dto.Type))
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "Type is required.",
                    Data = null
                };
            }

            // Check if common lookup with same name and type already exists
            var existingCommonLookup = await _context.CommonLookups
                .FirstOrDefaultAsync(cl => 
                    cl.Name.ToLower() == dto.Name.ToLower() && 
                    cl.Type.ToLower() == dto.Type.ToLower() && 
                    cl.DeletedAt == null);

            if (existingCommonLookup != null)
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "Common Lookup with this Name and Type already exists.",
                    Data = null
                };
            }

            var commonLookup = new CommonLookup
            {
                Name = dto.Name.Trim(),
                Type = dto.Type.Trim(),
                CreatedAt = DateTime.Now,
                CreatedBy = createdBy
            };

            _context.CommonLookups.Add(commonLookup);
            await _context.SaveChangesAsync();

            var commonLookupDto = MapToCommonLookupDto(commonLookup);
            return new APIResponse
            {
                IsSuccessful = true,
                Message = "Common Lookup created successfully",
                Data = commonLookupDto
            };
        }

        public async Task<APIResponse> UpdateCommonLookupAsync(int id, UpdateCommonLookupDto dto, string updatedBy)
        {
            var commonLookup = await _context.CommonLookups
                .FirstOrDefaultAsync(cl => cl.Id == id && cl.DeletedAt == null);

            if (commonLookup == null)
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "Common Lookup not found.",
                    Data = null
                };
            }

            // If name or type is being updated, check for duplicates
            if ((dto.Name != null && dto.Name.ToLower() != commonLookup.Name.ToLower()) ||
                (dto.Type != null && dto.Type.ToLower() != commonLookup.Type.ToLower()))
            {
                var newName = dto.Name?.Trim() ?? commonLookup.Name;
                var newType = dto.Type?.Trim() ?? commonLookup.Type;

                var existingCommonLookup = await _context.CommonLookups
                    .FirstOrDefaultAsync(cl =>
                        cl.Id != id &&
                        cl.Name.ToLower() == newName.ToLower() &&
                        cl.Type.ToLower() == newType.ToLower() &&
                        cl.DeletedAt == null);

                if (existingCommonLookup != null)
                {
                    return new APIResponse
                    {
                        IsSuccessful = false,
                        Message = "Common Lookup with this Name and Type already exists.",
                        Data = null
                    };
                }
            }

            // Update fields
            if (dto.Name != null)
            {
                commonLookup.Name = dto.Name.Trim();
            }

            if (dto.Type != null)
            {
                commonLookup.Type = dto.Type.Trim();
            }

            commonLookup.UpdatedAt = DateTime.Now;
            commonLookup.UpdatedBy = updatedBy;

            await _context.SaveChangesAsync();

            var commonLookupDto = MapToCommonLookupDto(commonLookup);
            return new APIResponse
            {
                IsSuccessful = true,
                Message = "Common Lookup updated successfully",
                Data = commonLookupDto
            };
        }

        public async Task<APIResponse> DeleteCommonLookupAsync(int id, string deletedBy)
        {
            var commonLookup = await _context.CommonLookups
                .FirstOrDefaultAsync(cl => cl.Id == id && cl.DeletedAt == null);

            if (commonLookup == null)
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "Common Lookup not found.",
                    Data = null
                };
            }

            // Check if common lookup is being used by any assets
            var isUsed = await _context.Assets
                .AnyAsync(a => a.CitizenImpactLevelId == id && a.DeletedAt == null);

            if (isUsed)
            {
                return new APIResponse
                {
                    IsSuccessful = false,
                    Message = "Cannot delete Common Lookup. It is being used by one or more assets.",
                    Data = null
                };
            }

            commonLookup.DeletedAt = DateTime.Now;
            commonLookup.DeletedBy = deletedBy;
            commonLookup.UpdatedAt = DateTime.Now;
            commonLookup.UpdatedBy = deletedBy;

            await _context.SaveChangesAsync();

            return new APIResponse
            {
                IsSuccessful = true,
                Message = "Common Lookup deleted successfully",
                Data = null
            };
        }

        private static CommonLookupDto MapToCommonLookupDto(CommonLookup commonLookup)
        {
            return new CommonLookupDto
            {
                Id = commonLookup.Id,
                Name = commonLookup.Name,
                Type = commonLookup.Type
            };
        }
    }
}
