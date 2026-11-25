import { useEffect, useState } from 'react';
import type { ReactNode } from 'react';
import { Card } from '@/components/ui/card';
import { getSession, request } from './api';
import { ApiKeys } from './ApiKeys';
import { Brand, Empty, Icon, Loading, Retry } from './components';
import { useResource, useRoute } from './hooks';
import { OrganizationDirectory } from './OrganizationDirectory';
import { organizationWorkspaceEntries, organizationWorkspaceHref, readLastOrganization, rememberOrganization } from './organizationWorkspace';
import { TenantList } from './TenantList';
import { ScopePage } from './ScopePage';
import { PublicHome } from './PublicHome';
import { ThemeControl, useTheme } from './ThemeControl';
import { WorkspaceHeader } from './WorkspaceHeader';
import type { Organization, Session, Tenant } from './types';

export function App() {
  const theme = useTheme();
  const session = useResource(getSession, []);
  if (session.loading) return <div className="initial-loading"><Brand /><Loading label="Opening your workspace…" /></div>;
  if (session.error || !session.data) return <main className="initial-loading"><Brand /><h1>Unable to connect</h1><Retry error={session.error ?? 'No session was returned.'} retry={session.refresh} /></main>;
  const themeControl = <ThemeControl {...theme} />;
  if (!session.data.isAuthenticated) return <PublicHome onLoggedIn={session.refresh} themeControl={themeControl} />;
  return <Workspace key={session.data.user?.userId} session={session.data} themeControl={themeControl} />;
}

function Workspace({ session, themeControl }: { session: Session; themeControl: ReactNode }) {
  const route = useRoute();
  const tenants = useResource((signal) => request<{ tenants: Tenant[] }>('/api/tenants', { signal }), []);
  const organizations = useResource((signal) => request<{ organizations: Organization[] }>('/api/organizations', { signal }), []);
  const userId = session.user?.userId;
  const [lastOrganization, setLastOrganization] = useState(() => readLastOrganization(userId));
  const tenantRoute = /^\/tenants\/(\d+)(?:\/(\w+))?$/.exec(route);
  const organizationRoute = /^\/organizations\/(\d+)(?:\/(\w+))?$/.exec(route);
  const refresh = () => { tenants.refresh(); organizations.refresh(); };
  const tenantItems = tenants.data?.tenants ?? [];
  const organizationItems = organizations.data?.organizations ?? [];
  const tenant = tenantItems.find((item) => item.tenantId === Number(tenantRoute?.[1]));
  const entries = organizationWorkspaceEntries(organizationItems, tenantItems);
  const current = entries.find(({ organization }) => organization.organizationId === Number(organizationRoute?.[1]));
  const organization = current?.organization;
  const resume = entries.find(({ organization }) => String(organization.organizationId) === lastOrganization)
    ?? (entries.length === 1 ? entries[0] : undefined);
  const resumeHref = resume && organizationWorkspaceHref(resume.organization.organizationId);
  const isOrganizationWorkspace = organizationRoute?.[2] === 'workspace';
  const organizationId = organization?.organizationId;

  useEffect(() => {
    if (route === '/' && resumeHref) window.location.replace(resumeHref);
  }, [route, resumeHref]);

  useEffect(() => {
    if (!isOrganizationWorkspace || organizationId === undefined || userId === undefined) return;
    rememberOrganization(userId, organizationId);
    setLastOrganization(String(organizationId));
  }, [isOrganizationWorkspace, organizationId, userId]);

  let content;
  if (route === '/') {
    if (organizations.loading || resumeHref) content = <Loading label="Opening your workspace…" />;
    else content = <>
      <h1 className="workspace-title">Your Workspace</h1>
      {organizations.error ? <Retry error={organizations.error} retry={organizations.refresh} /> : <>
        {entries.length > 0 && <p className="workspace-intro">Choose an organization to open its workspace.</p>}
        <OrganizationDirectory entries={entries} />
      </>}
    </>;
  } else if (route === '/tenants') {
    content = <TenantList resource={tenants} />;
  } else if (route === '/api-keys') {
    content = <ApiKeys />;
  } else if (route === '/organizations') {
    content = <>
      <h1 className="workspace-title">Organizations</h1>
      {organizations.loading ? <Loading label="Loading organizations…" /> : organizations.error ? <Retry error={organizations.error} retry={organizations.refresh} /> : <OrganizationDirectory entries={entries} />}
    </>;
  } else if (tenantRoute || organizationRoute) {
    const resource = tenantRoute ? tenants : organizations;
    const entity = tenantRoute ? tenant : organization;
    if (resource.loading) content = <Loading />;
    else if (resource.error) content = <Retry error={resource.error} retry={resource.refresh} />;
    else if (!entity) content = <Card className="panel"><Empty title="Workspace unavailable">You may no longer have access to this workspace. <a href="#/">Return to your workspace.</a></Empty></Card>;
    else if (organization && isOrganizationWorkspace) content = <section key={organization.organizationId} aria-labelledby="workspace-title">
      <h1 id="workspace-title" className="workspace-title">Your Workspace</h1>
      <Card className="panel workspace-empty">
        <span className="empty-icon"><Icon name="grid" size={26} /></span>
        <p>Future features will appear here.</p>
      </Card>
    </section>;
    else content = <ScopePage key={`${tenantRoute ? 'tenants' : 'organizations'}-${tenantRoute?.[1] ?? organizationRoute?.[1]}`} scope={{ kind: tenantRoute ? 'tenants' : 'organizations', id: tenantRoute ? Number(tenantRoute[1]) : Number(organizationRoute?.[1]), ...entity }} tab={(tenantRoute ?? organizationRoute)?.[2] ?? 'overview'} onChanged={refresh} tenantName={organization ? tenantItems.find((tenant) => tenant.tenantId === organization.tenantId)?.name : undefined} />;
  } else {
    content = <Card className="panel"><Empty title="Page not found">This page is unavailable. <a href="#/">Return to your workspace.</a></Empty></Card>;
  }

  return <div className="app-shell">
    <WorkspaceHeader session={session} current={current} available={entries} route={route} themeControl={themeControl}
      homeHref={organizationId === undefined ? resumeHref : organizationWorkspaceHref(organizationId)} />
    <main id="main-content" className="main-content">
      {content}
    </main>
  </div>;
}
