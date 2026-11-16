import { useEffect, useId, useState } from 'react';
import type { InputHTMLAttributes, ReactNode } from 'react';
import { permissionLabels } from './types';
import type { Permission } from './types';

const paths = {
  grid: 'M3 3h7v7H3z M14 3h7v7h-7z M3 14h7v7H3z M14 14h7v7h-7z',
  building: 'M3 21h18 M5 21V3h10v18 M15 9h4v12 M8 7h4 M8 11h4 M8 15h4 M9 21v-3h2v3',
  teams: 'M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2 M22 21v-2a4 4 0 0 0-3-3.87 M16 3a4 4 0 0 1 0 8 M13 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0',
  arrow: 'M5 12h14 M13 6l6 6-6 6',
  plus: 'M12 5v14 M5 12h14',
  shield: 'M12 3l8 3v6c0 5-8 9-8 9s-8-4-8-9V6z M8 12l3 3 5-6',
  settings: 'M12 8a4 4 0 1 0 0 8 4 4 0 0 0 0-8 M12 2v3 M12 19v3 M2 12h3 M19 12h3 M5 5l2 2 M17 17l2 2 M5 19l2-2 M17 7l2-2',
  key: 'M15 3a6 6 0 1 1-4.8 9.6L3 20v-4l5.4-5.4A6 6 0 0 1 15 3 M16 7h.01',
  search: 'M21 21l-4.3-4.3 M19 11a8 8 0 1 1-16 0 8 8 0 0 1 16 0',
  check: 'M5 12l4 4L19 6',
  logout: 'M9 5H5v14h4 M13 8l4 4-4 4 M8 12h13',
};
export function Icon({ name, size = 20 }: { name: keyof typeof paths; size?: number }) {
  return <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true"><path d={paths[name]} /></svg>;
}

export function Brand() {
  return <a className="brand" href="#/tenants" aria-label="Workspace home"><span className="brand-mark"><Icon name="grid" /></span><span>Workspace<span className="brand-subtitle">VSA TEMPLATE</span></span></a>;
}

export function ThemeControl() {
  const [theme, setTheme] = useState(() => {
    try { return localStorage.getItem('workspace-theme') ?? 'auto'; }
    catch { return 'auto'; }
  });
  useEffect(() => {
    const media = window.matchMedia('(prefers-color-scheme: dark)');
    const apply = () => { document.documentElement.dataset.theme = theme === 'auto' ? media.matches ? 'dark' : 'light' : theme; };
    apply();
    media.addEventListener('change', apply);
    try { localStorage.setItem('workspace-theme', theme); } catch { /* Storage may be unavailable in a private session. */ }
    return () => media.removeEventListener('change', apply);
  }, [theme]);
  return <label className="theme-control"><Icon name="settings" size={15} /><span className="sr-only">Color theme</span><select aria-label="Color theme" value={theme} onChange={(event) => setTheme(event.target.value)}><option value="auto">System</option><option value="light">Light</option><option value="dark">Dark</option></select></label>;
}

export function Alert({ children, success = false }: { children: ReactNode; success?: boolean }) {
  return <div className={`alert ${success ? 'alert-success' : 'alert-error'}`} role={success ? 'status' : 'alert'}>{children}</div>;
}

export function Loading({ label = 'Loading workspace…' }: { label?: string }) {
  return <div className="loading" role="status"><span className="spinner" />{label}</div>;
}

export function Empty({ icon = 'building', title, children }: { icon?: keyof typeof paths; title: string; children: ReactNode }) {
  return <div className="empty-state"><span className="empty-icon"><Icon name={icon} size={28} /></span><h3>{title}</h3><p>{children}</p></div>;
}

export function Status({ active }: { active: boolean }) {
  return <span className={`status ${active ? 'status-active' : 'status-inactive'}`}><span />{active ? 'Active' : 'Inactive'}</span>;
}

export function Field({ label, hint, ...props }: InputHTMLAttributes<HTMLInputElement> & { label: string; hint?: string }) {
  const id = useId();
  return <label className="field" htmlFor={id}><span>{label}</span><input id={id} aria-describedby={hint ? `${id}-hint` : undefined} {...props} />{hint && <small id={`${id}-hint`}>{hint}</small>}</label>;
}

export function PageHeading({ eyebrow, title, children, action }: { eyebrow: string; title: string; children?: ReactNode; action?: ReactNode }) {
  return <header className="page-heading"><div><div className="eyebrow">{eyebrow}</div><h1>{title}</h1>{children && <p>{children}</p>}</div>{action && <div className="heading-action">{action}</div>}</header>;
}

export function Permissions({ permissions }: { permissions: readonly Permission[] }) {
  return <div className="pills">{permissions.filter((p) => p !== 'None').map((p) => <span className="pill" key={p}>{permissionLabels[p]}</span>)}{(!permissions.length || permissions.every((p) => p === 'None')) && <span className="pill">Member</span>}</div>;
}

export function Retry({ error, retry }: { error: string; retry: () => void }) {
  return <><Alert>{error}</Alert><button className="button secondary" onClick={retry}>Try again</button></>;
}
