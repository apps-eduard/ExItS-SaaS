import { afterEach, describe, expect, it, vi } from "vitest";
import { PlatformApiError, platformRequest } from "@/api/platform-http";

describe("platformRequest", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("sends credentials and a correlation id", async () => {
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({ ok: true }),
    });
    vi.stubGlobal("fetch", fetchMock);

    await platformRequest<{ ok: boolean }>("http://localhost:8091", {
      path: "/api/v1/platform/auth/me",
    });

    expect(fetchMock).toHaveBeenCalledOnce();
    const [, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(init.credentials).toBe("include");
    const headers = new Headers(init.headers);
    expect(headers.get("X-Correlation-Id")).toBeTruthy();
  });

  it("normalizes problem+json failures", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: false,
        status: 401,
        json: async () => ({
          title: "Unauthorized",
          status: 401,
          detail: "Session is not valid.",
          errorCode: "auth.session_invalid",
        }),
      }),
    );

    await expect(
      platformRequest("http://localhost:8091", { path: "/api/v1/platform/auth/me" }),
    ).rejects.toBeInstanceOf(PlatformApiError);
  });
});
