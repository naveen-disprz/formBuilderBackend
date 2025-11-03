using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using Backend.Business;
using Backend.DataAccess;
using Backend.DTOs.Form;
using Backend.Models.Nosql;
using Backend.Enums;
using Backend.Exceptions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using MongoDB.Bson;

namespace Backend.Tests.UnitTests.Business
{
    public class FormBLTests
    {
        private readonly Mock<IFormDAL> _formDALMock;
        private readonly Mock<IResponseDAL> _responseDALMock;
        private readonly Mock<IUserDAL> _userDALMock;
        private readonly Mock<ILogger<FormBL>> _loggerMock;
        private readonly FormBL _formBL;

        public FormBLTests()
        {
            _formDALMock = new Mock<IFormDAL>();
            _responseDALMock = new Mock<IResponseDAL>();
            _userDALMock = new Mock<IUserDAL>();
            _loggerMock = new Mock<ILogger<FormBL>>();

            _formBL = new FormBL(
                _userDALMock.Object,
                _formDALMock.Object,
                _responseDALMock.Object,
                _loggerMock.Object
            );
        }

        #region CreateFormAsync Tests

        [Fact]
        public async Task CreateFormAsync_WithValidData_ReturnsFormDetailDto()
        {
            // Arrange
            var creatorId = Guid.NewGuid();
            var createFormDto = new CreateFormDto
            {
                Title = "Test Form",
                Description = "Test Description",
                HeaderTitle = "Header Title",
                HeaderDescription = "Header Description",
                Questions = new List<QuestionDto>
                {
                    new QuestionDto
                    {
                        Label = "Question 1",
                        Description = "Q1 Description",
                        Type = "ShortText",
                        Required = true,
                        Order = 1
                    },
                    new QuestionDto
                    {
                        Label = "Question 2",
                        Type = "SingleSelect",
                        Required = false,
                        Options = new List<OptionDto>
                        {
                            new OptionDto { Id =ObjectId.GenerateNewId().ToString(), Label = "Option 1" },
                            new OptionDto { Id = ObjectId.GenerateNewId().ToString(), Label = "Option 2" }
                        },
                        Order = 2
                    }
                }
            };

            var createdForm = new Form
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Title = createFormDto.Title,
                Description = createFormDto.Description,
                HeaderTitle = createFormDto.HeaderTitle,
                HeaderDescription = createFormDto.HeaderDescription,
                CreatedBy = creatorId,
                IsPublished = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Questions = new List<Question>
                {
                    new Question
                    {
                        Id = ObjectId.GenerateNewId().ToString(),
                        Label = "Question 1",
                        Description = "Q1 Description",
                        Type = QuestionType.ShortText,
                        Required = true,
                        Order = 1
                    },
                    new Question
                    {
                        Id = ObjectId.GenerateNewId().ToString(),
                        Label = "Question 2",
                        Type = QuestionType.SingleSelect,
                        Required = false,
                        Options = new List<Option>
                        {
                            new Option { Id = ObjectId.GenerateNewId().ToString(), Label = "Option 1" },
                            new Option { Id = ObjectId.GenerateNewId().ToString(), Label = "Option 2" }
                        },
                        Order = 2
                    }
                }
            };

            _formDALMock.Setup(x => x.CreateFormAsync(It.IsAny<Form>()))
                .ReturnsAsync(createdForm);

            // Act
            var result = await _formBL.CreateFormAsync(createFormDto, creatorId);

            // Assert
            result.Should().NotBeNull();
            result.Title.Should().Be(createFormDto.Title);
            result.Description.Should().Be(createFormDto.Description);
            result.Questions.Should().HaveCount(2);
            result.Questions[0].Label.Should().Be("Question 1");
            result.Questions[1].Options.Should().HaveCount(2);
        }

        [Fact]
        public async Task CreateFormAsync_WithEmptyTitle_ThrowsFormValidationException()
        {
            // Arrange
            var createFormDto = new CreateFormDto
            {
                Title = "",
                Questions = new List<QuestionDto>()
            };

            // Act
            var act = async () => await _formBL.CreateFormAsync(createFormDto, Guid.NewGuid());

            // Assert
            await act.Should().ThrowAsync<FormValidationException>()
                .WithMessage("Form title is required");
        }

