using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using Backend.Business;
using Backend.DataAccess;
using Backend.DTOs.Response;
using Backend.Models.Sql;
using Backend.Models.Nosql;
using Backend.Enums;
using Backend.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FileNotFoundException = Backend.Exceptions.FileNotFoundException;

namespace Backend.Tests.UnitTests.Business
{
    public class ResponseBLTests
    {
        private readonly Mock<IResponseDAL> _responseDALMock;
        private readonly Mock<IFormDAL> _formDALMock;
        private readonly Mock<IUserDAL> _userDALMock;
        private readonly Mock<ILogger<ResponseBL>> _loggerMock;
        private readonly ResponseBL _responseBL;

        public ResponseBLTests()
        {
            _responseDALMock = new Mock<IResponseDAL>();
            _formDALMock = new Mock<IFormDAL>();
            _userDALMock = new Mock<IUserDAL>();
            _loggerMock = new Mock<ILogger<ResponseBL>>();

            _responseBL = new ResponseBL(
                _responseDALMock.Object,
                _formDALMock.Object,
                _userDALMock.Object,
                _loggerMock.Object
            );
        }

        [Fact]
        public async Task SubmitResponseAsync_WithValidData_ReturnsSuccessResult()
        {
            // Arrange
            var formId = "507f1f77bcf86cd799439011";
            var userId = Guid.NewGuid();
            
            var form = new Form
            {
                Id = formId,
                Title = "Test Form",
                IsPublished = true,
                Questions = new List<Question>
                {
                    new Question
                    {
                        Id = "q1",
                        Label = "Question 1",
                        Type = QuestionType.ShortText,
                        Required = true
                    },
                    new Question
                    {
                        Id = "q2",
                        Label = "Question 2",
                        Type = QuestionType.Number,
                        Required = false
                    }
                }
            };

            var submitDto = new SubmitResponseDto
            {
                Answers = new List<AnswerDto>
                {
                    new AnswerDto
                    {
                        QuestionId = "q1",
                        Value = "Answer to question 1"
                    },
                    new AnswerDto
                    {
                        QuestionId = "q2",
                        Value = "42"
                    }
                },
                ClientIp = "192.168.1.1",
                UserAgent = "Mozilla/5.0"
            };

            var createdResponse = new Response
            {
                ResponseId = Guid.NewGuid(),
                FormId = formId,
                SubmittedBy = userId,
                SubmittedAt = DateTime.UtcNow
            };

            _formDALMock.Setup(x => x.GetFormByIdAsync(formId))
                .ReturnsAsync(form);
            
            _responseDALMock.Setup(x => x.UserHasRespondedToFormAsync(userId, formId))
                .ReturnsAsync(false);
            
            _responseDALMock.Setup(x => x.CreateResponseAsync(It.IsAny<Response>()))
                .ReturnsAsync(createdResponse);
            
            _responseDALMock.Setup(x => x.CreateAnswerAsync(It.IsAny<Answer>()))
                .ReturnsAsync((Answer a) => a);

            // Act
            var result = await _responseBL.SubmitResponseAsync(formId, submitDto, userId);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.ResponseId.Should().Be(createdResponse.ResponseId);
            result.Message.Should().Be("Response submitted successfully");
            
            _responseDALMock.Verify(x => x.CreateResponseAsync(It.IsAny<Response>()), Times.Once);
            _responseDALMock.Verify(x => x.CreateAnswerAsync(It.IsAny<Answer>()), Times.Exactly(2));
        }

        [Fact]
        public async Task SubmitResponseAsync_WithNonExistentForm_ThrowsFormNotFoundForResponseException()
        {
            // Arrange
            var formId = "507f1f77bcf86cd799439011";
            var userId = Guid.NewGuid();
            var submitDto = new SubmitResponseDto { Answers = new List<AnswerDto>() };

            _formDALMock.Setup(x => x.GetFormByIdAsync(formId))
                .ReturnsAsync((Form)null);

            // Act
            var act = async () => await _responseBL.SubmitResponseAsync(formId, submitDto, userId);

            // Assert
            await act.Should().ThrowAsync<FormNotFoundForResponseException>()
                .WithMessage($"Form not found: {formId}");
        }

