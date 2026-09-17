import { useEffect, useMemo, useState, type ReactNode } from "react";
import { Link } from "react-router-dom";
import { Eye, Wallet } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { ExitsChipBar, type ExitsChipItem } from "@/components/exits/ExitsChipBar";
import { ExitsResponsiveDataView } from "@/components/exits/ExitsResponsiveDataView";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { StatusChip } from "@/components/exits/StatusChip";
import { useResponsiveDataLayout } from "@/components/exits/useResponsiveDataLayout";
import {
  B2B_OBLIGATION_PAGE_SIZE,
  countB2bObligationsByFilter,
  filterB2bObligations,
  paginateB2bObligations,
  resolveB2bObligationSourceHref,
  type B2bObligationItem,
  type B2bObligationListFilter,
  type B2bObligationPerspective,
} from "@/features/b2b-obligations/b2b-obligations-model";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";

function statusTone(
  status: string,
  isOverdue: boolean,
): "success" | "warning" | "info" | "danger" {
  if (status === "Paid") {
    return "success";
  }
  if (status === "Voided" || status === "Reversed") {
    return "warning";
  }
  if (isOverdue) {
    return "danger";
  }
  if (status === "PartiallyPaid") {
    return "info";
  }
  return "warning";
}

function statusLabelKey(status: string): MessageKey {
  switch (status) {
    case "Open":
      return "b2bObligations.status.open";
    case "PartiallyPaid":
      return "b2bObligations.status.partiallyPaid";
    case "Paid":
      return "b2bObligations.status.paid";
    case "Voided":
      return "b2bObligations.status.voided";
    case "Reversed":
      return "b2bObligations.status.reversed";
    default:
      return "b2bObligations.status.open";
  }
}

function sourceTypeLabelKey(sourceType: string): MessageKey {
  if (sourceType === "DirectPurchaseReceipt") {
    return "b2bObligations.source.directPurchase";
  }
  if (sourceType === "Sale") {
    return "b2bObligations.source.sale";
  }
  if (sourceType === "GoodsReceipt") {
    return "b2bObligations.source.goodsReceipt";
  }
  return "b2bObligations.source.other";
}

function formatDueDate(value: string | null | undefined): string {
  if (!value) {
    return "—";
  }
  const trimmed = value.trim();
  if (/^\d{4}-\d{2}-\d{2}/.test(trimmed)) {
    return trimmed.slice(0, 10);
  }
  return trimmed;
}

function formatTransactionDate(iso: string): string {
  try {
    return new Date(iso).toLocaleDateString(undefined, {
      year: "numeric",
      month: "short",
      day: "numeric",
    });
  } catch {
    return iso;
  }
}

function ObligationStatusChips({
  item,
  t,
}: {
  item: B2bObligationItem;
  t: (key: MessageKey) => string;
}) {
  return (
    <div className="flex flex-wrap items-center gap-1.5">
      <StatusChip tone={statusTone(item.status, item.isOverdue)}>
        {t(statusLabelKey(item.status))}
      </StatusChip>
      {item.isOverdue &&
      item.status !== "Paid" &&
      item.status !== "Voided" &&
      item.status !== "Reversed" ? (
        <StatusChip tone="danger">{t("b2bObligations.overdueBadge")}</StatusChip>
      ) : null}
    </div>
  );
}

