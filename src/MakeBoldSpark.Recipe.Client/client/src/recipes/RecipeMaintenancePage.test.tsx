import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { AuthProvider } from '../auth/AuthContext';
import { RecipeLayout } from './RecipeLayout';
import { RecipeMaintenancePage } from './RecipeMaintenancePage';

const recipe = {
  id: 3,
  name: 'Pancakes',
  description: 'Breakfast',
  authorName: 'Tester',
  ingredients: 'Flour',
  instructions: 'Mix',
  servings: 2,
  isApproved: false,
  domainId: 1,
  recipeCategoryId: 9,
  recipeCategoryName: 'Breakfast',
  version: 4,
  updatedDate: '2026-06-26T00:00:00Z',
};

const category = {
  id: 9,
  name: 'Breakfast',
  description: '',
  displayOrder: 1,
  isActive: true,
  domainId: 1,
  version: 1,
};

function jsonResponse(body: unknown, status = 200) {
  return Promise.resolve({
    ok: status >= 200 && status < 300,
    status,
    json: () => Promise.resolve(body),
    text: () => Promise.resolve(JSON.stringify(body)),
  } as Response);
}

function renderPage() {
  sessionStorage.setItem(
    'mbs.recipe.session',
    JSON.stringify({ token: 'test-token', displayName: 'Tester', expiresAt: '2099-01-01T00:00:00Z' }),
  );

  return render(
    <AuthProvider>
      <RecipeMaintenancePage />
    </AuthProvider>,
  );
}

function renderPageWithLayout() {
  sessionStorage.setItem(
    'mbs.recipe.session',
    JSON.stringify({ token: 'test-token', displayName: 'Tester', expiresAt: '2099-01-01T00:00:00Z' }),
  );

  return render(
    <AuthProvider>
      <MemoryRouter>
        <RecipeLayout>
          <RecipeMaintenancePage />
        </RecipeLayout>
      </MemoryRouter>
    </AuthProvider>,
  );
}

function mockRecipeFetch() {
  return vi.spyOn(globalThis, 'fetch').mockImplementation((input, init) => {
    const url = String(input);
    const method = init?.method ?? 'GET';
    if (url.endsWith('/domains')) return jsonResponse([{ id: 1, name: 'Recipes' }]);
    if (url.includes('/categories') && method === 'GET') return jsonResponse([category]);
    if (method === 'PUT') return jsonResponse({ ...recipe, name: 'Blueberry pancakes', version: 5 });
    if (method === 'DELETE') return jsonResponse({}, 204);
    return jsonResponse({ items: [recipe], page: 1, pageSize: 50, totalCount: 1 });
  });
}

