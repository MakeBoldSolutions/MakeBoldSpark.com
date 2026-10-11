using MakeBoldSpark.Api.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;
using MakeBoldSpark.Api.Features.FamilyMemories;

namespace MakeBoldSpark.Api.Infrastructure.Data;

public class MakeBoldSparkDbContext(DbContextOptions<MakeBoldSparkDbContext> options) : DbContext(options)
{
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<BoldInstallToken> BoldInstallTokens => Set<BoldInstallToken>();
    public DbSet<BoldRun> BoldRuns => Set<BoldRun>();
    public DbSet<FamilyMemory> FamilyMemories => Set<FamilyMemory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FamilyMemory>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.PersonName).IsRequired().HasMaxLength(200);
            entity.Property(m => m.PersonPronouns).IsRequired().HasMaxLength(100);
            entity.Property(m => m.RelationshipToJared).IsRequired().HasMaxLength(300);
            entity.Property(m => m.Body).IsRequired().HasMaxLength(100000);
            entity.HasIndex(m => new { m.IsPublished, m.SortOrder, m.Id });
            // SQLite returns Kind=Unspecified; mark as UTC so JSON carries the trailing Z.
            entity.Property(m => m.CreatedDate).HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
            entity.Property(m => m.UpdatedDate).HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        });
        modelBuilder.Entity<Article>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Slug).IsRequired().HasMaxLength(200);
            entity.HasIndex(a => a.Slug).IsUnique();
            entity.Property(a => a.Title).IsRequired().HasMaxLength(300);
            entity.Property(a => a.Summary).IsRequired().HasMaxLength(1000);
            entity.Property(a => a.Body).IsRequired();
            entity.Property(a => a.Status).IsRequired();
            entity.Property(a => a.CreatedAt).IsRequired();
            entity.Property(a => a.UpdatedAt).IsRequired();

            entity.HasMany(a => a.Tags)
                  .WithMany(t => t.Articles)
                  .UsingEntity(j => j.ToTable("ArticleTag"));
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(t => t.Name).IsUnique();
        });

        modelBuilder.Entity<BoldInstallToken>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Name).IsRequired().HasMaxLength(200);
            entity.Property(t => t.TokenHash).IsRequired().HasMaxLength(128);
            entity.HasIndex(t => t.TokenHash).IsUnique();
            entity.Property(t => t.CreatedAt).IsRequired();
        });

        modelBuilder.Entity<BoldRun>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.RunId).IsRequired().HasMaxLength(64);
            entity.HasIndex(r => r.RunId).IsUnique();
            entity.Property(r => r.Provider).IsRequired().HasMaxLength(32);
            entity.Property(r => r.Model).IsRequired().HasMaxLength(100);
            entity.Property(r => r.ModelRole).IsRequired().HasMaxLength(32);
            entity.Property(r => r.Workflow).HasMaxLength(16);
            entity.Property(r => r.WorkspaceId).HasMaxLength(200);
            entity.Property(r => r.StarterId).HasMaxLength(200);
            entity.Property(r => r.RunLabel).HasMaxLength(200);
            entity.Property(r => r.Status).IsRequired().HasMaxLength(16);
            entity.Property(r => r.EstimatedCostUsd).HasColumnType("TEXT");
            entity.Property(r => r.ErrorCode).HasMaxLength(64);
            entity.Property(r => r.ErrorMessage).HasMaxLength(500);
            entity.Property(r => r.ProviderRequestId).HasMaxLength(200);
            entity.Property(r => r.CreatedAt).IsRequired();

            entity.HasIndex(r => r.InstallTokenId);
            entity.HasIndex(r => new { r.InstallTokenId, r.CreatedAt });
            entity.HasIndex(r => new { r.InstallTokenId, r.WorkspaceId });
            entity.HasIndex(r => new { r.InstallTokenId, r.Workflow });
        });
    }
}
