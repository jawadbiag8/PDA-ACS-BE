using DAMS.Application.Models;
using DAMS.Application.DTOs;

namespace DAMS.Application.Interfaces
{
    public interface IIncidentService
    {
        Task<APIResponse> GetIncidentByIdAsync(int id);
        Task<APIResponse> GetAllIncidentsAsync(IncidentFilterDto filter);
        Task<APIResponse> GetIncidentsByAssetIdAsync(int assetId, IncidentFilterDto? filter = null);
        Task<APIResponse> CreateIncidentAsync(CreateIncidentDto dto, string createdBy);
        Task<APIResponse> UpdateIncidentAsync(int id, UpdateIncidentDto dto, string updatedBy);
        Task<APIResponse> DeleteIncidentAsync(int id, string deletedBy);
        Task<APIResponse> GetIncidentCommentsAsync(int incidentId);
        Task<APIResponse> AddIncidentCommentAsync(CreateIncidentCommentDto dto, string createdBy);
        Task<APIResponse> GetIncidentDetailsAsync(int id);
    }
}
