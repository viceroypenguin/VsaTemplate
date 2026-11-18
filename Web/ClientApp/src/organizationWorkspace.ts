import type { Organization, Tenant } from './types';

export type OrganizationWorkspaceEntry = {
  organization: Organization;
  context: string | undefined;
};

export function organizationWorkspaceHref(organizationId: number) {
  return `#/organizations/${organizationId}/workspace`;
}

export function canOpenOrganizationWorkspace(organization: Organization) {
  return organization.isActive && organization.permissions.length > 0;
}

export function organizationWorkspaceEntries(organizations: readonly Organization[], tenants: readonly Tenant[]): OrganizationWorkspaceEntry[] {
  const tenantNames = new Map(tenants.map((tenant) => [tenant.tenantId, tenant.name]));
  const names = new Map<string, number>();
  for (const { name } of organizations) {
    const key = name.toLowerCase();
    names.set(key, (names.get(key) ?? 0) + 1);
  }

  return organizations.map((organization) => {
    const tenantName = tenantNames.get(organization.tenantId);
    const duplicate = (names.get(organization.name.toLowerCase()) ?? 0) > 1;
    const identity = `Organization #${organization.organizationId}`;
    const context = duplicate ? [tenantName, identity].filter(Boolean).join(' · ') : tenantName;
    return { organization, context };
  });
}

export function readLastOrganization(userId: number | undefined) {
  if (userId === undefined) return null;
  try {
    return localStorage.getItem(`workspace-organization:${userId}`);
  } catch {
    return null;
  }
}

export function rememberOrganization(userId: number, organizationId: number) {
  try {
    localStorage.setItem(`workspace-organization:${userId}`, String(organizationId));
  } catch { /* The workspace URL still carries the organization when storage is unavailable. */ }
}
