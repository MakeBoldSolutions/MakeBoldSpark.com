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
            .WithName("GetDomains").WithTags(ApiSparkOpenApiTags.WebSparkDomains)
            .WithSummary("List all domains").WithDescription("Returns all WebSpark website / domain records.")
            .Produces<IEnumerable<WebSite>>(200).AllowAnonymous();

        group.MapGet("/domains/{id:int}", async (int id, WebSparkService svc, CancellationToken ct) =>
        {
            var item = await svc.GetDomainAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        }).WithName("GetDomain").WithTags(ApiSparkOpenApiTags.WebSparkDomains)
          .WithSummary("Get a domain by ID").WithDescription("Returns a single WebSpark website record. Returns 404 if not found.")
          .Produces<WebSite>(200).Produces(404).AllowAnonymous();

        // Blogs
        group.MapGet("/blogs", async (WebSparkService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetBlogsAsync(ct)))
            .WithName("GetBlogs").WithTags(ApiSparkOpenApiTags.WebSparkBlogs)
            .WithSummary("List all blogs").WithDescription("Returns all blog records associated with any domain.")
            .Produces<IEnumerable<Blog>>(200).AllowAnonymous();

        group.MapGet("/blogs/{id:int}", async (int id, WebSparkService svc, CancellationToken ct) =>
        {
            var item = await svc.GetBlogAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        }).WithName("GetBlog").WithTags(ApiSparkOpenApiTags.WebSparkBlogs)
          .WithSummary("Get a blog by ID").WithDescription("Returns a single blog record. Returns 404 if not found.")
          .Produces<Blog>(200).Produces(404).AllowAnonymous();

        // Authors
        group.MapGet("/authors", async (WebSparkService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAuthorsAsync(ct)))
            .WithName("GetAuthors").WithTags(ApiSparkOpenApiTags.WebSparkAuthors)
            .WithSummary("List all authors").WithDescription("Returns all author records.")
            .Produces<IEnumerable<Author>>(200).AllowAnonymous();

        group.MapGet("/authors/{id:int}", async (int id, WebSparkService svc, CancellationToken ct) =>
        {
            var item = await svc.GetAuthorAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        }).WithName("GetAuthor").WithTags(ApiSparkOpenApiTags.WebSparkAuthors)
          .WithSummary("Get an author by ID").WithDescription("Returns a single author record. Returns 404 if not found.")
          .Produces<Author>(200).Produces(404).AllowAnonymous();

        // Posts
        group.MapGet("/posts", async (int? blogId, WebSparkService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetPostsAsync(blogId, ct)))
            .WithName("GetPosts").WithTags(ApiSparkOpenApiTags.WebSparkPosts)
            .WithSummary("List posts").WithDescription("Returns all posts, optionally filtered by blogId query parameter.")
            .Produces<IEnumerable<Post>>(200).AllowAnonymous();

        group.MapGet("/posts/{id:int}", async (int id, WebSparkService svc, CancellationToken ct) =>
        {
            var item = await svc.GetPostAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        }).WithName("GetPost").WithTags(ApiSparkOpenApiTags.WebSparkPosts)
          .WithSummary("Get a post by ID").WithDescription("Returns a single post record. Returns 404 if not found.")
          .Produces<Post>(200).Produces(404).AllowAnonymous();

        // Categories
        group.MapGet("/categories", async (WebSparkService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetCategoriesAsync(ct)))
            .WithName("GetCategories").WithTags(ApiSparkOpenApiTags.WebSparkCategories)
            .WithSummary("List all categories").WithDescription("Returns all content categories.")
            .Produces<IEnumerable<Category>>(200).AllowAnonymous();

        group.MapGet("/categories/{id:int}", async (int id, WebSparkService svc, CancellationToken ct) =>
        {
            var item = await svc.GetCategoryAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        }).WithName("GetCategory").WithTags(ApiSparkOpenApiTags.WebSparkCategories)
          .WithSummary("Get a category by ID").WithDescription("Returns a single category. Returns 404 if not found.")
          .Produces<Category>(200).Produces(404).AllowAnonymous();

        // Menu
        group.MapGet("/menus", async (int? domainId, WebSparkService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetMenusAsync(domainId, ct)))
            .WithName("GetMenus").WithTags(ApiSparkOpenApiTags.WebSparkMenu)
            .WithSummary("List menus").WithDescription("Returns all navigation menus, optionally filtered by domainId.")
            .Produces<IEnumerable<Menu>>(200).AllowAnonymous();

        group.MapGet("/menus/{id:int}", async (int id, WebSparkService svc, CancellationToken ct) =>
        {
            var item = await svc.GetMenuAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        }).WithName("GetMenu").WithTags(ApiSparkOpenApiTags.WebSparkMenu)
          .WithSummary("Get a menu by ID").WithDescription("Returns a single navigation menu. Returns 404 if not found.")
          .Produces<Menu>(200).Produces(404).AllowAnonymous();

        // Keywords
        group.MapGet("/keywords", async (WebSparkService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetKeywordsAsync(ct)))
            .WithName("GetKeywords").WithTags(ApiSparkOpenApiTags.WebSparkKeywords)
            .WithSummary("List all keywords").WithDescription("Returns all SEO keyword / tag records.")
            .Produces<IEnumerable<Keyword>>(200).AllowAnonymous();

        group.MapGet("/keywords/{id:int}", async (int id, WebSparkService svc, CancellationToken ct) =>
        {
            var item = await svc.GetKeywordAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        }).WithName("GetKeyword").WithTags(ApiSparkOpenApiTags.WebSparkKeywords)
          .WithSummary("Get a keyword by ID").WithDescription("Returns a single keyword record. Returns 404 if not found.")
          .Produces<Keyword>(200).Produces(404).AllowAnonymous();

        // ContentParts
        group.MapGet("/content-parts", async (WebSparkService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetContentPartsAsync(ct)))
            .WithName("GetContentParts").WithTags(ApiSparkOpenApiTags.WebSparkContentParts)
            .WithSummary("List all content parts").WithDescription("Returns all reusable content part records.")
            .Produces<IEnumerable<ContentPart>>(200).AllowAnonymous();

        group.MapGet("/content-parts/{id:int}", async (int id, WebSparkService svc, CancellationToken ct) =>
        {
            var item = await svc.GetContentPartAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        }).WithName("GetContentPart").WithTags(ApiSparkOpenApiTags.WebSparkContentParts)
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
        }).WithName("CreateDomain").WithTags(ApiSparkOpenApiTags.WebSparkDomains)
          .WithSummary("Create a domain").WithDescription("Creates a new WebSpark website record. Requires AdminOnly policy.")
          .Produces<WebSite>(201).ProducesProblem(400);

        group.MapPut("/domains/{id:int}", async (int id, WebSite entity, WebSparkService svc, CancellationToken ct) =>
        {
            var updated = await svc.UpdateDomainAsync(id, entity, ct);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        }).WithName("UpdateDomain").WithTags(ApiSparkOpenApiTags.WebSparkDomains)
          .WithSummary("Update a domain").WithDescription("Replaces a domain record. Returns 404 if not found.")
          .Produces<WebSite>(200).Produces(404);

        group.MapDelete("/domains/{id:int}", async (int id, WebSparkService svc, CancellationToken ct) =>
            await svc.DeleteDomainAsync(id, ct) ? Results.NoContent() : Results.NotFound())
            .WithName("DeleteDomain").WithTags(ApiSparkOpenApiTags.WebSparkDomains)
            .WithSummary("Delete a domain").WithDescription("Permanently removes a domain record. Returns 204 on success.")
            .Produces(204).Produces(404);

        // Blog CRUD
        group.MapPost("/blogs", async (Blog entity, WebSparkService svc, CancellationToken ct) =>
        {
            var created = await svc.CreateBlogAsync(entity, ct);
            return Results.Created($"/api/public/webspark/blogs/{created.Id}", created);
        }).WithName("CreateBlog").WithTags(ApiSparkOpenApiTags.WebSparkBlogs)
          .WithSummary("Create a blog").WithDescription("Creates a new blog record.")
          .Produces<Blog>(201).ProducesProblem(400);

        group.MapPut("/blogs/{id:int}", async (int id, Blog entity, WebSparkService svc, CancellationToken ct) =>
        {
            var updated = await svc.UpdateBlogAsync(id, entity, ct);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        }).WithName("UpdateBlog").WithTags(ApiSparkOpenApiTags.WebSparkBlogs)
          .WithSummary("Update a blog").Produces<Blog>(200).Produces(404);

        group.MapDelete("/blogs/{id:int}", async (int id, WebSparkService svc, CancellationToken ct) =>
            await svc.DeleteBlogAsync(id, ct) ? Results.NoContent() : Results.NotFound())
            .WithName("DeleteBlog").WithTags(ApiSparkOpenApiTags.WebSparkBlogs)
            .WithSummary("Delete a blog").Produces(204).Produces(404);

        // Author CRUD
        group.MapPost("/authors", async (Author entity, WebSparkService svc, CancellationToken ct) =>
        {
            var created = await svc.CreateAuthorAsync(entity, ct);
            return Results.Created($"/api/public/webspark/authors/{created.Id}", created);
        }).WithName("CreateAuthor").WithTags(ApiSparkOpenApiTags.WebSparkAuthors)
          .WithSummary("Create an author").Produces<Author>(201).ProducesProblem(400);

        group.MapPut("/authors/{id:int}", async (int id, Author entity, WebSparkService svc, CancellationToken ct) =>
        {
            var updated = await svc.UpdateAuthorAsync(id, entity, ct);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        }).WithName("UpdateAuthor").WithTags(ApiSparkOpenApiTags.WebSparkAuthors)
          .WithSummary("Update an author").Produces<Author>(200).Produces(404);

        group.MapDelete("/authors/{id:int}", async (int id, WebSparkService svc, CancellationToken ct) =>
            await svc.DeleteAuthorAsync(id, ct) ? Results.NoContent() : Results.NotFound())
            .WithName("DeleteAuthor").WithTags(ApiSparkOpenApiTags.WebSparkAuthors)
            .WithSummary("Delete an author").Produces(204).Produces(404);

        // Post CRUD
        group.MapPost("/posts", async (Post entity, WebSparkService svc, CancellationToken ct) =>
        {
            var created = await svc.CreatePostAsync(entity, ct);
            return Results.Created($"/api/public/webspark/posts/{created.Id}", created);
        }).WithName("CreatePost").WithTags(ApiSparkOpenApiTags.WebSparkPosts)
          .WithSummary("Create a post").Produces<Post>(201).ProducesProblem(400);

        group.MapPut("/posts/{id:int}", async (int id, Post entity, WebSparkService svc, CancellationToken ct) =>
        {
            var updated = await svc.UpdatePostAsync(id, entity, ct);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        }).WithName("UpdatePost").WithTags(ApiSparkOpenApiTags.WebSparkPosts)
          .WithSummary("Update a post").Produces<Post>(200).Produces(404);

        group.MapDelete("/posts/{id:int}", async (int id, WebSparkService svc, CancellationToken ct) =>
            await svc.DeletePostAsync(id, ct) ? Results.NoContent() : Results.NotFound())
            .WithName("DeletePost").WithTags(ApiSparkOpenApiTags.WebSparkPosts)
            .WithSummary("Delete a post").Produces(204).Produces(404);

        // Category CRUD
        group.MapPost("/categories", async (Category entity, WebSparkService svc, CancellationToken ct) =>
        {
            var created = await svc.CreateCategoryAsync(entity, ct);
            return Results.Created($"/api/public/webspark/categories/{created.Id}", created);
        }).WithName("CreateCategory").WithTags(ApiSparkOpenApiTags.WebSparkCategories)
          .WithSummary("Create a category").Produces<Category>(201).ProducesProblem(400);

        group.MapPut("/categories/{id:int}", async (int id, Category entity, WebSparkService svc, CancellationToken ct) =>
        {
            var updated = await svc.UpdateCategoryAsync(id, entity, ct);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        }).WithName("UpdateCategory").WithTags(ApiSparkOpenApiTags.WebSparkCategories)
          .WithSummary("Update a category").Produces<Category>(200).Produces(404);

        group.MapDelete("/categories/{id:int}", async (int id, WebSparkService svc, CancellationToken ct) =>
            await svc.DeleteCategoryAsync(id, ct) ? Results.NoContent() : Results.NotFound())
            .WithName("DeleteCategory").WithTags(ApiSparkOpenApiTags.WebSparkCategories)
            .WithSummary("Delete a category").Produces(204).Produces(404);

        // Menu CRUD
        group.MapPost("/menus", async (Menu entity, WebSparkService svc, CancellationToken ct) =>
        {
            var created = await svc.CreateMenuAsync(entity, ct);
            return Results.Created($"/api/public/webspark/menus/{created.Id}", created);
        }).WithName("CreateMenu").WithTags(ApiSparkOpenApiTags.WebSparkMenu)
          .WithSummary("Create a menu").Produces<Menu>(201).ProducesProblem(400);

        group.MapPut("/menus/{id:int}", async (int id, Menu entity, WebSparkService svc, CancellationToken ct) =>
        {
            var updated = await svc.UpdateMenuAsync(id, entity, ct);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        }).WithName("UpdateMenu").WithTags(ApiSparkOpenApiTags.WebSparkMenu)
          .WithSummary("Update a menu").Produces<Menu>(200).Produces(404);

        group.MapDelete("/menus/{id:int}", async (int id, WebSparkService svc, CancellationToken ct) =>
            await svc.DeleteMenuAsync(id, ct) ? Results.NoContent() : Results.NotFound())
            .WithName("DeleteMenu").WithTags(ApiSparkOpenApiTags.WebSparkMenu)
            .WithSummary("Delete a menu").Produces(204).Produces(404);

        // Keyword CRUD
        group.MapPost("/keywords", async (Keyword entity, WebSparkService svc, CancellationToken ct) =>
        {
            var created = await svc.CreateKeywordAsync(entity, ct);
            return Results.Created($"/api/public/webspark/keywords/{created.Id}", created);
        }).WithName("CreateKeyword").WithTags(ApiSparkOpenApiTags.WebSparkKeywords)
          .WithSummary("Create a keyword").Produces<Keyword>(201).ProducesProblem(400);

        group.MapPut("/keywords/{id:int}", async (int id, Keyword entity, WebSparkService svc, CancellationToken ct) =>
        {
            var updated = await svc.UpdateKeywordAsync(id, entity, ct);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        }).WithName("UpdateKeyword").WithTags(ApiSparkOpenApiTags.WebSparkKeywords)
          .WithSummary("Update a keyword").Produces<Keyword>(200).Produces(404);

        group.MapDelete("/keywords/{id:int}", async (int id, WebSparkService svc, CancellationToken ct) =>
            await svc.DeleteKeywordAsync(id, ct) ? Results.NoContent() : Results.NotFound())
            .WithName("DeleteKeyword").WithTags(ApiSparkOpenApiTags.WebSparkKeywords)
            .WithSummary("Delete a keyword").Produces(204).Produces(404);

        // ContentPart CRUD
        group.MapPost("/content-parts", async (ContentPart entity, WebSparkService svc, CancellationToken ct) =>
        {
            var created = await svc.CreateContentPartAsync(entity, ct);
            return Results.Created($"/api/public/webspark/content-parts/{created.Id}", created);
        }).WithName("CreateContentPart").WithTags(ApiSparkOpenApiTags.WebSparkContentParts)
          .WithSummary("Create a content part").Produces<ContentPart>(201).ProducesProblem(400);

        group.MapPut("/content-parts/{id:int}", async (int id, ContentPart entity, WebSparkService svc, CancellationToken ct) =>
        {
            var updated = await svc.UpdateContentPartAsync(id, entity, ct);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        }).WithName("UpdateContentPart").WithTags(ApiSparkOpenApiTags.WebSparkContentParts)
          .WithSummary("Update a content part").Produces<ContentPart>(200).Produces(404);

        group.MapDelete("/content-parts/{id:int}", async (int id, WebSparkService svc, CancellationToken ct) =>
            await svc.DeleteContentPartAsync(id, ct) ? Results.NoContent() : Results.NotFound())
            .WithName("DeleteContentPart").WithTags(ApiSparkOpenApiTags.WebSparkContentParts)
            .WithSummary("Delete a content part").Produces(204).Produces(404);

        // Subscriber CRUD
        group.MapGet("/subscribers", async (WebSparkService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetSubscribersAsync(ct)))
            .WithName("GetSubscribers").WithTags(ApiSparkOpenApiTags.WebSparkSubscribers)
            .WithSummary("List all subscribers").WithDescription("Returns all newsletter subscribers. Requires AdminOnly policy.")
            .Produces<IEnumerable<Subscriber>>(200);

        group.MapGet("/subscribers/{id:int}", async (int id, WebSparkService svc, CancellationToken ct) =>
        {
            var item = await svc.GetSubscriberAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        }).WithName("GetSubscriber").WithTags(ApiSparkOpenApiTags.WebSparkSubscribers)
          .WithSummary("Get a subscriber by ID").Produces<Subscriber>(200).Produces(404);

        group.MapPost("/subscribers", async (Subscriber entity, WebSparkService svc, CancellationToken ct) =>
        {
            var created = await svc.CreateSubscriberAsync(entity, ct);
            return Results.Created($"/api/admin/webspark/subscribers/{created.Id}", created);
        }).WithName("CreateSubscriber").WithTags(ApiSparkOpenApiTags.WebSparkSubscribers)
          .WithSummary("Create a subscriber").Produces<Subscriber>(201).ProducesProblem(400);

        group.MapPut("/subscribers/{id:int}", async (int id, Subscriber entity, WebSparkService svc, CancellationToken ct) =>
        {
            var updated = await svc.UpdateSubscriberAsync(id, entity, ct);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        }).WithName("UpdateSubscriber").WithTags(ApiSparkOpenApiTags.WebSparkSubscribers)
          .WithSummary("Update a subscriber").Produces<Subscriber>(200).Produces(404);

        group.MapDelete("/subscribers/{id:int}", async (int id, WebSparkService svc, CancellationToken ct) =>
            await svc.DeleteSubscriberAsync(id, ct) ? Results.NoContent() : Results.NotFound())
            .WithName("DeleteSubscriber").WithTags(ApiSparkOpenApiTags.WebSparkSubscribers)
            .WithSummary("Delete a subscriber").Produces(204).Produces(404);

        // Newsletter CRUD
        group.MapGet("/newsletters", async (WebSparkService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetNewslettersAsync(ct)))
            .WithName("GetNewsletters").WithTags(ApiSparkOpenApiTags.WebSparkNewsletters)
            .WithSummary("List all newsletters").Produces<IEnumerable<Newsletter>>(200);

        group.MapGet("/newsletters/{id:int}", async (int id, WebSparkService svc, CancellationToken ct) =>
        {
            var item = await svc.GetNewsletterAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        }).WithName("GetNewsletter").WithTags(ApiSparkOpenApiTags.WebSparkNewsletters)
          .WithSummary("Get a newsletter by ID").Produces<Newsletter>(200).Produces(404);

        group.MapPost("/newsletters", async (Newsletter entity, WebSparkService svc, CancellationToken ct) =>
        {
            var created = await svc.CreateNewsletterAsync(entity, ct);
            return Results.Created($"/api/admin/webspark/newsletters/{created.Id}", created);
        }).WithName("CreateNewsletter").WithTags(ApiSparkOpenApiTags.WebSparkNewsletters)
          .WithSummary("Create a newsletter").Produces<Newsletter>(201).ProducesProblem(400);

        group.MapDelete("/newsletters/{id:int}", async (int id, WebSparkService svc, CancellationToken ct) =>
            await svc.DeleteNewsletterAsync(id, ct) ? Results.NoContent() : Results.NotFound())
            .WithName("DeleteNewsletter").WithTags(ApiSparkOpenApiTags.WebSparkNewsletters)
            .WithSummary("Delete a newsletter").Produces(204).Produces(404);

        // MailSettings CRUD
        group.MapGet("/mail-settings", async (WebSparkService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetMailSettingsAsync(ct)))
            .WithName("GetMailSettings").WithTags(ApiSparkOpenApiTags.WebSparkMailSettings)
            .WithSummary("List all mail settings").Produces<IEnumerable<MailSetting>>(200);

        group.MapGet("/mail-settings/{id:int}", async (int id, WebSparkService svc, CancellationToken ct) =>
        {
            var item = await svc.GetMailSettingAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        }).WithName("GetMailSetting").WithTags(ApiSparkOpenApiTags.WebSparkMailSettings)
          .WithSummary("Get a mail setting by ID").Produces<MailSetting>(200).Produces(404);

        group.MapPost("/mail-settings", async (MailSetting entity, WebSparkService svc, CancellationToken ct) =>
        {
            var created = await svc.CreateMailSettingAsync(entity, ct);
            return Results.Created($"/api/admin/webspark/mail-settings/{created.Id}", created);
        }).WithName("CreateMailSetting").WithTags(ApiSparkOpenApiTags.WebSparkMailSettings)
          .WithSummary("Create a mail setting").Produces<MailSetting>(201).ProducesProblem(400);

        group.MapPut("/mail-settings/{id:int}", async (int id, MailSetting entity, WebSparkService svc, CancellationToken ct) =>
        {
            var updated = await svc.UpdateMailSettingAsync(id, entity, ct);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        }).WithName("UpdateMailSetting").WithTags(ApiSparkOpenApiTags.WebSparkMailSettings)
          .WithSummary("Update a mail setting").Produces<MailSetting>(200).Produces(404);

        group.MapDelete("/mail-settings/{id:int}", async (int id, WebSparkService svc, CancellationToken ct) =>
            await svc.DeleteMailSettingAsync(id, ct) ? Results.NoContent() : Results.NotFound())
            .WithName("DeleteMailSetting").WithTags(ApiSparkOpenApiTags.WebSparkMailSettings)
            .WithSummary("Delete a mail setting").Produces(204).Produces(404);

        return group;
    }
}
