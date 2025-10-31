using Backend.Models.Nosql;
using System;
using System.Linq;
using System.Threading.Tasks;
using Backend.DataAccess;
using Backend.DTOs.Form;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Backend.Enums;
using Backend.Exceptions;

namespace Backend.Business
{
    public class FormBL : IFormBL
    {
        private readonly IFormDAL _formDAL;
        private readonly IUserDAL _userDAL;
        private readonly IResponseDAL _responseDAL;
        private readonly ILogger<FormBL> _logger;

        public FormBL(IUserDAL userDAL, IFormDAL formDAL, IResponseDAL responseDAL, ILogger<FormBL> logger)
        {
            _formDAL = formDAL;
            _responseDAL = responseDAL;
            _userDAL = userDAL;
            _logger = logger;
        }

        public async Task<FormDetailDto> CreateFormAsync(CreateFormDto createFormDto, Guid creatorId)
        {
            try
            {
                _logger.LogInformation($"Creating form: {createFormDto.Title}");

                // Validate input
                if (string.IsNullOrWhiteSpace(createFormDto.Title))
                {
                    throw new FormValidationException("Form title is required");
                }

                // if (createFormDto.Questions == null || !createFormDto.Questions.Any())
                // {
                //     throw new FormValidationException("At least one question is required");
                // }

                // Validate options for select type questions
                foreach (var question in createFormDto.Questions)
                {
                    if ((question.Type == "singleSelect" || question.Type == "multiSelect")
                        && (question.Options == null || !question.Options.Any()))
                    {
                        throw new QuestionValidationException($"Options are required for {question.Type} questions");
                    }
                }

                // Create form model
                var form = new Form
                {
                    Title = createFormDto.Title,
                    Description = createFormDto.Description,
                    HeaderTitle = createFormDto.HeaderTitle,
                    HeaderDescription = createFormDto.HeaderDescription,
                    CreatedBy = creatorId,
                    Questions = createFormDto.Questions.Select((q, index) => new Question
                    {
                        Label = q.Label,
                        Description = q.Description,
                        Type = Enum.Parse<QuestionType>(q.Type),
                        Required = q.Required,
                        Options = q.Options?.Select(o => new Option
                        {
                            Label = o.Label
                        }).ToList(),
                        DateFormat = q.DateFormat,
                        Order = q.Order > 0 ? q.Order : index + 1
                    }).ToList()
                };

                var createdForm = await _formDAL.CreateFormAsync(form);

                return MapToFormDetailDto(createdForm);
            }
            catch (FormException)
            {
                throw; // Re-throw our custom exceptions
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating form");
                throw new FormDataAccessException("Failed to create form", ex);
            }
        }

