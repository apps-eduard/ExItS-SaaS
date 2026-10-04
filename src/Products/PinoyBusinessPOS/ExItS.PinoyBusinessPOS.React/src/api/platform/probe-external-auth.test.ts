import { afterEach, describe, expect, it, vi } from "vitest";
import { probeExternalAuthProvider } from "@/api/platform/platform-auth-client";

describe("probeExternalAuthProvider", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    Object.defineProperty(window.navigator, "onLine", { configurable: true, value: true });
  });

  it("treats an opaque Google redirect as available", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async () => ({ status: 0, type: "opaqueredirect" }) as Response),
    );
    await expect(probeExternalAuthProvider("google")).resolves.toBe("available");
  });

  it("treats a missing provider as disabled", async () => {
    vi.stubGlobal("fetch", vi.fn(async () => ({ status: 404, type: "basic" }) as Response));
    await expect(probeExternalAuthProvider("google")).resolves.toBe("disabled");
  });

  it("does not probe while offline", async () => {
    const fetchMock = vi.fn();
    vi.stubGlobal("fetch", fetchMock);
    Object.defineProperty(window.navigator, "onLine", { configurable: true, value: false });
    await expect(probeExternalAuthProvider("google")).resolves.toBe("offline");
    expect(fetchMock).not.toHaveBeenCalled();
  });
});
