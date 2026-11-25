import { useState } from 'react';
import type { FormEvent } from 'react';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { errorMessage, request } from './api';
import { Alert, Empty, Field, Icon, Loading, PageHeading, Permissions, Retry } from './components';
import { navigate } from './hooks';
import type { useResource } from './hooks';
import type { Tenant } from './types';

export function TenantList({ resource }: { resource: ReturnType<typeof useResource<{ tenants: Tenant[] }>> }) {
  const [showCreate, setShowCreate] = useState(false);
  const [name, setName] = useState('');
  const [search, setSearch] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const tenants = resource.data?.tenants ?? [];
  const filtered = tenants.filter((tenant) => tenant.name.toLowerCase().includes(search.toLowerCase()));

  async function create(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setBusy(true); setError(null);
    try {
      const result = await request<{ tenantId: number }>('/api/tenants/create', { method: 'POST', body: { name: name.trim() } });
      resource.refresh();
      setName(''); setShowCreate(false);
      navigate(`/tenants/${result.tenantId}`);
    } catch (error) { setError(errorMessage(error)); }
    finally { setBusy(false); }
  }

  return <>
    <PageHeading eyebrow="Administration" title="Tenants" action={<Button onClick={() => setShowCreate(!showCreate)} aria-expanded={showCreate}><Icon name="plus" size={18} />Create tenant</Button>}>Manage organizations, members, and permissions at the tenant level.</PageHeading>
    {showCreate && <Card className="panel form-panel"><div className="section-heading"><div><h2>Create a tenant</h2><p>You will become its first administrator.</p></div></div>{error && <Alert>{error}</Alert>}<form className="inline-form" onSubmit={(event) => { void create(event); }}><Field label="Tenant name" placeholder="e.g. Acme Company" value={name} onChange={(event) => setName(event.target.value)} required maxLength={200} autoFocus disabled={busy} /><div className="form-actions"><Button variant="outline" type="button" onClick={() => setShowCreate(false)} disabled={busy}>Cancel</Button><Button disabled={busy || !name.trim()}>{busy ? 'Creating…' : 'Create tenant'}</Button></div></form></Card>}
    <div className="stats-row" aria-label="Tenant summary"><div><span>Total tenants</span><strong>{resource.loading ? '—' : tenants.length}</strong><Icon name="building" /></div><div><span>You administer</span><strong>{resource.loading ? '—' : tenants.filter((tenant) => tenant.permissions.includes('Admin')).length}</strong><Icon name="shield" /></div></div>
    <div className="collection-heading"><div><h2>Your tenants <Badge variant="outline">{tenants.length}</Badge></h2></div><div className="collection-filters"><div className="search-field"><Icon name="search" size={17} /><Input type="search" value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Search tenants" aria-label="Search tenants" /></div></div></div>
    {resource.loading ? <Loading label="Loading tenants…" /> : resource.error ? <Retry error={resource.error} retry={resource.refresh} /> : filtered.length ? <div className="entity-grid">{filtered.map((tenant) => <Card className="entity-card" role="article" key={tenant.tenantId}><div className="entity-card-top"><span className="entity-icon"><Icon name="building" size={23} /></span></div><p className="card-eyebrow">TENANT</p><h2><a href={`#/tenants/${tenant.tenantId}`}>{tenant.name}</a></h2><Permissions permissions={tenant.permissions} /><div className="entity-card-footer"><a href={`#/tenants/${tenant.tenantId}`} className="card-link">Manage tenant <Icon name="arrow" size={17} /></a></div></Card>)}</div> : <Card className="panel"><Empty title={tenants.length ? 'No matching tenants' : 'No tenants available'}>{tenants.length ? 'Try a different name.' : 'Create a tenant to manage organizations and members.'}</Empty></Card>}
  </>;
}
