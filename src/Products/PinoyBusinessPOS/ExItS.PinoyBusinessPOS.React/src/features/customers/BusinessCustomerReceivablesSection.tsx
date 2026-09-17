import { useEffect, useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import {
  listBusinessCustomerReceivables,
  type BusinessReceivable,
} from "@/api/pos/pos-connected-suppliers-client";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { B2bObligationsView } from "@/features/b2b-obligations/B2bObligationsView";
import {
  mapReceivableToObligation,
  type B2bObligationListFilter,
} from "@/features/b2b-obligations/b2b-obligations-model";
import { RecordPaymentModal } from "@/features/customers/RecordPaymentModal";

type BusinessCustomerReceivablesSectionProps = {
  workspace: PosWorkspaceScope;
  connectionId: string;
  online: boolean;
  displayName: string;
  outstandingBalance: number;
  canRecordPayment: boolean;
  focusToken?: string | number | null;
  initialFilter?: B2bObligationListFilter;
};

export function BusinessCustomerReceivablesSection({
  workspace,
  connectionId,
  online,
  displayName,
  outstandingBalance,
  canRecordPayment,
  focusToken = null,
  initialFilter = "open",
}: BusinessCustomerReceivablesSectionProps) {
  const [filter, setFilter] = useState<B2bObligationListFilter>(initialFilter);
  const [paymentCreditEntryId, setPaymentCreditEntryId] = useState<string | null>(null);
  const [paymentOpen, setPaymentOpen] = useState(false);

  useEffect(() => {
    if (focusToken == null || focusToken === "" || focusToken === 0) {
      return;
    }
    setFilter("open");
  }, [focusToken]);

  const receivablesQuery = useQuery({
    queryKey: ["business-customers", "receivables", workspace.organizationId, connectionId],
    enabled: online,
    queryFn: ({ signal }) => listBusinessCustomerReceivables(workspace, connectionId, signal),
  });

  const obligations = useMemo(
    () =>
      (receivablesQuery.data ?? []).map((item: BusinessReceivable) =>
        mapReceivableToObligation(item),
      ),
    [receivablesQuery.data],
  );

  return (
    <>
      <B2bObligationsView
        perspective="receivable"
        items={obligations}
        isLoading={receivablesQuery.isLoading}
        isError={receivablesQuery.isError}
        filter={filter}
        onFilterChange={setFilter}
        focusToken={focusToken}
        canRecordPayment={canRecordPayment}
        onRecordPayment={(id) => {
          setPaymentCreditEntryId(id);
          setPaymentOpen(true);
        }}
        testIdPrefix="business-receivables"
      />
      {canRecordPayment ? (
        <RecordPaymentModal
          open={paymentOpen}
          onOpenChange={(open) => {
            setPaymentOpen(open);
            if (!open) {
              setPaymentCreditEntryId(null);
            }
          }}
          customerKind="business"
          connectionId={connectionId}
          displayName={displayName}
          outstandingBalance={outstandingBalance}
          preselectedCreditEntryId={paymentCreditEntryId}
          onSuccess={() => {
            void receivablesQuery.refetch();
          }}
        />
      ) : null}
    </>
  );
}
