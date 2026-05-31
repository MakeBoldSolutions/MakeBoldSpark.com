using ApiSpark.Api.Infrastructure.OpenApi;
using WebSpark.Core.Data;

namespace ApiSpark.Api.Features.WebSpark;

public static class WebSparkEndpoints
{
    public static RouteGroupBuilder MapPublicWebSparkApi(this RouteGroupBuilder group)
    {
        // Domain / WebSites
        group.MapGet("/domains", async (WebSparkService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetDomainsAsync(ct)))
            .WithName("GetDomains").WithTags(ApiSparkOpenApiTags.WebSparkPublicDomains)
            .WithSummary("List all domains").WithDescription("Returns all WebSpark website / domain records.")
            .Produces<IEnumerable<WebSite>>(200).AllowAnonymous();

        group.MapGet("/domains/{id:int}", async (int id, WebSparkService svc, CancellationToken ct) =>
        {
            var item = await svc.GetDomainAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        }).WithName("GetDomain").WithTags(ApiSparkOpenApiTags.WebSparkPublicDomains)
          .WithSummary("Get a domain by ID").WithDescription("Returns a single WebSpark website record. Returns 404 if not found.")
          .Produces<WebSite>(200).Produces(404).AllowAnonymous();

        // Blogs
        group.MapGet("/blogs", async (WebSparkService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetBlogsAsync(ct)))
            .WithName("GetBlogs").WithTags(ApiSparkOpenApiTags.WebSparkPublicBlogs)
            .WithSummary("List all blogs").WithDescription("Returns all blog records associated with any domain.")
            .Produces<IEnumerable<Blog>>(200).AllowAnonymous();

        group.MapGet("/blogs/{id:int}", async (int id, WebSparkService svc, CancellationToken ct) =>
        {
            var item = await svc.GetBlogAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        }).WithName("GetBlog").WithTags(ApiSparkOpenApiTags.WebSparkPublicBlogs)
          .WithSummary("Get a blog by ID").WithDescription("Returns a single blog record. Returns 404 if not found.")
          .Produces<Blog>(200).Produces(404).AllowAnonymous();

        // Authors
        group.MapGet("/authors", async (WebSparkService svc, CancellationToken ct) =>
            Results.Ok((await svc.GetAuthorsAsync(ct)).Select(AuthorResponse.FromAuthor)))
            .WithName("GetAuthors").WithTags(ApiSparkOpenApiTags.WebSparkPublicAuthors)
            .WithSummary("List all authors").WithDescription("Returns public author profile fields. Password hashes are never included in the list response.")
            .Produces<IEnumerable<AuthorResponse>>(200).AllowAnonymous();

        group.MapGet("/authors/{id:int}", async (int id, WebSparkService svc, CancellationToken ct) =>
        {
            var item = await svc.GetAuthorAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(AuthorResponse.FromAuthor(item));
        }).WithName("GetAuthor").WithTags(ApiSparkOpenApiTags.WebSparkPublicAuthors)
          .WithSummary("Get an author by ID").WithDescription("Returns a single public author profile. Password hashes are never included. Returns 404 if not found.")
          .Produces<AuthorResponse>(200).Produces(404).AllowAnonymous();

        // Posts
        group.MapGet("/posts", async (int? blogId, WebSparkService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetPostsAsync(blogId, ct)))
            .WithName("GetPosts").WithTags(ApiSparkOpenApiTags.WebSparkPublicPosts)
            .WithSummary("List posts").WithDescription("Returns all posts, optionally filtered by blogId query parameter.")
            .Produces<IEnumerable<Post>>(200).AllowAnonymous();

        group.MapGet("/posts/{id:int}", async (int id, WebSparkService svc, CancellationToken ct) =>
        {
            var item = await svc.GetPostAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        }).WithName("GetPost").WithTags(ApiSparkOpenApiTags.WebSparkPublicPosts)
          .WithSummary("Get a post by ID").WithDescription("Returns a single post record. Returns 404 if not found.")
          .Produces<Post>(200).Produces(404).AllowAnonymous();

        // Categories
        group.MapGet("/categories", async (WebSparkService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetCategoriesAsync(ct)))
            .WithName("GetCategories").WithTags(ApiSparkOpenApiTags.WebSparkPublicCategories)
            .WithSummary("List all categories").WithDescription("Returns all content categories.")
            .Produces<IEnumerable<Category>>(200).AllowAnonymous();

        group.MapGet("/categories/{id:int}", async (int id, WebSparkService svc, CancellationToken ct) =>
        {
            var item = await svc.GetCategoryAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        }).WithName("GetCategory").WithTags(ApiSparkOpenApiTags.WebSparkPublicCategories)
          .WithSummary("Get a category by ID").WithDescription("Returns a single category. Returns 404 if not found.")
          .Produces<Category>(200).Produces(404).AllowAnonymous();

        // Menu
        group.MapGet("/menus", async (int? domainId, WebSparkService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetMenusAsync(domainId, ct)))
            .WithName("GetMenus").WithTags(ApiSparkOpenApiTags.WebSparkPublicMenus)
            .WithSummary("List menus").WithDescription("Returns all navigation menus, optionally filtered by domainId.")
            .Produces<IEnumerable<Menu>>(200).AllowAnonymous();

        group.MapGet("/menus/{id:int}", async (int id, WebSparkService svc, CancellationToken ct) =>
        {
            var item = await svc.GetMenuAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        }).WithName("GetMenu").WithTags(ApiSparkOpenApiTags.WebSparkPublicMenus)
          .WithSummary("Get a menu by ID").WithDescription("Returns a single navigation menu. Returns 404 if not found.")
          .Produces<Menu>(200).Produces(404).AllowAnonymous();

        // Keywords
        group.MapGet("/keywords", async (WebSparkService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetKeywordsAsync(ct)))
            .WithName("GetKeywords").WithTags(ApiSparkOpenApiTags.WebSparkPublicKeywords)
            .WithSummary("List all keywords").WithDescription("Returns all SEO keyword / tag records.")
            .Produces<IEnumerable<Keyword>>(200).AllowAnonymous();

        group.MapGet("/keywords/{id:int}", async (int id, WebSparkService svc, CancellationToken ct) =>
        {
            var item = await svc.GetKeywordAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        }).WithName("GetKeyword").WithTags(ApiSparkOpenApiTags.WebSparkPublicKeywords)
          .WithSummary("Get a keyword by ID").WithDescription("Returns a single keyword record. Returns 404 if not found.")
          .Produces<Keyword>(200).Produces(404).AllowAnonymous();

        // ContentParts
        group.MapGet("/content-parts", async (WebSparkService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetContentPartsAsync(ct)))
            .WithName("GetContentParts").WithTags(ApiSparkOpenApiTags.WebSparkPublicContentParts)
            .WithSummary("List all content parts").WithDescription("Returns all reusable content part records.")
            .Produces<IEnumerable<ContentPart>>(200).AllowAnonymous();

        group.MapGet("/content-parts/{id:int}", async (int id, WebSparkService svc, CancellationToken ct) =>
        {
            var item = await svc.GetContentPartAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        }).WithName("GetContentPart").WithTags(ApiSparkOpenApiTags.WebSparkPublicContentParts)
          .WithSummary("Get a content part by ID").WithDescription("Returns a single reusable content part. Returns 404 if not found.")
          .Produces<ContentPart>(200).Produces(404).AllowAnonymous();

        return group;
    }

