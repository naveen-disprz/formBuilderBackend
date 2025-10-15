using System;
using System.Threading.Tasks;
using Backend.DTOs.Response;
using Backend.Models.Sql;

namespace Backend.Business;

public interface IResponseBL
{
    Task<SubmitResponseResultDto> SubmitResponseAsync(string formId, SubmitResponseDto submitDto, Guid userId);
    Task<ResponseListDto> GetFormResponsesAsync(string formId, int page, int pageSize, Guid userId);
    Task<ResponseDetailDto> GetResponseByIdAsync(Guid responseId, Guid userId, string userRole);
    Task<FileUpload> GetFileContentAsync(Guid fileId, Guid userId, string userRole);
}