        public async Task<FormListDto> GetFormsAsync(int page, int pageSize, string userRole, Guid userId)
        {
            try
            {
                // Learners only see published forms, admins see all
                bool? isPublished = userRole.Equals("learner", StringComparison.OrdinalIgnoreCase) ? true : null;
                bool? visibility = userRole.Equals("learner", StringComparison.OrdinalIgnoreCase) ? true : null;

                // Get forms and total count
                var forms = await _formDAL.GetAllFormsAsync(page, pageSize, isPublished, visibility);
                var totalCount = await _formDAL.GetFormCountAsync(isPublished);

                var formItems = new List<FormItemDto>();

                // Sequentially process forms to avoid concurrent DbContext usage
                foreach (var f in forms)
                {
                    bool hasResponded = false;

                    // Only learners need to check if they've responded
                    if (userRole.Equals("learner", StringComparison.OrdinalIgnoreCase))
                    {
                        hasResponded = await _responseDAL.UserHasRespondedToFormAsync(userId, f.Id);
                    }

                    // Get the user (publisher or creator)
                    var user = await _userDAL.GetUserByIdAsync((Guid)(f.IsPublished ? f.PublishedBy : f.CreatedBy)!);

                    formItems.Add(new FormItemDto
                    {
                        Id = f.Id,
                        Title = f.Title,
                        Description = f.Description,
                        QuestionCount = f.Questions?.Count ?? 0,
                        IsPublished = f.IsPublished,
                        CreatedAt = f.CreatedAt,
                        CreatedBy = f.CreatedBy,
                        PublisherName = user?.Username,
                        PublishedBy = f.PublishedBy,
                        Visibility = f.Visibility,
                        CreatorName = user?.Username,
                        Responded = hasResponded
                    });
                }

                return new FormListDto
                {
                    Forms = formItems,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting forms");
                throw new FormDataAccessException("Failed to retrieve forms", ex);
            }
        }


        public async Task<FormDetailDto> GetFormByIdAsync(string formId)
        {
            try
            {
                var form = await _formDAL.GetFormByIdAsync(formId);

                if (form == null)
                {
                    throw new FormNotFoundException(formId);
                }

                return MapToFormDetailDto(form);
            }
            catch (FormException)
            {
                throw; // Re-throw our custom exceptions
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting form: {formId}");
                throw new FormDataAccessException($"Failed to retrieve form: {formId}", ex);
            }
        }

        public async Task<FormDetailDto> UpdateFormAsync(string formId, UpdateFormDto updateFormDto, Guid userId)
        {
            try
            {
                var existingForm = await _formDAL.GetFormByIdAsync(formId);

                if (existingForm == null)
                {
                    throw new FormNotFoundException(formId);
                }

                // Check if form has responses
                var responseCount = await _responseDAL.GetResponseCountByFormIdAsync(formId);
                if (responseCount > 0 && existingForm.IsPublished)
                {
                    throw new FormLockedException("Cannot update published form with responses");
                }

                // Validate options for select type questions
                foreach (var question in updateFormDto.Questions)
                {
                    if ((question.Type == "singleSelect" || question.Type == "multiSelect")
                        && (question.Options == null || !question.Options.Any()))
                    {
                        throw new QuestionValidationException($"Options are required for {question.Type} questions");
                    }
                }

                // Update form
                existingForm.Title = updateFormDto.Title;
                existingForm.Description = updateFormDto.Description;
                existingForm.HeaderTitle = updateFormDto.HeaderTitle;
                existingForm.Visibility = updateFormDto.Visibility;
                existingForm.HeaderDescription = updateFormDto.HeaderDescription;
                existingForm.Questions = updateFormDto.Questions.Select((q, index) => new Question
                {
                    Label = q.Label,
                    Description = q.Description,
                    Type = Enum.Parse<QuestionType>(q.Type),
                    Required = q.Required,
                    Options = q.Options?.Select(o => new Option
                    {
                        Label = o.Label
                    }).ToList(),
                    DateFormat = q.DateFormat,
                    Order = q.Order > 0 ? q.Order : index + 1
                }).ToList();

                var updatedForm = await _formDAL.UpdateFormAsync(formId, existingForm);

                return MapToFormDetailDto(updatedForm!);
            }
            catch (FormException)
            {
                throw; // Re-throw our custom exceptions
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating form: {formId}");
                throw new FormDataAccessException($"Failed to update form: {formId}", ex);
            }
        }

        public async Task<bool> DeleteFormAsync(string formId, Guid userId)
        {
            try
            {
                var form = await _formDAL.GetFormByIdAsync(formId);

                if (form == null)
                {
                    throw new FormNotFoundException(formId);
                }

                return await _formDAL.SoftDeleteFormAsync(formId);
            }
            catch (FormException)
            {
                throw; // Re-throw our custom exceptions
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting form: {formId}");
                throw new FormDataAccessException($"Failed to delete form: {formId}", ex);
            }
        }

        public async Task<bool> PublishFormAsync(string formId, Guid userId)
        {
            try
            {
                var form = await _formDAL.GetFormByIdAsync(formId);

                if (form == null)
                {
                    throw new FormNotFoundException(formId);
                }

                return await _formDAL.PublishFormAsync(formId, userId);
            }
            catch (FormException)
            {
                throw; // Re-throw our custom exceptions
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error publishing form: {formId}");
                throw new FormDataAccessException($"Failed to publish form: {formId}", ex);
            }
        }

        public async Task<bool> ToggleVisibility(string formId, bool visibility)
        {
            try
            {
                var form = await _formDAL.GetFormByIdAsync(formId);

                if (form == null)
                {
                    throw new FormNotFoundException(formId);
                }

                return await _formDAL.ToggleVisibility(formId, visibility);
            }
            catch (FormException)
            {
                throw; // Re-throw our custom exceptions
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error publishing form: {formId}");
                throw new FormDataAccessException($"Failed to publish form: {formId}", ex);
            }
        }

        private FormDetailDto MapToFormDetailDto(Form form)
        {
            return new FormDetailDto
            {
                Id = form.Id,
                Title = form.Title,
                Description = form.Description,
                HeaderTitle = form.HeaderTitle,
                HeaderDescription = form.HeaderDescription,
                IsPublished = form.IsPublished,
                CreatedBy = form.CreatedBy,
                PublishedBy = form.PublishedBy,
                Visibility = form.Visibility,
                Questions = form.Questions?.Select(q => new QuestionDetailDto
                {
                    Id = q.Id.ToString(),
                    Label = q.Label,
                    Description = q.Description,
                    DateFormat = q.Type == QuestionType.Date ? q.DateFormat : null,
                    Type = q.Type.ToString(),
                    Required = q.Required,
                    Options = q.Options?.Select(o => new OptionDetailDto
                    {
                        Id = o.Id,
                        Label = o.Label
                    }).ToList(),
                    Order = q.Order
                }).OrderBy(q => q.Order).ToList() ?? new List<QuestionDetailDto>(),
                CreatedAt = form.CreatedAt,
                UpdatedAt = form.UpdatedAt
            };
        }
    }
}