    public static RouteGroupBuilder MapAdminWebSparkApi(this RouteGroupBuilder group)
    {
        // Domain CRUD
        group.MapPost("/domains", async (WebSite entity, WebSparkService svc, CancellationToken ct) =>
        {
            var created = await svc.CreateDomainAsync(entity, ct);
            return Results.Created($"/api/public/webspark/domains/{created.Id}", created);
        }).WithName("CreateDomain").WithTags(ApiSparkOpenApiTags.WebSparkAdminDomains)
          .WithSummary("Create a domain").WithDescription("Creates a new WebSpark website record. Requires AdminOnly policy.")
          .Produces<WebSite>(201).ProducesProblem(400);

        group.MapPut("/domains/{id:int}", async (int id, WebSite entity, WebSparkService svc, CancellationToken ct) =>
        {
            var updated = await svc.UpdateDomainAsync(id, entity, ct);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        }).WithName("UpdateDomain").WithTags(ApiSparkOpenApiTags.WebSparkAdminDomains)
          .WithSummary("Update a domain").WithDescription("Replaces a domain record. Returns 404 if not found.")
          .Produces<WebSite>(200).Produces(404);

        group.MapDelete("/domains/{id:int}", async (int id, WebSparkService svc, CancellationToken ct) =>
            await svc.DeleteDomainAsync(id, ct) ? Results.NoContent() : Results.NotFound())
            .WithName("DeleteDomain").WithTags(ApiSparkOpenApiTags.WebSparkAdminDomains)
            .WithSummary("Delete a domain").WithDescription("Permanently removes a domain record. Returns 204 on success.")
            .Produces(204).Produces(404);

        // Blog CRUD
        group.MapPost("/blogs", async (Blog entity, WebSparkService svc, CancellationToken ct) =>
        {
            var created = await svc.CreateBlogAsync(entity, ct);
            return Results.Created($"/api/public/webspark/blogs/{created.Id}", created);
        }).WithName("CreateBlog").WithTags(ApiSparkOpenApiTags.WebSparkAdminBlogs)
          .WithSummary("Create a blog").WithDescription("Creates a new blog record.")
          .Produces<Blog>(201).ProducesProblem(400);

        group.MapPut("/blogs/{id:int}", async (int id, Blog entity, WebSparkService svc, CancellationToken ct) =>
        {
            var updated = await svc.UpdateBlogAsync(id, entity, ct);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        }).WithName("UpdateBlog").WithTags(ApiSparkOpenApiTags.WebSparkAdminBlogs)
          .WithSummary("Update a blog").Produces<Blog>(200).Produces(404);

        group.MapDelete("/blogs/{id:int}", async (int id, WebSparkService svc, CancellationToken ct) =>
            await svc.DeleteBlogAsync(id, ct) ? Results.NoContent() : Results.NotFound())
            .WithName("DeleteBlog").WithTags(ApiSparkOpenApiTags.WebSparkAdminBlogs)
            .WithSummary("Delete a blog").Produces(204).Produces(404);

        // Author CRUD
        group.MapPost("/authors", async (Author entity, WebSparkService svc, CancellationToken ct) =>
        {
            var created = await svc.CreateAuthorAsync(entity, ct);
            return Results.Created($"/api/public/webspark/authors/{created.Id}", AuthorResponse.FromAuthor(created));
        }).WithName("CreateAuthor").WithTags(ApiSparkOpenApiTags.WebSparkAdminAuthors)
          .WithSummary("Create an author").WithDescription("Creates a CMS author. The request includes a password, but the response never returns the stored password hash.")
          .Produces<AuthorResponse>(201).ProducesProblem(400);

        group.MapPut("/authors/{id:int}", async (int id, Author entity, WebSparkService svc, CancellationToken ct) =>
        {
            var updated = await svc.UpdateAuthorAsync(id, entity, ct);
            return updated is null ? Results.NotFound() : Results.Ok(AuthorResponse.FromAuthor(updated));
        }).WithName("UpdateAuthor").WithTags(ApiSparkOpenApiTags.WebSparkAdminAuthors)
          .WithSummary("Update an author").WithDescription("Updates a CMS author. The request may include a password, but the response never returns the stored password hash.")
          .Produces<AuthorResponse>(200).Produces(404);

        group.MapDelete("/authors/{id:int}", async (int id, WebSparkService svc, CancellationToken ct) =>
            await svc.DeleteAuthorAsync(id, ct) ? Results.NoContent() : Results.NotFound())
            .WithName("DeleteAuthor").WithTags(ApiSparkOpenApiTags.WebSparkAdminAuthors)
            .WithSummary("Delete an author").Produces(204).Produces(404);

        // Post CRUD
        group.MapPost("/posts", async (Post entity, WebSparkService svc, CancellationToken ct) =>
        {
            var created = await svc.CreatePostAsync(entity, ct);
            return Results.Created($"/api/public/webspark/posts/{created.Id}", created);
        }).WithName("CreatePost").WithTags(ApiSparkOpenApiTags.WebSparkAdminPosts)
          .WithSummary("Create a post").Produces<Post>(201).ProducesProblem(400);

        group.MapPut("/posts/{id:int}", async (int id, Post entity, WebSparkService svc, CancellationToken ct) =>
        {
            var updated = await svc.UpdatePostAsync(id, entity, ct);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        }).WithName("UpdatePost").WithTags(ApiSparkOpenApiTags.WebSparkAdminPosts)
          .WithSummary("Update a post").Produces<Post>(200).Produces(404);

        group.MapDelete("/posts/{id:int}", async (int id, WebSparkService svc, CancellationToken ct) =>
            await svc.DeletePostAsync(id, ct) ? Results.NoContent() : Results.NotFound())
            .WithName("DeletePost").WithTags(ApiSparkOpenApiTags.WebSparkAdminPosts)
            .WithSummary("Delete a post").Produces(204).Produces(404);

        // Category CRUD
        group.MapPost("/categories", async (Category entity, WebSparkService svc, CancellationToken ct) =>
        {
            var created = await svc.CreateCategoryAsync(entity, ct);
            return Results.Created($"/api/public/webspark/categories/{created.Id}", created);
        }).WithName("CreateCategory").WithTags(ApiSparkOpenApiTags.WebSparkAdminCategories)
          .WithSummary("Create a category").Produces<Category>(201).ProducesProblem(400);

        group.MapPut("/categories/{id:int}", async (int id, Category entity, WebSparkService svc, CancellationToken ct) =>
        {
            var updated = await svc.UpdateCategoryAsync(id, entity, ct);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        }).WithName("UpdateCategory").WithTags(ApiSparkOpenApiTags.WebSparkAdminCategories)
          .WithSummary("Update a category").Produces<Category>(200).Produces(404);

        group.MapDelete("/categories/{id:int}", async (int id, WebSparkService svc, CancellationToken ct) =>
            await svc.DeleteCategoryAsync(id, ct) ? Results.NoContent() : Results.NotFound())
            .WithName("DeleteCategory").WithTags(ApiSparkOpenApiTags.WebSparkAdminCategories)
            .WithSummary("Delete a category").Produces(204).Produces(404);

        // Menu CRUD
        group.MapPost("/menus", async (Menu entity, WebSparkService svc, CancellationToken ct) =>
        {
            var created = await svc.CreateMenuAsync(entity, ct);
            return Results.Created($"/api/public/webspark/menus/{created.Id}", created);
        }).WithName("CreateMenu").WithTags(ApiSparkOpenApiTags.WebSparkAdminMenus)
          .WithSummary("Create a menu").Produces<Menu>(201).ProducesProblem(400);

        group.MapPut("/menus/{id:int}", async (int id, Menu entity, WebSparkService svc, CancellationToken ct) =>
        {
            var updated = await svc.UpdateMenuAsync(id, entity, ct);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        }).WithName("UpdateMenu").WithTags(ApiSparkOpenApiTags.WebSparkAdminMenus)
          .WithSummary("Update a menu").Produces<Menu>(200).Produces(404);

        group.MapDelete("/menus/{id:int}", async (int id, WebSparkService svc, CancellationToken ct) =>
            await svc.DeleteMenuAsync(id, ct) ? Results.NoContent() : Results.NotFound())
            .WithName("DeleteMenu").WithTags(ApiSparkOpenApiTags.WebSparkAdminMenus)
            .WithSummary("Delete a menu").Produces(204).Produces(404);

        // Keyword CRUD
        group.MapPost("/keywords", async (Keyword entity, WebSparkService svc, CancellationToken ct) =>
        {
            var created = await svc.CreateKeywordAsync(entity, ct);
            return Results.Created($"/api/public/webspark/keywords/{created.Id}", created);
        }).WithName("CreateKeyword").WithTags(ApiSparkOpenApiTags.WebSparkAdminKeywords)
          .WithSummary("Create a keyword").Produces<Keyword>(201).ProducesProblem(400);

        group.MapPut("/keywords/{id:int}", async (int id, Keyword entity, WebSparkService svc, CancellationToken ct) =>
        {
            var updated = await svc.UpdateKeywordAsync(id, entity, ct);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        }).WithName("UpdateKeyword").WithTags(ApiSparkOpenApiTags.WebSparkAdminKeywords)
          .WithSummary("Update a keyword").Produces<Keyword>(200).Produces(404);

        group.MapDelete("/keywords/{id:int}", async (int id, WebSparkService svc, CancellationToken ct) =>
            await svc.DeleteKeywordAsync(id, ct) ? Results.NoContent() : Results.NotFound())
            .WithName("DeleteKeyword").WithTags(ApiSparkOpenApiTags.WebSparkAdminKeywords)
            .WithSummary("Delete a keyword").Produces(204).Produces(404);

        // ContentPart CRUD
        group.MapPost("/content-parts", async (ContentPart entity, WebSparkService svc, CancellationToken ct) =>
        {
            var created = await svc.CreateContentPartAsync(entity, ct);
            return Results.Created($"/api/public/webspark/content-parts/{created.Id}", created);
        }).WithName("CreateContentPart").WithTags(ApiSparkOpenApiTags.WebSparkAdminContentParts)
          .WithSummary("Create a content part").Produces<ContentPart>(201).ProducesProblem(400);

        group.MapPut("/content-parts/{id:int}", async (int id, ContentPart entity, WebSparkService svc, CancellationToken ct) =>
        {
            var updated = await svc.UpdateContentPartAsync(id, entity, ct);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        }).WithName("UpdateContentPart").WithTags(ApiSparkOpenApiTags.WebSparkAdminContentParts)
          .WithSummary("Update a content part").Produces<ContentPart>(200).Produces(404);

        group.MapDelete("/content-parts/{id:int}", async (int id, WebSparkService svc, CancellationToken ct) =>
            await svc.DeleteContentPartAsync(id, ct) ? Results.NoContent() : Results.NotFound())
            .WithName("DeleteContentPart").WithTags(ApiSparkOpenApiTags.WebSparkAdminContentParts)
            .WithSummary("Delete a content part").Produces(204).Produces(404);

        // Subscriber CRUD
        group.MapGet("/subscribers", async (WebSparkService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetSubscribersAsync(ct)))
            .WithName("GetSubscribers").WithTags(ApiSparkOpenApiTags.WebSparkMessagingSubscribers)
            .WithSummary("List all subscribers").WithDescription("Returns all newsletter subscribers. Requires AdminOnly policy.")
            .Produces<IEnumerable<Subscriber>>(200);

        group.MapGet("/subscribers/{id:int}", async (int id, WebSparkService svc, CancellationToken ct) =>
        {
            var item = await svc.GetSubscriberAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        }).WithName("GetSubscriber").WithTags(ApiSparkOpenApiTags.WebSparkMessagingSubscribers)
          .WithSummary("Get a subscriber by ID").Produces<Subscriber>(200).Produces(404);

        group.MapPost("/subscribers", async (Subscriber entity, WebSparkService svc, CancellationToken ct) =>
        {
            var created = await svc.CreateSubscriberAsync(entity, ct);
            return Results.Created($"/api/admin/webspark/subscribers/{created.Id}", created);
        }).WithName("CreateSubscriber").WithTags(ApiSparkOpenApiTags.WebSparkMessagingSubscribers)
          .WithSummary("Create a subscriber").Produces<Subscriber>(201).ProducesProblem(400);

        group.MapPut("/subscribers/{id:int}", async (int id, Subscriber entity, WebSparkService svc, CancellationToken ct) =>
        {
            var updated = await svc.UpdateSubscriberAsync(id, entity, ct);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        }).WithName("UpdateSubscriber").WithTags(ApiSparkOpenApiTags.WebSparkMessagingSubscribers)
          .WithSummary("Update a subscriber").Produces<Subscriber>(200).Produces(404);

        group.MapDelete("/subscribers/{id:int}", async (int id, WebSparkService svc, CancellationToken ct) =>
            await svc.DeleteSubscriberAsync(id, ct) ? Results.NoContent() : Results.NotFound())
            .WithName("DeleteSubscriber").WithTags(ApiSparkOpenApiTags.WebSparkMessagingSubscribers)
            .WithSummary("Delete a subscriber").Produces(204).Produces(404);

        // Newsletter CRUD
        group.MapGet("/newsletters", async (WebSparkService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetNewslettersAsync(ct)))
            .WithName("GetNewsletters").WithTags(ApiSparkOpenApiTags.WebSparkMessagingNewsletters)
            .WithSummary("List all newsletters").Produces<IEnumerable<Newsletter>>(200);

        group.MapGet("/newsletters/{id:int}", async (int id, WebSparkService svc, CancellationToken ct) =>
        {
            var item = await svc.GetNewsletterAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        }).WithName("GetNewsletter").WithTags(ApiSparkOpenApiTags.WebSparkMessagingNewsletters)
          .WithSummary("Get a newsletter by ID").Produces<Newsletter>(200).Produces(404);

        group.MapPost("/newsletters", async (Newsletter entity, WebSparkService svc, CancellationToken ct) =>
        {
            var created = await svc.CreateNewsletterAsync(entity, ct);
            return Results.Created($"/api/admin/webspark/newsletters/{created.Id}", created);
        }).WithName("CreateNewsletter").WithTags(ApiSparkOpenApiTags.WebSparkMessagingNewsletters)
          .WithSummary("Create a newsletter").Produces<Newsletter>(201).ProducesProblem(400);

        group.MapDelete("/newsletters/{id:int}", async (int id, WebSparkService svc, CancellationToken ct) =>
            await svc.DeleteNewsletterAsync(id, ct) ? Results.NoContent() : Results.NotFound())
            .WithName("DeleteNewsletter").WithTags(ApiSparkOpenApiTags.WebSparkMessagingNewsletters)
            .WithSummary("Delete a newsletter").Produces(204).Produces(404);

        // MailSettings CRUD
        group.MapGet("/mail-settings", async (WebSparkService svc, CancellationToken ct) =>
            Results.Ok((await svc.GetMailSettingsAsync(ct)).Select(MailSettingResponse.FromMailSetting)))
            .WithName("GetMailSettings").WithTags(ApiSparkOpenApiTags.WebSparkMessagingMailSettings)
            .WithSummary("List all mail settings").WithDescription("Returns mail sender configuration without SMTP passwords.")
            .Produces<IEnumerable<MailSettingResponse>>(200);

        group.MapGet("/mail-settings/{id:int}", async (int id, WebSparkService svc, CancellationToken ct) =>
        {
            var item = await svc.GetMailSettingAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(MailSettingResponse.FromMailSetting(item));
        }).WithName("GetMailSetting").WithTags(ApiSparkOpenApiTags.WebSparkMessagingMailSettings)
          .WithSummary("Get a mail setting by ID").WithDescription("Returns one mail sender configuration without the SMTP password.")
          .Produces<MailSettingResponse>(200).Produces(404);

        group.MapPost("/mail-settings", async (MailSetting entity, WebSparkService svc, CancellationToken ct) =>
        {
            var created = await svc.CreateMailSettingAsync(entity, ct);
            return Results.Created($"/api/admin/webspark/mail-settings/{created.Id}", MailSettingResponse.FromMailSetting(created));
        }).WithName("CreateMailSetting").WithTags(ApiSparkOpenApiTags.WebSparkMessagingMailSettings)
          .WithSummary("Create a mail setting").WithDescription("Creates mail sender configuration. The request includes UserPassword, but responses never return it.")
          .Produces<MailSettingResponse>(201).ProducesProblem(400);

        group.MapPut("/mail-settings/{id:int}", async (int id, MailSetting entity, WebSparkService svc, CancellationToken ct) =>
        {
            var updated = await svc.UpdateMailSettingAsync(id, entity, ct);
            return updated is null ? Results.NotFound() : Results.Ok(MailSettingResponse.FromMailSetting(updated));
        }).WithName("UpdateMailSetting").WithTags(ApiSparkOpenApiTags.WebSparkMessagingMailSettings)
          .WithSummary("Update a mail setting").WithDescription("Updates mail sender configuration. The request includes UserPassword, but responses never return it.")
          .Produces<MailSettingResponse>(200).Produces(404);

        group.MapDelete("/mail-settings/{id:int}", async (int id, WebSparkService svc, CancellationToken ct) =>
            await svc.DeleteMailSettingAsync(id, ct) ? Results.NoContent() : Results.NotFound())
            .WithName("DeleteMailSetting").WithTags(ApiSparkOpenApiTags.WebSparkMessagingMailSettings)
            .WithSummary("Delete a mail setting").Produces(204).Produces(404);

        return group;
    }
}
