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
import type { Organization, Session, Tenant } from './types';

export function WorkspaceHeader({ session, scope, route }: {
  session: Session;
  scope: Tenant | Organization | undefined;
  route: string;
}) {
  const userName = session.user?.name || session.user?.emailAddress || 'Your account';
  const scopeIcon = scope ? ('organizationId' in scope ? 'teams' : 'building') : 'grid';

  return <header className="topbar">
    <div className="topbar-inner">
      <Brand />
      <NavigationMenu className="topbar-navigation" aria-label="Workspace and account" viewport={false}>
        <NavigationMenuList>
          <NavigationMenuItem className="topbar-menu-item">
            <NavigationMenuTrigger className="topbar-trigger workspace-trigger">
              <Icon name={scopeIcon} size={16} />
              <span className="topbar-menu-label">{scope?.name ?? 'Workspaces'}</span>
            </NavigationMenuTrigger>
            <NavigationMenuContent className="topbar-menu-panel">
              <NavigationMenuLink className="topbar-menu-action" href="#/tenants" active={route === '/tenants' || route.startsWith('/tenants/')}>
                <Icon name="building" size={16} />Manage tenants
              </NavigationMenuLink>
              <NavigationMenuLink className="topbar-menu-action" href="#/organizations" active={route === '/organizations' || route.startsWith('/organizations/')}>
                <Icon name="teams" size={16} />Manage organizations
              </NavigationMenuLink>
            </NavigationMenuContent>
          </NavigationMenuItem>
          <NavigationMenuItem className="topbar-menu-item">
            <NavigationMenuTrigger className="topbar-trigger">
              <AccountAvatar name={userName} />
              <span className="topbar-menu-label">{userName}</span>
            </NavigationMenuTrigger>
            <NavigationMenuContent className="topbar-menu-panel">
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
  </header>;
}
