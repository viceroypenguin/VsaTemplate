import { useId, useRef, useState } from 'react';
import type { FormEvent } from 'react';
import { AlertDialog } from '@/components/ui/alert-dialog';
import { Badge } from '@/components/ui/badge';
import { Breadcrumb, BreadcrumbItem, BreadcrumbLink, BreadcrumbList, BreadcrumbPage, BreadcrumbSeparator } from '@/components/ui/breadcrumb';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { Label } from '@/components/ui/label';
import { NavigationMenu, NavigationMenuItem, NavigationMenuLink, NavigationMenuList } from '@/components/ui/navigation-menu';
import { Switch } from '@/components/ui/switch';
import { errorMessage, request } from './api';
import { AccessPanel } from './access/AccessPanel';
import { Alert, ConfirmAction, Empty, Field, Icon, Loading, PageHeading, Permissions, Retry, Status } from './components';
import { useResource } from './hooks';
import { canOpenOrganizationWorkspace, organizationWorkspaceHref } from './organizationWorkspace';
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
    <PageHeading eyebrow={tenant ? 'Tenant administration' : 'Organization details'} title={scope.name} action={<Status active={scope.isActive} />} />
    {!scope.isActive && <Alert>This {tenant ? 'tenant' : 'organization'} is inactive. Administrators can reactivate it in Settings.</Alert>}
    <NavigationMenu className="scope-navigation" viewport={false} aria-label={`${tenant ? 'Tenant' : 'Organization'} sections`}>
      <NavigationMenuList className="tabs">
        {tabs.map((item) => <NavigationMenuItem key={item.id}>
          <NavigationMenuLink href={`#/${scope.kind}/${scope.id}/${item.id}`} active={selected === item.id}>{item.name}</NavigationMenuLink>
        </NavigationMenuItem>)}
      </NavigationMenuList>
    </NavigationMenu>
    {selected === 'settings' && admin ? <Settings scope={scope} onChanged={onChanged} /> : selected === 'organizations' && manageOrganizations ? <Organizations scope={scope} onChanged={onChanged} /> : (selected === 'members' || selected === 'roles') && manageMembers ? scope.isActive ? <AccessPanel key={selected} scope={scope} view={selected} onChanged={onChanged} /> : <Card className="panel"><Empty title="Workspace is inactive">Reactivate this workspace in Settings before changing access.</Empty></Card> : <>
      <div className="overview-grid">
        <Card className="panel overview-details">
          <div className="section-heading"><h2>Details</h2><Icon name={tenant ? 'building' : 'teams'} /></div>
          <dl className="detail-list">
            <div><dt>Name</dt><dd>{scope.name}</dd></div>
            <div><dt>Status</dt><dd><Status active={scope.isActive} /></dd></div>
            {tenantName && <div><dt>Tenant</dt><dd>{tenantName}</dd></div>}
          </dl>
        </Card>
        <Card className="panel overview-details">
          <div className="section-heading"><h2>Your access</h2><Icon name="shield" /></div>
          <p className="muted">Your permissions apply to this {tenant ? 'tenant' : 'organization'}.</p>
          <Permissions permissions={scope.permissions} />
          {manageMembers && scope.isActive && <Button asChild variant="outline" className="mt-[22px] self-start">
            <a href={`#/${scope.kind}/${scope.id}/members`}>Manage members <Icon name="arrow" size={17} /></a>
          </Button>}
        </Card>
      </div>
      {manageOrganizations && scope.isActive && <Organizations scope={scope} onChanged={onChanged} />}
    </>}
  </>;
}

