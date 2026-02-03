using DAMS.Application.DTOs;
using DAMS.Application.Interfaces;
using DAMS.Application.Models;
using DAMS.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DAMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "PDA Analyst,PMO Executive")]
    public class DepartmentController : ControllerBase
    {
        private readonly IDepartmentService _departmentService;

        public DepartmentController(IDepartmentService departmentService)
        {
            _departmentService = departmentService;
        }

        private string GetCurrentUsername()
        {
            return User?.Identity?.Name ?? string.Empty;
        }

        [HttpGet]
        public async Task<ActionResult<APIResponse>> GetAllDepartments([FromQuery] DepartmentFilterDto filter)
        {
            var response = await _departmentService.GetAllDepartmentsAsync(filter);
            return Ok(response);
        }

        [HttpGet("getall/{ministryId}")]
        public async Task<ActionResult<APIResponse>> GetAllDepartments(int ministryId)
        {
            var response = await _departmentService.GetAllDepartmentsAsync(ministryId);
            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<APIResponse>> GetDepartmentById(int id)
        {
            var response = await _departmentService.GetDepartmentByIdAsync(id);
            if (!response.IsSuccessful)
            {
                return NotFound(response);
            }
            return Ok(response);
        }

        [HttpGet("ministry/{ministryId}")]
        public async Task<ActionResult<APIResponse>> GetDepartmentsByMinistry(int ministryId)
        {
            var response = await _departmentService.GetDepartmentsByMinistryIdAsync(ministryId);
            return Ok(response);
        }

        [HttpPost]
        public async Task<ActionResult<APIResponse>> CreateDepartment([FromBody] CreateDepartmentDto dto)
        {
            var username = GetCurrentUsername();
            var response = await _departmentService.CreateDepartmentAsync(dto, username);

            if (!response.IsSuccessful)
            {
                return BadRequest(response);
            }

            if (response.Data is DepartmentDto departmentDto)
            {
                return CreatedAtAction(nameof(GetDepartmentById), new { id = departmentDto.Id }, response);
            }

            return CreatedAtAction(nameof(GetDepartmentById), new { id = 0 }, response);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<APIResponse>> UpdateDepartment(int id, [FromBody] UpdateDepartmentDto dto)
        {
            var username = GetCurrentUsername();
            var response = await _departmentService.UpdateDepartmentAsync(id, dto, username);

            return Ok(response);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<APIResponse>> DeleteDepartment(int id)
        {
            var username = GetCurrentUsername();
            var response = await _departmentService.DeleteDepartmentAsync(id, username);
            return Ok(response);
        }
    }
}
