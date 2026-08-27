import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  acceptOwnershipTransfer,
  declineOwnershipTransfer,
  listMyPendingOwnershipTransfers,
} from "@/api/platform/ownership-transfer-client";

const platformRequest = vi.hoisted(() => vi.fn());

vi.mock("@/api/platform/platform-http", async () => {
  const actual = await vi.importActual<typeof import("@/api/platform/platform-http")>(
    "@/api/platform/platform-http",
  );
  return {
    ...actual,
    platformRequest: platformRequest,
  };
});

const transferId = "11111111-1111-4111-8111-111111111111";
const orgId = "22222222-2222-4222-8222-222222222222";
const fromOwner = "33333333-3333-4333-8333-333333333333";
const toUser = "44444444-4444-4444-8444-444444444444";

const sampleTransfer = {
  id: transferId,
  organizationId: orgId,
  organizationDisplayName: "Corner Store",
  publicOrganizationId: "ORG123456",
  fromOwnerUserId: fromOwner,
  toUserId: toUser,
  toDisplayName: "Paul",
  toPublicUserId: "EX-1111-2222",
  status: "Pending",
  createdAtUtc: "2026-08-20T00:00:00Z",
  expiresAtUtc: "2026-08-27T00:00:00Z",
  acceptedAtUtc: null,
  declinedAtUtc: null,
  cancelledAtUtc: null,
  completedAtUtc: null,
  updatedAtUtc: "2026-08-20T00:00:00Z",
};

describe("ownership-transfer-client", () => {
  beforeEach(() => {
    platformRequest.mockReset();
  });

  it("lists my pending ownership transfers", async () => {
    platformRequest.mockResolvedValueOnce([sampleTransfer]);

    const result = await listMyPendingOwnershipTransfers();

    expect(result).toHaveLength(1);
    expect(result[0]?.organizationDisplayName).toBe("Corner Store");
    expect(platformRequest).toHaveBeenCalledWith(
      expect.objectContaining({
        path: "/api/v1/platform/ownership-transfers/my-pending",
      }),
    );
  });

  it("normalizes PascalCase pending list payloads", async () => {
    platformRequest.mockResolvedValueOnce([
      {
        Id: transferId,
        OrganizationId: orgId,
        OrganizationDisplayName: "Sari-Sari",
        PublicOrganizationId: "ORG999999",
        FromOwnerUserId: fromOwner,
        ToUserId: toUser,
        ToDisplayName: "Ana",
        ToPublicUserId: "EX-9999-8888",
        Status: "Pending",
        CreatedAtUtc: "2026-08-20T00:00:00Z",
        ExpiresAtUtc: "2026-08-27T00:00:00Z",
        AcceptedAtUtc: null,
        DeclinedAtUtc: null,
        CancelledAtUtc: null,
        CompletedAtUtc: null,
        UpdatedAtUtc: "2026-08-20T00:00:00Z",
      },
    ]);

    const result = await listMyPendingOwnershipTransfers();
    expect(result[0]?.organizationDisplayName).toBe("Sari-Sari");
    expect(result[0]?.publicOrganizationId).toBe("ORG999999");
  });

  it("posts accept and returns transfer dto", async () => {
    platformRequest.mockResolvedValueOnce({
      ...sampleTransfer,
      status: "Accepted",
      acceptedAtUtc: "2026-08-21T00:00:00Z",
    });

    const result = await acceptOwnershipTransfer(transferId);

    expect(result.status).toBe("Accepted");
    expect(platformRequest).toHaveBeenCalledWith(
      expect.objectContaining({
        method: "POST",
        path: `/api/v1/platform/ownership-transfers/${transferId}/accept`,
      }),
    );
  });

  it("posts decline and returns transfer dto", async () => {
    platformRequest.mockResolvedValueOnce({
      ...sampleTransfer,
      status: "Declined",
      declinedAtUtc: "2026-08-21T00:00:00Z",
    });

    const result = await declineOwnershipTransfer(transferId);

    expect(result.status).toBe("Declined");
    expect(platformRequest).toHaveBeenCalledWith(
      expect.objectContaining({
        method: "POST",
        path: `/api/v1/platform/ownership-transfers/${transferId}/decline`,
      }),
    );
  });
});
