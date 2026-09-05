import { describe, expect, it, vi } from "vitest";
import { FEATURE_STORE_AREA_MANAGEMENT, FEATURE_STORE_WAREHOUSE } from "@/access/pos-capabilities";

vi.mock("@/api/platform/platform-http", () => ({
  platformRequest: vi.fn(),
  PlatformApiError: class PlatformApiError extends Error {
    status: number;
    problem: { detail?: string };
    constructor(status: number, problem: { detail?: string }) {
      super(problem.detail ?? "error");
      this.status = status;
      this.problem = problem;
    }
  },
}));

describe("session grant feature code normalization", () => {
  it("maps enabledFeatureCodes from token issue onto featureCodes", async () => {
    const { platformRequest } = await import("@/api/platform/platform-http");
    const { issueSessionGrant } = await import("@/api/platform/platform-auth-client");

    vi.mocked(platformRequest).mockResolvedValue({
      accessToken: "tok",
      productAccessAllowed: true,
      organizationManagementAuthority: true,
      membershipRole: "OrganizationOwner",
      mappedPosRoleCode: "Owner",
      enabledFeatureCodes: [FEATURE_STORE_AREA_MANAGEMENT, FEATURE_STORE_WAREHOUSE],
    });

    const result = await issueSessionGrant("11111111-1111-1111-1111-111111111111");
    expect(result.ok).toBe(true);
    if (!result.ok) {
      return;
    }
    expect(result.grant.featureCodes).toEqual([
      FEATURE_STORE_AREA_MANAGEMENT,
      FEATURE_STORE_WAREHOUSE,
    ]);
    expect(result.grant.grantedFeatureCodes).toEqual([
      FEATURE_STORE_AREA_MANAGEMENT,
      FEATURE_STORE_WAREHOUSE,
    ]);
  });
});
