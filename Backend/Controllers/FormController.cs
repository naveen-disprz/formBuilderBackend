using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Backend.Business;
using Backend.DTOs.Form;
using Backend.Enums;
using Backend.Filters;
using Backend.Exceptions;

namespace Backend.Controllers
{
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
            catch (FormValidationException ex)
            {
                _logger.LogWarning(ex, "Form validation failed");
                return BadRequest(new { error = ex.Message, code = ex.ErrorCode });
            }
            catch (QuestionValidationException ex)
            {
                _logger.LogWarning(ex, "Question validation failed");
                return BadRequest(new { error = ex.Message, code = ex.ErrorCode });
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
            catch (FormDataAccessException ex)
            {
                _logger.LogError(ex, "Data access error getting forms");
                return StatusCode(500, new { error = ex.Message, code = ex.ErrorCode });
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
            catch (FormNotFoundException ex)
            {
                _logger.LogWarning(ex, $"Form not found: {formId}");
                return NotFound(new { error = ex.Message, code = ex.ErrorCode });
            }
            catch (FormDataAccessException ex)
            {
                _logger.LogError(ex, $"Data access error getting form: {formId}");
                return StatusCode(500, new { error = ex.Message, code = ex.ErrorCode });
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
            catch (FormNotFoundException ex)
            {
                _logger.LogWarning(ex, $"Form not found: {formId}");
                return NotFound(new { error = ex.Message, code = ex.ErrorCode });
            }
            catch (FormUnauthorizedException ex)
            {
                _logger.LogWarning(ex, $"Unauthorized form update attempt: {formId}");
                return Forbid(ex.Message);
            }
            catch (FormLockedException ex)
            {
                _logger.LogWarning(ex, $"Attempted to update locked form: {formId}");
                return Conflict(new { error = ex.Message, code = ex.ErrorCode });
            }
            catch (QuestionValidationException ex)
            {
                _logger.LogWarning(ex, "Question validation failed during update");
                return BadRequest(new { error = ex.Message, code = ex.ErrorCode });
            }
            catch (FormDataAccessException ex)
            {
                _logger.LogError(ex, $"Data access error updating form: {formId}");
                return StatusCode(500, new { error = ex.Message, code = ex.ErrorCode });
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

                throw new FormOperationException("Failed to delete form");
            }
            catch (FormNotFoundException ex)
            {
                _logger.LogWarning(ex, $"Form not found: {formId}");
                return NotFound(new { error = ex.Message, code = ex.ErrorCode });
            }
            catch (FormOperationException ex)
            {
                _logger.LogWarning(ex, $"Form operation failed: {formId}");
                return BadRequest(new { error = ex.Message, code = ex.ErrorCode });
            }
            catch (FormDataAccessException ex)
            {
                _logger.LogError(ex, $"Data access error deleting form: {formId}");
                return StatusCode(500, new { error = ex.Message, code = ex.ErrorCode });
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

                throw new FormOperationException("Failed to publish form");
            }
            catch (FormNotFoundException ex)
            {
                _logger.LogWarning(ex, $"Form not found: {formId}");
                return NotFound(new { error = ex.Message, code = ex.ErrorCode });
            }
            catch (FormOperationException ex)
            {
                _logger.LogWarning(ex, $"Form operation failed: {formId}");
                return BadRequest(new { error = ex.Message, code = ex.ErrorCode });
            }
            catch (FormDataAccessException ex)
            {
                _logger.LogError(ex, $"Data access error publishing form: {formId}");
                return StatusCode(500, new { error = ex.Message, code = ex.ErrorCode });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error publishing form: {formId}");
                return StatusCode(500, new { error = "An error occurred while publishing the form" });
            }
        }
    }
}
