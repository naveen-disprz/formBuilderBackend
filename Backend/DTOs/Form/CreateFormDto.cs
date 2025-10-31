using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs.Form;

public class CreateFormDto
{
    [Required] 
    [MaxLength(200)] 
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)] 
    public string? Description { get; set; }

    [MaxLength(200)] 
    public string? HeaderTitle { get; set; }

    [MaxLength(1000)] 
    public string? HeaderDescription { get; set; }

    [Required] 
    public List<QuestionDto> Questions { get; set; } = new();
}

public class QuestionDto
{
    public string? Id { get; set; }

    [Required] 
    [MaxLength(500)] 
    public string Label { get; set; } = string.Empty;

    [MaxLength(1000)] 
    public string? Description { get; set; }

    [Required] 
    public string Type { get; set; } = string.Empty;

    public bool Required { get; set; }

    public List<OptionDto>? Options { get; set; } // CHANGED: Now list of OptionDto
    
    public string? DateFormat { get; set; } 

    public int Order { get; set; }
}

public class OptionDto
{
    public string? Id { get; set; } // Optional, will be generated if not provided
    
    [Required]
    [MaxLength(200)]
    public string Label { get; set; } = string.Empty;
}