        [Fact]
        public async Task SubmitResponseAsync_WithUnpublishedForm_ThrowsUnpublishedFormException()
        {
            // Arrange
            var formId = "507f1f77bcf86cd799439011";
            var userId = Guid.NewGuid();
            var submitDto = new SubmitResponseDto { Answers = new List<AnswerDto>() };

            var form = new Form
            {
                Id = formId,
                IsPublished = false
            };

            _formDALMock.Setup(x => x.GetFormByIdAsync(formId))
                .ReturnsAsync(form);

            // Act
            var act = async () => await _responseBL.SubmitResponseAsync(formId, submitDto, userId);

            // Assert
            await act.Should().ThrowAsync<UnpublishedFormException>()
                .WithMessage("Cannot submit response to unpublished form");
        }

        [Fact]
        public async Task SubmitResponseAsync_WhenUserAlreadyResponded_ThrowsDuplicateResponseException()
        {
            // Arrange
            var formId = "507f1f77bcf86cd799439011";
            var userId = Guid.NewGuid();
            var submitDto = new SubmitResponseDto { Answers = new List<AnswerDto>() };

            var form = new Form
            {
                Id = formId,
                IsPublished = true
            };

            _formDALMock.Setup(x => x.GetFormByIdAsync(formId))
                .ReturnsAsync(form);
            
            _responseDALMock.Setup(x => x.UserHasRespondedToFormAsync(userId, formId))
                .ReturnsAsync(true);

            // Act
            var act = async () => await _responseBL.SubmitResponseAsync(formId, submitDto, userId);

            // Assert
            await act.Should().ThrowAsync<DuplicateResponseException>()
                .WithMessage("You have already responded to this form");
        }

        [Fact]
        public async Task SubmitResponseAsync_WithMissingRequiredAnswer_ThrowsRequiredQuestionException()
        {
            // Arrange
            var formId = "507f1f77bcf86cd799439011";
            var userId = Guid.NewGuid();
            
            var form = new Form
            {
                Id = formId,
                IsPublished = true,
                Questions = new List<Question>
                {
                    new Question
                    {
                        Id = "q1",
                        Label = "Required Question",
                        Type = QuestionType.ShortText,
                        Required = true
                    }
                }
            };

            var submitDto = new SubmitResponseDto
            {
                Answers = new List<AnswerDto>() // No answer provided
            };

            _formDALMock.Setup(x => x.GetFormByIdAsync(formId))
                .ReturnsAsync(form);
            
            _responseDALMock.Setup(x => x.UserHasRespondedToFormAsync(userId, formId))
                .ReturnsAsync(false);

            // Act
            var act = async () => await _responseBL.SubmitResponseAsync(formId, submitDto, userId);

            // Assert
            await act.Should().ThrowAsync<RequiredQuestionException>()
                .WithMessage("Required question not answered: Required Question");
        }