export function OrganizationCard({ organization }: { organization: Organization }) {
  const accessible = organization.permissions.length > 0;
  const workspace = canOpenOrganizationWorkspace(organization);
  const detailsHref = `#/organizations/${organization.organizationId}`;
  return <Card className="entity-card" role="article">
    <div className="entity-card-top"><span className="entity-icon"><Icon name="teams" size={23} /></span><Status active={organization.isActive} /></div>
    <h2>{accessible ? <a href={detailsHref}>{organization.name}</a> : organization.name}</h2>
    {accessible && <Permissions permissions={organization.permissions} />}
    <div className="entity-card-footer">{accessible
      ? <a href={workspace ? organizationWorkspaceHref(organization.organizationId) : detailsHref} className="card-link">{workspace ? 'Open workspace' : 'Organization details'} <Icon name="arrow" size={17} /></a>
      : <span className="muted">Organization membership required to open</span>}
    </div>
  </Card>;
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

  return <section className="organization-collection"><div className="collection-heading"><div><h2>Organizations <Badge variant="outline">{resource.data?.organizations.length ?? 0}</Badge></h2><p>Bring your teams together inside {scope.name}.</p></div><Button disabled={!scope.isActive} onClick={() => setShowCreate(!showCreate)} aria-expanded={showCreate}><Icon name="plus" size={17} />Create organization</Button></div>
    {showCreate && <Card className="panel form-panel"><h3>Create an organization</h3>{error && <Alert>{error}</Alert>}<form className="inline-form" onSubmit={(event) => { void create(event); }}><Field label="Organization name" placeholder="e.g. Product team" value={name} onChange={(event) => setName(event.target.value)} required maxLength={200} autoFocus disabled={busy} /><div className="form-actions"><Button variant="outline" type="button" onClick={() => setShowCreate(false)} disabled={busy}>Cancel</Button><Button disabled={busy || !name.trim()}>{busy ? 'Creating…' : 'Create organization'}</Button></div></form></Card>}
    {!scope.isActive ? <Card className="panel"><Empty title="Tenant is inactive">Reactivate this tenant in Settings to manage its organizations.</Empty></Card> : resource.loading ? <Loading label="Loading organizations…" /> : resource.error ? <Retry error={resource.error} retry={resource.refresh} /> : resource.data?.organizations.length ? <div className="entity-grid">{resource.data.organizations.map((organization) => <OrganizationCard organization={organization} key={organization.organizationId} />)}</div> : <Card className="panel"><Empty icon="teams" title="No organizations yet">Create an organization to give it its own workspace, members, and permissions.</Empty></Card>}
  </section>;
}

function Settings({ scope, onChanged }: { scope: Scope; onChanged: () => void }) {
  const activeId = useId();
  const saveButton = useRef<HTMLButtonElement>(null);
  const [name, setName] = useState(scope.name);
  const [active, setActive] = useState(scope.isActive);
  const [confirming, setConfirming] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [saved, setSaved] = useState(false);
  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (scope.isActive && !active) setConfirming(true);
    else void save();
  }

  async function save() {
    setBusy(true); setError(null); setSaved(false);
    try {
      await request(`/api/${scope.kind}/${scope.id}`, { method: 'PUT', body: { name: name.trim(), isActive: active } });
      setSaved(true); onChanged();
    } catch (error) { setError(errorMessage(error)); }
    finally { setBusy(false); }
  }
  return <Card className="panel form-panel settings-panel">
    <div className="section-heading"><div><h2>Workspace settings</h2><p>Update the name and availability of this workspace.</p></div><Icon name="settings" /></div>
    {error && <Alert>{error}</Alert>}
    {saved && <Alert success>Workspace settings saved.</Alert>}
    <form onSubmit={submit}>
      <Field label={scope.kind === 'tenants' ? 'Tenant name' : 'Organization name'} value={name} onChange={(event) => setName(event.target.value)} maxLength={200} required disabled={busy} />
      <div className="status-setting">
        <div>
          <Label htmlFor={activeId} className="field-label">Active workspace</Label>
          <p id={`${activeId}-description`}>Members can access an active workspace. Administrators can reactivate it at any time.</p>
        </div>
        <Switch id={activeId} aria-describedby={`${activeId}-description`} checked={active} onCheckedChange={setActive} disabled={busy} />
      </div>
      <Button ref={saveButton} disabled={busy || !name.trim() || (name.trim() === scope.name && active === scope.isActive)}>{busy ? 'Saving…' : 'Save changes'}</Button>
    </form>
    <AlertDialog open={confirming} onOpenChange={setConfirming}>
      <ConfirmAction
        title={`Deactivate ${scope.name}?`}
        description="Members will lose access until an administrator reactivates it."
        action={scope.kind === 'tenants' ? 'Deactivate tenant' : 'Deactivate organization'}
        onConfirm={() => { void save(); }}
        onCloseAutoFocus={(event) => {
          event.preventDefault();
          saveButton.current?.focus();
        }}
      />
    </AlertDialog>
  </Card>;
}
