import { afterEach, describe, expect, it, vi } from "vitest";
import {
  activateAccount,
  getLocalValidationEnabled,
  login,
  registerPersonalAccount,
  requestPasswordReset,
  resetPassword,
} from "@/api/auth/auth-client";
import { sampleSession } from "@/test/auth-fixtures";

function jsonOk(body: unknown) {
  return {
    ok: true,
    status: 200,
    json: async () => body,
  };
}

describe("auth client", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("does not keep the login session token in the mapped session", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: true,
        status: 200,
        json: async () => ({ ...sampleSession, sessionToken: "opaque-session-token" }),
      }),
    );

    const session = await login("http://localhost:8091", {
      usernameOrEmail: "olivia@example.test",
      password: "secret-password",
    });

    expect(session).not.toHaveProperty("sessionToken");
    expect(JSON.stringify(session)).not.toContain("opaque-session-token");
  });

  it("treats Local Validation enabled as true only for JSON true", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: true,
        status: 200,
        json: async () => true,
      }),
    );

    await expect(getLocalValidationEnabled("http://localhost:8091")).resolves.toBe(true);

    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: true,
        status: 200,
        json: async () => ({ enabled: true }),
      }),
    );
    await expect(getLocalValidationEnabled("http://localhost:8091")).resolves.toBe(false);
  });

  it("serializes personal registration without a password or debug token", async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      jsonOk({
        message: "If the email is eligible, a verification message was sent.",
        debugToken: "must-not-surface",
        expiresAtUtc: "2026-08-23T00:00:00Z",
      }),
    );
    vi.stubGlobal("fetch", fetchMock);

    const ack = await registerPersonalAccount("", {
      displayName: "Ana Cruz",
      email: "ana@example.test",
    });

    expect(ack.message).toContain("If the email is eligible");
    expect(ack).not.toHaveProperty("debugToken");
    expect(JSON.stringify(ack)).not.toContain("must-not-surface");
    expect(fetchMock).toHaveBeenCalledTimes(1);
    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toBe("/api/v1/platform/auth/register");
    expect(init.method).toBe("POST");
    expect(JSON.parse(String(init.body))).toEqual({
      displayName: "Ana Cruz",
      email: "ana@example.test",
    });
    expect(JSON.parse(String(init.body))).not.toHaveProperty("password");
  });

  it("serializes account activation with token and password", async () => {
    const fetchMock = vi.fn().mockResolvedValue(jsonOk({ userId: "hidden" }));
    vi.stubGlobal("fetch", fetchMock);

    await activateAccount("", { token: "opaque-activation-token", password: "new-password" });

    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toBe("/api/v1/platform/auth/activate-account");
    expect(JSON.parse(String(init.body))).toEqual({
      token: "opaque-activation-token",
      password: "new-password",
    });
  });

  it("serializes forgot-password with usernameOrEmail and strips debug tokens", async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      jsonOk({
        message: "If an eligible account exists, a password reset token was issued.",
        debugToken: "must-not-surface",
      }),
    );
    vi.stubGlobal("fetch", fetchMock);

    const ack = await requestPasswordReset("", { usernameOrEmail: "unknown@example.test" });
    expect(ack.message).toContain("If an eligible account exists");
    expect(ack).not.toHaveProperty("debugToken");
    expect(JSON.stringify(ack)).not.toContain("must-not-surface");
    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toBe("/api/v1/platform/auth/forgot-password");
    expect(JSON.parse(String(init.body))).toEqual({ usernameOrEmail: "unknown@example.test" });
  });

  it("serializes password reset with token and newPassword", async () => {
    const fetchMock = vi.fn().mockResolvedValue(jsonOk({}));
    vi.stubGlobal("fetch", fetchMock);

    await resetPassword("", { token: "opaque-reset-token", newPassword: "replacement-password" });

    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toBe("/api/v1/platform/auth/reset-password");
    expect(JSON.parse(String(init.body))).toEqual({
      token: "opaque-reset-token",
      newPassword: "replacement-password",
    });
  });
});
