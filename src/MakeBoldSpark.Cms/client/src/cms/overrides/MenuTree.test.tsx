import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import type { Menu } from '../../api/types';
import { AuthProvider } from '../../auth/AuthContext';
import { MenuTree } from './MenuTree';

const parentMenu: Menu = {
  id: 1,
  displayOrder: 1,
  title: 'Top Level',
  description: '',
  keyWords: '',
  controller: 'Home',
  action: 'Index',
  argument: null,
  icon: '',
  url: '/',
  pageContent: '',
  domainId: 1,
  parentId: null,
  createdDate: '2026-01-01T00:00:00Z',
  updatedDate: '2026-01-01T00:00:00Z',
  createdID: 1,
  updatedID: null,
};

const childMenu: Menu = {
  ...parentMenu,
  id: 2,
  title: 'Nested Item',
  parentId: 1,
};

function jsonResponse(body: unknown) {
  return Promise.resolve({
    ok: true,
    status: 200,
    json: () => Promise.resolve(body),
    text: () => Promise.resolve(JSON.stringify(body)),
  } as Response);
}

describe('MenuTree', () => {
  beforeEach(() => {
    sessionStorage.setItem(
      'makeboldspark.cms.session',
      JSON.stringify({ token: 'test-token', displayName: 'Tester', expiresAt: '2099-01-01T00:00:00Z' }),
    );
  });

  afterEach(() => {
    vi.restoreAllMocks();
    sessionStorage.clear();
  });

  it('renders menu items as a parent/child tree, not a flat list (FR-003)', async () => {
    vi.spyOn(globalThis, 'fetch').mockImplementation(() => jsonResponse([parentMenu, childMenu]));

    render(
      <AuthProvider>
        <MenuTree domainId={1} />
      </AuthProvider>,
    );

    await waitFor(() => expect(screen.getByText('Top Level')).toBeInTheDocument());

    const parentItem = screen.getByText('Top Level').closest('li');
    const childItem = screen.getByText('Nested Item').closest('li');

    expect(parentItem).not.toBeNull();
    expect(childItem).not.toBeNull();
    // The nested item's <li> is indented further than the top-level item's, confirming
    // hierarchical rendering rather than a flat sibling list.
    expect(childItem!.style.marginLeft).not.toBe(parentItem!.style.marginLeft);
  });

  it('hides the selected site ID and provides a top-level parent option when editing', async () => {
    const user = userEvent.setup();
    vi.spyOn(globalThis, 'fetch').mockImplementation(() => jsonResponse([parentMenu, childMenu]));

    render(
      <AuthProvider>
        <MenuTree domainId={1} />
      </AuthProvider>,
    );

    await waitFor(() => expect(screen.getByText('Nested Item')).toBeInTheDocument());
    await user.click(screen.getByText('Nested Item'));

    expect(screen.queryByLabelText('Site Id')).not.toBeInTheDocument();
    expect(screen.getByLabelText('Parent Page')).toHaveValue('1');
    expect(screen.getByRole('option', { name: 'Top level' })).toBeInTheDocument();
    expect(screen.getByRole('option', { name: 'Top Level' })).toBeInTheDocument();
    expect(screen.queryByRole('option', { name: 'Nested Item' })).not.toBeInTheDocument();
  });
});
