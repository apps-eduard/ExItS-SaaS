import { beforeEach, describe, expect, it, vi } from "vitest";
import { resolveCatalogLookup } from "@/api/pos/catalog-lookup";
import type { PosCatalogProductDto } from "@/api/pos/pos-catalog-types";

const workspace = {
  organizationId: "11111111-1111-1111-1111-111111111111",
  branchId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
};

const coke: PosCatalogProductDto = {
  productId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
  organizationId: workspace.organizationId,
  name: "Coke",
  sku: "COKE-330",
  barcode: "4006381333931",
  unitOfMeasure: "bottle",
  sellingMode: "Unit",
  sellingPrice: 25,
  status: "Active",
  createdAtUtc: "2026-01-01T00:00:00Z",
  updatedAtUtc: "2026-01-01T00:00:00Z",
};

describe("resolveCatalogLookup", () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it("tries sku then name search for typed text without calling barcode", async () => {
    const calls: string[] = [];
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        const url = String(input);
        calls.push(url);

        if (url.includes("/by-barcode/")) {
          throw new Error("barcode lookup must not run for typed SKU text");
        }

        if (url.includes("/by-sku/")) {
          return {
            ok: true,
            status: 200,
            json: async () => coke,
            text: async () => "",
          } as Response;
        }

        return {
          ok: false,
          status: 404,
          json: async () => ({ detail: "not mocked" }),
          text: async () => "",
        } as Response;
      }),
    );

    const result = await resolveCatalogLookup(workspace, "COKE-330");
    expect(result.kind).toBe("exact");
    if (result.kind === "exact") {
      expect(result.matchedBy).toBe("sku");
      expect(result.product.productId).toBe(coke.productId);
    }

    expect(calls.some((url) => url.includes("/by-barcode/"))).toBe(false);
    expect(calls.some((url) => url.includes("/by-sku/"))).toBe(true);
  });

  it("returns unknown barcode error without falling through to name search", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        const url = String(input);
        if (url.includes("/by-barcode/") || url.includes("/by-sku/")) {
          return {
            ok: false,
            status: 404,
            json: async () => ({ detail: "not found" }),
            text: async () => "",
          } as Response;
        }
        throw new Error("name search must not run for unknown barcode scan");
      }),
    );

    const result = await resolveCatalogLookup(workspace, "4006381333930");
    expect(result.kind).toBe("empty");
    if (result.kind === "empty") {
      expect(result.unknownBarcode).toBe(true);
    }
  });

  it("falls back to name search when sku misses for a typed letter", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        const url = String(input);
        if (url.includes("/by-barcode/")) {
          throw new Error("barcode lookup must not run for a single typed letter");
        }

        if (url.includes("/by-sku/")) {
          return {
            ok: false,
            status: 404,
            json: async () => ({ detail: "not found" }),
            text: async () => "",
          } as Response;
        }

        if (url.includes("/catalog/products?") && url.includes("search=s")) {
          return {
            ok: true,
            status: 200,
            json: async () => ({
              items: [coke],
              totalCount: 1,
              page: 1,
              pageSize: 24,
            }),
            text: async () => "",
          } as Response;
        }

        return {
          ok: false,
          status: 404,
          json: async () => ({ detail: "not mocked" }),
          text: async () => "",
        } as Response;
      }),
    );

    const result = await resolveCatalogLookup(workspace, "s");
    expect(result.kind).toBe("search");
    if (result.kind === "search") {
      expect(result.products).toHaveLength(1);
      expect(result.products[0]?.name).toBe("Coke");
    }
  });

  it("falls back to name search when barcode and sku miss", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        const url = String(input);
        if (url.includes("/by-barcode/") || url.includes("/by-sku/")) {
          return {
            ok: false,
            status: 404,
            json: async () => ({ detail: "not found" }),
            text: async () => "",
          } as Response;
        }

        if (url.includes("/catalog/products?") && url.includes("search=cola")) {
          return {
            ok: true,
            status: 200,
            json: async () => ({
              items: [coke],
              totalCount: 1,
              page: 1,
              pageSize: 24,
            }),
            text: async () => "",
          } as Response;
        }

        return {
          ok: false,
          status: 404,
          json: async () => ({ detail: "not mocked" }),
          text: async () => "",
        } as Response;
      }),
    );

    const result = await resolveCatalogLookup(workspace, "cola");
    expect(result.kind).toBe("search");
    if (result.kind === "search") {
      expect(result.products).toHaveLength(1);
      expect(result.products[0]?.name).toBe("Coke");
    }
  });

  it("treats barcode validation 400 as an unknown scan, not a connection error", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        const url = String(input);
        if (url.includes("/by-barcode/")) {
          return {
            ok: false,
            status: 400,
            json: async () => ({
              detail: "Barcode check digit is invalid",
              errorCode: "pos.product.barcode.invalid",
            }),
            text: async () => "",
          } as Response;
        }
        if (url.includes("/by-sku/")) {
          return {
            ok: false,
            status: 404,
            json: async () => ({ detail: "not found" }),
            text: async () => "",
          } as Response;
        }
        throw new Error("name search must not run for invalid barcode scan");
      }),
    );

    const result = await resolveCatalogLookup(workspace, "4006381333931");
    expect(result.kind).toBe("empty");
    if (result.kind === "empty") {
      expect(result.unknownBarcode).toBe(true);
    }
  });
});
