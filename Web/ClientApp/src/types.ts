export type Permission = 'None' | 'Admin' | 'UserManagement' | 'OrganizationManagement';

export interface Session {
  isAuthenticated: boolean;
  isAdmin: boolean;
  user: { userId: number; name: string | null; emailAddress: string } | null;
  requestToken: string | null;
}

export interface Tenant {
  tenantId: number;
  name: string;
  isActive: boolean;
  permissions: Permission[];
}

export interface Organization {
  organizationId: number;
  tenantId: number;
  name: string;
  isActive: boolean;
  permissions: Permission[];
}

export interface Role {
  roleId: number;
  name: string;
  permissions: Permission[];
}

export interface Member {
  userId: number;
  name: string | null;
  emailAddress: string;
  roles: { roleId: number; name: string }[];
}

export type Scope = {
  kind: 'tenants' | 'organizations';
  id: number;
  name: string;
  isActive: boolean;
  permissions: Permission[];
};

export function hasPermission(permissions: readonly Permission[], permission: Permission): boolean {
  return permissions.includes('Admin') || permissions.includes(permission);
}

export function canDelegate(permissions: readonly Permission[], role: Pick<Role, 'permissions'>): boolean {
  return permissions.includes('Admin') || role.permissions.every((p) => p === 'None' || (p !== 'Admin' && permissions.includes(p)));
}

export const permissionLabels: Record<Permission, string> = {
  None: 'Member',
  Admin: 'Administrator',
  UserManagement: 'Manage members and roles',
  OrganizationManagement: 'Manage organizations',
};
