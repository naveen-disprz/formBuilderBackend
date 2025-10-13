using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs.Response;

public class SubmitResponseDto
{
    [Required] [MinLength(1)] public List<AnswerDto> Answers { get; set; } = new();

    public string? ClientIp { get; set; }
    public string? UserAgent { get; set; }
}

public class AnswerDto
{
    [Required] public string QuestionId { get; set; } = string.Empty;

    public object? Value { get; set; }

    public FileDataDto? FileData { get; set; }
}

public class FileDataDto
{
    [Required] public string FileName { get; set; } = string.Empty;

    [Required] public string MimeType { get; set; } = string.Empty;

    [Required] public long FileSizeBytes { get; set; }

    [Required] public string Base64Content { get; set; } = string.Empty;
}