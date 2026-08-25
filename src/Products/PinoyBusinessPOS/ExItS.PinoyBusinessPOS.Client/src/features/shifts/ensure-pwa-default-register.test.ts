import { describe, expect, it, vi, beforeEach } from "vitest";
import {
  ensurePwaDefaultCashRegister,
  PWA_DEFAULT_REGISTER_NAME,
} from "@/features/shifts/ensure-pwa-default-register";

vi.mock("@/api/pos/pos-registers-client", () => ({
  listRegisters: vi.fn(),
  createRegister: vi.fn(),
}));

import { createRegister, listRegisters } from "@/api/pos/pos-registers-client";

const workspace = {
  organizationId: "11111111-1111-4111-8111-111111111111",
  branchId: "22222222-2222-4222-8222-222222222222",
};

describe("ensurePwaDefaultCashRegister", () => {
  beforeEach(() => {
    vi.mocked(listRegisters).mockReset();
    vi.mocked(createRegister).mockReset();
  });

  it("reuses free active PWA-0001", async () => {
    vi.mocked(listRegisters).mockResolvedValue({
      items: [
        {
          registerId: "reg-1",
          organizationId: workspace.organizationId,
          registerCode: "REG-000001",
          name: PWA_DEFAULT_REGISTER_NAME,
          status: "Active",
          createdAtUtc: "",
          createdBy: "",
          updatedAtUtc: "",
          updatedBy: "",
          hasOpenShift: false,
        },
      ],
      totalCount: 1,
      page: 1,
      pageSize: 50,
    });

    const result = await ensurePwaDefaultCashRegister(workspace);
    expect(result.registerId).toBe("reg-1");
    expect(createRegister).not.toHaveBeenCalled();
  });

  it("creates PWA-0001 when no free active register exists", async () => {
    vi.mocked(listRegisters).mockResolvedValue({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 50,
    });
    vi.mocked(createRegister).mockResolvedValue({
      registerId: "reg-new",
      organizationId: workspace.organizationId,
      registerCode: "REG-000001",
      name: PWA_DEFAULT_REGISTER_NAME,
      status: "Active",
      createdAtUtc: "",
      createdBy: "",
      updatedAtUtc: "",
      updatedBy: "",
      hasOpenShift: false,
    });

    const result = await ensurePwaDefaultCashRegister(workspace);
    expect(createRegister).toHaveBeenCalledWith(workspace, {
      name: PWA_DEFAULT_REGISTER_NAME,
      description: "Auto-created cash register for web POS (PWA).",
    });
    expect(result.registerId).toBe("reg-new");
  });
});
