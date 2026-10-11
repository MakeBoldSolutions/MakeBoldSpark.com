import { afterEach, expect, it, vi } from 'vitest';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { AuthProvider } from '../auth/AuthContext';
import { EntityCrudPage } from './EntityCrudPage';
import { familyMemoryConfig } from './entityConfigs';

afterEach(() => { vi.restoreAllMocks(); sessionStorage.clear(); });

it('uses authenticated management and preserves plain text through create, publish, reorder, unpublish and delete', async () => {
  sessionStorage.setItem('makeboldspark.cms.session', JSON.stringify({ token: 'fictional-test-token', displayName: 'Fictional Editor', expiresAt: '2099-01-01T00:00:00Z' }));
  const requests: Array<{ url: string; options: RequestInit }> = [];
  const fetch = vi.spyOn(globalThis, 'fetch').mockImplementation(async (url, options) => {
    requests.push({ url: String(url), options: options ?? {} });
    const record = options?.body ? JSON.parse(String(options.body)) : null;
    return { ok: true, status: options?.method === 'DELETE' ? 204 : 200, json: async () => record ? { ...record, id: 1 } : [] } as Response;
  });
  render(<AuthProvider><EntityCrudPage config={familyMemoryConfig} /></AuthProvider>);
  const user = userEvent.setup();
  await user.click(await screen.findByRole('button', { name: 'New Family Memory' }));
  expect(requests[0].url).toBe('/api/admin/makeboldspark/family-memories');
  const body = 'Fictional Zoë 🦊\n\nA [**Fictional Name**](https://example.com) — <b>literal</b>.';
  await user.type(screen.getByLabelText('Name (up to 200 characters)'), 'Fictional Contributor');
  await user.type(screen.getByLabelText(/Relationship to Jared/), 'Fictional friend');
  const textarea = screen.getByLabelText(/Memory \(up to/);
  expect(textarea.tagName).toBe('TEXTAREA');
  expect(textarea).toHaveAttribute('maxlength', '100000');
  fireEvent.change(textarea, { target: { value: body } });
  expect(screen.getByLabelText('Published')).not.toBeChecked();
  await user.click(screen.getByRole('button', { name: 'Save' }));
  await screen.findByText('Fictional Contributor');
  expect(JSON.parse(String(requests.at(-1)!.options.body))).toMatchObject({ body, personPronouns: '', isPublished: false });
  await user.click(screen.getByText('Fictional Contributor'));
  await user.click(screen.getByLabelText('Published'));
  fireEvent.change(screen.getByLabelText(/Editorial Order/), { target: { value: '20' } });
  await user.click(screen.getByRole('button', { name: 'Save' }));
  await screen.findByText('Fictional Contributor');
  expect(JSON.parse(String(requests.at(-1)!.options.body))).toMatchObject({ body, isPublished: true, sortOrder: 20 });
  await user.click(screen.getByText('Fictional Contributor'));
  await user.click(screen.getByLabelText('Published'));
  await user.click(screen.getByRole('button', { name: 'Save' }));
  await screen.findByText('Fictional Contributor');
  expect(JSON.parse(String(requests.at(-1)!.options.body)).isPublished).toBe(false);
  await user.click(screen.getByText('Fictional Contributor'));
  const confirm = vi.spyOn(window, 'confirm').mockReturnValue(false);
  await user.click(screen.getByRole('button', { name: 'Delete Family Memory' }));
  expect(requests.at(-1)!.options.method).toBe('PUT');
  confirm.mockReturnValue(true);
  await user.click(screen.getByRole('button', { name: 'Delete Family Memory' }));
  await waitFor(() => expect(screen.queryByText('Fictional Contributor')).not.toBeInTheDocument());
  expect(requests.at(-1)!.options.method).toBe('DELETE');
  for (const request of requests) expect(request.options.headers).toMatchObject({ Authorization: 'Bearer fictional-test-token' });
  expect(fetch).toHaveBeenCalled();
});

it('keeps editorial order visible after a saved reorder', async () => {
  sessionStorage.setItem('makeboldspark.cms.session', JSON.stringify({ token: 'fictional-test-token', displayName: 'Fictional Editor', expiresAt: '2099-01-01T00:00:00Z' }));
  const first = { ...familyMemoryConfig.defaultValues, id: 1, personName: 'Fictional First', sortOrder: 10 };
  const second = { ...first, id: 2, personName: 'Fictional Second', sortOrder: 20 };
  vi.spyOn(globalThis, 'fetch').mockImplementation(async (_, options) => ({ ok: true, status: 200,
    json: async () => options?.body ? JSON.parse(String(options.body)) : [first, second],
  } as Response));
  render(<AuthProvider><EntityCrudPage config={familyMemoryConfig} /></AuthProvider>);
  const user = userEvent.setup();
  await user.click(await screen.findByText('Fictional Second'));
  fireEvent.change(screen.getByLabelText(/Relationship to Jared/), { target: { value: 'Fictional friend' } });
  fireEvent.change(screen.getByLabelText(/Memory \(up to/), { target: { value: 'Fictional text' } });
  fireEvent.change(screen.getByLabelText(/Editorial Order/), { target: { value: '0' } });
  await user.click(screen.getByRole('button', { name: 'Save' }));
  await screen.findByText('Fictional Second');
  expect(screen.getAllByRole('row')[1]).toHaveTextContent('Fictional Second');
});
