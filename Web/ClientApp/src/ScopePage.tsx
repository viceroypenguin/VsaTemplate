import { useState } from 'react';
import type { FormEvent } from 'react';
import { errorMessage, request } from './api';
import { AccessPanel } from './access/AccessPanel';
import { Alert, Empty, Field, Icon, Loading, PageHeading, Permissions, Retry, Status } from './components';
import { OrganizationCard } from './App';
import { useResource } from './hooks';
import { hasPermission } from './types';
import type { Organization, Scope } from './types';

export function ScopePage({ scope, tab, tenantName, onChanged }: { scope: Scope; tab: string; tenantName?: string | undefined; onChanged: () => void }) {
  const tenant = scope.kind === 'tenants';
  const manageMembers = hasPermission(scope.permissions, 'UserManagement');
  const manageOrganizations = tenant && hasPermission(scope.permissions, 'OrganizationManagement');
  const admin = hasPermission(scope.permissions, 'Admin');
  const tabs = [
    { id: 'overview', name: 'Overview' },
    ...(manageOrganizations ? [{ id: 'organizations', name: 'Organizations' }] : []),
    ...(manageMembers ? [{ id: 'members', name: 'Members' }, { id: 'roles', name: 'Roles' }] : []),
    ...(admin ? [{ id: 'settings', name: 'Settings' }] : []),
  ];
  const selected = tabs.some((item) => item.id === tab) ? tab : 'overview';

  return <>
    <div className="breadcrumbs"><a href={tenant ? '#/tenants' : '#/organizations'}>{tenant ? 'Tenants' : 'Organizations'}</a><span>/</span><span>{scope.name}</span></div>
    <PageHeading eyebrow={tenant ? 'Tenant workspace' : `${tenantName ?? 'Team'} / Organization`} title={scope.name} action={<Status active={scope.isActive} />}>{tenant ? 'Your organizations, people, and access in one place.' : 'Manage this organization and the people who make it work.'}</PageHeading>
    {!scope.isActive && <Alert>This {tenant ? 'tenant' : 'organization'} is inactive. Administrators can reactivate it in Settings.</Alert>}
    <nav className="tabs" aria-label={`${tenant ? 'Tenant' : 'Organization'} sections`}>{tabs.map((item) => <a key={item.id} href={`#/${scope.kind}/${scope.id}/${item.id}`} aria-current={selected === item.id ? 'page' : undefined} className={selected === item.id ? 'active' : ''}>{item.name}</a>)}</nav>
    {selected === 'settings' && admin ? <Settings scope={scope} onChanged={onChanged} /> : selected === 'organizations' && manageOrganizations ? <Organizations scope={scope} onChanged={onChanged} /> : (selected === 'members' || selected === 'roles') && manageMembers ? scope.isActive ? <AccessPanel key={selected} scope={scope} view={selected} onChanged={onChanged} /> : <div className="panel"><Empty title="Workspace is inactive">Reactivate this workspace in Settings before changing access.</Empty></div> : <>
      <div className="overview-grid"><section className="panel overview-details"><div className="section-heading"><h2>Workspace details</h2><Icon name={tenant ? 'building' : 'teams'} /></div><dl className="detail-list"><div><dt>Name</dt><dd>{scope.name}</dd></div><div><dt>Status</dt><dd><Status active={scope.isActive} /></dd></div><div><dt>Workspace type</dt><dd>{tenant ? 'Tenant' : 'Organization'}</dd></div>{tenantName && <div><dt>Tenant</dt><dd>{tenantName}</dd></div>}</dl></section><section className="panel overview-details"><div className="section-heading"><h2>Your access</h2><Icon name="shield" /></div><p className="muted">Your permissions apply to this {tenant ? 'tenant' : 'organization'}.</p><Permissions permissions={scope.permissions} />{manageMembers && scope.isActive && <a className="button secondary" href={`#/${scope.kind}/${scope.id}/members`}>Manage members <Icon name="arrow" size={17} /></a>}</section></div>
      {manageOrganizations && scope.isActive && <Organizations scope={scope} onChanged={onChanged} />}
    </>}
  </>;
}

