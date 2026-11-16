import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { App, OrganizationCard } from './App';
import { AccessPanel } from './access/AccessPanel';
import { ScopePage } from './ScopePage';
import type { Scope } from './types';

const adminScope: Scope = { kind: 'tenants', id: 1, name: 'Acme', isActive: true, permissions: ['Admin'] };
const json = (value: unknown, status = 200) => new Response(JSON.stringify(value), { status, headers: { 'Content-Type': 'application/json' } });

describe('workspace flows', () => {
  it('shows a public sign-in page without fetching private workspace data', async () => {
    const fetchMock = vi.fn().mockResolvedValue(json({ isAuthenticated: false, user: null, requestToken: null }));
    vi.stubGlobal('fetch', fetchMock);
    render(<App />);
    expect(await screen.findByRole('button', { name: 'Log in to your workspace' })).toBeInTheDocument();
    expect(screen.queryByRole('textbox', { name: 'Email address' })).not.toBeInTheDocument();
    expect(fetchMock).toHaveBeenCalledTimes(1);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/session');
  });

  it('creates a tenant and requires acknowledgment before dismissing its recovery keys', async () => {
    const user = userEvent.setup();
    let created = false;
    const fetchMock = vi.fn(async (input: RequestInfo | URL, options?: RequestInit) => {
      const path = String(input);
      if (path === '/api/session') return json({ isAuthenticated: true, user: { userId: 1, name: 'Alex', emailAddress: 'alex@example.com' }, requestToken: 'csrf-token' });
      if (path === '/api/tenants/create' && options?.method === 'POST') { created = true; return json({ tenantId: 1, recoveryKeys: ['first-recovery-key', 'second-recovery-key'] }); }
      if (path === '/api/tenants') return json({ tenants: created ? [{ tenantId: 1, name: 'Acme', isActive: true, permissions: ['Admin'] }] : [] });
      if (path.endsWith('/organizations')) return json({ organizations: [] });
      throw new Error(`Unexpected request ${path}`);
    });
    vi.stubGlobal('fetch', fetchMock);
    render(<App />);
    await user.click(await screen.findByRole('button', { name: 'Create tenant' }));
    const name = screen.getByRole('textbox', { name: 'Tenant name' });
    await user.type(name, '  Acme  ');
    await user.click(within(name.closest('form')!).getByRole('button', { name: 'Create tenant' }));
    expect(await screen.findByRole('heading', { name: 'Save your recovery keys' })).toBeInTheDocument();
    expect(screen.getByText('first-recovery-key')).toBeInTheDocument();
    const continueButton = screen.getByRole('button', { name: 'Continue to workspace' });
    expect(continueButton).toBeDisabled();
    await user.click(screen.getByRole('checkbox', { name: 'I have saved these recovery keys securely.' }));
    expect(continueButton).toBeEnabled();
    await user.click(continueButton);
    expect(screen.queryByRole('heading', { name: 'Save your recovery keys' })).not.toBeInTheDocument();
    const creation = fetchMock.mock.calls.find(([path]) => path === '/api/tenants/create');
    expect(creation?.[1]?.body).toBe('{"name":"Acme"}');
    expect(new Headers(creation?.[1]?.headers).get('X-CSRF-TOKEN')).toBe('csrf-token');
  });

  it('creates an organization inside the selected tenant', async () => {
    const user = userEvent.setup();
    const onChanged = vi.fn();
    let created = false;
    const fetchMock = vi.fn(async (_input: RequestInfo | URL, options?: RequestInit) => {
      if (options?.method === 'PUT') { created = true; return json({ organizationId: 2 }); }
      return json({ organizations: created ? [{ organizationId: 2, tenantId: 1, name: 'Design team', isActive: true, permissions: ['Admin'] }] : [] });
    });
    vi.stubGlobal('fetch', fetchMock);
    render(<ScopePage scope={adminScope} tab="organizations" onChanged={onChanged} />);
    await user.click(await screen.findByRole('button', { name: 'Create organization' }));
    const name = screen.getByRole('textbox', { name: 'Organization name' });
    await user.type(name, 'Design team');
    await user.click(within(name.closest('form')!).getByRole('button', { name: 'Create organization' }));
    expect(await screen.findByRole('link', { name: 'Design team' })).toHaveAttribute('href', '#/organizations/2');
    expect(fetchMock).toHaveBeenCalledWith('/api/tenants/1/organizations', expect.objectContaining({ method: 'PUT', body: '{"name":"Design team"}' }));
    expect(onChanged).toHaveBeenCalledOnce();
  });

  it('allows role edits within the manager permissions and disables administrator escalation', async () => {
    const user = userEvent.setup();
    const onChanged = vi.fn();
    const fetchMock = vi.fn(async (input: RequestInfo | URL, options?: RequestInit) => {
      if (options?.method === 'PUT') return new Response(null, { status: 204 });
      if (String(input).endsWith('/roles')) return json({ roles: [{ roleId: 1, name: 'Owner', permissions: ['Admin'] }, { roleId: 2, name: 'People manager', permissions: ['UserManagement'] }] });
      return json({ users: [] });
    });
    vi.stubGlobal('fetch', fetchMock);
    render(<AccessPanel scope={{ ...adminScope, permissions: ['UserManagement'] }} view="roles" onChanged={onChanged} />);
    expect(await screen.findByRole('button', { name: 'Edit role Owner' })).toBeDisabled();
    await user.click(screen.getByRole('button', { name: 'Edit role People manager' }));
    expect(screen.getByRole('checkbox', { name: /Administrator/ })).toBeDisabled();
    const name = screen.getByRole('textbox', { name: 'Role name' });
    await user.clear(name); await user.type(name, 'Member support');
    await user.click(screen.getByRole('button', { name: 'Save role' }));
    await waitFor(() => expect(onChanged).toHaveBeenCalledOnce());
    expect(fetchMock).toHaveBeenCalledWith('/api/tenants/1/roles/2', expect.objectContaining({ method: 'PUT', body: '{"name":"Member support","permissions":["UserManagement"]}' }));
  });

  it('keeps the member form open on errors and adds an organization member by email', async () => {
    const user = userEvent.setup();
    const onChanged = vi.fn();
    const fetchMock = vi.fn(async (input: RequestInfo | URL, options?: RequestInit) => {
      if (options?.method === 'PUT') {
        return String(options.body).includes('inactive@example.com') ? json({ detail: 'This account is inactive.' }, 409) : json({ userId: 2 });
      }
      if (String(input).endsWith('/roles')) return json({ roles: [{ roleId: 3, name: 'Member', permissions: ['None'] }] });
      return json({ users: [{ userId: 1, name: 'Alex', emailAddress: 'alex@example.com', roles: [{ roleId: 3, name: 'Member' }] }] });
    });
    vi.stubGlobal('fetch', fetchMock);
    render(<AccessPanel scope={{ ...adminScope, kind: 'organizations', id: 4 }} view="members" onChanged={onChanged} />);
    await user.click(await screen.findByRole('button', { name: 'Add member' }));
    expect(screen.getByText(/access this organization when they sign in/)).toBeInTheDocument();
    const email = screen.getByRole('textbox', { name: 'Email address' });
    await user.type(email, 'inactive@example.com');
    const submit = within(email.closest('form')!).getByRole('button', { name: 'Add member' });
    await user.click(submit);
    expect(await screen.findByRole('alert')).toHaveTextContent('This account is inactive.');
    expect(email).toHaveValue('inactive@example.com');
    await user.clear(email); await user.type(email, 'teammate@example.com');
    await user.click(submit);
    await waitFor(() => expect(onChanged).toHaveBeenCalledOnce());
    expect(fetchMock).toHaveBeenCalledWith('/api/organizations/4/users', expect.objectContaining({ method: 'PUT', body: '{"emailAddress":"teammate@example.com","roleId":3}' }));
  });

  it('lets read-only members open their organization without exposing management controls', () => {
    const fetchMock = vi.fn(); vi.stubGlobal('fetch', fetchMock);
    const organization = { organizationId: 2, tenantId: 1, name: 'Design team', isActive: true, permissions: ['None'] as const };
    render(<><OrganizationCard organization={{ ...organization, permissions: [...organization.permissions] }} /><ScopePage scope={{ kind: 'organizations', id: 2, name: organization.name, isActive: true, permissions: ['None'] }} tab="overview" onChanged={vi.fn()} /></>);
    expect(screen.getByRole('link', { name: 'Open organization' })).toHaveAttribute('href', '#/organizations/2');
    const navigation = screen.getByRole('navigation', { name: 'Organization sections' });
    expect(within(navigation).queryByRole('link', { name: 'Members' })).not.toBeInTheDocument();
    expect(within(navigation).queryByRole('link', { name: 'Roles' })).not.toBeInTheDocument();
    expect(within(navigation).queryByRole('link', { name: 'Settings' })).not.toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
    fireEvent.click(screen.getByRole('link', { name: 'Open organization' }));
  });
});
