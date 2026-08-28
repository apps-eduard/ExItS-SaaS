import { useMemo } from "react";
import { useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { getDirectPurchaseReceipt } from "@/api/pos/pos-direct-purchase-receipts-client";
import { Card } from "@/components/ui/card";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { PageHeader } from "@/components/exits/PageHeader";
import { ActorAttribution } from "@/features/actors/ActorAttribution";
import { useActorDirectory } from "@/features/actors/useActorDirectory";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function DirectPurchaseDetailPage() {
  const { t } = useI18n();
  const online = useBrowserOnline();
  const { receiptId } = useParams<{ receiptId: string }>();
  const { boundWorkspace } = useWorkspace();

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const query = useQuery({
    queryKey: ["direct-purchase", workspace?.organizationId, receiptId],
    enabled: Boolean(workspace) && Boolean(receiptId) && online,
    queryFn: ({ signal }) => getDirectPurchaseReceipt(workspace!, receiptId!, signal),
  });

  const actors = useActorDirectory(workspace?.organizationId, [query.data?.createdByUserId]);

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }
  if (!receiptId) {
    return (
      <ErrorState title={t("purchasing.errorTitle")} detail={t("purchasing.directNotFound")} />
    );
  }
  if (query.isLoading) {
    return <LoadingState label={t("purchasing.loading")} />;
  }
  if (query.isError || !query.data) {
    return (
      <ErrorState title={t("purchasing.errorTitle")} detail={t("purchasing.directNotFound")} />
    );
  }

  const receipt = query.data;
  const notes = receipt.notes?.trim();

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="direct-purchase-detail-page">
      <PageHeader
        title={receipt.receiptNumber}
        description={t("purchasing.directDetailLede")}
        backTo="/purchasing/direct-purchases"
        backLabel={t("purchasing.backDirect")}
        backTestId="page-header-back-purchasing"
      />

      <section aria-labelledby="direct-purchase-info">
        <h2
          id="direct-purchase-info"
          className="m-0 mb-2 text-[length:var(--exits-text-md)] font-medium"
        >
          {t("purchasing.purchaseInformation")}
        </h2>
        <Card className="flex flex-col gap-3 p-3">
          <dl className="m-0 grid gap-3 sm:grid-cols-2">
            <div>
              <dt className="text-[length:var(--exits-text-sm)] text-muted">
                {t("purchasing.purchaseDate")}
              </dt>
              <dd className="m-0" data-testid="direct-purchase-date">
                {receipt.purchaseDate}
              </dd>
            </div>
            <div>
              <dt className="text-[length:var(--exits-text-sm)] text-muted">
                {t("purchasing.boughtFrom")}
              </dt>
              <dd className="m-0" data-testid="direct-purchase-source">
                {receipt.sourceNameSnapshot ?? t("purchasing.sourceEmpty")}
              </dd>
            </div>
            {receipt.referenceNumber ? (
              <div>
                <dt className="text-[length:var(--exits-text-sm)] text-muted">
                  {t("purchasing.reference")}
                </dt>
                <dd className="m-0" data-testid="direct-purchase-reference">
                  {receipt.referenceNumber}
                </dd>
              </div>
            ) : null}
            <div>
              <dt className="text-[length:var(--exits-text-sm)] text-muted">
                {t("purchasing.totalPurchaseCost")}
              </dt>
              <dd className="m-0" data-testid="direct-purchase-total">
                <MoneyDisplay amount={receipt.totalCost} />
              </dd>
            </div>
          </dl>
          <ActorAttribution
            labelKey="common.recordedBy"
            actorId={receipt.createdByUserId}
            occurredAtUtc={receipt.createdAtUtc}
            resolved={actors.resolve(receipt.createdByUserId)}
            isLoading={actors.isResolving}
            testId="direct-purchase-recorded-by"
          />
          {notes ? (
            <div data-testid="direct-purchase-notes">
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                {t("purchasing.notes")}
              </p>
              <p className="mt-1 mb-0 whitespace-pre-wrap text-[length:var(--exits-text-sm)]">
                {notes}
              </p>
            </div>
          ) : null}
        </Card>
      </section>

      <section aria-labelledby="direct-purchase-lines">
        <h2
          id="direct-purchase-lines"
          className="m-0 mb-2 text-[length:var(--exits-text-md)] font-medium"
        >
          {t("purchasing.items")}
        </h2>
        <ul className="m-0 flex list-none flex-col gap-2 p-0">
          {receipt.lines.map((line) => (
            <li key={line.lineId}>
              <Card
                className="flex flex-col gap-2 p-3"
                data-testid={`direct-purchase-line-${line.lineId}`}
              >
                <p className="m-0 font-medium">{line.productNameSnapshot}</p>
                <p className="m-0 text-[length:var(--exits-text-sm)]">
                  {line.quantity} {line.unitOfMeasure}
                </p>
                <dl className="m-0 grid gap-1 text-[length:var(--exits-text-sm)]">
                  <div className="flex flex-wrap items-baseline justify-between gap-2">
                    <dt className="text-muted">{t("purchasing.unitPurchaseCost")}</dt>
                    <dd className="m-0">
                      <MoneyDisplay amount={line.unitCost} />
                      <span className="text-muted"> / {line.unitOfMeasure}</span>
                    </dd>
                  </div>
                  <div className="flex flex-wrap items-baseline justify-between gap-2">
                    <dt className="text-muted">{t("purchasing.lineTotal")}</dt>
                    <dd className="m-0">
                      <MoneyDisplay amount={line.lineTotal} />
                    </dd>
                  </div>
                  {line.expiryDate ? (
                    <div className="flex flex-wrap items-baseline justify-between gap-2">
                      <dt className="text-muted">{t("purchasing.expiryDate")}</dt>
                      <dd className="m-0">{line.expiryDate}</dd>
                    </div>
                  ) : null}
                  {line.expiryDate || line.lotNumber ? (
                    <div className="flex flex-wrap items-baseline justify-between gap-2">
                      <dt className="text-muted">{t("purchasing.lotNumber")}</dt>
                      <dd className="m-0">{line.lotNumber?.trim() || "—"}</dd>
                    </div>
                  ) : null}
                </dl>
              </Card>
            </li>
          ))}
        </ul>
      </section>
    </div>
  );
}
