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
    const fetchMock = vi.fn().mockResolvedValue(json({ isAuthenticated: false, isAdmin: false, user: null, requestToken: null }));
    vi.stubGlobal('fetch', fetchMock);
    render(<App />);
    expect(await screen.findByRole('button', { name: 'Log in to your workspace' })).toBeInTheDocument();
    expect(screen.queryByRole('textbox', { name: 'Email address' })).not.toBeInTheDocument();
    expect(fetchMock).toHaveBeenCalledTimes(1);
    expect(fetchMock.mock.calls[0]?.[0]).toBe('/api/session');
  });

  it.each([true, false])('opens the workspace and shows account actions for isAdmin=%s', async (isAdmin) => {
    const user = userEvent.setup();
    vi.stubGlobal('fetch', vi.fn(async (input: RequestInfo | URL) => {
      const path = String(input);
      if (path === '/api/session') return json({ isAuthenticated: true, isAdmin, user: { userId: 1, name: 'Alex', emailAddress: 'alex@example.com' }, requestToken: 'csrf-token' });
      if (path === '/api/tenants') return json({ tenants: [] });
      if (path === '/api/organizations') return json({ organizations: [] });
      throw new Error(`Unexpected request ${path}`);
    }));
    render(<App />);

    expect(await screen.findByRole('heading', { name: 'Your Workspace' })).toBeInTheDocument();
    expect(screen.getByText('Future features will appear here.')).toBeInTheDocument();
    expect(screen.getAllByText('Alex')).toHaveLength(1);
    expect(screen.queryByRole('link', { name: 'Manage tenants' })).not.toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Alex' }));
    expect(await screen.findByRole('link', { name: 'API keys' })).toHaveAttribute('href', '#/api-keys');
    const dashboard = screen.queryByRole('link', { name: /Immediate\.Jobs dashboard/ });
    if (isAdmin) {
      expect(dashboard).toHaveAttribute('href', '/jobs');
      expect(dashboard).toHaveAttribute('aria-disabled', 'true');
    } else {
      expect(dashboard).not.toBeInTheDocument();
    }
    const signOut = screen.getByRole('button', { name: 'Sign out' });
    const form = signOut.closest('form')!;
    expect(form).toHaveAttribute('method', 'post');
    expect(form).toHaveAttribute('action', '/Logout');
    expect(new FormData(form).get('__RequestVerificationToken')).toBe('csrf-token');
  });

  it('creates a named API key, masks it in the list, and invalidates it after confirmation', async () => {
    const user = userEvent.setup();
    const secret = 'test-key-released-xyz';
    let issued = false;
    const fetchMock = vi.fn(async (input: RequestInfo | URL, options?: RequestInit) => {
      const path = String(input);
      if (path === '/api/session') return json({ isAuthenticated: true, isAdmin: true, user: { userId: 1, name: 'Alex', emailAddress: 'alex@example.com' }, requestToken: 'csrf-token' });
      if (path === '/api/tenants') return json({ tenants: [] });
      if (path === '/api/organizations') return json({ organizations: [] });
      if (path === '/api/users/apikey/create' && options?.method === 'POST') {
        issued = true;
        return json({ apiKey: secret });
      }
      if (path === '/api/users/apikey/5' && options?.method === 'DELETE') {
        issued = false;
        return new Response(null, { status: 200 });
      }
      if (path === '/api/users/apikey') return json({
        apiKeys: issued ? [{ apiKeyId: 5, note: 'Release automation', permissions: ['Admin'], keySuffix: 'xyz' }] : [],
        canCreateAdminKey: true,
      });
      throw new Error(`Unexpected request ${path}`);
    });
    vi.stubGlobal('fetch', fetchMock);
    render(<App />);

    await user.click(await screen.findByRole('button', { name: 'Alex' }));
    await user.click(await screen.findByRole('link', { name: 'API keys' }));
    await screen.findByRole('heading', { name: 'No active API keys' });
    await user.click(screen.getByRole('button', { name: 'Create API key' }));
    await user.type(screen.getByRole('textbox', { name: 'Purpose note (optional)' }), ' Release automation ');
    await user.click(screen.getByRole('checkbox', { name: 'Grant administrator permissions' }));
    await user.click(screen.getByRole('button', { name: 'Create key' }));

    const disclosure = await screen.findByRole('region', { name: 'New API key' });
    expect(within(disclosure).getByText(secret)).toBeInTheDocument();
    const table = await screen.findByRole('table', { name: 'Issued API keys' });
    expect(within(table).getByText('****xyz')).toBeInTheDocument();
    expect(within(table).getByText('Administrator')).toBeInTheDocument();
    expect(within(table).queryByText(secret)).not.toBeInTheDocument();
    const creation = fetchMock.mock.calls.find(([path]) => path === '/api/users/apikey/create');
    expect(creation?.[1]?.body).toBe('{"note":"Release automation","permissions":["Admin"],"lifetime":"30.00:00:00"}');
    await user.click(within(disclosure).getByRole('button', { name: 'Done' }));
    expect(screen.queryByText(secret)).not.toBeInTheDocument();

    const invalidate = within(table).getByRole('button', { name: 'Invalidate Release automation (****xyz)' });
    await user.click(invalidate);
    await user.click(within(screen.getByRole('alertdialog')).getByRole('button', { name: 'Cancel' }));
    expect(fetchMock.mock.calls.filter(([, options]) => options?.method === 'DELETE')).toHaveLength(0);

    await user.click(invalidate);
    await user.click(within(screen.getByRole('alertdialog')).getByRole('button', { name: 'Invalidate key' }));
    await screen.findByRole('heading', { name: 'No active API keys' });
    expect(screen.getByText('API key invalidated.')).toBeInTheDocument();
    const deletion = fetchMock.mock.calls.find(([, options]) => options?.method === 'DELETE');
    expect(deletion?.[0]).toBe('/api/users/apikey/5');
    expect(new Headers(deletion?.[1]?.headers).get('X-CSRF-TOKEN')).toBe('csrf-token');
  });

  it('creates a tenant and requires acknowledgment before dismissing its recovery keys', async () => {
    const user = userEvent.setup();
    let created = false;
    const fetchMock = vi.fn(async (input: RequestInfo | URL, options?: RequestInit) => {
      const path = String(input);
      if (path === '/api/session') return json({ isAuthenticated: true, isAdmin: false, user: { userId: 1, name: 'Alex', emailAddress: 'alex@example.com' }, requestToken: 'csrf-token' });
      if (path === '/api/tenants/create' && options?.method === 'POST') { created = true; return json({ tenantId: 1, recoveryKeys: ['first-recovery-key', 'second-recovery-key'] }); }
      if (path === '/api/tenants') return json({ tenants: created ? [{ tenantId: 1, name: 'Acme', isActive: true, permissions: ['Admin'] }] : [] });
      if (path.endsWith('/organizations')) return json({ organizations: [] });
      throw new Error(`Unexpected request ${path}`);
    });
    vi.stubGlobal('fetch', fetchMock);
    render(<App />);
    await user.click(await screen.findByRole('button', { name: 'Workspaces' }));
    await user.click(await screen.findByRole('link', { name: 'Manage tenants' }));
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
    expect(await screen.findByRole('button', { name: 'Acme' })).toBeInTheDocument();
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

  it('removes only a delegable member and only after confirmation', async () => {
    const user = userEvent.setup();
    const onChanged = vi.fn();
    const fetchMock = vi.fn(async (input: RequestInfo | URL, options?: RequestInit) => {
      if (options?.method === 'DELETE') return new Response(null, { status: 204 });
      if (String(input).endsWith('/roles')) return json({ roles: [
        { roleId: 1, name: 'Owner', permissions: ['Admin'] },
        { roleId: 2, name: 'Member', permissions: ['None'] },
      ] });
      return json({ users: [
        { userId: 1, name: 'Owner', emailAddress: 'owner@example.com', roles: [{ roleId: 1, name: 'Owner' }] },
        { userId: 2, name: 'Member', emailAddress: 'member@example.com', roles: [{ roleId: 2, name: 'Member' }] },
      ] });
    });
    vi.stubGlobal('fetch', fetchMock);
    render(<AccessPanel scope={{ ...adminScope, permissions: ['UserManagement'] }} view="members" onChanged={onChanged} />);

    expect(await screen.findByRole('button', { name: 'Remove access for owner@example.com' })).toBeDisabled();
    const trigger = screen.getByRole('button', { name: 'Remove access for member@example.com' });
    await user.click(trigger);
    const dialog = screen.getByRole('alertdialog', { name: 'Remove member@example.com from Acme?' });
    expect(within(dialog).getByRole('button', { name: 'Cancel' })).toHaveFocus();
    await user.keyboard('{Escape}');
    await waitFor(() => expect(trigger).toHaveFocus());
    expect(fetchMock.mock.calls.filter(([, options]) => options?.method === 'DELETE')).toHaveLength(0);

    await user.click(trigger);
    await user.click(within(screen.getByRole('alertdialog')).getByRole('button', { name: 'Remove access' }));
    await waitFor(() => expect(onChanged).toHaveBeenCalledOnce());
    expect(fetchMock).toHaveBeenCalledWith('/api/tenants/1/users/2', expect.objectContaining({ method: 'DELETE' }));
  });

  it('confirms deactivation on keyboard form submission and restores focus on cancel', async () => {
    const user = userEvent.setup();
    const onChanged = vi.fn();
    const fetchMock = vi.fn().mockResolvedValue(new Response(null, { status: 204 }));
    vi.stubGlobal('fetch', fetchMock);
    render(<ScopePage scope={adminScope} tab="settings" onChanged={onChanged} />);

    const settingsLink = screen.getByRole('link', { name: 'Settings' });
    expect(settingsLink).toHaveAttribute('aria-current', 'page');
    expect(settingsLink).toHaveAttribute('href', '#/tenants/1/settings');
    await user.click(screen.getByRole('switch', { name: 'Active workspace' }));
    await user.type(screen.getByRole('textbox', { name: 'Tenant name' }), '{Enter}');
    const dialog = screen.getByRole('alertdialog', { name: 'Deactivate Acme?' });
    await user.click(within(dialog).getByRole('button', { name: 'Cancel' }));
    const save = screen.getByRole('button', { name: 'Save changes' });
    await waitFor(() => expect(save).toHaveFocus());
    expect(fetchMock).not.toHaveBeenCalled();

    await user.click(save);
    await user.click(within(screen.getByRole('alertdialog')).getByRole('button', { name: 'Deactivate tenant' }));
    await waitFor(() => expect(onChanged).toHaveBeenCalledOnce());
    expect(fetchMock).toHaveBeenCalledWith('/api/tenants/1', expect.objectContaining({ method: 'PUT', body: '{"name":"Acme","isActive":false}' }));
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
