import type { ReactNode } from 'react';
import { Button } from '@/components/ui/button';
import {
  NavigationMenu,
  NavigationMenuContent,
  NavigationMenuItem,
  NavigationMenuLink,
  NavigationMenuList,
  NavigationMenuTrigger,
} from '@/components/ui/navigation-menu';
import { AccountAvatar, Brand, Icon } from './components';
import { organizationWorkspaceHref } from './organizationWorkspace';
import type { OrganizationWorkspaceEntry } from './organizationWorkspace';
import type { Session } from './types';

export function WorkspaceHeader({ session, current, available, route, themeControl, homeHref = '#/' }: {
  session: Session;
  current: OrganizationWorkspaceEntry | undefined;
  available: OrganizationWorkspaceEntry[];
  route: string;
  themeControl: ReactNode;
  homeHref: string | undefined;
}) {
  const userName = session.user?.name || session.user?.emailAddress || 'Your account';
  const organizationName = current?.organization.name ?? 'Select organization';
  const organizationDetails = current && `#/organizations/${current.organization.organizationId}/overview`;

  return <header className="topbar">
    <div className="topbar-inner">
      <Brand href={homeHref} />
      <NavigationMenu className="topbar-navigation organization-navigation" aria-label="Organization" viewport={false}>
        <NavigationMenuList>
          <NavigationMenuItem className="topbar-menu-item">
            <NavigationMenuTrigger className="topbar-trigger organization-trigger" title={organizationName}>
              <Icon name="teams" size={17} />
              <span className="organization-trigger-label">
                <span>{organizationName}</span>
                {current?.context && <> <small>{current.context}</small></>}
              </span>
            </NavigationMenuTrigger>
            <NavigationMenuContent className="topbar-menu-panel organization-menu-panel">
              {available.length > 0 && <ul className="organization-menu-list" aria-label="Available organizations">
                {available.map(({ organization, context }) => {
                  const href = organizationWorkspaceHref(organization.organizationId);
                  const selected = organization.organizationId === current?.organization.organizationId;
                  const active = route === href.slice(1);
                  return <li key={organization.organizationId}>
                    <NavigationMenuLink className="topbar-menu-action" href={href} active={active}
                      aria-current={active ? 'page' : selected ? 'location' : undefined}>
                      <Icon name="teams" size={16} />
                      <span className="organization-menu-name">{organization.name}{context && <> <small>{context}</small></>}</span>
                      {selected && <Icon name="check" size={15} />}
                    </NavigationMenuLink>
                  </li>;
                })}
              </ul>}
              <NavigationMenuLink className="topbar-menu-action" href="#/organizations" active={route === '/organizations'}>
                <Icon name="search" size={16} />Browse organizations
              </NavigationMenuLink>
              {organizationDetails && <NavigationMenuLink className="topbar-menu-action" href={organizationDetails} active={route === organizationDetails.slice(1)}>
                <Icon name="settings" size={16} />Organization details
              </NavigationMenuLink>}
            </NavigationMenuContent>
          </NavigationMenuItem>
        </NavigationMenuList>
      </NavigationMenu>
      <div className="header-actions">
        {themeControl}
        <NavigationMenu className="topbar-navigation account-navigation" aria-label="Account" viewport={false}>
          <NavigationMenuList>
            <NavigationMenuItem className="topbar-menu-item">
              <NavigationMenuTrigger className="topbar-trigger account-trigger" title={userName}>
                <AccountAvatar name={userName} />
                <span className="topbar-menu-label">{userName}</span>
              </NavigationMenuTrigger>
              <NavigationMenuContent className="topbar-menu-panel account-menu-panel">
                {session.isAdmin && <NavigationMenuLink
                  className="topbar-menu-action"
                  href="/jobs"
                  aria-disabled="true"
                  onClick={(event) => event.preventDefault()}
                >
                  <Icon name="grid" size={16} />
                  <span>Immediate.Jobs dashboard <small className="topbar-menu-note">Coming soon</small></span>
                </NavigationMenuLink>}
                <NavigationMenuLink className="topbar-menu-action" href="#/api-keys" active={route === '/api-keys'}>
                  <Icon name="key" size={16} />API keys
                </NavigationMenuLink>
                <NavigationMenuLink className="topbar-menu-action" href="#/tenants" active={route === '/tenants'}>
                  <Icon name="building" size={16} />Manage tenants
                </NavigationMenuLink>
                <form className="topbar-menu-sign-out" method="post" action="/Logout">
                  <input type="hidden" name="__RequestVerificationToken" value={session.requestToken ?? ''} />
                  <Button className="topbar-menu-action" variant="ghost" type="submit">
                    <Icon name="logout" size={16} />Sign out
                  </Button>
                </form>
              </NavigationMenuContent>
            </NavigationMenuItem>
          </NavigationMenuList>
        </NavigationMenu>
      </div>
    </div>
  </header>;
}
