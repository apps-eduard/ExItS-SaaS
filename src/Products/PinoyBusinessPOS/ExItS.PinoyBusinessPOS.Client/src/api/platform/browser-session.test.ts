import { afterEach, describe, expect, it, vi } from "vitest";
import {
  assertBrowserStorageHasNoBearerToken,
  assertBrowserStorageHasNoSessionToken,
  PLATFORM_API_BASE_PATH,
  toBrowserSessionSnapshot,
} from "@/api/platform/browser-session";
import { assertRelativePlatformBase } from "@/api/platform/platform-http";

describe("browser session transport", () => {
  afterEach(() => {
    window.localStorage.clear();
    window.sessionStorage.clear();
    vi.unstubAllGlobals();
  });

  it("uses a relative /platform-api base", () => {
    expect(PLATFORM_API_BASE_PATH).toBe("/platform-api");
    expect(() => assertRelativePlatformBase("/platform-api")).not.toThrow();
    expect(() => assertRelativePlatformBase("http://127.0.0.1:8091")).toThrow(
      /relative \/platform-api/,
    );
  });

  it("strips SessionToken from the browser-facing snapshot", () => {
    const snapshot = toBrowserSessionSnapshot({
      sessionId: "11111111-1111-1111-1111-111111111111",
      username: "olivia",
      sessionToken: "reusable-session-token",
    });
    expect(snapshot).toEqual({
      sessionId: "11111111-1111-1111-1111-111111111111",
      username: "olivia",
    });
    expect(JSON.stringify(snapshot)).not.toMatch(/sessionToken/i);
    expect(Object.prototype.hasOwnProperty.call(snapshot, "sessionToken")).toBe(false);
  });

  it("does not persist SessionToken or Bearer in web storage", () => {
    const snapshot = toBrowserSessionSnapshot({
      sessionId: "s1",
      sessionToken: "must-not-be-stored",
    });
    window.localStorage.setItem("exits.pos-client.ui-preferences.v1", JSON.stringify(snapshot));
    window.sessionStorage.setItem("exits.pos-client.ui-preferences.v1", JSON.stringify(snapshot));
    expect(() => assertBrowserStorageHasNoSessionToken(window.localStorage)).not.toThrow();
    expect(() => assertBrowserStorageHasNoSessionToken(window.sessionStorage)).not.toThrow();
    expect(() => assertBrowserStorageHasNoBearerToken(window.localStorage)).not.toThrow();
    window.localStorage.setItem("probe", "Bearer abc");
    expect(() => assertBrowserStorageHasNoBearerToken(window.localStorage)).toThrow();
  });
});
