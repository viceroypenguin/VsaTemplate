import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError, request, setRequestToken } from './api';

describe('API requests', () => {
  beforeEach(() => setRequestToken(null));

  it('sends same-origin cookies, JSON, and the current antiforgery token for mutations', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(null, { status: 204 }));
    vi.stubGlobal('fetch', fetchMock);
    setRequestToken('session-token');
    await request('/api/tenants/1', { method: 'PUT', body: { name: 'Acme', isActive: true } });
    const options = fetchMock.mock.calls[0]?.[1] as RequestInit;
    expect(options.credentials).toBe('same-origin');
    expect(options.body).toBe(JSON.stringify({ name: 'Acme', isActive: true }));
    expect(new Headers(options.headers).get('X-CSRF-TOKEN')).toBe('session-token');
    expect(new Headers(options.headers).get('Content-Type')).toBe('application/json');
  });

  it('does not attach a mutation token to reads', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response('{"tenants":[]}'));
    vi.stubGlobal('fetch', fetchMock); setRequestToken('secret-token');
    expect(await request('/api/tenants')).toEqual({ tenants: [] });
    const options = fetchMock.mock.calls[0]?.[1] as RequestInit;
    expect(new Headers(options.headers).has('X-CSRF-TOKEN')).toBe(false);
  });

  it('shows field validation messages returned as problem details', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({ errors: { Name: ['A name is required.'], Permissions: ['Choose a permission.'] } }), { status: 400 })));
    await expect(request('/api/tenants/create', { method: 'POST', body: {} })).rejects.toThrow('A name is required. Choose a permission.');
  });

  it('preserves actionable conflict details and HTTP status', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('{"detail":"The final administrator cannot be removed."}', { status: 409 })));
    await expect(request('/api/tenants/1/users/1', { method: 'DELETE' })).rejects.toMatchObject({ status: 409, message: 'The final administrator cannot be removed.' });
  });

  it('handles expired authentication even when the server body is not JSON', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('Unauthorized', { status: 401 })));
    await expect(request('/api/tenants')).rejects.toBeInstanceOf(ApiError);
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('Unauthorized', { status: 401 })));
    await expect(request('/api/tenants')).rejects.toThrow('Your session has ended. Sign in to continue.');
  });
});
