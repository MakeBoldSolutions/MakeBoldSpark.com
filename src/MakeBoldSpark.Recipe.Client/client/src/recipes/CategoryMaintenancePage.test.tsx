import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { AuthProvider } from '../auth/AuthContext';
import { CategoryMaintenancePage } from './CategoryMaintenancePage';
import { RecipeLayout } from './RecipeLayout';

const category = {
  id: 9,
  name: 'Breakfast',
  description: 'Morning recipes',
  displayOrder: 1,
  isActive: true,
  domainId: 1,
  version: 3,
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
      <CategoryMaintenancePage />
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
          <CategoryMaintenancePage />
        </RecipeLayout>
      </MemoryRouter>
    </AuthProvider>,
  );
}

function mockCategoryFetch(deleteStatus = 204) {
  return vi.spyOn(globalThis, 'fetch').mockImplementation((input, init) => {
    const url = String(input);
    const method = init?.method ?? 'GET';
    if (url.endsWith('/domains')) return jsonResponse([{ id: 1, name: 'Recipes' }]);
    if (method === 'POST') return jsonResponse({ ...category, id: 10, name: 'Dinner' }, 201);
    if (method === 'PUT') return jsonResponse({ ...category, name: 'Brunch', version: 4 });
    if (method === 'DELETE') return jsonResponse({}, deleteStatus);
    return jsonResponse([category]);
  });
}

describe('CategoryMaintenancePage', () => {
  beforeEach(() => {
    sessionStorage.clear();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('creates a category in the active domain', async () => {
    const fetchSpy = mockCategoryFetch();
    renderPage();

    await waitFor(() => expect(screen.getByRole('option', { name: 'Recipes' })).toBeInTheDocument());
    fireEvent.change(screen.getByLabelText('Domain'), { target: { value: '1' } });
    fireEvent.click(screen.getByRole('button', { name: /new category/i }));
    fireEvent.change(screen.getByLabelText('Category name'), { target: { value: 'Dinner' } });
    fireEvent.click(screen.getByRole('button', { name: /^save$/i }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Category saved.');
    expect(fetchSpy.mock.calls.some(([, init]) => init?.method === 'POST')).toBe(true);
  });

  it('edits a category with an If-Match header', async () => {
    const fetchSpy = mockCategoryFetch();
    renderPage();

    await waitFor(() => expect(screen.getByRole('option', { name: 'Recipes' })).toBeInTheDocument());
    fireEvent.change(screen.getByLabelText('Domain'), { target: { value: '1' } });
    fireEvent.click(await screen.findByRole('button', { name: 'Breakfast' }));
    fireEvent.change(screen.getByLabelText('Category name'), { target: { value: 'Brunch' } });
    fireEvent.click(screen.getByRole('button', { name: /^save$/i }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Category saved.');
    const updateCall = fetchSpy.mock.calls.find(([, init]) => init?.method === 'PUT');
    expect((updateCall?.[1]?.headers as Headers).get('If-Match')).toBe('"3"');
  });

  it('shows an in-use message when delete is blocked', async () => {
    mockCategoryFetch(409);
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    renderPage();

    await waitFor(() => expect(screen.getByRole('option', { name: 'Recipes' })).toBeInTheDocument());
    fireEvent.change(screen.getByLabelText('Domain'), { target: { value: '1' } });
    await screen.findByText('Breakfast');
    fireEvent.click(screen.getByRole('button', { name: /^delete$/i }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Category is in use.');
  });

  it('validates required category name before saving', async () => {
    mockCategoryFetch();
    renderPage();

    await waitFor(() => expect(screen.getByRole('option', { name: 'Recipes' })).toBeInTheDocument());
    fireEvent.change(screen.getByLabelText('Domain'), { target: { value: '1' } });
    fireEvent.click(screen.getByRole('button', { name: /new category/i }));
    fireEvent.click(screen.getByRole('button', { name: /^save$/i }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Category name is required.');
  });

  it('prompts for reauthentication after a 401 save without discarding category edits', async () => {
    vi.spyOn(globalThis, 'fetch').mockImplementation((input, init) => {
      const url = String(input);
      const method = init?.method ?? 'GET';
      if (url.endsWith('/domains')) return jsonResponse([{ id: 1, name: 'Recipes' }]);
      if (method === 'PUT') return jsonResponse({}, 401);
      return jsonResponse([category]);
    });
    renderPageWithLayout();

    await waitFor(() => expect(screen.getByRole('option', { name: 'Recipes' })).toBeInTheDocument());
    fireEvent.change(screen.getByLabelText('Domain'), { target: { value: '1' } });
    fireEvent.click(await screen.findByRole('button', { name: 'Breakfast' }));
    fireEvent.change(screen.getByLabelText('Category name'), { target: { value: 'Unsaved brunch' } });
    fireEvent.click(screen.getByRole('button', { name: /^save$/i }));

    expect(await screen.findByRole('region', { name: /reauthentication required/i })).toBeInTheDocument();
    expect(screen.getByDisplayValue('Unsaved brunch')).toBeInTheDocument();
  });

  it('keeps category draft values visible when a stale save returns 412', async () => {
    vi.spyOn(globalThis, 'fetch').mockImplementation((input, init) => {
      const url = String(input);
      const method = init?.method ?? 'GET';
      if (url.endsWith('/domains')) return jsonResponse([{ id: 1, name: 'Recipes' }]);
      if (method === 'PUT') return jsonResponse({}, 412);
      return jsonResponse([category]);
    });
    renderPage();

    await waitFor(() => expect(screen.getByRole('option', { name: 'Recipes' })).toBeInTheDocument());
    fireEvent.change(screen.getByLabelText('Domain'), { target: { value: '1' } });
    fireEvent.click(await screen.findByRole('button', { name: 'Breakfast' }));
    fireEvent.change(screen.getByLabelText('Category name'), { target: { value: 'Unsaved stale brunch' } });
    fireEvent.click(screen.getByRole('button', { name: /^save$/i }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Category changed elsewhere.');
    expect(screen.getByDisplayValue('Unsaved stale brunch')).toBeInTheDocument();
  });
});
