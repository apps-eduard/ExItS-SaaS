import { Link } from "react-router-dom";
import { ChevronRight, ReceiptText } from "lucide-react";
import {
  isOpenCashierShift,
  type PosCashierShiftDto,
} from "@/api/pos/pos-shifts-client";
import { StatusChip } from "@/components/exits/StatusChip";
import { useI18n } from "@/i18n/I18nProvider";
import { formatPeso } from "@/lib/format-money";

function formatOpenedWhen(iso: string): string {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) {
    return iso;
  }
  return date.toLocaleString(undefined, {
    month: "short",
    day: "numeric",
    hour: "numeric",
    minute: "2-digit",
  });
}

function formatClosedWhen(iso: string | null | undefined): string {
  if (!iso) {
    return "—";
  }
  return formatOpenedWhen(iso);
}

export type ShiftHistoryResponsiveListProps = {
  shifts: PosCashierShiftDto[];
  resolveCashierName: (actorId: string) => string | null;
  showCashier: boolean;
  showRegister: boolean;
  /** Prefix for row test ids, e.g. `shift-history` or `register-history-shift`. */
  rowTestIdPrefix: string;
  viewShiftTestIdPrefix: string;
  viewTxnsTestIdPrefix: string;
};

/**
 * Mobile cards + desktop table for shift history rows.
 * Same data; no duplicate fetch.
 */
