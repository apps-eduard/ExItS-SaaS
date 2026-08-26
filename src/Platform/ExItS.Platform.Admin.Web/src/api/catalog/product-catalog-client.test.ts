import { describe, expect, it } from "vitest";
import { catalogProductsListRequestPath } from "@/api/catalog/product-list-query";
import { mapCatalogProduct, listCatalogProductsPage } from "@/api/catalog/product-catalog-client";

describe("mapCatalogProduct", () => {
  it("maps catalog product fields", () => {
    expect(
      mapCatalogProduct({
        id: "11111111-1111-1111-1111-111111111111",
        code: "future-product-x",
        displayName: "Future Product X",
        status: "Active",
      }),
    ).toEqual({
      id: "11111111-1111-1111-1111-111111111111",
      code: "future-product-x",
      displayName: "Future Product X",
      status: "Active",
    });
  });
});

describe("catalogProductsListRequestPath", () => {
  it("includes search, status, sort, and paging", () => {
    const path = catalogProductsListRequestPath({
      page: 2,
      pageSize: 20,
      status: "Active",
      search: "pos",
      sortBy: "Code",
      sortDesc: true,
    });
    expect(path).toContain("page=2");
    expect(path).toContain("status=Active");
    expect(path).toContain("search=pos");
    expect(path).toContain("sortBy=Code");
    expect(path).toContain("sortDesc=true");
  });
});

describe("listCatalogProductsPage", () => {
  it("is exported for full catalog list queries", () => {
    expect(typeof listCatalogProductsPage).toBe("function");
  });
});
