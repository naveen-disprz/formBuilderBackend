using System;

namespace Backend.DTOs.Response;

public class SubmitResponseResultDto
{
    public bool Success { get; set; }
    public Guid ResponseId { get; set; }
    public DateTime SubmittedAt { get; set; }
    public string Message { get; set; } = string.Empty;
}