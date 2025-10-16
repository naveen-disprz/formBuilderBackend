using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Backend.Controllers;
using Backend.Business;
using Backend.DTOs.Form;
using Backend.Exceptions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
using MongoDB.Bson;

namespace Backend.Tests.UnitTests.Controllers
{
    public class FormControllerTests
    {
        private readonly Mock<IFormBL> _formBLMock;
        private readonly Mock<ILogger<FormController>> _loggerMock;
        private readonly FormController _formController;
        private readonly Guid _testUserId;
        private readonly string _testUserRole;

        public FormControllerTests()
        {
            _formBLMock = new Mock<IFormBL>();
            _loggerMock = new Mock<ILogger<FormController>>();
            
            _testUserId = Guid.NewGuid();
            _testUserRole = "Admin";

            // Create controller instance
            _formController = new FormController(_formBLMock.Object, _loggerMock.Object);

            // Setup HttpContext with user claims
            var claims = new List<Claim>
            {
                new Claim("UserId", _testUserId.ToString()),
                new Claim("Role", _testUserRole),
                new Claim(ClaimTypes.NameIdentifier, _testUserId.ToString()),
                new Claim(ClaimTypes.Role, _testUserRole)
            };
            
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            
            var httpContext = new DefaultHttpContext
            {
                User = principal
            };
            
            _formController.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        // Helper method to get CurrentUserId from controller
        private Guid GetCurrentUserId()
        {
            return _testUserId; // Since we know what we set in claims
        }

        // Helper method to get CurrentUserRole from controller  
        private string GetCurrentUserRole()
        {
            return _testUserRole; // Since we know what we set in claims
        }

        #region CreateForm Tests

        [Fact]
        public async Task CreateForm_WithValidData_ReturnsCreatedResult()
        {
            // Arrange
            var createFormDto = new CreateFormDto
            {
                Title = "Test Form",
                Description = "Test Description",
                Questions = new List<QuestionDto>
                {
                    new QuestionDto
                    {
                        Label = "Question 1",
                        Type = "ShortText",
                        Required = true
                    }
                }
            };

            var formDetailDto = new FormDetailDto
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Title = createFormDto.Title,
                Description = createFormDto.Description,
                CreatedBy = _testUserId
            };

            _formBLMock.Setup(x => x.CreateFormAsync(It.IsAny<CreateFormDto>(), It.IsAny<Guid>()))
                .ReturnsAsync(formDetailDto);

            // Act
            var result = await _formController.CreateForm(createFormDto);

            // Assert
            var createdResult = result.Should().BeOfType<CreatedResult>().Subject;
            createdResult.Location.Should().Be($"/api/forms/{formDetailDto.Id}");
            createdResult.Value.Should().BeEquivalentTo(formDetailDto);
        }

        [Fact]
        public async Task CreateForm_WithInvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            var createFormDto = new CreateFormDto();
            _formController.ModelState.AddModelError("Title", "Title is required");

