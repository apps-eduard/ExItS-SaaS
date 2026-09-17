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
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { PageHeader } from "@/components/exits/PageHeader";
import { ActorAttribution } from "@/features/actors/ActorAttribution";
import { useActorDirectory } from "@/features/actors/useActorDirectory";
import { pageBackNav } from "@/navigation/page-back-nav";
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

  const actors = useActorDirectory(workspace?.organizationId, [detailQuery.data?.createdBy]);

  if (!returnId) {
    return (
      <div data-testid="return-detail-missing" className="flex flex-col gap-3">
        <PageHeader
          title={t("returns.detailTitle")}
          description={t("returns.missingReturn")}
          backTo={pageBackNav.returns.to}
          backLabel={t(pageBackNav.returns.labelKey)}
          backTestId="page-header-back-returns"
        />
      </div>
    );
  }

  if (detailQuery.isLoading || !workspace) {
    return <LoadingSkeleton label={t("loading.label")} />;
  }

  if (detailQuery.isError || !detailQuery.data) {
    return (
      <div data-testid="return-detail-error" className="flex flex-col gap-3">
        <PageHeader
          title={t("returns.detailTitle")}
          description={t("returns.loadError")}
          backTo={pageBackNav.returns.to}
          backLabel={t(pageBackNav.returns.labelKey)}
          backTestId="page-header-back-returns"
        />
        <ErrorState title={t("error.title")} detail={t("returns.errorNotFound")} />
      </div>
    );
  }

  const detail = detailQuery.data;
  const methodLabel = formatRefundMethodLabel(detail.refundMethod);

  return (
    <div
      data-testid="return-detail-page"
      className="return-detail-page exits-page flex min-w-0 flex-col gap-3"
    >
      <PageHeader
        title={t("returns.detailTitle")}
        description={`${detail.returnNumber} · ${detail.returnDate}`}
        backTo={pageBackNav.returns.to}
        backLabel={t(pageBackNav.returns.labelKey)}
        backTestId="page-header-back-returns"
      />

      <section className="catalog-form-section exits-animate-panel gap-0">
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

        <div className="mt-3 border-t border-border pt-3">
          <ActorAttribution
            labelKey="common.processedBy"
            actorId={detail.createdBy}
            occurredAtUtc={detail.createdAtUtc}
            resolved={actors.resolve(detail.createdBy)}
            isLoading={actors.isResolving}
            testId="return-processed-by"
          />
        </div>

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
      </section>

      <section className="catalog-form-section exits-animate-panel gap-0">
        <h2 className="catalog-form-section__title">
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
      </section>

      <div className="flex flex-wrap gap-2">
        <Button asChild variant="ghost">
          <Link to={`/sell/sales/${detail.saleId}/summary`}>{t("returns.viewTransaction")}</Link>
        </Button>
      </div>
    </div>
  );
}
