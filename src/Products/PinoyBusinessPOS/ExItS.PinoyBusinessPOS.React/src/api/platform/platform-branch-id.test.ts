import { describe, expect, it } from "vitest";
import { resolvePlatformBranchId } from "@/api/platform/platform-auth-client";

describe("resolvePlatformBranchId", () => {
  it("prefers ListBranches id over legacy branchId", () => {
    expect(
      resolvePlatformBranchId({
        id: "742fb3f3-14f9-4bee-a94e-f5acccc7cbc5",
        branchId: "wrong",
      }),
    ).toBe("742fb3f3-14f9-4bee-a94e-f5acccc7cbc5");
  });

  it("returns null when both id and branchId are missing", () => {
    expect(resolvePlatformBranchId({ id: "" })).toBeNull();
    expect(resolvePlatformBranchId({ id: "   " })).toBeNull();
  });
});
