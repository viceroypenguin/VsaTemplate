import { useState } from 'react';
import type { FormEvent } from 'react';
import { AlertDialog, AlertDialogCancel, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle, AlertDialogTrigger } from '@/components/ui/alert-dialog';
import { Badge } from '@/components/ui/badge';
import { Breadcrumb, BreadcrumbItem, BreadcrumbLink, BreadcrumbList, BreadcrumbPage, BreadcrumbSeparator } from '@/components/ui/breadcrumb';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { NavigationMenu, NavigationMenuItem, NavigationMenuLink, NavigationMenuList } from '@/components/ui/navigation-menu';
import { errorMessage, request } from './api';
import { AccessPanel } from './access/AccessPanel';
import { Alert, Empty, Field, Icon, Loading, PageHeading, Permissions, Retry } from './components';
import { navigate, useResource } from './hooks';
import { organizationWorkspaceHref } from './organizationWorkspace';
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
    <Breadcrumb className="breadcrumbs">
      <BreadcrumbList>
        <BreadcrumbItem><BreadcrumbLink href={tenant ? '#/tenants' : '#/organizations'}>{tenant ? 'Tenants' : 'Organizations'}</BreadcrumbLink></BreadcrumbItem>
        <BreadcrumbSeparator />
        <BreadcrumbItem className="min-w-0"><BreadcrumbPage className="wrap-anywhere">{scope.name}</BreadcrumbPage></BreadcrumbItem>
      </BreadcrumbList>
    </Breadcrumb>
    <PageHeading eyebrow={tenant ? 'Tenant administration' : 'Organization details'} title={scope.name} />
    <NavigationMenu className="scope-navigation" viewport={false} aria-label={`${tenant ? 'Tenant' : 'Organization'} sections`}>
      <NavigationMenuList className="tabs">
        {tabs.map((item) => <NavigationMenuItem key={item.id}>
          <NavigationMenuLink href={`#/${scope.kind}/${scope.id}/${item.id}`} active={selected === item.id}>{item.name}</NavigationMenuLink>
        </NavigationMenuItem>)}
      </NavigationMenuList>
    </NavigationMenu>
    {selected === 'settings' && admin ? <Settings scope={scope} onChanged={onChanged} /> : selected === 'organizations' && manageOrganizations ? <Organizations scope={scope} onChanged={onChanged} /> : (selected === 'members' || selected === 'roles') && manageMembers ? <AccessPanel key={selected} scope={scope} view={selected} onChanged={onChanged} /> : <>
      <div className="overview-grid">
        <Card className="panel overview-details">
          <div className="section-heading"><h2>Details</h2><Icon name={tenant ? 'building' : 'teams'} /></div>
          <dl className="detail-list">
            <div><dt>Name</dt><dd>{scope.name}</dd></div>
            {tenantName && <div><dt>Tenant</dt><dd>{tenantName}</dd></div>}
          </dl>
        </Card>
        <Card className="panel overview-details">
          <div className="section-heading"><h2>Your access</h2><Icon name="shield" /></div>
          <p className="muted">Your permissions apply to this {tenant ? 'tenant' : 'organization'}.</p>
          <Permissions permissions={scope.permissions} />
          {manageMembers && <Button asChild variant="outline" className="mt-[22px] self-start">
            <a href={`#/${scope.kind}/${scope.id}/members`}>Manage members <Icon name="arrow" size={17} /></a>
          </Button>}
        </Card>
      </div>
      {manageOrganizations && <Organizations scope={scope} onChanged={onChanged} />}
    </>}
  </>;
}

export function OrganizationCard({ organization }: { organization: Organization }) {
  const accessible = organization.permissions.length > 0;
  const detailsHref = `#/organizations/${organization.organizationId}`;
  return <Card className="entity-card" role="article">
    <div className="entity-card-top"><span className="entity-icon"><Icon name="teams" size={23} /></span></div>
    <h2>{accessible ? <a href={detailsHref}>{organization.name}</a> : organization.name}</h2>
    {accessible && <Permissions permissions={organization.permissions} />}
    <div className="entity-card-footer">{accessible
      ? <a href={organizationWorkspaceHref(organization.organizationId)} className="card-link">Open workspace <Icon name="arrow" size={17} /></a>
      : <span className="muted">Organization membership required to open</span>}
    </div>
  </Card>;
}

