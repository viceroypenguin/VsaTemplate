import { useId, useState } from 'react';
import type { FormEvent } from 'react';
import { AlertDialog, AlertDialogTrigger } from '@/components/ui/alert-dialog';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { errorMessage, request } from '../api';
import { AccountAvatar, Alert, ConfirmAction, Empty, Field, Icon, Loading, Permissions, Retry } from '../components';
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
  const roleSelectId = useId();
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
    setBusy(true); setError(null);
    try { await request(`/api/${scope.kind}/${scope.id}/users/${member.userId}`, { method: 'DELETE' }); onChanged(); }
    catch (error) { setError(errorMessage(error)); }
    finally { setBusy(false); }
  }

  return <section>
    <div className="collection-heading">
      <div><h2>Members <Badge variant="outline">{members.length}</Badge></h2><p>People with access to {scope.name}.</p></div>
      <Button onClick={() => setShowAdd(!showAdd)} aria-expanded={showAdd} disabled={busy}><Icon name="plus" size={17} />Add member</Button>
    </div>
    {error && <Alert>{error}</Alert>}
    {showAdd && <Card className="panel form-panel">
      <h3>Add a member or assign a role</h3>
      <p className="muted">{tenant ? 'Enter their email address. They can access this tenant when they sign in with that address.' : 'Enter their email address. They can access this organization when they sign in with that address.'} To give a member another role, add their email again.</p>
      {assignableRoles.length ? <form className="member-form" onSubmit={(event) => { void add(event); }}>
        <Field label="Email address" type="email" value={email} onChange={(event) => setEmail(event.target.value)} maxLength={500} placeholder="teammate@example.com" required autoFocus disabled={busy} />
        <div className="field">
          <Label htmlFor={roleSelectId} className="field-label">Role</Label>
          <Select value={selectedRoleId} onValueChange={setRoleId} disabled={busy} required>
            <SelectTrigger id={roleSelectId} className="w-full"><SelectValue /></SelectTrigger>
            <SelectContent position="popper">
              {assignableRoles.map((role) => <SelectItem key={role.roleId} value={String(role.roleId)}>{role.name}</SelectItem>)}
            </SelectContent>
          </Select>
        </div>
        <div className="form-actions">
          <Button type="button" variant="outline" onClick={() => setShowAdd(false)} disabled={busy}>Cancel</Button>
          <Button disabled={busy || !email.trim() || !selectedRoleId}>{busy ? 'Adding…' : 'Add member'}</Button>
        </div>
      </form> : <Alert>Create a role with permissions you can delegate before adding members.</Alert>}
    </Card>}
    {members.length ? <div className="table-panel">
      <Table>
        <TableHeader><TableRow>
          <TableHead scope="col">Member</TableHead>
          <TableHead scope="col">Roles</TableHead>
          <TableHead scope="col"><span className="sr-only">Actions</span></TableHead>
        </TableRow></TableHeader>
        <TableBody>{members.map((member) => {
          const memberPermissions = member.roles.flatMap<Permission>((role) => roles.find((item) => item.roleId === role.roleId)?.permissions ?? ['Admin']);
          const mayRemove = canDelegate(scope.permissions, { permissions: memberPermissions });
          return <TableRow key={member.userId}>
            <TableCell>
              <div className="member-cell">
                <AccountAvatar name={member.name || member.emailAddress} />
                <div><strong>{member.name || member.emailAddress}</strong>{member.name && <span>{member.emailAddress}</span>}</div>
              </div>
            </TableCell>
            <TableCell><div className="pills">{member.roles.map((role) => <Badge variant="outline" className="pill" key={role.roleId}>{role.name}</Badge>)}</div></TableCell>
            <TableCell className="table-actions">
              <AlertDialog>
                <AlertDialogTrigger asChild>
                  <Button variant="ghost" className="text-action text-destructive" aria-label={`Remove access for ${member.emailAddress}`} disabled={busy || !mayRemove} title={!mayRemove ? 'Only an administrator can remove this member.' : undefined}>Remove access</Button>
                </AlertDialogTrigger>
                <ConfirmAction
                  title={`Remove ${member.emailAddress} from ${scope.name}?`}
                  description="All their roles in this workspace will be removed."
                  action="Remove access"
                  onConfirm={() => { void remove(member); }}
                />
              </AlertDialog>
            </TableCell>
          </TableRow>;
        })}</TableBody>
      </Table>
    </div> : <Card className="panel"><Empty icon="teams" title="No members yet">Add people and give them a role in this workspace.</Empty></Card>}
  </section>;
}

