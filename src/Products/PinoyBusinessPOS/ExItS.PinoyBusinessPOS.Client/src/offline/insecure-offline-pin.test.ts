import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
  isInsecureOfflinePinFallbackAllowed,
  shouldShowInsecureOfflinePinWarning,
} from "@/offline/insecure-offline-pin-gate";
import {
  clearAllInsecureOfflinePinDevState,
  enrollInsecureOfflinePinAndDek,
  isInsecureOfflinePinAndDekConfigured,
  isInsecureOfflineSessionUnlocked,
  verifyInsecureOfflinePin,
  INSECURE_OFFLINE_PIN_MODE,
  INSECURE_OFFLINE_PIN_STORE_KEY,
} from "@/offline/offline-pin-insecure-dev";
import {
  clearAllWrappedDekRecords,
  enrollOfflinePinAndDek,
  isOfflinePinAndDekConfigured,
  unlockOfflineCryptoWithPin,
  verifyOfflinePinForUnlock,
} from "@/offline/local-store-key";
import { clearAllOfflinePinVerifiers } from "@/offline/offline-pin";
import { clearUnlockedDek } from "@/offline/offline-unlock-session";

const USER = "11111111-1111-4111-8111-111111111111";
const PIN = "246810";

describe("insecure offline PIN gate", () => {
  afterEach(() => {
    vi.unstubAllEnvs();
    vi.unstubAllGlobals();
  });

  it("fails closed when VITE flag is not true", () => {
    vi.stubEnv("VITE_ALLOW_INSECURE_OFFLINE_PIN", "false");
    Object.defineProperty(window, "isSecureContext", { configurable: true, value: false });
    vi.stubGlobal("crypto", { getRandomValues: (b: Uint8Array) => b });
    expect(isInsecureOfflinePinFallbackAllowed()).toBe(false);
  });

  it("fails closed in production mode even if flag is true", () => {
    vi.stubEnv("DEV", false);
    vi.stubEnv("PROD", true);
    vi.stubEnv("MODE", "production");
    vi.stubEnv("VITE_ALLOW_INSECURE_OFFLINE_PIN", "true");
    Object.defineProperty(window, "isSecureContext", { configurable: true, value: false });
    vi.stubGlobal("crypto", { getRandomValues: (b: Uint8Array) => b });
    expect(isInsecureOfflinePinFallbackAllowed()).toBe(false);
    expect(shouldShowInsecureOfflinePinWarning()).toBe(false);
  });

  it("allows fallback in DEV with flag when secure context is false", () => {
    vi.stubEnv("DEV", true);
    vi.stubEnv("PROD", false);
    vi.stubEnv("MODE", "development");
    vi.stubEnv("VITE_ALLOW_INSECURE_OFFLINE_PIN", "true");
    Object.defineProperty(window, "isSecureContext", { configurable: true, value: false });
    vi.stubGlobal("crypto", { getRandomValues: (b: Uint8Array) => b });
    expect(isInsecureOfflinePinFallbackAllowed()).toBe(true);
    expect(shouldShowInsecureOfflinePinWarning()).toBe(true);
  });
});

describe("insecure offline PIN enroll/unlock", () => {
  beforeEach(() => {
    vi.stubEnv("DEV", true);
    vi.stubEnv("PROD", false);
    vi.stubEnv("MODE", "development");
    vi.stubEnv("VITE_ALLOW_INSECURE_OFFLINE_PIN", "true");
    Object.defineProperty(window, "isSecureContext", { configurable: true, value: false });
    vi.stubGlobal("crypto", { getRandomValues: (b: Uint8Array) => b });
    clearAllInsecureOfflinePinDevState();
    clearAllOfflinePinVerifiers();
    clearAllWrappedDekRecords();
    clearUnlockedDek();
  });

  afterEach(() => {
    clearAllInsecureOfflinePinDevState();
    clearAllOfflinePinVerifiers();
    clearAllWrappedDekRecords();
    clearUnlockedDek();
    vi.unstubAllEnvs();
    vi.unstubAllGlobals();
    Object.defineProperty(window, "isSecureContext", { configurable: true, value: true });
  });

  it("enrolls and unlocks with separate insecure store (not pretending to encrypt)", async () => {
    expect(await enrollOfflinePinAndDek(USER, PIN)).toBe(true);
    expect(isOfflinePinAndDekConfigured(USER)).toBe(true);
    expect(isInsecureOfflinePinAndDekConfigured(USER)).toBe(true);
    expect(isInsecureOfflineSessionUnlocked(USER)).toBe(true);

    const raw = window.localStorage.getItem(INSECURE_OFFLINE_PIN_STORE_KEY);
    expect(raw).toBeTruthy();
    expect(raw!).toContain(INSECURE_OFFLINE_PIN_MODE);
    expect(raw!).toContain(PIN);
    expect(window.localStorage.getItem("exits.pos-client.offline-pin-verifier.v1")).toBeNull();

    clearUnlockedDek();
    expect(isInsecureOfflineSessionUnlocked(USER)).toBe(false);

    const verify = await verifyOfflinePinForUnlock(USER, PIN);
    expect(verify.ok).toBe(true);
    expect(await unlockOfflineCryptoWithPin(USER, PIN)).toBe(true);
    expect(isInsecureOfflineSessionUnlocked(USER)).toBe(true);

    expect(verifyInsecureOfflinePin(USER, "000000").ok).toBe(false);
  });

  it("refuses insecure enroll API when gate is closed", () => {
    vi.stubEnv("VITE_ALLOW_INSECURE_OFFLINE_PIN", "false");
    expect(() => enrollInsecureOfflinePinAndDek(USER, PIN)).toThrow(/not allowed/i);
  });
});

describe("secure offline PIN path unchanged when subtle is available", () => {
  beforeEach(() => {
    vi.stubEnv("DEV", true);
    vi.stubEnv("PROD", false);
    vi.stubEnv("MODE", "development");
    vi.stubEnv("VITE_ALLOW_INSECURE_OFFLINE_PIN", "true");
    Object.defineProperty(window, "isSecureContext", { configurable: true, value: true });
    clearAllInsecureOfflinePinDevState();
    clearAllOfflinePinVerifiers();
    clearAllWrappedDekRecords();
    clearUnlockedDek();
  });

  afterEach(() => {
    clearAllInsecureOfflinePinDevState();
    clearAllOfflinePinVerifiers();
    clearAllWrappedDekRecords();
    clearUnlockedDek();
    vi.unstubAllEnvs();
  });

  it("uses secure enroll when secure context + subtle are available", async () => {
    expect(crypto.subtle).toBeDefined();
    expect(isInsecureOfflinePinFallbackAllowed()).toBe(false);

    expect(await enrollOfflinePinAndDek(USER, PIN)).toBe(true);
    expect(isOfflinePinAndDekConfigured(USER)).toBe(true);
    expect(window.localStorage.getItem(INSECURE_OFFLINE_PIN_STORE_KEY)).toBeNull();
    expect(window.localStorage.getItem("exits.pos-client.offline-pin-verifier.v1")).toBeTruthy();
    expect(await unlockOfflineCryptoWithPin(USER, PIN)).toBe(true);
  });
});
