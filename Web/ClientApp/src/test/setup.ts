import '@testing-library/jest-dom/vitest';
import { cleanup } from '@testing-library/react';
import { afterEach, vi } from 'vitest';

Object.defineProperty(window, 'matchMedia', { writable: true, value: vi.fn((query: string) => ({ matches: false, media: query, addEventListener: vi.fn(), removeEventListener: vi.fn() })) });
// Radix observes the size of form controls, which jsdom does not lay out.
Object.defineProperty(window, 'ResizeObserver', {
  configurable: true,
  value: class {
    observe() {}
    unobserve() {}
    disconnect() {}
  },
});
window.scrollTo = vi.fn();

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
  window.location.hash = '';
  delete document.documentElement.dataset.theme;
  localStorage.clear();
  sessionStorage.clear();
});
