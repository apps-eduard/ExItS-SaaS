import { describe, expect, it } from "vitest";
import { hasOrganizationManagementAuthority } from "@/access/pos-capabilities";
import type { PosSessionGrantFacts } from "@/access/pos-capabilities";

describe("TASK61 organization profile permission gates", () => {
  it("Owner/Admin grant can edit organization profile", () => {
    const grant = {
      organizationManagementAuthority: true,
      mappedPosRoleCode: "Owner",
    } as PosSessionGrantFacts;
    expect(hasOrganizationManagementAuthority(grant)).toBe(true);
  });

  it("normal staff grant cannot edit organization profile via UI gate", () => {
    const grant = {
      organizationManagementAuthority: false,
      mappedPosRoleCode: "Cashier",
    } as PosSessionGrantFacts;
    expect(hasOrganizationManagementAuthority(grant)).toBe(false);
  });
});
