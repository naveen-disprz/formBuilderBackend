using System;
using System.Collections.Generic;

namespace Backend.DTOs.Form;

public class FormListDto
{
    public List<FormItemDto> Forms { get; set; } = new();
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public long TotalCount { get; set; }
    public int TotalPages { get; set; }
}

public class FormItemDto
{
    public string FormId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int QuestionCount { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
}