using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using Backend.Business;
using Backend.DataAccess;
using Backend.DTOs.Auth;
using Backend.Models.Sql;
using Backend.Utils;
using Backend.Enums;
using Backend.Exceptions;
using System;
using System.Threading.Tasks;

namespace Backend.Tests.UnitTests.Business
{
    public class AuthBLTests
    {
        private readonly Mock<IUserDAL> _userDALMock;
        private readonly Mock<IPasswordHasher> _passwordHasherMock;
        private readonly Mock<IJwtTokenHelper> _jwtTokenHelperMock;
        private readonly Mock<ILogger<AuthBL>> _loggerMock;
        private readonly AuthBL _authBL;

        public AuthBLTests()
        {
            _userDALMock = new Mock<IUserDAL>();
            _passwordHasherMock = new Mock<IPasswordHasher>();
            _jwtTokenHelperMock = new Mock<IJwtTokenHelper>();
            _loggerMock = new Mock<ILogger<AuthBL>>();

            _authBL = new AuthBL(
                _userDALMock.Object,
                _passwordHasherMock.Object,
                _jwtTokenHelperMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task RegisterAsync_WithValidData_ReturnsSuccessResponse()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "test@example.com",
                Username = "testuser",
                Password = "Password123!",
                ConfirmPassword = "Password123!"
            };

            var createdUser = new User
            {
                UserId = Guid.NewGuid(),
                Email = registerDto.Email.ToLower(),
                Username = registerDto.Username,
                PasswordHash = "hashedpassword",
                Role = UserRole.Learner,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _userDALMock.Setup(x => x.UserExistsByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync(false);
            _userDALMock.Setup(x => x.UserExistsByUsernameAsync(It.IsAny<string>()))
                .ReturnsAsync(false);
            _passwordHasherMock.Setup(x => x.HashPassword(It.IsAny<string>()))
                .Returns("hashedpassword");
            _userDALMock.Setup(x => x.CreateUserAsync(It.IsAny<User>()))
                .ReturnsAsync(createdUser);
            _jwtTokenHelperMock.Setup(x => x.GenerateToken(It.IsAny<User>()))
                .Returns("jwt-token");

            // Act
            var result = await _authBL.RegisterAsync(registerDto);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Token.Should().Be("jwt-token");
            result.Email.Should().Be(createdUser.Email);
            result.Username.Should().Be(createdUser.Username);
            result.Role.Should().Be(UserRole.Learner);
            result.Message.Should().Be("Registration successful");
        }

        [Fact]
        public async Task RegisterAsync_WithEmptyEmail_ThrowsValidationException()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "",
                Username = "testuser",
                Password = "Password123!",
                ConfirmPassword = "Password123!"
            };

            // Act
            var act = async () => await _authBL.RegisterAsync(registerDto);

            // Assert
            await act.Should().ThrowAsync<ValidationException>()
                .WithMessage("Email, username, and password are required");
        }

        [Fact]
        public async Task RegisterAsync_WithExistingEmail_ThrowsUserAlreadyExistsException()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "existing@example.com",
                Username = "testuser",
                Password = "Password123!",
                ConfirmPassword = "Password123!"
            };

            _userDALMock.Setup(x => x.UserExistsByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync(true);

            // Act
            var act = async () => await _authBL.RegisterAsync(registerDto);

            // Assert
            await act.Should().ThrowAsync<UserAlreadyExistsException>()
                .WithMessage("User with this email already exists");
        }

