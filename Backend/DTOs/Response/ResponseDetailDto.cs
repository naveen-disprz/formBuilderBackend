using System;
using System.Collections.Generic;
using Backend.Enums;

namespace Backend.DTOs.Response;
    public class ResponseDetailDto
    {
        public Guid ResponseId { get; set; }
        public string FormId { get; set; } = string.Empty;
        public string FormTitle { get; set; } = string.Empty;
        public Guid SubmittedBy { get; set; }
        public string SubmitterUsername { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
        public string? ClientIp { get; set; }   
        public string? UserAgent { get; set; }
        public List<AnswerDetailDto> Answers { get; set; } = new();
    }

    public class AnswerDetailDto
    {
        public Guid AnswerId { get; set; }
        public string QuestionId { get; set; } = string.Empty;
        public string QuestionLabel { get; set; } = string.Empty;
        public string QuestionType { get; set; } = string.Empty;
        public object? Value { get; set; }
        public List<FileMetadataDto>? Files { get; set; }
    }

    public class FileMetadataDto
    {
        public Guid FileId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public DateTime UploadedAt { get; set; }
    }
