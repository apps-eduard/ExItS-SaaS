import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { StatusChip } from "@/components/exits/StatusChip";
import { Card } from "@/components/ui/card";
import type { PosSupplierPayableReportDto } from "@/api/pos/pos-supplier-payables-client";
import { laterPaymentsAmount } from "@/features/purchasing/receive-payment";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";

function sourceLabel(sourceType: string, t: (key: MessageKey) => string): string {
  return sourceType === "DirectPurchaseReceipt"
    ? t("reports.supplierPayables.source.directPurchase")
    : t("reports.supplierPayables.source.goodsReceipt");
}

function statusLabel(status: string, t: (key: MessageKey) => string): string {
  switch (status) {
    case "PartiallyPaid":
      return t("supplierPayables.status.partiallyPaid");
    case "Paid":
      return t("supplierPayables.status.paid");
    case "Voided":
      return t("supplierPayables.status.voided");
    default:
      return t("supplierPayables.status.open");
  }
}

function statusTone(status: string, isOverdue: boolean): "success" | "warning" | "info" | "danger" {
  if (status === "Paid") {
    return "success";
  }
  if (status === "Voided") {
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

type SupplierPayablesReportViewProps = {
  report: PosSupplierPayableReportDto;
};

export function SupplierPayablesReportView({ report }: SupplierPayablesReportViewProps) {
  const { t } = useI18n();
  const { summary, suppliers, payables, asOfDate } = report;

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="supplier-payables-report">
      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="supplier-payables-as-of">
        {t("reports.supplierPayables.asOf")}: {asOfDate}
      </p>

      <dl
        className="m-0 grid gap-2 sm:grid-cols-2 lg:grid-cols-4 text-[length:var(--exits-text-sm)]"
        data-testid="supplier-payables-summary"
      >
        <Card className="p-3">
          <dt className="text-muted">{t("reports.supplierPayables.outstanding")}</dt>
          <dd className="m-0">
            <MoneyDisplay amount={summary.outstandingTotal} />
          </dd>
        </Card>
        <Card className="p-3">
          <dt className="text-muted">{t("reports.supplierPayables.overdue")}</dt>
          <dd className="m-0">
            <MoneyDisplay amount={summary.overdueTotal} />
          </dd>
        </Card>
        <Card className="p-3">
          <dt className="text-muted">{t("reports.supplierPayables.open")}</dt>
          <dd className="m-0" data-testid="supplier-payables-open-count">
            {summary.openCount}
          </dd>
        </Card>
        <Card className="p-3">
          <dt className="text-muted">{t("reports.supplierPayables.partiallyPaid")}</dt>
          <dd className="m-0">{summary.partiallyPaidCount}</dd>
        </Card>
      </dl>

      <section aria-labelledby="supplier-payables-balances-heading">
        <h2
          id="supplier-payables-balances-heading"
          className="m-0 mb-2 text-[length:var(--exits-text-sm)] font-semibold"
        >
          {t("reports.supplierPayables.supplierBalances")}
        </h2>
        {suppliers.length === 0 ? (
          <p className="m-0 text-muted">{t("reports.emptyDetail")}</p>
        ) : (
          <ul
            className="m-0 flex list-none flex-col gap-2 p-0"
            data-testid="supplier-payables-supplier-list"
          >
            {suppliers.map((row) => (
              <li
                key={row.supplierId}
                className="rounded-md border border-border p-3 text-[length:var(--exits-text-sm)]"
              >
                <div className="font-medium">
                  {row.supplierName?.trim() || t("reports.unknownSupplier")}
                </div>
                <dl className="m-0 mt-2 grid gap-2 sm:grid-cols-2 lg:grid-cols-4">
                  <div>
                    <dt className="text-muted">{t("reports.supplierPayables.outstanding")}</dt>
                    <dd className="m-0">
                      <MoneyDisplay amount={row.outstandingBalance} />
                    </dd>
                  </div>
                  <div>
                    <dt className="text-muted">{t("reports.supplierPayables.overdue")}</dt>
                    <dd className="m-0">
                      <MoneyDisplay amount={row.overdueBalance} />
                    </dd>
                  </div>
                  <div>
                    <dt className="text-muted">{t("reports.supplierPayables.open")}</dt>
                    <dd className="m-0">{row.openPayables}</dd>
                  </div>
                  <div>
                    <dt className="text-muted">{t("reports.supplierPayables.oldestDue")}</dt>
                    <dd className="m-0">{row.oldestDueDate?.trim() || "—"}</dd>
                  </div>
                </dl>
              </li>
            ))}
          </ul>
        )}
      </section>

      <section aria-labelledby="supplier-payables-detail-heading">
        <h2
          id="supplier-payables-detail-heading"
          className="m-0 mb-2 text-[length:var(--exits-text-sm)] font-semibold"
        >
          {t("reports.supplierPayables.payableDetail")}
        </h2>
        {payables.length === 0 ? (
          <p className="m-0 text-muted">{t("reports.emptyDetail")}</p>
        ) : (
          <>
            <div className="hidden overflow-x-auto md:block" data-testid="supplier-payables-table">
              <table className="w-full min-w-[48rem] border-collapse text-left text-[length:var(--exits-text-sm)]">
                <thead>
                  <tr className="border-b border-border">
                    <th className="py-2 pr-2 font-medium">{t("reports.supplierPayables.supplier")}</th>
                    <th className="py-2 pr-2 font-medium">{t("reports.supplierPayables.source")}</th>
                    <th className="py-2 pr-2 font-medium">{t("reports.supplierPayables.receiptDate")}</th>
                    <th className="py-2 pr-2 font-medium">{t("reports.supplierPayables.originalAmount")}</th>
                    <th className="py-2 pr-2 font-medium">{t("reports.supplierPayables.paidAtReceipt")}</th>
                    <th className="py-2 pr-2 font-medium">{t("reports.supplierPayables.laterPayments")}</th>
                    <th className="py-2 pr-2 font-medium">{t("reports.supplierPayables.totalPaid")}</th>
                    <th className="py-2 pr-2 font-medium">{t("reports.supplierPayables.balance")}</th>
                    <th className="py-2 pr-2 font-medium">{t("reports.supplierPayables.dueDate")}</th>
                    <th className="py-2 font-medium">{t("reports.supplierPayables.status")}</th>
                  </tr>
                </thead>
                <tbody>
                  {payables.map((row) => (
                    <tr key={row.payableId} className="border-b border-border align-top">
                      <td className="py-2 pr-2">
                        {row.supplierName?.trim() || t("reports.unknownSupplier")}
                      </td>
                      <td className="py-2 pr-2">{sourceLabel(row.sourceType, t)}</td>
                      <td className="py-2 pr-2">
                        {new Date(row.createdAtUtc).toLocaleDateString()}
                      </td>
                      <td className="py-2 pr-2">
                        <MoneyDisplay amount={row.originalAmount} />
                      </td>
                      <td className="py-2 pr-2">
                        <MoneyDisplay amount={row.paidAtReceiptAmount} />
                      </td>
                      <td className="py-2 pr-2">
                        <MoneyDisplay
                          amount={laterPaymentsAmount(row.paidAmount, row.paidAtReceiptAmount)}
                        />
                      </td>
                      <td className="py-2 pr-2">
                        <MoneyDisplay amount={row.paidAmount} />
                      </td>
                      <td className="py-2 pr-2">
                        <MoneyDisplay amount={row.balance} />
                      </td>
                      <td className="py-2 pr-2">{row.dueDate?.trim() || "—"}</td>
                      <td className="py-2">
                        <StatusChip tone={statusTone(row.status, row.isOverdue)}>
                          {statusLabel(row.status, t)}
                        </StatusChip>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <ul
              className="m-0 flex list-none flex-col gap-3 p-0 md:hidden"
              data-testid="supplier-payables-cards"
            >
              {payables.map((row) => (
                <li
                  key={row.payableId}
                  className="rounded-md border border-border p-3 text-[length:var(--exits-text-sm)]"
                >
                  <div className="mb-2 flex flex-wrap items-center gap-2">
                    <span className="font-medium">
                      {row.supplierName?.trim() || t("reports.unknownSupplier")}
                    </span>
                    <StatusChip tone={statusTone(row.status, row.isOverdue)}>
                      {statusLabel(row.status, t)}
                    </StatusChip>
                  </div>
                  <dl className="m-0 grid gap-1">
                    <div className="flex justify-between gap-2">
                      <dt className="text-muted">{t("reports.supplierPayables.source")}</dt>
                      <dd className="m-0">{sourceLabel(row.sourceType, t)}</dd>
                    </div>
                    <div className="flex justify-between gap-2">
                      <dt className="text-muted">{t("reports.supplierPayables.balance")}</dt>
                      <dd className="m-0">
                        <MoneyDisplay amount={row.balance} />
                      </dd>
                    </div>
                    <div className="flex justify-between gap-2">
                      <dt className="text-muted">{t("reports.supplierPayables.dueDate")}</dt>
                      <dd className="m-0">{row.dueDate?.trim() || "—"}</dd>
                    </div>
                  </dl>
                </li>
              ))}
            </ul>
          </>
        )}
      </section>
    </div>
  );
}
