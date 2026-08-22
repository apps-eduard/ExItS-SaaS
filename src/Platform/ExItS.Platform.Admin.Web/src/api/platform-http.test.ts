import { afterEach, describe, expect, it, vi } from "vitest";
import {
  PlatformApiError,
  clearPlatformAntiforgeryToken,
  platformRequest,
} from "@/api/platform-http";

describe("platformRequest", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    clearPlatformAntiforgeryToken();
  });

  it("sends credentials and a correlation id", async () => {
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      text: async () => JSON.stringify({ ok: true }),
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
          traceId: "00-server-trace",
        }),
      }),
    );

    const error = await platformRequest("http://localhost:8091", {
      path: "/api/v1/platform/auth/me",
    }).catch((caught: unknown) => caught);

    expect(error).toBeInstanceOf(PlatformApiError);
    const apiError = error as PlatformApiError;
    expect(apiError.status).toBe(401);
    expect(apiError.errorCode).toBe("auth.session_invalid");
    expect(apiError.traceId).toBe("00-server-trace");
    expect(apiError.requestCorrelationId).toMatch(
      /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i,
    );
  });

  it("reads errorCode from ProblemDetails extensions", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: false,
        status: 401,
        json: async () => ({
          title: "Unauthorized",
          status: 401,
          extensions: { errorCode: "application.auth.login_failed" },
        }),
      }),
    );

    const error = await platformRequest("http://localhost:8091", {
      path: "/api/v1/platform/auth/login",
    }).catch((caught: unknown) => caught);

    expect((error as PlatformApiError).errorCode).toBe("application.auth.login_failed");
  });
});
