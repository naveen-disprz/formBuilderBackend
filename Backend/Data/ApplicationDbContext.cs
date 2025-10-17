using Microsoft.EntityFrameworkCore;
using Backend.Models.Sql;

namespace Backend.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Response> Responses { get; set; }
    public DbSet<Answer> Answers { get; set; }
    public DbSet<FileUpload> Files { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Username).IsUnique();

            entity.Property(e => e.Role)
                .HasConversion<string>()
                .HasMaxLength(50);
        });

        // Response configuration
        modelBuilder.Entity<Response>(entity =>
        {
            entity.HasKey(e => e.ResponseId);
            entity.HasIndex(e => e.FormId);
            entity.HasIndex(e => e.SubmittedBy);
            entity.HasIndex(e => e.SubmittedAt);

            entity.HasOne(e => e.User)
                .WithMany(u => u.Responses)
                .HasForeignKey(e => e.SubmittedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Answer configuration
        modelBuilder.Entity<Answer>(entity =>
        {
            entity.HasKey(e => e.AnswerId);
            entity.HasIndex(e => e.ResponseId);
            entity.HasIndex(e => e.QuestionId);
            
            entity.Property(e => e.AnswerType)
                .HasConversion<string>()
                .HasMaxLength(50);
        });

        // FileUpload configuration
        modelBuilder.Entity<FileUpload>(entity =>
        {
            entity.HasKey(e => e.FileId);
            entity.HasIndex(e => e.AnswerId);
        });
    }
}