        [Fact]
        public async Task SubmitResponseAsync_WithFileUpload_CreatesFileUpload()
        {
            // Arrange
            var formId = "507f1f77bcf86cd799439011";
            var userId = Guid.NewGuid();
            
            var form = new Form
            {
                Id = formId,
                IsPublished = true,
                Questions = new List<Question>
                {
                    new Question
                    {
                        Id = "q1",
                        Label = "File Upload",
                        Type = QuestionType.File,
                        Required = false
                    }
                }
            };

            var submitDto = new SubmitResponseDto
            {
                Answers = new List<AnswerDto>
                {
                    new AnswerDto
                    {
                        QuestionId = "q1",
                        FileData = new FileDataDto
                        {
                            FileName = "document.pdf",
                            MimeType = "application/pdf",
                            FileSizeBytes = 2048,
                            Base64Content = "base64encodedcontent"
                        }
                    }
                }
            };

            var createdResponse = new Response
            {
                ResponseId = Guid.NewGuid(),
                FormId = formId,
                SubmittedBy = userId,
                SubmittedAt = DateTime.UtcNow
            };

            var createdAnswer = new Answer
            {
                AnswerId = Guid.NewGuid(),
                ResponseId = createdResponse.ResponseId,
                QuestionId = "q1"
            };

            _formDALMock.Setup(x => x.GetFormByIdAsync(formId))
                .ReturnsAsync(form);
            
            _responseDALMock.Setup(x => x.UserHasRespondedToFormAsync(userId, formId))
                .ReturnsAsync(false);
            
            _responseDALMock.Setup(x => x.CreateResponseAsync(It.IsAny<Response>()))
                .ReturnsAsync(createdResponse);
            
            _responseDALMock.Setup(x => x.CreateAnswerAsync(It.IsAny<Answer>()))
                .ReturnsAsync(createdAnswer);
            
            _responseDALMock.Setup(x => x.CreateFileUploadAsync(It.IsAny<FileUpload>()))
                .ReturnsAsync((FileUpload f) => f);

            // Act
            var result = await _responseBL.SubmitResponseAsync(formId, submitDto, userId);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            
            _responseDALMock.Verify(x => x.CreateFileUploadAsync(
                It.Is<FileUpload>(f => 
                    f.FileName == "document.pdf" && 
                    f.MimeType == "application/pdf" &&
                    f.FileSizeBytes == 2048 &&
                    f.FileContent == "base64encodedcontent"
                )), Times.Once);
        }

        [Fact]
        public async Task GetFormResponsesAsync_WithValidFormAndOwner_ReturnsResponseList()
        {
            // Arrange
            var formId = "507f1f77bcf86cd799439011";
            var userId = Guid.NewGuid();
            
            var form = new Form
            {
                Id = formId,
                Title = "Test Form",
                CreatedBy = userId // User owns the form
            };

            var responses = new List<Response>
            {
                new Response
                {
                    ResponseId = Guid.NewGuid(),
                    FormId = formId,
                    SubmittedBy = Guid.NewGuid(),
                    SubmittedAt = DateTime.UtcNow,
                    User = new User { Username = "user1" },
                    Answers = new List<Answer> { new Answer(), new Answer() }
                },
                new Response
                {
                    ResponseId = Guid.NewGuid(),
                    FormId = formId,
                    SubmittedBy = Guid.NewGuid(),
                    SubmittedAt = DateTime.UtcNow.AddHours(-1),
                    User = new User { Username = "user2" },
                    Answers = new List<Answer> { new Answer() }
                }
            };

            _formDALMock.Setup(x => x.GetFormByIdAsync(formId))
                .ReturnsAsync(form);
            
            _responseDALMock.Setup(x => x.GetResponsesByFormIdAsync(formId, 1, 10))
                .ReturnsAsync(responses);
            
            _responseDALMock.Setup(x => x.GetResponseCountByFormIdAsync(formId))
                .ReturnsAsync(2);

            // Act
            var result = await _responseBL.GetFormResponsesAsync(formId, 1, 10, userId);

            // Assert
            result.Should().NotBeNull();
            result.Responses.Should().HaveCount(2);
            result.CurrentPage.Should().Be(1);
            result.PageSize.Should().Be(10);
            result.TotalCount.Should().Be(2);
            result.TotalPages.Should().Be(1);
            
            result.Responses[0].SubmitterUsername.Should().Be("user1");
            result.Responses[0].AnswerCount.Should().Be(2);
            result.Responses[1].SubmitterUsername.Should().Be("user2");
            result.Responses[1].AnswerCount.Should().Be(1);
        }

        [Fact]
        public async Task GetFormResponsesAsync_WithNonExistentForm_ThrowsFormNotFoundForResponseException()
        {
            // Arrange
            var formId = "507f1f77bcf86cd799439011";
            var userId = Guid.NewGuid();

            _formDALMock.Setup(x => x.GetFormByIdAsync(formId))
                .ReturnsAsync((Form)null);

            // Act
            var act = async () => await _responseBL.GetFormResponsesAsync(formId, 1, 10, userId);

            // Assert
            await act.Should().ThrowAsync<FormNotFoundForResponseException>()
                .WithMessage($"Form not found: {formId}");
        }

