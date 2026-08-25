import { describe, expect, it, beforeEach, afterEach, vi } from "vitest";
import {
  INSTALLATION_DEVICE_ID_STORAGE_KEY,
  clearDurableInstallationDeviceIdOnLogout,
  getDurableInstallationDeviceId,
  isValidInstallationDeviceId,
  peekDurableInstallationDeviceId,
} from "@/workspace/browser-installation-identity";

describe("browser-installation-identity", () => {
  beforeEach(() => {
    localStorage.clear();
  });

  afterEach(() => {
    localStorage.clear();
    vi.unstubAllGlobals();
  });

  it("creates and persists a UUID on first call", () => {
    const first = getDurableInstallationDeviceId();
    expect(first.ok).toBe(true);
    if (!first.ok) {
      return;
    }
    expect(first.created).toBe(true);
    expect(isValidInstallationDeviceId(first.installationDeviceId)).toBe(true);
    expect(localStorage.getItem(INSTALLATION_DEVICE_ID_STORAGE_KEY)).toBe(
      first.installationDeviceId,
    );
  });

  it("returns the same id on subsequent calls", () => {
    const first = getDurableInstallationDeviceId();
    const second = getDurableInstallationDeviceId();
    expect(first.ok && second.ok).toBe(true);
    if (!first.ok || !second.ok) {
      return;
    }
    expect(second.created).toBe(false);
    expect(second.installationDeviceId).toBe(first.installationDeviceId);
  });

  it("survives logout clear (no-op) and user/org switch simulation", () => {
    const first = getDurableInstallationDeviceId();
    expect(first.ok).toBe(true);
    if (!first.ok) {
      return;
    }
    clearDurableInstallationDeviceIdOnLogout();
    // Simulate session teardown that must not touch installation id.
    sessionStorage.clear();
    const after = getDurableInstallationDeviceId();
    expect(after.ok).toBe(true);
    if (!after.ok) {
      return;
    }
    expect(after.installationDeviceId).toBe(first.installationDeviceId);
    expect(after.created).toBe(false);
  });

  it("replaces malformed stored values before register", () => {
    localStorage.setItem(INSTALLATION_DEVICE_ID_STORAGE_KEY, "not-a-uuid");
    const result = getDurableInstallationDeviceId();
    expect(result.ok).toBe(true);
    if (!result.ok) {
      return;
    }
    expect(result.created).toBe(true);
    expect(isValidInstallationDeviceId(result.installationDeviceId)).toBe(true);
    expect(localStorage.getItem(INSTALLATION_DEVICE_ID_STORAGE_KEY)).toBe(
      result.installationDeviceId,
    );
  });

  it("fail-closes when localStorage is unavailable (no ephemeral id)", () => {
    vi.stubGlobal("localStorage", {
      getItem: () => {
        throw new Error("blocked");
      },
      setItem: () => {
        throw new Error("blocked");
      },
      removeItem: () => {
        throw new Error("blocked");
      },
      clear: () => undefined,
      key: () => null,
      length: 0,
    } as Storage);

    const result = getDurableInstallationDeviceId();
    expect(result.ok).toBe(false);
    if (result.ok) {
      return;
    }
    expect(result.reason).toBe("storage_unavailable");
    expect(peekDurableInstallationDeviceId()).toBeNull();
  });

  it("fail-closes when crypto.randomUUID is unavailable", () => {
    vi.stubGlobal("crypto", {});
    const result = getDurableInstallationDeviceId();
    expect(result.ok).toBe(false);
    if (result.ok) {
      return;
    }
    expect(result.reason).toBe("crypto_unavailable");
  });

  it("peek returns null when missing and does not create", () => {
    expect(peekDurableInstallationDeviceId()).toBeNull();
    expect(localStorage.getItem(INSTALLATION_DEVICE_ID_STORAGE_KEY)).toBeNull();
  });

  it("rejects invalid UUID formats", () => {
    expect(isValidInstallationDeviceId("")).toBe(false);
    expect(isValidInstallationDeviceId("abc")).toBe(false);
    expect(isValidInstallationDeviceId("00000000-0000-0000-0000-000000000000")).toBe(false);
  });
});
