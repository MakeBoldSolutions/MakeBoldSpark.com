import { afterEach, describe, expect, it, vi } from 'vitest';
import { recipeApi } from './recipeClient';

function jsonResponse(body: unknown, status = 200) {
  return Promise.resolve({
    ok: status >= 200 && status < 300,
    status,
    json: () => Promise.resolve(body),
    text: () => Promise.resolve(JSON.stringify(body)),
  } as Response);
}

describe('recipeApi', () => {
  afterEach(() => vi.restoreAllMocks());

  it('loads protected configured domains with the publisher token', async () => {
    const fetchSpy = vi.spyOn(globalThis, 'fetch').mockResolvedValue({
      ok: true,
      status: 200,
      json: () => Promise.resolve([{ id: 1, name: 'Recipes' }]),
      text: () => Promise.resolve(''),
    } as Response);

    await recipeApi.domains('test-token');

    expect(fetchSpy).toHaveBeenCalledWith('/api/publish/recipes/domains', expect.objectContaining({ headers: expect.any(Headers) }));
    const headers = fetchSpy.mock.calls[0][1]?.headers as Headers;
    expect(headers.get('Authorization')).toBe('Bearer test-token');
  });

  it('loads domain-scoped recipe inventory with bounded pagination query values', async () => {
    const fetchSpy = vi.spyOn(globalThis, 'fetch').mockImplementation(() => jsonResponse({ items: [], page: 2, pageSize: 25, totalCount: 0 }));

    await recipeApi.recipes('test-token', 7, 2, 25);

    expect(fetchSpy).toHaveBeenCalledWith('/api/publish/recipes?domainId=7&page=2&pageSize=25', expect.any(Object));
  });

  it('sends If-Match when updating and deleting recipes', async () => {
    const fetchSpy = vi.spyOn(globalThis, 'fetch').mockImplementation(() =>
      jsonResponse({
        id: 3,
        name: 'Updated',
        description: '',
        authorName: 'Tester',
        ingredients: 'Water',
        instructions: 'Stir',
        servings: 1,
        isApproved: false,
        domainId: 7,
        recipeCategoryId: 11,
        recipeCategoryName: 'Dinner',
        version: 6,
        updatedDate: '2026-06-26T00:00:00Z',
      }),
    );

    await recipeApi.saveRecipe('test-token', 7, {
      id: 3,
      name: 'Updated',
      authorName: 'Tester',
      ingredients: 'Water',
      instructions: 'Stir',
      servings: 1,
      recipeCategoryId: 11,
      version: 5,
    });
    await recipeApi.deleteRecipe('test-token', 7, {
      id: 3,
      name: 'Updated',
      description: '',
      authorName: 'Tester',
      ingredients: 'Water',
      instructions: 'Stir',
      servings: 1,
      isApproved: false,
      domainId: 7,
      recipeCategoryId: 11,
      recipeCategoryName: 'Dinner',
      version: 6,
      updatedDate: '2026-06-26T00:00:00Z',
    });

    const updateHeaders = fetchSpy.mock.calls[0][1]?.headers as Headers;
    const deleteHeaders = fetchSpy.mock.calls[1][1]?.headers as Headers;
    expect(updateHeaders.get('If-Match')).toBe('"5"');
    expect(deleteHeaders.get('If-Match')).toBe('"6"');
  });

  it('loads and mutates domain-scoped categories with conditional headers', async () => {
    const fetchSpy = vi.spyOn(globalThis, 'fetch').mockImplementation(() =>
      jsonResponse([
        {
          id: 11,
          name: 'Dinner',
          description: '',
          displayOrder: 1,
          isActive: true,
          domainId: 7,
          version: 3,
        },
      ]),
    );

    await recipeApi.categories('test-token', 7);
    await recipeApi.saveCategory('test-token', 7, {
      id: 11,
      name: 'Dinner',
      description: '',
      displayOrder: 2,
      isActive: true,
      version: 3,
    });
    await recipeApi.deleteCategory('test-token', 7, {
      id: 11,
      name: 'Dinner',
      description: '',
      displayOrder: 2,
      isActive: true,
      domainId: 7,
      version: 4,
    });

    expect(fetchSpy.mock.calls[0][0]).toBe('/api/publish/recipes/categories?domainId=7');
    expect((fetchSpy.mock.calls[1][1]?.headers as Headers).get('If-Match')).toBe('"3"');
    expect((fetchSpy.mock.calls[2][1]?.headers as Headers).get('If-Match')).toBe('"4"');
  });
});
