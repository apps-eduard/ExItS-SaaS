import { useMemo } from "react";
import { useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { getDirectPurchaseReceipt } from "@/api/pos/pos-direct-purchase-receipts-client";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
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

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="direct-purchase-detail-page">
      <PageHeader
        title={receipt.receiptNumber}
        description={t("purchasing.directDetailLede")}
        backTo="/purchasing/direct-purchases"
        backLabel={t("purchasing.backDirect")}
        backTestId="page-header-back-purchasing"
      />
      <dl className="m-0 grid gap-2 sm:grid-cols-2">
        <div>
          <dt className="text-[length:var(--exits-text-sm)] text-muted">
            {t("purchasing.purchaseDate")}
          </dt>
          <dd className="m-0">{receipt.purchaseDate}</dd>
        </div>
        <div>
          <dt className="text-[length:var(--exits-text-sm)] text-muted">
            {t("purchasing.boughtFrom")}
          </dt>
          <dd className="m-0">{receipt.sourceNameSnapshot ?? t("purchasing.sourceEmpty")}</dd>
        </div>
        {receipt.referenceNumber ? (
          <div>
            <dt className="text-[length:var(--exits-text-sm)] text-muted">
              {t("purchasing.reference")}
            </dt>
            <dd className="m-0">{receipt.referenceNumber}</dd>
          </div>
        ) : null}
        <div>
          <dt className="text-[length:var(--exits-text-sm)] text-muted">
            {t("purchasing.totalCost")}
          </dt>
          <dd className="m-0">{receipt.totalCost}</dd>
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
      <section>
        <h2 className="m-0 mb-2 text-[length:var(--exits-text-md)] font-medium">
          {t("purchasing.lines")}
        </h2>
        <ul className="m-0 flex list-none flex-col gap-2 p-0">
          {receipt.lines.map((line) => (
            <li key={line.lineId} className="rounded-md border border-border p-3">
              <div className="font-medium">{line.productNameSnapshot}</div>
              <p className="mt-1 mb-0 text-[length:var(--exits-text-sm)] text-muted">
                {line.quantity} {line.unitOfMeasure} · {line.unitCost}
                {line.expiryDate ? ` · ${line.expiryDate}` : ""}
                {line.lotNumber ? ` · ${line.lotNumber}` : ""}
              </p>
            </li>
          ))}
        </ul>
      </section>
    </div>
  );
}
