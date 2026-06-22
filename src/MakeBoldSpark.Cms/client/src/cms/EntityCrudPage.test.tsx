import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import type { BaseEntity, Newsletter } from '../api/types';
import { AuthProvider, useAuth } from '../auth/AuthContext';
import { EntityCrudPage, type EntityConfig } from './EntityCrudPage';

interface MockThing extends BaseEntity {
  name: string;
  refId: number;
}

const mockConfig: EntityConfig<MockThing> = {
  resource: 'mock-things',
  label: 'Mock Thing',
  publicRead: false,
  columns: [{ key: 'name', label: 'Name', type: 'text' }],
  fields: [
    { key: 'name', label: 'Name', type: 'text', required: true },
    { key: 'refId', label: 'Ref Id', type: 'number' },
  ],
  defaultValues: { name: '', refId: 0 },
};

const baseRecord: MockThing = {
  id: 1,
  name: 'First Thing',
  refId: 10,
  createdDate: '2026-01-01T00:00:00Z',
  updatedDate: '2026-06-01T12:00:00Z',
  createdID: 1,
  updatedID: 1,
};

function jsonResponse(body: unknown, status = 200) {
  return Promise.resolve({
    ok: status >= 200 && status < 300,
    status,
    json: () => Promise.resolve(body),
    text: () => Promise.resolve(JSON.stringify(body)),
  } as Response);
}

function ReauthProbe() {
  const { reauthRequired } = useAuth();
  return <span data-testid="reauth-required">{String(reauthRequired)}</span>;
}

function renderPage(listResult: MockThing[] = [baseRecord]) {
  sessionStorage.setItem(
    'makeboldspark.cms.session',
    JSON.stringify({ token: 'test-token', displayName: 'Tester', expiresAt: '2099-01-01T00:00:00Z' }),
  );

  vi.spyOn(globalThis, 'fetch').mockImplementation((input) => {
    const url = String(input);
    if (url.includes('/mock-things') && !url.match(/\/mock-things\/\d+/)) {
      return jsonResponse(listResult);
    }
    return jsonResponse(listResult[0]);
  });

  return render(
    <AuthProvider>
      <ReauthProbe />
      <EntityCrudPage config={mockConfig} />
    </AuthProvider>,
  );
}

const newsletterRecord: Newsletter = {
  id: 1,
  postId: 42,
  success: true,
  createdDate: '2026-06-22T12:00:00Z',
  updatedDate: '2026-06-22T12:00:00Z',
  createdID: 1,
  updatedID: 1,
};

const newsletterConfig: EntityConfig<Newsletter> = {
  resource: 'newsletters',
  label: 'Newsletter',
  publicRead: false,
  allowEdit: false,
  columns: [{ key: 'postId', label: 'Post Id', type: 'number' }],
  fields: [
    { key: 'postId', label: 'Post Id', type: 'number', required: true },
    { key: 'success', label: 'Success', type: 'boolean' },
  ],
  defaultValues: { postId: 0, success: false },
};

describe('EntityCrudPage', () => {
  beforeEach(() => {
    sessionStorage.clear();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('renders the list and supports create', async () => {
    const user = userEvent.setup();
    renderPage([baseRecord]);

    await waitFor(() => expect(screen.getByText('First Thing')).toBeInTheDocument());

    (globalThis.fetch as ReturnType<typeof vi.fn>).mockImplementationOnce(() =>
      jsonResponse({ ...baseRecord, id: 2, name: 'New Thing' }, 201),
    );

    await user.click(screen.getByRole('button', { name: /new mock thing/i }));
    await user.type(screen.getByLabelText('Name'), 'New Thing');
    await user.click(screen.getByRole('button', { name: /save/i }));

    await waitFor(() => expect(screen.getByText('New Thing')).toBeInTheDocument());
  });

  it('renders a specific message when delete is blocked by dependent records (FR-012)', async () => {
    const user = userEvent.setup();
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    const alertSpy = vi.spyOn(window, 'alert').mockImplementation(() => {});
    renderPage([baseRecord]);

    await waitFor(() => expect(screen.getByText('First Thing')).toBeInTheDocument());

    (globalThis.fetch as ReturnType<typeof vi.fn>).mockImplementationOnce(() => jsonResponse({}, 500));

    await user.click(screen.getByRole('button', { name: /delete/i }));

    await waitFor(() =>
      expect(alertSpy).toHaveBeenCalledWith(expect.stringContaining('other records still depend on it')),
    );
  });

  it('renders a distinct message when save fails due to an invalid reference (FR-007)', async () => {
    const user = userEvent.setup();
    renderPage([baseRecord]);

    await waitFor(() => expect(screen.getByText('First Thing')).toBeInTheDocument());
    await user.click(screen.getByRole('button', { name: /edit/i }));

    (globalThis.fetch as ReturnType<typeof vi.fn>).mockImplementationOnce(() => jsonResponse({}, 500));
    await user.click(screen.getByRole('button', { name: /save/i }));

    expect(await screen.findByRole('alert')).toHaveTextContent('references a record that does not exist');
  });

  it('displays the record\'s updatedDate in the edit view', async () => {
    const user = userEvent.setup();
    renderPage([baseRecord]);

    await waitFor(() => expect(screen.getByText('First Thing')).toBeInTheDocument());
    await user.click(screen.getByRole('button', { name: /edit/i }));

    expect(screen.getByText(/last changed/i)).toBeInTheDocument();
  });

  it('preserves unsaved edits and prompts re-auth on a 401 mid-edit', async () => {
    const user = userEvent.setup();
    renderPage([baseRecord]);

    await waitFor(() => expect(screen.getByText('First Thing')).toBeInTheDocument());
    await user.click(screen.getByRole('button', { name: /edit/i }));

    const nameInput = screen.getByLabelText('Name');
    await user.clear(nameInput);
    await user.type(nameInput, 'Edited Unsaved Name');

    (globalThis.fetch as ReturnType<typeof vi.fn>).mockImplementationOnce(() => jsonResponse({}, 401));
    await user.click(screen.getByRole('button', { name: /save/i }));

    await waitFor(() => expect(screen.getByTestId('reauth-required')).toHaveTextContent('true'));
    expect(screen.getByLabelText('Name')).toHaveValue('Edited Unsaved Name');
  });

  it('allows Newsletter create/delete but exposes no edit action (FR-004)', async () => {
    sessionStorage.setItem(
      'makeboldspark.cms.session',
      JSON.stringify({ token: 'test-token', displayName: 'Tester', expiresAt: '2099-01-01T00:00:00Z' }),
    );
    vi.spyOn(globalThis, 'fetch').mockImplementation(() => jsonResponse([newsletterRecord]));

    render(
      <AuthProvider>
        <EntityCrudPage config={newsletterConfig} />
      </AuthProvider>,
    );

    await waitFor(() => expect(screen.getByText('42')).toBeInTheDocument());
    expect(screen.getByRole('button', { name: /new newsletter/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /^delete$/i })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /^edit$/i })).not.toBeInTheDocument();
  });
});
