using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Backend.Data;
using Backend.DataAccess;
using Backend.Models.Sql;
using Backend.Enums;
using Backend.Exceptions;
using System;
using System.Threading.Tasks;

namespace Backend.Tests.UnitTests.DataAccess
{
    public class UserDALTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly UserDAL _userDAL;
        private readonly Mock<ILogger<UserDAL>> _loggerMock;

        public UserDALTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _loggerMock = new Mock<ILogger<UserDAL>>();
            _userDAL = new UserDAL(_context, _loggerMock.Object);
        }

        [Fact]
        public async Task CreateUserAsync_WithValidUser_CreatesAndReturnsUser()
        {
            // Arrange
            var user = new User
            {
                UserId = Guid.NewGuid(),
                Email = "test@example.com",
                Username = "testuser",
                PasswordHash = "hashedpassword",
                Role = UserRole.Learner
            };

            // Act
            var result = await _userDAL.CreateUserAsync(user);

            // Assert
            result.Should().NotBeNull();
            result.Email.Should().Be(user.Email);
            result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

            var savedUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == user.UserId);
            savedUser.Should().NotBeNull();
        }

        [Fact]
        public async Task GetUserByIdAsync_WithExistingUser_ReturnsUser()
        {
            // Arrange
            var user = new User
            {
                UserId = Guid.NewGuid(),
                Email = "test@example.com",
                Username = "testuser",
                PasswordHash = "hashedpassword",
                Role = UserRole.Admin,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userDAL.GetUserByIdAsync(user.UserId);

            // Assert
            result.Should().NotBeNull();
            result.UserId.Should().Be(user.UserId);
            result.Email.Should().Be(user.Email);
        }

        [Fact]
        public async Task GetUserByIdAsync_WithNonExistingUser_ReturnsNull()
        {
            // Arrange
            var nonExistingUserId = Guid.NewGuid();

            // Act
            var result = await _userDAL.GetUserByIdAsync(nonExistingUserId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetUserByEmailAsync_WithExistingEmail_ReturnsUser()
        {
            // Arrange
            var user = new User
            {
                UserId = Guid.NewGuid(),
                Email = "Test@Example.COM",
                Username = "testuser",
                PasswordHash = "hashedpassword",
                Role = UserRole.Learner,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userDAL.GetUserByEmailAsync("test@example.com");

            // Assert
            result.Should().NotBeNull();
            result.Email.Should().Be("Test@Example.COM");
        }

        [Fact]
        public async Task GetUserByEmailAsync_WithNonExistingEmail_ReturnsNull()
        {
            // Act
            var result = await _userDAL.GetUserByEmailAsync("nonexisting@example.com");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetUserByUsernameAsync_WithExistingUsername_ReturnsUser()
        {
            // Arrange
            var user = new User
            {
                UserId = Guid.NewGuid(),
                Email = "test@example.com",
                Username = "TestUser",
                PasswordHash = "hashedpassword",
                Role = UserRole.Learner,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userDAL.GetUserByUsernameAsync("testuser");

            // Assert
            result.Should().NotBeNull();
            result.Username.Should().Be("TestUser");
        }

        [Fact]
        public async Task GetUserByUsernameAsync_WithNonExistingUsername_ReturnsNull()
        {
            // Act
            var result = await _userDAL.GetUserByUsernameAsync("nonexistinguser");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task UserExistsByEmailAsync_WithExistingEmail_ReturnsTrue()
        {
            // Arrange
            var user = new User
            {
                UserId = Guid.NewGuid(),
                Email = "test@example.com",
                Username = "testuser",
                PasswordHash = "hashedpassword",
                Role = UserRole.Learner,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userDAL.UserExistsByEmailAsync("Test@Example.Com");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task UserExistsByEmailAsync_WithNonExistingEmail_ReturnsFalse()
        {
            // Act
            var result = await _userDAL.UserExistsByEmailAsync("nonexisting@example.com");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task UserExistsByUsernameAsync_WithExistingUsername_ReturnsTrue()
        {
            // Arrange
            var user = new User
            {
                UserId = Guid.NewGuid(),
                Email = "test@example.com",
                Username = "testuser",
                PasswordHash = "hashedpassword",
                Role = UserRole.Learner,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userDAL.UserExistsByUsernameAsync("TestUser");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task UserExistsByUsernameAsync_WithNonExistingUsername_ReturnsFalse()
        {
            // Act
            var result = await _userDAL.UserExistsByUsernameAsync("nonexistinguser");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateUserAsync_WithValidUser_UpdatesAndReturnsUser()
        {
            // Arrange
            var user = new User
            {
                UserId = Guid.NewGuid(),
                Email = "test@example.com",
                Username = "testuser",
                PasswordHash = "hashedpassword",
                Role = UserRole.Learner,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            // Modify user
            user.Email = "updated@example.com";

            // Act
            var result = await _userDAL.UpdateUserAsync(user);

            // Assert
            result.Should().NotBeNull();
            result.Email.Should().Be("updated@example.com");
            result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task UpdateLastLoginAsync_WithExistingUser_UpdatesTimestamp()
        {
            // Arrange
            var user = new User
            {
                UserId = Guid.NewGuid(),
                Email = "test@example.com",
                Username = "testuser",
                PasswordHash = "hashedpassword",
                Role = UserRole.Learner,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var oldUpdateTime = user.UpdatedAt;

            // Act
            await _userDAL.UpdateLastLoginAsync(user.UserId);

            // Assert
            var updatedUser = await _context.Users.FindAsync(user.UserId);
            updatedUser.UpdatedAt.Should().BeAfter((DateTime)oldUpdateTime);
            updatedUser.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task UpdateLastLoginAsync_WithNonExistingUser_DoesNotThrow()
        {
            // Arrange
            var nonExistingUserId = Guid.NewGuid();

            // Act
            var act = async () => await _userDAL.UpdateLastLoginAsync(nonExistingUserId);

            // Assert
            await act.Should().NotThrowAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
