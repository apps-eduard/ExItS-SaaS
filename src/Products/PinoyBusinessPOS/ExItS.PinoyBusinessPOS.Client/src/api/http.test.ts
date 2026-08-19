import { afterEach, describe, expect, it, vi } from "vitest";
import { ApiClientError, createCorrelationId, platformRequest, posRequest } from "@/api/http";

describe("api http foundation", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("sends a correlation id and does not attach auth tokens", async () => {
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({ ok: true }),
    });
    vi.stubGlobal("fetch", fetchMock);

    await platformRequest<{ ok: boolean }>({ path: "/health" });

    const [, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    const headers = new Headers(init.headers);
    expect(headers.get("X-Correlation-Id")).toMatch(
      /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i,
    );
    expect(headers.get("Authorization")).toBeNull();
    expect(init.credentials).toBe("include");
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

    const error = await posRequest({ path: "/health" }).catch((caught: unknown) => caught);
    expect(error).toBeInstanceOf(ApiClientError);
    const apiError = error as ApiClientError;
    expect(apiError.status).toBe(401);
    expect(apiError.errorCode).toBe("auth.session_invalid");
    expect(apiError.source).toBe("pos");
  });

  it("forwards AbortSignal", async () => {
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      status: 204,
    });
    vi.stubGlobal("fetch", fetchMock);
    const controller = new AbortController();
    await platformRequest({ path: "/health", signal: controller.signal });
    const [, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(init.signal).toBe(controller.signal);
  });

  it("creates a uuid correlation id", () => {
    expect(createCorrelationId()).toMatch(
      /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i,
    );
  });
});
