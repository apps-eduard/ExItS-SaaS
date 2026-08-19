import { afterEach, describe, expect, it, vi } from "vitest";
import { login, getLocalValidationEnabled } from "@/api/auth/auth-client";
import { sampleSession } from "@/test/auth-fixtures";

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
});
