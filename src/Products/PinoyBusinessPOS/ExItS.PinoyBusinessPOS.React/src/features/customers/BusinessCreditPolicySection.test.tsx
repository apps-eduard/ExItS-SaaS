import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { PosBusinessCustomerCreditPolicy } from "@/api/pos/pos-business-credit-policy-client";
import * as businessCreditClient from "@/api/pos/pos-business-credit-policy-client";
import { PosApiError } from "@/api/pos/pos-http";
import { BusinessCreditPolicySection } from "@/features/customers/BusinessCreditPolicySection";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import type { ReactNode } from "react";

vi.mock("@/i18n/I18nProvider", () => ({
  useI18n: () => ({
    t: (key: string) => key,
  }),
}));

vi.mock("@/features/actors/useActorDirectory", () => ({
  useActorDirectory: () => ({
    resolve: () => null,
    isResolving: false,
  }),
}));

afterEach(() => {
  vi.restoreAllMocks();
});

const workspace = {
  organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  branchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
};

const connectionId = "cccccccc-cccc-4ccc-8ccc-cccccccccccc";

function policy(partial: Partial<PosBusinessCustomerCreditPolicy>): PosBusinessCustomerCreditPolicy {
  return {
    connectionId,
    sellerOrganizationId: workspace.organizationId,
    buyerOrganizationId: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
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
    ...partial,
  };
}

function wrap(ui: ReactNode) {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(<QueryClientProvider client={client}>{ui}</QueryClientProvider>);
}

describe("BusinessCreditPolicySection", () => {
  it("renders NotConfigured with Configure credit", () => {
    wrap(
      <BusinessCreditPolicySection
        workspace={workspace}
        connectionId={connectionId}
        online
        canManage
        canApprove
        policyOverride={policy({ status: "NotConfigured" })}
      />,
    );
    expect(screen.getByTestId("business-credit-policy-status")).toHaveTextContent(
      "customers.creditPolicy.status.NotConfigured",
    );
    expect(screen.getByTestId("business-credit-policy-configure")).toBeInTheDocument();
    expect(screen.getByText("customers.business.creditPolicy.notApprovedHint")).toBeInTheDocument();
  });

  it("renders PendingApproval with Approve", () => {
    wrap(
      <BusinessCreditPolicySection
        workspace={workspace}
        connectionId={connectionId}
        online
        canManage
        canApprove
        policyOverride={policy({
          status: "PendingApproval",
          creditLimit: 100_000,
          defaultTermDays: 90,
          expectedUpdatedAtUtc: "2026-09-10T00:00:00Z",
        })}
      />,
    );
    expect(screen.getByTestId("business-credit-policy-approve")).toBeInTheDocument();
    expect(screen.getByTestId("business-credit-policy-disable")).toBeInTheDocument();
  });

  it("renders Approved with limit term and Edit", () => {
    wrap(
      <BusinessCreditPolicySection
        workspace={workspace}
        connectionId={connectionId}
        online
        canManage
        canApprove
        policyOverride={policy({
          status: "Approved",
          creditLimit: 100_000,
          defaultTermDays: 90,
          outstandingAmount: 25_000,
          availableCredit: 75_000,
          expectedUpdatedAtUtc: "2026-09-10T00:00:00Z",
          approvedByUserId: "22222222-2222-2222-2222-222222222222",
          approvedAtUtc: "2026-09-10T12:00:00Z",
        })}
      />,
    );
    expect(screen.getByTestId("business-credit-policy-configure")).toHaveTextContent(
      "customers.creditPolicy.edit",
    );
    expect(screen.getByTestId("business-credit-policy-limit")).toBeInTheDocument();
    expect(screen.getByTestId("business-credit-policy-term")).toBeInTheDocument();
    expect(screen.queryByTestId("business-credit-policy-approve")).not.toBeInTheDocument();
  });

  it("hides management actions for Cashier", () => {
    wrap(
      <BusinessCreditPolicySection
        workspace={workspace}
        connectionId={connectionId}
        online
        canManage={false}
        canApprove={false}
        policyOverride={policy({
          status: "PendingApproval",
          creditLimit: 1000,
          defaultTermDays: 30,
          expectedUpdatedAtUtc: "2026-09-10T00:00:00Z",
        })}
      />,
    );
    expect(screen.queryByTestId("business-credit-policy-configure")).not.toBeInTheDocument();
    expect(screen.queryByTestId("business-credit-policy-approve")).not.toBeInTheDocument();
    expect(screen.queryByTestId("business-credit-policy-disable")).not.toBeInTheDocument();
  });

  it("shows friendly error (not raw) for real load failure and Retry refetches", async () => {
    const user = userEvent.setup();
    const getSpy = vi
      .spyOn(businessCreditClient, "getBusinessCustomerCreditPolicy")
      .mockRejectedValueOnce(
        new PosApiError(404, { status: 404, title: "Not Found", detail: "POS API request failed (404)" }),
      )
      .mockResolvedValueOnce(policy({ status: "NotConfigured" }));

    wrap(
      <BusinessCreditPolicySection
        workspace={workspace}
        connectionId={connectionId}
        online
        canManage
        canApprove
      />,
    );

    expect(await screen.findByText("customers.creditPolicy.loadFailed")).toBeInTheDocument();
    expect(screen.queryByText(/POS API request failed/i)).not.toBeInTheDocument();

    await user.click(screen.getByTestId("business-credit-policy-retry"));
    expect(await screen.findByTestId("business-credit-policy-configure")).toBeInTheDocument();
    expect(getSpy).toHaveBeenCalledTimes(2);
    getSpy.mockRestore();
  });

  it("shows friendly error for 403 and Retry is available", async () => {
    vi.spyOn(businessCreditClient, "getBusinessCustomerCreditPolicy").mockRejectedValue(
      new PosApiError(403, { status: 403, title: "Forbidden", detail: "denied", errorCode: "pos.forbidden" }),
    );

    wrap(
      <BusinessCreditPolicySection
        workspace={workspace}
        connectionId={connectionId}
        online
        canManage
        canApprove
      />,
    );

    expect(await screen.findByText("customers.creditPolicy.loadFailed")).toBeInTheDocument();
    expect(screen.getByTestId("business-credit-policy-retry")).toBeInTheDocument();
    expect(screen.queryByText("denied")).not.toBeInTheDocument();
  });
});
