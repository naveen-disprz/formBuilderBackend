using System.ComponentModel.DataAnnotations;
using Backend.DTOs.Form;

namespace Backend.DTOs.Form;

public class UpdateFormDto : CreateFormDto
{
    public bool Visibility { get; set; }
    
    // Inherits all properties from CreateFormDto
}