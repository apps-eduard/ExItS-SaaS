import { describe, expect, it } from "vitest";
import {
  organizationListSearchParams,
  organizationsListRequestPath,
  parseOrganizationListSearchParams,
} from "@/api/organizations/organization-list-query";

describe("organization list query", () => {
  it("parses and serializes supported URL state", () => {
    const params = new URLSearchParams(
      "search=acme&status=Suspended&page=2&sortBy=Slug&sortDesc=true",
    );
    const state = parseOrganizationListSearchParams(params);
    expect(state).toEqual({
      page: 2,
      search: "acme",
      status: "Suspended",
      sortBy: "Slug",
      sortDesc: true,
    });
    expect(organizationListSearchParams(state).toString()).toBe(
      "search=acme&status=Suspended&sortBy=Slug&sortDesc=true&page=2",
    );
  });

  it("ignores unknown status and sort values without inventing them", () => {
    const state = parseOrganizationListSearchParams(
      new URLSearchParams("status=Trialing&sortBy=planName"),
    );
    expect(state.status).toBe("");
    expect(state.sortBy).toBe("DisplayName");
    expect(state.sortDesc).toBe(false);
    expect(state.page).toBe(1);
  });

  it("builds the real list path with supported server parameters", () => {
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
  });
});
