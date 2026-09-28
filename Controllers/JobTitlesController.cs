using Microsoft.AspNetCore.Mvc;
using BackendApp.DTOs;
using BackendApp.Services;
using Microsoft.AspNetCore.Authorization;
using BackendApp.Configurations;
using BackendApp.DTOs.Common;

namespace BackendApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobTitlesController : ControllerBase
{
    private readonly JobTitleService _jobTitleService;

    public JobTitlesController(JobTitleService service)
    {
        _jobTitleService = service;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<JobTitleResponseDto>>> GetAll([FromQuery] JobTitleFilterRequestDTO request)
    {
        var result = await _jobTitleService.GetAllJobTitlesAsync(request);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetJobTitleById(int id)
    {
        var res = await _jobTitleService.GetJobTitleByIdAsync(id);
        return res == null ? NotFound(new { message = "Không tìm thấy chức danh." }) : Ok(res);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,HRManager")]
    public async Task<IActionResult> CreateJobTitle([FromBody] CreateJobTitleDto dto)
    {
        var res = await _jobTitleService.CreateJobTitleAsync(dto);
        return CreatedAtAction(nameof(GetJobTitleById), new { id = res.Id }, res);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,HRManager")]
    public async Task<IActionResult> UpdateJobTitle(int id, [FromBody] UpdateJobTitleDto dto)
    {
        var res = await _jobTitleService.UpdateJobTitleAsync(id, dto);
        return res == null ? NotFound(new { message = "Không tìm thấy chức danh." }) : Ok(res);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, message) = await _jobTitleService.DeleteJobTitleAsync(id);
        return success ? Ok(new { message }) : BadRequest(new { message });
    }
}