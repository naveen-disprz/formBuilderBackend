using System;
using System.Collections.Generic;

namespace Backend.DTOs.Response;

public class ResponseListDto
{
    public List<ResponseItemDto> Responses { get; set; } = new();
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public long TotalCount { get; set; }
    public int TotalPages { get; set; }
}

public class ResponseItemDto
{
    public Guid ResponseId { get; set; }
    public string FormId { get; set; } = string.Empty;
    public Guid SubmittedBy { get; set; }
    public string SubmitterUsername { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public int AnswerCount { get; set; }
}