function Roles({ scope, roles, onChanged }: { scope: Scope; roles: Role[]; onChanged: () => void }) {
  const [editing, setEditing] = useState<Role | 'new' | null>(null);
  return <section><div className="collection-heading"><div><h2>Roles <Badge variant="outline">{roles.length}</Badge></h2><p>Define the permissions available to members of this workspace.</p></div><Button onClick={() => setEditing('new')}><Icon name="plus" size={17} />Create role</Button></div>
    {editing && <RoleEditor key={editing === 'new' ? 'new' : editing.roleId} scope={scope} role={editing === 'new' ? undefined : editing} onCancel={() => setEditing(null)} onSaved={() => { setEditing(null); onChanged(); }} />}
    {roles.length ? <div className="role-list">{roles.map((role) => {
      const mayEdit = canDelegate(scope.permissions, role);
      return <Card className="panel role-row" role="article" key={role.roleId}>
        <span className="role-icon"><Icon name="shield" /></span>
        <div><h3>{role.name}</h3><Permissions permissions={role.permissions} /></div>
        <Button className="edit-role" variant="outline" size="sm" aria-label={`Edit role ${role.name}`} onClick={() => setEditing(role)} disabled={!mayEdit} title={!mayEdit ? 'You can only edit roles within your own permissions.' : undefined}>Edit role</Button>
      </Card>;
    })}</div> : <Card className="panel"><Empty icon="shield" title="Create your first role">Choose a name and the permissions members should receive.</Empty></Card>}
  </section>;
}

function RoleEditor({ scope, role, onCancel, onSaved }: { scope: Scope; role?: Role | undefined; onCancel: () => void; onSaved: () => void }) {
  const permissionId = useId();
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
  return <Card className="panel form-panel">
    <h3>{role ? `Edit ${role.name}` : 'Create a role'}</h3>
    {error && <Alert>{error}</Alert>}
    <form onSubmit={(event) => { void save(event); }}>
      <Field label="Role name" value={name} onChange={(event) => setName(event.target.value)} maxLength={200} placeholder="e.g. Team manager" required autoFocus disabled={busy} />
      <fieldset className="permission-options" disabled={busy}>
        <legend>Permissions</legend>
        {options.map((permission) => {
          const allowed = canDelegate(scope.permissions, { permissions: [permission] });
          const id = `${permissionId}-${permission}`;
          return <Label key={permission} htmlFor={id} className={`permission-option ${allowed ? '' : 'disabled'}`}>
            <Checkbox id={id} checked={permissions.includes(permission)} disabled={busy || !allowed} onCheckedChange={(checked) => setPermissions((current) => {
              if (!checked) return current.filter((item) => item !== permission);
              return permission === 'None' ? ['None'] : [...current.filter((item) => item !== 'None'), permission];
            })} />
            <span><strong>{permissionLabels[permission]}</strong><small>{permission === 'None' ? 'View this workspace without management permissions.' : permission === 'Admin' ? 'Full access, including workspace settings and every permission.' : permission === 'UserManagement' ? 'Add and remove members, create roles, and assign access.' : 'Create and view organizations within this tenant.'}</small></span>
          </Label>;
        })}
      </fieldset>
      <p className="form-hint">You can grant only the permissions you already have.</p>
      <div className="form-actions">
        <Button type="button" variant="outline" onClick={onCancel} disabled={busy}>Cancel</Button>
        <Button disabled={busy || !name.trim() || !permissions.length}>{busy ? 'Saving…' : role ? 'Save role' : 'Create role'}</Button>
      </div>
    </form>
  </Card>;
}