            // Act
            var result = await _formController.CreateForm(createFormDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CreateForm_WhenFormValidationExceptionOccurs_ReturnsBadRequest()
        {
            // Arrange
            var createFormDto = new CreateFormDto { Title = "Test", Questions = new List<QuestionDto>() };
            var exception = new FormValidationException("Validation failed");

            _formBLMock.Setup(x => x.CreateFormAsync(It.IsAny<CreateFormDto>(), It.IsAny<Guid>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _formController.CreateForm(createFormDto);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateForm_WhenQuestionValidationExceptionOccurs_ReturnsBadRequest()
        {
            // Arrange
            var createFormDto = new CreateFormDto { Title = "Test", Questions = new List<QuestionDto>() };
            var exception = new QuestionValidationException("Question validation failed");

            _formBLMock.Setup(x => x.CreateFormAsync(It.IsAny<CreateFormDto>(), It.IsAny<Guid>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _formController.CreateForm(createFormDto);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().NotBeNull();
        }

        #endregion

        #region GetForms Tests

        [Fact]
        public async Task GetForms_WithValidParameters_ReturnsOkResult()
        {
            // Arrange
            var formListDto = new FormListDto
            {
                Forms = new List<FormItemDto>
                {
                    new FormItemDto { Id = "1", Title = "Form 1", CreatedBy = _testUserId },
                    new FormItemDto { Id = "2", Title = "Form 2", CreatedBy = _testUserId }
                },
                CurrentPage = 1,
                PageSize = 10,
                TotalCount = 2,
                TotalPages = 1
            };

            _formBLMock.Setup(x => x.GetFormsAsync(It.IsAny<int>(), It.IsAny<int>(), 
                It.IsAny<string>(), It.IsAny<Guid>()))
                .ReturnsAsync(formListDto);

            // Act
            var result = await _formController.GetForms(1, 10);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(formListDto);
        }

        [Fact]
        public async Task GetForms_WhenDataAccessExceptionOccurs_ReturnsInternalServerError()
        {
            // Arrange
            var exception = new FormDataAccessException("Database error", new Exception());

            _formBLMock.Setup(x => x.GetFormsAsync(It.IsAny<int>(), It.IsAny<int>(), 
                It.IsAny<string>(), It.IsAny<Guid>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _formController.GetForms();

            // Assert
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }

        #endregion

        #region GetFormById Tests

        [Fact]
        public async Task GetFormById_WithExistingForm_ReturnsOkResult()
        {
            // Arrange
            var formId = ObjectId.GenerateNewId().ToString();
            var formDetailDto = new FormDetailDto
            {
                Id = formId,
                Title = "Test Form",
                CreatedBy = _testUserId
            };

            _formBLMock.Setup(x => x.GetFormByIdAsync(formId))
                .ReturnsAsync(formDetailDto);

            // Act
            var result = await _formController.GetFormById(formId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(formDetailDto);
        }

        [Fact]
        public async Task GetFormById_WithNonExistentForm_ReturnsNotFound()
        {
            // Arrange
            var formId = ObjectId.GenerateNewId().ToString();
            var exception = new FormNotFoundException(formId);

            _formBLMock.Setup(x => x.GetFormByIdAsync(formId))
                .ThrowsAsync(exception);

            // Act
            var result = await _formController.GetFormById(formId);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.Value.Should().NotBeNull();
        }

        #endregion

        #region UpdateForm Tests

        [Fact]
        public async Task UpdateForm_WithValidData_ReturnsOkResult()
        {
            // Arrange
            var formId = ObjectId.GenerateNewId().ToString();
            var updateFormDto = new UpdateFormDto
            {
                Title = "Updated Form",
                Questions = new List<QuestionDto>
                {
                    new QuestionDto
                    {
                        Label = "Question 1",
                        Type = "ShortText",
                        Required = true
                    }
                }
            };

            var formDetailDto = new FormDetailDto
            {
                Id = formId,
                Title = updateFormDto.Title,
                CreatedBy = _testUserId
            };

            _formBLMock.Setup(x => x.UpdateFormAsync(formId, updateFormDto, It.IsAny<Guid>()))
                .ReturnsAsync(formDetailDto);

            // Act
            var result = await _formController.UpdateForm(formId, updateFormDto);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(formDetailDto);
        }

        [Fact]
        public async Task UpdateForm_WhenFormNotFound_ReturnsNotFound()
        {
            // Arrange
            var formId = ObjectId.GenerateNewId().ToString();
            var updateFormDto = new UpdateFormDto 
            { 
                Title = "Test",
                Questions = new List<QuestionDto>() 
            };
            var exception = new FormNotFoundException(formId);

            _formBLMock.Setup(x => x.UpdateFormAsync(formId, updateFormDto, It.IsAny<Guid>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _formController.UpdateForm(formId, updateFormDto);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task UpdateForm_WhenUnauthorized_ReturnsForbid()
        {
            // Arrange
            var formId = ObjectId.GenerateNewId().ToString();
            var updateFormDto = new UpdateFormDto 
            { 
                Title = "Test",
                Questions = new List<QuestionDto>() 
            };
            var exception = new FormUnauthorizedException("Not authorized");

            _formBLMock.Setup(x => x.UpdateFormAsync(formId, updateFormDto, It.IsAny<Guid>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _formController.UpdateForm(formId, updateFormDto);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task UpdateForm_WhenFormLocked_ReturnsConflict()
        {
            // Arrange
            var formId = ObjectId.GenerateNewId().ToString();
            var updateFormDto = new UpdateFormDto 
            { 
                Title = "Test",
                Questions = new List<QuestionDto>() 
            };
            var exception = new FormLockedException();

            _formBLMock.Setup(x => x.UpdateFormAsync(formId, updateFormDto, It.IsAny<Guid>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _formController.UpdateForm(formId, updateFormDto);

            // Assert
            var conflictResult = result.Should().BeOfType<ConflictObjectResult>().Subject;
            conflictResult.Value.Should().NotBeNull();
        }

        #endregion

        #region DeleteForm Tests

        [Fact]
        public async Task DeleteForm_WithValidData_ReturnsOkResult()
        {
            // Arrange
            var formId = ObjectId.GenerateNewId().ToString();

            _formBLMock.Setup(x => x.DeleteFormAsync(formId, It.IsAny<Guid>()))
                .ReturnsAsync(true);

            // Act
            var result = await _formController.DeleteForm(formId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task DeleteForm_WhenDeleteFails_ReturnsBadRequest()
        {
            // Arrange
            var formId = ObjectId.GenerateNewId().ToString();

            _formBLMock.Setup(x => x.DeleteFormAsync(formId, It.IsAny<Guid>()))
                .ReturnsAsync(false);

            // Act
            var result = await _formController.DeleteForm(formId);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task DeleteForm_WhenFormNotFound_ReturnsNotFound()
        {
            // Arrange
            var formId = ObjectId.GenerateNewId().ToString();
            var exception = new FormNotFoundException(formId);

            _formBLMock.Setup(x => x.DeleteFormAsync(formId, It.IsAny<Guid>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _formController.DeleteForm(formId);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        #endregion

        #region PublishForm Tests

        [Fact]
        public async Task PublishForm_WithValidData_ReturnsOkResult()
        {
            // Arrange
            var formId = ObjectId.GenerateNewId().ToString();

            _formBLMock.Setup(x => x.PublishFormAsync(formId, It.IsAny<Guid>()))
                .ReturnsAsync(true);

            // Act
            var result = await _formController.PublishForm(formId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task PublishForm_WhenPublishFails_ReturnsBadRequest()
        {
            // Arrange
            var formId = ObjectId.GenerateNewId().ToString();

            _formBLMock.Setup(x => x.PublishFormAsync(formId, It.IsAny<Guid>()))
                .ReturnsAsync(false);

            // Act
            var result = await _formController.PublishForm(formId);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task PublishForm_WhenFormNotFound_ReturnsNotFound()
        {
            // Arrange
            var formId = ObjectId.GenerateNewId().ToString();
            var exception = new FormNotFoundException(formId);

            _formBLMock.Setup(x => x.PublishFormAsync(formId, It.IsAny<Guid>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _formController.PublishForm(formId);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        #endregion
    }
}
