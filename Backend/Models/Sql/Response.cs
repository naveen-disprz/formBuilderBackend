using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Backend.Models.Nosql;

namespace Backend.Models.Sql;

[Table("Responses")]
public class Response
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid ResponseId { get; set; } = Guid.NewGuid();

    [Required] [StringLength(24)] public string FormId { get; set; } // MongoDB ObjectId reference

    [Required] public Guid SubmittedBy { get; set; }

    [Required] public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    [StringLength(45)] public string? ClientIp { get; set; }

    [StringLength(500)] public string? UserAgent { get; set; }

    // Navigation properties
    [ForeignKey("SubmittedBy")] public virtual User User { get; set; }
    public virtual ICollection<Answer> Answers { get; set; } = new List<Answer>();
}