import { afterEach, describe, expect, it, vi } from "vitest";
import { isFrontendLocalValidationMode } from "@/api/platform-auth/local-validation-gate";
import {
  fetchLocalValidationIdentities,
  loginWithPassword,
  registerPersonalAccount,
} from "@/api/platform-auth/platform-auth-client";
import { jsonResponse } from "@/test/render";

describe("local validation gate", () => {
  it("allows development, test, and testing modes only", () => {
    expect(isFrontendLocalValidationMode("development")).toBe(true);
    expect(isFrontendLocalValidationMode("test")).toBe(true);
    expect(isFrontendLocalValidationMode("testing")).toBe(true);
    expect(isFrontendLocalValidationMode("production")).toBe(false);
  });
});

describe("platform auth client", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("posts the login contract and strips sessionToken", async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
      expect(String(input)).toBe("/platform-api/api/v1/platform/auth/login");
      expect(init?.credentials).toBe("include");
      expect(init?.method).toBe("POST");
      const body = JSON.parse(String(init?.body)) as { usernameOrEmail: string; password: string };
      expect(body).toEqual({ usernameOrEmail: "olivia", password: "secret" });
      return jsonResponse(200, {
        sessionId: "s1",
        username: "olivia",
        displayName: "Olivia Mendoza",
        sessionToken: "must-not-escape",
      });
    });
    vi.stubGlobal("fetch", fetchMock);
    const result = await loginWithPassword("olivia", "secret");
    expect(result.ok).toBe(true);
    if (result.ok) {
      expect(result.session).toEqual({
        sessionId: "s1",
        username: "olivia",
        displayName: "Olivia Mendoza",
      });
      expect(JSON.stringify(result.session)).not.toMatch(/sessionToken/i);
    }
  });

  it("does not request identities unless the frontend mode gate and enabled flag pass", async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL) => {
      const url = String(input);
      if (url.includes("/local-validation/enabled")) {
        return jsonResponse(200, false);
      }
      return jsonResponse(500, { error: "must-not-request-identities" });
    });
    vi.stubGlobal("fetch", fetchMock);
    await expect(fetchLocalValidationIdentities()).resolves.toEqual([]);
    expect(fetchMock.mock.calls.some((call) => String(call[0]).includes("quick-login"))).toBe(
      false,
    );
  });

  it("posts registration with the public surface identifier only", async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
      expect(String(input)).toBe("/platform-api/api/v1/platform/auth/register");
      expect(JSON.parse(String(init?.body))).toEqual({
        displayName: "Pat",
        email: "pat@example.com",
        publicSurface: "pinoy-loan-manager",
      });
      return jsonResponse(409, { errorCode: "application.auth.email_conflict" });
    });
    vi.stubGlobal("fetch", fetchMock);
    await expect(registerPersonalAccount("Pat", "pat@example.com")).resolves.toEqual({ ok: true });
  });
});
