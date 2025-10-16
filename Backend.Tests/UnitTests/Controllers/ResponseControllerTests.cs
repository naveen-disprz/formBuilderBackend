using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Backend.Controllers;
using Backend.Business;
using Backend.DTOs.Response;
using Backend.Exceptions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Net;
using Backend.Models.Sql;
using FileNotFoundException = Backend.Exceptions.FileNotFoundException;

namespace Backend.Tests.UnitTests.Controllers
{
    public class ResponseControllerTests
    {
        private readonly Mock<IResponseBL> _responseBLMock;
        private readonly Mock<ILogger<ResponseController>> _loggerMock;
        private readonly ResponseController _controller;
        private readonly Guid _testUserId = Guid.NewGuid();
        private readonly string _testUserRole = "Learner";

        public ResponseControllerTests()
        {
            _responseBLMock = new Mock<IResponseBL>();
            _loggerMock = new Mock<ILogger<ResponseController>>();
            
            _controller = new ResponseController(_responseBLMock.Object, _loggerMock.Object);
            
            // Setup controller context with user claims
            SetupControllerContext();
        }

        private void SetupControllerContext()
        {
            var claims = new List<Claim>
            {
                new Claim("UserId", _testUserId.ToString()),
                new Claim("Role", _testUserRole)
            };
            
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            
            var httpContext = new DefaultHttpContext
            {
                User = claimsPrincipal,
                Connection =
                {
                    RemoteIpAddress = IPAddress.Parse("192.168.1.1")
                }
            };
            
            httpContext.Request.Headers["User-Agent"] = "Mozilla/5.0 TestAgent";
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        #region SubmitResponse Tests

        [Fact]
        public async Task SubmitResponse_WithValidData_ReturnsCreatedResult()
        {
            // Arrange
            var formId = "507f1f77bcf86cd799439011";
            var submitDto = new SubmitResponseDto
            {
                Answers = new List<AnswerDto>
                {
                    new AnswerDto
                    {
                        QuestionId = "q1",
                        Value = "Answer 1"
                    }
                }
            };

            var expectedResult = new SubmitResponseResultDto
            {
                Success = true,
                ResponseId = Guid.NewGuid(),
                SubmittedAt = DateTime.UtcNow,
                Message = "Response submitted successfully"
            };

            _responseBLMock.Setup(x => x.SubmitResponseAsync(
                It.IsAny<string>(), 
                It.IsAny<SubmitResponseDto>(),
                It.IsAny<Guid>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.SubmitResponse(formId, submitDto);

            // Assert
            var createdResult = result.Should().BeOfType<CreatedResult>().Subject;
            createdResult.StatusCode.Should().Be(201);
            createdResult.Location.Should().Be($"/api/response/{expectedResult.ResponseId}");
            createdResult.Value.Should().BeEquivalentTo(expectedResult);
        }

        [Fact]
        public async Task SubmitResponse_WithInvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            var formId = "507f1f77bcf86cd799439011";
            var submitDto = new SubmitResponseDto(); 
            
            _controller.ModelState.AddModelError("Answers", "Answers are required");

            // Act
            var result = await _controller.SubmitResponse(formId, submitDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            _responseBLMock.Verify(x => x.SubmitResponseAsync(
                It.IsAny<string>(), 
                It.IsAny<SubmitResponseDto>(), 
                It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task SubmitResponse_WhenFormNotFound_ReturnsNotFound()
        {
            // Arrange
            var formId = "507f1f77bcf86cd799439011";
            var submitDto = new SubmitResponseDto
            {
                Answers = new List<AnswerDto> { new AnswerDto { QuestionId = "q1", Value = "test" } }
            };

            _responseBLMock.Setup(x => x.SubmitResponseAsync(
                It.IsAny<string>(), 
                It.IsAny<SubmitResponseDto>(), 
                It.IsAny<Guid>()))
                .ThrowsAsync(new FormNotFoundForResponseException(formId));

            // Act
            var result = await _controller.SubmitResponse(formId, submitDto);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.StatusCode.Should().Be(404);
            notFoundResult.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task SubmitResponse_WhenFormUnpublished_ReturnsBadRequest()
        {
            // Arrange
            var formId = "507f1f77bcf86cd799439011";
            var submitDto = new SubmitResponseDto
            {
                Answers = new List<AnswerDto> { new AnswerDto { QuestionId = "q1", Value = "test" } }
            };

            _responseBLMock.Setup(x => x.SubmitResponseAsync(
                It.IsAny<string>(), 
                It.IsAny<SubmitResponseDto>(), 
                It.IsAny<Guid>()))
                .ThrowsAsync(new UnpublishedFormException());

            // Act
            var result = await _controller.SubmitResponse(formId, submitDto);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.StatusCode.Should().Be(400);
            badRequestResult.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task SubmitResponse_WhenDuplicateResponse_ReturnsConflict()
        {
            // Arrange
            var formId = "507f1f77bcf86cd799439011";
            var submitDto = new SubmitResponseDto
            {
                Answers = new List<AnswerDto> { new AnswerDto { QuestionId = "q1", Value = "test" } }
            };

            _responseBLMock.Setup(x => x.SubmitResponseAsync(
                It.IsAny<string>(), 
                It.IsAny<SubmitResponseDto>(), 
                It.IsAny<Guid>()))
                .ThrowsAsync(new DuplicateResponseException());

            // Act
            var result = await _controller.SubmitResponse(formId, submitDto);

            // Assert
            var conflictResult = result.Should().BeOfType<ConflictObjectResult>().Subject;
            conflictResult.StatusCode.Should().Be(409);
            conflictResult.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task SubmitResponse_WhenRequiredQuestionMissing_ReturnsBadRequest()
        {
            // Arrange
            var formId = "507f1f77bcf86cd799439011";
            var submitDto = new SubmitResponseDto
            {
                Answers = new List<AnswerDto>()
            };

            _responseBLMock.Setup(x => x.SubmitResponseAsync(
                It.IsAny<string>(), 
                It.IsAny<SubmitResponseDto>(), 
                It.IsAny<Guid>()))
                .ThrowsAsync(new RequiredQuestionException("Question 1"));

            // Act
            var result = await _controller.SubmitResponse(formId, submitDto);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task SubmitResponse_WhenValidationFails_ReturnsBadRequest()
        {
            // Arrange
            var formId = "507f1f77bcf86cd799439011";
            var submitDto = new SubmitResponseDto
            {
                Answers = new List<AnswerDto> { new AnswerDto { QuestionId = "q1", Value = "test" } }
            };

            _responseBLMock.Setup(x => x.SubmitResponseAsync(
                It.IsAny<string>(), 
                It.IsAny<SubmitResponseDto>(), 
                It.IsAny<Guid>()))
                .ThrowsAsync(new ResponseValidationException("Invalid response data"));

            // Act
            var result = await _controller.SubmitResponse(formId, submitDto);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task SubmitResponse_WhenDataAccessError_ReturnsInternalServerError()
        {
            // Arrange
            var formId = "507f1f77bcf86cd799439011";
            var submitDto = new SubmitResponseDto
            {
                Answers = new List<AnswerDto> { new AnswerDto { QuestionId = "q1", Value = "test" } }
            };

            _responseBLMock.Setup(x => x.SubmitResponseAsync(
                It.IsAny<string>(), 
                It.IsAny<SubmitResponseDto>(), 
                It.IsAny<Guid>()))
                .ThrowsAsync(new ResponseDataAccessException("Database error", new Exception()));

            // Act
            var result = await _controller.SubmitResponse(formId, submitDto);

            // Assert
            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(500);
            objectResult.Value.Should().NotBeNull();
        }

        #endregion

        #region GetFormResponses Tests

        [Fact]
        public async Task GetFormResponses_WithValidRequest_ReturnsOk()
        {
            // Arrange
            var formId = "507f1f77bcf86cd799439011";
            var expectedResult = new ResponseListDto
            {
                Responses = new List<ResponseItemDto>
                {
                    new ResponseItemDto
                    {
                        ResponseId = Guid.NewGuid(),
                        FormId = formId,
                        SubmittedBy = Guid.NewGuid(),
                        SubmitterUsername = "user1",
                        SubmittedAt = DateTime.UtcNow,
                        AnswerCount = 3
                    }
                },
                CurrentPage = 1,
                PageSize = 10,
                TotalCount = 1,
                TotalPages = 1
            };

            _responseBLMock.Setup(x => x.GetFormResponsesAsync(
                It.IsAny<string>(), 
                It.IsAny<int>(), 
                It.IsAny<int>(), 
                It.IsAny<Guid>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetFormResponses(formId, 1, 10);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
            okResult.Value.Should().BeEquivalentTo(expectedResult);
        }

        [Fact]
        public async Task GetFormResponses_WhenFormNotFound_ReturnsNotFound()
        {
            // Arrange
            var formId = "507f1f77bcf86cd799439011";

            _responseBLMock.Setup(x => x.GetFormResponsesAsync(
                It.IsAny<string>(), 
                It.IsAny<int>(), 
                It.IsAny<int>(), 
                It.IsAny<Guid>()))
                .ThrowsAsync(new FormNotFoundForResponseException(formId));

            // Act
            var result = await _controller.GetFormResponses(formId);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task GetFormResponses_WhenUnauthorized_ReturnsForbid()
        {
            // Arrange
            var formId = "507f1f77bcf86cd799439011";

            _responseBLMock.Setup(x => x.GetFormResponsesAsync(
                It.IsAny<string>(), 
                It.IsAny<int>(), 
                It.IsAny<int>(), 
                It.IsAny<Guid>()))
                .ThrowsAsync(new ResponseUnauthorizedException("You can only view responses for your own forms"));

            // Act
            var result = await _controller.GetFormResponses(formId);

            // Assert
            var forbidResult = result.Should().BeOfType<ForbidResult>().Subject;
        }

        [Fact]
        public async Task GetFormResponses_WhenDataAccessError_ReturnsInternalServerError()
        {
            // Arrange
            var formId = "507f1f77bcf86cd799439011";

            _responseBLMock.Setup(x => x.GetFormResponsesAsync(
                It.IsAny<string>(), 
                It.IsAny<int>(), 
                It.IsAny<int>(), 
                It.IsAny<Guid>()))
                .ThrowsAsync(new ResponseDataAccessException("Database error", new Exception()));

            // Act
            var result = await _controller.GetFormResponses(formId);

            // Assert
            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(500);
        }

        #endregion

        #region GetResponseById Tests

        [Fact]
        public async Task GetResponseById_WithValidId_ReturnsOk()
        {
            // Arrange
            var responseId = Guid.NewGuid();
            var expectedResult = new ResponseDetailDto
            {
                ResponseId = responseId,
                FormId = "507f1f77bcf86cd799439011",
                FormTitle = "Test Form",
                SubmittedBy = _testUserId,
                SubmitterUsername = "testuser",
                SubmittedAt = DateTime.UtcNow,
                ClientIp = "192.168.1.1",
                UserAgent = "Mozilla/5.0",
                Answers = new List<AnswerDetailDto>
                {
                    new AnswerDetailDto
                    {
                        AnswerId = Guid.NewGuid(),
                        QuestionId = "q1",
                        QuestionLabel = "Question 1",
                        QuestionType = "ShortText",
                        Value = "Answer 1"
                    }
                }
            };

            _responseBLMock.Setup(x => x.GetResponseByIdAsync(
                It.IsAny<Guid>(), 
                It.IsAny<Guid>(), 
                It.IsAny<string>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetResponseById(responseId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
            okResult.Value.Should().BeEquivalentTo(expectedResult);
        }

        [Fact]
        public async Task GetResponseById_WhenResponseNotFound_ReturnsNotFound()
        {
            // Arrange
            var responseId = Guid.NewGuid();

            _responseBLMock.Setup(x => x.GetResponseByIdAsync(
                It.IsAny<Guid>(), 
                It.IsAny<Guid>(), 
                It.IsAny<string>()))
                .ThrowsAsync(new ResponseNotFoundException(responseId));

            // Act
            var result = await _controller.GetResponseById(responseId);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task GetResponseById_WhenUnauthorized_ReturnsForbid()
        {
            // Arrange
            var responseId = Guid.NewGuid();

            _responseBLMock.Setup(x => x.GetResponseByIdAsync(
                It.IsAny<Guid>(), 
                It.IsAny<Guid>(), 
                It.IsAny<string>()))
                .ThrowsAsync(new ResponseUnauthorizedException("You can only view your own responses"));

            // Act
            var result = await _controller.GetResponseById(responseId);

            // Assert
            var forbidResult = result.Should().BeOfType<ForbidResult>().Subject;
        }

        [Fact]
        public async Task GetResponseById_WhenDataAccessError_ReturnsInternalServerError()
        {
            // Arrange
            var responseId = Guid.NewGuid();

            _responseBLMock.Setup(x => x.GetResponseByIdAsync(
                It.IsAny<Guid>(), 
                It.IsAny<Guid>(), 
                It.IsAny<string>()))
                .ThrowsAsync(new ResponseDataAccessException("Database error", new Exception()));

            // Act
            var result = await _controller.GetResponseById(responseId);

            // Assert
            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(500);
        }

        #endregion

        #region GetFile Tests

        [Fact]
        public async Task GetFile_WithValidId_ReturnsFile()
        {
            // Arrange
            var fileId = Guid.NewGuid();
            var fileContent = new FileUpload
            {
                FileId = fileId,
                FileName = "document.pdf",
                MimeType = "application/pdf",
                FileContent = Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5 })
            };

            _responseBLMock.Setup(x => x.GetFileContentAsync(
                It.IsAny<Guid>(), 
                It.IsAny<Guid>(), 
                It.IsAny<string>()))
                .ReturnsAsync(fileContent);

            // Act
            var result = await _controller.GetFile(fileId, download: false);

            // Assert
            var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
            fileResult.ContentType.Should().Be("application/pdf");
            fileResult.FileContents.Should().BeEquivalentTo(new byte[] { 1, 2, 3, 4, 5 });
        }

        [Fact]
        public async Task GetFile_WithDownloadFlag_ReturnsFileWithFilename()
        {
            // Arrange
            var fileId = Guid.NewGuid();
            var fileContent = new FileUpload
            {
                FileId = fileId,
                FileName = "document.pdf",
                MimeType = "application/pdf",
                FileContent = Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5 })
            };

            _responseBLMock.Setup(x => x.GetFileContentAsync(
                It.IsAny<Guid>(), 
                It.IsAny<Guid>(), 
                It.IsAny<string>()))
                .ReturnsAsync(fileContent);

            // Act
            var result = await _controller.GetFile(fileId, download: true);

            // Assert
            var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
            fileResult.ContentType.Should().Be("application/pdf");
            fileResult.FileDownloadName.Should().Be("document.pdf");
            fileResult.FileContents.Should().BeEquivalentTo(new byte[] { 1, 2, 3, 4, 5 });
        }

        [Fact]
        public async Task GetFile_WhenFileNotFound_ReturnsNotFound()
        {
            // Arrange
            var fileId = Guid.NewGuid();

            _responseBLMock.Setup(x => x.GetFileContentAsync(
                It.IsAny<Guid>(), 
                It.IsAny<Guid>(), 
                It.IsAny<string>()))
                .ThrowsAsync(new FileNotFoundException(fileId));

            // Act
            var result = await _controller.GetFile(fileId);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.StatusCode.Should().Be(404);
        }

        [Fact]
        public async Task GetFile_WhenUnauthorized_ReturnsForbid()
        {
            // Arrange
            var fileId = Guid.NewGuid();

            _responseBLMock.Setup(x => x.GetFileContentAsync(
                It.IsAny<Guid>(), 
                It.IsAny<Guid>(), 
                It.IsAny<string>()))
                .ThrowsAsync(new ResponseUnauthorizedException("You don't have access to this file"));

            // Act
            var result = await _controller.GetFile(fileId);

            // Assert
            var forbidResult = result.Should().BeOfType<ForbidResult>().Subject;
        }

        [Fact]
        public async Task GetFile_WhenDataAccessError_ReturnsInternalServerError()
        {
            // Arrange
            var fileId = Guid.NewGuid();

            _responseBLMock.Setup(x => x.GetFileContentAsync(
                It.IsAny<Guid>(), 
                It.IsAny<Guid>(), 
                It.IsAny<string>()))
                .ThrowsAsync(new ResponseDataAccessException("Database error", new Exception()));

            // Act
            var result = await _controller.GetFile(fileId);

            // Assert
            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task GetFile_WhenUnexpectedError_ReturnsInternalServerError()
        {
            // Arrange
            var fileId = Guid.NewGuid();

            _responseBLMock.Setup(x => x.GetFileContentAsync(
                It.IsAny<Guid>(), 
                It.IsAny<Guid>(), 
                It.IsAny<string>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _controller.GetFile(fileId);

            // Assert
            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(500);
        }

        #endregion
    }
}
