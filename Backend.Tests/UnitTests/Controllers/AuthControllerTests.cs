using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Backend.Controllers;
using Backend.Business;
using Backend.DTOs.Auth;
using Backend.Enums;
using Backend.Exceptions;
using System;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.Extensions.Primitives;

namespace Backend.Tests.UnitTests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IAuthBL> _authBLMock;
        private readonly Mock<ILogger<AuthController>> _loggerMock;
        private readonly AuthController _authController;

        public AuthControllerTests()
        {
            _authBLMock = new Mock<IAuthBL>();
            _loggerMock = new Mock<ILogger<AuthController>>();
            _authController = new AuthController(_authBLMock.Object, _loggerMock.Object);

            // Setup HttpContext
            var httpContext = new DefaultHttpContext();
            _authController.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        [Fact]
        public async Task Register_WithValidModel_ReturnsCreatedResult()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "test@example.com",
                Username = "testuser",
                Password = "Password123!",
                ConfirmPassword = "Password123!"
            };

            var authResponse = new AuthResponseDto
            {
                Success = true,
                Token = "jwt-token",
                UserId = Guid.NewGuid(),
                Email = registerDto.Email,
                Username = registerDto.Username,
                Role = UserRole.Learner,
                Message = "Registration successful"
            };

            _authBLMock.Setup(x => x.RegisterAsync(It.IsAny<RegisterDto>()))
                .ReturnsAsync(authResponse);

            // Act
            var result = await _authController.Register(registerDto);

            // Assert
            result.Should().BeOfType<CreatedResult>();
            var createdResult = result as CreatedResult;
            createdResult.Location.Should().Contain($"/api/users/{authResponse.UserId}");
            createdResult.Value.Should().BeEquivalentTo(authResponse);
        }

        [Fact]
        public async Task Register_WithInvalidModel_ReturnsBadRequest()
        {
            // Arrange
            _authController.ModelState.AddModelError("Email", "Email is required");
            var registerDto = new RegisterDto();

            // Act
            var result = await _authController.Register(registerDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Register_WithValidationException_ReturnsBadRequest()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "",
                Username = "testuser",
                Password = "Password123!",
                ConfirmPassword = "Password123!"
            };

            _authBLMock.Setup(x => x.RegisterAsync(It.IsAny<RegisterDto>()))
                .ThrowsAsync(new ValidationException("Email is required"));

            // Act
            var result = await _authController.Register(registerDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult.Value.Should().BeEquivalentTo(new { error = "Email is required", code = "VALIDATION_ERROR" });
        }

        [Fact]
        public async Task Register_WithUserAlreadyExistsException_ReturnsConflict()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "existing@example.com",
                Username = "testuser",
                Password = "Password123!",
                ConfirmPassword = "Password123!"
            };

            _authBLMock.Setup(x => x.RegisterAsync(It.IsAny<RegisterDto>()))
                .ThrowsAsync(new UserAlreadyExistsException("User already exists"));

            // Act
            var result = await _authController.Register(registerDto);

            // Assert
            result.Should().BeOfType<ConflictObjectResult>();
            var conflictResult = result as ConflictObjectResult;
            conflictResult.Value.Should().BeEquivalentTo(new { error = "User already exists", code = "USER_EXISTS" });
        }

        [Fact]
        public async Task Register_WithPasswordPolicyException_ReturnsBadRequest()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "test@example.com",
                Username = "testuser",
                Password = "weak",
                ConfirmPassword = "weak"
            };

            _authBLMock.Setup(x => x.RegisterAsync(It.IsAny<RegisterDto>()))
                .ThrowsAsync(new PasswordPolicyException("Password too weak"));

            // Act
            var result = await _authController.Register(registerDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult.Value.Should().BeEquivalentTo(new { error = "Password too weak", code = "PASSWORD_POLICY" });
        }

        [Fact]
        public async Task Register_WithUnexpectedException_ReturnsInternalServerError()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "test@example.com",
                Username = "testuser",
                Password = "Password123!",
                ConfirmPassword = "Password123!"
            };

            _authBLMock.Setup(x => x.RegisterAsync(It.IsAny<RegisterDto>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _authController.Register(registerDto);

            // Assert
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult.StatusCode.Should().Be(500);
            objectResult.Value.Should().BeEquivalentTo(new { error = "An error occurred during registration" });
        }

        [Fact]
        public async Task Login_WithValidCredentials_ReturnsOkResult()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                EmailOrUsername = "test@example.com",
                Password = "Password123!"
            };

            var authResponse = new AuthResponseDto
            {
                Success = true,
                Token = "jwt-token",
                UserId = Guid.NewGuid(),
                Email = "test@example.com",
                Username = "testuser",
                Role = UserRole.Learner,
                Message = "Login successful"
            };

            _authBLMock.Setup(x => x.LoginAsync(It.IsAny<LoginDto>()))
                .ReturnsAsync(authResponse);

            // Act
            var result = await _authController.Login(loginDto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult.Value.Should().BeEquivalentTo(authResponse);
        }

        [Fact]
        public async Task Login_WithInvalidModel_ReturnsBadRequest()
        {
            // Arrange
            _authController.ModelState.AddModelError("Password", "Password is required");
            var loginDto = new LoginDto();

            // Act
            var result = await _authController.Login(loginDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Login_WithValidationException_ReturnsBadRequest()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                EmailOrUsername = "",
                Password = "Password123!"
            };

            _authBLMock.Setup(x => x.LoginAsync(It.IsAny<LoginDto>()))
                .ThrowsAsync(new ValidationException("Email/Username is required"));

            // Act
            var result = await _authController.Login(loginDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Login_WithAuthenticationFailedException_ReturnsUnauthorized()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                EmailOrUsername = "test@example.com",
                Password = "WrongPassword!"
            };

            _authBLMock.Setup(x => x.LoginAsync(It.IsAny<LoginDto>()))
                .ThrowsAsync(new AuthenticationFailedException("Invalid credentials"));

            // Act
            var result = await _authController.Login(loginDto);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
            var unauthorizedResult = result as UnauthorizedObjectResult;
            unauthorizedResult.Value.Should().BeEquivalentTo(new { error = "Invalid credentials", code = "AUTH_FAILED" });
        }

        [Fact]
        public async Task Logout_WithValidToken_ReturnsOkResult()
        {
            // Arrange
            var token = "valid-jwt-token";
            _authController.HttpContext.Request.Headers["Authorization"] = $"Bearer {token}";

            _authBLMock.Setup(x => x.LogoutAsync(It.IsAny<string>()))
                .ReturnsAsync(true);

            // Act
            var result = await _authController.Logout();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult.Value.Should().BeEquivalentTo(new { message = "Logged out successfully" });
        }

        [Fact]
        public async Task Logout_WithTokenFromCookie_ReturnsOkResult()
        {
            // Arrange
            var token = "valid-jwt-token";
            _authController.HttpContext.Request.Cookies = new RequestCookieCollection(new Dictionary<string, string>
            {
                { "jwt", token }
            });

            _authBLMock.Setup(x => x.LogoutAsync(It.IsAny<string>()))
                .ReturnsAsync(true);

            // Act
            var result = await _authController.Logout();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task Logout_WithNoToken_StillReturnsOkResult()
        {
            // Arrange
            // No token in headers or cookies

            // Act
            var result = await _authController.Logout();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            _authBLMock.Verify(x => x.LogoutAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ValidateToken_WithValidToken_ReturnsOkWithUserInfo()
        {
            // Arrange
            var token = "valid-jwt-token";
            var userId = Guid.NewGuid();
            
            _authController.HttpContext.Request.Headers["Authorization"] = $"Bearer {token}";
            
            // Setup user claims
            var claims = new List<Claim>
            {
                new Claim("UserId", userId.ToString()),
                new Claim("Email", "test@example.com"),
                new Claim("Role", "Learner")
            };
            _authController.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims));

            _authBLMock.Setup(x => x.ValidateTokenAsync(It.IsAny<string>()))
                .ReturnsAsync(true);

            // Act
            var result = await _authController.ValidateToken();

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult.Value.Should().BeEquivalentTo(new
            {
                valid = true,
                userId = userId.ToString(),
                email = "test@example.com",
                role = "Learner"
            });
        }

        [Fact]
        public async Task ValidateToken_WithNoToken_ThrowsTokenException()
        {
            // Arrange
            // No token in headers

            // Act
            var result = await _authController.ValidateToken();

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
            var unauthorizedResult = result as UnauthorizedObjectResult;
            unauthorizedResult.Value.Should().BeEquivalentTo(new { error = "No token provided", code = "TOKEN_ERROR" });
        }

        [Fact]
        public async Task ValidateToken_WithInvalidToken_ReturnsUnauthorized()
        {
            // Arrange
            var token = "invalid-jwt-token";
            _authController.HttpContext.Request.Headers["Authorization"] = $"Bearer {token}";

            _authBLMock.Setup(x => x.ValidateTokenAsync(It.IsAny<string>()))
                .ReturnsAsync(false);

            // Act
            var result = await _authController.ValidateToken();

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
            var unauthorizedResult = result as UnauthorizedObjectResult;
            unauthorizedResult.Value.Should().BeEquivalentTo(new { valid = false, code = "INVALID_TOKEN" });
        }
    }

    // Helper class for creating request cookies collection
    internal class RequestCookieCollection : IRequestCookieCollection
    {
        private readonly Dictionary<string, string> _cookies;

        public RequestCookieCollection(Dictionary<string, string> cookies)
        {
            _cookies = cookies;
        }

        public string this[string key] => _cookies.TryGetValue(key, out var value) ? value : null;

        public int Count => _cookies.Count;

        public ICollection<string> Keys => _cookies.Keys;

        public bool ContainsKey(string key) => _cookies.ContainsKey(key);

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _cookies.GetEnumerator();

        public bool TryGetValue(string key, out string value) => _cookies.TryGetValue(key, out value);

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _cookies.GetEnumerator();
    }
}
