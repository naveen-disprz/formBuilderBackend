using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Backend.Business;
using Backend.DTOs.Response;
using Backend.Enums;
using Backend.Filters;

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
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
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
                _logger.LogError(ex, $"Error getting response: {responseId}");
                return StatusCode(500, new { error = "An error occurred while getting the response" });
            }
        }

        [HttpGet("files/{fileId}")]
        public async Task<IActionResult> GetFile(Guid fileId, [FromQuery] bool download = false)
        {
            try
            {
                var fileContent = await _responseBL.GetFileContentAsync(fileId, CurrentUserId, CurrentUserRole);
                
                // TODO: Get file metadata to set proper content type
                var contentType = "application/octet-stream";
                
                if (download)
                {
                    return File(fileContent, contentType, $"file_{fileId}");
                }
                return File(fileContent, contentType);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting file: {fileId}");
                return StatusCode(500, new { error = "An error occurred while getting the file" });
            }
        }
    }
}
