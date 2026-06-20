using MakeBoldSpark.Api.Infrastructure.OpenApi;
using MakeBoldSpark.Core.Data;

namespace MakeBoldSpark.Api.Features.MakeBoldSpark;

public static class MakeBoldSparkEndpoints
{
    public static RouteGroupBuilder MapPublicMakeBoldSparkApi(this RouteGroupBuilder group)
    {
        // Domain / WebSites
        group.MapGet("/domains", async (MakeBoldSparkService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetDomainsAsync(ct)))
            .WithName("GetDomains").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkPublicDomains)
            .WithSummary("List all domains").WithDescription("Returns all MakeBoldSpark website / domain records.")
            .Produces<IEnumerable<WebSite>>(200).AllowAnonymous();

        group.MapGet("/domains/{id:int}", async (int id, MakeBoldSparkService svc, CancellationToken ct) =>
        {
            var item = await svc.GetDomainAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        }).WithName("GetDomain").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkPublicDomains)
          .WithSummary("Get a domain by ID").WithDescription("Returns a single MakeBoldSpark website record. Returns 404 if not found.")
          .Produces<WebSite>(200).Produces(404).AllowAnonymous();

        // Blogs
        group.MapGet("/blogs", async (MakeBoldSparkService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetBlogsAsync(ct)))
            .WithName("GetBlogs").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkPublicBlogs)
            .WithSummary("List all blogs").WithDescription("Returns all blog records associated with any domain.")
            .Produces<IEnumerable<Blog>>(200).AllowAnonymous();

        group.MapGet("/blogs/{id:int}", async (int id, MakeBoldSparkService svc, CancellationToken ct) =>
        {
            var item = await svc.GetBlogAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        }).WithName("GetBlog").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkPublicBlogs)
          .WithSummary("Get a blog by ID").WithDescription("Returns a single blog record. Returns 404 if not found.")
          .Produces<Blog>(200).Produces(404).AllowAnonymous();

        // Authors
        group.MapGet("/authors", async (MakeBoldSparkService svc, CancellationToken ct) =>
            Results.Ok((await svc.GetAuthorsAsync(ct)).Select(AuthorResponse.FromAuthor)))
            .WithName("GetAuthors").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkPublicAuthors)
            .WithSummary("List all authors").WithDescription("Returns public author profile fields. Password hashes are never included in the list response.")
            .Produces<IEnumerable<AuthorResponse>>(200).AllowAnonymous();

        group.MapGet("/authors/{id:int}", async (int id, MakeBoldSparkService svc, CancellationToken ct) =>
        {
            var item = await svc.GetAuthorAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(AuthorResponse.FromAuthor(item));
        }).WithName("GetAuthor").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkPublicAuthors)
          .WithSummary("Get an author by ID").WithDescription("Returns a single public author profile. Password hashes are never included. Returns 404 if not found.")
          .Produces<AuthorResponse>(200).Produces(404).AllowAnonymous();

        // Posts
        group.MapGet("/posts", async (int? blogId, MakeBoldSparkService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetPostsAsync(blogId, ct)))
            .WithName("GetPosts").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkPublicPosts)
            .WithSummary("List posts").WithDescription("Returns all posts, optionally filtered by blogId query parameter.")
            .Produces<IEnumerable<Post>>(200).AllowAnonymous();

        group.MapGet("/posts/{id:int}", async (int id, MakeBoldSparkService svc, CancellationToken ct) =>
        {
            var item = await svc.GetPostAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        }).WithName("GetPost").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkPublicPosts)
          .WithSummary("Get a post by ID").WithDescription("Returns a single post record. Returns 404 if not found.")
          .Produces<Post>(200).Produces(404).AllowAnonymous();

        // Categories
        group.MapGet("/categories", async (MakeBoldSparkService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetCategoriesAsync(ct)))
            .WithName("GetCategories").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkPublicCategories)
            .WithSummary("List all categories").WithDescription("Returns all content categories.")
            .Produces<IEnumerable<Category>>(200).AllowAnonymous();

        group.MapGet("/categories/{id:int}", async (int id, MakeBoldSparkService svc, CancellationToken ct) =>
        {
            var item = await svc.GetCategoryAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        }).WithName("GetCategory").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkPublicCategories)
          .WithSummary("Get a category by ID").WithDescription("Returns a single category. Returns 404 if not found.")
          .Produces<Category>(200).Produces(404).AllowAnonymous();

        // Menu
        group.MapGet("/menus", async (int? domainId, MakeBoldSparkService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetMenusAsync(domainId, ct)))
            .WithName("GetMenus").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkPublicMenus)
            .WithSummary("List menus").WithDescription("Returns all navigation menus, optionally filtered by domainId.")
            .Produces<IEnumerable<Menu>>(200).AllowAnonymous();

        group.MapGet("/menus/{id:int}", async (int id, MakeBoldSparkService svc, CancellationToken ct) =>
        {
            var item = await svc.GetMenuAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        }).WithName("GetMenu").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkPublicMenus)
          .WithSummary("Get a menu by ID").WithDescription("Returns a single navigation menu. Returns 404 if not found.")
          .Produces<Menu>(200).Produces(404).AllowAnonymous();

        // Keywords
        group.MapGet("/keywords", async (MakeBoldSparkService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetKeywordsAsync(ct)))
            .WithName("GetKeywords").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkPublicKeywords)
            .WithSummary("List all keywords").WithDescription("Returns all SEO keyword / tag records.")
            .Produces<IEnumerable<Keyword>>(200).AllowAnonymous();

        group.MapGet("/keywords/{id:int}", async (int id, MakeBoldSparkService svc, CancellationToken ct) =>
        {
            var item = await svc.GetKeywordAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        }).WithName("GetKeyword").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkPublicKeywords)
          .WithSummary("Get a keyword by ID").WithDescription("Returns a single keyword record. Returns 404 if not found.")
          .Produces<Keyword>(200).Produces(404).AllowAnonymous();

        // ContentParts
        group.MapGet("/content-parts", async (MakeBoldSparkService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetContentPartsAsync(ct)))
            .WithName("GetContentParts").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkPublicContentParts)
            .WithSummary("List all content parts").WithDescription("Returns all reusable content part records.")
            .Produces<IEnumerable<ContentPart>>(200).AllowAnonymous();

        group.MapGet("/content-parts/{id:int}", async (int id, MakeBoldSparkService svc, CancellationToken ct) =>
        {
            var item = await svc.GetContentPartAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        }).WithName("GetContentPart").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkPublicContentParts)
          .WithSummary("Get a content part by ID").WithDescription("Returns a single reusable content part. Returns 404 if not found.")
          .Produces<ContentPart>(200).Produces(404).AllowAnonymous();

        return group;
    }

    public static RouteGroupBuilder MapAdminMakeBoldSparkApi(this RouteGroupBuilder group)
    {
        // Domain CRUD
        group.MapPost("/domains", async (WebSite entity, MakeBoldSparkService svc, CancellationToken ct) =>
        {
            var created = await svc.CreateDomainAsync(entity, ct);
            return Results.Created($"/api/public/makeboldspark/domains/{created.Id}", created);
        }).WithName("CreateDomain").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkAdminDomains)
          .WithSummary("Create a domain").WithDescription("Creates a new MakeBoldSpark website record. Requires AdminOnly policy.")
          .Produces<WebSite>(201).ProducesProblem(400);

        group.MapPut("/domains/{id:int}", async (int id, WebSite entity, MakeBoldSparkService svc, CancellationToken ct) =>
        {
            var updated = await svc.UpdateDomainAsync(id, entity, ct);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        }).WithName("UpdateDomain").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkAdminDomains)
          .WithSummary("Update a domain").WithDescription("Replaces a domain record. Returns 404 if not found.")
          .Produces<WebSite>(200).Produces(404);

        group.MapDelete("/domains/{id:int}", async (int id, MakeBoldSparkService svc, CancellationToken ct) =>
            await svc.DeleteDomainAsync(id, ct) ? Results.NoContent() : Results.NotFound())
            .WithName("DeleteDomain").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkAdminDomains)
            .WithSummary("Delete a domain").WithDescription("Permanently removes a domain record. Returns 204 on success.")
            .Produces(204).Produces(404);

        // Blog CRUD
        group.MapPost("/blogs", async (Blog entity, MakeBoldSparkService svc, CancellationToken ct) =>
        {
            var created = await svc.CreateBlogAsync(entity, ct);
            return Results.Created($"/api/public/makeboldspark/blogs/{created.Id}", created);
        }).WithName("CreateBlog").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkAdminBlogs)
          .WithSummary("Create a blog").WithDescription("Creates a new blog record.")
          .Produces<Blog>(201).ProducesProblem(400);

        group.MapPut("/blogs/{id:int}", async (int id, Blog entity, MakeBoldSparkService svc, CancellationToken ct) =>
        {
            var updated = await svc.UpdateBlogAsync(id, entity, ct);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        }).WithName("UpdateBlog").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkAdminBlogs)
          .WithSummary("Update a blog").Produces<Blog>(200).Produces(404);

        group.MapDelete("/blogs/{id:int}", async (int id, MakeBoldSparkService svc, CancellationToken ct) =>
            await svc.DeleteBlogAsync(id, ct) ? Results.NoContent() : Results.NotFound())
            .WithName("DeleteBlog").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkAdminBlogs)
            .WithSummary("Delete a blog").Produces(204).Produces(404);

        // Author CRUD
        group.MapPost("/authors", async (Author entity, MakeBoldSparkService svc, CancellationToken ct) =>
        {
            var created = await svc.CreateAuthorAsync(entity, ct);
            return Results.Created($"/api/public/makeboldspark/authors/{created.Id}", AuthorResponse.FromAuthor(created));
        }).WithName("CreateAuthor").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkAdminAuthors)
          .WithSummary("Create an author").WithDescription("Creates a CMS author. The request includes a password, but the response never returns the stored password hash.")
          .Produces<AuthorResponse>(201).ProducesProblem(400);

        group.MapPut("/authors/{id:int}", async (int id, Author entity, MakeBoldSparkService svc, CancellationToken ct) =>
        {
            var updated = await svc.UpdateAuthorAsync(id, entity, ct);
            return updated is null ? Results.NotFound() : Results.Ok(AuthorResponse.FromAuthor(updated));
        }).WithName("UpdateAuthor").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkAdminAuthors)
          .WithSummary("Update an author").WithDescription("Updates a CMS author. The request may include a password, but the response never returns the stored password hash.")
          .Produces<AuthorResponse>(200).Produces(404);

        group.MapDelete("/authors/{id:int}", async (int id, MakeBoldSparkService svc, CancellationToken ct) =>
            await svc.DeleteAuthorAsync(id, ct) ? Results.NoContent() : Results.NotFound())
            .WithName("DeleteAuthor").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkAdminAuthors)
            .WithSummary("Delete an author").Produces(204).Produces(404);

        // Post CRUD
        group.MapPost("/posts", async (Post entity, MakeBoldSparkService svc, CancellationToken ct) =>
        {
            var created = await svc.CreatePostAsync(entity, ct);
            return Results.Created($"/api/public/makeboldspark/posts/{created.Id}", created);
        }).WithName("CreatePost").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkAdminPosts)
          .WithSummary("Create a post").Produces<Post>(201).ProducesProblem(400);

        group.MapPut("/posts/{id:int}", async (int id, Post entity, MakeBoldSparkService svc, CancellationToken ct) =>
        {
            var updated = await svc.UpdatePostAsync(id, entity, ct);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        }).WithName("UpdatePost").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkAdminPosts)
          .WithSummary("Update a post").Produces<Post>(200).Produces(404);

        group.MapDelete("/posts/{id:int}", async (int id, MakeBoldSparkService svc, CancellationToken ct) =>
            await svc.DeletePostAsync(id, ct) ? Results.NoContent() : Results.NotFound())
            .WithName("DeletePost").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkAdminPosts)
            .WithSummary("Delete a post").Produces(204).Produces(404);

        // Category CRUD
        group.MapPost("/categories", async (Category entity, MakeBoldSparkService svc, CancellationToken ct) =>
        {
            var created = await svc.CreateCategoryAsync(entity, ct);
            return Results.Created($"/api/public/makeboldspark/categories/{created.Id}", created);
        }).WithName("CreateCategory").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkAdminCategories)
          .WithSummary("Create a category").Produces<Category>(201).ProducesProblem(400);

        group.MapPut("/categories/{id:int}", async (int id, Category entity, MakeBoldSparkService svc, CancellationToken ct) =>
        {
            var updated = await svc.UpdateCategoryAsync(id, entity, ct);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        }).WithName("UpdateCategory").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkAdminCategories)
          .WithSummary("Update a category").Produces<Category>(200).Produces(404);

        group.MapDelete("/categories/{id:int}", async (int id, MakeBoldSparkService svc, CancellationToken ct) =>
            await svc.DeleteCategoryAsync(id, ct) ? Results.NoContent() : Results.NotFound())
            .WithName("DeleteCategory").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkAdminCategories)
            .WithSummary("Delete a category").Produces(204).Produces(404);

        // Menu CRUD
        group.MapPost("/menus", async (Menu entity, MakeBoldSparkService svc, CancellationToken ct) =>
        {
            var created = await svc.CreateMenuAsync(entity, ct);
            return Results.Created($"/api/public/makeboldspark/menus/{created.Id}", created);
        }).WithName("CreateMenu").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkAdminMenus)
          .WithSummary("Create a menu").Produces<Menu>(201).ProducesProblem(400);

        group.MapPut("/menus/{id:int}", async (int id, Menu entity, MakeBoldSparkService svc, CancellationToken ct) =>
        {
            var updated = await svc.UpdateMenuAsync(id, entity, ct);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        }).WithName("UpdateMenu").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkAdminMenus)
          .WithSummary("Update a menu").Produces<Menu>(200).Produces(404);

        group.MapDelete("/menus/{id:int}", async (int id, MakeBoldSparkService svc, CancellationToken ct) =>
            await svc.DeleteMenuAsync(id, ct) ? Results.NoContent() : Results.NotFound())
            .WithName("DeleteMenu").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkAdminMenus)
            .WithSummary("Delete a menu").Produces(204).Produces(404);

        // Keyword CRUD
        group.MapPost("/keywords", async (Keyword entity, MakeBoldSparkService svc, CancellationToken ct) =>
        {
            var created = await svc.CreateKeywordAsync(entity, ct);
            return Results.Created($"/api/public/makeboldspark/keywords/{created.Id}", created);
        }).WithName("CreateKeyword").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkAdminKeywords)
          .WithSummary("Create a keyword").Produces<Keyword>(201).ProducesProblem(400);

        group.MapPut("/keywords/{id:int}", async (int id, Keyword entity, MakeBoldSparkService svc, CancellationToken ct) =>
        {
            var updated = await svc.UpdateKeywordAsync(id, entity, ct);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        }).WithName("UpdateKeyword").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkAdminKeywords)
          .WithSummary("Update a keyword").Produces<Keyword>(200).Produces(404);

        group.MapDelete("/keywords/{id:int}", async (int id, MakeBoldSparkService svc, CancellationToken ct) =>
            await svc.DeleteKeywordAsync(id, ct) ? Results.NoContent() : Results.NotFound())
            .WithName("DeleteKeyword").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkAdminKeywords)
            .WithSummary("Delete a keyword").Produces(204).Produces(404);

        // ContentPart CRUD
        group.MapPost("/content-parts", async (ContentPart entity, MakeBoldSparkService svc, CancellationToken ct) =>
        {
            var created = await svc.CreateContentPartAsync(entity, ct);
            return Results.Created($"/api/public/makeboldspark/content-parts/{created.Id}", created);
        }).WithName("CreateContentPart").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkAdminContentParts)
          .WithSummary("Create a content part").Produces<ContentPart>(201).ProducesProblem(400);

        group.MapPut("/content-parts/{id:int}", async (int id, ContentPart entity, MakeBoldSparkService svc, CancellationToken ct) =>
        {
            var updated = await svc.UpdateContentPartAsync(id, entity, ct);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        }).WithName("UpdateContentPart").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkAdminContentParts)
          .WithSummary("Update a content part").Produces<ContentPart>(200).Produces(404);

        group.MapDelete("/content-parts/{id:int}", async (int id, MakeBoldSparkService svc, CancellationToken ct) =>
            await svc.DeleteContentPartAsync(id, ct) ? Results.NoContent() : Results.NotFound())
            .WithName("DeleteContentPart").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkAdminContentParts)
            .WithSummary("Delete a content part").Produces(204).Produces(404);

        // Subscriber CRUD
        group.MapGet("/subscribers", async (MakeBoldSparkService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetSubscribersAsync(ct)))
            .WithName("GetSubscribers").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkMessagingSubscribers)
            .WithSummary("List all subscribers").WithDescription("Returns all newsletter subscribers. Requires AdminOnly policy.")
            .Produces<IEnumerable<Subscriber>>(200);

        group.MapGet("/subscribers/{id:int}", async (int id, MakeBoldSparkService svc, CancellationToken ct) =>
        {
            var item = await svc.GetSubscriberAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        }).WithName("GetSubscriber").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkMessagingSubscribers)
          .WithSummary("Get a subscriber by ID").Produces<Subscriber>(200).Produces(404);

        group.MapPost("/subscribers", async (Subscriber entity, MakeBoldSparkService svc, CancellationToken ct) =>
        {
            var created = await svc.CreateSubscriberAsync(entity, ct);
            return Results.Created($"/api/admin/makeboldspark/subscribers/{created.Id}", created);
        }).WithName("CreateSubscriber").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkMessagingSubscribers)
          .WithSummary("Create a subscriber").Produces<Subscriber>(201).ProducesProblem(400);

        group.MapPut("/subscribers/{id:int}", async (int id, Subscriber entity, MakeBoldSparkService svc, CancellationToken ct) =>
        {
            var updated = await svc.UpdateSubscriberAsync(id, entity, ct);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        }).WithName("UpdateSubscriber").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkMessagingSubscribers)
          .WithSummary("Update a subscriber").Produces<Subscriber>(200).Produces(404);

        group.MapDelete("/subscribers/{id:int}", async (int id, MakeBoldSparkService svc, CancellationToken ct) =>
            await svc.DeleteSubscriberAsync(id, ct) ? Results.NoContent() : Results.NotFound())
            .WithName("DeleteSubscriber").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkMessagingSubscribers)
            .WithSummary("Delete a subscriber").Produces(204).Produces(404);

        // Newsletter CRUD
        group.MapGet("/newsletters", async (MakeBoldSparkService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetNewslettersAsync(ct)))
            .WithName("GetNewsletters").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkMessagingNewsletters)
            .WithSummary("List all newsletters").Produces<IEnumerable<Newsletter>>(200);

        group.MapGet("/newsletters/{id:int}", async (int id, MakeBoldSparkService svc, CancellationToken ct) =>
        {
            var item = await svc.GetNewsletterAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        }).WithName("GetNewsletter").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkMessagingNewsletters)
          .WithSummary("Get a newsletter by ID").Produces<Newsletter>(200).Produces(404);

        group.MapPost("/newsletters", async (Newsletter entity, MakeBoldSparkService svc, CancellationToken ct) =>
        {
            var created = await svc.CreateNewsletterAsync(entity, ct);
            return Results.Created($"/api/admin/makeboldspark/newsletters/{created.Id}", created);
        }).WithName("CreateNewsletter").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkMessagingNewsletters)
          .WithSummary("Create a newsletter").Produces<Newsletter>(201).ProducesProblem(400);

        group.MapDelete("/newsletters/{id:int}", async (int id, MakeBoldSparkService svc, CancellationToken ct) =>
            await svc.DeleteNewsletterAsync(id, ct) ? Results.NoContent() : Results.NotFound())
            .WithName("DeleteNewsletter").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkMessagingNewsletters)
            .WithSummary("Delete a newsletter").Produces(204).Produces(404);

        // MailSettings CRUD
        group.MapGet("/mail-settings", async (MakeBoldSparkService svc, CancellationToken ct) =>
            Results.Ok((await svc.GetMailSettingsAsync(ct)).Select(MailSettingResponse.FromMailSetting)))
            .WithName("GetMailSettings").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkMessagingMailSettings)
            .WithSummary("List all mail settings").WithDescription("Returns mail sender configuration without SMTP passwords.")
            .Produces<IEnumerable<MailSettingResponse>>(200);

        group.MapGet("/mail-settings/{id:int}", async (int id, MakeBoldSparkService svc, CancellationToken ct) =>
        {
            var item = await svc.GetMailSettingAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(MailSettingResponse.FromMailSetting(item));
        }).WithName("GetMailSetting").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkMessagingMailSettings)
          .WithSummary("Get a mail setting by ID").WithDescription("Returns one mail sender configuration without the SMTP password.")
          .Produces<MailSettingResponse>(200).Produces(404);

        group.MapPost("/mail-settings", async (MailSetting entity, MakeBoldSparkService svc, CancellationToken ct) =>
        {
            var created = await svc.CreateMailSettingAsync(entity, ct);
            return Results.Created($"/api/admin/makeboldspark/mail-settings/{created.Id}", MailSettingResponse.FromMailSetting(created));
        }).WithName("CreateMailSetting").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkMessagingMailSettings)
          .WithSummary("Create a mail setting").WithDescription("Creates mail sender configuration. The request includes UserPassword, but responses never return it.")
          .Produces<MailSettingResponse>(201).ProducesProblem(400);

        group.MapPut("/mail-settings/{id:int}", async (int id, MailSetting entity, MakeBoldSparkService svc, CancellationToken ct) =>
        {
            var updated = await svc.UpdateMailSettingAsync(id, entity, ct);
            return updated is null ? Results.NotFound() : Results.Ok(MailSettingResponse.FromMailSetting(updated));
        }).WithName("UpdateMailSetting").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkMessagingMailSettings)
          .WithSummary("Update a mail setting").WithDescription("Updates mail sender configuration. The request includes UserPassword, but responses never return it.")
          .Produces<MailSettingResponse>(200).Produces(404);

        group.MapDelete("/mail-settings/{id:int}", async (int id, MakeBoldSparkService svc, CancellationToken ct) =>
            await svc.DeleteMailSettingAsync(id, ct) ? Results.NoContent() : Results.NotFound())
            .WithName("DeleteMailSetting").WithTags(MakeBoldSparkOpenApiTags.MakeBoldSparkMessagingMailSettings)
            .WithSummary("Delete a mail setting").Produces(204).Produces(404);

        return group;
    }
}
