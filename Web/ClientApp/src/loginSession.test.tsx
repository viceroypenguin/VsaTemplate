import { renderHook, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { LOGIN_CHALLENGE_KEY, LOGIN_SESSION_KEY, useLoginSession } from './loginSession';

class TestBroadcastChannel {
  static channels: TestBroadcastChannel[] = [];
  onmessage: ((event: MessageEvent<unknown>) => void) | null = null;
  private listeners = new Set<(event: MessageEvent<unknown>) => void>();
  private closed = false;

  constructor(readonly name: string) {
    TestBroadcastChannel.channels.push(this);
  }

  postMessage(data: unknown) {
    for (const channel of TestBroadcastChannel.channels) {
      if (channel !== this && channel.name === this.name) {
        queueMicrotask(() => channel.receive(data));
      }
    }
  }

  addEventListener(_type: string, listener: (event: MessageEvent<unknown>) => void) {
    this.listeners.add(listener);
  }

  removeEventListener(_type: string, listener: (event: MessageEvent<unknown>) => void) {
    this.listeners.delete(listener);
  }

  close() {
    this.closed = true;
  }

  private receive(data: unknown) {
    if (this.closed) {
      return;
    }

    const event = new MessageEvent('message', { data });
    this.onmessage?.(event);

    for (const listener of this.listeners) {
      listener(event);
    }
  }
}

describe('login tab session', () => {
  beforeEach(() => {
    sessionStorage.clear();
    localStorage.clear();
    TestBroadcastChannel.channels = [];
  });

  it('stores a random tab session in session storage and preserves it across a reload', async () => {
    vi.stubGlobal('BroadcastChannel', undefined);
    const firstPage = renderHook(() => useLoginSession());

    await waitFor(() => expect(firstPage.result.current.ready).toBe(true));

    const sessionId = firstPage.result.current.sessionId;
    expect(sessionId).toMatch(/^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i);
    expect(sessionStorage.getItem(LOGIN_SESSION_KEY)).toBe(sessionId);
    expect(localStorage.getItem(LOGIN_SESSION_KEY)).toBeNull();
    firstPage.unmount();

    const reloadedPage = renderHook(() => useLoginSession());
    await waitFor(() => expect(reloadedPage.result.current.ready).toBe(true));
    expect(reloadedPage.result.current.sessionId).toBe(sessionId);
  });

  it('creates a fresh session after the previous tab session storage is gone', async () => {
    vi.stubGlobal('BroadcastChannel', undefined);
    const firstPage = renderHook(() => useLoginSession());
    await waitFor(() => expect(firstPage.result.current.ready).toBe(true));

    const previousId = firstPage.result.current.sessionId;
    firstPage.unmount();
    sessionStorage.clear();

    const newTab = renderHook(() => useLoginSession());
    await waitFor(() => expect(newTab.result.current.ready).toBe(true));
    expect(newTab.result.current.sessionId).not.toBe(previousId);
  });

  it('does not enable login when the browser cannot retain the tab session', async () => {
    vi.stubGlobal('BroadcastChannel', undefined);

    const write = vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new DOMException('Storage is unavailable.');
    });

    try {
      const tab = renderHook(() => useLoginSession());
      await waitFor(() => expect(tab.result.current.error).toMatch(/session storage/i));

      expect(tab.result.current.ready).toBe(false);
      expect(tab.result.current.sessionId).toBeNull();
    } finally {
      write.mockRestore();
    }
  });

  it('rotates a copied session in a duplicate tab while preserving the original tab session', async () => {
    vi.stubGlobal('BroadcastChannel', TestBroadcastChannel);
    const original = renderHook(() => useLoginSession());
    await waitFor(() => expect(original.result.current.ready).toBe(true));

    const originalId = original.result.current.sessionId;
    sessionStorage.setItem(LOGIN_CHALLENGE_KEY, JSON.stringify({
      emailAddress: 'person@example.test',
      expiresAt: new Date(Date.now() + 15 * 60_000).toISOString(),
      resendAfter: Date.now() + 60_000,
    }));

    const duplicate = renderHook(() => useLoginSession());
    await waitFor(() => expect(duplicate.result.current.ready).toBe(true));

    expect(duplicate.result.current.sessionId).not.toBe(originalId);
    expect(original.result.current.sessionId).toBe(originalId);
    expect(sessionStorage.getItem(LOGIN_CHALLENGE_KEY)).toBeNull();
    expect(duplicate.result.current.notice).toBeTruthy();
  });
});
