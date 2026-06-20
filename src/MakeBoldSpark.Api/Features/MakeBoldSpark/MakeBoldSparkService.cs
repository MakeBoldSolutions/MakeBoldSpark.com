using Microsoft.EntityFrameworkCore;
using MakeBoldSpark.Core.Data;

namespace MakeBoldSpark.Api.Features.MakeBoldSpark;

public class MakeBoldSparkService(MakeBoldSparkCoreDbContext db, ILogger<MakeBoldSparkService> logger)
{
    private async Task<List<T>> SafeListAsync<T>(Func<Task<List<T>>> query, string entity)
    {
        try { return await query(); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to query {Entity} — returning empty list", entity);
            return [];
        }
    }

    private async Task<T?> SafeSingleAsync<T>(Func<Task<T?>> query, string entity) where T : class
    {
        try { return await query(); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to query {Entity} — returning null", entity);
            return null;
        }
    }

    // ── Domain (WebSite) ────────────────────────────────────────────────────

    public Task<List<WebSite>> GetDomainsAsync(CancellationToken ct = default)
        => SafeListAsync(() => db.Domain.OrderBy(d => d.Name).ToListAsync(ct), "Domain");

    public Task<WebSite?> GetDomainAsync(int id, CancellationToken ct = default)
        => SafeSingleAsync(async () => await db.Domain.FindAsync([id], ct), "Domain");

