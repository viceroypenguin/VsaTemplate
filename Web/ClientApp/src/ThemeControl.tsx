import { useLayoutEffect, useState } from 'react';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Icon } from './components';

type Theme = 'auto' | 'light' | 'dark';
const storageKey = 'workspace-theme';

function readTheme(): Theme {
  try {
    const theme = localStorage.getItem(storageKey);
    if (theme === 'light' || theme === 'dark') return theme;
  } catch { /* The theme still works when storage is unavailable. */ }
  return 'auto';
}

export function useTheme() {
  const [theme, setTheme] = useState(readTheme);
  useLayoutEffect(() => {
    const media = window.matchMedia('(prefers-color-scheme: dark)');
    const applyTheme = () => {
      document.documentElement.dataset.theme = theme === 'auto' ? (media.matches ? 'dark' : 'light') : theme;
    };

    applyTheme();
    if (theme === 'auto') media.addEventListener('change', applyTheme);
    try { localStorage.setItem(storageKey, theme); } catch { /* The theme still works when storage is unavailable. */ }
    return () => {
      if (theme === 'auto') media.removeEventListener('change', applyTheme);
    };
  }, [theme]);

  return { theme, setTheme };
}

export function ThemeControl({ theme, setTheme }: ReturnType<typeof useTheme>) {
  return <div className="theme-control">
    <Icon name="settings" size={15} />
    <Select value={theme} onValueChange={(value) => {
      if (value === 'auto' || value === 'light' || value === 'dark') setTheme(value);
    }}>
      <SelectTrigger size="sm" aria-label="Color theme"><SelectValue /></SelectTrigger>
      <SelectContent position="popper">
        <SelectItem value="auto">System</SelectItem>
        <SelectItem value="light">Light</SelectItem>
        <SelectItem value="dark">Dark</SelectItem>
      </SelectContent>
    </Select>
  </div>;
}
