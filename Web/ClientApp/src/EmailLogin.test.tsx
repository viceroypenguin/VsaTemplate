import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { setRequestToken } from './api';
import { EmailLogin } from './EmailLogin';
import { LOGIN_CHALLENGE_KEY, LOGIN_SESSION_KEY } from './loginSession';
import type { LoginSessionState } from './loginSession';

const sessionId = 'b6da5a7d-2d04-4d4b-8ad7-a1a99ce881b2';
const emailAddress = 'person@example.test';

const session: LoginSessionState = {
  sessionId,
  ready: true,
  error: null,
  notice: null,
};

const json = (value: unknown, status = 200) => new Response(JSON.stringify(value), {
  status,
  headers: { 'Content-Type': 'application/json' },
});

function pendingChallenge(expiresAt = new Date(Date.now() + 15 * 60_000).toISOString()) {
  sessionStorage.setItem(LOGIN_CHALLENGE_KEY, JSON.stringify({
    emailAddress,
    expiresAt,
    resendAfter: Date.now() - 1,
  }));
}

describe('email login', () => {
  beforeEach(() => {
    sessionStorage.clear();
    sessionStorage.setItem(LOGIN_SESSION_KEY, sessionId);
    setRequestToken('anonymous-antiforgery-token');
  });

  it('binds requesting and redeeming a code to this tab and preserves leading zeroes', async () => {
    const user = userEvent.setup();
    const onLoggedIn = vi.fn();
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      if (String(input) === '/api/auth/email/request') {
        return json({ expiresAt: new Date(Date.now() + 15 * 60_000).toISOString() });
      }

      if (String(input) === '/api/auth/email/verify') {
        return new Response(null, { status: 204 });
      }

      throw new Error(`Unexpected request ${String(input)}`);
    });

    vi.stubGlobal('fetch', fetchMock);
    render(<EmailLogin session={session} onLoggedIn={onLoggedIn} />);

    const email = screen.getByRole('textbox', { name: 'Email address' });
    await user.type(email, emailAddress);
    await user.click(screen.getByRole('button', { name: 'Send login code' }));

    const code = await screen.findByRole('textbox', { name: 'Login code' });
    expect(email).toBeDisabled();
    expect(email).toHaveValue(emailAddress);
    expect(code).toHaveAttribute('inputmode', 'numeric');
    expect(code).toHaveAttribute('autocomplete', 'one-time-code');
    expect(screen.getByRole('button', { name: /Send a new code/ })).toBeDisabled();

    await user.type(code, '012345');
    expect(sessionStorage.getItem(LOGIN_CHALLENGE_KEY)).not.toContain('012345');
    await user.click(screen.getByRole('button', { name: 'Log in' }));

    await waitFor(() => expect(onLoggedIn).toHaveBeenCalledOnce());

    const requestCalls = fetchMock.mock.calls as [RequestInfo | URL, RequestInit?][];
    const requestOptions = requestCalls.find(([path]) => path === '/api/auth/email/request')?.[1];
    const verifyOptions = requestCalls.find(([path]) => path === '/api/auth/email/verify')?.[1];

    expect(JSON.parse(requestOptions?.body as string)).toEqual({ emailAddress, sessionId });
    expect(JSON.parse(verifyOptions?.body as string)).toEqual({ emailAddress, sessionId, token: '012345' });
    expect(new Headers(requestOptions?.headers).get('X-CSRF-TOKEN')).toBe('anonymous-antiforgery-token');
    expect(new Headers(verifyOptions?.headers).get('X-CSRF-TOKEN')).toBe('anonymous-antiforgery-token');
    expect(verifyOptions?.credentials).toBe('same-origin');
    expect(sessionStorage.getItem(LOGIN_CHALLENGE_KEY)).toBeNull();
    expect(sessionStorage.getItem(LOGIN_SESSION_KEY)).toBe(sessionId);
  });

  it('keeps the email editable when requesting the email fails', async () => {
    const user = userEvent.setup();
    const fetchMock = vi.fn().mockResolvedValue(json({ detail: 'Mail is temporarily unavailable. Please try again.' }, 503));

    vi.stubGlobal('fetch', fetchMock);
    render(<EmailLogin session={session} onLoggedIn={vi.fn()} />);

    const email = screen.getByRole('textbox', { name: 'Email address' });
    await user.type(email, emailAddress);
    await user.click(screen.getByRole('button', { name: 'Send login code' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Mail is temporarily unavailable. Please try again.');
    expect(email).toBeEnabled();
    expect(email).toHaveValue(emailAddress);
    expect(screen.queryByRole('textbox', { name: 'Login code' })).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Send login code' })).toBeEnabled();
  });

  it('shows a rejected code without logging the user in', async () => {
    const user = userEvent.setup();
    const onLoggedIn = vi.fn();
    pendingChallenge();

    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(json({ detail: 'This code is invalid or expired. Request a new code in this tab.' }, 400)));
    render(<EmailLogin session={session} onLoggedIn={onLoggedIn} />);

    await user.type(screen.getByRole('textbox', { name: 'Login code' }), '123456');
    await user.click(screen.getByRole('button', { name: 'Log in' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('This code is invalid or expired. Request a new code in this tab.');
    expect(onLoggedIn).not.toHaveBeenCalled();
    expect(screen.getByRole('textbox', { name: 'Email address' })).toBeDisabled();
  });

  it('resumes a pending login after a reload without requesting another email', async () => {
    const user = userEvent.setup();
    const onLoggedIn = vi.fn();
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(json({ expiresAt: new Date(Date.now() + 15 * 60_000).toISOString() }))
      .mockResolvedValueOnce(new Response(null, { status: 204 }));

    vi.stubGlobal('fetch', fetchMock);
    const firstPage = render(<EmailLogin session={session} onLoggedIn={onLoggedIn} />);

    await user.type(screen.getByRole('textbox', { name: 'Email address' }), emailAddress);
    await user.click(screen.getByRole('button', { name: 'Send login code' }));
    await screen.findByRole('textbox', { name: 'Login code' });
    firstPage.unmount();

    render(<EmailLogin session={session} onLoggedIn={onLoggedIn} />);

    expect(screen.getByRole('textbox', { name: 'Email address' })).toHaveValue(emailAddress);
    expect(screen.getByRole('textbox', { name: 'Email address' })).toBeDisabled();
    expect(fetchMock).toHaveBeenCalledOnce();

    await user.type(screen.getByRole('textbox', { name: 'Login code' }), '456789');
    await user.click(screen.getByRole('button', { name: 'Log in' }));
    await waitFor(() => expect(onLoggedIn).toHaveBeenCalledOnce());

    const verifyOptions = fetchMock.mock.calls[1]?.[1] as RequestInit;
    expect(JSON.parse(verifyOptions.body as string)).toEqual({ emailAddress, sessionId, token: '456789' });
  });

  it('requires a new code after expiry and keeps the same tab session when resending', async () => {
    const user = userEvent.setup();
    pendingChallenge(new Date(Date.now() - 1_000).toISOString());

    const fetchMock = vi.fn().mockResolvedValue(json({ expiresAt: new Date(Date.now() + 15 * 60_000).toISOString() }));
    vi.stubGlobal('fetch', fetchMock);
    render(<EmailLogin session={session} onLoggedIn={vi.fn()} />);

    expect(screen.getByRole('button', { name: 'Log in' })).toBeDisabled();
    expect(screen.getByText(/expired/i)).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: /Send a new code/ }));

    await waitFor(() => expect(fetchMock).toHaveBeenCalledOnce());

    const options = fetchMock.mock.calls[0]?.[1] as RequestInit;
    expect(JSON.parse(options.body as string)).toEqual({ emailAddress, sessionId });
    expect(screen.getByRole('textbox', { name: 'Login code' })).toHaveValue('');
    expect(screen.getByRole('button', { name: /Send a new code/ })).toBeDisabled();
  });

  it('allows changing the email while retaining the tab session and clearing the old pending login', async () => {
    const user = userEvent.setup();
    pendingChallenge();
    const fetchMock = vi.fn().mockResolvedValue(json({ expiresAt: new Date(Date.now() + 15 * 60_000).toISOString() }));

    vi.stubGlobal('fetch', fetchMock);
    render(<EmailLogin session={session} onLoggedIn={vi.fn()} />);

    await user.click(screen.getByRole('button', { name: 'Use a different email' }));

    const email = screen.getByRole('textbox', { name: 'Email address' });
    expect(email).toBeEnabled();
    expect(screen.queryByRole('textbox', { name: 'Login code' })).not.toBeInTheDocument();
    expect(sessionStorage.getItem(LOGIN_CHALLENGE_KEY)).toBeNull();
    expect(sessionStorage.getItem(LOGIN_SESSION_KEY)).toBe(sessionId);

    await user.clear(email);
    await user.type(email, 'another@example.test');
    await user.click(screen.getByRole('button', { name: 'Send login code' }));

    await screen.findByRole('textbox', { name: 'Login code' });

    const options = fetchMock.mock.calls[0]?.[1] as RequestInit;
    expect(JSON.parse(options.body as string)).toEqual({ emailAddress: 'another@example.test', sessionId });
  });
});
