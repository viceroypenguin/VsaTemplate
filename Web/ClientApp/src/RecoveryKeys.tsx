import { useId, useState } from 'react';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
import { Label } from '@/components/ui/label';
import { Alert, Icon } from './components';

export function RecoveryKeys({ name, keys, onClose }: { name: string; keys: string[]; onClose: () => void }) {
  const acknowledgementId = useId();
  const [acknowledged, setAcknowledged] = useState(false);
  const [copied, setCopied] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const keyText = `${name} — tenant recovery keys\n\n${keys.join('\n')}\n`;
  function download() {
    const url = URL.createObjectURL(new Blob([keyText], { type: 'text/plain;charset=utf-8' }));
    const link = document.createElement('a'); link.href = url; link.download = 'tenant-recovery-keys.txt';
    link.click(); setTimeout(() => URL.revokeObjectURL(url), 1000);
  }
  async function copy() {
    try { await navigator.clipboard.writeText(keyText); setCopied(true); setError(null); }
    catch { setError('Copy is unavailable. Download the keys or select and copy them below.'); }
  }
  return <Card className="recovery-panel" role="region" aria-labelledby="recovery-title">
    <div className="recovery-heading"><span className="recovery-icon"><Icon name="key" size={25} /></span><div><div className="eyebrow">TENANT CREATED</div><h2 id="recovery-title">Save your recovery keys</h2></div></div>
    <p><strong>{name}</strong> is ready. These keys are shown once. Save them somewhere secure before leaving or refreshing this page.</p>
    <div className="recovery-keys">{keys.map((key) => <code key={key}>{key}</code>)}</div>
    {error && <Alert>{error}</Alert>}
    <div className="recovery-actions"><Button variant="outline" onClick={() => { void copy(); }}>{copied ? 'Copied' : 'Copy keys'}</Button><Button variant="outline" onClick={download}>Download keys</Button></div>
    <div className="recovery-acknowledgement">
      <Checkbox id={acknowledgementId} checked={acknowledged} onCheckedChange={(checked) => setAcknowledged(checked === true)} />
      <Label htmlFor={acknowledgementId}>I have saved these recovery keys securely.</Label>
    </div>
    <Button className="self-start" disabled={!acknowledged} onClick={onClose}><Icon name="check" size={17} />Continue to workspace</Button>
  </Card>;
}
