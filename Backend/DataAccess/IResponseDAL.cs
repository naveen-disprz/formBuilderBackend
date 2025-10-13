using Backend.Models.Sql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backend.DataAccess;

public interface IResponseDAL
{
    // Create
    Task<Response> CreateResponseAsync(Response response);
    Task<Answer> CreateAnswerAsync(Answer answer);
    Task<FileUpload> CreateFileUploadAsync(FileUpload file);

    // Read
    Task<Response?> GetResponseByIdAsync(Guid responseId);
    Task<List<Response>> GetResponsesByFormIdAsync(string formId, int page, int pageSize);
    Task<List<Response>> GetResponsesByUserIdAsync(Guid userId, int page, int pageSize);
    Task<List<Answer>> GetAnswersByResponseIdAsync(Guid responseId);
    Task<FileUpload?> GetFileByIdAsync(Guid fileId);
    Task<long> GetResponseCountByFormIdAsync(string formId);
    Task<bool> UserHasRespondedToFormAsync(Guid userId, string formId);

    // Update
    Task<Response> UpdateResponseAsync(Response response);

    // Delete
    Task<bool> DeleteResponseAsync(Guid responseId);
}