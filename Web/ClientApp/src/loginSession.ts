import { useEffect, useState } from 'react';

export const LOGIN_SESSION_KEY = 'vsa.email-login.session';
export const LOGIN_CHALLENGE_KEY = 'vsa.email-login.challenge';
export const LOGIN_CHANNEL_NAME = 'vsa.email-login.tabs';

export interface LoginChallenge {
  emailAddress: string;
  expiresAt: string;
  resendAfter: number;
}

export interface LoginSessionState {
  sessionId: string | null;
  ready: boolean;
  error: string | null;
  notice: string | null;
}

interface TabMessage {
  type: 'probe' | 'occupied';
  sessionId: string;
  instanceId?: string;
  forInstanceId?: string;
}

export function readLoginChallenge(): LoginChallenge | null {
  try {
    const stored = sessionStorage.getItem(LOGIN_CHALLENGE_KEY);

    if (!stored) return null;

    const challenge: unknown = JSON.parse(stored);

    if (challenge && typeof challenge === 'object'
      && 'emailAddress' in challenge && typeof challenge.emailAddress === 'string'
      && 'expiresAt' in challenge && typeof challenge.expiresAt === 'string'
      && Number.isFinite(Date.parse(challenge.expiresAt))
      && 'resendAfter' in challenge && typeof challenge.resendAfter === 'number') {
      return challenge as LoginChallenge;
    }

    sessionStorage.removeItem(LOGIN_CHALLENGE_KEY);
  } catch {
    // The form can still explain unavailable storage when the user tries to log in.
  }

  return null;
}

export function saveLoginChallenge(challenge: LoginChallenge): void {
  try {
    sessionStorage.setItem(LOGIN_CHALLENGE_KEY, JSON.stringify(challenge));
  } catch {
    // The session ID is already stored; only restoring this form after a reload is optional.
  }
}

export function clearLoginChallenge(): void {
  try {
    sessionStorage.removeItem(LOGIN_CHALLENGE_KEY);
  } catch {
    // There is nothing to clear when session storage is unavailable.
  }
}

function isTabMessage(data: unknown): data is TabMessage {
  return data !== null && typeof data === 'object'
    && 'type' in data && (data.type === 'probe' || data.type === 'occupied')
    && 'sessionId' in data && typeof data.sessionId === 'string';
}

export function useLoginSession(): LoginSessionState {
  const [state, setState] = useState<LoginSessionState>({
    sessionId: null,
    ready: false,
    error: null,
    notice: null,
  });

  useEffect(() => {
    let sessionId: string;

    try {
      sessionId = sessionStorage.getItem(LOGIN_SESSION_KEY) || crypto.randomUUID();
      sessionStorage.setItem(LOGIN_SESSION_KEY, sessionId);
    } catch {
      setState({
        sessionId: null,
        ready: false,
        error: 'This browser could not keep the login in this tab. Enable session storage and try again.',
        notice: null,
      });

      return;
    }

    const readyState = {
      sessionId,
      ready: true,
      error: null,
      notice: null,
    };

    if (typeof BroadcastChannel === 'undefined') {
      setState(readyState);

      return;
    }

    let channel: BroadcastChannel;

    try {
      channel = new BroadcastChannel(LOGIN_CHANNEL_NAME);
    } catch {
      setState(readyState);

      return;
    }

    const instanceId = crypto.randomUUID();
    let active = false;
    let disposed = false;

    setState({ ...readyState, ready: false });

    channel.onmessage = (event: MessageEvent<unknown>) => {
      const message = event.data;

      if (!isTabMessage(message) || message.sessionId !== sessionId) return;

      if (message.type === 'probe' && active && typeof message.instanceId === 'string' && message.instanceId !== instanceId) {
        channel.postMessage({
          type: 'occupied',
          sessionId,
          forInstanceId: message.instanceId,
        } satisfies TabMessage);
      } else if (message.type === 'occupied' && message.forInstanceId === instanceId) {
        // Browsers can copy session storage when a tab is duplicated. Give that new tab
        // its own ID so it cannot redeem the challenge still open in the original tab.
        try {
          sessionId = crypto.randomUUID();
          sessionStorage.setItem(LOGIN_SESSION_KEY, sessionId);
          clearLoginChallenge();
          active = true;

          setState({
            ...readyState,
            sessionId,
            notice: 'This is a new tab. Request a login code here to continue.',
          });
        } catch {
          setState({
            sessionId: null,
            ready: false,
            error: 'This browser could not keep the login in this tab. Enable session storage and try again.',
            notice: null,
          });
        }
      }
    };

    channel.postMessage({ type: 'probe', sessionId, instanceId } satisfies TabMessage);

    const timer = window.setTimeout(() => {
      if (disposed) return;

      active = true;
      setState((current) => ({ ...current, ready: current.error === null }));
    }, 150);

    return () => {
      disposed = true;
      window.clearTimeout(timer);
      channel.close();
    };
  }, []);

  return state;
}
