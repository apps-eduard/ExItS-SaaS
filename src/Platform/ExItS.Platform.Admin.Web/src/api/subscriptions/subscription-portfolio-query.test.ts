import { describe, expect, it } from "vitest";
import {
  hasActiveSubscriptionPortfolioFilters,
  parseSubscriptionPortfolioSearchParams,
  subscriptionPortfolioSearchParams,
} from "@/api/subscriptions/subscription-portfolio-query";

describe("parseSubscriptionPortfolioSearchParams", () => {
  it("defaults to page 1 and UpdatedAtUtc desc", () => {
    expect(parseSubscriptionPortfolioSearchParams(new URLSearchParams())).toEqual({
      page: 1,
      pageSize: 20,
      search: "",
      status: "",
      isTrial: "",
      productCode: "",
      planId: "",
      sortBy: "UpdatedAtUtc",
      sortDesc: true,
    });
  });

  it("parses search, status, trial, product, plan, sort, and page", () => {
    const params = new URLSearchParams({
      search: "northwind",
      status: "Active",
      isTrial: "true",
      productCode: "pinoy-business-pos",
      planId: "11111111-1111-1111-1111-111111111111",
      sortBy: "ProductDisplayName",
      sortDesc: "false",
      page: "3",
    });
    expect(parseSubscriptionPortfolioSearchParams(params)).toMatchObject({
      page: 3,
      search: "northwind",
      status: "Active",
      isTrial: "true",
      productCode: "pinoy-business-pos",
      planId: "11111111-1111-1111-1111-111111111111",
      sortBy: "ProductDisplayName",
      sortDesc: false,
    });
  });

  it("rejects invalid status, trial, sort, and page values", () => {
    const params = new URLSearchParams({
      status: "NotAStatus",
      isTrial: "maybe",
      sortBy: "NotSortable",
      page: "0",
    });
    expect(parseSubscriptionPortfolioSearchParams(params)).toMatchObject({
      page: 1,
      status: "",
      isTrial: "",
      sortBy: "UpdatedAtUtc",
    });
  });
});

describe("subscriptionPortfolioSearchParams", () => {
  it("round-trips active filters", () => {
    const state = parseSubscriptionPortfolioSearchParams(
      subscriptionPortfolioSearchParams({
        page: 2,
        pageSize: 20,
        search: "acme",
        status: "Trialing",
        isTrial: "true",
        productCode: "pos",
        planId: "22222222-2222-2222-2222-222222222222",
        sortBy: "Status",
        sortDesc: false,
      }),
    );
    expect(state).toMatchObject({
      page: 2,
      search: "acme",
      status: "Trialing",
      isTrial: "true",
      productCode: "pos",
      planId: "22222222-2222-2222-2222-222222222222",
      sortBy: "Status",
      sortDesc: false,
    });
    expect(hasActiveSubscriptionPortfolioFilters(state)).toBe(true);
  });
});
