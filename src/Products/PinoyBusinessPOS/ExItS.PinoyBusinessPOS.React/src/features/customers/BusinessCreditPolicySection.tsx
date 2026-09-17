import {
  approveBusinessCustomerCreditPolicy,
  disableBusinessCustomerCreditPolicy,
  getBusinessCustomerCreditPolicy,
  listBusinessCustomerCreditPolicyHistory,
  type PosBusinessCustomerCreditPolicy,
  upsertBusinessCustomerCreditPolicy,
} from "@/api/pos/pos-business-credit-policy-client";
import { getBusinessCustomerUtangSummary } from "@/api/pos/pos-connected-suppliers-client";
import { PosApiError, type PosWorkspaceScope } from "@/api/pos/pos-http";
import { CreditTermsSection } from "@/features/customers/CreditTermsSection";

export type BusinessCreditPolicySectionProps = {
  workspace: PosWorkspaceScope;
  connectionId: string;
  online: boolean;
  canManage: boolean;
  canApprove: boolean;
  canRecordPayment?: boolean;
  canViewStatement?: boolean;
  onRecordPayment?: () => void;
  onOpenReceivables?: () => void;
  /** Display under dialog title, e.g. "Kizy Bakery · ORG436352". */
  subjectIdentity?: string | null;
  /** When set, skip fetch (used by unit tests). */
  policyOverride?: PosBusinessCustomerCreditPolicy | null;
};

export function BusinessCreditPolicySection(props: BusinessCreditPolicySectionProps) {
  return (
    <CreditTermsSection
      kind="business"
      testIdPrefix="business"
      workspace={props.workspace}
      connectionId={props.connectionId}
      online={props.online}
      canManage={props.canManage}
      canApprove={props.canApprove}
      canRecordPayment={props.canRecordPayment}
      canViewStatement={props.canViewStatement}
      onRecordPayment={props.onRecordPayment}
      onOpenReceivables={props.onOpenReceivables}
      subjectIdentity={props.subjectIdentity}
      policyOverride={props.policyOverride}
      titleKey="customers.business.creditPolicy.title"
      hintKeys={{
        notApproved: "customers.business.creditPolicy.notApprovedHint",
        pending: "customers.business.creditPolicy.pendingHint",
        disabled: "customers.business.creditPolicy.disabledHint",
        allowCreditOff: "customers.business.creditPolicy.allowCreditOffHint",
        allowCreditNeedsSetup: "customers.business.creditPolicy.allowCreditNeedsSetupHint",
      }}
      checkoutNoteKey="customers.business.creditPolicy.checkoutNote"
      sectionQueryPrefix="business-customers"
      statementPath={(connectionId) => `/customers/business/${connectionId}/statement`}
      getPolicy={getBusinessCustomerCreditPolicy}
      listPolicyHistory={listBusinessCustomerCreditPolicyHistory}
      getUtangSummary={getBusinessCustomerUtangSummary}
      upsertPolicy={upsertBusinessCustomerCreditPolicy}
      approvePolicy={approveBusinessCustomerCreditPolicy}
      disablePolicy={disableBusinessCustomerCreditPolicy}
      onPolicyLoadError={(error, connectionId) => {
        if (!import.meta.env.DEV) {
          return;
        }
        if (error instanceof PosApiError) {
          console.warn("[business-credit-policy] load failed", {
            status: error.status,
            errorCode: error.errorCode,
            detail: error.problem.detail,
            connectionId,
            path: `/api/v1/pos/connected-suppliers/business-customers/${connectionId}/credit-policy`,
          });
        } else {
          console.warn("[business-credit-policy] load failed", error);
        }
      }}
    />
  );
}
