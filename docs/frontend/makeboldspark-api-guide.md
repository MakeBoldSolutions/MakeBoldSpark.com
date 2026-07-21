# MakeBoldSpark CMS API — Frontend Developer Guide

**Base path**: `/api/public/makeboldspark/*` (anonymous) · `/api/admin/makeboldspark/*` (Admin role)  
**Data source**: `MakeBoldSpark.Core` domain library → SQLite via EF Core  
**Spec file**: `../../bold-docs/system/contracts/makeboldspark.yaml`

---

## Table of Contents

1. [Mental Model](#mental-model)
2. [Entity Hierarchy](#entity-hierarchy)
3. [TypeScript Types](#typescript-types)
4. [Client Setup](#client-setup)
5. [Public Endpoints](#public-endpoints)
   - [Domains](#domains)
   - [Blogs](#blogs)
   - [Authors](#authors)
   - [Posts](#posts)
   - [Categories](#categories)
   - [Menus](#menus)
   - [Keywords](#keywords)
   - [Content Parts](#content-parts)
6. [Authentication](#authentication)
7. [Admin Endpoints](#admin-endpoints)
   - [Subscribers](#subscribers)
   - [Newsletters](#newsletters)
   - [Mail Settings](#mail-settings)
   - [Admin CRUD for Public Entities](#admin-crud-for-public-entities)
8. [Error Handling](#error-handling)
9. [Patterns and Gotchas](#patterns-and-gotchas)

---

## Mental Model

The MakeBoldSpark CMS API exposes the `MakeBoldSpark.Core` publishing platform data — the same data
that powers [markhazleton.com](https://markhazleton.com). Think of it as a headless CMS:

- **Domain (WebSite)** — top-level tenant / site config
- **Blog** — a publication belonging to a domain
- **Author** — a content creator, linked to one or more blogs
- **Post** — an individual article, belonging to a blog and an author
- **Category** — a tag/topic grouping for posts (many-to-many via `PostCategory`)
- **Menu** — hierarchical navigation items scoped to a domain
- **Keyword** — SEO and discovery tags linked to menus and content parts
- **ContentPart** — reusable content blocks linked to keywords
- **Subscriber** — email subscriber to a blog
- **Newsletter** — a sent newsletter referencing a post
- **MailSetting** — SMTP configuration for a blog (admin only — contains credentials)

All entities share a `BaseEntity` with `id`, `createdDate`, `updatedDate`, `createdID`,
`updatedID`.

---

## Entity Hierarchy

```text
WebSite (Domain)
├── Menu[]          ← hierarchical; parentId self-reference
│   └── Keyword[]  ← many-to-many
Blog[]
├── Author[]        ← many-to-many through Blog.Authors
├── Post[]
│   └── Category[] ← many-to-many through PostCategory
├── Subscriber[]
├── Newsletter[]    ← references Post
└── MailSetting[]
ContentPart[]
└── Keyword[]       ← many-to-many
```

---

## TypeScript Types

```typescript
// Shared base for all entities
export interface BaseEntity {
  id: number;
  createdDate: string;   // ISO 8601
  updatedDate: string;   // ISO 8601
  createdID: number | null;
  updatedID: number | null;
}

export interface WebSite extends BaseEntity {
  name: string;
  description: string;
  template: string;
  galleryFolder: string;
  domainUrl: string;
  title: string;
  useBreadCrumbUrl: boolean;
  versionNo: number;
  style: string;
  isRecipeSite: boolean;
  // menus: Menu[];  ← navigation property, not returned by list endpoints
}

export interface Blog extends BaseEntity {
  title: string;
  description: string;
  theme: string;
  includeFeatured: boolean;
  itemsPerPage: number;
  cover: string | null;
  logo: string | null;
  headerScript: string | null;
  footerScript: string | null;
  analyticsListType: number;
  analyticsPeriod: number;
  // posts and authors are navigation properties — not in flat GET responses
}

export interface Author extends BaseEntity {
  email: string;
  password: string;       // ⚠️  NEVER render in public UI — see Gotchas
  displayName: string;
  bio: string | null;
  avatar: string | null;  // often an SVG data URI
  isAdmin: boolean;
}

export type PostType = number; // enum value from the server

export interface Post extends BaseEntity {
  authorId: number;
  blogId: number;
  title: string;
  slug: string;
  description: string;
  content: string;        // HTML or Markdown — see Gotchas
  cover: string;
  isFeatured: boolean;
  postType: PostType;
  postViews: number;
  published: string;      // ISO 8601
  rating: number;
  selected: boolean;
  // postCategories: PostCategory[];  ← many-to-many junction — not returned by default
  // blog: Blog;  ← navigation property
}

export interface Category extends BaseEntity {
  content: string;        // ⚠️  this is the category NAME — the field is named "content"
  description: string;
}

export interface Menu extends BaseEntity {
  displayOrder: number;
  title: string;
  description: string;
  keyWords: string;
  controller: string;
  action: string;
  argument: string | null;
  icon: string;
  url: string;
  pageContent: string;
  domainId: number;
  parentId: number | null;  // null = top-level; non-null = child menu item
  // keywords: Keyword[];  ← navigation property
}

export interface Keyword extends BaseEntity {
  name: string;
  description: string;
}

export interface ContentPart extends BaseEntity {
  title: string;
  description: string;
  content: string;        // HTML or Markdown
}

export interface Subscriber extends BaseEntity {
  email: string;
  ip: string;
  country: string;
  region: string;
  blogId: number;
}

export interface Newsletter extends BaseEntity {
  postId: number;
  success: boolean;
  // post: Post;  ← navigation property
}

export interface MailSetting extends BaseEntity {
  host: string;
  port: number;
  userEmail: string;
  userPassword: string;   // ⚠️  SMTP credential — admin eyes only
  fromName: string;
  fromEmail: string;
  toName: string;
  enabled: boolean;
  blogId: number;
}
```

---

## Client Setup

```typescript
const API_BASE = import.meta.env.VITE_API_URL ?? 'https://api.markhazleton.com';

class ApiError extends Error {
  constructor(public status: number, public body: string) {
    super(`API ${status}: ${body}`);
  }
}

async function apiFetch<T>(path: string, options: RequestInit = {}): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, {
    headers: { 'Content-Type': 'application/json', ...options.headers },
    ...options,
  });
  if (!res.ok) throw new ApiError(res.status, await res.text());
  if (res.status === 204) return undefined as T;
  return res.json() as Promise<T>;
}

function authedFetch<T>(token: string, path: string, options: RequestInit = {}) {
  return apiFetch<T>(path, {
    ...options,
    headers: { Authorization: `Bearer ${token}`, ...options.headers },
  });
}
```

---

## Authentication

**`POST /api/public/auth/login`** issues an access token for an existing administrator author.
The route is anonymous only because it verifies credentials and issues the token; it never
returns CMS data. Send the token as `Authorization: Bearer <accessToken>` on every
`/api/admin/makeboldspark/*` call.

```typescript
interface LoginResponse {
  accessToken: string;
  expiresAt: string;
  displayName: string;
}

export const login = (email: string, password: string) =>
  apiFetch<LoginResponse>('/api/public/auth/login', {
    method: 'POST',
    body: JSON.stringify({ email, password }),
  });
```

A successful response is `200`. Every failure — unknown email, non-administrator account,
wrong password, an overlong password, or the request throttle — is the same `401` response:

```json
{ "message": "Invalid email or password." }
```

Do not infer account status from the response. Store the access token only for the current
session and clear it on sign-out or when an authenticated request returns `401`.

---

## Public Endpoints

All `GET /api/public/makeboldspark/*` routes are **anonymous** — no token required.

---

### Domains

**`GET /api/public/makeboldspark/domains`** — returns all domains ordered by `name`.

```typescript
export const getDomains = () =>
  apiFetch<WebSite[]>('/api/public/makeboldspark/domains');
```

**`GET /api/public/makeboldspark/domains/{id}`** — single domain or 404.

```typescript
export const getDomain = (id: number) =>
  apiFetch<WebSite>(`/api/public/makeboldspark/domains/${id}`);
```

**Example response** (single domain):

```json
{
  "id": 1,
  "name": "MarkHazleton.com",
  "description": "Solutions Architect Blog",
  "template": "bootstrap5",
  "galleryFolder": "images/gallery",
  "domainUrl": "https://markhazleton.com",
  "title": "Mark Hazleton — Solutions Architect",
  "useBreadCrumbUrl": true,
  "versionNo": 42,
  "style": "dark",
  "isRecipeSite": false,
  "createdDate": "2024-01-01T00:00:00Z",
  "updatedDate": "2026-05-09T12:00:00Z",
  "createdID": 1,
  "updatedID": 1
}
```

---

### Blogs

**`GET /api/public/makeboldspark/blogs`** — all blogs ordered by `title`.

```typescript
export const getBlogs = () =>
  apiFetch<Blog[]>('/api/public/makeboldspark/blogs');
```

**`GET /api/public/makeboldspark/blogs/{id}`** — single blog or 404.

```typescript
export const getBlog = (id: number) =>
  apiFetch<Blog>(`/api/public/makeboldspark/blogs/${id}`);
```

**Example response**:

```json
{
  "id": 1,
  "title": "Mark Hazleton's Blog",
  "description": "Thoughts on software architecture, .NET, and the web.",
  "theme": "minimal",
  "includeFeatured": true,
  "itemsPerPage": 10,
  "cover": "/images/blog-cover.jpg",
  "logo": null,
  "headerScript": null,
  "footerScript": "<script>/* analytics */</script>",
  "analyticsListType": 1,
  "analyticsPeriod": 30,
  "createdDate": "2024-01-15T00:00:00Z",
  "updatedDate": "2026-04-20T09:00:00Z",
  "createdID": 1,
  "updatedID": 1
}
```

---

### Authors

**`GET /api/public/makeboldspark/authors`** — all authors ordered by `displayName`.

```typescript
export const getAuthors = () =>
  apiFetch<Author[]>('/api/public/makeboldspark/authors');
```

**`GET /api/public/makeboldspark/authors/{id}`** — single author or 404.

```typescript
export const getAuthor = (id: number) =>
  apiFetch<Author>(`/api/public/makeboldspark/authors/${id}`);
```

**Example response**:

```json
{
  "id": 1,
  "email": "mark@markhazleton.com",
  "password": "",
  "displayName": "Mark Hazleton",
  "bio": "Solutions Architect, lifelong learner...",
  "avatar": "data:image/svg+xml,%3Csvg ...",
  "isAdmin": true,
  "createdDate": "2024-01-01T00:00:00Z",
  "updatedDate": "2026-05-06T00:00:00Z",
  "createdID": 1,
  "updatedID": 1
}
```

> **⚠️ Do not render `password` in any public UI.** The field is present in the API response
> because it is part of the entity — never display it. If you build author profile components,
> explicitly exclude `password` from what you bind or render. See [Gotchas](#patterns-and-gotchas).

---

### Posts

**`GET /api/public/makeboldspark/posts`** — all posts ordered by `published` descending (newest
first). Accepts an optional `blogId` query parameter to filter by blog.

```typescript
export const getPosts = (blogId?: number) =>
  apiFetch<Post[]>(
    blogId != null
      ? `/api/public/makeboldspark/posts?blogId=${blogId}`
      : '/api/public/makeboldspark/posts'
  );
```

**`GET /api/public/makeboldspark/posts/{id}`** — single post or 404.

```typescript
export const getPost = (id: number) =>
  apiFetch<Post>(`/api/public/makeboldspark/posts/${id}`);
```

**Example response** (one post):

```json
{
  "id": 47,
  "authorId": 1,
  "blogId": 1,
  "title": "Building Modular APIs with ASP.NET Core Minimal APIs",
  "slug": "building-modular-apis-aspnet-core",
  "description": "A walkthrough of the feature-folder pattern using route groups.",
  "content": "<h2>Introduction</h2><p>Minimal APIs in .NET 10 allow...</p>",
  "cover": "/images/posts/minimal-apis.jpg",
  "isFeatured": true,
  "postType": 1,
  "postViews": 842,
  "published": "2026-04-15T08:00:00Z",
  "rating": 4.5,
  "selected": false,
  "createdDate": "2026-04-14T20:00:00Z",
  "updatedDate": "2026-04-15T08:00:00Z",
  "createdID": 1,
  "updatedID": 1
}
```

**Common pattern — blog post listing with featured first**:

```typescript
async function getBlogPosts(blogId: number) {
  const posts = await getPosts(blogId);
  return {
    featured: posts.filter(p => p.isFeatured),
    regular: posts.filter(p => !p.isFeatured),
  };
}
```

**Note — `content` field**: Posts store their content as HTML. Render it with
`dangerouslySetInnerHTML` (React) or `v-html` (Vue), but **sanitize first** if the content
can be edited by untrusted users:

```tsx
import DOMPurify from 'dompurify';

function PostContent({ html }: { html: string }) {
  return <article dangerouslySetInnerHTML={{ __html: DOMPurify.sanitize(html) }} />;
}
```

---

### Categories

**`GET /api/public/makeboldspark/categories`** — all categories ordered by `content` (the
category name field — see Gotchas).

```typescript
export const getCategories = () =>
  apiFetch<Category[]>('/api/public/makeboldspark/categories');
```

**`GET /api/public/makeboldspark/categories/{id}`** — single category or 404.

```typescript
export const getCategory = (id: number) =>
  apiFetch<Category>(`/api/public/makeboldspark/categories/${id}`);
```

**Example response**:

```json
[
  {
    "id": 1,
    "content": "ASP.NET Core",
    "description": "Posts about ASP.NET Core and related Microsoft technologies",
    "createdDate": "2024-01-01T00:00:00Z",
    "updatedDate": "2024-01-01T00:00:00Z",
    "createdID": 1,
    "updatedID": null
  },
  {
    "id": 2,
    "content": "Career Development",
    "description": "Advice on growing as a software professional",
    "createdDate": "2024-02-01T00:00:00Z",
    "updatedDate": "2024-02-01T00:00:00Z",
    "createdID": 1,
    "updatedID": null
  }
]
```

> **⚠️ The category "name" field is called `content`** — not `name`, not `title`. This is a
> quirk of the MakeBoldSpark.Core entity design. When rendering category labels, use `category.content`.

---

### Menus

**`GET /api/public/makeboldspark/menus`** — all menu items ordered by `displayOrder` then `title`.
Accepts an optional `domainId` query parameter to filter by site domain.

```typescript
export const getMenus = (domainId?: number) =>
  apiFetch<Menu[]>(
    domainId != null
      ? `/api/public/makeboldspark/menus?domainId=${domainId}`
      : '/api/public/makeboldspark/menus'
  );
```

**`GET /api/public/makeboldspark/menus/{id}`** — single menu item or 404.

**Example response** (selected items showing hierarchy):

```json
[
  {
    "id": 1,
    "displayOrder": 1,
    "title": "Home",
    "description": "Home page",
    "keyWords": "home, main",
    "controller": "Home",
    "action": "Index",
    "argument": null,
    "icon": "bi-house",
    "url": "/",
    "pageContent": "",
    "domainId": 1,
    "parentId": null,
    "createdDate": "2024-01-01T00:00:00Z",
    "updatedDate": "2024-01-01T00:00:00Z",
    "createdID": 1,
    "updatedID": null
  },
  {
    "id": 5,
    "displayOrder": 2,
    "title": "Articles",
    "description": "Blog articles",
    "keyWords": "articles, blog",
    "controller": "Blog",
    "action": "Index",
    "argument": null,
    "icon": "bi-journal-text",
    "url": "/articles",
    "pageContent": "",
    "domainId": 1,
    "parentId": null,
    "createdDate": "2024-01-01T00:00:00Z",
    "updatedDate": "2024-05-01T00:00:00Z",
    "createdID": 1,
    "updatedID": 1
  },
  {
    "id": 12,
    "displayOrder": 1,
    "title": "ASP.NET Core",
    "description": "Articles about ASP.NET Core",
    "keyWords": "aspnet, dotnet",
    "controller": "Blog",
    "action": "Category",
    "argument": "1",
    "icon": "bi-diagram-3",
    "url": "/articles/aspnet-core",
    "pageContent": "",
    "domainId": 1,
    "parentId": 5,
    "createdDate": "2024-02-01T00:00:00Z",
    "updatedDate": "2024-02-01T00:00:00Z",
    "createdID": 1,
    "updatedID": null
  }
]
```

**Building a navigation tree from the flat list**:

```typescript
export interface MenuNode extends Menu {
  children: MenuNode[];
}

export function buildMenuTree(flat: Menu[], rootParentId: number | null = null): MenuNode[] {
  return flat
    .filter(m => m.parentId === rootParentId)
    .sort((a, b) => a.displayOrder - b.displayOrder || a.title.localeCompare(b.title))
    .map(m => ({
      ...m,
      children: buildMenuTree(flat, m.id),
    }));
}

// Usage
const allMenus = await getMenus(1); // domain 1
const tree = buildMenuTree(allMenus);
// tree[0] = Home (parentId: null)
// tree[1] = Articles (parentId: null)
//   tree[1].children[0] = ASP.NET Core (parentId: 5)
```

---

### Keywords

**`GET /api/public/makeboldspark/keywords`** — all keywords ordered by `name`.

```typescript
export const getKeywords = () =>
  apiFetch<Keyword[]>('/api/public/makeboldspark/keywords');

export const getKeyword = (id: number) =>
  apiFetch<Keyword>(`/api/public/makeboldspark/keywords/${id}`);
```

**Example response**:

```json
[
  {
    "id": 1,
    "name": "clean-architecture",
    "description": "Articles and content about Clean Architecture patterns",
    "createdDate": "2024-03-01T00:00:00Z",
    "updatedDate": "2024-03-01T00:00:00Z",
    "createdID": 1,
    "updatedID": null
  },
  {
    "id": 2,
    "name": "dotnet",
    "description": "Everything .NET related",
    "createdDate": "2024-01-01T00:00:00Z",
    "updatedDate": "2024-01-01T00:00:00Z",
    "createdID": 1,
    "updatedID": null
  }
]
```

---

### Content Parts

**`GET /api/public/makeboldspark/content-parts`** — all content parts ordered by `title`.

```typescript
export const getContentParts = () =>
  apiFetch<ContentPart[]>('/api/public/makeboldspark/content-parts');

export const getContentPart = (id: number) =>
  apiFetch<ContentPart>(`/api/public/makeboldspark/content-parts/${id}`);
```

**Example response**:

```json
[
  {
    "id": 1,
    "title": "About Me",
    "description": "Short bio for the sidebar",
    "content": "<p>I'm a Solutions Architect with 25+ years of experience...</p>",
    "createdDate": "2024-01-01T00:00:00Z",
    "updatedDate": "2026-02-14T09:00:00Z",
    "createdID": 1,
    "updatedID": 1
  }
]
```

**Common use case — embedding a reusable content block by title**:

```typescript
async function getContentPartByTitle(title: string): Promise<ContentPart | undefined> {
  const parts = await getContentParts();
  return parts.find(p => p.title.toLowerCase() === title.toLowerCase());
}

const aboutMe = await getContentPartByTitle('About Me');
```

---

## Admin Endpoints

All `POST`, `PUT`, `DELETE` routes under `/api/admin/makeboldspark/*` and all
`GET /api/admin/makeboldspark/subscribers`, `newsletters`, `mail-settings` require:

```text
Authorization: Bearer <token>   (Admin role)
```

**401** — no or invalid token.  
**403** — valid token, but user is not in `Admin` role.

---

### Subscribers

**`GET /api/admin/makeboldspark/subscribers`** — all subscribers ordered by `email` (Admin only).

```typescript
export const getSubscribers = (token: string) =>
  authedFetch<Subscriber[]>(token, '/api/admin/makeboldspark/subscribers');
```

**`POST /api/admin/makeboldspark/subscribers`** — add a subscriber:

```typescript
export async function createSubscriber(
  token: string,
  data: { email: string; blogId: number; country?: string; region?: string; ip?: string }
): Promise<Subscriber> {
  return authedFetch<Subscriber>(token, '/api/admin/makeboldspark/subscribers', {
    method: 'POST',
    body: JSON.stringify(data),
  });
}
```

**`PUT /api/admin/makeboldspark/subscribers/{id}`** — update a subscriber.  
**`DELETE /api/admin/makeboldspark/subscribers/{id}`** — delete a subscriber.

---

### Newsletters

Newsletters record that a post was sent as a newsletter email.

**`GET /api/admin/makeboldspark/newsletters`** — all newsletters ordered by `createdDate`
descending (newest first).

```typescript
export const getNewsletters = (token: string) =>
  authedFetch<Newsletter[]>(token, '/api/admin/makeboldspark/newsletters');
```

**`POST /api/admin/makeboldspark/newsletters`** — record a sent newsletter:

```typescript
export async function createNewsletter(
  token: string,
  data: { postId: number; success: boolean }
): Promise<Newsletter> {
  return authedFetch<Newsletter>(token, '/api/admin/makeboldspark/newsletters', {
    method: 'POST',
    body: JSON.stringify(data),
  });
}
```

**`DELETE /api/admin/makeboldspark/newsletters/{id}`** — delete a newsletter record (no PUT — newsletters are append-only by design).

---

### Mail Settings

> **⚠️ Sensitive** — `MailSetting` records contain SMTP credentials (`userPassword`). This
> endpoint is Admin-only for this reason. Never expose mail settings responses to non-admin
> users or log them.

**`GET /api/admin/makeboldspark/mail-settings`** — all mail settings ordered by `fromEmail`.

```typescript
export const getMailSettings = (token: string) =>
  authedFetch<MailSetting[]>(token, '/api/admin/makeboldspark/mail-settings');
```

**`POST /api/admin/makeboldspark/mail-settings`** — create a mail setting:

```typescript
export async function createMailSetting(
  token: string,
  data: {
    host: string;
    port: number;
    userEmail: string;
    userPassword: string;
    fromName: string;
    fromEmail: string;
    toName: string;
    enabled: boolean;
    blogId: number;
  }
): Promise<MailSetting> {
  return authedFetch<MailSetting>(token, '/api/admin/makeboldspark/mail-settings', {
    method: 'POST',
    body: JSON.stringify(data),
  });
}
```

**`PUT /api/admin/makeboldspark/mail-settings/{id}`** — full replace (same field set as POST).  
**`DELETE /api/admin/makeboldspark/mail-settings/{id}`** — delete.

---

### Admin CRUD for Public Entities

Every entity that is publicly readable also has admin write endpoints. The pattern is
identical for all of them: POST to create, PUT `/{id}` to full-replace, DELETE `/{id}` to remove.
All inherit the `AdminOnly` policy from the `/api/admin` route group.

```typescript
// Generic admin CRUD factory
function adminCrud<T extends BaseEntity>(resource: string) {
  const base = `/api/admin/makeboldspark/${resource}`;
  return {
    create: (token: string, data: Omit<T, keyof BaseEntity>) =>
      authedFetch<T>(token, base, { method: 'POST', body: JSON.stringify(data) }),

    update: (token: string, id: number, data: Partial<T>) =>
      authedFetch<T>(token, `${base}/${id}`, {
        method: 'PUT',
        body: JSON.stringify({ ...data, id }),
      }),

    delete: (token: string, id: number) =>
      authedFetch<void>(token, `${base}/${id}`, { method: 'DELETE' }),
  };
}

export const domains     = adminCrud<WebSite>('domains');
export const blogs       = adminCrud<Blog>('blogs');
export const authors     = adminCrud<Author>('authors');
export const posts       = adminCrud<Post>('posts');
export const categories  = adminCrud<Category>('categories');
export const menus       = adminCrud<Menu>('menus');
export const keywords    = adminCrud<Keyword>('keywords');
export const contentParts = adminCrud<ContentPart>('content-parts');

// Usage
await domains.create(token, {
  name: 'My New Site',
  description: 'A portfolio site',
  domainUrl: 'https://example.com',
  title: 'Example',
  template: 'bootstrap5',
  galleryFolder: 'images',
  useBreadCrumbUrl: false,
  versionNo: 1,
  style: 'light',
  isRecipeSite: false,
});
```

**Full-replace semantics on PUT**: Like the Recipe API, PUT replaces the whole record.
Always fetch the current record first, spread it, and override only the changed fields:

```typescript
const current = await getDomain(1);
await domains.update(token, 1, { ...current, versionNo: current.versionNo + 1 });
```

---

## Error Handling

| Status | Meaning | Action |
|---|---|---|
| 200 | OK | Parse body |
| 201 | Created | Parse body — new resource with assigned `id` |
| 204 | No Content | No body (DELETE success) |
| 400 | Bad Request | Check request body |
| 401 | Unauthorized | Token missing/expired — redirect to login |
| 403 | Forbidden | Wrong role — check user permissions |
| 404 | Not Found | Resource does not exist |
| 500 | Server Error | Usually a constraint violation — check FK fields |

```typescript
type ApiResult<T> = { ok: true; data: T } | { ok: false; status: number; message: string };

async function safeApiFetch<T>(fn: () => Promise<T>): Promise<ApiResult<T>> {
  try {
    return { ok: true, data: await fn() };
  } catch (err) {
    if (err instanceof ApiError) {
      return { ok: false, status: err.status, message: err.body };
    }
    throw err;
  }
}

// Usage
const result = await safeApiFetch(() => getPost(id));
if (!result.ok) {
  if (result.status === 404) return null;
  console.error('Post load failed:', result.message);
  return null;
}
return result.data;
```

---

## Patterns and Gotchas

### `Category.content` is the display name

The blog `Category` entity uses a field named `content` for what is conceptually the
category name. This is a naming quirk from the original MakeBoldSpark.Core data model:

```typescript
// ❌ Wrong
categories.map(c => c.name)    // undefined

// ✅ Correct
categories.map(c => c.content) // "ASP.NET Core", "Career Development", ...
```

### `Author.password` is always in the response

The `Author` entity includes a `password` field. The seed data now stores `""` (empty string)
for this field, but the field itself is present on every author response. Never render it,
log it, or pass it to analytics:

```typescript
// Build a safe public author type
type PublicAuthor = Omit<Author, 'password'>;

function toPublicAuthor({ password: _, ...rest }: Author): PublicAuthor {
  return rest;
}

const authors = (await getAuthors()).map(toPublicAuthor);
```

### `Post.content` is HTML

Post content is stored and returned as HTML. When rendering in a framework that escapes
by default (React, Vue, Angular), you must explicitly opt into raw HTML rendering AND
sanitize:

```tsx
// React — sanitize before rendering
import DOMPurify from 'dompurify';

<div dangerouslySetInnerHTML={{ __html: DOMPurify.sanitize(post.content) }} />
```

```vue
<!-- Vue — sanitize before rendering -->
<div v-html="sanitized(post.content)" />
```

### Menus are a flat list — build the tree yourself

`GET /api/public/makeboldspark/menus` returns a flat array. The hierarchy is encoded via
`parentId`. Use `buildMenuTree()` (shown in the [Menus section](#menus)) to assemble
a nested structure for rendering navigation.

### Posts are not filtered by blog on the list endpoint unless you pass `blogId`

Without `?blogId=N`, you get **all posts from all blogs** sorted by `published` descending.
If your site has multiple blogs, always pass `blogId` to scope the response:

```typescript
// ❌ Gets all posts from all blogs — probably not what you want
const posts = await getPosts();

// ✅ Scoped to blog 1
const posts = await getPosts(1);
```

### Menus are scoped by `domainId` — always pass it for multi-domain setups

Similarly, menus returned without `?domainId=N` include menus from ALL domains:

```typescript
const DOMAIN_ID = 1; // your site's domain ID

export const getSiteMenus = () => getMenus(DOMAIN_ID);
```

### Navigation properties are not eagerly loaded

The API returns flat entity objects. Navigation properties (`blog.posts`, `post.blog`,
`author.posts`, `menu.keywords`, etc.) are **not included** in GET responses — they appear
as null or absent in the JSON. To show related data you must make multiple requests:

```typescript
// Get a post and its blog info
const [post, blog] = await Promise.all([
  getPost(47),
  getBlog(1),  // you must know the blogId from the post
]);
```

### `MailSetting.userPassword` is never safe for a client

Even inside an admin UI, consider masking this value after initial load. Treat it like
a form password field — display as `••••••••`, only send it to the API on an explicit save:

```typescript
// In your admin state — never store clear-text password in component state beyond a form
const [showPassword, setShowPassword] = useState(false);

<input
  type={showPassword ? 'text' : 'password'}
  value={mailSetting.userPassword}
  onChange={e => setMailSetting(prev => ({ ...prev, userPassword: e.target.value }))}
/>
```

### Full-replace PUT semantics — always spread the current record

All PUT endpoints are full replacements, not partial updates (no PATCH). Sending a
partial body will reset omitted fields to their default values:

```typescript
// ❌ Resets all fields except title to defaults
await domains.update(token, 1, { title: 'New Title' });

// ✅ Preserves all existing fields
const current = await getDomain(1);
await domains.update(token, 1, { ...current, title: 'New Title' });
```

### 500 usually means a FK constraint

The API does not return structured validation errors for constraint violations. A 500
from a create or delete operation almost always means:

- **Create**: a required FK field points to a non-existent parent record
- **Delete**: the record has child records that prevent deletion

Check your `blogId`, `authorId`, `domainId` values before creating records, and check
for dependent children before deleting parents.
