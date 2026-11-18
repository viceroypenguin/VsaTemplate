import { useState } from 'react';
import type { ReactNode } from 'react';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { NavigationMenu, NavigationMenuItem, NavigationMenuLink, NavigationMenuList } from '@/components/ui/navigation-menu';
import { getSession, request } from './api';
import { AccountAvatar, Brand, Empty, Icon, Loading, PageHeading, Permissions, Retry, Status } from './components';
import { useResource, useRoute } from './hooks';
import { TenantList } from './TenantList';
import { ScopePage } from './ScopePage';
import { RecoveryKeys } from './RecoveryKeys';
import { PublicHome } from './PublicHome';
import { ThemeControl, useTheme } from './ThemeControl';
import type { Organization, Session, Tenant } from './types';

export function App() {
  const theme = useTheme();
  const session = useResource(getSession, []);
  if (session.loading) return <div className="initial-loading"><Brand /><Loading label="Opening your workspace…" /></div>;
  if (session.error || !session.data) return <main className="initial-loading"><Brand /><h1>Unable to connect</h1><Retry error={session.error ?? 'No session was returned.'} retry={session.refresh} /></main>;
  const themeControl = <ThemeControl {...theme} />;
  if (!session.data.isAuthenticated) return <PublicHome onLoggedIn={session.refresh} themeControl={themeControl} />;
  return <Workspace session={session.data} themeControl={themeControl} />;
}

function Workspace({ session, themeControl }: { session: Session; themeControl: ReactNode }) {
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
      {organizations.loading ? <Loading /> : organizations.error ? <Retry error={organizations.error} retry={organizations.refresh} /> : organizationItems.length ? <div className="entity-grid">{organizationItems.map((item) => <OrganizationCard key={item.organizationId} organization={item} tenantName={tenantItems.find((tenant) => tenant.tenantId === item.tenantId)?.name} />)}</div> : <Card className="panel"><Empty icon="teams" title="No organizations yet">Create an organization inside a tenant, or ask a workspace administrator to add you.</Empty></Card>}
    </>;
  } else if (tenantRoute || organizationRoute) {
    const resource = tenantRoute ? tenants : organizations;
    const entity = tenantRoute ? tenant : organization;
    if (resource.loading) content = <Loading />;
    else if (resource.error) content = <Retry error={resource.error} retry={resource.refresh} />;
    else if (!entity) content = <Card className="panel"><Empty title="Workspace unavailable">You may no longer have access to this workspace. <a href="#/tenants">Return to your tenants.</a></Empty></Card>;
    else content = <ScopePage key={`${tenantRoute ? 'tenants' : 'organizations'}-${tenantRoute?.[1] ?? organizationRoute?.[1]}`} scope={{ kind: tenantRoute ? 'tenants' : 'organizations', id: tenantRoute ? Number(tenantRoute[1]) : Number(organizationRoute?.[1]), ...entity }} tab={(tenantRoute ?? organizationRoute)?.[2] ?? 'overview'} onChanged={refresh} tenantName={organization ? tenantItems.find((tenant) => tenant.tenantId === organization.tenantId)?.name : undefined} />;
  } else {
    content = <Card className="panel"><Empty title="Page not found">This page is unavailable. <a href="#/tenants">Go to your tenants.</a></Empty></Card>;
  }

  return <div className="app-shell">
    <aside className="sidebar">
      <Brand />
      <div className="nav-caption">YOUR WORKSPACE</div>
      <NavigationMenu className="sidebar-navigation" orientation="vertical" viewport={false} aria-label="Main navigation">
        <NavigationMenuList className="primary-nav">
          <NavigationMenuItem>
            <NavigationMenuLink href="#/tenants" active={/^\/tenants(?:\/|$)/.test(route) || route === '/'}>
              <Icon name="building" />Tenants<span className="nav-count">{tenantItems.length}</span>
            </NavigationMenuLink>
          </NavigationMenuItem>
          <NavigationMenuItem>
            <NavigationMenuLink href="#/organizations" active={/^\/organizations(?:\/|$)/.test(route)}>
              <Icon name="teams" />Organizations<span className="nav-count">{organizationItems.length}</span>
            </NavigationMenuLink>
          </NavigationMenuItem>
        </NavigationMenuList>
      </NavigationMenu>
      <div className="sidebar-note"><Icon name="shield" /><p>A clear place for people,<br />teams, and access.</p></div>
      {themeControl}
      <div className="sidebar-account"><AccountAvatar name={userName} /><div><strong>{userName}</strong><span>{session.user?.emailAddress}</span></div></div>
    </aside>
    <div className="workspace-body">
      <header className="topbar"><span className="topbar-label"><span className="live-dot" /> Workspace console</span><div className="topbar-actions"><span className="account-name">{userName}</span><form method="post" action="/Logout"><input type="hidden" name="__RequestVerificationToken" value={session.requestToken ?? ''} /><Button variant="ghost" className="text-action" type="submit"><Icon name="logout" size={17} />Sign out</Button></form></div></header>
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
  return <Card className="entity-card" role="article">
    <div className="entity-card-top"><span className="entity-icon"><Icon name="teams" size={23} /></span><Status active={organization.isActive} /></div>
    <p className="card-eyebrow">{tenantName ?? `Tenant ${organization.tenantId}`}</p>
    <h2>{accessible ? <a href={`#/organizations/${organization.organizationId}`}>{organization.name}</a> : organization.name}</h2>
    <Permissions permissions={organization.permissions} />
    <div className="entity-card-footer">{accessible ? <a href={`#/organizations/${organization.organizationId}`} className="card-link">Open organization <Icon name="arrow" size={17} /></a> : <span className="muted">Organization membership required to open</span>}</div>
  </Card>;
}
