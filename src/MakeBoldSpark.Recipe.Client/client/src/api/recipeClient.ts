export const SESSION_EXPIRED_EVENT = 'makeboldspark-recipe-session-expired';

export class ApiError extends Error {
  constructor(
    public status: number,
    message: string,
  ) {
    super(message);
  }
}

export type Domain = {
  id: number;
  name: string;
};

export type Category = {
  id: number;
  name: string;
  description: string;
  displayOrder: number;
  isActive: boolean;
  domainId: number;
  version: number;
};

export type Recipe = {
  id: number;
  name: string;
  description: string;
  authorName: string;
  ingredients: string;
  instructions: string;
  servings: number;
  isApproved: boolean;
  domainId: number;
  recipeCategoryId: number;
  recipeCategoryName: string;
  version: number;
  updatedDate: string;
};

export type RecipeWrite = Omit<Recipe, 'id' | 'domainId' | 'recipeCategoryName' | 'version' | 'updatedDate'>;
export type CategoryWrite = Omit<Category, 'id' | 'domainId' | 'version'>;
export type Page<T> = { items: T[]; page: number; pageSize: number; totalCount: number };

const BASE_URL = '/api/publish/recipes';

async function request<T>(token: string, path: string, init: RequestInit = {}) {
  const headers = new Headers(init.headers);
  headers.set('Authorization', `Bearer ${token}`);
  if (init.body && !headers.has('Content-Type')) headers.set('Content-Type', 'application/json');

  const response = await fetch(`${BASE_URL}${path}`, { ...init, headers });
  if (response.status === 401) window.dispatchEvent(new Event(SESSION_EXPIRED_EVENT));
  if (!response.ok) throw new ApiError(response.status, await response.text());
  if (response.status === 204) return undefined as T;
  return (await response.json()) as T;
}

function domainQuery(domainId: number) {
  return `domainId=${encodeURIComponent(String(domainId))}`;
}

function withVersion(version: number) {
  return { 'If-Match': `"${version}"` };
}

export const recipeApi = {
  domains: (token: string) => request<Domain[]>(token, '/domains'),
  recipes: (token: string, domainId: number, page = 1, pageSize = 50) =>
    request<Page<Recipe>>(token, `?${domainQuery(domainId)}&page=${page}&pageSize=${pageSize}`),
  recipe: (token: string, domainId: number, id: number) =>
    request<Recipe>(token, `/${id}?${domainQuery(domainId)}`),
  categories: (token: string, domainId: number) =>
    request<Category[]>(token, `/categories?${domainQuery(domainId)}`),
  saveRecipe: (token: string, domainId: number, recipe: Partial<Recipe>) => {
    const body: RecipeWrite = {
      name: recipe.name ?? '',
      description: recipe.description ?? '',
      authorName: recipe.authorName ?? '',
      ingredients: recipe.ingredients ?? '',
      instructions: recipe.instructions ?? '',
      servings: recipe.servings ?? 0,
      recipeCategoryId: recipe.recipeCategoryId ?? 0,
      isApproved: recipe.isApproved ?? false,
    };

    return recipe.id
      ? request<Recipe>(token, `/${recipe.id}?${domainQuery(domainId)}`, {
          method: 'PUT',
          headers: withVersion(recipe.version ?? 0),
          body: JSON.stringify(body),
        })
      : request<Recipe>(token, `?${domainQuery(domainId)}`, {
          method: 'POST',
          body: JSON.stringify(body),
        });
  },
  deleteRecipe: (token: string, domainId: number, recipe: Recipe) =>
    request<void>(token, `/${recipe.id}?${domainQuery(domainId)}`, {
      method: 'DELETE',
      headers: withVersion(recipe.version),
    }),
  saveCategory: (token: string, domainId: number, category: Partial<Category>) => {
    const body: CategoryWrite = {
      name: category.name ?? '',
      description: category.description ?? '',
      displayOrder: category.displayOrder ?? 0,
      isActive: category.isActive ?? true,
    };

    return category.id
      ? request<Category>(token, `/categories/${category.id}?${domainQuery(domainId)}`, {
          method: 'PUT',
          headers: withVersion(category.version ?? 0),
          body: JSON.stringify(body),
        })
      : request<Category>(token, `/categories?${domainQuery(domainId)}`, {
          method: 'POST',
          body: JSON.stringify(body),
        });
  },
  deleteCategory: (token: string, domainId: number, category: Category) =>
    request<void>(token, `/categories/${category.id}?${domainQuery(domainId)}`, {
      method: 'DELETE',
      headers: withVersion(category.version),
    }),
};
