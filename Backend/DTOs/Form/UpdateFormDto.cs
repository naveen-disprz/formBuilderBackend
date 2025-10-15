using System.ComponentModel.DataAnnotations;
using Backend.DTOs.Form;

namespace Backend.DTOs.Form;

public class UpdateFormDto : CreateFormDto
{
    [MaxLength(1000)] 
    public string? Update { get; set; }
    // Inherits all properties from CreateFormDto
}