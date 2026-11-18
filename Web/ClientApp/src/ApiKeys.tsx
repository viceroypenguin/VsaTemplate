import { useId, useState } from 'react';
import type { FormEvent } from 'react';
import { AlertDialog, AlertDialogTrigger } from '@/components/ui/alert-dialog';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
import { Label } from '@/components/ui/label';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { errorMessage, request } from './api';
import { Alert, ConfirmAction, Empty, Field, Icon, Loading, PageHeading, Permissions, Retry } from './components';
import { useResource } from './hooks';
import type { Permission } from './types';

interface ApiKey {
  apiKeyId: number;
  note: string | null;
  permissions: Permission[];
  keySuffix: string;
}

interface ApiKeyList {
  apiKeys: ApiKey[];
  canCreateAdminKey: boolean;
}

export function ApiKeys() {
  const resource = useResource((signal) => request<ApiKeyList>('/api/users/apikey', { signal }), []);
  const adminId = useId();
  const [showCreate, setShowCreate] = useState(false);
  const [note, setNote] = useState('');
  const [lifetime, setLifetime] = useState('30');
  const [admin, setAdmin] = useState(false);
  const [createdKey, setCreatedKey] = useState<string | null>(null);
  const [creating, setCreating] = useState(false);
  const [invalidatingId, setInvalidatingId] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const busy = creating || invalidatingId !== null;
  const days = Number(lifetime);
  const validLifetime = Number.isInteger(days) && days >= 1 && days <= 365;
  const canCreateAdminKey = resource.data?.canCreateAdminKey === true;

  async function create(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!validLifetime || busy) return;

    setCreating(true);
    setError(null);
    setNotice(null);

    try {
      const { apiKey } = await request<{ apiKey: string }>('/api/users/apikey/create', {
        method: 'POST',
        body: { note: note.trim() || null, permissions: [canCreateAdminKey && admin ? 'Admin' : 'None'], lifetime: `${days}.00:00:00` },
      });
      setCreatedKey(apiKey);
      setShowCreate(false);
      setNote('');
      setLifetime('30');
      setAdmin(false);
      resource.refresh();
    } catch (error) {
      setError(errorMessage(error));
    } finally {
      setCreating(false);
    }
  }

  async function invalidate(apiKey: ApiKey) {
    if (busy) return;

    setInvalidatingId(apiKey.apiKeyId);
    setError(null);
    setNotice(null);

    try {
      await request(`/api/users/apikey/${apiKey.apiKeyId}`, { method: 'DELETE' });
      setNotice('API key invalidated.');
      resource.refresh();
    } catch (error) {
      setError(errorMessage(error));
    } finally {
      setInvalidatingId(null);
    }
  }

  return <>
    <PageHeading eyebrow="Your account" title="API keys" action={
      <Button onClick={() => setShowCreate(!showCreate)} aria-expanded={showCreate} disabled={busy || createdKey !== null || !resource.data}>
        <Icon name="plus" size={17} />Create API key
      </Button>
    }>
      Issue keys for your integrations and invalidate keys you no longer use.
    </PageHeading>

    {error && <Alert>{error}</Alert>}
    {notice && <Alert success>{notice}</Alert>}

    {showCreate && <Card className="panel form-panel settings-panel">
      <h2>New API key</h2>
      <form className="grid gap-4" onSubmit={(event) => { void create(event); }}>
        <Field label="Purpose note (optional)" placeholder="e.g. Release automation" value={note} onChange={(event) => setNote(event.target.value)} maxLength={200} disabled={busy} autoFocus />
        <Field label="Lifetime in days" type="number" min={1} max={365} step={1} value={lifetime} onChange={(event) => setLifetime(event.target.value)} required disabled={busy} />
        {canCreateAdminKey && <div className="flex items-center gap-2">
          <Checkbox id={adminId} checked={admin} onCheckedChange={(checked) => setAdmin(checked === true)} disabled={busy} />
          <Label htmlFor={adminId}>Grant administrator permissions</Label>
        </div>}
        <div className="form-actions">
          <Button variant="outline" type="button" onClick={() => setShowCreate(false)} disabled={busy}>Cancel</Button>
          <Button type="submit" disabled={busy || !validLifetime}>{creating ? 'Creating…' : 'Create key'}</Button>
        </div>
      </form>
    </Card>}

    {createdKey && <Card className="panel form-panel settings-panel" role="region" aria-label="New API key">
      <h2>Save your new API key</h2>
      <p>Copy the full key now. It is shown only once.</p>
      <code className="mb-4 rounded-md border bg-muted p-3 text-sm break-all select-all">{createdKey}</code>
      <Button variant="outline" className="self-start" onClick={() => setCreatedKey(null)}>Done</Button>
    </Card>}

    {resource.loading ? <Loading label="Loading API keys…" />
      : resource.error || !resource.data ? <Retry error={resource.error ?? 'No API keys were returned.'} retry={resource.refresh} />
      : resource.data.apiKeys.length === 0 ? <Card className="panel">
        <Empty icon="key" title="No active API keys">Create a key to connect an integration to your account.</Empty>
      </Card>
      : <div className="table-panel">
        <Table aria-label="Issued API keys">
          <TableHeader>
            <TableRow>
              <TableHead scope="col">Name</TableHead>
              <TableHead scope="col">Permissions</TableHead>
              <TableHead scope="col">Key</TableHead>
              <TableHead scope="col"><span className="sr-only">Actions</span></TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {resource.data.apiKeys.map((apiKey) => {
              const name = apiKey.note ?? 'Unnamed key';
              const maskedKey = `****${apiKey.keySuffix}`;

              return <TableRow key={apiKey.apiKeyId}>
                <TableCell className="whitespace-normal wrap-anywhere">{name}</TableCell>
                <TableCell><Permissions permissions={apiKey.permissions} /></TableCell>
                <TableCell><code>{maskedKey}</code></TableCell>
                <TableCell className="table-actions">
                  <AlertDialog>
                    <AlertDialogTrigger asChild>
                      <Button variant="ghost" className="text-action text-destructive" aria-label={`Invalidate ${name} (${maskedKey})`} disabled={busy}>
                        {invalidatingId === apiKey.apiKeyId ? 'Invalidating…' : 'Invalidate'}
                      </Button>
                    </AlertDialogTrigger>
                    <ConfirmAction
                      title={`Invalidate ${name}?`}
                      description={`Integrations using ${maskedKey} will lose access.`}
                      action="Invalidate key"
                      onConfirm={() => { void invalidate(apiKey); }}
                    />
                  </AlertDialog>
                </TableCell>
              </TableRow>;
            })}
          </TableBody>
        </Table>
      </div>}
  </>;
}
