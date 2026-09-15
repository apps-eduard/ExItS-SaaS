import {
  approveCustomerCreditPolicy,
  disableCustomerCreditPolicy,
  getCustomerCreditPolicy,
  listCustomerCreditPolicyHistory,
  type PosCustomerCreditPolicy,
  upsertCustomerCreditPolicy,
} from "@/api/pos/pos-credit-policy-client";
import { getCustomerCreditSummary } from "@/api/pos/pos-customers-client";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { CreditTermsSection } from "@/features/customers/CreditTermsSection";

export type CreditPolicySectionProps = {
  workspace: PosWorkspaceScope;
  customerId: string;
  online: boolean;
  canManage: boolean;
  canApprove: boolean;
  canRecordPayment?: boolean;
  canViewStatement?: boolean;
  onRecordPayment?: () => void;
  /** Display under dialog title, e.g. "Juan Dela Cruz · PER123456". */
  subjectIdentity?: string | null;
  /** When set, skip fetch (used by unit tests). */
  policyOverride?: PosCustomerCreditPolicy | null;
};

export function CreditPolicySection(props: CreditPolicySectionProps) {
  return (
    <CreditTermsSection
      kind="personal"
      testIdPrefix="customer"
      workspace={props.workspace}
      customerId={props.customerId}
      online={props.online}
      canManage={props.canManage}
      canApprove={props.canApprove}
      canRecordPayment={props.canRecordPayment}
      canViewStatement={props.canViewStatement}
      onRecordPayment={props.onRecordPayment}
      subjectIdentity={props.subjectIdentity}
      policyOverride={props.policyOverride}
      titleKey="customers.creditPolicy.title"
      hintKeys={{
        notApproved: "customers.creditPolicy.notApprovedHint",
        pending: "customers.creditPolicy.pendingHint",
        disabled: "customers.creditPolicy.disabledHint",
      }}
      checkoutNoteKey="customers.creditPolicy.checkoutNote"
      sectionQueryPrefix="customers"
      statementPath={(customerId) => `/customers/${customerId}/statement`}
      getPolicy={getCustomerCreditPolicy}
      listPolicyHistory={listCustomerCreditPolicyHistory}
      getUtangSummary={getCustomerCreditSummary}
      upsertPolicy={upsertCustomerCreditPolicy}
      approvePolicy={approveCustomerCreditPolicy}
      disablePolicy={disableCustomerCreditPolicy}
    />
  );
}
