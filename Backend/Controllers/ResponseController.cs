using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Backend.Business;
using Backend.DTOs.Response;
using Backend.Enums;
using Backend.Filters;
using Backend.Exceptions;
using FileNotFoundException = Backend.Exceptions.FileNotFoundException;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api")]
    [Authorize]
    [ServiceFilter(typeof(UserContextActionFilter))]
    public class ResponseController : BaseApiController
    {
        private readonly IResponseBL _responseBL;
        private readonly ILogger<ResponseController> _logger;

        public ResponseController(IResponseBL responseBL, ILogger<ResponseController> logger)
        {
            _responseBL = responseBL;
            _logger = logger;
        }

        [HttpPost("form/{formId}/response")]
        [Authorize(Roles = nameof(UserRole.Learner))]
        public async Task<IActionResult> SubmitResponse(string formId, [FromBody] SubmitResponseDto submitDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                
                // Add client info
                submitDto.ClientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                submitDto.UserAgent = Request.Headers["User-Agent"].ToString();

                var result = await _responseBL.SubmitResponseAsync(formId, submitDto, CurrentUserId);
                return Created($"/api/response/{result.ResponseId}", result);
            }
            catch (FormNotFoundForResponseException ex)
            {
                _logger.LogWarning(ex, $"Form not found for response submission: {formId}");
                return NotFound(new { error = ex.Message, code = ex.ErrorCode });
            }
            catch (UnpublishedFormException ex)
            {
                _logger.LogWarning(ex, $"Attempted to submit response to unpublished form: {formId}");
                return BadRequest(new { error = ex.Message, code = ex.ErrorCode });
            }
            catch (DuplicateResponseException ex)
            {
                _logger.LogWarning(ex, $"Duplicate response attempt for form: {formId}");
                return Conflict(new { error = ex.Message, code = ex.ErrorCode });
            }
            catch (RequiredQuestionException ex)
            {
                _logger.LogWarning(ex, $"Required question not answered for form: {formId}");
                return BadRequest(new { error = ex.Message, code = ex.ErrorCode });
            }
            catch (ResponseValidationException ex)
            {
                _logger.LogWarning(ex, "Response validation failed");
                return BadRequest(new { error = ex.Message, code = ex.ErrorCode });
            }
            catch (ResponseDataAccessException ex)
            {
                _logger.LogError(ex, $"Data access error submitting response for form: {formId}");
                return StatusCode(500, new { error = ex.Message, code = ex.ErrorCode });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error submitting response for form: {formId}");
                return StatusCode(500, new { error = "An error occurred while submitting the response" });
            }
        }

        [HttpGet("form/{formId}/response")]
        [Authorize(Roles = nameof(UserRole.Admin))]
        public async Task<IActionResult> GetFormResponses(string formId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var result = await _responseBL.GetFormResponsesAsync(formId, page, pageSize, CurrentUserId);
                return Ok(result);
            }
            catch (FormNotFoundForResponseException ex)
            {
                _logger.LogWarning(ex, $"Form not found: {formId}");
                return NotFound(new { error = ex.Message, code = ex.ErrorCode });
            }
            catch (ResponseUnauthorizedException ex)
            {
                _logger.LogWarning(ex, $"Unauthorized access to form responses: {formId}");
                return Forbid(ex.Message);
            }
            catch (ResponseDataAccessException ex)
            {
                _logger.LogError(ex, $"Data access error getting responses for form: {formId}");
                return StatusCode(500, new { error = ex.Message, code = ex.ErrorCode });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting responses for form: {formId}");
                return StatusCode(500, new { error = "An error occurred while getting responses" });
            }
        }

        [HttpGet("response/{responseId}")]
        public async Task<IActionResult> GetResponseById(Guid responseId)
        {
            try
            {
                var result = await _responseBL.GetResponseByIdAsync(responseId, CurrentUserId, CurrentUserRole);
                return Ok(result);
            }
            catch (ResponseNotFoundException ex)
            {
                _logger.LogWarning(ex, $"Response not found: {responseId}");
                return NotFound(new { error = ex.Message, code = ex.ErrorCode });
            }
            catch (ResponseUnauthorizedException ex)
            {
                _logger.LogWarning(ex, $"Unauthorized access to response: {responseId}");
                return Forbid(ex.Message);
            }
            catch (ResponseDataAccessException ex)
            {
                _logger.LogError(ex, $"Data access error getting response: {responseId}");
                return StatusCode(500, new { error = ex.Message, code = ex.ErrorCode });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting response: {responseId}");
                return StatusCode(500, new { error = "An error occurred while getting the response" });
            }
        }

        [HttpGet("files/{fileId}")]
        public async Task<IActionResult> GetFile(Guid fileId, [FromQuery] bool download = false)
        {
            try
            {
                var file = await _responseBL.GetFileContentAsync(fileId, CurrentUserId, CurrentUserRole);
                
                byte[] fileByte = Convert.FromBase64String(file.FileContent);
                
                if (download)
                {
                    return File(fileByte, file.MimeType, file.FileName);
                }
                return File(fileByte, file.MimeType);
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogWarning(ex, $"File not found: {fileId}");
                return NotFound(new { error = ex.Message, code = ex.ErrorCode });
            }
            catch (ResponseUnauthorizedException ex)
            {
                _logger.LogWarning(ex, $"Unauthorized access to file: {fileId}");
                return Forbid(ex.Message);
            }
            catch (ResponseDataAccessException ex)
            {
                _logger.LogError(ex, $"Data access error getting file: {fileId}");
                return StatusCode(500, new { error = ex.Message, code = ex.ErrorCode });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting file: {fileId}");
                return StatusCode(500, new { error = "An error occurred while getting the file" });
            }
        }
        
        [HttpGet("responses")]
        [Authorize(Roles = nameof(UserRole.Learner))]
        public async Task<IActionResult> GetMyResponses([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var result = await _responseBL.GetResponsesByUserIdAsync(CurrentUserId, page, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting responses for user: {CurrentUserId}");
                return StatusCode(500, new { error = "An error occurred while getting responses" });
            }
        }
    }
}
