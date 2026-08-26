import { afterEach, describe, expect, it, vi } from "vitest";
import { getPlatformCredentialStatus } from "@/api/platform/platform-credentials-client";
import { clearPlatformAntiforgeryToken } from "@/api/platform/platform-http";

describe("platform-credentials-client", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    clearPlatformAntiforgeryToken();
  });

  it("reads the signed-in credential status from the self-service auth route", async () => {
    const fetchMock = vi.fn(async () => ({
      ok: true,
      status: 200,
      json: async () => ({
        UserId: "33333333-3333-4333-8333-333333333333",
        HasPassword: true,
        EmailVerified: false,
        IsLockedOut: false,
      }),
    }));
    vi.stubGlobal("fetch", fetchMock);

    const result = await getPlatformCredentialStatus();
    expect(result).toEqual({
      ok: true,
      value: { hasPassword: true, emailVerified: false, isLockedOut: false },
    });
    const calledUrl = fetchMock.mock.calls[0] as unknown as [string] | undefined;
    expect(String(calledUrl?.[0] ?? "")).toBe("/platform-api/api/v1/platform/auth/credentials");
  });

  it("reports hasPassword false rather than assuming a password exists", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async () => ({
        ok: true,
        status: 200,
        json: async () => ({ hasPassword: false }),
      })),
    );

    const result = await getPlatformCredentialStatus();
    expect(result.ok && result.value.hasPassword).toBe(false);
  });

  it("surfaces transport failures instead of guessing", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async () => ({
        ok: false,
        status: 503,
        json: async () => ({ errorCode: "application.unavailable" }),
      })),
    );

    expect(await getPlatformCredentialStatus()).toMatchObject({ ok: false, status: 503 });
  });
});