describe('RecipeMaintenancePage', () => {
  beforeEach(() => {
    sessionStorage.clear();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('loads domains and lists recipes after domain selection', async () => {
    mockRecipeFetch();
    renderPage();

    await waitFor(() => expect(screen.getByRole('option', { name: 'Recipes' })).toBeInTheDocument());
    fireEvent.change(screen.getByLabelText('Domain'), { target: { value: '1' } });

    expect(await screen.findByText('Pancakes')).toBeInTheDocument();
    expect(screen.getByText('Draft')).toBeInTheDocument();
  });

  it('validates required editor fields before saving', async () => {
    mockRecipeFetch();
    renderPage();

    await waitFor(() => expect(screen.getByRole('option', { name: 'Recipes' })).toBeInTheDocument());
    fireEvent.change(screen.getByLabelText('Domain'), { target: { value: '1' } });
    await screen.findByText('Pancakes');

    fireEvent.click(screen.getByRole('button', { name: /new recipe/i }));
    fireEvent.click(screen.getByRole('button', { name: /^save$/i }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Recipe name is required.');
  });

  it('saves an edited recipe and keeps the If-Match header', async () => {
    const fetchSpy = mockRecipeFetch();
    renderPage();

    await waitFor(() => expect(screen.getByRole('option', { name: 'Recipes' })).toBeInTheDocument());
    fireEvent.change(screen.getByLabelText('Domain'), { target: { value: '1' } });
    fireEvent.click(await screen.findByRole('button', { name: 'Pancakes' }));
    fireEvent.change(screen.getByLabelText('Name'), { target: { value: 'Blueberry pancakes' } });
    fireEvent.click(screen.getByRole('button', { name: /^save$/i }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Recipe saved.');
    const updateCall = fetchSpy.mock.calls.find(([, init]) => init?.method === 'PUT');
    expect((updateCall?.[1]?.headers as Headers).get('If-Match')).toBe('"4"');
  });

  it('requires confirmation before deleting a recipe', async () => {
    const fetchSpy = mockRecipeFetch();
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    renderPage();

    await waitFor(() => expect(screen.getByRole('option', { name: 'Recipes' })).toBeInTheDocument());
    fireEvent.change(screen.getByLabelText('Domain'), { target: { value: '1' } });
    await screen.findByText('Pancakes');
    fireEvent.click(screen.getByRole('button', { name: /^delete$/i }));

    await waitFor(() => expect(window.confirm).toHaveBeenCalledWith('Delete Pancakes?'));
    expect(fetchSpy.mock.calls.some(([, init]) => init?.method === 'DELETE')).toBe(true);
  });

  it('prompts for reauthentication after a 401 save without discarding the draft', async () => {
    vi.spyOn(globalThis, 'fetch').mockImplementation((input, init) => {
      const url = String(input);
      const method = init?.method ?? 'GET';
      if (url.endsWith('/domains')) return jsonResponse([{ id: 1, name: 'Recipes' }]);
      if (url.includes('/categories') && method === 'GET') return jsonResponse([category]);
      if (method === 'PUT') return jsonResponse({}, 401);
      return jsonResponse({ items: [recipe], page: 1, pageSize: 50, totalCount: 1 });
    });
    renderPageWithLayout();

    await waitFor(() => expect(screen.getByRole('option', { name: 'Recipes' })).toBeInTheDocument());
    fireEvent.change(screen.getByLabelText('Domain'), { target: { value: '1' } });
    fireEvent.click(await screen.findByRole('button', { name: 'Pancakes' }));
    fireEvent.change(screen.getByLabelText('Name'), { target: { value: 'Unsaved pancakes' } });
    fireEvent.click(screen.getByRole('button', { name: /^save$/i }));

    expect(await screen.findByRole('region', { name: /reauthentication required/i })).toBeInTheDocument();
    expect(screen.getByDisplayValue('Unsaved pancakes')).toBeInTheDocument();
  });

  it('keeps draft values visible when a stale save returns 412', async () => {
    vi.spyOn(globalThis, 'fetch').mockImplementation((input, init) => {
      const url = String(input);
      const method = init?.method ?? 'GET';
      if (url.endsWith('/domains')) return jsonResponse([{ id: 1, name: 'Recipes' }]);
      if (url.includes('/categories') && method === 'GET') return jsonResponse([category]);
      if (method === 'PUT') return jsonResponse({}, 412);
      return jsonResponse({ items: [recipe], page: 1, pageSize: 50, totalCount: 1 });
    });
    renderPage();

    await waitFor(() => expect(screen.getByRole('option', { name: 'Recipes' })).toBeInTheDocument());
    fireEvent.change(screen.getByLabelText('Domain'), { target: { value: '1' } });
    fireEvent.click(await screen.findByRole('button', { name: 'Pancakes' }));
    fireEvent.change(screen.getByLabelText('Name'), { target: { value: 'Unsaved stale pancakes' } });
    fireEvent.click(screen.getByRole('button', { name: /^save$/i }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Recipe changed elsewhere.');
    expect(screen.getByDisplayValue('Unsaved stale pancakes')).toBeInTheDocument();
  });
});
