using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Backend.DataAccess;
using Backend.Data;
using Backend.Models.Sql;
using Backend.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend.Enums;

namespace Backend.Tests.UnitTests.DataAccess
{
    public class ResponseDALTests
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILogger<ResponseDAL>> _loggerMock;
        private readonly ResponseDAL _responseDAL;

        public ResponseDALTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .EnableSensitiveDataLogging()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _loggerMock = new Mock<ILogger<ResponseDAL>>();
            _responseDAL = new ResponseDAL(_context, _loggerMock.Object);
        }

        [Fact]
        public async Task CreateResponseAsync_WithValidResponse_ReturnsCreatedResponse()
        {
            // Arrange
            var response = new Response
            {
                FormId = "507f1f77bcf86cd799439011",
                SubmittedBy = Guid.NewGuid(),
                ClientIp = "192.168.1.1",
                UserAgent = "Mozilla/5.0"
            };

            // Act
            var result = await _responseDAL.CreateResponseAsync(response);

            // Assert
            result.Should().NotBeNull();
            result.ResponseId.Should().NotBeEmpty();
            result.FormId.Should().Be(response.FormId);
            result.SubmittedBy.Should().Be(response.SubmittedBy);
            result.SubmittedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

            // Verify it was saved to database
            var savedResponse = await _context.Responses.FindAsync(result.ResponseId);
            savedResponse.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateAnswerAsync_WithValidAnswer_ReturnsCreatedAnswer()
        {
            // Arrange
            var answer = new Answer
            {
                ResponseId = Guid.NewGuid(),
                QuestionId = "507f1f77bcf86cd799439011",
                AnswerType = Backend.Enums.QuestionType.ShortText,
                AnswerValue = "Test Answer"
            };

            // Act
            var result = await _responseDAL.CreateAnswerAsync(answer);

            // Assert
            result.Should().NotBeNull();
            result.AnswerId.Should().NotBeEmpty();
            result.QuestionId.Should().Be(answer.QuestionId);
            result.AnswerValue.Should().Be("Test Answer");
            result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

            // Verify it was saved to database
            var savedAnswer = await _context.Answers.FindAsync(result.AnswerId);
            savedAnswer.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateFileUploadAsync_WithValidFile_ReturnsCreatedFile()
        {
            // Arrange
            var file = new FileUpload
            {
                AnswerId = Guid.NewGuid(),
                FileName = "test.pdf",
                MimeType = "application/pdf",
                FileSizeBytes = 1024,
                FileContent = "base64content"
            };

            // Act
            var result = await _responseDAL.CreateFileUploadAsync(file);

            // Assert
            result.Should().NotBeNull();
            result.FileId.Should().NotBeEmpty();
            result.FileName.Should().Be("test.pdf");
            result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

            // Verify it was saved to database
            var savedFile = await _context.Files.FindAsync(result.FileId);
            savedFile.Should().NotBeNull();
        }

        [Fact]
        public async Task GetResponseByIdAsync_WithExistingResponse_ReturnsResponse()
        {
            // Arrange
            var user = new User
            {
                UserId = Guid.NewGuid(),
                Email = "test@example.com",
                Username = "testuser",
                PasswordHash = "hash",
                Role = Backend.Enums.UserRole.Learner
            };
            await _context.Users.AddAsync(user);

            var response = new Response
            {
                ResponseId = Guid.NewGuid(),
                FormId = "507f1f77bcf86cd799439011",
                SubmittedBy = user.UserId,
                SubmittedAt = DateTime.UtcNow
            };
            await _context.Responses.AddAsync(response);
            await _context.SaveChangesAsync();

            // Act
            var result = await _responseDAL.GetResponseByIdAsync(response.ResponseId);

            // Assert
            result.Should().NotBeNull();
            result!.ResponseId.Should().Be(response.ResponseId);
            result.FormId.Should().Be(response.FormId);
            result.User.Should().NotBeNull();
            result.User!.Username.Should().Be("testuser");
        }

        [Fact]
        public async Task GetResponseByIdAsync_WithNonExistentResponse_ReturnsNull()
        {
            // Arrange
            var responseId = Guid.NewGuid();

            // Act
            var result = await _responseDAL.GetResponseByIdAsync(responseId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetResponsesByFormIdAsync_WithResponses_ReturnsFilteredResponses()
        {
            // Arrange
            var formId = "507f1f77bcf86cd799439011";
            var user = new User
            {
                UserId = Guid.NewGuid(),
                Email = "test@example.com",
                Username = "testuser",
                PasswordHash = "hash",
                Role = Backend.Enums.UserRole.Learner
            };
            await _context.Users.AddAsync(user);

            for (int i = 0; i < 5; i++)
            {
                await _context.Responses.AddAsync(new Response
                {
                    ResponseId = Guid.NewGuid(),
                    FormId = formId,
                    SubmittedBy = user.UserId,
                    SubmittedAt = DateTime.UtcNow.AddMinutes(-i)
                });
            }

            // Add responses for different form
            await _context.Responses.AddAsync(new Response
            {
                ResponseId = Guid.NewGuid(),
                FormId = "different_form_id",
                SubmittedBy = user.UserId
            });

            await _context.SaveChangesAsync();

            // Act
            var result = await _responseDAL.GetResponsesByFormIdAsync(formId, 1, 3);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            result.Should().OnlyContain(r => r.FormId == formId);
            result.Should().BeInDescendingOrder(r => r.SubmittedAt);
        }

        [Fact]
        public async Task GetResponsesByUserIdAsync_WithResponses_ReturnsUserResponses()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();

            for (int i = 0; i < 3; i++)
            {
                await _context.Responses.AddAsync(new Response
                {
                    ResponseId = Guid.NewGuid(),
                    FormId = $"form_{i}",
                    SubmittedBy = userId,
                    SubmittedAt = DateTime.UtcNow.AddMinutes(-i)
                });
            }

            await _context.Responses.AddAsync(new Response
            {
                ResponseId = Guid.NewGuid(),
                FormId = "other_form",
                SubmittedBy = otherUserId
            });

            await _context.SaveChangesAsync();

            // Act
            var result = await _responseDAL.GetResponsesByUserIdAsync(userId, 1, 10);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            result.Should().OnlyContain(r => r.SubmittedBy == userId);
        }

        [Fact]
        public async Task GetAnswersByResponseIdAsync_WithAnswers_ReturnsAnswersWithFiles()
        {
            // Arrange
            var responseId = Guid.NewGuid();
            
            var answer1 = new Answer
            {
                AnswerId = Guid.NewGuid(),
                ResponseId = responseId,
                QuestionId = "q1",
                AnswerType = QuestionType.ShortText,
                AnswerValue = "Answer 1"
            };

            var answer2 = new Answer
            {
                AnswerId = Guid.NewGuid(),
                ResponseId = responseId,
                QuestionId = "q2",
                AnswerType = QuestionType.File,
                AnswerValue = null
            };

            await _context.Answers.AddRangeAsync(answer1, answer2);

            var file = new FileUpload
            {
                FileId = Guid.NewGuid(),
                AnswerId = answer2.AnswerId,
                FileName = "test.pdf",
                MimeType = "application/pdf",
                FileContent = "NSphQEUqMnYmPT5yK1U/SCIwWlc3\ndnQ+XV52Jiw=\nQQ==\nP0E9KGNE\nOiRiWjQ/RkY=",
                FileSizeBytes = 1024
            };

            await _context.Files.AddAsync(file);
            await _context.SaveChangesAsync();

            // Act
            var result = await _responseDAL.GetAnswersByResponseIdAsync(responseId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            
            var fileAnswer = result.FirstOrDefault(a => a.AnswerType == Backend.Enums.QuestionType.File);
            fileAnswer.Should().NotBeNull();
            fileAnswer!.Files.Should().HaveCount(1);
            fileAnswer.Files.First().FileName.Should().Be("test.pdf");
        }

        [Fact]
        public async Task GetFileByIdAsync_WithExistingFile_ReturnsFile()
        {
            // Arrange
            var file = new FileUpload
            {
                FileId = Guid.NewGuid(),
                AnswerId = Guid.NewGuid(),
                FileName = "document.pdf",
                MimeType = "application/pdf",
                FileSizeBytes = 2048,
                FileContent = "base64content"
            };

            await _context.Files.AddAsync(file);
            await _context.SaveChangesAsync();

            // Act
            var result = await _responseDAL.GetFileByIdAsync(file.FileId);

            // Assert
            result.Should().NotBeNull();
            result!.FileId.Should().Be(file.FileId);
            result.FileName.Should().Be("document.pdf");
            result.FileContent.Should().Be("base64content");
        }

        [Fact]
        public async Task GetFileByIdAsync_WithNonExistentFile_ReturnsNull()
        {
            // Arrange
            var fileId = Guid.NewGuid();

            // Act
            var result = await _responseDAL.GetFileByIdAsync(fileId);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetResponseCountByFormIdAsync_ReturnsCorrectCount()
        {
            // Arrange
            var formId = "507f1f77bcf86cd799439011";

            for (int i = 0; i < 5; i++)
            {
                await _context.Responses.AddAsync(new Response
                {
                    ResponseId = Guid.NewGuid(),
                    FormId = formId,
                    SubmittedBy = Guid.NewGuid()
                });
            }

            await _context.SaveChangesAsync();

            // Act
            var result = await _responseDAL.GetResponseCountByFormIdAsync(formId);

            // Assert
            result.Should().Be(5);
        }

        [Fact]
        public async Task UserHasRespondedToFormAsync_WhenUserHasResponded_ReturnsTrue()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var formId = "507f1f77bcf86cd799439011";

            await _context.Responses.AddAsync(new Response
            {
                ResponseId = Guid.NewGuid(),
                FormId = formId,
                SubmittedBy = userId
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _responseDAL.UserHasRespondedToFormAsync(userId, formId);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task UserHasRespondedToFormAsync_WhenUserHasNotResponded_ReturnsFalse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var formId = "507f1f77bcf86cd799439011";

            // Act
            var result = await _responseDAL.UserHasRespondedToFormAsync(userId, formId);

            // Assert
            result.Should().BeFalse();
        }
        
        [Fact]
        public async Task CreateResponseAsync_WhenDatabaseError_ThrowsResponseDataAccessException()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            var responseDAL = new ResponseDAL(context, _loggerMock.Object);

            var response = new Response
            {
                FormId = "507f1f77bcf86cd799439011",
                SubmittedBy = Guid.NewGuid(),
                ClientIp = "192.168.1.1",
                UserAgent = "Mozilla/5.0"
            };

            // Force an error by disposing the context
            await context.DisposeAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ResponseDataAccessException>(
                () => responseDAL.CreateResponseAsync(response));

            exception.Message.Should().Be("Database error while creating response");
            exception.InnerException.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateAnswerAsync_WhenDatabaseError_ThrowsResponseDataAccessException()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            var responseDAL = new ResponseDAL(context, _loggerMock.Object);

            var answer = new Answer
            {
                ResponseId = Guid.NewGuid(),
                QuestionId = "507f1f77bcf86cd799439011",
                AnswerType = QuestionType.ShortText,
                AnswerValue = "Test Answer"
            };

            // Force an error by disposing the context
            await context.DisposeAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ResponseDataAccessException>(
                () => responseDAL.CreateAnswerAsync(answer));

            exception.Message.Should().Be("Database error while creating answer");
            exception.InnerException.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateFileUploadAsync_WhenDatabaseError_ThrowsResponseDataAccessException()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            var responseDAL = new ResponseDAL(context, _loggerMock.Object);

            var file = new FileUpload
            {
                AnswerId = Guid.NewGuid(),
                FileName = "test.pdf",
                MimeType = "application/pdf",
                FileSizeBytes = 1024,
                FileContent = "base64content"
            };

            // Force an error by disposing the context
            await context.DisposeAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ResponseDataAccessException>(
                () => responseDAL.CreateFileUploadAsync(file));

            exception.Message.Should().Be("Database error while creating file upload");
            exception.InnerException.Should().NotBeNull();
        }

        [Fact]
        public async Task GetResponseByIdAsync_WhenDatabaseError_ThrowsResponseDataAccessException()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            var responseDAL = new ResponseDAL(context, _loggerMock.Object);

            var responseId = Guid.NewGuid();

            // Force an error by disposing the context
            await context.DisposeAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ResponseDataAccessException>(
                () => responseDAL.GetResponseByIdAsync(responseId));

            exception.Message.Should().Be($"Database error while retrieving response: {responseId}");
            exception.InnerException.Should().NotBeNull();
        }

        [Fact]
        public async Task GetResponsesByFormIdAsync_WhenDatabaseError_ThrowsResponseDataAccessException()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            var responseDAL = new ResponseDAL(context, _loggerMock.Object);

            var formId = "507f1f77bcf86cd799439011";

            // Force an error by disposing the context
            await context.DisposeAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ResponseDataAccessException>(
                () => responseDAL.GetResponsesByFormIdAsync(formId, 1, 10));

            exception.Message.Should().Be($"Database error while retrieving responses for form: {formId}");
            exception.InnerException.Should().NotBeNull();
        }

        [Fact]
        public async Task GetResponsesByUserIdAsync_WhenDatabaseError_ThrowsResponseDataAccessException()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            var responseDAL = new ResponseDAL(context, _loggerMock.Object);

            var userId = Guid.NewGuid();

            // Force an error by disposing the context
            await context.DisposeAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ResponseDataAccessException>(
                () => responseDAL.GetResponsesByUserIdAsync(userId, 1, 10));

            exception.Message.Should().Be($"Database error while retrieving responses for user: {userId}");
            exception.InnerException.Should().NotBeNull();
        }

        [Fact]
        public async Task GetAnswersByResponseIdAsync_WhenDatabaseError_ThrowsResponseDataAccessException()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            var responseDAL = new ResponseDAL(context, _loggerMock.Object);

            var responseId = Guid.NewGuid();

            // Force an error by disposing the context
            await context.DisposeAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ResponseDataAccessException>(
                () => responseDAL.GetAnswersByResponseIdAsync(responseId));

            exception.Message.Should().Be($"Database error while retrieving answers for response: {responseId}");
            exception.InnerException.Should().NotBeNull();
        }

        [Fact]
        public async Task GetFileByIdAsync_WhenDatabaseError_ThrowsResponseDataAccessException()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            var responseDAL = new ResponseDAL(context, _loggerMock.Object);

            var fileId = Guid.NewGuid();

            // Force an error by disposing the context
            await context.DisposeAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ResponseDataAccessException>(
                () => responseDAL.GetFileByIdAsync(fileId));

            exception.Message.Should().Be($"Database error while retrieving file: {fileId}");
            exception.InnerException.Should().NotBeNull();
        }

        [Fact]
        public async Task GetResponseCountByFormIdAsync_WhenDatabaseError_ThrowsResponseDataAccessException()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            var responseDAL = new ResponseDAL(context, _loggerMock.Object);

            var formId = "507f1f77bcf86cd799439011";

            // Force an error by disposing the context
            await context.DisposeAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ResponseDataAccessException>(
                () => responseDAL.GetResponseCountByFormIdAsync(formId));

            exception.Message.Should().Be($"Database error while counting responses for form: {formId}");
            exception.InnerException.Should().NotBeNull();
        }

        [Fact]
        public async Task UserHasRespondedToFormAsync_WhenDatabaseError_ThrowsResponseDataAccessException()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new ApplicationDbContext(options);
            var responseDAL = new ResponseDAL(context, _loggerMock.Object);

            var userId = Guid.NewGuid();
            var formId = "507f1f77bcf86cd799439011";

            // Force an error by disposing the context
            await context.DisposeAsync();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ResponseDataAccessException>(
                () => responseDAL.UserHasRespondedToFormAsync(userId, formId));

            exception.Message.Should().Be($"Database error while checking user response for form: {formId}");
            exception.InnerException.Should().NotBeNull();
        }


        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
