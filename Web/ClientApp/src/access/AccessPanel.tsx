import { useState } from 'react';
import type { FormEvent } from 'react';
import { errorMessage, request } from '../api';
import { Alert, Empty, Field, Icon, Loading, Permissions, Retry } from '../components';
import { useResource } from '../hooks';
import { canDelegate, permissionLabels } from '../types';
import type { Member, Permission, Role, Scope } from '../types';

export function AccessPanel({ scope, view, onChanged }: { scope: Scope; view: 'members' | 'roles'; onChanged: () => void }) {
  const resource = useResource(async (signal) => {
    const [roles, members] = await Promise.all([
      request<{ roles: Role[] }>(`/api/${scope.kind}/${scope.id}/roles`, { signal }),
      request<{ users: Member[] }>(`/api/${scope.kind}/${scope.id}/users`, { signal }),
    ]);
    return { roles: roles.roles, members: members.users };
  }, [scope.id, scope.kind]);

  if (resource.loading) return <Loading label={`Loading ${view}…`} />;
  if (resource.error || !resource.data) return <Retry error={resource.error ?? 'No access details were returned.'} retry={resource.refresh} />;
  const onMutation = () => { resource.refresh(); onChanged(); };
  return view === 'members' ? <Members scope={scope} roles={resource.data.roles} members={resource.data.members} onChanged={onMutation} /> : <Roles scope={scope} roles={resource.data.roles} onChanged={onMutation} />;
}

function Members({ scope, roles, members, onChanged }: { scope: Scope; roles: Role[]; members: Member[]; onChanged: () => void }) {
  const [showAdd, setShowAdd] = useState(false);
  const [email, setEmail] = useState('');
  const [roleId, setRoleId] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const assignableRoles = roles.filter((role) => canDelegate(scope.permissions, role));
  const selectedRoleId = roleId || assignableRoles[0]?.roleId.toString() || '';
  const tenant = scope.kind === 'tenants';

  async function add(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setBusy(true); setError(null);
    try {
      await request(`/api/${scope.kind}/${scope.id}/users`, { method: 'PUT', body: { emailAddress: email.trim(), roleId: Number(selectedRoleId) } });
      setEmail(''); setShowAdd(false); onChanged();
    } catch (error) { setError(errorMessage(error)); }
    finally { setBusy(false); }
  }

  async function remove(member: Member) {
    if (!window.confirm(`Remove ${member.emailAddress} from ${scope.name}? All their roles in this workspace will be removed.`)) return;
    setBusy(true); setError(null);
    try { await request(`/api/${scope.kind}/${scope.id}/users/${member.userId}`, { method: 'DELETE' }); onChanged(); }
    catch (error) { setError(errorMessage(error)); }
    finally { setBusy(false); }
  }

  return <section><div className="collection-heading"><div><h2>Members <span>{members.length}</span></h2><p>People with access to {scope.name}.</p></div><button className="button primary" onClick={() => setShowAdd(!showAdd)} aria-expanded={showAdd} disabled={busy}><Icon name="plus" size={17} />Add member</button></div>
    {error && <Alert>{error}</Alert>}
    {showAdd && <div className="panel form-panel"><h3>Add a member or assign a role</h3><p className="muted">{tenant ? 'Enter their email address. They can access this tenant when they sign in with that address.' : 'Enter the email address of an existing member of the parent tenant.'} To give a member another role, add their email again.</p>{assignableRoles.length ? <form className="member-form" onSubmit={(event) => { void add(event); }}><Field label="Email address" type="email" value={email} onChange={(event) => setEmail(event.target.value)} maxLength={500} placeholder="teammate@example.com" required autoFocus disabled={busy} /><label className="field"><span>Role</span><select value={selectedRoleId} onChange={(event) => setRoleId(event.target.value)} disabled={busy} required>{assignableRoles.map((role) => <option key={role.roleId} value={role.roleId}>{role.name}</option>)}</select></label><div className="form-actions"><button type="button" className="button secondary" onClick={() => setShowAdd(false)} disabled={busy}>Cancel</button><button className="button primary" disabled={busy || !email.trim() || !selectedRoleId}>{busy ? 'Adding…' : 'Add member'}</button></div></form> : <Alert>Create a role with permissions you can delegate before adding members.</Alert>}</div>}
    {members.length ? <div className="table-panel"><table><thead><tr><th scope="col">Member</th><th scope="col">Roles</th><th scope="col"><span className="sr-only">Actions</span></th></tr></thead><tbody>{members.map((member) => {
      const memberPermissions = member.roles.flatMap<Permission>((role) => roles.find((item) => item.roleId === role.roleId)?.permissions ?? ['Admin']);
      const mayRemove = canDelegate(scope.permissions, { permissions: memberPermissions });
      return <tr key={member.userId}><td><div className="member-cell"><span className="avatar">{(member.name || member.emailAddress).slice(0, 1).toUpperCase()}</span><div><strong>{member.name || member.emailAddress}</strong>{member.name && <span>{member.emailAddress}</span>}</div></div></td><td><div className="pills">{member.roles.map((role) => <span className="pill" key={role.roleId}>{role.name}</span>)}</div></td><td className="table-actions"><button className="text-button danger" aria-label={`Remove access for ${member.emailAddress}`} disabled={busy || !mayRemove} title={!mayRemove ? 'Only an administrator can remove this member.' : undefined} onClick={() => { void remove(member); }}>Remove access</button></td></tr>;
    })}</tbody></table></div> : <div className="panel"><Empty icon="teams" title="No members yet">Add people and give them a role in this workspace.</Empty></div>}
  </section>;
}

