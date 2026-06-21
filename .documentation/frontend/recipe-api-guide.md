# Recipe API — Frontend Developer Guide

**Base path**: `/api/public/recipes` (anonymous) · `/api/publish/recipes` (Publisher role)  
**Data source**: `MakeBoldSpark.Recipe` domain library → SQLite via EF Core  
**Spec file**: `../specs/002-recipe-api/contracts/recipe.yaml`

---

## Table of Contents

1. [Mental Model](#mental-model)
2. [TypeScript Types](#typescript-types)
3. [Client Setup](#client-setup)
4. [Public Endpoints](#public-endpoints)
   - [List Approved Recipes](#get-apipublicrecipes)
   - [Get Recipe by ID](#get-apipublicrecipesid)
   - [List Recipe Categories](#get-apipublicrecipescategories)
5. [Publisher Endpoints](#publisher-endpoints)
   - [Create Recipe](#post-apipublishrecipes)
   - [Update Recipe](#put-apipublishrecipesid)
   - [Delete Recipe](#delete-apipublishrecipesid)
   - [Create Category](#post-apipublishrecipescategories)
   - [Update Category](#put-apipublishrecipescategoriesid)
   - [Delete Category](#delete-apipublishrecipescategoriesid)
6. [Error Handling](#error-handling)
7. [Patterns and Gotchas](#patterns-and-gotchas)

---

## Mental Model

The Recipe API wraps the `MakeBoldSpark.Recipe` domain library. The key concepts:

- **RecipeModel** — a recipe with ingredients, instructions, author info, images, ratings, and a category
- **RecipeCategoryModel** — a named grouping; categories contain a `recipes` array (be aware of payload size)
- **Approved filter** — `GET /api/public/recipes` returns **only** recipes where `isApproved === true`. Unapproved recipes are invisible to public callers, even if you know the ID — `GET /api/public/recipes/{id}` will return **404** for unapproved records
- **Category FK** — every recipe requires a valid `recipeCategoryID`. Creating a recipe without a matching category will fail at the DB layer with a FK constraint error

---

## TypeScript Types

Derived from the actual `RecipeModel` and `RecipeCategoryModel` C# classes. Property names
are camelCase (ASP.NET Core JSON serialization default).

```typescript
export interface RecipeCategory {
  id: number;
  name: string;
  description: string;
  displayOrder: number;
  isActive: boolean;
  url: string;
  domainID: number;
  recipes: Recipe[];           // populated — can be large; see Gotchas
}

export interface RecipeImage {
  id: number;
  fileName: string;
  fileDescription: string;
  recipeId: number;
}

export interface Recipe {
  id: number;
  name: string;
  description: string;
  ingredients: string;         // free-text, often multi-line
  instructions: string;        // free-text, often multi-line
  authorNM: string;            // display name, not a user ID
  isApproved: boolean;         // always true on public list; see Gotchas
  servings: number;
  averageRating: number;       // 0–5
  ratingCount: number;
  commentCount: number;
  viewCount: number;
  lastViewDT: string;          // ISO 8601 datetime
  modifiedDT: string;          // ISO 8601 datetime
  modifiedID: number;
  recipeCategoryID: number;
  recipeCategoryNM: string;    // denormalized category name
  recipeCategory: RecipeCategory;
  recipeURL: string;
  fileName: string;            // primary image file name (legacy)
  fileDescription: string;     // primary image description (legacy)
  images: RecipeImage[];       // additional images
  domainID: number;
  seoKeywords: string;
  recipeCategories: RecipeOption[]; // dropdown options, populated on forms
}

export interface RecipeOption {
  value: number;
  text: string;
}
```

---

## Client Setup

```typescript
const API_BASE = import.meta.env.VITE_API_URL ?? 'https://api.markhazleton.com';

async function apiFetch<T>(
  path: string,
  options: RequestInit = {}
): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, {
    headers: {
      'Content-Type': 'application/json',
      ...options.headers,
    },
    ...options,
  });

  if (!res.ok) {
    const body = await res.text();
    throw new ApiError(res.status, body);
  }

  return res.json() as Promise<T>;
}

class ApiError extends Error {
  constructor(public status: number, public body: string) {
    super(`API ${status}: ${body}`);
  }
}

// Authenticated variant — pass the JWT token from your auth provider
function authedFetch<T>(token: string, path: string, options: RequestInit = {}) {
  return apiFetch<T>(path, {
    ...options,
    headers: {
      Authorization: `Bearer ${token}`,
      ...options.headers,
    },
  });
}
```

---

## Public Endpoints

### `GET /api/public/recipes`

Returns all **approved** recipes. Unapproved recipes are excluded server-side.

**Response**: `Recipe[]`

**Example response** (one item shown):

```json
[
  {
    "id": 12,
    "name": "Grandma's Apple Pie",
    "description": "A classic double-crust apple pie with cinnamon filling.",
    "ingredients": "6 cups thinly sliced apples\n3/4 cup sugar\n1 tsp cinnamon\n2 tbsp butter\nPie crust (top and bottom)",
    "instructions": "Preheat oven to 425°F.\nFill bottom crust with apple mixture.\nDot with butter, add top crust.\nBake 45–50 minutes until golden.",
    "authorNM": "Mark Hazleton",
    "isApproved": true,
    "servings": 8,
    "averageRating": 4.7,
    "ratingCount": 23,
    "commentCount": 5,
    "viewCount": 312,
    "lastViewDT": "2026-05-09T14:22:00Z",
    "modifiedDT": "2026-04-01T10:00:00Z",
    "modifiedID": 1,
    "recipeCategoryID": 3,
    "recipeCategoryNM": "Desserts",
    "recipeCategory": {
      "id": 3,
      "name": "Desserts",
      "description": "Sweet treats and baked goods",
      "displayOrder": 3,
      "isActive": true,
      "url": "desserts",
      "domainID": 1,
      "recipes": []
    },
    "recipeURL": "grandmas-apple-pie",
    "fileName": "apple-pie.jpg",
    "fileDescription": "Golden apple pie fresh from the oven",
    "images": [],
    "domainID": 1,
    "seoKeywords": "apple pie, dessert, classic, homemade",
    "recipeCategories": []
  }
]
```

**Usage**:

```typescript
import type { Recipe } from './types';

export async function getApprovedRecipes(): Promise<Recipe[]> {
  return apiFetch<Recipe[]>('/api/public/recipes');
}

// React example
function RecipeList() {
  const [recipes, setRecipes] = useState<Recipe[]>([]);

  useEffect(() => {
    getApprovedRecipes().then(setRecipes).catch(console.error);
  }, []);

  return (
    <ul>
      {recipes.map(r => (
        <li key={r.id}>
          <strong>{r.name}</strong> — {r.recipeCategoryNM} ({r.servings} servings)
        </li>
      ))}
    </ul>
  );
}
```

**Gotcha — `recipeCategory.recipes` is empty on list**: When recipes are returned from the
list endpoint, the nested `recipeCategory.recipes` array is `[]`. The API does not eagerly
load the full category recipe list inside each recipe to avoid circular/explosive payloads.
Use the categories endpoint if you need the full category→recipe tree.

---

### `GET /api/public/recipes/{id}`

Returns a single recipe. Returns **404** if:

- The ID does not exist
- The recipe exists but `isApproved === false`

**Response**: `Recipe` (same shape as list item)

**Usage**:

```typescript
export async function getRecipeById(id: number): Promise<Recipe | null> {
  try {
    return await apiFetch<Recipe>(`/api/public/recipes/${id}`);
  } catch (err) {
    if (err instanceof ApiError && err.status === 404) return null;
    throw err;
  }
}

// Next.js page example
export async function getStaticProps({ params }: { params: { id: string } }) {
  const recipe = await getRecipeById(Number(params.id));
  if (!recipe) return { notFound: true };
  return { props: { recipe }, revalidate: 3600 };
}
```

**Gotcha — ID 0 is always 404**: The underlying domain library uses `Id == 0` as its
"not found" sentinel. A request for `/api/public/recipes/0` will always return 404 even
though 0 is a technically valid integer.

---

### `GET /api/public/recipes/categories`

Returns all recipe categories. Each category includes an empty `recipes` array at this
endpoint — recipes are **not** nested here.

**Response**: `RecipeCategory[]`

**Example response**:

```json
[
  {
    "id": 1,
    "name": "Breakfast",
    "description": "Morning meals and brunch items",
    "displayOrder": 1,
    "isActive": true,
    "url": "breakfast",
    "domainID": 1,
    "recipes": []
  },
  {
    "id": 2,
    "name": "Main Dishes",
    "description": "Entrées and main courses",
    "displayOrder": 2,
    "isActive": true,
    "url": "main-dishes",
    "domainID": 1,
    "recipes": []
  },
  {
    "id": 3,
    "name": "Desserts",
    "description": "Sweet treats and baked goods",
    "displayOrder": 3,
    "isActive": true,
    "url": "desserts",
    "domainID": 1,
    "recipes": []
  }
]
```

**Usage**:

```typescript
export async function getRecipeCategories(): Promise<RecipeCategory[]> {
  return apiFetch<RecipeCategory[]>('/api/public/recipes/categories');
}

// Build a category filter for a recipe listing page
function CategoryFilter({
  onSelect,
}: {
  onSelect: (categoryId: number | null) => void;
}) {
  const [categories, setCategories] = useState<RecipeCategory[]>([]);

  useEffect(() => {
    getRecipeCategories().then(setCategories);
  }, []);

  return (
    <select onChange={e => onSelect(e.target.value ? Number(e.target.value) : null)}>
      <option value="">All Categories</option>
      {categories.map(c => (
        <option key={c.id} value={c.id}>{c.name}</option>
      ))}
    </select>
  );
}
```

**Note — no server-side filter by category**: There is no `?categoryId=` query param on
`GET /api/public/recipes`. Filter client-side after fetching all recipes:

```typescript
const filtered = recipes.filter(r => r.recipeCategoryID === selectedCategoryId);
```

---

## Publisher Endpoints

All `/api/publish/recipes/*` routes require a JWT token with `Admin` or `Publisher` role.

```text
Authorization: Bearer <token>
```

A **401** means no token or an expired/invalid token.  
A **403** means the token is valid but the user lacks the required role.

---

### `POST /api/publish/recipes`

Creates a new recipe. The new recipe is **not approved** by default — it will not appear
in `GET /api/public/recipes` until `isApproved` is set to `true`.

**Request body**: `Partial<Recipe>` with required fields:

| Field | Required | Notes |
|---|---|---|
| `name` | ✅ | max 150 chars |
| `description` | ✅ | |
| `ingredients` | ✅ | free text, newlines OK |
| `instructions` | ✅ | free text, newlines OK |
| `authorNM` | ✅ | max 50 chars |
| `recipeCategoryID` | ✅ | must reference an existing category |
| `isApproved` | optional | defaults to `false` |
| `servings` | optional | defaults to `0` |
| `domainID` | optional | defaults to the library's `INT_MOM_DomainId` constant |
| `seoKeywords` | optional | |

**Response**: `Recipe` (201 Created with the saved object including assigned `id`)

**Example**:

```typescript
export async function createRecipe(
  token: string,
  data: {
    name: string;
    description: string;
    ingredients: string;
    instructions: string;
    authorNM: string;
    recipeCategoryID: number;
    isApproved?: boolean;
    servings?: number;
    seoKeywords?: string;
  }
): Promise<Recipe> {
  return authedFetch<Recipe>(token, '/api/publish/recipes', {
    method: 'POST',
    body: JSON.stringify(data),
  });
}

// Usage
const newRecipe = await createRecipe(token, {
  name: 'Lemon Bars',
  description: 'Tangy lemon curd on a buttery shortbread crust.',
  ingredients: '1 cup flour\n1/4 cup powdered sugar\n1/2 cup butter\n4 eggs\n1 1/2 cups sugar\n1/4 cup lemon juice',
  instructions: 'Make shortbread crust. Bake 15 min at 350°F.\nMix lemon filling. Pour over crust. Bake 25 min.',
  authorNM: 'Mark Hazleton',
  recipeCategoryID: 3,
  isApproved: false,
  servings: 16,
  seoKeywords: 'lemon bars, dessert, easy',
});
console.log('Created recipe id:', newRecipe.id);
```

**Gotcha — FK constraint on `recipeCategoryID`**: If the category ID you supply does not
exist in the database, the server will return a **500** (the EF Core FK violation bubbles up
without a friendly error message). Always fetch and validate categories before creating a
recipe.

---

### `PUT /api/publish/recipes/{id}`

Replaces a recipe. The request body is the full `RecipeModel` — omitted fields may reset
to defaults. Returns **404** if the ID does not exist.

**Response**: `Recipe`

**Example**:

```typescript
export async function updateRecipe(
  token: string,
  id: number,
  data: Partial<Recipe>
): Promise<Recipe | null> {
  try {
    return await authedFetch<Recipe>(token, `/api/publish/recipes/${id}`, {
      method: 'PUT',
      body: JSON.stringify({ ...data, id }),
    });
  } catch (err) {
    if (err instanceof ApiError && err.status === 404) return null;
    throw err;
  }
}

// Approve a recipe
const existing = await getRecipeById(42);
if (existing) {
  await updateRecipe(token, 42, { ...existing, isApproved: true });
}
```

**Pattern — always fetch before update**: The API does a full replace, not a PATCH. Fetch
the current record first, spread its values, then override only what you intend to change.

---

### `DELETE /api/publish/recipes/{id}`

Deletes a recipe. Returns **204 No Content** on success, **404** if the ID does not exist.

**Example**:

```typescript
export async function deleteRecipe(token: string, id: number): Promise<boolean> {
  try {
    await authedFetch<void>(token, `/api/publish/recipes/${id}`, { method: 'DELETE' });
    return true;
  } catch (err) {
    if (err instanceof ApiError && err.status === 404) return false;
    throw err;
  }
}
```

---

### `POST /api/publish/recipes/categories`

Creates a new recipe category.

**Request body**:

| Field | Required | Notes |
|---|---|---|
| `name` | ✅ | max 70 chars |
| `description` | optional | max 1500 chars |
| `displayOrder` | optional | integer; lower = first |
| `isActive` | optional | defaults to `false` |
| `domainID` | optional | |

**Response**: `RecipeCategory` (201 Created)

**Example**:

```typescript
export async function createCategory(
  token: string,
  data: { name: string; description?: string; displayOrder?: number; isActive?: boolean }
): Promise<RecipeCategory> {
  return authedFetch<RecipeCategory>(token, '/api/publish/recipes/categories', {
    method: 'POST',
    body: JSON.stringify(data),
  });
}
```

---

### `PUT /api/publish/recipes/categories/{id}`

Updates a category. Full replace semantics — same pattern as recipe update.

Returns **404** if the ID does not exist.

**Example**:

```typescript
export async function updateCategory(
  token: string,
  id: number,
  data: Partial<RecipeCategory>
): Promise<RecipeCategory | null> {
  try {
    return await authedFetch<RecipeCategory>(token, `/api/publish/recipes/categories/${id}`, {
      method: 'PUT',
      body: JSON.stringify({ ...data, id }),
    });
  } catch (err) {
    if (err instanceof ApiError && err.status === 404) return null;
    throw err;
  }
}
```

---

### `DELETE /api/publish/recipes/categories/{id}`

Deletes a category. Returns **204** on success, **404** if not found.

**Gotcha — FK constraint**: Deleting a category that still has recipes assigned will fail
with a **500** (SQLite FK restrict). Delete or re-categorize all recipes in the category
before deleting it.

```typescript
export async function deleteCategory(token: string, id: number): Promise<boolean> {
  try {
    await authedFetch<void>(token, `/api/publish/recipes/categories/${id}`, { method: 'DELETE' });
    return true;
  } catch (err) {
    if (err instanceof ApiError && err.status === 404) return false;
    throw err;
  }
}
```

---

## Error Handling

| Status | Meaning | Action |
|---|---|---|
| 200 | OK | Parse body |
| 201 | Created | Parse body — contains the new resource |
| 204 | No Content | No body to parse |
| 400 | Bad Request | Validation error — check request body |
| 401 | Unauthorized | Token missing or expired — redirect to login |
| 403 | Forbidden | Token valid but wrong role |
| 404 | Not Found | Resource doesn't exist or isn't approved |
| 500 | Server Error | Usually a FK constraint violation — check your payload |

```typescript
async function safeCall<T>(fn: () => Promise<T>): Promise<{ data: T } | { error: ApiError }> {
  try {
    return { data: await fn() };
  } catch (err) {
    if (err instanceof ApiError) return { error: err };
    throw err;
  }
}

// Usage
const result = await safeCall(() => getRecipeById(id));
if ('error' in result) {
  if (result.error.status === 404) return <NotFound />;
  return <ErrorMessage message={result.error.message} />;
}
const recipe = result.data;
```

---

## Patterns and Gotchas

### Only approved recipes are public

`GET /api/public/recipes` applies a server-side filter: `WHERE isApproved = 1`. There is
no way to see unapproved recipes through the public API — not by ID, not by listing. This
is by design. Publishing workflow:

1. `POST /api/publish/recipes` with `isApproved: false` (draft)
2. Review the draft via an admin UI
3. `PUT /api/publish/recipes/{id}` with `isApproved: true`
4. Recipe appears in the public listing on the next request

### Client-side category filtering

The API has no `?categoryId=` filter. For a filtered recipe listing, load all recipes once
and filter in the browser. With reasonable recipe counts (< 1000) this is fast and allows
instant filtering without round trips:

```typescript
const [selectedCategory, setSelectedCategory] = useState<number | null>(null);
const visible = useMemo(
  () => selectedCategory === null
    ? recipes
    : recipes.filter(r => r.recipeCategoryID === selectedCategory),
  [recipes, selectedCategory]
);
```

### Rendering ingredients and instructions

Both fields are free-text and commonly contain newlines (`\n`). Render them as `<pre>` or
convert newlines to `<br>` tags — do not display them in a single-line element:

```tsx
function RecipeInstructions({ text }: { text: string }) {
  return (
    <ol>
      {text.split('\n').filter(Boolean).map((step, i) => (
        <li key={i}>{step}</li>
      ))}
    </ol>
  );
}
```

### Image handling

`fileName` and `fileDescription` are legacy single-image fields from before the `images`
array was added. Modern recipes use `images[]`. Check both:

```typescript
function getPrimaryImage(recipe: Recipe): { src: string; alt: string } | null {
  if (recipe.images.length > 0) {
    return { src: recipe.images[0].fileName, alt: recipe.images[0].fileDescription };
  }
  if (recipe.fileName) {
    return { src: recipe.fileName, alt: recipe.fileDescription };
  }
  return null;
}
```

### `domainID` scoping

Recipes are scoped to a `domainID` (a site/tenant identifier from the MakeBoldSpark.Core library).
The default (`INT_MOM_DomainId`) is set by the domain library constant. If your site runs
multiple domains sharing the same database, you may need to filter by `domainID` client-side.

### Avoid sending `recipeCategories` on write

The `recipeCategories` field (an array of dropdown options) is only meaningful for MVC form
rendering. When `POST`-ing or `PUT`-ing, send it as `[]` or omit it. Sending a populated
array has no effect and wastes bandwidth.
