import { useState } from 'react';
import { getSession, request } from './api';
import { Brand, Empty, Icon, Loading, PageHeading, Permissions, Retry, Status, ThemeControl } from './components';
import { useResource, useRoute } from './hooks';
import { TenantList } from './TenantList';
import { ScopePage } from './ScopePage';
import { RecoveryKeys } from './RecoveryKeys';
import { PublicHome } from './PublicHome';
import type { Organization, Session, Tenant } from './types';

export function App() {
  const session = useResource(getSession, []);
  if (session.loading) return <div className="initial-loading"><Brand /><Loading label="Opening your workspace…" /></div>;
  if (session.error || !session.data) return <main className="initial-loading"><Brand /><h1>Unable to connect</h1><Retry error={session.error ?? 'No session was returned.'} retry={session.refresh} /></main>;
  if (!session.data.isAuthenticated) return <PublicHome onLoggedIn={session.refresh} />;
  return <Workspace session={session.data} />;
}

function Workspace({ session }: { session: Session }) {
  const route = useRoute();
  const tenants = useResource((signal) => request<{ tenants: Tenant[] }>('/api/tenants', { signal }), []);
  const organizations = useResource((signal) => request<{ organizations: Organization[] }>('/api/organizations', { signal }), []);
  const [recovery, setRecovery] = useState<{ name: string; keys: string[] } | null>(null);
  const userName = session.user?.name || session.user?.emailAddress || 'Your account';
  const tenantRoute = /^\/tenants\/(\d+)(?:\/(\w+))?$/.exec(route);
  const organizationRoute = /^\/organizations\/(\d+)(?:\/(\w+))?$/.exec(route);
  const tenant = tenants.data?.tenants.find((item) => item.tenantId === Number(tenantRoute?.[1]));
  const organization = organizations.data?.organizations.find((item) => item.organizationId === Number(organizationRoute?.[1]));
  const refresh = () => { tenants.refresh(); organizations.refresh(); };
  const tenantItems = tenants.data?.tenants ?? [];
  const organizationItems = organizations.data?.organizations ?? [];

  let content;
  if (route === '/tenants' || route === '/') {
    content = <TenantList resource={tenants} onCreated={(name, keys) => { setRecovery({ name, keys }); refresh(); }} />;
  } else if (route === '/organizations') {
    content = <>
      <PageHeading eyebrow="Your workspace" title="Organizations">The teams and organizations you belong to, across your tenants.</PageHeading>
      {organizations.loading ? <Loading /> : organizations.error ? <Retry error={organizations.error} retry={organizations.refresh} /> : organizationItems.length ? <div className="entity-grid">{organizationItems.map((item) => <OrganizationCard key={item.organizationId} organization={item} tenantName={tenantItems.find((tenant) => tenant.tenantId === item.tenantId)?.name} />)}</div> : <div className="panel"><Empty icon="teams" title="No organizations yet">Create an organization inside a tenant, or ask a workspace administrator to add you.</Empty></div>}
    </>;
  } else if (tenantRoute || organizationRoute) {
    const resource = tenantRoute ? tenants : organizations;
    const entity = tenantRoute ? tenant : organization;
    if (resource.loading) content = <Loading />;
    else if (resource.error) content = <Retry error={resource.error} retry={resource.refresh} />;
    else if (!entity) content = <div className="panel"><Empty title="Workspace unavailable">You may no longer have access to this workspace. <a href="#/tenants">Return to your tenants.</a></Empty></div>;
    else content = <ScopePage key={`${tenantRoute ? 'tenants' : 'organizations'}-${tenantRoute?.[1] ?? organizationRoute?.[1]}`} scope={{ kind: tenantRoute ? 'tenants' : 'organizations', id: tenantRoute ? Number(tenantRoute[1]) : Number(organizationRoute?.[1]), ...entity }} tab={(tenantRoute ?? organizationRoute)?.[2] ?? 'overview'} onChanged={refresh} tenantName={organization ? tenantItems.find((tenant) => tenant.tenantId === organization.tenantId)?.name : undefined} />;
  } else {
    content = <div className="panel"><Empty title="Page not found">This page is unavailable. <a href="#/tenants">Go to your tenants.</a></Empty></div>;
  }

  return <div className="app-shell">
    <aside className="sidebar">
      <Brand />
      <div className="nav-caption">YOUR WORKSPACE</div>
      <nav className="primary-nav" aria-label="Main navigation">
        <a href="#/tenants" className={route.startsWith('/tenants') || route === '/' ? 'selected' : ''} aria-current={route.startsWith('/tenants') ? 'page' : undefined}><Icon name="building" />Tenants<span className="nav-count">{tenantItems.length}</span></a>
        <a href="#/organizations" className={route.startsWith('/organizations') ? 'selected' : ''} aria-current={route.startsWith('/organizations') ? 'page' : undefined}><Icon name="teams" />Organizations<span className="nav-count">{organizationItems.length}</span></a>
      </nav>
      <div className="sidebar-note"><Icon name="shield" /><p>A clear place for people,<br />teams, and access.</p></div>
      <ThemeControl />
      <div className="sidebar-account"><span className="avatar">{userName.slice(0, 1).toUpperCase()}</span><div><strong>{userName}</strong><span>{session.user?.emailAddress}</span></div></div>
    </aside>
    <div className="workspace-body">
      <header className="topbar"><span className="topbar-label"><span className="live-dot" /> Workspace console</span><div className="topbar-actions"><span className="account-name">{userName}</span><form method="post" action="/Logout"><input type="hidden" name="__RequestVerificationToken" value={session.requestToken ?? ''} /><button className="text-button" type="submit"><Icon name="logout" size={17} />Sign out</button></form></div></header>
      <main id="main-content" className="main-content">
        {recovery && <RecoveryKeys name={recovery.name} keys={recovery.keys} onClose={() => setRecovery(null)} />}
        {content}
      </main>
      <footer className="app-footer"><span>Workspace / VSA Template</span><span>Made for your teams.</span></footer>
    </div>
  </div>;
}

export function OrganizationCard({ organization, tenantName }: { organization: Organization; tenantName?: string | undefined }) {
  const accessible = organization.permissions.length > 0;
  return <article className="entity-card">
    <div className="entity-card-top"><span className="entity-icon"><Icon name="teams" size={23} /></span><Status active={organization.isActive} /></div>
    <p className="card-eyebrow">{tenantName ?? `Tenant ${organization.tenantId}`}</p>
    <h2>{accessible ? <a href={`#/organizations/${organization.organizationId}`}>{organization.name}</a> : organization.name}</h2>
    <Permissions permissions={organization.permissions} />
    <div className="entity-card-footer">{accessible ? <a href={`#/organizations/${organization.organizationId}`} className="card-link">Open organization <Icon name="arrow" size={17} /></a> : <span className="muted">Organization membership required to open</span>}</div>
  </article>;
}
