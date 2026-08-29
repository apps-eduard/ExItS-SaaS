import { useMemo, useState } from "react";
import type { PosProductProfitabilityRowDto } from "@/api/pos/pos-reporting-client";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { productionCostStatusLabelKey } from "@/features/inventory/production-labels";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";

export type ProductProfitabilityRankBy =
  | "grossProfitDesc"
  | "grossProfitAsc"
  | "netSalesDesc"
  | "grossMarginDesc";

type SortKey =
  | "productName"
  | "quantitySold"
  | "salesBeforeDiscounts"
  | "commercialDiscounts"
  | "netSales"
  | "quantityReturned"
  | "knownCogs"
  | "grossProfit"
  | "grossMarginPercent"
  | "costCompletenessPercent";

const RANK_OPTIONS: { value: ProductProfitabilityRankBy; labelKey: MessageKey }[] = [
  { value: "grossProfitDesc", labelKey: "reports.rank.grossProfitDesc" },
  { value: "grossProfitAsc", labelKey: "reports.rank.grossProfitAsc" },
  { value: "netSalesDesc", labelKey: "reports.rank.netSalesDesc" },
  { value: "grossMarginDesc", labelKey: "reports.rank.grossMarginDesc" },
];

function formatMargin(value: number | null | undefined): string {
  if (value == null) {
    return "—";
  }
  return `${value.toFixed(1)}%`;
}

function compareNullableNumber(
  a: number | null | undefined,
  b: number | null | undefined,
  asc: boolean,
): number {
  if (a == null && b == null) {
    return 0;
  }
  if (a == null) {
    return 1;
  }
  if (b == null) {
    return -1;
  }
  return asc ? a - b : b - a;
}

export function ProductProfitabilityTable({
  rows,
  rankBy,
  onRankByChange,
}: {
  rows: PosProductProfitabilityRowDto[];
  rankBy: ProductProfitabilityRankBy;
  onRankByChange: (next: ProductProfitabilityRankBy) => void;
}) {
  const { t } = useI18n();
  const [sortKey, setSortKey] = useState<SortKey | null>(null);
  const [sortAsc, setSortAsc] = useState(false);

  const displayed = useMemo(() => {
    if (!sortKey) {
      return rows;
    }
    const copy = [...rows];
    copy.sort((a, b) => {
      switch (sortKey) {
        case "productName":
          return sortAsc
            ? a.productName.localeCompare(b.productName)
            : b.productName.localeCompare(a.productName);
        case "quantitySold":
        case "salesBeforeDiscounts":
        case "commercialDiscounts":
        case "netSales":
        case "quantityReturned":
        case "knownCogs":
        case "costCompletenessPercent":
          return sortAsc ? a[sortKey] - b[sortKey] : b[sortKey] - a[sortKey];
        case "grossProfit":
        case "grossMarginPercent":
          return compareNullableNumber(a[sortKey], b[sortKey], sortAsc);
        default:
          return 0;
      }
    });
    return copy;
  }, [rows, sortKey, sortAsc]);

  function toggleSort(key: SortKey) {
    if (sortKey === key) {
      setSortAsc((v) => !v);
      return;
    }
    setSortKey(key);
    setSortAsc(key === "productName");
  }

  function header(labelKey: MessageKey, key: SortKey) {
    const active = sortKey === key;
    return (
      <th className="whitespace-nowrap px-2 py-2 text-left text-[length:var(--exits-text-xs)] font-medium">
        <button
          type="button"
          className="inline-flex min-h-9 items-center gap-1 text-left"
          onClick={() => toggleSort(key)}
          data-testid={`product-profit-sort-${key}`}
        >
          {t(labelKey)}
          {active ? (sortAsc ? " ↑" : " ↓") : null}
        </button>
      </th>
    );
  }

  return (
    <div className="flex min-w-0 flex-col gap-3" data-testid="product-profitability-table">
      <label className="flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-sm)]">
        <span>{t("reports.rank.label")}</span>
        <select
          className="min-h-11 rounded-md border border-border bg-background px-3"
          value={rankBy}
          onChange={(e) => onRankByChange(e.target.value as ProductProfitabilityRankBy)}
          data-testid="product-profitability-rank"
        >
          {RANK_OPTIONS.map((opt) => (
            <option key={opt.value} value={opt.value}>
              {t(opt.labelKey)}
            </option>
          ))}
        </select>
      </label>

      <div className="overflow-x-auto">
        <table className="w-full min-w-[56rem] border-collapse text-[length:var(--exits-text-sm)]">
          <thead>
            <tr className="border-b border-border">
              {header("reports.col.product", "productName")}
              {header("reports.col.qtySold", "quantitySold")}
              {header("reports.metric.preDiscountGross", "salesBeforeDiscounts")}
              {header("reports.metric.commercialDiscounts", "commercialDiscounts")}
              {header("reports.metric.netSales", "netSales")}
              {header("reports.col.returnedQty", "quantityReturned")}
              {header("reports.metric.knownCogs", "knownCogs")}
              <th className="whitespace-nowrap px-2 py-2 text-left text-[length:var(--exits-text-xs)] font-medium">
                {t("reports.metric.cogsStatus")}
              </th>
              {header("reports.metric.grossProfit", "grossProfit")}
              {header("reports.metric.grossMargin", "grossMarginPercent")}
              {header("reports.metric.costCompleteness", "costCompletenessPercent")}
            </tr>
          </thead>
          <tbody>
            {displayed.map((row) => {
              const complete = row.cogsStatus === "Complete";
              return (
                <tr
                  key={row.productId}
                  className="border-b border-border align-top"
                  data-testid={`product-profit-row-${row.productId}`}
                >
                  <td className="px-2 py-2">
                    <div className="font-medium">{row.productName}</div>
                    {row.sku ? (
                      <div className="text-[length:var(--exits-text-xs)] text-muted">{row.sku}</div>
                    ) : null}
                  </td>
                  <td className="px-2 py-2 tabular-nums">{row.quantitySold}</td>
                  <td className="px-2 py-2">
                    <MoneyDisplay amount={row.salesBeforeDiscounts} />
                  </td>
                  <td className="px-2 py-2">
                    <MoneyDisplay amount={row.commercialDiscounts} />
                  </td>
                  <td className="px-2 py-2">
                    <MoneyDisplay amount={row.netSales} />
                  </td>
                  <td className="px-2 py-2 tabular-nums">
                    {row.quantityReturned}
                    {row.refundAmount > 0 ? (
                      <div className="text-[length:var(--exits-text-xs)] text-muted">
                        <MoneyDisplay amount={row.refundAmount} />
                      </div>
                    ) : null}
                  </td>
                  <td className="px-2 py-2">
                    <MoneyDisplay amount={row.knownCogs} />
                  </td>
                  <td className="px-2 py-2">
                    {t(productionCostStatusLabelKey(row.cogsStatus))}
                  </td>
                  <td className="px-2 py-2">
                    {complete && row.grossProfit != null ? (
                      <MoneyDisplay amount={row.grossProfit} />
                    ) : (
                      <span className="text-muted">—</span>
                    )}
                  </td>
                  <td className="px-2 py-2 tabular-nums">
                    {complete ? formatMargin(row.grossMarginPercent) : "—"}
                  </td>
                  <td className="px-2 py-2 tabular-nums">
                    {formatMargin(row.costCompletenessPercent)}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
}
