import { afterEach, describe, expect, it, vi } from "vitest";
import {
  DEV_SW_CLEARED_SESSION_KEY,
  recoverDevelopmentOriginFromStaleServiceWorker,
} from "@/pwa/dev-service-worker-guard";

describe("recoverDevelopmentOriginFromStaleServiceWorker", () => {
  afterEach(() => {
    window.sessionStorage.clear();
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it("unregisters leftover service workers and reloads once in development", async () => {
    const unregister = vi.fn().mockResolvedValue(true);
    const getRegistrations = vi.fn().mockResolvedValue([{ unregister }]);
    const cachesDelete = vi.fn().mockResolvedValue(true);
    const reload = vi.fn();

    Object.defineProperty(window, "location", {
      configurable: true,
      value: { reload },
    });
    vi.stubGlobal("navigator", {
      serviceWorker: { getRegistrations },
    });
    vi.stubGlobal("caches", {
      keys: vi.fn().mockResolvedValue(["workbox-precache-v1"]),
      delete: cachesDelete,
    });

    const result = await recoverDevelopmentOriginFromStaleServiceWorker();

    expect(unregister).toHaveBeenCalledTimes(1);
    expect(cachesDelete).toHaveBeenCalledWith("workbox-precache-v1");
    expect(window.sessionStorage.getItem(DEV_SW_CLEARED_SESSION_KEY)).toBe("1");
    expect(reload).toHaveBeenCalledTimes(1);
    expect(result).toEqual({
      recovered: true,
      willReload: true,
      unregistered: 1,
      cachesCleared: 1,
    });
  });

  it("does not reload twice in the same tab session", async () => {
    window.sessionStorage.setItem(DEV_SW_CLEARED_SESSION_KEY, "1");
    const unregister = vi.fn().mockResolvedValue(true);
    const reload = vi.fn();
    Object.defineProperty(window, "location", {
      configurable: true,
      value: { reload },
    });
    vi.stubGlobal("navigator", {
      serviceWorker: {
        getRegistrations: vi.fn().mockResolvedValue([{ unregister }]),
      },
    });
    vi.stubGlobal("caches", {
      keys: vi.fn().mockResolvedValue([]),
      delete: vi.fn(),
    });

    const result = await recoverDevelopmentOriginFromStaleServiceWorker();
    expect(reload).not.toHaveBeenCalled();
    expect(result.willReload).toBe(false);
    expect(result.recovered).toBe(true);
  });
});
