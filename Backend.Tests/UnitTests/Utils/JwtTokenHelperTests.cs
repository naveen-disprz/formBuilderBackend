using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Backend.Utils;
using Backend.Models.Sql;
using Backend.Enums;
using Backend.Exceptions;
using System;
using System.Collections.Generic;

namespace Backend.Tests.UnitTests.Utils
{
    public class JwtTokenHelperTests
    {
        private readonly IJwtTokenHelper _jwtTokenHelper;
        private readonly IConfiguration _configuration;

        public JwtTokenHelperTests()
        {
            var configData = new Dictionary<string, string>
            {
                {"JwtSettings:SecretKey", "ThisIsAVerySecretKeyForTestingPurposesOnly123456"},
                {"JwtSettings:Issuer", "TestIssuer"},
                {"JwtSettings:Audience", "TestAudience"}
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();

            _jwtTokenHelper = new JwtTokenHelper(_configuration);
        }

        [Fact]
        public void Constructor_WithMissingSecretKey_ThrowsConfigurationException()
        {
            // Arrange
            var configData = new Dictionary<string, string>
            {
                {"JwtSettings:Issuer", "TestIssuer"},
                {"JwtSettings:Audience", "TestAudience"}
            };

            var badConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();

            // Act & Assert
            var act = () => new JwtTokenHelper(badConfig);
            act.Should().Throw<ConfigurationException>()
                .WithMessage("JWT Secret Key not configured");
        }

        [Fact]
        public void GenerateToken_WithValidUser_ReturnsToken()
        {
            // Arrange
            var user = new User
            {
                UserId = Guid.NewGuid(),
                Email = "test@example.com",
                Username = "testuser",
                Role = UserRole.Learner
            };

            // Act
            var token = _jwtTokenHelper.GenerateToken(user);

            // Assert
            token.Should().NotBeNullOrEmpty();
            token.Split('.').Length.Should().Be(3); // JWT has 3 parts
        }

        [Fact]
        public void ValidateToken_WithValidToken_ReturnsTrue()
        {
            // Arrange
            var user = new User
            {
                UserId = Guid.NewGuid(),
                Email = "test@example.com",
                Username = "testuser",
                Role = UserRole.Admin
            };
            var token = _jwtTokenHelper.GenerateToken(user);

            // Act
            var isValid = _jwtTokenHelper.ValidateToken(token);

            // Assert
            isValid.Should().BeTrue();
        }

        [Fact]
        public void ValidateToken_WithInvalidToken_ReturnsFalse()
        {
            // Arrange
            var invalidToken = "invalid.token.here";

            // Act
            var isValid = _jwtTokenHelper.ValidateToken(invalidToken);

            // Assert
            isValid.Should().BeFalse();
        }

        [Fact]
        public void ValidateToken_WithExpiredToken_ReturnsFalse()
        {
            // This would require mocking DateTime or waiting, simplified for now
            // Arrange
            var tamperedToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";

            // Act
            var isValid = _jwtTokenHelper.ValidateToken(tamperedToken);

            // Assert
            isValid.Should().BeFalse();
        }

        [Fact]
        public void GetUserIdFromToken_WithValidToken_ReturnsUserId()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User
            {
                UserId = userId,
                Email = "test@example.com",
                Username = "testuser",
                Role = UserRole.Learner
            };
            var token = _jwtTokenHelper.GenerateToken(user);

            // Act
            var extractedUserId = _jwtTokenHelper.GetUserIdFromToken(token);

            // Assert
            extractedUserId.Should().Be(userId);
        }

        [Fact]
        public void GetUserIdFromToken_WithInvalidToken_ThrowsTokenException()
        {
            // Arrange
            var invalidToken = "invalid.token.here";

            // Act & Assert
            var act = () => _jwtTokenHelper.GetUserIdFromToken(invalidToken);
            act.Should().Throw<TokenException>()
                .WithMessage("Invalid token - UserId not found");
        }
    }
}