        [Fact]
        public async Task CreateFormAsync_WithSelectQuestionWithoutOptions_ThrowsQuestionValidationException()
        {
            // Arrange
            var createFormDto = new CreateFormDto
            {
                Title = "Test Form",
                Questions = new List<QuestionDto>
                {
                    new QuestionDto
                    {
                        Label = "Select Question",
                        Type = "singleSelect",
                        Options = null
                    }
                }
            };

            // Act
            var act = async () => await _formBL.CreateFormAsync(createFormDto, Guid.NewGuid());

            // Assert
            await act.Should().ThrowAsync<QuestionValidationException>()
                .WithMessage("Options are required for singleSelect questions");
        }

        #endregion

        #region GetFormsAsync Tests

        [Fact]
        public async Task GetFormsAsync_ForLearner_ReturnsOnlyPublishedForms()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var forms = new List<Form>
            {
                new Form
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    Title = "Published Form",
                    IsPublished = true,
                    PublishedBy = userId,
                    CreatedBy = userId,
                    Questions = new List<Question> { new Question() }
                }
            };

            _formDALMock.Setup(x => x.GetAllFormsAsync(1, 10, "",true, true))
                .ReturnsAsync(forms);
            _formDALMock.Setup(x => x.GetFormCountAsync(1, 10, "",true, true))
                .ReturnsAsync(1);

            // Act
            var result = await _formBL.GetFormsAsync(1, 10, "","learner", userId);

            // Assert
            result.Should().NotBeNull();
            result.Forms.Should().HaveCount(1);
            result.TotalCount.Should().Be(1);
            result.CurrentPage.Should().Be(1);
            result.PageSize.Should().Be(10);
            _formDALMock.Verify(x => x.GetAllFormsAsync(1, 10, "",true, true), Times.Once);
        }

        [Fact]
        public async Task GetFormsAsync_ForAdmin_ReturnsAllForms()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var forms = new List<Form>
            {
                new Form { Id = "1", Title = "Form 1", IsPublished = false, Visibility = true, CreatedBy = userId },
                new Form { Id = "2", Title = "Form 2", IsPublished = false,Visibility = true, CreatedBy = userId }
            };

            _formDALMock.Setup(x => x.GetAllFormsAsync(1, 10, "",null, null))
                .ReturnsAsync(forms);
            _formDALMock.Setup(x => x.GetFormCountAsync(1, 10, "",null, null))
                .ReturnsAsync(2);

            // Act
            var result = await _formBL.GetFormsAsync(1, 10, "","admin", userId);

            // Assert
            result.Forms.Should().HaveCount(2);
            result.TotalCount.Should().Be(2);
            _formDALMock.Verify(x => x.GetAllFormsAsync(1, 10, "",null, null), Times.Once);
        }

        #endregion

        #region GetFormByIdAsync Tests

        [Fact]
        public async Task GetFormByIdAsync_WithExistingForm_ReturnsFormDetailDto()
        {
            // Arrange
            var formId = ObjectId.GenerateNewId().ToString();
            var form = new Form
            {
                Id = formId,
                Title = "Test Form",
                Description = "Description",
                CreatedBy = Guid.NewGuid(),
                IsPublished = true,
                Questions = new List<Question>
                {
                    new Question
                    {
                        Id = ObjectId.GenerateNewId().ToString(),
                        Label = "Question 1",
                        Type = QuestionType.ShortText,
                        Order = 1
                    }
                }
            };

            _formDALMock.Setup(x => x.GetFormByIdAsync(formId))
                .ReturnsAsync(form);

            // Act
            var result = await _formBL.GetFormByIdAsync(formId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(formId);
            result.Title.Should().Be("Test Form");
            result.Questions.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetFormByIdAsync_WithNonExistentForm_ThrowsFormNotFoundException()
        {
            // Arrange
            var formId = ObjectId.GenerateNewId().ToString();
            _formDALMock.Setup(x => x.GetFormByIdAsync(formId))
                .ReturnsAsync((Form)null);

            // Act
            var act = async () => await _formBL.GetFormByIdAsync(formId);

            // Assert
            await act.Should().ThrowAsync<FormNotFoundException>()
                .WithMessage($"Form not found: {formId}");
        }

        #endregion

        #region UpdateFormAsync Tests

        [Fact]
        public async Task UpdateFormAsync_WithValidData_ReturnsUpdatedFormDetailDto()
        {
            // Arrange
            var formId = ObjectId.GenerateNewId().ToString();
            var userId = Guid.NewGuid();
            var updateFormDto = new UpdateFormDto
            {
                Title = "Updated Form",
                Description = "Updated Description",
                Questions = new List<QuestionDto>
                {
                    new QuestionDto
                    {
                        Label = "Updated Question",
                        Type = "LongText",
                        Required = true,
                        Order = 1
                    }
                }
            };

            var existingForm = new Form
            {
                Id = formId,
                Title = "Original Form",
                CreatedBy = userId,
                IsPublished = false
            };

            var updatedForm = new Form
            {
                Id = formId,
                Title = updateFormDto.Title,
                Description = updateFormDto.Description,
                CreatedBy = userId,
                UpdatedAt = DateTime.UtcNow,
                Questions = new List<Question>
                {
                    new Question
                    {
                        Label = "Updated Question",
                        Type = QuestionType.LongText,
                        Required = true,
                        Order = 1
                    }
                }
            };

            _formDALMock.Setup(x => x.GetFormByIdAsync(formId))
                .ReturnsAsync(existingForm);
            _responseDALMock.Setup(x => x.GetResponseCountByFormIdAsync(formId))
                .ReturnsAsync(0);
            _formDALMock.Setup(x => x.UpdateFormAsync(formId, It.IsAny<Form>()))
                .ReturnsAsync(updatedForm);

            // Act
            var result = await _formBL.UpdateFormAsync(formId, updateFormDto, userId);

            // Assert
            result.Should().NotBeNull();
            result.Title.Should().Be("Updated Form");
            result.Description.Should().Be("Updated Description");
        }

        [Fact]
        public async Task UpdateFormAsync_WithNonExistentForm_ThrowsFormNotFoundException()
        {
            // Arrange
            var formId = ObjectId.GenerateNewId().ToString();
            var updateFormDto = new UpdateFormDto { Title = "Test" };

            _formDALMock.Setup(x => x.GetFormByIdAsync(formId))
                .ReturnsAsync((Form)null);

            // Act
            var act = async () => await _formBL.UpdateFormAsync(formId, updateFormDto, Guid.NewGuid());

            // Assert
            await act.Should().ThrowAsync<FormNotFoundException>();
        }

        [Fact]
        public async Task UpdateFormAsync_WithPublishedFormHavingResponses_ThrowsFormLockedException()
        {
            // Arrange
            var formId = ObjectId.GenerateNewId().ToString();
            var userId = Guid.NewGuid();
            var updateFormDto = new UpdateFormDto { Title = "Test" };

            var existingForm = new Form
            {
                Id = formId,
                CreatedBy = userId,
                IsPublished = true
            };

            _formDALMock.Setup(x => x.GetFormByIdAsync(formId))
                .ReturnsAsync(existingForm);
            _responseDALMock.Setup(x => x.GetResponseCountByFormIdAsync(formId))
                .ReturnsAsync(5);

            // Act
            var act = async () => await _formBL.UpdateFormAsync(formId, updateFormDto, userId);
        }

        #endregion

        #region DeleteFormAsync Tests

        [Fact]
        public async Task DeleteFormAsync_WithValidData_ReturnsTrue()
        {
            // Arrange
            var formId = ObjectId.GenerateNewId().ToString();
            var userId = Guid.NewGuid();
            var form = new Form
            {
                Id = formId,
                CreatedBy = userId
            };

            _formDALMock.Setup(x => x.GetFormByIdAsync(formId))
                .ReturnsAsync(form);
            _formDALMock.Setup(x => x.SoftDeleteFormAsync(formId))
                .ReturnsAsync(true);

            // Act
            var result = await _formBL.DeleteFormAsync(formId, userId);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteFormAsync_WithNonExistentForm_ThrowsFormNotFoundException()
        {
            // Arrange
            var formId = ObjectId.GenerateNewId().ToString();
            _formDALMock.Setup(x => x.GetFormByIdAsync(formId))
                .ReturnsAsync((Form)null);

            // Act
            var act = async () => await _formBL.DeleteFormAsync(formId, Guid.NewGuid());

            // Assert
            await act.Should().ThrowAsync<FormNotFoundException>();
        }

        #endregion

        #region PublishFormAsync Tests

        [Fact]
        public async Task PublishFormAsync_WithValidData_ReturnsTrue()
        {
            // Arrange
            var formId = ObjectId.GenerateNewId().ToString();
            var userId = Guid.NewGuid();
            var form = new Form
            {
                Id = formId,
                CreatedBy = userId,
                IsPublished = false
            };

            _formDALMock.Setup(x => x.GetFormByIdAsync(formId))
                .ReturnsAsync(form);
            _formDALMock.Setup(x => x.PublishFormAsync(formId, userId))
                .ReturnsAsync(true);

            // Act
            var result = await _formBL.PublishFormAsync(formId, userId);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task PublishFormAsync_WithNonExistentForm_ThrowsFormNotFoundException()
        {
            // Arrange
            var formId = ObjectId.GenerateNewId().ToString();
            _formDALMock.Setup(x => x.GetFormByIdAsync(formId))
                .ReturnsAsync((Form)null);

            // Act
            var act = async () => await _formBL.PublishFormAsync(formId, Guid.NewGuid());

            // Assert
            await act.Should().ThrowAsync<FormNotFoundException>();
        }

        #endregion
        
        #region CreateFormAsync Exception Tests

        [Fact]
        public async Task CreateFormAsync_WhenDALThrowsGenericException_ThrowsFormDataAccessException()
        {
            // Arrange
            var creatorId = Guid.NewGuid();
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
                        Required = true,
                        Order = 1
                    }
                }
            };

            _formDALMock.Setup(x => x.CreateFormAsync(It.IsAny<Form>()))
                .ThrowsAsync(new Exception("Database connection failed"));

            // Act
            var act = async () => await _formBL.CreateFormAsync(createFormDto, creatorId);

            // Assert
            var exception = await act.Should().ThrowAsync<FormDataAccessException>();
            exception.Which.Message.Should().Be("Failed to create form");
            exception.Which.InnerException.Should().NotBeNull();
            exception.Which.InnerException!.Message.Should().Be("Database connection failed");
        }

        [Fact]
        public async Task CreateFormAsync_WithMultiSelectQuestionWithoutOptions_ThrowsQuestionValidationException()
        {
            // Arrange
            var createFormDto = new CreateFormDto
            {
                Title = "Test Form",
                Questions = new List<QuestionDto>
                {
                    new QuestionDto
                    {
                        Label = "Multi Select Question",
                        Type = "multiSelect",
                        Options = new List<OptionDto>() // Empty options list
                    }
                }
            };

            // Act
            var act = async () => await _formBL.CreateFormAsync(createFormDto, Guid.NewGuid());

            // Assert
            await act.Should().ThrowAsync<QuestionValidationException>()
                .WithMessage("Options are required for multiSelect questions");
        }

        #endregion

        #region GetFormsAsync Exception Tests

        [Fact]
        public async Task GetFormsAsync_WhenGetAllFormsThrowsException_ThrowsFormDataAccessException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            
            _formDALMock.Setup(x => x.GetAllFormsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(),It.IsAny<bool?>(), It.IsAny<bool?>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var act = async () => await _formBL.GetFormsAsync(1, 10, "","learner", userId);

            // Assert
            var exception = await act.Should().ThrowAsync<FormDataAccessException>();
            exception.Which.Message.Should().Be("Failed to retrieve forms");
            exception.Which.InnerException.Should().NotBeNull();
        }

        [Fact]
        public async Task GetFormsAsync_WhenGetFormCountThrowsException_ThrowsFormDataAccessException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var forms = new List<Form>
            {
                new Form { Id = "1", Title = "Form 1", IsPublished = true }
            };

            _formDALMock.Setup(x => x.GetAllFormsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<bool?>()))
                .ReturnsAsync(forms);
            _formDALMock.Setup(x => x.GetFormCountAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(),It.IsAny<bool?>(), It.IsAny<bool?>()))
                .ThrowsAsync(new Exception("Count query failed"));

            // Act
            var act = async () => await _formBL.GetFormsAsync(1, 10, "","admin", userId);

            // Assert
            var exception = await act.Should().ThrowAsync<FormDataAccessException>();
            exception.Which.Message.Should().Be("Failed to retrieve forms");
            exception.Which.InnerException.Should().NotBeNull();
        }

        #endregion

        #region GetFormByIdAsync Exception Tests

        [Fact]
        public async Task GetFormByIdAsync_WhenDALThrowsGenericException_ThrowsFormDataAccessException()
        {
            // Arrange
            var formId = ObjectId.GenerateNewId().ToString();
            
            _formDALMock.Setup(x => x.GetFormByIdAsync(formId))
                .ThrowsAsync(new Exception("Database connection lost"));

            // Act
            var act = async () => await _formBL.GetFormByIdAsync(formId);

            // Assert
            var exception = await act.Should().ThrowAsync<FormDataAccessException>();
            exception.Which.Message.Should().Be($"Failed to retrieve form: {formId}");
            exception.Which.InnerException.Should().NotBeNull();
        }

        #endregion

        #region UpdateFormAsync Exception Tests

        [Fact]
        public async Task UpdateFormAsync_WhenDALThrowsGenericException_ThrowsFormDataAccessException()
        {
            // Arrange
            var formId = ObjectId.GenerateNewId().ToString();
            var userId = Guid.NewGuid();
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

            var existingForm = new Form
            {
                Id = formId,
                CreatedBy = userId,
                IsPublished = false
            };

            _formDALMock.Setup(x => x.GetFormByIdAsync(formId))
                .ReturnsAsync(existingForm);
            _responseDALMock.Setup(x => x.GetResponseCountByFormIdAsync(formId))
                .ReturnsAsync(0);
            _formDALMock.Setup(x => x.UpdateFormAsync(formId, It.IsAny<Form>()))
                .ThrowsAsync(new Exception("Update failed"));

            // Act
            var act = async () => await _formBL.UpdateFormAsync(formId, updateFormDto, userId);

            // Assert
            var exception = await act.Should().ThrowAsync<FormDataAccessException>();
            exception.Which.Message.Should().Be($"Failed to update form: {formId}");
            exception.Which.InnerException.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateFormAsync_WithMultiSelectQuestionWithoutOptions_ThrowsQuestionValidationException()
        {
            // Arrange
            var formId = ObjectId.GenerateNewId().ToString();
            var userId = Guid.NewGuid();
            var updateFormDto = new UpdateFormDto
            {
                Title = "Updated Form",
                Questions = new List<QuestionDto>
                {
                    new QuestionDto
                    {
                        Label = "Multi Select Question",
                        Type = "multiSelect",
                        Options = new List<OptionDto>() // Empty options
                    }
                }
            };

            var existingForm = new Form
            {
                Id = formId,
                CreatedBy = userId,
                IsPublished = false
            };

            _formDALMock.Setup(x => x.GetFormByIdAsync(formId))
                .ReturnsAsync(existingForm);
            _responseDALMock.Setup(x => x.GetResponseCountByFormIdAsync(formId))
                .ReturnsAsync(0);

            // Act
            var act = async () => await _formBL.UpdateFormAsync(formId, updateFormDto, userId);
        }

        [Fact]
        public async Task UpdateFormAsync_WhenGetResponseCountThrowsException_ThrowsFormDataAccessException()
        {
            // Arrange
            var formId = ObjectId.GenerateNewId().ToString();
            var userId = Guid.NewGuid();
            var updateFormDto = new UpdateFormDto
            {
                Title = "Updated Form",
                Questions = new List<QuestionDto>()
            };

            var existingForm = new Form
            {
                Id = formId,
                CreatedBy = userId,
                IsPublished = true
            };

            _formDALMock.Setup(x => x.GetFormByIdAsync(formId))
                .ReturnsAsync(existingForm);
            _responseDALMock.Setup(x => x.GetResponseCountByFormIdAsync(formId))
                .ThrowsAsync(new Exception("Response count failed"));

            // Act
            var act = async () => await _formBL.UpdateFormAsync(formId, updateFormDto, userId);

            // Assert
            var exception = await act.Should().ThrowAsync<FormDataAccessException>();
            exception.Which.Message.Should().Be($"Failed to update form: {formId}");
            exception.Which.InnerException.Should().NotBeNull();
        }

        #endregion

        #region DeleteFormAsync Exception Tests

        [Fact]
        public async Task DeleteFormAsync_WhenDALThrowsGenericException_ThrowsFormDataAccessException()
        {
            // Arrange
            var formId = ObjectId.GenerateNewId().ToString();
            var userId = Guid.NewGuid();

            var form = new Form
            {
                Id = formId,
                CreatedBy = userId
            };

            _formDALMock.Setup(x => x.GetFormByIdAsync(formId))
                .ReturnsAsync(form);
            _formDALMock.Setup(x => x.SoftDeleteFormAsync(formId))
                .ThrowsAsync(new Exception("Delete failed"));

            // Act
            var act = async () => await _formBL.DeleteFormAsync(formId, userId);

            // Assert
            var exception = await act.Should().ThrowAsync<FormDataAccessException>();
            exception.Which.Message.Should().Be($"Failed to delete form: {formId}");
            exception.Which.InnerException.Should().NotBeNull();
        }

        [Fact]
        public async Task DeleteFormAsync_WhenGetFormByIdThrowsGenericException_ThrowsFormDataAccessException()
        {
            // Arrange
            var formId = ObjectId.GenerateNewId().ToString();
            var userId = Guid.NewGuid();

            _formDALMock.Setup(x => x.GetFormByIdAsync(formId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var act = async () => await _formBL.DeleteFormAsync(formId, userId);

            // Assert
            var exception = await act.Should().ThrowAsync<FormDataAccessException>();
            exception.Which.Message.Should().Be($"Failed to delete form: {formId}");
            exception.Which.InnerException.Should().NotBeNull();
        }

        #endregion

        #region PublishFormAsync Exception Tests

        [Fact]
        public async Task PublishFormAsync_WhenDALThrowsGenericException_ThrowsFormDataAccessException()
        {
            // Arrange
            var formId = ObjectId.GenerateNewId().ToString();
            var userId = Guid.NewGuid();

            var form = new Form
            {
                Id = formId,
                CreatedBy = userId,
                IsPublished = false
            };

            _formDALMock.Setup(x => x.GetFormByIdAsync(formId))
                .ReturnsAsync(form);
            _formDALMock.Setup(x => x.PublishFormAsync(formId, userId))
                .ThrowsAsync(new Exception("Publish failed"));

            // Act
            var act = async () => await _formBL.PublishFormAsync(formId, userId);

            // Assert
            var exception = await act.Should().ThrowAsync<FormDataAccessException>();
            exception.Which.Message.Should().Be($"Failed to publish form: {formId}");
            exception.Which.InnerException.Should().NotBeNull();
        }

        [Fact]
        public async Task PublishFormAsync_WhenGetFormByIdThrowsGenericException_ThrowsFormDataAccessException()
        {
            // Arrange
            var formId = ObjectId.GenerateNewId().ToString();
            var userId = Guid.NewGuid();

            _formDALMock.Setup(x => x.GetFormByIdAsync(formId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var act = async () => await _formBL.PublishFormAsync(formId, userId);

            // Assert
            var exception = await act.Should().ThrowAsync<FormDataAccessException>();
            exception.Which.Message.Should().Be($"Failed to publish form: {formId}");
            exception.Which.InnerException.Should().NotBeNull();
        }

        #endregion
    }
}
