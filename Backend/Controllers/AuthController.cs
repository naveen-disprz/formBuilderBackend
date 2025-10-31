using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
using Backend.Business;
using Backend.DTOs.Auth;
using Backend.Exceptions;
using Backend.Filters;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [ServiceFilter(typeof(UserContextActionFilter))]
    public class AuthController : BaseApiController
    {
        private readonly IAuthBL _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthBL authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Register a new user
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _authService.RegisterAsync(registerDto);

                // Set JWT token in cookie (optional)
                SetTokenCookie(result.Token);

                return Created($"/api/users/{result.UserId}", result);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Invalid registration data");
                return BadRequest(new { error = ex.Message, code = ex.ErrorCode });
            }
            catch (UserAlreadyExistsException ex)
            {
                _logger.LogWarning(ex, "Registration conflict");
                return Conflict(new { error = ex.Message, code = ex.ErrorCode });
            }
            catch (PasswordPolicyException ex)
            {
                _logger.LogWarning(ex, "Password policy violation");
                return BadRequest(new { error = ex.Message, code = ex.ErrorCode });
            }
            catch (AuthException ex)
            {
                _logger.LogError(ex, "Authentication error during registration");
                return BadRequest(new { error = ex.Message, code = ex.ErrorCode });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during registration");
                return StatusCode(500, new { error = "An error occurred during registration" });
            }
        }

        /// <summary>
        /// Login user
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _authService.LoginAsync(loginDto);

                // Set JWT token in cookie (optional)
                SetTokenCookie(result.Token);

                return Ok(result);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Invalid login data");
                return BadRequest(new { error = ex.Message, code = ex.ErrorCode });
            }
            catch (AuthenticationFailedException ex)
            {
                _logger.LogWarning(ex, "Failed login attempt");
                return Unauthorized(new { error = ex.Message, code = ex.ErrorCode });
            }
            catch (UserNotFoundException ex)
            {
                _logger.LogWarning(ex, "User not found during login");
                return Unauthorized(new { error = "Invalid credentials", code = ex.ErrorCode });
            }
            catch (AuthException ex)
            {
                _logger.LogError(ex, "Authentication error during login");
                return Unauthorized(new { error = ex.Message, code = ex.ErrorCode });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during login");
                return StatusCode(500, new { error = "An error occurred during login" });
            }
        }

        /// <summary>
        /// Logout user
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                // Get token from Authorization header
                var token = Request.Headers["Authorization"]
                    .FirstOrDefault()?.Split(" ").Last();

                if (string.IsNullOrEmpty(token))
                {
                    // Try to get from cookie
                    token = Request.Cookies["jwt"];
                }

                if (!string.IsNullOrEmpty(token))
                {
                    await _authService.LogoutAsync(token);
                }

                // Clear cookie
                ClearTokenCookie();

                return Ok(new { message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, new { error = "An error occurred during logout" });
            }
        }

        /// <summary>
        /// Validate current token
        /// </summary>
        [HttpGet("validate")]
        [Authorize]
        public async Task<IActionResult> ValidateToken()
        {
            try
            {
                // var token = Request.Headers["Authorization"]
                //     .FirstOrDefault()?.Split(" ").Last();
                //
                // if (string.IsNullOrEmpty(token))
                // {
                //     throw new TokenException("No token provided");
                // }

                // var isValid = await _authService.ValidateTokenAsync(token);


                return Ok(new
                {
                    valid = true,
                    userId = CurrentUserId,
                    role = CurrentUserRole
                });
                // if (isValid)
                //{
                // Get user info from token claims
                // var userId = User.FindFirst("UserId")?.Value;
                // var email = User.FindFirst("Email")?.Value;
                // var role = User.FindFirst("Role")?.Value;
                //}

                return Unauthorized(new { valid = false, code = "INVALID_TOKEN" });
            }
            catch (TokenException ex)
            {
                return Unauthorized(new { error = ex.Message, code = ex.ErrorCode });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token");
                return StatusCode(500, new { error = "An error occurred during validation" });
            }
        }

        #region Helper Methods

        private void SetTokenCookie(string token)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddDays(7),
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps,
            };

            Response.Cookies.Append("jwt", token, cookieOptions);
        }

        private void ClearTokenCookie()
        {
            Response.Cookies.Delete("jwt");
        }

        #endregion
    }
}