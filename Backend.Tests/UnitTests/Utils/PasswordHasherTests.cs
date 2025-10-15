using Xunit;
using FluentAssertions;
using Backend.Utils;

namespace Backend.Tests.UnitTests.Utils
{
    public class PasswordHasherTests
    {
        private readonly PasswordHasher _passwordHasher;

        public PasswordHasherTests()
        {
            _passwordHasher = new PasswordHasher();
        }

        [Fact]
        public void HashPassword_WithValidPassword_ReturnsHashedPassword()
        {
            // Arrange
            var password = "TestPassword123!";

            // Act
            var hashedPassword = _passwordHasher.HashPassword(password);

            // Assert
            hashedPassword.Should().NotBeNullOrEmpty();
            hashedPassword.Should().NotBe(password);
            hashedPassword.Length.Should().BeGreaterThan(50);
        }

        [Fact]
        public void HashPassword_SamePasswordTwice_ReturnsDifferentHashes()
        {
            // Arrange
            var password = "TestPassword123!";

            // Act
            var hash1 = _passwordHasher.HashPassword(password);
            var hash2 = _passwordHasher.HashPassword(password);

            // Assert
            hash1.Should().NotBe(hash2);
        }

        [Fact]
        public void VerifyPassword_WithCorrectPassword_ReturnsTrue()
        {
            // Arrange
            var password = "TestPassword123!";
            var hashedPassword = _passwordHasher.HashPassword(password);

            // Act
            var result = _passwordHasher.VerifyPassword(password, hashedPassword);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void VerifyPassword_WithIncorrectPassword_ReturnsFalse()
        {
            // Arrange
            var password = "TestPassword123!";
            var wrongPassword = "WrongPassword123!";
            var hashedPassword = _passwordHasher.HashPassword(password);

            // Act
            var result = _passwordHasher.VerifyPassword(wrongPassword, hashedPassword);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void VerifyPassword_WithEmptyPassword_ReturnsFalse()
        {
            // Arrange
            var password = "TestPassword123!";
            var hashedPassword = _passwordHasher.HashPassword(password);

            // Act
            var result = _passwordHasher.VerifyPassword("", hashedPassword);

            // Assert
            result.Should().BeFalse();
        }
    }
}
