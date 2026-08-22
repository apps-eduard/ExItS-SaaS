import { describe, expect, it } from "vitest";
import {
  globalCategoryListSearchParams,
  globalCategoriesListRequestPath,
  parseGlobalCategoryListSearchParams,
} from "@/api/global-catalog/category-list-query";
import {
  mapGlobalBusinessType,
  mapGlobalCategoryListItem,
  mapGlobalProductListItem,
} from "@/api/global-catalog/global-catalog-client";
import { globalProductImageUrl } from "@/api/global-catalog/global-catalog-http";
import {
  globalProductListSearchParams,
  globalProductsListRequestPath,
  parseGlobalProductListSearchParams,
} from "@/api/global-catalog/product-list-query";

describe("global catalog list query builders", () => {
  it("parses and serializes category URL state", () => {
    const state = parseGlobalCategoryListSearchParams(
      new URLSearchParams(
        "search=beverage&status=Active&businessTypeId=dddddddd-dddd-dddd-dddd-dddddddddddd&sortBy=Name&sortDesc=true&page=2",
      ),
    );
    expect(state.search).toBe("beverage");
    expect(state.status).toBe("Active");
    expect(state.businessTypeId).toBe("dddddddd-dddd-dddd-dddd-dddddddddddd");
    expect(state.sortBy).toBe("Name");
    expect(state.sortDesc).toBe(true);
    expect(globalCategoryListSearchParams(state).toString()).toContain("search=beverage");
    expect(
      globalCategoriesListRequestPath({
        page: 2,
        pageSize: 20,
        status: "Active",
        search: "beverage",
        businessTypeId: "dddddddd-dddd-dddd-dddd-dddddddddddd",
        sortBy: "Name",
        sortDesc: true,
      }),
    ).toBe(
      "/api/v1/platform/global-catalog/categories?page=2&pageSize=20&status=Active&businessTypeId=dddddddd-dddd-dddd-dddd-dddddddddddd&search=beverage&sortBy=Name&sortDesc=true",
    );
  });

  it("parses and serializes product URL state", () => {
    const state = parseGlobalProductListSearchParams(
      new URLSearchParams("search=water&categoryId=aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa&sku=BW"),
    );
    expect(state.search).toBe("water");
    expect(state.categoryId).toBe("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    expect(state.sku).toBe("BW");
    expect(globalProductListSearchParams(state).get("sku")).toBe("BW");
    expect(
      globalProductsListRequestPath({
        page: 1,
        pageSize: 20,
        categoryId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
        sku: "BW",
        sortBy: "Sku",
      }),
    ).toBe(
      "/api/v1/platform/global-catalog/products?page=1&pageSize=20&categoryId=aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa&sku=BW&sortBy=Sku",
    );
  });
});

describe("global catalog mappers", () => {
  it("maps business type, category, and product payloads", () => {
    expect(
      mapGlobalBusinessType({
        id: "dddddddd-dddd-dddd-dddd-dddddddddddd",
        code: "sari-sari",
        name: "Sari-Sari Store",
        status: "Active",
        sortOrder: 1,
      }),
    ).toMatchObject({ code: "sari-sari", name: "Sari-Sari Store" });

    expect(
      mapGlobalCategoryListItem({
        id: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
        name: "Beverages",
        parentId: null,
        sortOrder: 10,
        status: "Active",
        businessTypes: ["sari-sari"],
        businessTypeIds: ["dddddddd-dddd-dddd-dddd-dddddddddddd"],
      }),
    ).toMatchObject({ name: "Beverages", parentId: null });

    expect(
      mapGlobalProductListItem({
        id: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
        name: "Bottled Water",
        sku: "BW-500",
        brand: "Refresh",
        globalCategoryId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
        unit: "Bottle",
        sellingMode: "PerItem",
        status: "Active",
        searchTags: [],
        businessTypes: [],
        businessTypeIds: [],
        hasImage: true,
        imageVersion: 2,
      }),
    ).toMatchObject({ sku: "BW-500", hasImage: true, imageVersion: 2 });
  });

  it("builds image preview URLs with optional version", () => {
    expect(
      globalProductImageUrl(
        "http://platform.test",
        "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
        "medium",
        3,
      ),
    ).toBe(
      "http://platform.test/api/v1/platform/global-catalog/products/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb/image/medium?v=3",
    );
  });
});
