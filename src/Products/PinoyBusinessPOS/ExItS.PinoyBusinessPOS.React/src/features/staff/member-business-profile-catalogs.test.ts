import { describe, expect, it } from "vitest";
import {
  DEFAULT_DEPARTMENTS,
  DEFAULT_JOB_TITLES,
  mergeOrgScopedOptions,
} from "@/features/staff/member-business-profile-catalogs";

describe("member-business-profile-catalogs", () => {
  it("includes starter department and position defaults", () => {
    expect(DEFAULT_DEPARTMENTS).toContain("Purchasing");
    expect(DEFAULT_DEPARTMENTS).toContain("Inventory / Warehouse");
    expect(DEFAULT_JOB_TITLES).toContain("Purchasing Staff");
    expect(DEFAULT_JOB_TITLES).toContain("Owner / Proprietor");
    expect(DEFAULT_JOB_TITLES).not.toContain("OrganizationOwner");
  });

  it("merges organization-scoped custom values without duplicating defaults", () => {
    const merged = mergeOrgScopedOptions(DEFAULT_DEPARTMENTS, [
      "purchasing",
      "Head Barista Desk",
      "  ",
      null,
    ]);
    expect(merged.filter((v) => v.toLocaleLowerCase() === "purchasing")).toHaveLength(1);
    expect(merged).toContain("Head Barista Desk");
  });
});
