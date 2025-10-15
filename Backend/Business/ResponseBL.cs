using Backend.Models.Sql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend.DataAccess;
using Backend.DTOs.Response;
using Backend.Enums;
using Backend.Models.Nosql;
using Backend.Exceptions;
using Newtonsoft.Json;
using FileNotFoundException = Backend.Exceptions.FileNotFoundException;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Backend.Business
{
    public class ResponseBL : IResponseBL
    {
        private readonly IResponseDAL _responseDAL;
        private readonly IFormDAL _formDAL;
        private readonly IUserDAL _userDAL;
        private readonly ILogger<ResponseBL> _logger;

        public ResponseBL(
            IResponseDAL responseDAL,
            IFormDAL formDAL,
            IUserDAL userDAL,
            ILogger<ResponseBL> logger)
        {
            _responseDAL = responseDAL;
            _formDAL = formDAL;
            _userDAL = userDAL;
            _logger = logger;
        }

        public async Task<SubmitResponseResultDto> SubmitResponseAsync(string formId, SubmitResponseDto submitDto,
            Guid userId)
        {
            try
            {
                _logger.LogInformation($"User {userId} submitting response for form {formId}");

                // Get form to validate
                var form = await _formDAL.GetFormByIdAsync(formId);
                if (form == null)
                {
                    throw new FormNotFoundForResponseException(formId);
                }

                if (!form.IsPublished)
                {
                    throw new UnpublishedFormException();
                }

                // Check if user already responded
                var hasResponded = await _responseDAL.UserHasRespondedToFormAsync(userId, formId);
                if (hasResponded)
                {
                    throw new DuplicateResponseException();
                }

                // Validate all required questions are answered
                var requiredQuestions = form.Questions.Where(q => q.Required).ToList();
                foreach (var question in requiredQuestions)
                {
                    var answer = submitDto.Answers.FirstOrDefault(a => a.QuestionId == question.Id);
                    if (question.Type != QuestionType.File &&
                        (answer == null || string.IsNullOrWhiteSpace(answer.Value?.ToString())))
                    {
                        throw new RequiredQuestionException(question.Label);
                    }
                }

                // Create response
                var response = new Response
                {
                    FormId = formId,
                    SubmittedBy = userId,
                    ClientIp = submitDto.ClientIp,
                    UserAgent = submitDto.UserAgent
                };

                var createdResponse = await _responseDAL.CreateResponseAsync(response);

                // Create answers
                foreach (var answerDto in submitDto.Answers)
                {
                    var question = form.Questions.FirstOrDefault(q => q.Id == answerDto.QuestionId);
                    if (question == null) continue;

                    Answer answer;

                    if (question.Type.ToString() == "MultiSelect" || question.Type.ToString() == "SingleSelect")
                    {
                        answer = new Answer
                        {
                            ResponseId = createdResponse.ResponseId,
                            QuestionId = answerDto.QuestionId,
                            AnswerType = question.Type,
                            AnswerValue = JsonSerializer.Serialize(answerDto.Value)
                        };
                    }
                    else
                    {
                        answer = new Answer
                        {
                            ResponseId = createdResponse.ResponseId,
                            QuestionId = answerDto.QuestionId,
                            AnswerType = question.Type,
                            AnswerValue = answerDto.Value?.ToString()
                        };
                    }

                    var createdAnswer = await _responseDAL.CreateAnswerAsync(answer);

                    // Check if file data exists before creating FileUpload
                    if (answerDto.FileData != null)
                    {
                        var fileUpload = new FileUpload
                        {
                            AnswerId = createdAnswer.AnswerId,
                            FileName = answerDto.FileData.FileName,
                            MimeType = answerDto.FileData.MimeType,
                            FileSizeBytes = answerDto.FileData.FileSizeBytes,
                            FileContent = answerDto.FileData.Base64Content
                        };

                        await _responseDAL.CreateFileUploadAsync(fileUpload);
                    }
                }

                _logger.LogInformation($"Response submitted successfully: {createdResponse.ResponseId}");

                return new SubmitResponseResultDto
                {
                    Success = true,
                    ResponseId = createdResponse.ResponseId,
                    SubmittedAt = createdResponse.SubmittedAt,
                    Message = "Response submitted successfully"
                };
            }
            catch (ResponseException)
            {
                throw; // Re-throw our custom exceptions
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting response");
                throw new ResponseDataAccessException("Failed to submit response", ex);
            }
        }

        public async Task<ResponseListDto> GetFormResponsesAsync(string formId, int page, int pageSize, Guid userId)
        {
            try
            {
                // Check if user owns the form
                var form = await _formDAL.GetFormByIdAsync(formId);
                if (form == null)
                {
                    throw new FormNotFoundForResponseException(formId);
                }

                if (form.CreatedBy != userId)
                {
                    throw new ResponseUnauthorizedException("You can only view responses for your own forms");
                }

                var responses = await _responseDAL.GetResponsesByFormIdAsync(formId, page, pageSize);
                var totalCount = await _responseDAL.GetResponseCountByFormIdAsync(formId);

                var responseItems = responses.Select(r => new ResponseItemDto
                {
                    ResponseId = r.ResponseId,
                    FormId = r.FormId,
                    SubmittedBy = r.SubmittedBy,
                    SubmitterUsername = r.User?.Username ?? "Unknown",
                    SubmittedAt = r.SubmittedAt,
                    AnswerCount = r.Answers?.Count ?? 0
                }).ToList();

                return new ResponseListDto
                {
                    Responses = responseItems,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                };
            }
            catch (ResponseException)
            {
                throw; // Re-throw our custom exceptions
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting responses for form: {formId}");
                throw new ResponseDataAccessException($"Failed to retrieve responses for form: {formId}", ex);
            }
        }

        public async Task<ResponseDetailDto> GetResponseByIdAsync(Guid responseId, Guid userId, string userRole)
        {
            try
            {
                var response = await _responseDAL.GetResponseByIdAsync(responseId);

                if (response == null)
                {
                    throw new ResponseNotFoundException(responseId);
                }

                // Check permissions
                if (userRole.ToLower() != "admin")
                {
                    // Check if user owns the response or the form
                    var form1 = await _formDAL.GetFormByIdAsync(response.FormId);

                    if (response.SubmittedBy != userId && form1?.CreatedBy != userId)
                    {
                        throw new ResponseUnauthorizedException(
                            "You can only view your own responses or responses to your forms");
                    }
                }

                // Get form details to include question labels
                var form = await _formDAL.GetFormByIdAsync(response.FormId);

                var answerDetails = response.Answers.Select(a =>
                {
                    var question = form?.Questions.FirstOrDefault(q => q.Id == a.QuestionId);

                    return new AnswerDetailDto
                    {
                        AnswerId = a.AnswerId,
                        QuestionId = a.QuestionId,
                        QuestionLabel = question?.Label ?? "Unknown Question",
                        QuestionType = a.AnswerType.ToString(),
                        Value = a.AnswerType switch
                        {
                            QuestionType.MultiSelect or QuestionType.SingleSelect => JsonSerializer.Deserialize<List<object>>(a.AnswerValue!),
                            QuestionType.Number => int.Parse(a.AnswerValue!),
                            _ => a.AnswerValue
                        },
                        Files = a.Files?.Select(f => new FileMetadataDto
                        {
                            FileId = f.FileId,
                            FileName = f.FileName,
                            MimeType = f.MimeType,
                            FileSizeBytes = f.FileSizeBytes,
                            UploadedAt = f.CreatedAt
                        }).ToList()
                    };
                }).ToList();

                return new ResponseDetailDto
                {
                    ResponseId = response.ResponseId,
                    FormId = response.FormId,
                    FormTitle = form?.Title ?? "Unknown Form",
                    SubmittedBy = response.SubmittedBy,
                    SubmitterUsername = response.User?.Username ?? "Unknown",
                    SubmittedAt = response.SubmittedAt,
                    ClientIp = response.ClientIp,
                    UserAgent = response.UserAgent,
                    Answers = answerDetails
                };
            }
            catch (ResponseException)
            {
                throw; // Re-throw our custom exceptions
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting response: {responseId}");
                throw new ResponseDataAccessException($"Failed to retrieve response: {responseId}", ex);
            }
        }

        public async Task<FileUpload> GetFileContentAsync(Guid fileId, Guid userId, string userRole)
        {
            try
            {
                var file = await _responseDAL.GetFileByIdAsync(fileId);

                if (file == null)
                {
                    throw new FileNotFoundException(fileId);
                }

                // TODO: Add permission checks here
                // Check if user has access to this file

                // Convert Base64 back to bytes
                return file;
            }
            catch (ResponseException)
            {
                throw; // Re-throw our custom exceptions
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting file: {fileId}");
                throw new ResponseDataAccessException($"Failed to retrieve file: {fileId}", ex);
            }
        }
    }
}
