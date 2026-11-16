import type { Session } from './types';

let requestToken: string | null = null;

export function setRequestToken(token: string | null): void { requestToken = token; }

export class ApiError extends Error {
  constructor(message: string, readonly status: number) { super(message); this.name = 'ApiError'; }
}

export async function request<T>(path: string, options: { method?: string; body?: unknown; signal?: AbortSignal } = {}): Promise<T> {
  const headers = new Headers({ Accept: 'application/json' });
  const method = options.method ?? 'GET';
  if (options.body !== undefined) headers.set('Content-Type', 'application/json');
  if (!['GET', 'HEAD'].includes(method) && requestToken) headers.set('X-CSRF-TOKEN', requestToken);

  const response = await fetch(path, {
    method,
    credentials: 'same-origin',
    headers,
    ...(options.body !== undefined ? { body: JSON.stringify(options.body) } : {}),
    ...(options.signal ? { signal: options.signal } : {}),
  });

  if (!response.ok) {
    const problem: unknown = await response.json().catch(() => null);
    if (response.status === 401) throw new ApiError('Your session has ended. Sign in to continue.', 401);
    if (response.status === 403) throw new ApiError('You do not have permission to make this change or this workspace is inactive.', 403);
    let message = `The request could not be completed (${response.status}). Please try again.`;
    if (problem && typeof problem === 'object') {
      if ('detail' in problem && typeof problem.detail === 'string') message = problem.detail;
      if ('errors' in problem && problem.errors && typeof problem.errors === 'object') {
        const errors = Object.values(problem.errors).flat().filter((error): error is string => typeof error === 'string');
        if (errors.length) message = errors.join(' ');
      }
    }
    throw new ApiError(message, response.status);
  }

  if (response.status === 204 || response.headers.get('content-length') === '0') return undefined as T;
  const body = await response.text();
  return body ? JSON.parse(body) as T : undefined as T;
}

export async function getSession(signal?: AbortSignal): Promise<Session> {
  const session = await request<Session>('/api/session', signal ? { signal } : {});
  setRequestToken(session.requestToken);
  return session;
}

export function errorMessage(error: unknown): string {
  return error instanceof Error ? error.message : 'Something went wrong. Please try again.';
}
