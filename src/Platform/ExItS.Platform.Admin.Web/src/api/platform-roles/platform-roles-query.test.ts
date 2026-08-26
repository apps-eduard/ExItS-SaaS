import { describe, expect, it } from "vitest";
import {
  parsePlatformRoleId,
  parsePlatformRolesSearchParams,
  platformRolesListPath,
  platformRolesSearchParams,
} from "@/api/platform-roles/platform-roles-query";

describe("platform-roles-query", () => {
  it("parses filters and page from search params", () => {
    const state = parsePlatformRolesSearchParams(
      new URLSearchParams("search=admin&kind=Custom&status=Active&page=2"),
    );
    expect(state).toEqual({
      search: "admin",
      kind: "Custom",
      status: "Active",
      page: 2,
    });
  });

  it("ignores unrecognized kind/status values", () => {
    const state = parsePlatformRolesSearchParams(
      new URLSearchParams("kind=Nope&status=Mystery&page=0"),
    );
    expect(state.kind).toBe("");
    expect(state.status).toBe("");
    expect(state.page).toBe(1);
  });

  it("builds list path with only active filters", () => {
    expect(
      platformRolesListPath({ search: "ops", kind: "BuiltIn", status: "", page: 1 }),
    ).toBe(
      "/api/v1/platform/authorization/role-definitions?page=1&pageSize=20&kind=BuiltIn&search=ops",
    );
  });

  it("round-trips search params", () => {
    const state = {
      search: "x",
      kind: "Custom" as const,
      status: "Inactive" as const,
      page: 3,
    };
    expect(parsePlatformRolesSearchParams(platformRolesSearchParams(state))).toEqual(state);
  });

  it("validates role ids", () => {
    expect(parsePlatformRoleId("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")).toBe(
      "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    );
    expect(parsePlatformRoleId("not-a-guid")).toBeNull();
  });
});
