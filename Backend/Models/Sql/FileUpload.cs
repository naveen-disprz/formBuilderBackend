using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models.Sql;

[Table("Files")]
public class FileUpload
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid FileId { get; set; } = Guid.NewGuid();

    [Required] public Guid AnswerId { get; set; }

    [Required] [StringLength(260)] public string FileName { get; set; }

    [Required] [StringLength(100)] public string MimeType { get; set; }

    [Required] public long FileSizeBytes { get; set; }

    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public string FileContent { get; set; } // Base64 encoded

    [Required] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("AnswerId")] public virtual Answer Answer { get; set; }
}