import { describe, expect, it } from "vitest";
import {
  isOrganizationWorkspaceBranchesPath,
  isOrganizationWorkspacePeoplePath,
  isOrganizationWorkspacePath,
  organizationsListHref,
  parseOrganizationId,
} from "@/api/organizations/organization-id";

describe("parseOrganizationId", () => {
  it("accepts a GUID and rejects malformed values", () => {
    expect(parseOrganizationId("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")).toBe(
      "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    );
    expect(parseOrganizationId("not-a-guid")).toBeNull();
    expect(parseOrganizationId("")).toBeNull();
    expect(parseOrganizationId(undefined)).toBeNull();
  });
});

describe("organization workspace path helpers", () => {
  it("recognizes the workspace route and preserves list query hrefs", () => {
    expect(
      isOrganizationWorkspacePath("/admin/organizations/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
    ).toBe(true);
    expect(isOrganizationWorkspacePath("/admin/organizations")).toBe(false);
    expect(
      isOrganizationWorkspacePath(
        "/admin/organizations/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/branches",
      ),
    ).toBe(false);
    expect(
      isOrganizationWorkspaceBranchesPath(
        "/admin/organizations/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/branches",
      ),
    ).toBe(true);
    expect(
      isOrganizationWorkspacePeoplePath(
        "/admin/organizations/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/people",
      ),
    ).toBe(true);
    expect(organizationsListHref("?search=north&status=Active")).toBe(
      "/admin/organizations?search=north&status=Active",
    );
  });
});
