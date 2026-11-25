import { useState } from 'react';
import { Card } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Empty, Icon } from './components';
import { organizationWorkspaceHref } from './organizationWorkspace';
import type { OrganizationWorkspaceEntry } from './organizationWorkspace';

export function OrganizationDirectory({ entries }: { entries: OrganizationWorkspaceEntry[] }) {
  const [search, setSearch] = useState('');
  const query = search.trim().toLowerCase();
  const filtered = entries.filter(({ organization, context }) => (
    organization.name.toLowerCase().includes(query) || context?.toLowerCase().includes(query)
  ));

  if (!entries.length) return <Card className="panel">
    <Empty icon="teams" title="No organizations yet">Ask an organization administrator to invite you, or <a href="#/tenants">manage tenants</a> to set one up.</Empty>
  </Card>;

  return <Card className="panel organization-directory">
    {entries.length > 1 && <div className="search-field organization-search">
      <Icon name="search" size={17} />
      <Input type="search" value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Search organizations" aria-label="Search organizations" />
    </div>}
    {filtered.length ? <ul className="organization-list">
      {filtered.map(({ organization, context }) => <li key={organization.organizationId}>
        <a className="organization-row" href={organizationWorkspaceHref(organization.organizationId)}>
          <span className="entity-icon"><Icon name="teams" size={20} /></span>
          <span className="organization-row-name"><strong>{organization.name}</strong>{context && <> <small>{context}</small></>}</span>
          <Icon name="arrow" size={17} />
        </a>
      </li>)}
    </ul> : <p className="organization-no-results" role="status">No matching organizations.</p>}
  </Card>;
}
