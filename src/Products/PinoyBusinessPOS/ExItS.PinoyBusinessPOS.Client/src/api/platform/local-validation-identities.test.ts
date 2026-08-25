import { beforeEach, describe, expect, it, vi } from "vitest";
import { fetchLocalValidationIdentities } from "@/api/platform/platform-auth-client";
import { jsonResponse } from "@/test/render";

describe("fetchLocalValidationIdentities", () => {
  beforeEach(() => {
    vi.unstubAllGlobals();
  });

  it("returns identities when local validation is enabled", async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const url = String(input);
      if (url.includes("/local-validation/enabled")) {
        return jsonResponse(200, true);
      }
      if (url.includes("quick-login-identities")) {
        return jsonResponse(200, [
          {
            key: "ql:1",
            username: "owner1",
            email: "owner1@example.com",
            listLabel: "Owner One",
          },
        ]);
      }
      return jsonResponse(404, {});
    });
    vi.stubGlobal("fetch", fetchMock);

    await expect(fetchLocalValidationIdentities()).resolves.toEqual([
      {
        key: "ql:1",
        username: "owner1",
        email: "owner1@example.com",
        listLabel: "Owner One",
      },
    ]);
  });

  it("returns empty when local validation is disabled", async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      if (String(input).includes("/local-validation/enabled")) {
        return jsonResponse(200, false);
      }
      return jsonResponse(404, {});
    });
    vi.stubGlobal("fetch", fetchMock);

    await expect(fetchLocalValidationIdentities()).resolves.toEqual([]);
    expect(fetchMock.mock.calls.some((call) => String(call[0]).includes("quick-login"))).toBe(
      false,
    );
  });
});
