using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Backend.Enums;

namespace Backend.Models.Sql;

[Table("Answers")]
public class Answer
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid AnswerId { get; set; } = Guid.NewGuid();

    [Required] public Guid ResponseId { get; set; }

    [Required] [StringLength(100)] public string QuestionId { get; set; }

    [Required] [StringLength(50)] public QuestionType AnswerType { get; set; } // shortText, longText, number, file, etc.

    [Column(TypeName = "nvarchar(max)")] public string? AnswerValue { get; set; }

    [Required] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("ResponseId")] public virtual Response Response { get; set; }

    public virtual ICollection<FileUpload> Files { get; set; } = new List<FileUpload>();
}