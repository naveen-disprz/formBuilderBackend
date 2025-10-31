using Backend.DTOs;
using System;
using System.Threading.Tasks;
using Backend.DTOs.Form;

namespace Backend.Business;

public interface IFormBL
{
    Task<FormDetailDto> CreateFormAsync(CreateFormDto createFormDto, Guid creatorId);
    Task<FormListDto> GetFormsAsync(int page, int pageSize, string userRole, Guid userId);
    Task<FormDetailDto> GetFormByIdAsync(string formId);
    Task<FormDetailDto> UpdateFormAsync(string formId, UpdateFormDto updateFormDto, Guid userId);
    Task<bool> DeleteFormAsync(string formId, Guid userId);
    Task<bool> PublishFormAsync(string formId, Guid userId);
    Task<bool> ToggleVisibility(string formId, bool visibility);
}