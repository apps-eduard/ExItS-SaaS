import { afterEach, describe, expect, it, vi } from "vitest";
import { createCorrelationId } from "@/lib/create-correlation-id";
import {
  getLocalValidationEnabled,
  listQuickLoginIdentities,
} from "@/api/auth/auth-client";

const uuidPattern =
  /^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

describe("createCorrelationId", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("uses crypto.randomUUID when available", () => {
    const randomUUID = vi.fn(() => "11111111-2222-4333-8444-555555555555");
    vi.stubGlobal("crypto", { randomUUID, getRandomValues: vi.fn() });

    expect(createCorrelationId()).toBe("11111111-2222-4333-8444-555555555555");
    expect(randomUUID).toHaveBeenCalledOnce();
  });

  it("falls back to getRandomValues when randomUUID is unavailable", () => {
    const getRandomValues = vi.fn((buffer: Uint8Array) => {
      for (let i = 0; i < buffer.length; i += 1) {
        buffer[i] = i;
      }
      return buffer;
    });
    vi.stubGlobal("crypto", { getRandomValues });

    const id = createCorrelationId();
    expect(getRandomValues).toHaveBeenCalledOnce();
    expect(id).toMatch(uuidPattern);
    expect(id).not.toContain("undefined");
  });

  it("does not throw in a non-secure-context style environment without randomUUID", () => {
    vi.stubGlobal("crypto", {
      getRandomValues: (buffer: Uint8Array) => {
        buffer.fill(7);
        return buffer;
      },
    });

    expect(() => createCorrelationId()).not.toThrow();
    expect(createCorrelationId()).toMatch(uuidPattern);
  });

  it("uses a diagnostics-only fallback when Web Crypto is unavailable", () => {
    vi.stubGlobal("crypto", undefined);

    const id = createCorrelationId();
    expect(id.startsWith("corr-")).toBe(true);
    expect(id.length).toBeGreaterThan(10);
  });
});

describe("local-validation helpers without crypto.randomUUID", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  function stubNonSecureCrypto() {
    vi.stubGlobal("crypto", {
      getRandomValues: (buffer: Uint8Array) => {
        for (let i = 0; i < buffer.length; i += 1) {
          buffer[i] = (i * 17) & 0xff;
        }
        return buffer;
      },
    });
  }

  it("getLocalValidationEnabled reaches relative /api enabled path", async () => {
    stubNonSecureCrypto();
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      text: async () => "true",
      json: async () => true,
    });
    vi.stubGlobal("fetch", fetchMock);

    await expect(getLocalValidationEnabled("")).resolves.toBe(true);
    expect(String(fetchMock.mock.calls[0]?.[0])).toBe(
      "/api/v1/platform/local-validation/enabled",
    );
    const headers = new Headers((fetchMock.mock.calls[0]?.[1] as RequestInit).headers);
    expect(headers.get("X-Correlation-Id")).toMatch(uuidPattern);
  });

  it("listQuickLoginIdentities reaches relative /api identities path", async () => {
    stubNonSecureCrypto();
    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      text: async () =>
        JSON.stringify([
          {
            key: "olivia",
            email: "olivia.mendoza@exits.local",
            displayName: "Olivia Mendoza",
            listLabel: "Olivia Mendoza",
          },
        ]),
      json: async () => [
        {
          key: "olivia",
          email: "olivia.mendoza@exits.local",
          displayName: "Olivia Mendoza",
          listLabel: "Olivia Mendoza",
        },
      ],
    });
    vi.stubGlobal("fetch", fetchMock);

    const identities = await listQuickLoginIdentities("");
    expect(identities).toHaveLength(1);
    expect(String(fetchMock.mock.calls[0]?.[0])).toBe(
      "/api/v1/platform/local-validation/quick-login-identities",
    );
    const headers = new Headers((fetchMock.mock.calls[0]?.[1] as RequestInit).headers);
    expect(headers.get("X-Correlation-Id")).toMatch(uuidPattern);
  });
});
