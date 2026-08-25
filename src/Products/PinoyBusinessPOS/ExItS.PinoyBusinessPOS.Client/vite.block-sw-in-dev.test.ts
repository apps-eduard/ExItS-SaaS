import { describe, expect, it } from "vitest";
import { isBlockedServiceWorkerDevPath } from "./vite.block-sw-in-dev";

describe("blockServiceWorkerScriptsInDev", () => {
  it("blocks service worker script paths that must not SPA-fallback in development", () => {
    expect(isBlockedServiceWorkerDevPath("/sw.js")).toBe(true);
    expect(isBlockedServiceWorkerDevPath("/sw.js.map")).toBe(true);
    expect(isBlockedServiceWorkerDevPath("/dev-sw.js")).toBe(true);
    expect(isBlockedServiceWorkerDevPath("/workbox-abc123.js")).toBe(true);
    expect(isBlockedServiceWorkerDevPath("/sign-in")).toBe(false);
    expect(isBlockedServiceWorkerDevPath("/platform-api/api/v1/platform/auth/me")).toBe(false);
  });
});
