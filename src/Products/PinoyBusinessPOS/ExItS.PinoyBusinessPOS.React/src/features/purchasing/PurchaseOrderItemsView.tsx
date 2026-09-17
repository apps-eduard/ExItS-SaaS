import type { ReactNode } from "react";
import { Trash2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { ExitsDataRecordCard } from "@/components/exits/ExitsDataRecordCard";
import { ExitsResponsiveDataView } from "@/components/exits/ExitsResponsiveDataView";
import {
  ExitsTable,
  ExitsTableActions,
  ExitsTableBody,
  ExitsTableCell,
  ExitsTableContainer,
  ExitsTableHead,
  ExitsTableHeader,
  ExitsTableRow,
} from "@/components/exits/ExitsTable";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { Notice } from "@/components/exits/Notice";
import type { ResponsiveDataLayout } from "@/components/exits/responsive-data-view";
import { roundMoney } from "@/features/purchasing/purchase-order-create-connected";

export type PurchaseOrderSelectedLine = {
  productId: string;
  name: string;
  sku?: string | null;
  orderedQty: number;
  /** Authoritative supplier/catalog unit price (read-only). */
  unitPurchaseCost: number;
  quantityControl: ReactNode;
  /** Informational supplier availability (e.g. Available now: 5 Kg). */
  availabilityLabel?: string | null;
  /** Non-blocking over-order warning copy when requested qty exceeds availability. */
  overOrderWarning?: string | null;
  onRemove: () => void;
};

type Translate = (key: string) => string;

type PurchaseOrderItemsViewProps = {
  layout: ResponsiveDataLayout;
  lines: readonly PurchaseOrderSelectedLine[];
  t: Translate;
  lineTestIdPrefix?: string;
};

/**
 * Create PO selected lines — Qty stepper (with unit suffix) + read-only catalog price + line total.
 * Does not mix Receive Stock cost/expiry fields.
 */
export function PurchaseOrderItemsView({
  layout,
  lines,
  t,
  lineTestIdPrefix = "po-connected-selected",
}: PurchaseOrderItemsViewProps) {
  const tableBody = (
    <ExitsTableContainer className="po-order-items-table">
      <ExitsTable>
        <ExitsTableHeader>
          <ExitsTableRow>
            <ExitsTableHead cellAlign="text">{t("purchasing.colProduct")}</ExitsTableHead>
            <ExitsTableHead cellAlign="text">{t("purchasing.qty")}</ExitsTableHead>
            <ExitsTableHead cellAlign="numeric">{t("purchasing.catalogPrice")}</ExitsTableHead>
            <ExitsTableHead cellAlign="numeric">{t("purchasing.lineTotal")}</ExitsTableHead>
            <ExitsTableHead
              cellAlign="center"
              colSize="actions"
              className="po-order-items-table__action-col"
            >
              {t("purchasing.action")}
            </ExitsTableHead>
          </ExitsTableRow>
        </ExitsTableHeader>
        <ExitsTableBody>
          {lines.map((line) => {
            const lineTotal = roundMoney(line.orderedQty * line.unitPurchaseCost);
            return (
              <ExitsTableRow
                key={line.productId}
                data-testid={`${lineTestIdPrefix}-${line.productId}`}
              >
                <ExitsTableCell cellAlign="text">
                  <div className="font-medium leading-snug">{line.name}</div>
                  {line.sku?.trim() ? (
                    <div className="text-[length:var(--exits-text-xs)] text-muted">
                      {line.sku.trim()}
                    </div>
                  ) : null}
                  {line.availabilityLabel ? (
                    <div
                      className="mt-1 text-[length:var(--exits-text-xs)] text-muted"
                      data-testid={`${lineTestIdPrefix}-availability-${line.productId}`}
                    >
                      {line.availabilityLabel}
                    </div>
                  ) : null}
                  {line.overOrderWarning ? (
                    <Notice
                      tone="warning"
                      className="mt-2"
                      testId={`${lineTestIdPrefix}-over-order-${line.productId}`}
                    >
                      {line.overOrderWarning}
                    </Notice>
                  ) : null}
                </ExitsTableCell>
                <ExitsTableCell cellAlign="text">{line.quantityControl}</ExitsTableCell>
                <ExitsTableCell cellAlign="numeric">
                  <span className="tabular-nums" data-testid={`${lineTestIdPrefix}-price-${line.productId}`}>
                    <MoneyDisplay amount={line.unitPurchaseCost} />
                  </span>
                </ExitsTableCell>
                <ExitsTableCell cellAlign="numeric">
                  <span className="tabular-nums font-semibold">
                    <MoneyDisplay amount={lineTotal} />
                  </span>
                </ExitsTableCell>
                <ExitsTableCell
                  cellAlign="center"
                  colSize="actions"
                  className="po-order-items-table__action-col"
                >
                  <ExitsTableActions className="justify-center">
                    <Button
                      type="button"
                      variant="destructive"
                      size="icon"
                      aria-label={t("purchasing.removeNamed").replace("{name}", line.name)}
                      onClick={line.onRemove}
                      data-testid={`${lineTestIdPrefix}-remove-${line.productId}`}
                    >
                      <Trash2 className="size-4" aria-hidden />
                    </Button>
                  </ExitsTableActions>
                </ExitsTableCell>
              </ExitsTableRow>
            );
          })}
        </ExitsTableBody>
      </ExitsTable>
    </ExitsTableContainer>
  );

  const listBody = (
    <ul className="exits-data-record-list po-order-items-list">
      {lines.map((line) => {
        const lineTotal = roundMoney(line.orderedQty * line.unitPurchaseCost);
        return (
          <ExitsDataRecordCard
            key={line.productId}
            as="li"
            data-testid={`${lineTestIdPrefix}-${line.productId}`}
            title={line.name}
            subtitle={line.sku?.trim() || undefined}
            fields={[
              ...(line.availabilityLabel
                ? [
                    {
                      label: t("purchasing.colStock"),
                      value: (
                        <span data-testid={`${lineTestIdPrefix}-availability-${line.productId}`}>
                          {line.availabilityLabel}
                        </span>
                      ),
                    },
                  ]
                : []),
              { label: t("purchasing.qty"), value: line.quantityControl },
              {
                label: t("purchasing.catalogPrice"),
                value: (
                  <span data-testid={`${lineTestIdPrefix}-price-${line.productId}`}>
                    <MoneyDisplay amount={line.unitPurchaseCost} />
                  </span>
                ),
              },
              {
                label: t("purchasing.lineTotal"),
                value: <MoneyDisplay amount={lineTotal} />,
                emphasize: true,
              },
            ]}
            details={
              line.overOrderWarning ? (
                <Notice
                  tone="warning"
                  testId={`${lineTestIdPrefix}-over-order-${line.productId}`}
                >
                  {line.overOrderWarning}
                </Notice>
              ) : undefined
            }
            primaryAction={
              <Button
                type="button"
                variant="destructive"
                size="icon"
                aria-label={t("purchasing.removeNamed").replace("{name}", line.name)}
                onClick={line.onRemove}
                data-testid={`${lineTestIdPrefix}-remove-${line.productId}`}
              >
                <Trash2 className="size-4" aria-hidden />
              </Button>
            }
          />
        );
      })}
    </ul>
  );

  return (
    <ExitsResponsiveDataView
      layout={layout}
      testId="po-order-items"
      className="po-order-items-responsive"
      table={layout === "table" ? tableBody : null}
      list={layout === "list" ? listBody : null}
    />
  );
}
