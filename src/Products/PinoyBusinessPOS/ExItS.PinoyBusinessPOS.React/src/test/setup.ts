import "@testing-library/jest-dom/vitest";
import { cleanup } from "@testing-library/react";
import { afterEach, vi } from "vitest";

vi.mock("virtual:pwa-register", () => ({
  registerSW: () => async () => undefined,
}));

// Node 24 + jsdom: React Router may pass jsdom's AbortSignal into undici Request.
// Fall back to constructing Request without the incompatible signal (vitest#8374).
const NativeRequest = globalThis.Request;
globalThis.Request = class CompatibleRequest extends NativeRequest {
  constructor(input: RequestInfo | URL, init?: RequestInit) {
    try {
      super(input, init);
    } catch (error) {
      if (init?.signal) {
        const { signal, ...rest } = init;
        void signal;
        super(input, rest);
        return;
      }
      throw error;
    }
  }
} as typeof Request;

afterEach(async () => {
  cleanup();
  // Let React Router finish any aborted navigations before the next test stubs fetch.
  await Promise.resolve();
  vi.unstubAllGlobals();
  window.localStorage.clear();
  window.sessionStorage.clear();
  document.documentElement.dataset.theme = "system";
  document.documentElement.lang = "en";
});
