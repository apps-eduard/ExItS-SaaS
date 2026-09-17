import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { PosBusinessCustomerCreditPolicy } from "@/api/pos/pos-business-credit-policy-client";
import * as businessCreditClient from "@/api/pos/pos-business-credit-policy-client";
import * as connectedSuppliersClient from "@/api/pos/pos-connected-suppliers-client";
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
    reservedByActivePos: 0,
    hasEverBeenApproved: false,
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
  it("defaults to Off (NotConfigured) with allow-credit switch off", () => {
    wrap(
      <BusinessCreditPolicySection
        workspace={workspace}
        connectionId={connectionId}
        online
        canManage
        canApprove
        canRecordPayment
        policyOverride={policy({ status: "NotConfigured" })}
      />,
    );
    expect(screen.getByTestId("business-credit-policy-status")).toHaveTextContent(
      "customers.creditPolicy.status.Unavailable",
    );
    const allowSwitch = screen.getByTestId("business-credit-policy-allow-credit");
    expect(allowSwitch).toHaveAttribute("aria-checked", "false");
    expect(allowSwitch).toHaveTextContent("OFF");
    expect(screen.getByTestId("business-credit-policy-allow-credit-hint")).toHaveTextContent(
      "customers.business.creditPolicy.allowCreditOffHint",
    );
    expect(screen.queryByTestId("business-credit-policy-summary")).not.toBeInTheDocument();
    expect(screen.queryByTestId("business-credit-policy-repay")).not.toBeInTheDocument();
    expect(screen.queryByTestId("business-credit-policy-configure")).not.toBeInTheDocument();
    expect(screen.queryByTestId("business-credit-policy-approve")).not.toBeInTheDocument();
    expect(screen.queryByTestId("business-credit-policy-disable")).not.toBeInTheDocument();
  });

  it("renders PendingApproval as Activating with switch on and no approve/pause buttons", () => {
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
    expect(screen.getByTestId("business-credit-policy-status")).toHaveTextContent(
      "customers.creditPolicy.status.NeedsSetup",
    );
    expect(screen.getByTestId("business-credit-policy-allow-credit")).toHaveAttribute(
      "aria-checked",
      "true",
    );
    expect(screen.getByTestId("business-credit-policy-allow-credit")).toHaveTextContent("ON");
    expect(screen.queryByTestId("business-credit-policy-allow-credit-hint")).not.toBeInTheDocument();
    expect(screen.getByTestId("business-credit-policy-configure")).toHaveTextContent(
      "customers.creditPolicy.editProposedTerms",
    );
    expect(screen.queryByTestId("business-credit-policy-approve")).not.toBeInTheDocument();
    expect(screen.queryByTestId("business-credit-policy-disable")).not.toBeInTheDocument();
  });

  it("renders Approved as Active with switch on and metrics", () => {
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
    expect(screen.getByTestId("business-credit-policy-status")).toHaveTextContent(
      "customers.creditPolicy.status.Active",
    );
    expect(screen.getByTestId("business-credit-policy-allow-credit")).toHaveAttribute(
      "aria-checked",
      "true",
    );
    expect(screen.getByTestId("business-credit-policy-allow-credit")).toHaveTextContent("ON");
    expect(screen.queryByTestId("business-credit-policy-allow-credit-hint")).not.toBeInTheDocument();
    expect(screen.getByTestId("business-credit-policy-summary")).toBeInTheDocument();
    expect(screen.getByTestId("business-credit-policy-configure")).toHaveTextContent(
      "customers.creditPolicy.editCreditTerms",
    );
    expect(screen.queryByTestId("business-credit-policy-approve")).not.toBeInTheDocument();
    expect(screen.queryByTestId("business-credit-policy-disable")).not.toBeInTheDocument();
    expect(screen.queryByTestId("business-credit-policy-repay")).not.toBeInTheDocument();
    expect(screen.queryByTestId("business-credit-policy-statement")).not.toBeInTheDocument();
  });

  it("renders Disabled without prior approval as Credit unavailable", () => {
    wrap(
      <BusinessCreditPolicySection
        workspace={workspace}
        connectionId={connectionId}
        online
        canManage
        canApprove
        canRecordPayment
        policyOverride={policy({
          status: "Disabled",
          creditLimit: 1000,
          defaultTermDays: 15,
          hasEverBeenApproved: false,
          expectedUpdatedAtUtc: "2026-09-10T00:00:00Z",
        })}
      />,
    );
    expect(screen.getByTestId("business-credit-policy-status")).toHaveTextContent(
      "customers.creditPolicy.status.Unavailable",
    );
    expect(screen.getByTestId("business-credit-policy-allow-credit")).toHaveAttribute(
      "aria-checked",
      "false",
    );
    expect(screen.getByTestId("business-credit-policy-allow-credit")).toHaveTextContent("OFF");
    expect(screen.getByTestId("business-credit-policy-allow-credit-hint")).toHaveTextContent(
      "customers.business.creditPolicy.allowCreditOffHint",
    );
    expect(screen.queryByTestId("business-credit-policy-summary")).not.toBeInTheDocument();
    expect(screen.queryByTestId("business-credit-policy-repay")).not.toBeInTheDocument();
    expect(screen.queryByTestId("business-credit-policy-configure")).not.toBeInTheDocument();
  });

  it("renders Disabled after approval as Paused", () => {
    wrap(
      <BusinessCreditPolicySection
        workspace={workspace}
        connectionId={connectionId}
        online
        canManage
        canApprove
        canRecordPayment
        policyOverride={policy({
          status: "Disabled",
          creditLimit: 1000,
          defaultTermDays: 15,
          hasEverBeenApproved: true,
          outstandingAmount: 250,
          expectedUpdatedAtUtc: "2026-09-10T00:00:00Z",
          approvedAtUtc: "2026-09-09T00:00:00Z",
        })}
      />,
    );
    expect(screen.getByTestId("business-credit-policy-status")).toHaveTextContent(
      "customers.creditPolicy.status.Paused",
    );
    expect(screen.getByTestId("business-credit-policy-allow-credit")).toHaveAttribute(
      "aria-checked",
      "false",
    );
    expect(screen.getByTestId("business-credit-policy-repay")).toBeInTheDocument();
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

  it("re-enables from Off using a fresh concurrency token without manual reload", async () => {
    const user = userEvent.setup();
    const approved = policy({
      status: "Approved",
      creditLimit: 5000,
      defaultTermDays: 30,
      expectedUpdatedAtUtc: "2026-09-10T00:00:00.000Z",
      updatedAtUtc: "2026-09-10T00:00:00.000Z",
    });
    const disabled = policy({
      status: "Disabled",
      creditLimit: 5000,
      defaultTermDays: 30,
      expectedUpdatedAtUtc: "2026-09-10T00:01:00.000Z",
      updatedAtUtc: "2026-09-10T00:01:00.000Z",
    });
    const pending = policy({
      status: "PendingApproval",
      creditLimit: 5000,
      defaultTermDays: 30,
      expectedUpdatedAtUtc: "2026-09-10T00:02:00.000Z",
      updatedAtUtc: "2026-09-10T00:02:00.000Z",
    });
    const reapproved = policy({
      status: "Approved",
      creditLimit: 5000,
      defaultTermDays: 30,
      expectedUpdatedAtUtc: "2026-09-10T00:03:00.000Z",
      updatedAtUtc: "2026-09-10T00:03:00.000Z",
    });

    const getSpy = vi
      .spyOn(businessCreditClient, "getBusinessCustomerCreditPolicy")
      .mockResolvedValueOnce(approved)
      .mockResolvedValueOnce(disabled) // post-disable invalidate
      .mockResolvedValueOnce(disabled) // enable-from-disabled fresh read
      .mockResolvedValue(reapproved);
    const disableSpy = vi
      .spyOn(businessCreditClient, "disableBusinessCustomerCreditPolicy")
      .mockResolvedValue(disabled);
    const upsertSpy = vi
      .spyOn(businessCreditClient, "upsertBusinessCustomerCreditPolicy")
      .mockResolvedValue(pending);
    const approveSpy = vi
      .spyOn(businessCreditClient, "approveBusinessCustomerCreditPolicy")
      .mockResolvedValue(reapproved);
    vi.spyOn(connectedSuppliersClient, "getBusinessCustomerUtangSummary").mockResolvedValue({
      outstandingAmount: 0,
      pendingCheckAmount: 0,
      availableCredit: 5000,
    } as never);

    wrap(
      <BusinessCreditPolicySection
        workspace={workspace}
        connectionId={connectionId}
        online
        canManage
        canApprove
      />,
    );

    expect(await screen.findByTestId("business-credit-policy-allow-credit")).toHaveAttribute(
      "aria-checked",
      "true",
    );

    await user.click(screen.getByTestId("business-credit-policy-allow-credit"));
    await user.click(screen.getByTestId("business-credit-policy-allow-credit-confirm-confirm"));
    expect(await screen.findByTestId("business-credit-policy-allow-credit")).toHaveAttribute(
      "aria-checked",
      "false",
    );
    expect(disableSpy).toHaveBeenCalledWith(
      workspace,
      connectionId,
      expect.objectContaining({ expectedUpdatedAtUtc: "2026-09-10T00:00:00.000Z" }),
    );

    await user.click(screen.getByTestId("business-credit-policy-allow-credit"));
    await user.click(screen.getByTestId("business-credit-policy-allow-credit-confirm-confirm"));
    expect(await screen.findByTestId("business-credit-policy-allow-credit")).toHaveAttribute(
      "aria-checked",
      "true",
    );
    expect(upsertSpy).toHaveBeenCalledWith(
      workspace,
      connectionId,
      expect.objectContaining({
        creditLimit: 5000,
        defaultTermDays: 30,
        expectedUpdatedAtUtc: "2026-09-10T00:01:00.000Z",
      }),
    );
    expect(approveSpy).toHaveBeenCalledWith(
      workspace,
      connectionId,
      expect.objectContaining({ expectedUpdatedAtUtc: "2026-09-10T00:02:00.000Z" }),
    );
    expect(screen.queryByText(/changed concurrently/i)).not.toBeInTheDocument();
    expect(getSpy.mock.calls.length).toBeGreaterThanOrEqual(2);
  });

  it("opens configure from allow-credit switch when NotConfigured", async () => {
    const user = userEvent.setup();
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

    await user.click(screen.getByTestId("business-credit-policy-allow-credit"));
    expect(screen.getByTestId("business-credit-policy-allow-credit-confirm")).toBeInTheDocument();
    await user.click(screen.getByTestId("business-credit-policy-allow-credit-confirm-confirm"));
    expect(screen.getByTestId("business-credit-policy-dialog-configure")).toBeInTheDocument();
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
    expect(screen.getByTestId("business-credit-policy-allow-credit")).toBeDisabled();
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
    expect(await screen.findByTestId("business-credit-policy-allow-credit")).toBeInTheDocument();
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
