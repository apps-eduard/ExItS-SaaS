import { afterEach, describe, expect, it, vi } from "vitest";
import {
  assertBrowserStorageHasNoSessionToken,
  PLATFORM_API_BASE_PATH,
  platformApiUrl,
  toBrowserSessionSnapshot,
} from "@/api/platform-auth/browser-session";

describe("browser session transport", () => {
  afterEach(() => {
    window.localStorage.clear();
    window.sessionStorage.clear();
    vi.unstubAllGlobals();
  });

  it("uses a relative /platform-api base", () => {
    expect(PLATFORM_API_BASE_PATH).toBe("/platform-api");
    expect(platformApiUrl("/api/v1/platform/auth/me")).toBe(
      "/platform-api/api/v1/platform/auth/me",
    );
  });

  it("rejects absolute Platform API origins", () => {
    expect(() => platformApiUrl("http://localhost:8091/api/v1/platform/auth/me")).toThrow(
      /relative \/platform-api/,
    );
    expect(() => platformApiUrl("http://127.0.0.1:8091/api/v1/platform/auth/login")).toThrow();
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

  it("does not persist SessionToken in web storage", () => {
    const snapshot = toBrowserSessionSnapshot({
      sessionId: "s1",
      sessionToken: "must-not-be-stored",
    });
    window.localStorage.setItem("exits.plm-client.ui-preferences.v1", JSON.stringify(snapshot));
    window.sessionStorage.setItem("exits.plm-client.ui-preferences.v1", JSON.stringify(snapshot));
    expect(() => assertBrowserStorageHasNoSessionToken(window.localStorage)).not.toThrow();
    expect(() => assertBrowserStorageHasNoSessionToken(window.sessionStorage)).not.toThrow();
    expect(window.localStorage.getItem("exits.plm-client.ui-preferences.v1")).not.toMatch(
      /sessionToken/i,
    );
  });
});
