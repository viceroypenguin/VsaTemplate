import { useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { ApiError, errorMessage, request } from './api';
import { Alert, Field, Icon } from './components';
import {
  clearLoginChallenge,
  readLoginChallenge,
  saveLoginChallenge,
} from './loginSession';
import type { LoginChallenge, LoginSessionState } from './loginSession';

interface EmailLoginProps {
  onLoggedIn: () => void;
  session: LoginSessionState;
}

export function EmailLogin({ onLoggedIn, session }: EmailLoginProps) {
  const [challenge, setChallenge] = useState(readLoginChallenge);
  const [emailAddress, setEmailAddress] = useState(() => challenge?.emailAddress ?? '');
  const [token, setToken] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [now, setNow] = useState(Date.now);
  const expiresAt = challenge ? Date.parse(challenge.expiresAt) : null;
  const expired = expiresAt !== null && now >= expiresAt;
  const resendSeconds = challenge ? Math.max(0, Math.ceil((challenge.resendAfter - now) / 1000)) : 0;
  const unavailable = !session.ready || !session.sessionId || session.error !== null;

  useEffect(() => {
    if (!challenge) return;

    const timer = window.setInterval(() => setNow(Date.now()), 1000);

    return () => window.clearInterval(timer);
  }, [challenge]);

  async function sendCode(event?: FormEvent<HTMLFormElement>) {
    event?.preventDefault();

    if (unavailable || !session.sessionId) return;

    setBusy(true);
    setError(null);

    try {
      const response = await request<{ expiresAt: string }>('/api/auth/email/request', {
        method: 'POST',
        body: {
          emailAddress: emailAddress.trim(),
          sessionId: session.sessionId,
        },
      });

      const requestedAt = Date.now();
      const nextChallenge: LoginChallenge = {
        emailAddress: emailAddress.trim(),
        expiresAt: response.expiresAt,
        resendAfter: requestedAt + 60_000,
      };

      saveLoginChallenge(nextChallenge);
      setChallenge(nextChallenge);
      setNow(requestedAt);
      setToken('');
    } catch (error) {
      setError(errorMessage(error));
    } finally {
      setBusy(false);
    }
  }

  async function logIn(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!challenge || unavailable || !session.sessionId || expired) return;

    setBusy(true);
    setError(null);

    try {
      await request<void>('/api/auth/email/verify', {
        method: 'POST',
        body: {
          emailAddress: challenge.emailAddress,
          sessionId: session.sessionId,
          token,
        },
      });

      clearLoginChallenge();
      onLoggedIn();
    } catch (error) {
      setError(error instanceof ApiError && error.status === 401
        ? 'That code could not be verified. Check the code or request a new one in this tab.'
        : errorMessage(error));
    } finally {
      setBusy(false);
    }
  }

  function changeEmail() {
    clearLoginChallenge();
    setChallenge(null);
    setToken('');
    setError(null);
  }

  return (
    <Card className="panel email-login" role="region" aria-labelledby="login-title">
      <div className="email-login-heading">
        <span className="entity-icon"><Icon name="key" size={22} /></span>
        <div>
          <h2 id="login-title">Log in to your workspace</h2>
          <p>We will email you a one-time login code.</p>
        </div>
      </div>

      {session.error && <Alert>{session.error}</Alert>}
      {session.notice && <Alert success>{session.notice}</Alert>}
      {error && <Alert>{error}</Alert>}

      {!challenge ? (
        <form className="email-login-form" onSubmit={(event) => { void sendCode(event); }}>
          <Field
            label="Email address"
            type="email"
            value={emailAddress}
            onChange={(event) => setEmailAddress(event.target.value)}
            placeholder="you@example.com"
            autoComplete="email"
            maxLength={500}
            required
            autoFocus
            disabled={busy}
          />

          <Button disabled={busy || unavailable || !emailAddress.trim()}>
            {busy ? 'Sending code…' : 'Send login code'}
            <Icon name="arrow" size={17} />
          </Button>

          <p className="form-hint">Keep this tab open. The code can only be used here and expires after 15 minutes.</p>
        </form>
      ) : (
        <form className="email-login-form" onSubmit={(event) => { void logIn(event); }}>
          <Field label="Email address" type="email" value={challenge.emailAddress} disabled />

          <p className="login-delivery-note" role="status">Check your email for a six-digit code, then enter it in this tab.</p>

          <Field
            key={challenge.expiresAt}
            label="Login code"
            value={token}
            onChange={(event) => setToken(event.target.value.replace(/\D/g, '').slice(0, 6))}
            className="login-code"
            type="text"
            inputMode="numeric"
            autoComplete="one-time-code"
            pattern="[0-9]{6}"
            maxLength={6}
            placeholder="000000"
            required
            autoFocus
            disabled={busy || expired}
          />

          {expired ? (
            <Alert>This code has expired. Request a new code to continue in this tab.</Alert>
          ) : (
            <p className="form-hint">
              Expires at {new Date(challenge.expiresAt).toLocaleTimeString(undefined, { hour: 'numeric', minute: '2-digit' })}.
              {' '}Each code works once and only in the tab that requested it.
            </p>
          )}

          <Button disabled={busy || unavailable || expired || token.length !== 6}>
            {busy ? 'Logging in…' : 'Log in'}
            <Icon name="arrow" size={17} />
          </Button>

          <div className="email-login-secondary">
            <Button
              variant="ghost"
              className="text-action"
              type="button"
              onClick={() => { void sendCode(); }}
              disabled={busy || unavailable || resendSeconds > 0}
            >
              Send a new code{resendSeconds > 0 ? ` (${resendSeconds}s)` : ''}
            </Button>

            <Button variant="ghost" className="text-action" type="button" onClick={changeEmail} disabled={busy}>
              Use a different email
            </Button>
          </div>
        </form>
      )}
    </Card>
  );
}
