import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { PosCustomerCreditPolicy } from "@/api/pos/pos-credit-policy-client";
import { CreditPolicySection } from "@/features/customers/CreditPolicySection";
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
  return render(<QueryClientProvider client={client}>{ui}</QueryClientProvider>);
}

describe("CreditPolicySection", () => {
  it("renders NotConfigured state", () => {
    wrap(
      <CreditPolicySection
        workspace={workspace}
        customerId="11111111-1111-1111-1111-111111111111"
        online
        canManage
        canApprove
        policyOverride={policy({ status: "NotConfigured" })}
      />,
    );
    expect(screen.getByTestId("customer-credit-policy-status")).toHaveTextContent(
      "customers.creditPolicy.status.NotConfigured",
    );
    expect(screen.getByTestId("customer-credit-policy-configure")).toHaveTextContent(
      "customers.creditPolicy.configure",
    );
    expect(screen.getByText("customers.creditPolicy.notApprovedHint")).toBeInTheDocument();
  });

  it("renders PendingApproval with Approve action", () => {
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
      "customers.creditPolicy.status.PendingApproval",
    );
    expect(screen.getByTestId("customer-credit-policy-approve")).toBeInTheDocument();
    expect(screen.getByTestId("customer-credit-policy-disable")).toBeInTheDocument();
  });

  it("renders Approved with Edit and Disable", () => {
    wrap(
      <CreditPolicySection
        workspace={workspace}
        customerId="11111111-1111-1111-1111-111111111111"
        online
        canManage
        canApprove
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
      "customers.creditPolicy.status.Approved",
    );
    expect(screen.getByTestId("customer-credit-policy-configure")).toHaveTextContent(
      "customers.creditPolicy.edit",
    );
    expect(screen.queryByTestId("customer-credit-policy-approve")).not.toBeInTheDocument();
    expect(screen.getByTestId("customer-credit-policy-term")).toHaveTextContent(
      "customers.creditPolicy.termDays",
    );
  });

  it("renders Disabled without Approve", () => {
    wrap(
      <CreditPolicySection
        workspace={workspace}
        customerId="11111111-1111-1111-1111-111111111111"
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
    expect(screen.getByTestId("customer-credit-policy-status")).toHaveTextContent(
      "customers.creditPolicy.status.Disabled",
    );
    expect(screen.queryByTestId("customer-credit-policy-approve")).not.toBeInTheDocument();
    expect(screen.queryByTestId("customer-credit-policy-disable")).not.toBeInTheDocument();
  });
});
