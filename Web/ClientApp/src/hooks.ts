import { useCallback, useEffect, useState } from 'react';
import { errorMessage } from './api';

export function useResource<T>(load: (signal: AbortSignal) => Promise<T>, dependencies: readonly unknown[]) {
  const [data, setData] = useState<T | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [version, setVersion] = useState(0);
  const refresh = useCallback(() => setVersion((value) => value + 1), []);

  useEffect(() => {
    const controller = new AbortController();
    setLoading(true);
    setError(null);
    setData(null);
    void load(controller.signal).then((value) => {
      if (!controller.signal.aborted) setData(value);
    }).catch((error: unknown) => {
      if (!controller.signal.aborted) setError(errorMessage(error));
    }).finally(() => {
      if (!controller.signal.aborted) setLoading(false);
    });
    return () => controller.abort();
    // Callers supply every value used by their loader, just like useEffect.
  }, [...dependencies, version]);

  return { data, error, loading, refresh };
}

export function useRoute(): string {
  const getRoute = () => window.location.hash.slice(1).split('?')[0] || '/tenants';
  const [route, setRoute] = useState(getRoute);
  useEffect(() => {
    const onHashChange = () => { setRoute(getRoute()); window.scrollTo(0, 0); };
    window.addEventListener('hashchange', onHashChange);
    return () => window.removeEventListener('hashchange', onHashChange);
  }, []);
  return route;
}

export function navigate(route: string): void { window.location.hash = route; }
