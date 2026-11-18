import { useId } from 'react';
import type { ComponentProps, InputHTMLAttributes, ReactNode } from 'react';
import { Alert as UiAlert, AlertDescription } from '@/components/ui/alert';
import {
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Spinner } from '@/components/ui/spinner';
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
  return <svg width={size} height={size} style={{ width: size, height: size }} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true"><path d={paths[name]} /></svg>;
}

export function Brand({ href = '#/' }: { href?: string }) {
  return <a className="brand" href={href} aria-label="Workspace home"><span className="brand-mark"><Icon name="grid" /></span><span>Workspace<span className="brand-subtitle">VSA TEMPLATE</span></span></a>;
}

export function AccountAvatar({ name }: { name: string }) {
  return <Avatar className="avatar" aria-hidden="true">
    <AvatarFallback className="bg-primary-soft text-primary">{name.slice(0, 1).toUpperCase()}</AvatarFallback>
  </Avatar>;
}

export function Alert({ children, success = false }: { children: ReactNode; success?: boolean }) {
  return <UiAlert className={`notice ${success ? 'notice-success' : 'notice-error'}`} variant={success ? 'default' : 'destructive'} role={success ? 'status' : 'alert'}>
    <AlertDescription className="block text-xs text-current">{children}</AlertDescription>
  </UiAlert>;
}

export function Loading({ label = 'Loading workspace…' }: { label?: string }) {
  return <div className="loading" role="status"><Spinner className="text-primary" aria-hidden="true" />{label}</div>;
}

export function Empty({ icon = 'building', title, children }: { icon?: keyof typeof paths; title: string; children: ReactNode }) {
  return <div className="empty-state"><span className="empty-icon"><Icon name={icon} size={28} /></span><h3>{title}</h3><p>{children}</p></div>;
}

export function Status({ active }: { active: boolean }) {
  return <Badge className={`status ${active ? 'status-active' : 'status-inactive'}`} variant="secondary">
    <span aria-hidden="true" />{active ? 'Active' : 'Inactive'}
  </Badge>;
}

export function Field({ label, hint, id, ...props }: InputHTMLAttributes<HTMLInputElement> & { label: string; hint?: string }) {
  const generatedId = useId();
  const inputId = id ?? generatedId;
  return <div className="field">
    <Label htmlFor={inputId} className="field-label">{label}</Label>
    <Input id={inputId} aria-describedby={hint ? `${inputId}-hint` : undefined} {...props} />
    {hint && <small id={`${inputId}-hint`}>{hint}</small>}
  </div>;
}

export function PageHeading({ eyebrow, title, children, action }: { eyebrow: string; title: string; children?: ReactNode; action?: ReactNode }) {
  return <header className="page-heading"><div><div className="eyebrow">{eyebrow}</div><h1>{title}</h1>{children && <p>{children}</p>}</div>{action && <div className="heading-action">{action}</div>}</header>;
}

export function Permissions({ permissions }: { permissions: readonly Permission[] }) {
  const labels = permissions.filter((permission) => permission !== 'None').map((permission) => permissionLabels[permission]);
  return <div className="pills">{(labels.length ? labels : ['Member']).map((label) => <Badge key={label} variant="outline" className="pill">{label}</Badge>)}</div>;
}

export function Retry({ error, retry }: { error: string; retry: () => void }) {
  return <><Alert>{error}</Alert><Button variant="outline" onClick={retry}>Try again</Button></>;
}

export function ConfirmAction({
  title, description, action, onConfirm, onCloseAutoFocus,
}: {
  title: string;
  description: string;
  action: string;
  onConfirm: () => void;
  onCloseAutoFocus?: ComponentProps<typeof AlertDialogContent>['onCloseAutoFocus'];
}) {
  return <AlertDialogContent className="bg-card" onCloseAutoFocus={onCloseAutoFocus}>
    <AlertDialogHeader>
      <AlertDialogTitle className="m-0 wrap-anywhere">{title}</AlertDialogTitle>
      <AlertDialogDescription className="m-0">{description}</AlertDialogDescription>
    </AlertDialogHeader>
    <AlertDialogFooter>
      <AlertDialogCancel>Cancel</AlertDialogCancel>
      <AlertDialogAction className="confirmation-action" variant="destructive" onClick={onConfirm}>{action}</AlertDialogAction>
    </AlertDialogFooter>
  </AlertDialogContent>;
}