function ObligationActions({
  item,
  perspective,
  testIdPrefix,
  canRecordPayment,
  onRecordPayment,
  resolvePayNow,
  onPayNow,
  t,
  compact = false,
}: {
  item: B2bObligationItem;
  perspective: B2bObligationPerspective;
  testIdPrefix: string;
  canRecordPayment: boolean;
  onRecordPayment?: (obligationId: string) => void;
  resolvePayNow?: (item: B2bObligationItem) => "hidden" | "unavailable" | "pay_now";
  onPayNow?: (obligationId: string) => void;
  t: (key: MessageKey) => string;
  compact?: boolean;
}) {
  const sourceHref = resolveB2bObligationSourceHref(
    perspective,
    item.sourceType,
    item.sourceId,
  );
  const payCta = resolvePayNow?.(item) ?? "hidden";
  const canRepayThis =
    canRecordPayment &&
    perspective === "receivable" &&
    (item.status === "Open" || item.status === "PartiallyPaid") &&
    item.balance > 0;
  const btnClass = compact
    ? "supplier-detail-action-btn h-8 px-2 text-[length:var(--exits-text-xs)]"
    : "supplier-detail-action-btn";

  return (
    <div className={`flex flex-wrap gap-2 ${compact ? "justify-end" : ""}`}>
      {canRepayThis ? (
        <Button
          type="button"
          variant="success"
          className={btnClass}
          data-testid={`${testIdPrefix}-record-payment-${item.id}`}
          onClick={() => onRecordPayment?.(item.id)}
        >
          <Wallet className="size-4 shrink-0" aria-hidden />
          {t("b2bObligations.recordPayment")}
        </Button>
      ) : null}

      {payCta === "pay_now" ? (
        <Button
          type="button"
          className={btnClass}
          data-testid={`${testIdPrefix}-pay-now-${item.id}`}
          disabled={!onPayNow}
          title={onPayNow ? undefined : t("b2bObligations.payNowNotReady")}
          onClick={() => onPayNow?.(item.id)}
        >
          <Wallet className="size-4 shrink-0" aria-hidden />
          {t("b2bObligations.payNow")}
        </Button>
      ) : null}

      {sourceHref ? (
        <Button type="button" variant="outline" className={btnClass} asChild>
          <Link to={sourceHref} data-testid={`${testIdPrefix}-view-details-${item.id}`}>
            <Eye className="size-4 shrink-0" aria-hidden />
            {t("b2bObligations.viewDetails")}
          </Link>
        </Button>
      ) : (
        <Button
          type="button"
          variant="outline"
          className={btnClass}
          disabled
          data-testid={`${testIdPrefix}-view-details-${item.id}`}
          title={t("b2bObligations.viewDetailsUnavailable")}
        >
          <Eye className="size-4 shrink-0" aria-hidden />
          {t("b2bObligations.viewDetails")}
        </Button>
      )}
    </div>
  );
}

export type B2bObligationsViewProps = {
  perspective: B2bObligationPerspective;
  items: readonly B2bObligationItem[];
  isLoading?: boolean;
  isError?: boolean;
  /** Controlled filter; defaults to open. */
  filter?: B2bObligationListFilter;
  onFilterChange?: (filter: B2bObligationListFilter) => void;
  /** When set, scrolls this section into view (e.g. Open receivables card click). */
  focusToken?: string | number | null;
  testIdPrefix?: string;
  /** Seller-only: record payment for an open obligation. */
  onRecordPayment?: (obligationId: string) => void;
  canRecordPayment?: boolean;
  /** Buyer-only: show Pay now when truly available. */
  resolvePayNow?: (item: B2bObligationItem) => "hidden" | "unavailable" | "pay_now";
  onPayNow?: (obligationId: string) => void;
};

