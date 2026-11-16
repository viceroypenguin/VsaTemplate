import { useState } from 'react';
import { Alert, Icon } from './components';

export function RecoveryKeys({ name, keys, onClose }: { name: string; keys: string[]; onClose: () => void }) {
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
  return <section className="recovery-panel" aria-labelledby="recovery-title">
    <div className="recovery-heading"><span className="recovery-icon"><Icon name="key" size={25} /></span><div><div className="eyebrow">TENANT CREATED</div><h2 id="recovery-title">Save your recovery keys</h2></div></div>
    <p><strong>{name}</strong> is ready. These keys are shown once. Save them somewhere secure before leaving or refreshing this page.</p>
    <div className="recovery-keys">{keys.map((key) => <code key={key}>{key}</code>)}</div>
    {error && <Alert>{error}</Alert>}
    <div className="recovery-actions"><button className="button secondary" onClick={() => { void copy(); }}>{copied ? 'Copied' : 'Copy keys'}</button><button className="button secondary" onClick={download}>Download keys</button></div>
    <label className="checkbox"><input type="checkbox" checked={acknowledged} onChange={(event) => setAcknowledged(event.target.checked)} />I have saved these recovery keys securely.</label>
    <button className="button primary" disabled={!acknowledged} onClick={onClose}><Icon name="check" size={17} />Continue to workspace</button>
  </section>;
}
