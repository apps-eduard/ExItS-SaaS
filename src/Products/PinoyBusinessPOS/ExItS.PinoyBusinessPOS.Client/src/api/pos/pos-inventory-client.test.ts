import { beforeEach, afterEach, describe, expect, it, vi } from "vitest";
import {
  adjustInventoryStock,
  enableInventoryTracking,
  listExpiringLots,
  listProductLots,
} from "@/api/pos/pos-inventory-client";

const workspace = {
  organizationId: "11111111-1111-1111-1111-111111111111",
  branchId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
};

const productId = "cccccccc-cccc-cccc-cccc-cccccccccccc";

function accountJson(extra: Record<string, unknown> = {}) {
  return {
    productId,
    organizationId: workspace.organizationId,
    name: "Milk",
    unitOfMeasure: "Piece",
    productStatus: "Active",
    isTracked: true,
    onHandQuantity: 12,
    stockStatus: "InStock",
    isLowStock: false,
    tracksExpiration: true,
    createdAtUtc: "2026-01-01T00:00:00Z",
    updatedAtUtc: "2026-01-01T00:00:00Z",
    ...extra,
  };
}

describe("pos-inventory-client lots", () => {
  beforeEach(() => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
        const url = String(input);
        const method = init?.method ?? "GET";

        if (url.includes(`/inventory/${productId}/lots`) && method === "GET") {
          expect(url).toContain("includeDepleted=false");
          return new Response(JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 50 }), {
            status: 200,
            headers: { "Content-Type": "application/json" },
          });
        }

        if (url.includes("/inventory/lots?") && method === "GET") {
          if (url.includes("window=Custom")) {
            expect(url).toContain("fromDate=2026-08-01");
            expect(url).toContain("toDate=2026-08-31");
          } else {
            expect(url).toContain("window=Days7");
            expect(url).toContain("search=milk");
          }
          return new Response(
            JSON.stringify({
              items: [],
              totalCount: 0,
              page: 1,
              pageSize: 20,
              expiredCount: 0,
              nearExpiryCount: 0,
            }),
            { status: 200, headers: { "Content-Type": "application/json" } },
          );
        }

        if (url.includes(`/inventory/${productId}/enable`) && method === "POST") {
          const body = JSON.parse(String(init?.body));
          expect(body).toMatchObject({
            openingQuantity: 12,
            expirationDate: "2026-09-01",
            lotNumber: "L-1",
          });
          return new Response(JSON.stringify(accountJson()), {
            status: 200,
            headers: { "Content-Type": "application/json" },
          });
        }

        if (url.includes(`/inventory/${productId}/adjustments`) && method === "POST") {
          const body = JSON.parse(String(init?.body));
          expect(body).toMatchObject({
            direction: "Out",
            quantity: 2,
            reason: "Expired",
            lotId: "dddddddd-dddd-dddd-dddd-dddddddddddd",
          });
          return new Response(JSON.stringify(accountJson({ onHandQuantity: 10 })), {
            status: 200,
            headers: { "Content-Type": "application/json" },
          });
        }

        return new Response("{}", { status: 404 });
      }),
    );
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("lists product lots with includeDepleted query", async () => {
    await listProductLots(workspace, productId);
    expect(fetch).toHaveBeenCalled();
  });

  it("lists expiring lots with window and search", async () => {
    await listExpiringLots(workspace, { window: "Days7", search: "milk", pageSize: 20 });
    expect(fetch).toHaveBeenCalled();
  });

  it("lists expiring lots with custom from/to dates", async () => {
    await listExpiringLots(workspace, {
      window: "Custom",
      fromDate: "2026-08-01",
      toDate: "2026-08-31",
    });
    expect(fetch).toHaveBeenCalled();
    const url = String(vi.mocked(fetch).mock.calls.at(-1)![0]);
    expect(url).toContain("window=Custom");
    expect(url).toContain("fromDate=2026-08-01");
    expect(url).toContain("toDate=2026-08-31");
  });

  it("posts enable body with expirationDate and lotNumber", async () => {
    const account = await enableInventoryTracking(workspace, productId, {
      openingQuantity: 12,
      expirationDate: "2026-09-01",
      lotNumber: "L-1",
    });
    expect(account.tracksExpiration).toBe(true);
  });

  it("posts adjust body with lotId for Out", async () => {
    const account = await adjustInventoryStock(workspace, productId, {
      direction: "Out",
      quantity: 2,
      reason: "Expired",
      lotId: "dddddddd-dddd-dddd-dddd-dddddddddddd",
    });
    expect(account.onHandQuantity).toBe(10);
  });
});