function Organizations({ scope, onChanged }: { scope: Scope; onChanged: () => void }) {
  const resource = useResource((signal) => request<{ organizations: Organization[] }>(`/api/tenants/${scope.id}/organizations`, { signal }), [scope.id]);
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

  return <section className="organization-collection"><div className="collection-heading"><div><h2>Organizations <Badge variant="outline">{resource.data?.organizations.length ?? 0}</Badge></h2><p>Bring your teams together inside {scope.name}.</p></div><Button onClick={() => setShowCreate(!showCreate)} aria-expanded={showCreate}><Icon name="plus" size={17} />Create organization</Button></div>
    {showCreate && <Card className="panel form-panel"><h3>Create an organization</h3>{error && <Alert>{error}</Alert>}<form className="inline-form" onSubmit={(event) => { void create(event); }}><Field label="Organization name" placeholder="e.g. Product team" value={name} onChange={(event) => setName(event.target.value)} required maxLength={200} autoFocus disabled={busy} /><div className="form-actions"><Button variant="outline" type="button" onClick={() => setShowCreate(false)} disabled={busy}>Cancel</Button><Button disabled={busy || !name.trim()}>{busy ? 'Creating…' : 'Create organization'}</Button></div></form></Card>}
    {resource.loading ? <Loading label="Loading organizations…" /> : resource.error ? <Retry error={resource.error} retry={resource.refresh} /> : resource.data?.organizations.length ? <div className="entity-grid">{resource.data.organizations.map((organization) => <OrganizationCard organization={organization} key={organization.organizationId} />)}</div> : <Card className="panel"><Empty icon="teams" title="No organizations yet">Create an organization to give it its own workspace, members, and permissions.</Empty></Card>}
  </section>;
}

function Settings({ scope, onChanged }: { scope: Scope; onChanged: () => void }) {
  const [name, setName] = useState(scope.name);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [saved, setSaved] = useState(false);

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    setBusy(true); setError(null); setSaved(false);
    try {
      await request(`/api/${scope.kind}/${scope.id}`, { method: 'PUT', body: { name: name.trim(), isActive: true } });
      setSaved(true);
      onChanged();
    } catch (error) { setError(errorMessage(error)); }
    finally { setBusy(false); }
  }

  return <>
    <Card className="panel form-panel settings-panel">
      <div className="section-heading"><h2>Workspace settings</h2><Icon name="settings" /></div>
      {error && <Alert>{error}</Alert>}
      {saved && <Alert success>Workspace settings saved.</Alert>}
      <form className="grid gap-4" onSubmit={(event) => { void save(event); }}>
        <Field label={scope.kind === 'tenants' ? 'Tenant name' : 'Organization name'} value={name} onChange={(event) => setName(event.target.value)} maxLength={200} required disabled={busy} />
        <Button className="justify-self-start" disabled={busy || !name.trim() || name.trim() === scope.name}>{busy ? 'Saving…' : 'Save changes'}</Button>
      </form>
    </Card>
    <Deactivation scope={scope} onChanged={onChanged} disabled={busy} />
  </>;
}

function Deactivation({ scope, onChanged, disabled }: { scope: Scope; onChanged: () => void; disabled: boolean }) {
  const tenant = scope.kind === 'tenants';
  const action = tenant ? 'Deactivate tenant' : 'Deactivate organization';
  const [open, setOpen] = useState(false);
  const [confirmation, setConfirmation] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const canDeactivate = confirmation === 'DELETE' && !busy;

  function changeOpen(nextOpen: boolean) {
    if (busy) return;

    setOpen(nextOpen);
    setConfirmation('');
    setError(null);
  }

  async function deactivate(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!canDeactivate) return;

    setBusy(true);
    setError(null);
    try {
      await request(`/api/${scope.kind}/${scope.id}`, { method: 'PUT', body: { name: scope.name, isActive: false } });
      setOpen(false);
      onChanged();
      navigate(`/${scope.kind}`);
    } catch (error) { setError(errorMessage(error)); }
    finally { setBusy(false); }
  }

  return <Card className="panel form-panel settings-panel">
    <div className="section-heading"><div><h2>Deactivation</h2><p>{tenant
      ? 'Permanently removes access to this tenant and its organizations.'
      : 'Permanently removes access to this organization.'}</p></div></div>
    <AlertDialog open={open} onOpenChange={changeOpen}>
      <AlertDialogTrigger asChild>
        <Button type="button" variant="destructive" className="confirmation-action self-start" disabled={disabled}>{action}</Button>
      </AlertDialogTrigger>
      <AlertDialogContent className="bg-card">
        <AlertDialogHeader>
          <AlertDialogTitle className="m-0 wrap-anywhere">Deactivate {scope.name}?</AlertDialogTitle>
          <AlertDialogDescription className="m-0">{tenant
            ? 'Deactivation is permanent. This tenant and all of its organizations will be hidden, and all members will lose access.'
            : 'Deactivation is permanent. This organization will be hidden, and all members will lose access.'}</AlertDialogDescription>
        </AlertDialogHeader>
        <form className="grid gap-4" onSubmit={(event) => { void deactivate(event); }}>
          {error && <Alert>{error}</Alert>}
          <Field label="Type DELETE to confirm" value={confirmation} onChange={(event) => setConfirmation(event.target.value)} autoComplete="off" autoCapitalize="off" spellCheck={false} disabled={busy} />
          <AlertDialogFooter>
            <AlertDialogCancel disabled={busy}>Cancel</AlertDialogCancel>
            <Button type="submit" variant="destructive" className="confirmation-action" disabled={!canDeactivate}>{busy ? 'Deactivating…' : action}</Button>
          </AlertDialogFooter>
        </form>
      </AlertDialogContent>
    </AlertDialog>
  </Card>;
}