function Organizations({ scope, onChanged }: { scope: Scope; onChanged: () => void }) {
  const resource = useResource((signal) => scope.isActive ? request<{ organizations: Organization[] }>(`/api/tenants/${scope.id}/organizations`, { signal }) : Promise.resolve({ organizations: [] }), [scope.id, scope.isActive]);
  const [showCreate, setShowCreate] = useState(false);
  const [name, setName] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function create(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setBusy(true); setError(null);
    try {
      await request(`/api/tenants/${scope.id}/organizations`, { method: 'PUT', body: { name: name.trim() } });
      setName(''); setShowCreate(false); resource.refresh(); onChanged();
    } catch (error) { setError(errorMessage(error)); }
    finally { setBusy(false); }
  }

  return <section className="organization-collection"><div className="collection-heading"><div><h2>Organizations <span>{resource.data?.organizations.length ?? 0}</span></h2><p>Bring your teams together inside {scope.name}.</p></div><button className="button primary" disabled={!scope.isActive} onClick={() => setShowCreate(!showCreate)} aria-expanded={showCreate}><Icon name="plus" size={17} />Create organization</button></div>
    {showCreate && <div className="panel form-panel"><h3>Create an organization</h3>{error && <Alert>{error}</Alert>}<form className="inline-form" onSubmit={(event) => { void create(event); }}><Field label="Organization name" placeholder="e.g. Product team" value={name} onChange={(event) => setName(event.target.value)} required maxLength={200} autoFocus disabled={busy} /><div className="form-actions"><button className="button secondary" type="button" onClick={() => setShowCreate(false)} disabled={busy}>Cancel</button><button className="button primary" disabled={busy || !name.trim()}>{busy ? 'Creating…' : 'Create organization'}</button></div></form></div>}
    {!scope.isActive ? <div className="panel"><Empty title="Tenant is inactive">Reactivate this tenant in Settings to manage its organizations.</Empty></div> : resource.loading ? <Loading label="Loading organizations…" /> : resource.error ? <Retry error={resource.error} retry={resource.refresh} /> : resource.data?.organizations.length ? <div className="entity-grid">{resource.data.organizations.map((organization) => <OrganizationCard organization={organization} tenantName={scope.name} key={organization.organizationId} />)}</div> : <div className="panel"><Empty icon="teams" title="Make room for your first team">Create an organization to manage its own members and permissions.</Empty></div>}
  </section>;
}

function Settings({ scope, onChanged }: { scope: Scope; onChanged: () => void }) {
  const [name, setName] = useState(scope.name);
  const [active, setActive] = useState(scope.isActive);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [saved, setSaved] = useState(false);
  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (scope.isActive && !active && !window.confirm(`Deactivate ${scope.name}? Members will lose access until an administrator reactivates it.`)) return;
    setBusy(true); setError(null); setSaved(false);
    try {
      await request(`/api/${scope.kind}/${scope.id}`, { method: 'PUT', body: { name: name.trim(), isActive: active } });
      setSaved(true); onChanged();
    } catch (error) { setError(errorMessage(error)); }
    finally { setBusy(false); }
  }
  return <section className="panel form-panel settings-panel"><div className="section-heading"><div><h2>Workspace settings</h2><p>Update the name and availability of this workspace.</p></div><Icon name="settings" /></div>{error && <Alert>{error}</Alert>}{saved && <Alert success>Workspace settings saved.</Alert>}<form onSubmit={(event) => { void save(event); }}><Field label={scope.kind === 'tenants' ? 'Tenant name' : 'Organization name'} value={name} onChange={(event) => setName(event.target.value)} maxLength={200} required disabled={busy} /><div className="status-setting"><div><strong>Active workspace</strong><p>Members can access an active workspace. Administrators can reactivate it at any time.</p></div><label className="switch-label"><input type="checkbox" checked={active} onChange={(event) => setActive(event.target.checked)} disabled={busy} aria-label="Active workspace" /><span aria-hidden="true" /></label></div><button className="button primary" disabled={busy || !name.trim() || (name.trim() === scope.name && active === scope.isActive)}>{busy ? 'Saving…' : 'Save changes'}</button></form></section>;
}
