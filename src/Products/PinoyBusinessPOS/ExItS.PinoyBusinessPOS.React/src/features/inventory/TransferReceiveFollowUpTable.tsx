import { ExitsPillSelect } from "@/components/exits/ExitsPillSelect";
import { Card } from "@/components/ui/card";
import { cn } from "@/lib/cn";
import type {
  TransferDamagedOtherFollowUp,
  TransferFollowUpRow,
  TransferMissingFollowUp,
} from "@/features/inventory/transfer-receive-follow-up";

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
  linkedStockRequest: boolean;
  rows: readonly TransferFollowUpRow[];
  highlightUnresolved: boolean;
  onDecisionChange: (
    rowKey: string,
    action: TransferMissingFollowUp | TransferDamagedOtherFollowUp,
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
  linkedStockRequest,
  rows,
  highlightUnresolved,
  onDecisionChange,
  testId = "transfer-receive-follow-up",
}: TransferReceiveFollowUpTableProps) {
  if (rows.length === 0) {
    return null;
  }

  return (
    <Card className="receive-remaining-table flex flex-col gap-3 p-3" data-testid={testId}>
      <div className="min-w-0">
        <h2 className="m-0 text-[length:var(--exits-text-md)] font-medium">{title}</h2>
        <p className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted" data-testid={`${testId}-summary`}>
          {summaryText}
        </p>
      </div>

      <div className="receive-remaining-table__desktop overflow-hidden rounded-md border border-border">
        <table className="w-full border-collapse text-start text-[length:var(--exits-text-sm)]">
          <thead>
            <tr className="border-b border-border bg-muted/30">
              <th scope="col" className="px-3 py-2 text-start font-medium">
                {productColLabel}
              </th>
              <th scope="col" className="px-3 py-2 text-start font-medium whitespace-nowrap">
                {qtyColLabel}
              </th>
              <th scope="col" className="receive-remaining-table__issue-col px-3 py-2 text-start font-medium">
                {issueColLabel}
              </th>
              <th scope="col" className="px-3 py-2 text-start font-medium">
                {decisionColLabel}
              </th>
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
              const damagedOtherOptions = [
                ...(linkedStockRequest
                  ? [{ value: "request_replacement" as const, label: requestReplacementLabel }]
                  : []),
                { value: "accept_shortage" as const, label: acceptShortageLabel },
              ];
              const options = row.issueKind === "missing" ? missingOptions : damagedOtherOptions;

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
                    {row.remark ? (
                      <p className="m-0 mt-0.5 text-[length:var(--exits-text-xs)] text-muted">{row.remark}</p>
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
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </Card>
  );
}
