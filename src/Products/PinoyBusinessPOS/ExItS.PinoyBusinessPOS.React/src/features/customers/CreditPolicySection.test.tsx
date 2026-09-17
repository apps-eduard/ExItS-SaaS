import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { PosCustomerCreditPolicy } from "@/api/pos/pos-credit-policy-client";
import { CreditPolicySection } from "@/features/customers/CreditPolicySection";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import type { ReactNode } from "react";
import { MemoryRouter } from "react-router-dom";

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

const workspace = {
  organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  branchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
};

function policy(partial: Partial<PosCustomerCreditPolicy>): PosCustomerCreditPolicy {
  return {
    customerId: "11111111-1111-1111-1111-111111111111",
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
  return render(
    <MemoryRouter>
      <QueryClientProvider client={client}>{ui}</QueryClientProvider>
    </MemoryRouter>,
  );
}

describe("CreditPolicySection", () => {
  it("defaults to Off (NotConfigured) with allow-credit switch", () => {
    wrap(
      <CreditPolicySection
        workspace={workspace}
        customerId="11111111-1111-1111-1111-111111111111"
        online
        canManage
        canApprove
        canRecordPayment
        policyOverride={policy({ status: "NotConfigured" })}
      />,
    );
    expect(screen.getByTestId("customer-credit-policy-status")).toHaveTextContent(
      "customers.creditPolicy.status.Unavailable",
    );
    expect(screen.getByTestId("customer-credit-policy-allow-credit")).toHaveAttribute(
      "aria-checked",
      "false",
    );
    expect(screen.getByTestId("customer-credit-policy-allow-credit")).toHaveTextContent("OFF");
    expect(screen.getByTestId("customer-credit-policy-allow-credit-hint")).toHaveTextContent(
      "customers.creditPolicy.allowCreditOffHint",
    );
    expect(screen.queryByTestId("customer-credit-policy-repay")).not.toBeInTheDocument();
    expect(screen.queryByTestId("customer-credit-policy-configure")).not.toBeInTheDocument();
    expect(screen.queryByTestId("customer-credit-policy-approve")).not.toBeInTheDocument();
    expect(screen.queryByTestId("customer-credit-policy-disable")).not.toBeInTheDocument();
  });

  it("renders PendingApproval as Needs setup with switch on and no approve/pause buttons", async () => {
    const user = userEvent.setup();
    wrap(
      <CreditPolicySection
        workspace={workspace}
        customerId="11111111-1111-1111-1111-111111111111"
        online
        canManage
        canApprove
        policyOverride={policy({
          status: "PendingApproval",
          creditLimit: 5000,
          defaultTermDays: 30,
          expectedUpdatedAtUtc: "2026-09-10T00:00:00Z",
        })}
      />,
    );
    expect(screen.getByTestId("customer-credit-policy-status")).toHaveTextContent(
      "customers.creditPolicy.status.NeedsSetup",
    );
    expect(screen.getByTestId("customer-credit-policy-allow-credit")).toHaveAttribute(
      "aria-checked",
      "true",
    );
    expect(screen.getByTestId("customer-credit-policy-allow-credit")).toHaveTextContent("ON");
    // Terms already exist — switch model does not ask to "complete credit terms".
    expect(screen.queryByTestId("customer-credit-policy-allow-credit-hint")).not.toBeInTheDocument();
    expect(screen.queryByTestId("customer-credit-policy-approve")).not.toBeInTheDocument();
    expect(screen.queryByTestId("customer-credit-policy-disable")).not.toBeInTheDocument();

    await user.click(screen.getByTestId("customer-credit-policy-configure"));
    expect(screen.getByTestId("customer-credit-policy-dialog-submit")).toHaveTextContent(
      "customers.creditPolicy.updateProposedTerms",
    );
    expect(screen.getByTestId("customer-credit-policy-dialog-submit")).toBeDisabled();
    expect(screen.queryByTestId("customer-credit-policy-reapproval-warning")).not.toBeInTheDocument();
  });

  it("renders Approved as Active with switch on, metrics, and edit dialog", async () => {
    const user = userEvent.setup();
    wrap(
      <CreditPolicySection
        workspace={workspace}
        customerId="11111111-1111-1111-1111-111111111111"
        online
        canManage
        canApprove
        canRecordPayment
        canViewStatement
        subjectIdentity="Juan Dela Cruz · PER123456"
        policyOverride={policy({
          status: "Approved",
          creditLimit: 2000,
          defaultTermDays: 90,
          availableCredit: 1500,
          outstandingAmount: 500,
          expectedUpdatedAtUtc: "2026-09-10T00:00:00Z",
          approvedByUserId: "22222222-2222-2222-2222-222222222222",
          approvedAtUtc: "2026-09-09T00:00:00Z",
        })}
      />,
    );
    expect(screen.getByTestId("customer-credit-policy-status")).toHaveTextContent(
      "customers.creditPolicy.status.Active",
    );
    expect(screen.getByTestId("customer-credit-policy-allow-credit")).toHaveAttribute(
      "aria-checked",
      "true",
    );
    expect(screen.getByTestId("customer-credit-policy-allow-credit")).toHaveTextContent("ON");
    expect(screen.getByTestId("customer-credit-policy-summary")).toBeInTheDocument();
    expect(screen.getByText("customers.creditPolicy.checkoutNote")).toBeInTheDocument();
    expect(screen.getByTestId("customer-credit-policy-repay")).toHaveTextContent(
      "customers.recordPayment",
    );
    expect(screen.getByTestId("customer-credit-policy-statement")).toHaveTextContent(
      "customers.viewStatement",
    );
    expect(screen.getByTestId("customer-credit-policy-configure")).toHaveTextContent(
      "customers.creditPolicy.editCreditTerms",
    );
    expect(screen.queryByTestId("customer-credit-policy-approve")).not.toBeInTheDocument();
    expect(screen.queryByTestId("customer-credit-policy-disable")).not.toBeInTheDocument();

    await user.click(screen.getByTestId("customer-credit-policy-configure"));
    const dialog = screen.getByTestId("customer-credit-policy-dialog-configure");
    expect(screen.getByTestId("customer-credit-policy-dialog-subject")).toHaveTextContent(
      "Juan Dela Cruz · PER123456",
    );
    expect(screen.getByTestId("customer-credit-policy-reapproval-warning")).toBeInTheDocument();
    expect(dialog).toHaveTextContent("customers.creditPolicy.term");
    expect(dialog).toHaveTextContent("customers.creditPolicy.reasonForChangeHelper");
    for (const days of [7, 15, 30, 60, 90]) {
      expect(screen.getByTestId(`customer-credit-policy-term-${days}`)).toHaveTextContent(
        String(days),
      );
    }
    expect(screen.getByTestId("customer-credit-policy-dialog-submit")).toHaveTextContent(
      "customers.creditPolicy.saveForApproval",
    );
    expect(screen.getByTestId("customer-credit-policy-dialog-submit")).toBeDisabled();
  });

  it("renders Disabled as Off without approve/pause buttons", () => {
    wrap(
      <CreditPolicySection
        workspace={workspace}
        customerId="11111111-1111-1111-1111-111111111111"
        online
        canManage
        canApprove
        canRecordPayment
        policyOverride={policy({
          status: "Disabled",
          creditLimit: 1000,
          defaultTermDays: 15,
          outstandingAmount: 0,
          expectedUpdatedAtUtc: "2026-09-10T00:00:00Z",
        })}
      />,
    );
    expect(screen.getByTestId("customer-credit-policy-status")).toHaveTextContent(
      "customers.creditPolicy.status.Unavailable",
    );
    expect(screen.getByTestId("customer-credit-policy-allow-credit")).toHaveAttribute(
      "aria-checked",
      "false",
    );
    expect(screen.getByTestId("customer-credit-policy-allow-credit")).toHaveTextContent("OFF");
    expect(screen.queryByTestId("customer-credit-policy-repay")).not.toBeInTheDocument();
    expect(screen.queryByTestId("customer-credit-policy-configure")).not.toBeInTheDocument();
    expect(screen.queryByTestId("customer-credit-policy-approve")).not.toBeInTheDocument();
    expect(screen.queryByTestId("customer-credit-policy-disable")).not.toBeInTheDocument();
  });

  it("shows Record payment when Off but outstanding is greater than zero", () => {
    wrap(
      <CreditPolicySection
        workspace={workspace}
        customerId="11111111-1111-1111-1111-111111111111"
        online
        canManage
        canApprove
        canRecordPayment
        policyOverride={policy({
          status: "Disabled",
          creditLimit: 1000,
          defaultTermDays: 15,
          outstandingAmount: 747,
          expectedUpdatedAtUtc: "2026-09-10T00:00:00Z",
        })}
      />,
    );
    expect(screen.getByTestId("customer-credit-policy-allow-credit")).toHaveAttribute(
      "aria-checked",
      "false",
    );
    expect(screen.getByTestId("customer-credit-policy-repay")).toBeInTheDocument();
  });

  it("hides Record payment when On and outstanding is zero", () => {
    wrap(
      <CreditPolicySection
        workspace={workspace}
        customerId="11111111-1111-1111-1111-111111111111"
        online
        canManage
        canApprove
        canRecordPayment
        policyOverride={policy({
          status: "Approved",
          creditLimit: 2000,
          defaultTermDays: 30,
          outstandingAmount: 0,
          availableCredit: 2000,
          expectedUpdatedAtUtc: "2026-09-10T00:00:00Z",
        })}
      />,
    );
    expect(screen.getByTestId("customer-credit-policy-allow-credit")).toHaveAttribute(
      "aria-checked",
      "true",
    );
    expect(screen.queryByTestId("customer-credit-policy-repay")).not.toBeInTheDocument();
  });
});
