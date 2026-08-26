import { describe, expect, it } from "vitest";
import {
  organizationListSearchParams,
  organizationsListRequestPath,
  parseOrganizationListSearchParams,
  sanitizeOrganizationListProduct,
} from "@/api/organizations/organization-list-query";

describe("organization list query", () => {
  it("parses and serializes supported URL state including product", () => {
    const params = new URLSearchParams(
      "search=acme&status=Suspended&page=2&sortBy=Slug&sortDesc=true&product=future-product-x",
    );
    const state = parseOrganizationListSearchParams(params);
    expect(state).toEqual({
      page: 2,
      search: "acme",
      status: "Suspended",
      sortBy: "Slug",
      sortDesc: true,
      product: "future-product-x",
    });
    expect(organizationListSearchParams(state).toString()).toBe(
      "search=acme&status=Suspended&sortBy=Slug&sortDesc=true&product=future-product-x&page=2",
    );
  });

  it("preserves search status sort page when product changes in serialization", () => {
    const params = organizationListSearchParams({
      page: 3,
      search: "north",
      status: "Active",
      sortBy: "CreatedAtUtc",
      sortDesc: true,
      product: "pinoy-business-pos",
    });
    expect(params.get("search")).toBe("north");
    expect(params.get("status")).toBe("Active");
    expect(params.get("sortBy")).toBe("CreatedAtUtc");
    expect(params.get("sortDesc")).toBe("true");
    expect(params.get("page")).toBe("3");
    expect(params.get("product")).toBe("pinoy-business-pos");
  });

  it("ignores unknown status and sort values without inventing them", () => {
    const state = parseOrganizationListSearchParams(
      new URLSearchParams("status=Trialing&sortBy=planName"),
    );
    expect(state.status).toBe("");
    expect(state.sortBy).toBe("DisplayName");
    expect(state.sortDesc).toBe(false);
    expect(state.page).toBe(1);
    expect(state.product).toBe("");
  });

  it("builds the real list path with supported server parameters including productCode", () => {
    expect(
      organizationsListRequestPath({
        page: 2,
        pageSize: 20,
        status: "Active",
        search: "north",
        sortBy: "CreatedAtUtc",
        sortDesc: true,
      }),
    ).toBe(
      "/api/v1/platform/organizations?page=2&pageSize=20&status=Active&search=north&sortBy=CreatedAtUtc&sortDesc=true",
    );
    expect(
      organizationsListRequestPath({
        page: 1,
        pageSize: 20,
        productCode: "future-product-x",
      }),
    ).toBe("/api/v1/platform/organizations?page=1&pageSize=20&productCode=future-product-x");
  });

  it("sanitizes product against authorized catalog only", () => {
    const catalog = [
      { code: "future-product-x", displayName: "Future Product X" },
      { code: "pinoy-business-pos", displayName: "Pinoy Business POS" },
    ];
    expect(sanitizeOrganizationListProduct("future-product-x", catalog)).toEqual(catalog[0]);
    expect(sanitizeOrganizationListProduct("not-a-product", catalog)).toBeNull();
    expect(sanitizeOrganizationListProduct("", catalog)).toBeNull();
    expect(sanitizeOrganizationListProduct("FUTURE-PRODUCT-X", catalog)).toBeNull();
  });
});
