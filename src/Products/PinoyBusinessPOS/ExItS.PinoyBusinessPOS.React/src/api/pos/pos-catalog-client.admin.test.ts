import { describe, expect, it, vi, beforeEach, afterEach } from "vitest";
import {
  createCatalogBrand,
  createCatalogProduct,
  listCatalogBrands,
  listCatalogCategories,
  listCatalogProducts,
  updateCatalogProduct,
} from "@/api/pos/pos-catalog-client";
import { PosApiError } from "@/api/pos/pos-http";

const workspace = {
  organizationId: "11111111-1111-1111-1111-111111111111",
  branchId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
};

describe("pos-catalog-client admin", () => {
  beforeEach(() => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
        const url = String(input);
        const method = init?.method ?? "GET";
        if (url.includes("/catalog/categories") && method === "GET") {
          return new Response(JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 50 }), {
            status: 200,
            headers: { "Content-Type": "application/json" },
          });
        }
        if (url.includes("/catalog/brands") && method === "GET") {
          return new Response(JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 50 }), {
            status: 200,
            headers: { "Content-Type": "application/json" },
          });
        }
        if (url.includes("/catalog/brands") && method === "POST") {
          const body = JSON.parse(String(init?.body));
          return new Response(
            JSON.stringify({
              brandId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
              organizationId: workspace.organizationId,
              name: body.name,
              status: "Active",
              createdAtUtc: "2026-01-01T00:00:00Z",
              updatedAtUtc: "2026-01-01T00:00:00Z",
            }),
            { status: 201, headers: { "Content-Type": "application/json" } },
          );
        }
        if (url.includes("/catalog/products?") && method === "GET") {
          return new Response(JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 24 }), {
            status: 200,
            headers: { "Content-Type": "application/json" },
          });
        }
        if (url.includes("/catalog/products") && method === "POST") {
          const body = JSON.parse(String(init?.body));
          expect(body.unitOfMeasure).toBe("Piece");
          expect(body.sellingPrice).toBe(0);
          expect(body.sellingMode).toBe("PerItem");
          return new Response(
            JSON.stringify({
              productId: "pppppppp-pppp-pppp-pppp-pppppppppppp",
              organizationId: workspace.organizationId,
              name: body.name,
              unitOfMeasure: body.unitOfMeasure,
              sellingMode: body.sellingMode ?? "PerItem",
              sellingPrice: body.sellingPrice,
              brandId: body.brandId ?? null,
              status: "Active",
              createdAtUtc: "2026-01-01T00:00:00Z",
              updatedAtUtc: "2026-01-01T00:00:00Z",
              units: body.units ?? [],
            }),
            { status: 201, headers: { "Content-Type": "application/json" } },
          );
        }
        if (url.includes("/catalog/products/") && method === "PUT") {
          return new Response(JSON.stringify({ detail: "Product was modified by another user." }), {
            status: 409,
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

  it("lists categories with org and branch headers", async () => {
    await listCatalogCategories(workspace);
    const fetchMock = vi.mocked(fetch);
    expect(fetchMock).toHaveBeenCalled();
    const [, init] = fetchMock.mock.calls[0]!;
    const headers = new Headers(init?.headers);
    expect(headers.get("X-Pos-Organization-Id")).toBe(workspace.organizationId);
    expect(headers.get("X-Pos-Branch-Id")).toBe(workspace.branchId);
  });

  it("lists brands with org and branch headers", async () => {
    await listCatalogBrands(workspace);
    const fetchMock = vi.mocked(fetch);
    expect(fetchMock).toHaveBeenCalled();
    const [url, init] = fetchMock.mock.calls[0]!;
    expect(String(url)).toContain("/catalog/brands");
    const headers = new Headers(init?.headers);
    expect(headers.get("X-Pos-Organization-Id")).toBe(workspace.organizationId);
    expect(headers.get("X-Pos-Branch-Id")).toBe(workspace.branchId);
  });

  it("creates brand", async () => {
    const brand = await createCatalogBrand(workspace, { name: "San Miguel" });
    expect(brand.brandId).toBeTruthy();
    expect(brand.name).toBe("San Miguel");
  });

  it("passes brandId when listing products", async () => {
    await listCatalogProducts(workspace, {
      brandId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
    });
    const fetchMock = vi.mocked(fetch);
    const [url] = fetchMock.mock.calls[0]!;
    expect(String(url)).toContain("brandId=bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
  });

  it("creates product with RMAP-04 UOM/price defaults", async () => {
    const product = await createCatalogProduct(workspace, {
      name: "Test",
      unitOfMeasure: "Piece",
      sellingPrice: 0,
      sellingMode: "PerItem",
      canBeSold: true,
      brandId: null,
    });
    expect(product.productId).toBeTruthy();
  });

  it("surfaces concurrency conflict on update", async () => {
    await expect(
      updateCatalogProduct(workspace, "cccccccc-cccc-cccc-cccc-cccccccccccc", {
        name: "Coke",
        unitOfMeasure: "Piece",
        sellingPrice: 25,
        expectedUpdatedAtUtc: "2020-01-01T00:00:00Z",
      }),
    ).rejects.toBeInstanceOf(PosApiError);
  });
});
