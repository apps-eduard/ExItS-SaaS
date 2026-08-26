import { describe, expect, it } from "vitest";
import { roleAssignmentsRequestPath } from "@/api/authorization/assignment-list-query";
import { mapPlatformRoleAssignment } from "@/api/authorization/authorization-client";

describe("mapPlatformRoleAssignment", () => {
  it("maps assignment fields without inventing values", () => {
    const mapped = mapPlatformRoleAssignment({
      id: "11111111-1111-1111-1111-111111111111",
      platformUserId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
      role: "PlatformAdministrator",
      organizationId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
      status: "Active",
      grantedByActor: "admin@example.test",
      grantedAtUtc: "2026-08-01T08:00:00Z",
      reason: "Onboarding",
    });
    expect(mapped).toEqual({
      id: "11111111-1111-1111-1111-111111111111",
      platformUserId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
      role: "PlatformAdministrator",
      organizationId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
      status: "Active",
      grantedByActor: "admin@example.test",
      grantedAtUtc: "2026-08-01T08:00:00Z",
      reason: "Onboarding",
      revokedByActor: undefined,
      revokedAtUtc: undefined,
      revokeReason: undefined,
    });
  });
});

describe("roleAssignmentsRequestPath", () => {
  it("includes platformUserId and paging parameters", () => {
    const path = roleAssignmentsRequestPath({
      platformUserId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
      status: "Active",
      page: 2,
      pageSize: 10,
    });
    expect(path).toContain("platformUserId=bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    expect(path).toContain("status=Active");
    expect(path).toContain("page=2");
    expect(path).toContain("pageSize=10");
  });
});
