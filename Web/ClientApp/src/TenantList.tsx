import { useState } from 'react';
import type { FormEvent } from 'react';
import { errorMessage, request } from './api';
import { Alert, Empty, Field, Icon, Loading, PageHeading, Permissions, Retry, Status } from './components';
import { navigate } from './hooks';
import type { useResource } from './hooks';
import type { Tenant } from './types';

export function TenantList({ resource, onCreated }: { resource: ReturnType<typeof useResource<{ tenants: Tenant[] }>>; onCreated: (name: string, keys: string[]) => void }) {
  const [showCreate, setShowCreate] = useState(false);
  const [name, setName] = useState('');
  const [search, setSearch] = useState('');
  const [filter, setFilter] = useState('all');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const tenants = resource.data?.tenants ?? [];
  const filtered = tenants.filter((tenant) => tenant.name.toLowerCase().includes(search.toLowerCase()) && (filter === 'all' || tenant.isActive === (filter === 'active')));

  async function create(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setBusy(true); setError(null);
    try {
      const result = await request<{ tenantId: number; recoveryKeys: string[] }>('/api/tenants/create', { method: 'POST', body: { name: name.trim() } });
      onCreated(name.trim(), result.recoveryKeys);
      setName(''); setShowCreate(false);
      navigate(`/tenants/${result.tenantId}`);
    } catch (error) { setError(errorMessage(error)); }
    finally { setBusy(false); }
  }

  return <>
    <PageHeading eyebrow="Your workspace" title="Tenants" action={<button className="button primary" onClick={() => setShowCreate(!showCreate)} aria-expanded={showCreate}><Icon name="plus" size={18} />Create tenant</button>}>A home for each business. Manage its organizations, people, and permissions.</PageHeading>
    {showCreate && <section className="panel form-panel"><div className="section-heading"><div><h2>Create a tenant</h2><p>You will become its first administrator.</p></div></div>{error && <Alert>{error}</Alert>}<form className="inline-form" onSubmit={(event) => { void create(event); }}><Field label="Tenant name" placeholder="e.g. Acme Company" value={name} onChange={(event) => setName(event.target.value)} required maxLength={200} autoFocus disabled={busy} /><div className="form-actions"><button className="button secondary" type="button" onClick={() => setShowCreate(false)} disabled={busy}>Cancel</button><button className="button primary" disabled={busy || !name.trim()}>{busy ? 'Creating…' : 'Create tenant'}</button></div></form></section>}
    <div className="stats-row" aria-label="Tenant summary"><div><span>Total tenants</span><strong>{resource.loading ? '—' : tenants.length}</strong><Icon name="building" /></div><div><span>Active tenants</span><strong>{resource.loading ? '—' : tenants.filter((tenant) => tenant.isActive).length}</strong><span className="stat-dot" /></div><div><span>You administer</span><strong>{resource.loading ? '—' : tenants.filter((tenant) => tenant.permissions.includes('Admin')).length}</strong><Icon name="shield" /></div></div>
    <div className="collection-heading"><div><h2>Your tenants <span>{tenants.length}</span></h2><p>Select a tenant to manage its workspace.</p></div><div className="collection-filters"><label className="search-field"><Icon name="search" size={17} /><input type="search" value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Search tenants" aria-label="Search tenants" /></label><select aria-label="Filter tenant status" value={filter} onChange={(event) => setFilter(event.target.value)}><option value="all">All statuses</option><option value="active">Active</option><option value="inactive">Inactive</option></select></div></div>
    {resource.loading ? <Loading label="Loading tenants…" /> : resource.error ? <Retry error={resource.error} retry={resource.refresh} /> : filtered.length ? <div className="entity-grid">{filtered.map((tenant) => <article className="entity-card" key={tenant.tenantId}><div className="entity-card-top"><span className="entity-icon"><Icon name="building" size={23} /></span><Status active={tenant.isActive} /></div><p className="card-eyebrow">TENANT</p><h2><a href={`#/tenants/${tenant.tenantId}`}>{tenant.name}</a></h2><Permissions permissions={tenant.permissions} /><div className="entity-card-footer"><a href={`#/tenants/${tenant.tenantId}`} className="card-link">Manage tenant <Icon name="arrow" size={17} /></a></div></article>)}</div> : <div className="panel"><Empty title={tenants.length ? 'No matching tenants' : 'Your workspace starts here'}>{tenants.length ? 'Try a different name or status filter.' : 'Create your first tenant to bring your organizations and people together.'}</Empty></div>}
  </>;
}