function Roles({ scope, roles, onChanged }: { scope: Scope; roles: Role[]; onChanged: () => void }) {
  const [editing, setEditing] = useState<Role | 'new' | null>(null);
  return <section><div className="collection-heading"><div><h2>Roles <span>{roles.length}</span></h2><p>Define the permissions available to members of this workspace.</p></div><button className="button primary" onClick={() => setEditing('new')}><Icon name="plus" size={17} />Create role</button></div>
    {editing && <RoleEditor key={editing === 'new' ? 'new' : editing.roleId} scope={scope} role={editing === 'new' ? undefined : editing} onCancel={() => setEditing(null)} onSaved={() => { setEditing(null); onChanged(); }} />}
    {roles.length ? <div className="role-list">{roles.map((role) => <article className="panel role-row" key={role.roleId}><span className="role-icon"><Icon name="shield" /></span><div><h3>{role.name}</h3><Permissions permissions={role.permissions} /></div><button className="button secondary small" aria-label={`Edit role ${role.name}`} onClick={() => setEditing(role)} disabled={!canDelegate(scope.permissions, role)} title={!canDelegate(scope.permissions, role) ? 'You can only edit roles within your own permissions.' : undefined}>Edit role</button></article>)}</div> : <div className="panel"><Empty icon="shield" title="Create your first role">Choose a name and the permissions members should receive.</Empty></div>}
  </section>;
}

function RoleEditor({ scope, role, onCancel, onSaved }: { scope: Scope; role?: Role | undefined; onCancel: () => void; onSaved: () => void }) {
  const options: Permission[] = scope.kind === 'tenants' ? ['None', 'Admin', 'UserManagement', 'OrganizationManagement'] : ['None', 'Admin', 'UserManagement'];
  const [name, setName] = useState(role?.name ?? '');
  const [permissions, setPermissions] = useState<Permission[]>(role?.permissions ?? ['None']);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setBusy(true); setError(null);
    const path = role ? `/api/${scope.kind}/${scope.id}/roles/${role.roleId}` : `/api/${scope.kind}/${scope.id}/roles${scope.kind === 'organizations' ? '/create' : ''}`;
    try { await request(path, { method: !role && scope.kind === 'organizations' ? 'POST' : 'PUT', body: { name: name.trim(), permissions } }); onSaved(); }
    catch (error) { setError(errorMessage(error)); }
    finally { setBusy(false); }
  }
  return <div className="panel form-panel"><h3>{role ? `Edit ${role.name}` : 'Create a role'}</h3>{error && <Alert>{error}</Alert>}<form onSubmit={(event) => { void save(event); }}><Field label="Role name" value={name} onChange={(event) => setName(event.target.value)} maxLength={200} placeholder="e.g. Team manager" required autoFocus disabled={busy} /><fieldset className="permission-options" disabled={busy}><legend>Permissions</legend>{options.map((permission) => {
    const allowed = canDelegate(scope.permissions, { permissions: [permission] });
    return <label key={permission} className={`permission-option ${allowed ? '' : 'disabled'}`}><input type="checkbox" checked={permissions.includes(permission)} disabled={!allowed} onChange={(event) => setPermissions(event.target.checked ? permission === 'None' ? ['None'] : [...permissions.filter((p) => p !== 'None'), permission] : permissions.filter((p) => p !== permission))} /><span><strong>{permissionLabels[permission]}</strong><small>{permission === 'None' ? 'View this workspace without management permissions.' : permission === 'Admin' ? 'Full access, including workspace settings and every permission.' : permission === 'UserManagement' ? 'Add and remove members, create roles, and assign access.' : 'Create and view organizations within this tenant.'}</small></span></label>;
  })}</fieldset><p className="form-hint">You can grant only the permissions you already have.</p><div className="form-actions"><button type="button" className="button secondary" onClick={onCancel} disabled={busy}>Cancel</button><button className="button primary" disabled={busy || !name.trim() || !permissions.length}>{busy ? 'Saving…' : role ? 'Save role' : 'Create role'}</button></div></form></div>;
}
