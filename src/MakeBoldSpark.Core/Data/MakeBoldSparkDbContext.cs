using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MakeBoldSpark.Core.Data;


/// <summary>
/// 
/// </summary>
/// <remarks>
/// 
/// </remarks>
public partial class MakeBoldSparkCoreDbContext(DbContextOptions<MakeBoldSparkCoreDbContext> options) : DbContext(options)
{
    protected readonly DbContextOptions<MakeBoldSparkCoreDbContext> _options = options;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="modelBuilder"></param>
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

    private void UpdateDateTrackingFields()
    {
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.Entity is BaseEntity && (
                    e.State == EntityState.Added
                    || e.State == EntityState.Modified));

        foreach (var entityEntry in entries)
        {
            ((BaseEntity)entityEntry.Entity).UpdatedDate = DateTime.UtcNow;

            if (entityEntry.State == EntityState.Added)
            {
                ((BaseEntity)entityEntry.Entity).CreatedDate = DateTime.UtcNow;
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WebSite>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("Id");

            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(250);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(50);
            entity.HasIndex(e => e.Name).IsUnique();

            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(250);
            entity.HasIndex(e => e.Title).IsUnique();

            entity.Property(e => e.DomainUrl)
                .IsRequired()
                .HasMaxLength(250);
            entity.HasIndex(e => e.DomainUrl).IsUnique();

            entity.Property(e => e.GalleryFolder)
                .IsRequired()
                .HasMaxLength(250);

            entity.Property(e => e.Style)
                .IsRequired()
                .HasMaxLength(100);

        });

        modelBuilder.Entity<Menu>(entity =>
        {
            entity.Property(e => e.Action)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Controller)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.KeyWords)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Icon).HasMaxLength(50);

            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Url).HasMaxLength(100);

            entity.HasOne(d => d.Domain)
                .WithMany(p => p.Menus)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Menu_Domain");

            entity.HasOne(d => d.Parent)
                .WithMany(p => p.InverseParent)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Menu_ParentMenu_ParentId");

        });
        // Many-to-many relationship between Menu and Keyword
        modelBuilder.Entity<Menu>()
            .HasMany(m => m.Keywords)
            .WithMany(k => k.Menus)
            .UsingEntity<Dictionary<string, object>>(
                "MenuKeyword",
                j => j.HasOne<Keyword>().WithMany().HasForeignKey("KeywordId"),
                j => j.HasOne<Menu>().WithMany().HasForeignKey("MenuId"));


        // Many-to-many relationship between ContentPart and Keyword
        modelBuilder.Entity<ContentPart>()
                .HasMany(cp => cp.Keywords)
                .WithMany(k => k.ContentParts)
                .UsingEntity<Dictionary<string, object>>(
                    "ContentPartKeyword",
                    j => j.HasOne<Keyword>().WithMany().HasForeignKey("KeywordId"),
                    j => j.HasOne<ContentPart>().WithMany().HasForeignKey("ContentPartId"));

        modelBuilder.Entity<PostCategory>()
    .HasKey(t => new { t.PostId, t.CategoryId });

        modelBuilder.Entity<PostCategory>()
            .HasOne(pt => pt.Post)
            .WithMany(p => p.PostCategories)
            .HasForeignKey(pt => pt.PostId);

        modelBuilder.Entity<PostCategory>()
            .HasOne(pt => pt.Category)
            .WithMany(t => t.PostCategories)
            .HasForeignKey(pt => pt.CategoryId);

        string sql = "getdate()";

        if (_options.Extensions != null)
        {
            foreach (var ext in _options.Extensions)
            {
                if (ext.GetType().ToString().StartsWith("Microsoft.EntityFrameworkCore.Sqlite"))
                {
                    sql = "DATE('now')";
                    break;
                }
            }
        }

        modelBuilder.Entity<Blog>().Property(b => b.UpdatedDate).HasDefaultValueSql(sql);
        modelBuilder.Entity<Post>().Property(p => p.UpdatedDate).HasDefaultValueSql(sql);
        modelBuilder.Entity<Author>().Property(a => a.UpdatedDate).HasDefaultValueSql(sql);
        // Enforces, rather than merely assumes, the email-uniqueness invariant the CMS sign-in
        // design depends on (tasks.md T054a, critic Questionable Assumption #1).
        modelBuilder.Entity<Author>().HasIndex(a => a.Email).IsUnique();
        modelBuilder.Entity<Category>().Property(c => c.UpdatedDate).HasDefaultValueSql(sql);
        modelBuilder.Entity<Subscriber>().Property(s => s.UpdatedDate).HasDefaultValueSql(sql);
        modelBuilder.Entity<Newsletter>().Property(n => n.UpdatedDate).HasDefaultValueSql(sql);
        modelBuilder.Entity<MailSetting>().Property(n => n.UpdatedDate).HasDefaultValueSql(sql);

        OnModelCreatingPartial(modelBuilder);
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
        return await base.SaveChangesAsync();
    }

    public virtual DbSet<Author> Authors { get; set; }
    public virtual DbSet<Blog> Blogs { get; set; }
    public virtual DbSet<Category> Categories { get; set; }
    public virtual DbSet<WebSite> Domain { get; set; }
    public virtual DbSet<MailSetting> MailSettings { get; set; }
    public virtual DbSet<Menu> Menu { get; set; }
    public virtual DbSet<Newsletter> Newsletters { get; set; }
    public virtual DbSet<PostCategory> PostCategories { get; set; }
    public virtual DbSet<Post> Posts { get; set; }
    public virtual DbSet<Subscriber> Subscribers { get; set; }
    public virtual DbSet<Keyword> Keywords { get; set; }
    public virtual DbSet<ContentPart> ContentParts { get; set; }

}
