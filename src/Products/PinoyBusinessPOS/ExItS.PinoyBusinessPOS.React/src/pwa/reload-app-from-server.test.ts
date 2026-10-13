import { describe, expect, it, vi } from "vitest";
import { reloadAppFromServer } from "@/pwa/reload-app-from-server";

describe("reloadAppFromServer", () => {
  it("clears the installed app and reloads even when no update is waiting", async () => {
    const unregister = vi.fn().mockResolvedValue(true);
    const cacheDelete = vi.fn().mockResolvedValue(true);
    const reload = vi.fn();
    const target = {
      navigator: {
        serviceWorker: {
          getRegistrations: vi.fn().mockResolvedValue([{ unregister }]),
        },
      },
      caches: {
        keys: vi.fn().mockResolvedValue(["workbox-precache"]),
        delete: cacheDelete,
      },
      location: { reload },
    } as unknown as Window;

    await reloadAppFromServer(target);

    expect(unregister).toHaveBeenCalledTimes(1);
    expect(cacheDelete).toHaveBeenCalledWith("workbox-precache");
    expect(reload).toHaveBeenCalledTimes(1);
  });
});
