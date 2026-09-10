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
  it("renders NotConfigured with Set credit terms", () => {
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
    expect(screen.getByTestId("business-credit-policy-configure")).toHaveTextContent(
      "customers.creditPolicy.setCreditTerms",
    );
    expect(screen.getByText("customers.business.creditPolicy.notApprovedHint")).toBeInTheDocument();
  });

  it("renders PendingApproval with Approve credit and Edit proposed terms", () => {
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
    expect(screen.getByTestId("business-credit-policy-approve")).toHaveTextContent(
      "customers.creditPolicy.approveCredit",
    );
    expect(screen.getByTestId("business-credit-policy-configure")).toHaveTextContent(
      "customers.creditPolicy.editProposedTerms",
    );
    expect(screen.getByTestId("business-credit-policy-disable")).toHaveTextContent(
      "customers.creditPolicy.pauseCredit",
    );
  });

  it("renders Approved with Utang allowed, Edit credit terms, and Pause credit", () => {
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
    expect(screen.getByTestId("business-credit-policy-utang-allowed")).toHaveTextContent(
      "customers.creditPolicy.utangAllowed",
    );
    expect(screen.getByTestId("business-credit-policy-configure")).toHaveTextContent(
      "customers.creditPolicy.editCreditTerms",
    );
    expect(screen.getByTestId("business-credit-policy-disable")).toHaveTextContent(
      "customers.creditPolicy.pauseCredit",
    );
    expect(screen.queryByTestId("business-credit-policy-approve")).not.toBeInTheDocument();
  });

  it("renders Disabled as Paused with Set new credit terms", () => {
    wrap(
      <BusinessCreditPolicySection
        workspace={workspace}
        connectionId={connectionId}
        online
        canManage
        canApprove
        policyOverride={policy({
          status: "Disabled",
          creditLimit: 1000,
          defaultTermDays: 15,
          expectedUpdatedAtUtc: "2026-09-10T00:00:00Z",
        })}
      />,
    );
    expect(screen.getByTestId("business-credit-policy-status")).toHaveTextContent(
      "customers.creditPolicy.status.Disabled",
    );
    expect(screen.getByText("customers.business.creditPolicy.disabledHint")).toBeInTheDocument();
    expect(screen.getByTestId("business-credit-policy-configure")).toHaveTextContent(
      "customers.creditPolicy.setNewCreditTerms",
    );
  });

  it("opens Edit credit terms with term presets, custom input, approval warning, and disabled save until changed", async () => {
    const user = userEvent.setup();
    wrap(
      <BusinessCreditPolicySection
        workspace={workspace}
        connectionId={connectionId}
        online
        canManage
        canApprove
        subjectIdentity="Kizy Bakery · ORG436352"
        policyOverride={policy({
          status: "Approved",
          creditLimit: 5000,
          defaultTermDays: 30,
          expectedUpdatedAtUtc: "2026-09-10T00:00:00Z",
        })}
      />,
    );

    await user.click(screen.getByTestId("business-credit-policy-configure"));

    const dialog = screen.getByTestId("business-credit-policy-dialog-configure");
    expect(dialog).toBeInTheDocument();
    expect(dialog).toHaveTextContent("customers.creditPolicy.editCreditTerms");
    expect(screen.getByTestId("business-credit-policy-dialog-subject")).toHaveTextContent(
      "Kizy Bakery · ORG436352",
    );
    expect(screen.getByTestId("business-credit-policy-reapproval-warning")).toHaveTextContent(
      "customers.creditPolicy.reapprovalWarning",
    );
    expect(dialog).toHaveTextContent("customers.creditPolicy.term");
    expect(dialog).toHaveTextContent("customers.creditPolicy.termExampleHelper");
    for (const days of [7, 15, 30, 60, 90]) {
      expect(screen.getByTestId(`business-credit-policy-term-${days}`)).toHaveTextContent(
        String(days),
      );
    }
    expect(screen.getByTestId("business-credit-policy-dialog-submit")).toBeDisabled();
    expect(screen.getByTestId("business-credit-policy-dialog-submit")).toHaveTextContent(
      "customers.creditPolicy.saveForApproval",
    );

    await user.click(screen.getByTestId("business-credit-policy-term-custom"));
    expect(dialog).toHaveTextContent("customers.creditPolicy.customTerm");
    expect(screen.getByTestId("business-credit-policy-term-custom-input")).toBeInTheDocument();

    await user.clear(screen.getByTestId("business-credit-policy-reason"));
    await user.type(screen.getByTestId("business-credit-policy-reason"), "Adjust terms");
    await user.click(screen.getByTestId("business-credit-policy-term-15"));
    expect(screen.getByTestId("business-credit-policy-dialog-submit")).not.toBeDisabled();
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
