using System;
using System.Threading.Tasks;
using Backend.DTOs.Response;

namespace Backend.Business;

public interface IResponseBL
{
    Task<SubmitResponseResultDto> SubmitResponseAsync(string formId, SubmitResponseDto submitDto, Guid userId);
    Task<ResponseListDto> GetFormResponsesAsync(string formId, int page, int pageSize, Guid userId);
    Task<ResponseDetailDto> GetResponseByIdAsync(Guid responseId, Guid userId, string userRole);
    Task<byte[]> GetFileContentAsync(Guid fileId, Guid userId, string userRole);
}