        [Fact]
        public async Task GetFormResponsesAsync_WhenUserDoesNotOwnForm_ThrowsResponseUnauthorizedException()
        {
            // Arrange
            var formId = "507f1f77bcf86cd799439011";
            var userId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            
            var form = new Form
            {
                Id = formId,
                Title = "Test Form",
                CreatedBy = otherUserId // Different user owns the form
            };

            _formDALMock.Setup(x => x.GetFormByIdAsync(formId))
                .ReturnsAsync(form);

            // Act
            var act = async () => await _responseBL.GetFormResponsesAsync(formId, 1, 10, userId);

            // Assert
            await act.Should().ThrowAsync<ResponseUnauthorizedException>()
                .WithMessage("You can only view responses for your own forms");
        }

        [Fact]
        public async Task GetResponseByIdAsync_WithValidResponseAndOwner_ReturnsResponseDetail()
        {
            // Arrange
            var responseId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var formId = "507f1f77bcf86cd799439011";
            
            var response = new Response
            {
                ResponseId = responseId,
                FormId = formId,
                SubmittedBy = userId, // User owns this response
                SubmittedAt = DateTime.UtcNow,
                ClientIp = "192.168.1.1",
                UserAgent = "Mozilla/5.0",
                User = new User { Username = "testuser" },
                Answers = new List<Answer>
                {
                    new Answer
                    {
                        AnswerId = Guid.NewGuid(),
                        QuestionId = "q1",
                        AnswerType = QuestionType.ShortText,
                        AnswerValue = "Answer text",
                        Files = new List<FileUpload>()
                    },
                    new Answer
                    {
                        AnswerId = Guid.NewGuid(),
                        QuestionId = "q2",
                        AnswerType = QuestionType.Number,
                        AnswerValue = "42",
                        Files = new List<FileUpload>()
                    }
                }
            };

            var form = new Form
            {
                Id = formId,
                Title = "Test Form",
                Questions = new List<Question>
                {
                    new Question { Id = "q1", Label = "Question 1", Type = QuestionType.ShortText },
                    new Question { Id = "q2", Label = "Question 2", Type = QuestionType.Number }
                }
            };

            _responseDALMock.Setup(x => x.GetResponseByIdAsync(responseId))
                .ReturnsAsync(response);
            
            _formDALMock.Setup(x => x.GetFormByIdAsync(formId))
                .ReturnsAsync(form);

            // Act
            var result = await _responseBL.GetResponseByIdAsync(responseId, userId, "learner");

            // Assert
            result.Should().NotBeNull();
            result.ResponseId.Should().Be(responseId);
            result.FormId.Should().Be(formId);
            result.FormTitle.Should().Be("Test Form");
            result.SubmitterUsername.Should().Be("testuser");
            result.ClientIp.Should().Be("192.168.1.1");
            result.UserAgent.Should().Be("Mozilla/5.0");
            result.Answers.Should().HaveCount(2);
            
            result.Answers[0].QuestionLabel.Should().Be("Question 1");
            result.Answers[0].QuestionType.Should().Be("ShortText");
            result.Answers[0].Value.Should().Be("Answer text");
            
            result.Answers[1].QuestionLabel.Should().Be("Question 2");
            result.Answers[1].QuestionType.Should().Be("Number");
            result.Answers[1].Value.Should().Be(42);
        }

        [Fact]
        public async Task GetResponseByIdAsync_WithNonExistentResponse_ThrowsResponseNotFoundException()
        {
            // Arrange
            var responseId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _responseDALMock.Setup(x => x.GetResponseByIdAsync(responseId))
                .ReturnsAsync((Response)null);

            // Act
            var act = async () => await _responseBL.GetResponseByIdAsync(responseId, userId, "learner");

            // Assert
            await act.Should().ThrowAsync<ResponseNotFoundException>()
                .WithMessage($"Response not found: {responseId}");
        }

