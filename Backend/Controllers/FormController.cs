using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Backend.Business;
using Backend.DTOs.Form;
using Backend.Enums;
using Backend.Filters;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[ServiceFilter(typeof(UserContextActionFilter))]
public class FormController : BaseApiController
{
    private readonly IFormBL _formBL;
    private readonly ILogger<FormController> _logger;

    public FormController(IFormBL formBL, ILogger<FormController> logger)
    {
        _formBL = formBL;
        _logger = logger;
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> CreateForm([FromBody] CreateFormDto createFormDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _formBL.CreateFormAsync(createFormDto, CurrentUserId);

            return Created($"/api/forms/{result.Id}", result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating form");
            return StatusCode(500, new { error = "An error occurred while creating the form" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetForms([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var result = await _formBL.GetFormsAsync(page, pageSize, CurrentUserRole, CurrentUserId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting forms");
            return StatusCode(500, new { error = "An error occurred while getting forms" });
        }
    }

    [HttpGet("{formId}")]
    public async Task<IActionResult> GetFormById(string formId)
    {
        try
        {
            var result = await _formBL.GetFormByIdAsync(formId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting form: {formId}");
            return StatusCode(500, new { error = "An error occurred while getting the form" });
        }
    }

    [HttpPut("{formId}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> UpdateForm(string formId, [FromBody] UpdateFormDto updateFormDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _formBL.UpdateFormAsync(formId, updateFormDto, CurrentUserId);

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating form: {formId}");
            return StatusCode(500, new { error = "An error occurred while updating the form" });
        }
    }

    [HttpDelete("{formId}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> DeleteForm(string formId)
    {
        try
        {
            var result = await _formBL.DeleteFormAsync(formId, CurrentUserId);

            if (result)
            {
                return Ok(new { message = "Form deleted successfully" });
            }

            return BadRequest(new { error = "Failed to delete form" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting form: {formId}");
            return StatusCode(500, new { error = "An error occurred while deleting the form" });
        }
    }

    [HttpPost("{formId}/publish")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> PublishForm(string formId)
    {
        try
        {
            var result = await _formBL.PublishFormAsync(formId, CurrentUserId);

            if (result)
            {
                return Ok(new { message = "Form published successfully" });
            }

            return BadRequest(new { error = "Failed to publish form" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error publishing form: {formId}");
            return StatusCode(500, new { error = "An error occurred while publishing the form" });
        }
    }
}