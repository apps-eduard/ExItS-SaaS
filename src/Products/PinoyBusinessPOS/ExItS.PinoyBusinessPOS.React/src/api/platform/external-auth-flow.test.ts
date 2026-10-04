import { describe, expect, it } from "vitest";
import {
  buildExternalAuthChallengeUrl,
  isSafeExternalReturnPath,
  PERSONAL_EXTERNAL_AUTH_RETURN_PATH,
  stripSessionTokenFromLocation,
} from "@/api/platform/external-auth-flow";

describe("external auth flow", () => {
  it("builds a same-origin Google challenge with a relative personal return path", () => {
    const url = buildExternalAuthChallengeUrl("google", PERSONAL_EXTERNAL_AUTH_RETURN_PATH);
    expect(url).toBe(
      "/platform-api/api/v1/platform/auth/external/google/challenge?returnUrl=" +
        encodeURIComponent("/external-login-callback?target=personal"),
    );
    expect(url).not.toContain("http://");
    expect(url).not.toContain("https://");
  });

  it("rejects unsafe return paths and falls back to the personal callback", () => {
    expect(isSafeExternalReturnPath("//evil.example")).toBe(false);
    expect(isSafeExternalReturnPath("/\\evil.example")).toBe(false);
    expect(isSafeExternalReturnPath("javascript:alert(1)")).toBe(false);
    expect(isSafeExternalReturnPath("data:text/html,hi")).toBe(false);
    expect(isSafeExternalReturnPath(PERSONAL_EXTERNAL_AUTH_RETURN_PATH)).toBe(true);
    expect(buildExternalAuthChallengeUrl("google", "//evil.example")).toContain(
      encodeURIComponent(PERSONAL_EXTERNAL_AUTH_RETURN_PATH),
    );
  });

  it("strips sessionToken from the callback URL and does not store it", () => {
    window.localStorage.clear();
    window.sessionStorage.clear();
    const stripped = stripSessionTokenFromLocation(
      "/external-login-callback",
      "?target=personal&sessionToken=must-not-remain",
    );
    expect(stripped.sessionToken).toBe("must-not-remain");
    expect(stripped.nextUrl).toBe("/external-login-callback?target=personal");
    expect(stripped.nextUrl).not.toContain("sessionToken");
    expect(window.localStorage.getItem("sessionToken")).toBeNull();
    expect(window.sessionStorage.getItem("sessionToken")).toBeNull();
    expect(JSON.stringify(window.localStorage)).not.toContain("must-not-remain");
    expect(JSON.stringify(window.sessionStorage)).not.toContain("must-not-remain");
  });
});
