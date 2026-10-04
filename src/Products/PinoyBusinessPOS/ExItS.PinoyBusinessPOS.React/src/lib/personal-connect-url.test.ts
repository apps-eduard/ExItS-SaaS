import { describe, expect, it } from "vitest";
import { buildExternalAuthChallengeUrl } from "@/api/platform/external-auth-flow";
import {
  continuationAfterActivation,
  isSafeAuthContinuePath,
  landingPathAfterExternalAuth,
  resolveAuthContinuePath,
} from "@/features/store/store-acquisition";
import {
  clearPersonalConnectIntent,
  peekPersonalConnectIntent,
  rememberPersonalConnectIntent,
} from "@/features/personal/social/personal-connect-intent";
import {
  buildCanonicalPersonalConnectUrl,
  buildPersonalConnectPath,
  buildPersonalExternalAuthReturnPath,
  PERSONAL_CONNECT_PUBLIC_ORIGIN,
} from "@/lib/personal-connect-url";

describe("personal connect URL", () => {
  it("builds the canonical public QR payload without secrets", () => {
    const url = buildCanonicalPersonalConnectUrl("ex-4827-1936");
    expect(PERSONAL_CONNECT_PUBLIC_ORIGIN).toBe("https://my.exitsapps.com");
    expect(url).toBe("https://my.exitsapps.com/connect/EX-4827-1936");
    expect(url).not.toMatch(/localhost|@|token|session/i);
    expect(url).not.toMatch(/[0-9a-f]{8}-[0-9a-f]{4}-/i);
    expect(buildPersonalConnectPath("EX-4827-1936")).toBe("/connect/EX-4827-1936");
  });

  it("accepts only a relative connect continue path", () => {
    expect(isSafeAuthContinuePath("/connect/EX-4827-1936")).toBe(true);
    expect(isSafeAuthContinuePath("https://evil.example/connect/EX-4827-1936")).toBe(false);
    expect(isSafeAuthContinuePath("//evil.example/connect/EX-4827-1936")).toBe(false);
    expect(isSafeAuthContinuePath("/connect/EX-4827-1936?token=abc")).toBe(false);
    expect(isSafeAuthContinuePath("/\\connect/EX-4827-1936")).toBe(false);
    expect(isSafeAuthContinuePath("javascript:alert(1)")).toBe(false);
  });

  it("keeps a Google return on the personal callback and resumes the connect route", () => {
    const withContinue = buildPersonalExternalAuthReturnPath("/connect/EX-4827-1936");
    expect(withContinue).toBe(
      "/external-login-callback?target=personal&continue=" +
        encodeURIComponent("/connect/EX-4827-1936"),
    );
    expect(buildExternalAuthChallengeUrl("google", withContinue)).toContain(
      encodeURIComponent(withContinue),
    );
    expect(buildPersonalExternalAuthReturnPath(null)).toBe(
      "/external-login-callback?target=personal",
    );
    expect(landingPathAfterExternalAuth("?target=personal")).toBe("/personal");
    expect(
      landingPathAfterExternalAuth(
        "?target=personal&continue=" +
          encodeURIComponent("/connect/EX-4827-1936") +
          "&sessionToken=secret",
      ),
    ).toBe("/connect/EX-4827-1936");
    expect(landingPathAfterExternalAuth("?continue=https://evil.example")).toBe("/personal");
  });

  it("keeps a connect intent of only the public ID across storage", () => {
    window.localStorage.clear();
    window.sessionStorage.clear();
    rememberPersonalConnectIntent("EX-4827-1936");
    window.sessionStorage.clear();
    const intent = peekPersonalConnectIntent();
    expect(intent).toEqual({
      publicUserId: "EX-4827-1936",
      continuePath: "/connect/EX-4827-1936",
    });
    expect(window.localStorage.getItem("exits.personal.connectIntent")).not.toMatch(
      /@|token|session/i,
    );
    expect(continuationAfterActivation()).toBe("/connect/EX-4827-1936");
    clearPersonalConnectIntent();
    expect(peekPersonalConnectIntent()).toBeNull();
  });

  it("deletes expired and malformed intents and does not hijack a login without continue", () => {
    window.localStorage.clear();
    window.sessionStorage.clear();
    window.localStorage.setItem(
      "exits.personal.connectIntent",
      JSON.stringify({
        publicUserId: "EX-4827-1936",
        continuePath: "/connect/EX-4827-1936",
        savedAtUtc: Date.now() - 25 * 60 * 60 * 1000,
      }),
    );
    expect(peekPersonalConnectIntent()).toBeNull();
    expect(window.localStorage.getItem("exits.personal.connectIntent")).toBeNull();

    window.localStorage.setItem(
      "exits.personal.connectIntent",
      JSON.stringify({
        publicUserId: "not-an-id",
        continuePath: "/personal",
        email: "ada@example.com",
        savedAtUtc: Date.now(),
      }),
    );
    expect(peekPersonalConnectIntent()).toBeNull();
    expect(window.localStorage.getItem("exits.personal.connectIntent")).toBeNull();

    rememberPersonalConnectIntent("EX-4827-1936");
    expect(resolveAuthContinuePath(null)).toBeNull();
    expect(landingPathAfterExternalAuth("?target=personal")).toBe("/personal");
    clearPersonalConnectIntent();
  });
});
