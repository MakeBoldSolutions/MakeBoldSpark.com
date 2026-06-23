import type { BaseEntity, LoginRequest, LoginResponse } from './types';

// Same-origin in both dev (via the Vite proxy, vite.config.ts) and embedded/production mode.
const API_BASE = '';

export class ApiError extends Error {
  constructor(public status: number, public body: string) {
    super(`API ${status}: ${body}`);
  }
}

export const SESSION_EXPIRED_EVENT = 'mbs-session-expired';

export async function apiFetch<T>(path: string, options: RequestInit = {}): Promise<T> {
  // `authedFetch` supplies Authorization through options.headers. Separate it before spreading
  // options so the authenticated write path cannot overwrite Content-Type and cause a 415.
  const { headers: optionHeaders, ...requestOptions } = options;
  const res = await fetch(`${API_BASE}${path}`, {
    ...requestOptions,
    headers: { 'Content-Type': 'application/json', ...optionHeaders },
  });
  if (!res.ok) throw new ApiError(res.status, await res.text());
  if (res.status === 204) return undefined as T;
  return res.json() as Promise<T>;
}

export function authedFetch<T>(token: string, path: string, options: RequestInit = {}) {
  return apiFetch<T>(path, {
    ...options,
    headers: { Authorization: `Bearer ${token}`, ...options.headers },
  }).catch((err) => {
    // Signals AuthContext to prompt re-authentication in place, without navigating away
    // and discarding whatever the caller (e.g. an open EntityCrudPage edit) hasn't saved.
    if (err instanceof ApiError && err.status === 401) {
      window.dispatchEvent(new CustomEvent(SESSION_EXPIRED_EVENT));
    }
    throw err;
  });
}

export const login = (request: LoginRequest) =>
  apiFetch<LoginResponse>('/api/public/auth/login', {
    method: 'POST',
    body: JSON.stringify(request),
  });

/**
 * Most CMS entities (domains, blogs, authors, posts, categories, menus, keywords, content
 * parts) are readable anonymously under /api/public/makeboldspark/* — only POST/PUT/DELETE
 * exist under /api/admin/makeboldspark/* for them; there is no admin-only GET. Subscribers,
 * Newsletters, and Mail Configuration are the exception: GET also requires Admin, under the
 * same /api/admin/makeboldspark/* path (see makeboldspark-api-guide.md). `publicRead: true`
 * (the default) routes list/get through the public path; pass `false` for the admin-only trio.
 */
export function adminCrud<T extends BaseEntity>(resource: string, options: { publicRead?: boolean } = {}) {
  const publicRead = options.publicRead ?? true;
  const readBase = `/api/${publicRead ? 'public' : 'admin'}/makeboldspark/${resource}`;
  const writeBase = `/api/admin/makeboldspark/${resource}`;

  return {
    list: (token: string, query?: Record<string, string | number>) => {
      const qs = query ? `?${new URLSearchParams(query as Record<string, string>).toString()}` : '';
      return publicRead ? apiFetch<T[]>(`${readBase}${qs}`) : authedFetch<T[]>(token, `${readBase}${qs}`);
    },

    get: (token: string, id: number) =>
      publicRead ? apiFetch<T>(`${readBase}/${id}`) : authedFetch<T>(token, `${readBase}/${id}`),

    create: (token: string, data: Omit<T, keyof BaseEntity>) =>
      authedFetch<T>(token, writeBase, { method: 'POST', body: JSON.stringify(data) }),

    update: (token: string, id: number, data: Partial<T>) =>
      authedFetch<T>(token, `${writeBase}/${id}`, {
        method: 'PUT',
        body: JSON.stringify({ ...data, id }),
      }),

    delete: (token: string, id: number) =>
      authedFetch<void>(token, `${writeBase}/${id}`, { method: 'DELETE' }),
  };
}