export function ShiftHistoryResponsiveList({
  shifts,
  resolveCashierName,
  showCashier,
  showRegister,
  rowTestIdPrefix,
  viewShiftTestIdPrefix,
  viewTxnsTestIdPrefix,
}: ShiftHistoryResponsiveListProps) {
  const { t } = useI18n();

  if (shifts.length === 0) {
    return null;
  }

  return (
    <>
      <ul
        className="exits-list m-0 grid list-none gap-2 p-0 lg:hidden"
        data-testid={`${rowTestIdPrefix}-cards`}
      >
        {shifts.map((shift) => (
          <ShiftHistoryCard
            key={shift.shiftId}
            shift={shift}
            cashierName={resolveCashierName(shift.actorId)}
            showCashier={showCashier}
            showRegister={showRegister}
            rowTestId={`${rowTestIdPrefix}-${shift.shiftId}`}
            viewShiftTestId={`${viewShiftTestIdPrefix}-${shift.shiftId}`}
            viewTxnsTestId={`${viewTxnsTestIdPrefix}-${shift.shiftId}`}
          />
        ))}
      </ul>

      <div
        className="hidden min-w-0 overflow-x-auto lg:block"
        data-testid={`${rowTestIdPrefix}-table`}
      >
        <table className="w-full min-w-[48rem] border-collapse text-left text-[length:var(--exits-text-sm)]">
          <thead>
            <tr className="border-b border-border">
              <th className="whitespace-nowrap px-2 py-2 text-[length:var(--exits-text-xs)] font-medium text-muted">
                {t("shift.col.shift")}
              </th>
              {showCashier ? (
                <th className="whitespace-nowrap px-2 py-2 text-[length:var(--exits-text-xs)] font-medium text-muted">
                  {t("shift.col.cashier")}
                </th>
              ) : null}
              {showRegister ? (
                <th className="whitespace-nowrap px-2 py-2 text-[length:var(--exits-text-xs)] font-medium text-muted">
                  {t("shift.col.register")}
                </th>
              ) : null}
              <th className="whitespace-nowrap px-2 py-2 text-[length:var(--exits-text-xs)] font-medium text-muted">
                {t("shift.col.opened")}
              </th>
              <th className="hidden whitespace-nowrap px-2 py-2 text-[length:var(--exits-text-xs)] font-medium text-muted xl:table-cell">
                {t("shift.col.closed")}
              </th>
              <th className="whitespace-nowrap px-2 py-2 text-[length:var(--exits-text-xs)] font-medium text-muted">
                {t("shift.col.status")}
              </th>
              <th className="whitespace-nowrap px-2 py-2 text-right text-[length:var(--exits-text-xs)] font-medium text-muted">
                {t("shift.col.transactions")}
              </th>
              <th className="whitespace-nowrap px-2 py-2 text-right text-[length:var(--exits-text-xs)] font-medium text-muted">
                {t("shift.col.sales")}
              </th>
              <th className="hidden whitespace-nowrap px-2 py-2 text-right text-[length:var(--exits-text-xs)] font-medium text-muted 2xl:table-cell">
                {t("shift.col.variance")}
              </th>
              <th className="whitespace-nowrap px-2 py-2 text-[length:var(--exits-text-xs)] font-medium text-muted">
                {t("shift.col.actions")}
              </th>
            </tr>
          </thead>
          <tbody>
            {shifts.map((shift) => {
              const open = isOpenCashierShift(shift);
              const cashierName = resolveCashierName(shift.actorId);
              const registerLabel =
                shift.registerCode || shift.registerName || t("shift.noRegisterOnShift");
              return (
                <tr
                  key={shift.shiftId}
                  className="border-b border-border align-middle hover:bg-muted/40"
                  data-testid={`${rowTestIdPrefix}-table-row-${shift.shiftId}`}
                >
                  <td className="whitespace-nowrap px-2 py-2 font-semibold">{shift.shiftNumber}</td>
                  {showCashier ? (
                    <td className="max-w-[12rem] truncate px-2 py-2">{cashierName ?? "—"}</td>
                  ) : null}
                  {showRegister ? (
                    <td className="max-w-[10rem] truncate px-2 py-2">{registerLabel}</td>
                  ) : null}
                  <td className="whitespace-nowrap px-2 py-2 text-muted">
                    {formatOpenedWhen(shift.openedAtUtc)}
                  </td>
                  <td className="hidden whitespace-nowrap px-2 py-2 text-muted xl:table-cell">
                    {formatClosedWhen(shift.closedAtUtc)}
                  </td>
                  <td className="px-2 py-2">
                    <StatusChip tone={open ? "success" : "info"}>
                      {open ? t("shift.statusOpen") : shift.status}
                    </StatusChip>
                  </td>
                  <td className="px-2 py-2 text-right tabular-nums">
                    {shift.completedTransactionCount ?? "—"}
                  </td>
                  <td className="px-2 py-2 text-right tabular-nums">
                    {shift.completedSalesTotal != null
                      ? formatPeso(shift.completedSalesTotal)
                      : "—"}
                  </td>
                  <td className="hidden px-2 py-2 text-right tabular-nums 2xl:table-cell">
                    {shift.cashVarianceAmount != null
                      ? formatPeso(shift.cashVarianceAmount)
                      : "—"}
                  </td>
                  <td className="px-2 py-2">
                    <div className="flex min-w-0 flex-wrap gap-1.5">
                      <Link
                        to={`/shifts/${shift.shiftId}`}
                        className="inline-flex min-h-8 items-center gap-1 rounded-[var(--exits-radius-md)] border border-border px-2 text-[length:var(--exits-text-xs)] font-medium text-foreground no-underline"
                        data-testid={`${viewShiftTestIdPrefix}-${shift.shiftId}`}
                      >
                        {t("register.viewShift")}
                        <ChevronRight className="size-3" aria-hidden />
                      </Link>
                      <Link
                        to={`/shifts/${shift.shiftId}/transactions`}
                        className="inline-flex min-h-8 items-center gap-1 rounded-[var(--exits-radius-md)] border border-border px-2 text-[length:var(--exits-text-xs)] font-medium text-foreground no-underline"
                        data-testid={`${viewTxnsTestIdPrefix}-${shift.shiftId}`}
                      >
                        <ReceiptText className="size-3 shrink-0" aria-hidden />
                        {t("register.viewTransactions")}
                      </Link>
                    </div>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </>
  );
}

function ShiftHistoryCard({
  shift,
  cashierName,
  showCashier,
  showRegister,
  rowTestId,
  viewShiftTestId,
  viewTxnsTestId,
}: {
  shift: PosCashierShiftDto;
  cashierName: string | null;
  showCashier: boolean;
  showRegister: boolean;
  rowTestId: string;
  viewShiftTestId: string;
  viewTxnsTestId: string;
}) {
  const { t } = useI18n();
  const open = isOpenCashierShift(shift);
  const registerLabel =
    shift.registerCode && shift.registerName
      ? `${shift.registerCode} — ${shift.registerName}`
      : shift.registerCode || shift.registerName || t("shift.noRegisterOnShift");

  return (
    <li>
      <div className="exits-list__card flex min-w-0 flex-col gap-2 p-3" data-testid={rowTestId}>
        <div className="flex min-w-0 flex-wrap items-start justify-between gap-2">
          <div className="min-w-0">
            <p className="m-0 truncate font-semibold">{shift.shiftNumber}</p>
            <p className="mb-0 mt-0.5 text-[length:var(--exits-text-sm)] text-muted">
              {showRegister ? (
                <>
                  {registerLabel}
                  {" · "}
                </>
              ) : null}
              {formatOpenedWhen(shift.openedAtUtc)}
              {showCashier && cashierName ? ` · ${cashierName}` : null}
            </p>
          </div>
          <StatusChip tone={open ? "success" : "info"}>
            {open ? t("shift.statusOpen") : shift.status}
          </StatusChip>
        </div>
        {shift.completedTransactionCount != null || shift.completedSalesTotal != null ? (
          <p className="m-0 text-[length:var(--exits-text-sm)]">
            <span className="text-muted">{t("register.transactionsLabel")}: </span>
            <span className="font-medium tabular-nums">
              {shift.completedTransactionCount ?? 0}
            </span>
            {shift.completedSalesTotal != null ? (
              <>
                <span className="text-muted"> · </span>
                <span className="font-medium tabular-nums">
                  {formatPeso(shift.completedSalesTotal)}
                </span>
              </>
            ) : null}
          </p>
        ) : null}
        <div className="flex min-w-0 flex-wrap gap-2">
          <Link
            to={`/shifts/${shift.shiftId}`}
            className="inline-flex min-h-9 items-center gap-1 rounded-[var(--exits-radius-md)] border border-border px-2.5 text-[length:var(--exits-text-sm)] font-medium text-foreground no-underline"
            data-testid={viewShiftTestId}
          >
            {t("register.viewShift")}
            <ChevronRight className="size-3.5" aria-hidden />
          </Link>
          <Link
            to={`/shifts/${shift.shiftId}/transactions`}
            className="inline-flex min-h-9 items-center gap-1 rounded-[var(--exits-radius-md)] border border-border px-2.5 text-[length:var(--exits-text-sm)] font-medium text-foreground no-underline"
            data-testid={viewTxnsTestId}
          >
            <ReceiptText className="size-3.5 shrink-0" aria-hidden />
            {t("register.viewTransactions")}
          </Link>
        </div>
      </div>
    </li>
  );
}
