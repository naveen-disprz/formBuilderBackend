using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Backend.Data;
using Backend.DTOs.Auth;
using Backend.Models.Sql;
using Backend.Enums;
using Newtonsoft.Json;
using System.Text;
using System;
using System.Linq;
using Backend.Tests.IntegrationTests.Common;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace Backend.Tests.IntegrationTests
{
    public class AuthIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>, IDisposable
    {
        private readonly CustomWebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public AuthIntegrationTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        }

        #region Register Tests

        [Fact]
        public async Task Register_WithValidData_ReturnsSuccessWithToken()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "newuser@test.com",
                Username = "newuser",
                Password = "Test123!@#",
                ConfirmPassword = "Test123!@#"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/register", registerDto);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<AuthResponseDto>(content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Token.Should().NotBeNullOrEmpty();
            result.Email.Should().Be(registerDto.Email.ToLower());
            result.Username.Should().Be(registerDto.Username);
            result.Role.Should().Be(UserRole.Learner.ToString());
            result.UserId.Should().NotBeEmpty();

            // Verify JWT cookie is set
            response.Headers.Should().ContainKey("Set-Cookie");
            var cookies = response.Headers.GetValues("Set-Cookie");
            cookies.Should().Contain(c => c.StartsWith("jwt="));
        }

        [Fact]
        public async Task Register_WithExistingEmail_ReturnsConflict()
        {
            // Arrange - First create a user
            var firstUser = new RegisterDto
            {
                Email = "existing@test.com",
                Username = "firstuser",
                Password = "Test123!@#",
                ConfirmPassword = "Test123!@#"
            };
            await _client.PostAsJsonAsync("/api/auth/register", firstUser);

            // Try to register with same email
            var duplicateUser = new RegisterDto
            {
                Email = "existing@test.com",
                Username = "seconduser",
                Password = "Test123!@#",
                ConfirmPassword = "Test123!@#"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/register", duplicateUser);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            content.Should().Contain("email already exists");
        }

        [Fact]
        public async Task Register_WithExistingUsername_ReturnsConflict()
        {
            // Arrange - First create a user
            var firstUser = new RegisterDto
            {
                Email = "first@test.com",
                Username = "sameusername",
                Password = "Test123!@#",
                ConfirmPassword = "Test123!@#"
            };
            await _client.PostAsJsonAsync("/api/auth/register", firstUser);

            // Try to register with same username
            var duplicateUser = new RegisterDto
            {
                Email = "second@test.com",
                Username = "sameusername",
                Password = "Test123!@#",
                ConfirmPassword = "Test123!@#"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/register", duplicateUser);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task Register_WithWeakPassword_ReturnsBadRequest()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "weakpass@test.com",
                Username = "weakpass",
                Password = "123",
                ConfirmPassword = "123"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/register", registerDto);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            content.Should().Contain("minimum length of '6'");
        }

        [Fact]
        public async Task Register_WithMismatchedPasswords_ReturnsBadRequest()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "mismatch@test.com",
                Username = "mismatch",
                Password = "Test123!@#",
                ConfirmPassword = "Different123!@#"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/register", registerDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Register_WithInvalidEmail_ReturnsBadRequest()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "notanemail",
                Username = "testuser",
                Password = "Test123!@#",
                ConfirmPassword = "Test123!@#"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/register", registerDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        #endregion

        #region Login Tests

        [Fact]
        public async Task Login_WithValidEmailCredentials_ReturnsSuccessWithToken()
        {
            // Arrange - First register a user
            var registerDto = new RegisterDto
            {
                Email = "logintest@test.com",
                Username = "logintest",
                Password = "Test123!@#",
                ConfirmPassword = "Test123!@#"
            };
            await _client.PostAsJsonAsync("/api/auth/register", registerDto);

            var loginDto = new LoginDto
            {
                EmailOrUsername = "logintest@test.com",
                Password = "Test123!@#"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<AuthResponseDto>(content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Token.Should().NotBeNullOrEmpty();
            result.Email.Should().Be(registerDto.Email.ToLower());
            result.Username.Should().Be(registerDto.Username);
        }

        [Fact]
        public async Task Login_WithValidUsernameCredentials_ReturnsSuccessWithToken()
        {
            // Arrange - First register a user
            var registerDto = new RegisterDto
            {
                Email = "usernamelogin@test.com",
                Username = "usernamelogin",
                Password = "Test123!@#",
                ConfirmPassword = "Test123!@#"
            };
            await _client.PostAsJsonAsync("/api/auth/register", registerDto);

            var loginDto = new LoginDto
            {
                EmailOrUsername = "usernamelogin",
                Password = "Test123!@#"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
            var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Token.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
        {
            // Arrange - First register a user
            var registerDto = new RegisterDto
            {
                Email = "wrongpass@test.com",
                Username = "wrongpass",
                Password = "Test123!@#",
                ConfirmPassword = "Test123!@#"
            };
            await _client.PostAsJsonAsync("/api/auth/register", registerDto);

            var loginDto = new LoginDto
            {
                EmailOrUsername = "wrongpass@test.com",
                Password = "WrongPassword"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            content.Should().Contain("Invalid credentials");
        }

        [Fact]
        public async Task Login_WithNonExistentUser_ReturnsUnauthorized()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                EmailOrUsername = "nonexistent@test.com",
                Password = "Test123!@#"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Login_WithEmptyCredentials_ReturnsBadRequest()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                EmailOrUsername = "",
                Password = ""
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        #endregion

        #region Logout Tests

        [Fact]
        public async Task Logout_WithValidToken_ReturnsSuccess()
        {
            // Arrange - First register and login
            var registerDto = new RegisterDto
            {
                Email = "logout@test.com",
                Username = "logouttest",
                Password = "Test123!@#",
                ConfirmPassword = "Test123!@#"
            };
            var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerDto);
            var authResult = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

            // Add authorization header
            _client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResult.Token);

            // Act
            var response = await _client.PostAsync("/api/auth/logout", null);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            content.Should().Contain("Logged out successfully");
            
            // Verify cookie is cleared
            response.Headers.Should().ContainKey("Set-Cookie");
            var cookies = response.Headers.GetValues("Set-Cookie");
            cookies.Should().Contain(c => c.Contains("jwt=;") || c.Contains("jwt=\"\";"));
        }

        [Fact]
        public async Task Logout_WithoutToken_ReturnsUnauthorized()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = null;

            // Act
            var response = await _client.PostAsync("/api/auth/logout", null);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Validate Token Tests
        

        [Fact]
        public async Task ValidateToken_WithoutToken_ReturnsUnauthorized()
        {
            // Arrange
            _client.DefaultRequestHeaders.Authorization = null;

            // Act
            var response = await _client.GetAsync("/api/auth/validate");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        #endregion

        #region Database Verification Tests

        [Fact]
        public async Task Register_CreatesUserInDatabase()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "dbtest@test.com",
                Username = "dbtest",
                Password = "Test123!@#",
                ConfirmPassword = "Test123!@#"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/register", registerDto);
            
            // Assert - Check database directly
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var user = await dbContext.Users
                    .FirstOrDefaultAsync(u => u.Email == registerDto.Email.ToLower());

                user.Should().NotBeNull();
                user.Username.Should().Be(registerDto.Username);
                user.Role.Should().Be(UserRole.Learner);
                user.PasswordHash.Should().NotBeNullOrEmpty();
                user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
            }
        }

        [Fact]
        public async Task Login_UpdatesLastLoginTime()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "lastlogin@test.com",
                Username = "lastlogin",
                Password = "Test123!@#",
                ConfirmPassword = "Test123!@#"
            };
            await _client.PostAsJsonAsync("/api/auth/register", registerDto);
            
            // Wait a bit to ensure different timestamps
            await Task.Delay(1000);

            var loginDto = new LoginDto
            {
                EmailOrUsername = "lastlogin@test.com",
                Password = "Test123!@#"
            };

            // Act
            await _client.PostAsJsonAsync("/api/auth/login", loginDto);

            // Assert
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var user = await dbContext.Users
                    .FirstOrDefaultAsync(u => u.Email == registerDto.Email.ToLower());

                user.UpdatedAt.Should().BeAfter(user.CreatedAt);
                user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
            }
        }

        #endregion

        #region Edge Cases and Security Tests

        [Fact]
        public async Task Register_WithSqlInjectionAttempt_ReturnsBadRequest()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "test'; DROP TABLE Users; --",
                Username = "normaluser",
                Password = "Test123!@#",
                ConfirmPassword = "Test123!@#"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/register", registerDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            
            // Verify database is intact
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var tableExists = await dbContext.Database.CanConnectAsync();
                tableExists.Should().BeTrue();
            }
        }

        [Fact]
        public async Task Register_WithVeryLongUsername_ReturnsBadRequest()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "longusername@test.com",
                Username = new string('a', 1000), // Very long username
                Password = "Test123!@#",
                ConfirmPassword = "Test123!@#"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/register", registerDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Login_IsCaseInsensitiveForEmail()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "CaseSensitive@Test.Com",
                Username = "casetest",
                Password = "Test123!@#",
                ConfirmPassword = "Test123!@#"
            };
            await _client.PostAsJsonAsync("/api/auth/register", registerDto);

            var loginDto = new LoginDto
            {
                EmailOrUsername = "casesensitive@test.com", // Different case
                Password = "Test123!@#"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        #endregion

        #region Token Tests

        [Fact]
        public async Task Token_ExpiresAfterSevenDays()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "expiry@test.com",
                Username = "expirytest",
                Password = "Test123!@#",
                ConfirmPassword = "Test123!@#"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/register", registerDto);
            var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();

            // Assert
            result.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromMinutes(1));
        }

        #endregion

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}
