using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models.Sql;

[Table("Users")]
public class User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid UserId { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(320)]
    [EmailAddress]
    public string Email { get; set; }

    [Required] [StringLength(256)] public string PasswordHash { get; set; }

    [StringLength(200)] public string? Username { get; set; }

    [Required] [StringLength(50)] public string Role { get; set; } // "admin" or "learner"

    [Required] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<Response> Responses { get; set; } = new List<Response>();
}