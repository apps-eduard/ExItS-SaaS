import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  getBusinessCustomerCreditPolicy,
  posBusinessCustomerCreditPolicySchema,
} from "@/api/pos/pos-business-credit-policy-client";

const workspace = {
  organizationId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
  branchId: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb",
};

const connectionId = "cccccccc-cccc-4ccc-8ccc-cccccccccccc";
const buyerOrganizationId = "dddddddd-dddd-4ddd-8ddd-dddddddddddd";

function jsonResponse(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), {
    status,
    headers: { "Content-Type": "application/json" },
  });
}

const notConfiguredBody = {
  connectionId,
  sellerOrganizationId: workspace.organizationId,
  buyerOrganizationId,
  status: "NotConfigured",
  creditLimit: null,
  defaultTermDays: null,
  outstandingAmount: 0,
  availableCredit: 0,
  configuredByUserId: null,
  configuredAtUtc: null,
  approvedByUserId: null,
  approvedAtUtc: null,
  updatedByUserId: null,
  updatedAtUtc: null,
  expectedUpdatedAtUtc: null,
};

describe("pos-business-credit-policy-client", () => {
  beforeEach(() => {
    vi.stubGlobal("fetch", vi.fn());
  });

  it("GET uses connectionId in URL (not buyerOrganizationId)", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(jsonResponse(notConfiguredBody));

    await getBusinessCustomerCreditPolicy(workspace, connectionId);

    expect(fetch).toHaveBeenCalledTimes(1);
    const url = String(vi.mocked(fetch).mock.calls[0]![0]);
    expect(url).toBe(
      `/pos-api/api/v1/pos/connected-suppliers/business-customers/${connectionId}/credit-policy`,
    );
    expect(url).toContain(`/business-customers/${connectionId}/credit-policy`);
    expect(url).not.toContain(buyerOrganizationId);
    expect(url).not.toMatch(/\/customers\//);
  });

  it("parses NotConfigured nullable contract", () => {
    expect(posBusinessCustomerCreditPolicySchema.parse(notConfiguredBody)).toMatchObject({
      status: "NotConfigured",
      creditLimit: null,
      defaultTermDays: null,
      outstandingAmount: 0,
      availableCredit: 0,
    });
  });

  it("parses PendingApproval and Approved contracts", () => {
    const pending = posBusinessCustomerCreditPolicySchema.parse({
      ...notConfiguredBody,
      status: "PendingApproval",
      creditLimit: 100000,
      defaultTermDays: 90,
      updatedAtUtc: "2026-09-10T00:00:00Z",
      expectedUpdatedAtUtc: "2026-09-10T00:00:00Z",
      configuredByUserId: "11111111-1111-4111-8111-111111111111",
      configuredAtUtc: "2026-09-10T00:00:00Z",
    });
    expect(pending.status).toBe("PendingApproval");
    expect(pending.creditLimit).toBe(100000);

    const approved = posBusinessCustomerCreditPolicySchema.parse({
      ...pending,
      status: "Approved",
      outstandingAmount: 25000,
      availableCredit: 75000,
      approvedByUserId: "22222222-2222-4222-8222-222222222222",
      approvedAtUtc: "2026-09-10T12:00:00Z",
    });
    expect(approved.status).toBe("Approved");
    expect(approved.availableCredit).toBe(75000);
  });

  it("surfaces HTTP 404 and 403 as PosApiError", async () => {
    vi.mocked(fetch).mockResolvedValueOnce(
      jsonResponse(
        { title: "Not Found", status: 404, detail: "missing", errorCode: "pos.not_found" },
        404,
      ),
    );
    await expect(getBusinessCustomerCreditPolicy(workspace, connectionId)).rejects.toMatchObject({
      status: 404,
      errorCode: "pos.not_found",
    });

    vi.mocked(fetch).mockResolvedValueOnce(
      jsonResponse(
        { title: "Forbidden", status: 403, detail: "denied", errorCode: "pos.forbidden" },
        403,
      ),
    );
    await expect(getBusinessCustomerCreditPolicy(workspace, connectionId)).rejects.toMatchObject({
      status: 403,
      errorCode: "pos.forbidden",
    });
  });
});
