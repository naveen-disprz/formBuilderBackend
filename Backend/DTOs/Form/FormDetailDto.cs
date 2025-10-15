using System;
using System.Collections.Generic;
using Backend.Enums;

namespace Backend.DTOs.Form;

public class FormDetailDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? HeaderTitle { get; set; }
    public string? HeaderDescription { get; set; }
    public bool IsPublished { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid? PublishedBy { get; set; }
    public List<QuestionDetailDto> Questions { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class QuestionDetailDto
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; }
    public bool Required { get; set; }
    public List<OptionDetailDto>? Options { get; set; }
    public int Order { get; set; }
}

public class OptionDetailDto
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}