        [Fact]
        public async Task RegisterAsync_WithExistingUsername_ThrowsUserAlreadyExistsException()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "test@example.com",
                Username = "existinguser",
                Password = "Password123!",
                ConfirmPassword = "Password123!"
            };

            _userDALMock.Setup(x => x.UserExistsByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync(false);
            _userDALMock.Setup(x => x.UserExistsByUsernameAsync(It.IsAny<string>()))
                .ReturnsAsync(true);

            // Act
            var act = async () => await _authBL.RegisterAsync(registerDto);

            // Assert
            await act.Should().ThrowAsync<UserAlreadyExistsException>()
                .WithMessage("Username is already taken");
        }

        [Fact]
        public async Task RegisterAsync_WithWeakPassword_ThrowsPasswordPolicyException()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Email = "test@example.com",
                Username = "testuser",
                Password = "12345",
                ConfirmPassword = "12345"
            };

            _userDALMock.Setup(x => x.UserExistsByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync(false);
            _userDALMock.Setup(x => x.UserExistsByUsernameAsync(It.IsAny<string>()))
                .ReturnsAsync(false);

            // Act
            var act = async () => await _authBL.RegisterAsync(registerDto);

            // Assert
            await act.Should().ThrowAsync<PasswordPolicyException>()
                .WithMessage("Password must be at least 6 characters long");
        }

        [Fact]
        public async Task LoginAsync_WithValidCredentials_ReturnsSuccessResponse()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                EmailOrUsername = "test@example.com",
                Password = "Password123!"
            };

            var user = new User
            {
                UserId = Guid.NewGuid(),
                Email = "test@example.com",
                Username = "testuser",
                PasswordHash = "hashedpassword",
                Role = UserRole.Admin
            };

            _userDALMock.Setup(x => x.GetUserByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync(user);
            _passwordHasherMock.Setup(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(true);
            _userDALMock.Setup(x => x.UpdateLastLoginAsync(It.IsAny<Guid>()))
                .Returns(Task.CompletedTask);
            _jwtTokenHelperMock.Setup(x => x.GenerateToken(It.IsAny<User>()))
                .Returns("jwt-token");

            // Act
            var result = await _authBL.LoginAsync(loginDto);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Token.Should().Be("jwt-token");
            result.Email.Should().Be(user.Email);
            result.Username.Should().Be(user.Username);
            result.Role.Should().Be(UserRole.Admin);
            result.Message.Should().Be("Login successful");
        }

        [Fact]
        public async Task LoginAsync_WithUsername_ReturnsSuccessResponse()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                EmailOrUsername = "testuser",
                Password = "Password123!"
            };

            var user = new User
            {
                UserId = Guid.NewGuid(),
                Email = "test@example.com",
                Username = "testuser",
                PasswordHash = "hashedpassword",
                Role = UserRole.Learner
            };

            _userDALMock.Setup(x => x.GetUserByUsernameAsync(It.IsAny<string>()))
                .ReturnsAsync(user);
            _passwordHasherMock.Setup(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(true);
            _userDALMock.Setup(x => x.UpdateLastLoginAsync(It.IsAny<Guid>()))
                .Returns(Task.CompletedTask);
            _jwtTokenHelperMock.Setup(x => x.GenerateToken(It.IsAny<User>()))
                .Returns("jwt-token");

            // Act
            var result = await _authBL.LoginAsync(loginDto);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Token.Should().Be("jwt-token");
        }

        [Fact]
        public async Task LoginAsync_WithEmptyCredentials_ThrowsValidationException()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                EmailOrUsername = "",
                Password = "Password123!"
            };

            // Act
            var act = async () => await _authBL.LoginAsync(loginDto);

            // Assert
            await act.Should().ThrowAsync<ValidationException>()
                .WithMessage("Email/Username and password are required");
        }

        [Fact]
        public async Task LoginAsync_WithNonExistentUser_ThrowsAuthenticationFailedException()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                EmailOrUsername = "nonexistent@example.com",
                Password = "Password123!"
            };

            _userDALMock.Setup(x => x.GetUserByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync((User)null);

            // Act
            var act = async () => await _authBL.LoginAsync(loginDto);

            // Assert
            await act.Should().ThrowAsync<AuthenticationFailedException>()
                .WithMessage("Invalid credentials");
        }

        [Fact]
        public async Task LoginAsync_WithIncorrectPassword_ThrowsAuthenticationFailedException()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                EmailOrUsername = "test@example.com",
                Password = "WrongPassword!"
            };

            var user = new User
            {
                UserId = Guid.NewGuid(),
                Email = "test@example.com",
                Username = "testuser",
                PasswordHash = "hashedpassword",
                Role = UserRole.Learner
            };

            _userDALMock.Setup(x => x.GetUserByEmailAsync(It.IsAny<string>()))
                .ReturnsAsync(user);
            _passwordHasherMock.Setup(x => x.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(false);

            // Act
            var act = async () => await _authBL.LoginAsync(loginDto);

            // Assert
            await act.Should().ThrowAsync<AuthenticationFailedException>()
                .WithMessage("Invalid credentials");
        }

        [Fact]
        public async Task LogoutAsync_WithValidToken_ReturnsTrue()
        {
            // Arrange
            var token = "valid-jwt-token";

            // Act
            var result = await _authBL.LogoutAsync(token);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ValidateTokenAsync_WithValidToken_ReturnsTrue()
        {
            // Arrange
            var token = "valid-jwt-token";
            _jwtTokenHelperMock.Setup(x => x.ValidateToken(It.IsAny<string>()))
                .Returns(true);

            // Act
            var result = await _authBL.ValidateTokenAsync(token);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ValidateTokenAsync_WithInvalidToken_ReturnsFalse()
        {
            // Arrange
            var token = "invalid-jwt-token";
            _jwtTokenHelperMock.Setup(x => x.ValidateToken(It.IsAny<string>()))
                .Returns(false);

            // Act
            var result = await _authBL.ValidateTokenAsync(token);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ValidateTokenAsync_WhenExceptionOccurs_ReturnsFalse()
        {
            // Arrange
            var token = "valid-jwt-token";
            _jwtTokenHelperMock.Setup(x => x.ValidateToken(It.IsAny<string>()))
                .Throws(new Exception("Token validation error"));

            // Act
            var result = await _authBL.ValidateTokenAsync(token);

            // Assert
            result.Should().BeFalse();
        }
    }
}
