using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MakeBoldSpark.Recipe.Data;

public partial class RecipeDbContext(DbContextOptions<RecipeDbContext> options) : DbContext(options)
{
    protected readonly DbContextOptions<RecipeDbContext> _options = options;

    public virtual DbSet<Recipe> Recipe { get; set; }
    public virtual DbSet<RecipeCategory> RecipeCategory { get; set; }
    public virtual DbSet<RecipeComment> RecipeComment { get; set; }
    public virtual DbSet<RecipeImage> RecipeImage { get; set; }

    private void UpdateDateTrackingFields()
    {
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.Entity is RecipeBaseEntity && (
                    e.State == EntityState.Added
                    || e.State == EntityState.Modified));

        foreach (var entityEntry in entries)
        {
            ((RecipeBaseEntity)entityEntry.Entity).UpdatedDate = DateTime.UtcNow;
            if (entityEntry.State == EntityState.Added)
            {
                ((RecipeBaseEntity)entityEntry.Entity).CreatedDate = DateTime.UtcNow;
                ((RecipeBaseEntity)entityEntry.Entity).Version = 1;
            }
            else ((RecipeBaseEntity)entityEntry.Entity).Version++;
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Recipe>(entity =>
        {
            entity.Property(e => e.Version).IsConcurrencyToken().HasDefaultValue(1);
            entity.HasIndex(e => new { e.DomainId, e.Name });
            entity.Property(e => e.AuthorName)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.Ingredients).IsRequired();
            entity.Property(e => e.Instructions).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Keywords).HasMaxLength(100);
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(150);
            entity.Property(e => e.DomainId);
            entity.HasOne(d => d.RecipeCategory)
                   .WithMany(p => p.Recipe)
                   .OnDelete(DeleteBehavior.Restrict)
                   .HasConstraintName("FK_Recipe_RecipeCategory")
                   .IsRequired();
        });

        modelBuilder.Entity<RecipeCategory>(entity =>
        {
            entity.Property(e => e.Version).IsConcurrencyToken().HasDefaultValue(1);
            entity.HasIndex(e => new { e.DomainId, e.Name }).IsUnique();
            entity.Property(e => e.Comment).HasMaxLength(1500);
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(70);
            entity.Property(e => e.DomainId);
            entity.HasMany(entity => entity.Recipe)
                .WithOne(entity => entity.RecipeCategory)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RecipeComment>(entity =>
        {
            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(60);
            entity.Property(e => e.Comment).IsRequired();
            entity.HasOne(d => d.Recipe)
                .WithMany(p => p.RecipeComment)
                .OnDelete(DeleteBehavior.ClientCascade)
                .HasConstraintName("FK_RecipeComment_Recipe");
        });

        modelBuilder.Entity<RecipeImage>(entity =>
        {
            entity.Property(e => e.FileDescription).HasMaxLength(255);
            entity.Property(e => e.FileName)
                .IsRequired()
                .HasMaxLength(50);
            entity.HasOne(d => d.Recipe)
                .WithMany(p => p.RecipeImage)
                .OnDelete(DeleteBehavior.ClientCascade)
                .HasConstraintName("FK_RecipeImage_Recipe");
        });
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(warnings =>
            warnings.Ignore(RelationalEventId.NonTransactionalMigrationOperationWarning));
    }

    public override int SaveChanges()
    {
        UpdateDateTrackingFields();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateDateTrackingFields();
        return await base.SaveChangesAsync(cancellationToken);
    }
}
