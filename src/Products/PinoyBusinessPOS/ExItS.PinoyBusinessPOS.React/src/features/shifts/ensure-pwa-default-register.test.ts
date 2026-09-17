import { describe, expect, it, vi, beforeEach } from "vitest";
import {
  ensurePwaDefaultCashRegister,
  nextPwaRegisterDisplayName,
  PWA_DEFAULT_REGISTER_NAME,
} from "@/features/shifts/ensure-pwa-default-register";

vi.mock("@/api/pos/pos-registers-client", () => ({
  ensureAvailablePwaRegisterForShift: vi.fn(),
}));

import { ensureAvailablePwaRegisterForShift } from "@/api/pos/pos-registers-client";

const workspace = {
  organizationId: "11111111-1111-4111-8111-111111111111",
  branchId: "22222222-2222-4222-8222-222222222222",
};

describe("nextPwaRegisterDisplayName", () => {
  it("starts at PWA-0001 when empty", () => {
    expect(nextPwaRegisterDisplayName([])).toBe(PWA_DEFAULT_REGISTER_NAME);
  });

  it("uses max + 1 and ignores unrelated names", () => {
    expect(nextPwaRegisterDisplayName(["Front Counter", "PWA-0001", "PWA-0002"])).toBe("PWA-0003");
  });

  it("does not reuse gaps (max + 1 convention)", () => {
    expect(nextPwaRegisterDisplayName(["PWA-0001", "PWA-0003"])).toBe("PWA-0004");
  });
});

describe("ensurePwaDefaultCashRegister", () => {
  beforeEach(() => {
    vi.mocked(ensureAvailablePwaRegisterForShift).mockReset();
  });

  it("delegates to server ensure path", async () => {
    vi.mocked(ensureAvailablePwaRegisterForShift).mockResolvedValue({
      registerId: "reg-2",
      organizationId: workspace.organizationId,
      registerCode: "REG-000002",
      name: "PWA-0002",
      status: "Active",
      createdAtUtc: "",
      createdBy: "",
      updatedAtUtc: "",
      updatedBy: "",
      hasOpenShift: false,
    });

    const result = await ensurePwaDefaultCashRegister(workspace);
    expect(ensureAvailablePwaRegisterForShift).toHaveBeenCalledWith(workspace);
    expect(result.name).toBe("PWA-0002");
  });
});
