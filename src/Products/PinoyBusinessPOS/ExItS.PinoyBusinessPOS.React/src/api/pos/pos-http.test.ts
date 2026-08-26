import { beforeEach, describe, expect, it, vi } from "vitest";
import { INSTALLATION_DEVICE_ID_STORAGE_KEY } from "@/workspace/browser-installation-identity";

vi.mock("@/api/platform/pos-access-token", () => ({
  getPosAccessToken: () => "test-access-token",
}));

describe("pos-http installation device header", () => {
  beforeEach(() => {
    localStorage.clear();
    vi.restoreAllMocks();
  });

  it("attaches X-Pos-Installation-Device-Id when durable identity is available", async () => {
    const fixedId = "aaaaaaaa-bbbb-4ccc-8ddd-eeeeeeeeeeee";
    localStorage.setItem(INSTALLATION_DEVICE_ID_STORAGE_KEY, fixedId);

    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({ ok: true }),
    });
    vi.stubGlobal("fetch", fetchMock);

    const { posRequest } = await import("@/api/pos/pos-http");
    await posRequest({
      path: "/products",
      workspace: {
        organizationId: "11111111-1111-1111-1111-111111111111",
        branchId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      },
    });

    expect(fetchMock).toHaveBeenCalled();
    const init = fetchMock.mock.calls[0]?.[1] as RequestInit;
    const headers = new Headers(init.headers);
    expect(headers.get("X-Pos-Installation-Device-Id")).toBe(fixedId);
    expect(headers.get("X-Pos-Organization-Id")).toBe("11111111-1111-1111-1111-111111111111");
    expect(headers.get("X-Pos-Branch-Id")).toBe("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
  });

  it("omits installation header when storage is unavailable (fail-closed)", async () => {
    vi.stubGlobal("localStorage", {
      getItem: () => {
        throw new Error("blocked");
      },
      setItem: () => {
        throw new Error("blocked");
      },
      removeItem: () => {
        throw new Error("blocked");
      },
      clear: () => undefined,
      key: () => null,
      length: 0,
    } as Storage);

    const fetchMock = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({ ok: true }),
    });
    vi.stubGlobal("fetch", fetchMock);

    vi.resetModules();
    const { posRequest } = await import("@/api/pos/pos-http");
    await posRequest({
      path: "/products",
      workspace: {
        organizationId: "11111111-1111-1111-1111-111111111111",
        branchId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      },
    });

    const init = fetchMock.mock.calls[0]?.[1] as RequestInit;
    const headers = new Headers(init.headers);
    expect(headers.get("X-Pos-Installation-Device-Id")).toBeNull();
  });
});
