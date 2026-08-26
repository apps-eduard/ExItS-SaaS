import { describe, expect, it, vi } from "vitest";
import { getPersonalDashboard } from "@/api/platform/personal-dashboard-client";

describe("getPersonalDashboard", () => {
  it("parses PascalCase Platform dashboard payloads", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async () => ({
        ok: true,
        status: 200,
        json: async () => ({
          UserIdentityId: "11111111-1111-1111-1111-111111111111",
          AccountProfileId: "22222222-2222-2222-2222-222222222222",
          AccountClass: "Personal",
          UtangAvailable: true,
          ContactCount: 3,
          ActiveRelationshipCount: 2,
          TotalLentBalance: 1000,
          TotalBorrowedBalance: 250.5,
        }),
        text: async () => "",
      })),
    );

    const dto = await getPersonalDashboard();
    expect(dto.contactCount).toBe(3);
    expect(dto.totalLentBalance).toBe(1000);
    expect(dto.totalBorrowedBalance).toBe(250.5);
    expect(dto.pendingConfirmationCount).toBe(0);
    vi.unstubAllGlobals();
  });

  it("parses pendingConfirmationCount", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async () => ({
        ok: true,
        status: 200,
        json: async () => ({
          UserIdentityId: "11111111-1111-1111-1111-111111111111",
          AccountProfileId: "22222222-2222-2222-2222-222222222222",
          AccountClass: "Personal",
          UtangAvailable: true,
          ContactCount: 1,
          ActiveRelationshipCount: 1,
          TotalLentBalance: 10,
          TotalBorrowedBalance: 0,
          PendingConfirmationCount: 4,
        }),
        text: async () => "",
      })),
    );

    const dto = await getPersonalDashboard();
    expect(dto.pendingConfirmationCount).toBe(4);
    vi.unstubAllGlobals();
  });
});
