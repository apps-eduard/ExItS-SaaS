import { ExitsPillSelect } from "@/components/exits/ExitsPillSelect";
import { Card } from "@/components/ui/card";
import { cn } from "@/lib/cn";
import type { InventoryTransferDamagedCustodyDecisionCode } from "@/api/pos/pos-inventory-transfer-client";
import type {
  TransferDamagedOtherFollowUp,
  TransferFollowUpRow,
  TransferMissingFollowUp,
} from "@/features/inventory/transfer-receive-follow-up";
import { forcesReturnToSource } from "@/features/inventory/transfer-exception-custody-policy";

export type TransferReceiveFollowUpTableProps = {
  title: string;
  summaryText: string;
  productColLabel: string;
  qtyColLabel: string;
  issueColLabel: string;
  decisionColLabel: string;
  waitOriginalLabel: string;
  requestReplacementLabel: string;
  acceptShortageLabel: string;
  keepAtDestinationLabel?: string;
  returnToSourceLabel?: string;
  custodyDecisionColLabel?: string;
  otherCustodyDecisionColLabel?: string;
  allowCustodyDecision?: boolean;
  custodyDecisionByProductId?: ReadonlyMap<string, InventoryTransferDamagedCustodyDecisionCode | null>;
  otherCustodyDecisionByProductId?: ReadonlyMap<
    string,
    InventoryTransferDamagedCustodyDecisionCode | null
  >;
  linkedStockRequest: boolean;
  rows: readonly TransferFollowUpRow[];
  highlightUnresolved: boolean;
  onDecisionChange: (
    rowKey: string,
    action: TransferMissingFollowUp | TransferDamagedOtherFollowUp,
  ) => void;
  onCustodyDecisionChange?: (
    productId: string,
    decision: InventoryTransferDamagedCustodyDecisionCode,
  ) => void;
  onOtherCustodyDecisionChange?: (
    productId: string,
    decision: InventoryTransferDamagedCustodyDecisionCode,
  ) => void;
  testId?: string;
};

