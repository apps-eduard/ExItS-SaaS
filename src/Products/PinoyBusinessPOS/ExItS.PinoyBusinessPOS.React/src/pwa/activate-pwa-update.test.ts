import { describe, expect, it, vi } from "vitest";
import { activatePwaUpdate } from "@/pwa/activate-pwa-update";

describe("activatePwaUpdate", () => {
  it("asks the waiting copy to take over and does not clear it once it does", async () => {
    const postMessage = vi.fn();
    let onChange: (() => void) | undefined;
    const clearTimeout = vi.fn();
    const unregister = vi.fn();
    const target = {
      setTimeout: (callback: () => void) => {
        void callback;
        return 1;
      },
      clearTimeout,
      navigator: {
        serviceWorker: {
          getRegistration: vi.fn().mockResolvedValue({
            waiting: { postMessage },
            installing: null,
          }),
          addEventListener: (_name: string, listener: () => void) => {
            onChange = listener;
          },
          getRegistrations: vi.fn().mockResolvedValue([{ unregister }]),
        },
      },
      caches: {
        keys: vi.fn().mockResolvedValue(["workbox-precache"]),
        delete: vi.fn(),
      },
    } as unknown as Window;

    const pending = activatePwaUpdate(target);
    await vi.waitFor(() => expect(postMessage).toHaveBeenCalledWith({ type: "SKIP_WAITING" }));
    onChange?.();
    await pending;

    expect(unregister).not.toHaveBeenCalled();
    expect(clearTimeout).toHaveBeenCalled();
  });

  it("removes the installed copy when it never takes over", async () => {
    const unregister = vi.fn().mockResolvedValue(true);
    const cacheDelete = vi.fn().mockResolvedValue(true);
    const target = {
      setTimeout: (callback: () => void) => {
        callback();
        return 1;
      },
      clearTimeout: vi.fn(),
      navigator: {
        serviceWorker: {
          getRegistration: vi.fn().mockResolvedValue({
            waiting: { postMessage: vi.fn() },
            installing: null,
          }),
          addEventListener: vi.fn(),
          getRegistrations: vi.fn().mockResolvedValue([{ unregister }]),
        },
      },
      caches: {
        keys: vi.fn().mockResolvedValue(["workbox-precache"]),
        delete: cacheDelete,
      },
    } as unknown as Window;

    await activatePwaUpdate(target);

    expect(unregister).toHaveBeenCalledTimes(1);
    expect(cacheDelete).toHaveBeenCalledWith("workbox-precache");
  });
});