        [Fact]
        public async Task GetResponseByIdAsync_AsAdmin_CanViewAnyResponse()
        {
            // Arrange
            var responseId = Guid.NewGuid();
            var adminUserId = Guid.NewGuid();
            var responseOwnerUserId = Guid.NewGuid();
            var formId = "507f1f77bcf86cd799439011";
            
            var response = new Response
            {
                ResponseId = responseId,
                FormId = formId,
                SubmittedBy = responseOwnerUserId, // Different user owns this response
                User = new User { Username = "responseowner" },
                Answers = new List<Answer>()
            };

            var form = new Form
            {
                Id = formId,
                Title = "Test Form",
                CreatedBy = Guid.NewGuid(), // Different user owns the form
                Questions = new List<Question>()
            };

            _responseDALMock.Setup(x => x.GetResponseByIdAsync(responseId))
                .ReturnsAsync(response);
            
            _formDALMock.Setup(x => x.GetFormByIdAsync(formId))
                .ReturnsAsync(form);

            // Act
            var result = await _responseBL.GetResponseByIdAsync(responseId, adminUserId, "admin");

            // Assert
            result.Should().NotBeNull();
            result.ResponseId.Should().Be(responseId);
            // No authorization exception should be thrown for admin
        }

        [Fact]
        public async Task GetResponseByIdAsync_AsNonOwnerNonAdmin_ThrowsResponseUnauthorizedException()
        {
            // Arrange
            var responseId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var responseOwnerUserId = Guid.NewGuid();
            var formOwnerUserId = Guid.NewGuid();
            var formId = "507f1f77bcf86cd799439011";
            
            var response = new Response
            {
                ResponseId = responseId,
                FormId = formId,
                SubmittedBy = responseOwnerUserId, // Different user owns this response
                User = new User { Username = "responseowner" },
                Answers = new List<Answer>()
            };

            var form = new Form
            {
                Id = formId,
                Title = "Test Form",
                CreatedBy = formOwnerUserId, // Different user owns the form
                Questions = new List<Question>()
            };

            _responseDALMock.Setup(x => x.GetResponseByIdAsync(responseId))
                .ReturnsAsync(response);
            
            _formDALMock.Setup(x => x.GetFormByIdAsync(formId))
                .ReturnsAsync(form);

            // Act
            var act = async () => await _responseBL.GetResponseByIdAsync(responseId, userId, "learner");

            // Assert
            await act.Should().ThrowAsync<ResponseUnauthorizedException>()
                .WithMessage("You can only view your own responses or responses to your forms");
        }

        [Fact]
        public async Task GetFileContentAsync_WithValidFile_ReturnsFile()
        {
            // Arrange
            var fileId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            
            var file = new FileUpload
            {
                FileId = fileId,
                FileName = "document.pdf",
                MimeType = "application/pdf",
                FileSizeBytes = 2048,
                FileContent = "base64encodedcontent"
            };

            _responseDALMock.Setup(x => x.GetFileByIdAsync(fileId))
                .ReturnsAsync(file);

            // Act
            var result = await _responseBL.GetFileContentAsync(fileId, userId, "learner");

            // Assert
            result.Should().NotBeNull();
            result.FileId.Should().Be(fileId);
            result.FileName.Should().Be("document.pdf");
            result.MimeType.Should().Be("application/pdf");
            result.FileContent.Should().Be("base64encodedcontent");
        }

        [Fact]
        public async Task GetFileContentAsync_WithNonExistentFile_ThrowsFileNotFoundException()
        {
            // Arrange
            var fileId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            _responseDALMock.Setup(x => x.GetFileByIdAsync(fileId))
                .ReturnsAsync((FileUpload)null);

            // Act
            var act = async () => await _responseBL.GetFileContentAsync(fileId, userId, "learner");

            // Assert
            await act.Should().ThrowAsync<FileNotFoundException>()
                .WithMessage($"File not found: {fileId}");
        }
    }
}