export function TransferReceiveFollowUpTable({
  title,
  summaryText,
  productColLabel,
  qtyColLabel,
  issueColLabel,
  decisionColLabel,
  waitOriginalLabel,
  requestReplacementLabel,
  acceptShortageLabel,
  keepAtDestinationLabel = "Keep at destination",
  returnToSourceLabel = "Return to source",
  custodyDecisionColLabel = "Damage custody",
  otherCustodyDecisionColLabel = "Exception custody",
  allowCustodyDecision = false,
  custodyDecisionByProductId,
  otherCustodyDecisionByProductId,
  linkedStockRequest,
  rows,
  highlightUnresolved,
  onDecisionChange,
  onCustodyDecisionChange,
  onOtherCustodyDecisionChange,
  testId = "transfer-receive-follow-up",
}: TransferReceiveFollowUpTableProps) {
  if (rows.length === 0) {
    return null;
  }

  const showCustodyCol =
    allowCustodyDecision &&
    rows.some((row) => row.issueKind === "damaged" || row.issueKind === "other");

  return (
    <Card className="receive-remaining-table flex flex-col gap-3 p-3" data-testid={testId}>
      <div className="min-w-0">
        <h2 className="m-0 text-[length:var(--exits-text-md)] font-medium">{title}</h2>
        <p className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted" data-testid={`${testId}-summary`}>
          {summaryText}
        </p>
      </div>

      <div className="receive-remaining-table__desktop overflow-x-auto rounded-md border border-border">
        <table className="receive-remaining-table__grid w-full min-w-[40rem] border-collapse text-start text-[length:var(--exits-text-sm)]">
          <thead>
            <tr className="border-b border-border bg-muted/30">
              <th scope="col" className="receive-remaining-table__head px-3 py-2 text-start font-medium">
                {productColLabel}
              </th>
              <th scope="col" className="receive-remaining-table__head px-3 py-2 text-start font-medium">
                {qtyColLabel}
              </th>
              <th scope="col" className="receive-remaining-table__issue-col receive-remaining-table__head px-3 py-2 text-start font-medium">
                {issueColLabel}
              </th>
              <th scope="col" className="receive-remaining-table__head px-3 py-2 text-start font-medium">
                {decisionColLabel}
              </th>
              {showCustodyCol ? (
                <th scope="col" className="receive-remaining-table__head px-3 py-2 text-start font-medium">
                  {custodyDecisionColLabel}
                </th>
              ) : null}
            </tr>
          </thead>
          <tbody>
            {rows.map((row) => {
              const unresolved = row.action == null;
              const highlight = highlightUnresolved && unresolved;
              const missingOptions = [
                { value: "wait_original" as const, label: waitOriginalLabel },
                ...(linkedStockRequest
                  ? [{ value: "request_replacement" as const, label: requestReplacementLabel }]
                  : []),
                { value: "accept_shortage" as const, label: acceptShortageLabel },
              ];
              // Damaged / other: always Request replacement or Accept shortage (branch transfers included).
              const damagedOtherOptions = [
                { value: "request_replacement" as const, label: requestReplacementLabel },
                { value: "accept_shortage" as const, label: acceptShortageLabel },
              ];
              const options = row.issueKind === "missing" ? missingOptions : damagedOtherOptions;
              const custodyValue =
                custodyDecisionByProductId?.get(row.productId) ?? "KeepAtDestination";
              const otherCustodyValue =
                otherCustodyDecisionByProductId?.get(row.productId) ?? "KeepAtDestination";
              const otherForceReturn =
                row.issueKind === "other" &&
                row.otherReasonCode != null &&
                forcesReturnToSource(row.otherReasonCode);

              return (
                <tr
                  key={row.rowKey}
                  className={cn(
                    "border-b border-border last:border-b-0",
                    highlight && "receive-remaining-table__row--unresolved",
                  )}
                  data-testid={`${testId}-row-${row.rowKey}`}
                  data-unresolved={unresolved ? "true" : "false"}
                >
                  <td className="px-3 py-2 text-start align-middle">
                    <p className="m-0 font-medium">{row.name}</p>
                    {row.sku ? (
                      <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">{row.sku}</p>
                    ) : null}
                  </td>
                  <td className="px-3 py-2 text-start align-middle tabular-nums whitespace-nowrap">
                    {row.qtyLabel}
                  </td>
                  <td className="receive-remaining-table__issue-col px-3 py-2 text-start align-middle">
                    <p className="m-0">{row.issueLabel}</p>
                    {row.actualReceivedProductName?.trim() ? (
                      <p
                        className="m-0 mt-0.5 text-[length:var(--exits-text-xs)] text-muted"
                        data-testid={`${testId}-issue-actual-${row.rowKey}`}
                      >
                        {row.actualReceivedProductName.trim()}
                      </p>
                    ) : row.remark ? (
                      <p
                        className="m-0 mt-0.5 text-[length:var(--exits-text-xs)] text-muted"
                        data-testid={`${testId}-issue-remark-${row.rowKey}`}
                      >
                        {row.remark}
                      </p>
                    ) : null}
                  </td>
                  <td className="px-3 py-2 text-start align-middle">
                    <ExitsPillSelect<TransferMissingFollowUp | TransferDamagedOtherFollowUp | "_unset">
                      className="receive-remaining-choice"
                      aria-label={`${row.name}: ${decisionColLabel}`}
                      value={row.action ?? "_unset"}
                      onChange={(next) => {
                        if (next === "_unset") {
                          return;
                        }
                        onDecisionChange(row.rowKey, next);
                      }}
                      options={options}
                      testId={`${testId}-choice-${row.rowKey}`}
                    />
                  </td>
                  {showCustodyCol ? (
                    <td className="px-3 py-2 text-start align-middle">
                      {row.issueKind === "damaged" ? (
                        <ExitsPillSelect<InventoryTransferDamagedCustodyDecisionCode>
                          className="receive-remaining-choice"
                          aria-label={`${row.name}: ${custodyDecisionColLabel}`}
                          value={custodyValue}
                          onChange={(next) => onCustodyDecisionChange?.(row.productId, next)}
                          options={[
                            { value: "KeepAtDestination", label: keepAtDestinationLabel },
                            { value: "ReturnToSource", label: returnToSourceLabel },
                          ]}
                          testId={`${testId}-custody-${row.productId}`}
                        />
                      ) : row.issueKind === "other" ? (
                        <ExitsPillSelect<InventoryTransferDamagedCustodyDecisionCode>
                          className="receive-remaining-choice"
                          aria-label={`${row.name}: ${otherCustodyDecisionColLabel}`}
                          value={otherForceReturn ? "ReturnToSource" : otherCustodyValue}
                          onChange={(next) => {
                            if (otherForceReturn) {
                              return;
                            }
                            onOtherCustodyDecisionChange?.(row.productId, next);
                          }}
                          options={[
                            {
                              value: "KeepAtDestination",
                              label: keepAtDestinationLabel,
                              disabled: otherForceReturn,
                            },
                            { value: "ReturnToSource", label: returnToSourceLabel },
                          ]}
                          testId={`${testId}-custody-other-${row.productId}${otherForceReturn ? "-locked" : ""}`}
                        />
                      ) : (
                        <span className="text-muted">—</span>
                      )}
                    </td>
                  ) : null}
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </Card>
  );
}