export function B2bObligationsView({
  perspective,
  items,
  isLoading = false,
  isError = false,
  filter: controlledFilter,
  onFilterChange,
  focusToken,
  testIdPrefix = perspective === "receivable" ? "b2b-receivables" : "b2b-payables",
  onRecordPayment,
  canRecordPayment = false,
  resolvePayNow,
  onPayNow,
}: B2bObligationsViewProps) {
  const { t } = useI18n();
  const { layout } = useResponsiveDataLayout();
  const [internalFilter, setInternalFilter] = useState<B2bObligationListFilter>("open");
  const [visibleCount, setVisibleCount] = useState(B2B_OBLIGATION_PAGE_SIZE);
  const filter = controlledFilter ?? internalFilter;

  function setFilter(next: B2bObligationListFilter) {
    onFilterChange?.(next);
    if (controlledFilter === undefined) {
      setInternalFilter(next);
    }
    setVisibleCount(B2B_OBLIGATION_PAGE_SIZE);
  }

  useEffect(() => {
    setVisibleCount(B2B_OBLIGATION_PAGE_SIZE);
  }, [items, filter]);

  useEffect(() => {
    if (focusToken == null || focusToken === "") {
      return;
    }
    const el = document.getElementById(`${testIdPrefix}-section`);
    el?.scrollIntoView({ behavior: "smooth", block: "start" });
  }, [focusToken, testIdPrefix]);

  const counts = useMemo(() => countB2bObligationsByFilter(items), [items]);
  const filtered = useMemo(() => filterB2bObligations(items, filter), [items, filter]);
  const { visible, hasMore } = useMemo(
    () => paginateB2bObligations(filtered, visibleCount),
    [filtered, visibleCount],
  );

  const titleKey: MessageKey =
    perspective === "receivable" ? "b2bObligations.receivablesTitle" : "b2bObligations.payablesTitle";
  const paidAtLabelKey: MessageKey =
    perspective === "receivable"
      ? "b2bObligations.paidAtSource"
      : "b2bObligations.paidAtReceipt";

  const filterChips: ExitsChipItem[] = [
    {
      key: "open",
      label: t("b2bObligations.filter.open"),
      count: counts.open,
      state: filter === "open" ? "active" : "idle",
      testId: `${testIdPrefix}-filter-open`,
      onSelect: () => setFilter("open"),
    },
    {
      key: "overdue",
      label: t("b2bObligations.filter.overdue"),
      count: counts.overdue,
      state: filter === "overdue" ? "active" : "idle",
      testId: `${testIdPrefix}-filter-overdue`,
      onSelect: () => setFilter("overdue"),
    },
    {
      key: "paid",
      label: t("b2bObligations.filter.paid"),
      count: counts.paid,
      state: filter === "paid" ? "active" : "idle",
      testId: `${testIdPrefix}-filter-paid`,
      onSelect: () => setFilter("paid"),
    },
    {
      key: "all",
      label: t("b2bObligations.filter.all"),
      count: counts.all,
      state: filter === "all" ? "active" : "idle",
      testId: `${testIdPrefix}-filter-all`,
      onSelect: () => setFilter("all"),
    },
  ];

  const actionProps = {
    perspective,
    testIdPrefix,
    canRecordPayment,
    onRecordPayment,
    resolvePayNow,
    onPayNow,
    t,
  };

  let body: ReactNode = null;
  if (!isLoading && !isError && visible.length > 0) {
    const listBody = (
      <ul
        className="m-0 flex list-none flex-col gap-3 p-0"
        data-testid={`${testIdPrefix}-list`}
      >
        {visible.map((item) => {
          const sourceLabel =
            item.sourceReference?.trim() || t(sourceTypeLabelKey(item.sourceType));
          return (
            <li
              key={item.id}
              className="supplier-payable-item"
              data-testid={`${testIdPrefix}-item-${item.id}`}
              data-status={item.status}
            >
              <ObligationStatusChips item={item} t={t} />
              <p className="mt-2 mb-0.5 text-[length:var(--exits-text-sm)] text-muted">
                {t(sourceTypeLabelKey(item.sourceType))}
                {` · ${formatTransactionDate(item.transactionDateUtc)}`}
              </p>
              <p
                className="m-0 mb-2 text-[length:var(--exits-text-sm)] font-medium"
                data-testid={`${testIdPrefix}-source-ref-${item.id}`}
              >
                {sourceLabel}
              </p>
              <dl className="m-0 grid gap-2 text-[length:var(--exits-text-sm)] sm:grid-cols-2">
                <div>
                  <dt className="text-muted">{t("b2bObligations.originalAmount")}</dt>
                  <dd className="m-0 tabular-nums">
                    <MoneyDisplay amount={item.originalAmount} />
                  </dd>
                </div>
                <div>
                  <dt className="text-muted">{t(paidAtLabelKey)}</dt>
                  <dd className="m-0 tabular-nums">
                    <MoneyDisplay amount={item.paidAtSourceAmount} />
                  </dd>
                </div>
                <div>
                  <dt className="text-muted">{t("b2bObligations.laterPayments")}</dt>
                  <dd className="m-0 tabular-nums">
                    <MoneyDisplay amount={item.laterPaymentsAmount} />
                  </dd>
                </div>
                <div>
                  <dt className="text-muted">{t("b2bObligations.balance")}</dt>
                  <dd className="m-0 font-semibold tabular-nums">
                    <MoneyDisplay amount={item.balance} />
                  </dd>
                </div>
                <div>
                  <dt className="text-muted">{t("b2bObligations.dueDate")}</dt>
                  <dd className="m-0">{formatDueDate(item.dueDate)}</dd>
                </div>
              </dl>
              <div className="mt-3">
                <ObligationActions item={item} {...actionProps} />
              </div>
            </li>
          );
        })}
      </ul>
    );

    const tableBody = (
      <div className="b2b-obligations-table-wrap min-w-0">
        <table
          className="b2b-obligations-table w-full border-collapse text-left text-[length:var(--exits-text-sm)]"
          data-testid={`${testIdPrefix}-table`}
        >
          <thead>
            <tr className="border-b border-border bg-surface">
              <th className="px-2 py-2 font-medium text-muted">{t("b2bObligations.col.status")}</th>
              <th className="px-2 py-2 font-medium text-muted">{t("b2bObligations.col.source")}</th>
              <th className="px-2 py-2 font-medium text-muted tabular-nums">
                {t("b2bObligations.originalAmount")}
              </th>
              <th className="px-2 py-2 font-medium text-muted tabular-nums">{t(paidAtLabelKey)}</th>
              <th className="px-2 py-2 font-medium text-muted tabular-nums">
                {t("b2bObligations.laterPayments")}
              </th>
              <th className="px-2 py-2 font-medium text-muted tabular-nums">
                {t("b2bObligations.balance")}
              </th>
              <th className="px-2 py-2 font-medium text-muted">{t("b2bObligations.dueDate")}</th>
              <th className="px-2 py-2 font-medium text-muted">{t("b2bObligations.col.actions")}</th>
            </tr>
          </thead>
          <tbody>
            {visible.map((item) => {
              const sourceLabel =
                item.sourceReference?.trim() || t(sourceTypeLabelKey(item.sourceType));
              return (
                <tr
                  key={item.id}
                  className="border-b border-border last:border-b-0"
                  data-testid={`${testIdPrefix}-row-${item.id}`}
                  data-status={item.status}
                >
                  <td className="px-2 py-2.5 align-middle">
                    <ObligationStatusChips item={item} t={t} />
                  </td>
                  <td className="px-2 py-2.5 align-middle">
                    <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                      {t(sourceTypeLabelKey(item.sourceType))}
                      {` · ${formatTransactionDate(item.transactionDateUtc)}`}
                    </p>
                    <p
                      className="m-0 mt-0.5 font-medium"
                      data-testid={`${testIdPrefix}-source-ref-${item.id}`}
                    >
                      {sourceLabel}
                    </p>
                  </td>
                  <td className="px-2 py-2.5 align-middle tabular-nums">
                    <MoneyDisplay amount={item.originalAmount} />
                  </td>
                  <td className="px-2 py-2.5 align-middle tabular-nums">
                    <MoneyDisplay amount={item.paidAtSourceAmount} />
                  </td>
                  <td className="px-2 py-2.5 align-middle tabular-nums">
                    <MoneyDisplay amount={item.laterPaymentsAmount} />
                  </td>
                  <td className="px-2 py-2.5 align-middle font-semibold tabular-nums">
                    <MoneyDisplay amount={item.balance} />
                  </td>
                  <td className="px-2 py-2.5 align-middle whitespace-nowrap">
                    {formatDueDate(item.dueDate)}
                  </td>
                  <td className="px-2 py-2.5 align-middle">
                    <ObligationActions item={item} {...actionProps} compact />
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    );

    body = (
      <ExitsResponsiveDataView
        layout={layout}
        testId={`${testIdPrefix}-responsive`}
        className="b2b-obligations-responsive mt-1"
        table={layout === "table" ? tableBody : null}
        list={layout === "list" ? listBody : null}
      />
    );
  }

  return (
    <Card
      className="supplier-credit-card"
      id={`${testIdPrefix}-section`}
      data-testid={`${testIdPrefix}-section`}
      data-perspective={perspective}
    >
      <div className="supplier-credit-card__header">
        <h3 className="m-0 text-[length:var(--exits-text-sm)] font-semibold">{t(titleKey)}</h3>
      </div>

      <ExitsChipBar
        variant="filter"
        ariaLabel={t("b2bObligations.filterAria")}
        testId={`${testIdPrefix}-filters`}
        items={filterChips}
      />

      {isLoading ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("loading.label")}</p>
      ) : null}

      {isError ? (
        <p
          className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]"
          data-testid={`${testIdPrefix}-error`}
        >
          {t("b2bObligations.loadFailed")}
        </p>
      ) : null}

      {!isLoading && !isError && visible.length === 0 ? (
        <p
          className="m-0 text-[length:var(--exits-text-sm)] text-muted"
          data-testid={`${testIdPrefix}-empty`}
        >
          {filter === "open" ? t("b2bObligations.emptyOpen") : t("b2bObligations.empty")}
        </p>
      ) : null}

      {body}

      {hasMore ? (
        <div className="mt-3">
          <Button
            type="button"
            variant="outline"
            data-testid={`${testIdPrefix}-load-more`}
            onClick={() => setVisibleCount((n) => n + B2B_OBLIGATION_PAGE_SIZE)}
          >
            {t("b2bObligations.loadMore")}
          </Button>
        </div>
      ) : null}
    </Card>
  );
}