    public async Task<WebSite> CreateDomainAsync(WebSite entity, CancellationToken ct = default)
    {
        db.Domain.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<WebSite?> UpdateDomainAsync(int id, WebSite entity, CancellationToken ct = default)
    {
        var existing = await db.Domain.FindAsync([id], ct);
        if (existing is null) return null;
        entity.Id = id;
        db.Entry(existing).CurrentValues.SetValues(entity);
        await db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task<bool> DeleteDomainAsync(int id, CancellationToken ct = default)
    {
        var existing = await db.Domain.FindAsync([id], ct);
        if (existing is null) return false;
        db.Domain.Remove(existing);
        await db.SaveChangesAsync(ct);
        return true;
    }

    // ── Blogs ───────────────────────────────────────────────────────────────

    public Task<List<Blog>> GetBlogsAsync(CancellationToken ct = default)
        => SafeListAsync(() => db.Blogs.OrderBy(b => b.Title).ToListAsync(ct), "Blog");

    public Task<Blog?> GetBlogAsync(int id, CancellationToken ct = default)
        => SafeSingleAsync(async () => await db.Blogs.FindAsync([id], ct), "Blog");

    public async Task<Blog> CreateBlogAsync(Blog entity, CancellationToken ct = default)
    {
        db.Blogs.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<Blog?> UpdateBlogAsync(int id, Blog entity, CancellationToken ct = default)
    {
        var existing = await db.Blogs.FindAsync([id], ct);
        if (existing is null) return null;
        entity.Id = id;
        db.Entry(existing).CurrentValues.SetValues(entity);
        await db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task<bool> DeleteBlogAsync(int id, CancellationToken ct = default)
    {
        var existing = await db.Blogs.FindAsync([id], ct);
        if (existing is null) return false;
        db.Blogs.Remove(existing);
        await db.SaveChangesAsync(ct);
        return true;
    }

    // ── Authors ─────────────────────────────────────────────────────────────

    public Task<List<Author>> GetAuthorsAsync(CancellationToken ct = default)
        => SafeListAsync(() => db.Authors.OrderBy(a => a.DisplayName).ToListAsync(ct), "Author");

    public Task<Author?> GetAuthorAsync(int id, CancellationToken ct = default)
        => SafeSingleAsync(async () => await db.Authors.FindAsync([id], ct), "Author");

    public async Task<Author> CreateAuthorAsync(Author entity, CancellationToken ct = default)
    {
        db.Authors.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<Author?> UpdateAuthorAsync(int id, Author entity, CancellationToken ct = default)
    {
        var existing = await db.Authors.FindAsync([id], ct);
        if (existing is null) return null;
        entity.Id = id;
        db.Entry(existing).CurrentValues.SetValues(entity);
        await db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task<bool> DeleteAuthorAsync(int id, CancellationToken ct = default)
    {
        var existing = await db.Authors.FindAsync([id], ct);
        if (existing is null) return false;
        db.Authors.Remove(existing);
        await db.SaveChangesAsync(ct);
        return true;
    }

    // ── Posts ────────────────────────────────────────────────────────────────

    public Task<List<Post>> GetPostsAsync(int? blogId, CancellationToken ct = default)
        => SafeListAsync(() =>
        {
            var query = db.Posts.AsQueryable();
            if (blogId.HasValue) query = query.Where(p => p.Blog.Id == blogId.Value);
            return query.OrderByDescending(p => p.Published).ToListAsync(ct);
        }, "Post");

    public Task<Post?> GetPostAsync(int id, CancellationToken ct = default)
        => SafeSingleAsync(async () => await db.Posts.FindAsync([id], ct), "Post");

    public async Task<Post> CreatePostAsync(Post entity, CancellationToken ct = default)
    {
        db.Posts.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<Post?> UpdatePostAsync(int id, Post entity, CancellationToken ct = default)
    {
        var existing = await db.Posts.FindAsync([id], ct);
        if (existing is null) return null;
        entity.Id = id;
        db.Entry(existing).CurrentValues.SetValues(entity);
        await db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task<bool> DeletePostAsync(int id, CancellationToken ct = default)
    {
        var existing = await db.Posts.FindAsync([id], ct);
        if (existing is null) return false;
        db.Posts.Remove(existing);
        await db.SaveChangesAsync(ct);
        return true;
    }

    // ── Categories ──────────────────────────────────────────────────────────

    public Task<List<Category>> GetCategoriesAsync(CancellationToken ct = default)
        => SafeListAsync(() => db.Categories.OrderBy(c => c.Content).ToListAsync(ct), "Category");

    public Task<Category?> GetCategoryAsync(int id, CancellationToken ct = default)
        => SafeSingleAsync(async () => await db.Categories.FindAsync([id], ct), "Category");

    public async Task<Category> CreateCategoryAsync(Category entity, CancellationToken ct = default)
    {
        db.Categories.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<Category?> UpdateCategoryAsync(int id, Category entity, CancellationToken ct = default)
    {
        var existing = await db.Categories.FindAsync([id], ct);
        if (existing is null) return null;
        entity.Id = id;
        db.Entry(existing).CurrentValues.SetValues(entity);
        await db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task<bool> DeleteCategoryAsync(int id, CancellationToken ct = default)
    {
        var existing = await db.Categories.FindAsync([id], ct);
        if (existing is null) return false;
        db.Categories.Remove(existing);
        await db.SaveChangesAsync(ct);
        return true;
    }

    // ── Menu ────────────────────────────────────────────────────────────────

    public Task<List<Menu>> GetMenusAsync(int? domainId, CancellationToken ct = default)
        => SafeListAsync(() =>
        {
            var query = db.Menu.AsQueryable();
            if (domainId.HasValue) query = query.Where(m => m.Domain.Id == domainId.Value);
            return query.OrderBy(m => m.DisplayOrder).ThenBy(m => m.Title).ToListAsync(ct);
        }, "Menu");

    public Task<Menu?> GetMenuAsync(int id, CancellationToken ct = default)
        => SafeSingleAsync(async () => await db.Menu.FindAsync([id], ct), "Menu");

    public async Task<Menu> CreateMenuAsync(Menu entity, CancellationToken ct = default)
    {
        db.Menu.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<Menu?> UpdateMenuAsync(int id, Menu entity, CancellationToken ct = default)
    {
        var existing = await db.Menu.FindAsync([id], ct);
        if (existing is null) return null;
        entity.Id = id;
        db.Entry(existing).CurrentValues.SetValues(entity);
        await db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task<bool> DeleteMenuAsync(int id, CancellationToken ct = default)
    {
        var existing = await db.Menu.FindAsync([id], ct);
        if (existing is null) return false;
        db.Menu.Remove(existing);
        await db.SaveChangesAsync(ct);
        return true;
    }

    // ── Keywords ────────────────────────────────────────────────────────────

    public Task<List<Keyword>> GetKeywordsAsync(CancellationToken ct = default)
        => SafeListAsync(() => db.Keywords.OrderBy(k => k.Name).ToListAsync(ct), "Keyword");

    public Task<Keyword?> GetKeywordAsync(int id, CancellationToken ct = default)
        => SafeSingleAsync(async () => await db.Keywords.FindAsync([id], ct), "Keyword");

    public async Task<Keyword> CreateKeywordAsync(Keyword entity, CancellationToken ct = default)
    {
        db.Keywords.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<Keyword?> UpdateKeywordAsync(int id, Keyword entity, CancellationToken ct = default)
    {
        var existing = await db.Keywords.FindAsync([id], ct);
        if (existing is null) return null;
        entity.Id = id;
        db.Entry(existing).CurrentValues.SetValues(entity);
        await db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task<bool> DeleteKeywordAsync(int id, CancellationToken ct = default)
    {
        var existing = await db.Keywords.FindAsync([id], ct);
        if (existing is null) return false;
        db.Keywords.Remove(existing);
        await db.SaveChangesAsync(ct);
        return true;
    }

    // ── ContentParts ────────────────────────────────────────────────────────

    public Task<List<ContentPart>> GetContentPartsAsync(CancellationToken ct = default)
        => SafeListAsync(() => db.ContentParts.OrderBy(c => c.Title).ToListAsync(ct), "ContentPart");

    public Task<ContentPart?> GetContentPartAsync(int id, CancellationToken ct = default)
        => SafeSingleAsync(async () => await db.ContentParts.FindAsync([id], ct), "ContentPart");

    public async Task<ContentPart> CreateContentPartAsync(ContentPart entity, CancellationToken ct = default)
    {
        db.ContentParts.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<ContentPart?> UpdateContentPartAsync(int id, ContentPart entity, CancellationToken ct = default)
    {
        var existing = await db.ContentParts.FindAsync([id], ct);
        if (existing is null) return null;
        entity.Id = id;
        db.Entry(existing).CurrentValues.SetValues(entity);
        await db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task<bool> DeleteContentPartAsync(int id, CancellationToken ct = default)
    {
        var existing = await db.ContentParts.FindAsync([id], ct);
        if (existing is null) return false;
        db.ContentParts.Remove(existing);
        await db.SaveChangesAsync(ct);
        return true;
    }

    // ── Subscribers ─────────────────────────────────────────────────────────

    public Task<List<Subscriber>> GetSubscribersAsync(CancellationToken ct = default)
        => SafeListAsync(() => db.Subscribers.OrderBy(s => s.Email).ToListAsync(ct), "Subscriber");

    public Task<Subscriber?> GetSubscriberAsync(int id, CancellationToken ct = default)
        => SafeSingleAsync(async () => await db.Subscribers.FindAsync([id], ct), "Subscriber");

    public async Task<Subscriber> CreateSubscriberAsync(Subscriber entity, CancellationToken ct = default)
    {
        db.Subscribers.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<Subscriber?> UpdateSubscriberAsync(int id, Subscriber entity, CancellationToken ct = default)
    {
        var existing = await db.Subscribers.FindAsync([id], ct);
        if (existing is null) return null;
        entity.Id = id;
        db.Entry(existing).CurrentValues.SetValues(entity);
        await db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task<bool> DeleteSubscriberAsync(int id, CancellationToken ct = default)
    {
        var existing = await db.Subscribers.FindAsync([id], ct);
        if (existing is null) return false;
        db.Subscribers.Remove(existing);
        await db.SaveChangesAsync(ct);
        return true;
    }

    // ── Newsletters ─────────────────────────────────────────────────────────

    public Task<List<Newsletter>> GetNewslettersAsync(CancellationToken ct = default)
        => SafeListAsync(() => db.Newsletters.OrderByDescending(n => n.CreatedDate).ToListAsync(ct), "Newsletter");

    public Task<Newsletter?> GetNewsletterAsync(int id, CancellationToken ct = default)
        => SafeSingleAsync(async () => await db.Newsletters.FindAsync([id], ct), "Newsletter");

    public async Task<Newsletter> CreateNewsletterAsync(Newsletter entity, CancellationToken ct = default)
    {
        db.Newsletters.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<bool> DeleteNewsletterAsync(int id, CancellationToken ct = default)
    {
        var existing = await db.Newsletters.FindAsync([id], ct);
        if (existing is null) return false;
        db.Newsletters.Remove(existing);
        await db.SaveChangesAsync(ct);
        return true;
    }

    // ── MailSettings ─────────────────────────────────────────────────────────

    public Task<List<MailSetting>> GetMailSettingsAsync(CancellationToken ct = default)
        => SafeListAsync(() => db.MailSettings.OrderBy(m => m.FromEmail).ToListAsync(ct), "MailSetting");

    public Task<MailSetting?> GetMailSettingAsync(int id, CancellationToken ct = default)
        => SafeSingleAsync(async () => await db.MailSettings.FindAsync([id], ct), "MailSetting");

    public async Task<MailSetting> CreateMailSettingAsync(MailSetting entity, CancellationToken ct = default)
    {
        db.MailSettings.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    public async Task<MailSetting?> UpdateMailSettingAsync(int id, MailSetting entity, CancellationToken ct = default)
    {
        var existing = await db.MailSettings.FindAsync([id], ct);
        if (existing is null) return null;
        entity.Id = id;
        db.Entry(existing).CurrentValues.SetValues(entity);
        await db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task<bool> DeleteMailSettingAsync(int id, CancellationToken ct = default)
    {
        var existing = await db.MailSettings.FindAsync([id], ct);
        if (existing is null) return false;
        db.MailSettings.Remove(existing);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
