using MakeBoldSpark.Api.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace MakeBoldSpark.Api.Infrastructure.Data;

public class MakeBoldSparkDbContext(DbContextOptions<MakeBoldSparkDbContext> options) : DbContext(options)
{
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<Tag> Tags => Set<Tag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
    }
}
