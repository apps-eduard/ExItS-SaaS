import { ExitsPillSelect } from "@/components/exits/ExitsPillSelect";
import { Card } from "@/components/ui/card";
import { cn } from "@/lib/cn";
import type {
  RemainingDecisionAction,
  RemainingDecisionRow,
} from "@/features/purchasing/receive-remaining-decision";

export type ReceiveRemainingQuantityTableProps = {
  title: string;
  summaryText: string;
  applyToAllLabel: string;
  productColLabel: string;
  remainingColLabel: string;
  issueColLabel: string;
  decisionColLabel: string;
  replaceLaterLabel: string;
  cancelRemainingLabel: string;
  rows: readonly RemainingDecisionRow[];
  highlightUnresolved: boolean;
  onDecisionChange: (productId: string, action: RemainingDecisionAction) => void;
  onApplyToAll: (action: RemainingDecisionAction) => void;
  testId?: string;
};

/**
 * Compact remaining-quantity resolution table (per-product decisions + Apply to all).
 */
export function ReceiveRemainingQuantityTable({
  title,
  summaryText,
  applyToAllLabel,
  productColLabel,
  remainingColLabel,
  issueColLabel,
  decisionColLabel,
  replaceLaterLabel,
  cancelRemainingLabel,
  rows,
  highlightUnresolved,
  onDecisionChange,
  onApplyToAll,
  testId = "receive-remaining-decisions",
}: ReceiveRemainingQuantityTableProps) {
  if (rows.length === 0) {
    return null;
  }

  const choiceOptions = [
    { value: "replace_later" as const, label: replaceLaterLabel },
    { value: "cancel_remaining" as const, label: cancelRemainingLabel },
  ];

  return (
    <Card className="receive-remaining-table flex flex-col gap-3 p-3" data-testid={testId}>
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="min-w-0">
          <h2 className="m-0 text-[length:var(--exits-text-md)] font-medium">{title}</h2>
          <p
            className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted"
            data-testid={`${testId}-summary`}
          >
            {summaryText}
          </p>
        </div>
        <div
          className="flex min-w-0 flex-wrap items-center gap-2"
          data-testid={`${testId}-apply-all`}
        >
          <span className="text-[length:var(--exits-text-sm)] text-muted">{applyToAllLabel}</span>
          <ExitsPillSelect<RemainingDecisionAction | "__idle__">
            aria-label={applyToAllLabel}
            value="__idle__"
            onChange={(next) => {
              if (next === "__idle__") {
                return;
              }
              onApplyToAll(next);
            }}
            options={choiceOptions}
            testId={`${testId}-apply`}
          />
        </div>
      </div>

      <div className="receive-remaining-table__desktop overflow-hidden rounded-md border border-border">
        <table className="w-full border-collapse text-start text-[length:var(--exits-text-sm)]">
          <thead>
            <tr className="border-b border-border bg-muted/30">
              <th scope="col" className="px-3 py-2 text-start font-medium">
                {productColLabel}
              </th>
              <th scope="col" className="px-3 py-2 text-start font-medium whitespace-nowrap">
                {remainingColLabel}
              </th>
              <th
                scope="col"
                className="receive-remaining-table__issue-col px-3 py-2 text-start font-medium"
              >
                {issueColLabel}
              </th>
              <th scope="col" className="px-3 py-2 text-start font-medium">
                {decisionColLabel}
              </th>
            </tr>
          </thead>
          <tbody>
            {rows.map((row) => {
              const unresolved = row.remainingAction == null;
              const highlight = highlightUnresolved && unresolved;
              return (
                <tr
                  key={row.productId}
                  className={cn(
                    "border-b border-border last:border-b-0",
                    highlight && "receive-remaining-table__row--unresolved",
                  )}
                  data-testid={`${testId}-row-${row.productId}`}
                  data-unresolved={unresolved ? "true" : "false"}
                >
                  <td className="px-3 py-2 text-start align-middle">
                    <p className="m-0 font-medium">{row.name}</p>
                    {row.sku ? (
                      <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">{row.sku}</p>
                    ) : null}
                    {row.remark ? (
                      <p className="m-0 mt-0.5 text-[length:var(--exits-text-xs)] text-muted md:hidden">
                        {row.issueLabel}
                        {" · "}
                        {row.remark}
                      </p>
                    ) : (
                      <p className="m-0 mt-0.5 text-[length:var(--exits-text-xs)] text-muted md:hidden">
                        {row.issueLabel}
                      </p>
                    )}
                  </td>
                  <td className="px-3 py-2 text-start align-middle tabular-nums whitespace-nowrap">
                    {row.remainingLabel}
                  </td>
                  <td className="receive-remaining-table__issue-col px-3 py-2 text-start align-middle">
                    <p className="m-0">{row.issueLabel}</p>
                    {row.remark ? (
                      <p className="m-0 mt-0.5 text-[length:var(--exits-text-xs)] text-muted">
                        {row.remark}
                      </p>
                    ) : null}
                  </td>
                  <td className="px-3 py-2 text-start align-middle">
                    <ExitsPillSelect<RemainingDecisionAction | "_unset">
                      className="receive-remaining-choice"
                      aria-label={`${row.name}: ${replaceLaterLabel} / ${cancelRemainingLabel}`}
                      value={row.remainingAction ?? "_unset"}
                      onChange={(next) => {
                        if (next === "_unset") {
                          return;
                        }
                        onDecisionChange(row.productId, next);
                      }}
                      options={[
                        {
                          value: "replace_later",
                          label: replaceLaterLabel,
                        },
                        {
                          value: "cancel_remaining",
                          label: cancelRemainingLabel,
                        },
                      ]}
                      testId={`${testId}-choice-${row.productId}`}
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
