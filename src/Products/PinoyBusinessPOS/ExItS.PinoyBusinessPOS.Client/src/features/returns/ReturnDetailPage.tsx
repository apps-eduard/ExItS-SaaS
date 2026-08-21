import { Link, useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import {
  formatRefundMethodLabel,
  getSaleReturn,
  isCashRefundMethod,
  isGCashRefundMethod,
  isUtangRefundMethod,
} from "@/api/pos/pos-sale-returns-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { PageHeader } from "@/components/exits/PageHeader";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function ReturnDetailPage() {
  const { t } = useI18n();
  const { returnId } = useParams<{ returnId: string }>();
  const { boundWorkspace } = useWorkspace();

  const workspace =
    boundWorkspace?.branchId && boundWorkspace.organizationId
      ? {
          organizationId: boundWorkspace.organizationId,
          branchId: boundWorkspace.branchId,
        }
      : null;

  const detailQuery = useQuery({
    queryKey: ["sale-return", workspace?.organizationId, workspace?.branchId, returnId],
    enabled: Boolean(workspace && returnId),
    queryFn: ({ signal }) => getSaleReturn(workspace!, returnId!, signal),
  });

  if (!returnId) {
    return (
      <div data-testid="return-detail-missing" className="flex flex-col gap-3">
        <PageHeader title={t("returns.detailTitle")} description={t("returns.missingReturn")} />
        <Button asChild variant="ghost" className="min-h-11 w-fit">
          <Link to="/returns">{t("returns.back")}</Link>
        </Button>
      </div>
    );
  }

  if (detailQuery.isLoading || !workspace) {
    return <LoadingSkeleton label={t("loading.label")} />;
  }

  if (detailQuery.isError || !detailQuery.data) {
    return (
      <div data-testid="return-detail-error" className="flex flex-col gap-3">
        <PageHeader title={t("returns.detailTitle")} description={t("returns.loadError")} />
        <ErrorState title={t("error.title")} detail={t("returns.errorNotFound")} />
        <Button asChild className="min-h-11 w-fit">
          <Link to="/returns">{t("returns.back")}</Link>
        </Button>
      </div>
    );
  }

  const detail = detailQuery.data;
  const methodLabel = formatRefundMethodLabel(detail.refundMethod);

  return (
    <div data-testid="return-detail-page" className="flex min-w-0 flex-col gap-4">
      <PageHeader
        title={t("returns.detailTitle")}
        description={`${detail.returnNumber} · ${detail.returnDate}`}
      />

      <Card>
        <dl className="m-0 grid gap-2 text-[length:var(--exits-text-sm)]">
          <div className="flex justify-between gap-2">
            <dt className="text-muted">{t("returns.returnNumber")}</dt>
            <dd className="m-0 font-semibold" data-testid="return-detail-number">
              {detail.returnNumber}
            </dd>
          </div>
          <div className="flex justify-between gap-2">
            <dt className="text-muted">{t("returns.refundMethod")}</dt>
            <dd className="m-0" data-testid="return-detail-method">
              {methodLabel}
            </dd>
          </div>
          <div className="flex justify-between gap-2">
            <dt className="text-muted">{t("returns.reason")}</dt>
            <dd className="m-0">{detail.reason}</dd>
          </div>
          {detail.notes ? (
            <div className="flex justify-between gap-2">
              <dt className="text-muted">{t("returns.notes")}</dt>
              <dd className="m-0">{detail.notes}</dd>
            </div>
          ) : null}
          <div className="flex justify-between gap-2 font-semibold">
            <dt>{t("returns.refundAmount")}</dt>
            <dd className="m-0">
              <MoneyDisplay amount={detail.totalRefundAmount} testId="return-detail-refund" />
            </dd>
          </div>
        </dl>

        {isCashRefundMethod(detail.refundMethod) ? (
          <p
            className="mb-0 mt-3 text-[length:var(--exits-text-sm)]"
            data-testid="return-detail-cash"
          >
            {t("returns.successCash")}
          </p>
        ) : null}
        {isGCashRefundMethod(detail.refundMethod) ? (
          <p
            className="mb-0 mt-3 text-[length:var(--exits-text-sm)]"
            data-testid="return-detail-gcash"
          >
            {t("returns.successGCash")}
          </p>
        ) : null}
        {isUtangRefundMethod(detail.refundMethod) ? (
          <p
            className="mb-0 mt-3 text-[length:var(--exits-text-sm)]"
            data-testid="return-detail-utang"
          >
            {t("returns.successUtang")}
          </p>
        ) : null}
      </Card>

      <Card>
        <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
          {t("returns.linesTitle")}
        </h2>
        <ul className="mb-0 mt-3 list-none space-y-2 p-0">
          {detail.lines.map((line) => (
            <li
              key={line.saleReturnLineId}
              className="flex items-start justify-between gap-2 text-[length:var(--exits-text-sm)]"
            >
              <span className="min-w-0">
                <span className="font-semibold">{line.productNameSnapshot}</span>
                <span className="text-muted">
                  {" "}
                  · {line.quantityReturned} {line.unitOfMeasure} ·{" "}
                  {line.restockDisposition === "ReturnToStock"
                    ? t("returns.putBackInStock")
                    : t("returns.doNotReturnToStock")}
                </span>
              </span>
              <MoneyDisplay amount={line.refundAmount} />
            </li>
          ))}
        </ul>
      </Card>

      <div className="flex flex-wrap gap-2">
        <Button asChild variant="ghost" className="min-h-11" data-testid="return-detail-back">
          <Link to="/returns">{t("returns.back")}</Link>
        </Button>
        <Button asChild variant="ghost" className="min-h-11">
          <Link to={`/sell/sales/${detail.saleId}/summary`}>{t("returns.viewTransaction")}</Link>
        </Button>
      </div>
    </div>
  );
}
