import { afterEach, describe, expect, it, vi } from "vitest";
import {
  PlatformApiError,
  clearPlatformAntiforgeryToken,
  createCorrelationId,
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

describe("createCorrelationId", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("falls back to getRandomValues when randomUUID is missing (Tailscale HTTP)", () => {
    const bytes = new Uint8Array(16);
    for (let i = 0; i < 16; i += 1) {
      bytes[i] = i;
    }
    vi.stubGlobal("crypto", {
      getRandomValues: (target: Uint8Array) => {
        target.set(bytes);
        return target;
      },
    });

    const correlationId = createCorrelationId();
    expect(correlationId).toMatch(
      /^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i,
    );
  });

  it("falls back when randomUUID throws outside a secure context", () => {
    vi.stubGlobal("crypto", {
      randomUUID: () => {
        throw new Error("Secure context required");
      },
      getRandomValues: (target: Uint8Array) => {
        for (let i = 0; i < target.length; i += 1) {
          target[i] = (i * 7) & 0xff;
        }
        return target;
      },
    });

    expect(createCorrelationId()).toMatch(
      /^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i,
    );
  });
});
