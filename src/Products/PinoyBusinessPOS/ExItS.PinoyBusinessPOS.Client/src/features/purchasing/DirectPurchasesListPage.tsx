import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { canManageInventory } from "@/access/pos-capabilities";
import { listDirectPurchaseReceipts } from "@/api/pos/pos-direct-purchase-receipts-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const PAGE_SIZE = 20;

export function DirectPurchasesListPage() {
  const { t } = useI18n();
  const online = useBrowserOnline();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const [page, setPage] = useState(1);
  const allowManage = canManageInventory(sessionGrant);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const query = useQuery({
    queryKey: ["direct-purchases", workspace?.organizationId, workspace?.branchId, page],
    enabled: Boolean(workspace) && online,
    queryFn: ({ signal }) =>
      listDirectPurchaseReceipts(workspace!, { page, pageSize: PAGE_SIZE }, signal),
  });

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  const totalCount = query.data?.totalCount ?? 0;
  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="direct-purchases-list-page">
      <PageHeader
        title={t("purchasing.directPurchases")}
        description={t("purchasing.directPurchasesLede")}
      />
      {!online ? (
        <Card>
          <p className="m-0">{t("purchasing.offline")}</p>
        </Card>
      ) : null}
      <div className="flex flex-wrap gap-2">
        {allowManage ? (
          <Button asChild className="min-h-11" disabled={!online} data-testid="direct-new">
            <Link to="/purchasing/receive-stock">{t("purchasing.receiveStock")}</Link>
          </Button>
        ) : null}
        <Button asChild variant="ghost" className="min-h-11">
          <Link to="/purchasing">{t("purchasing.backHub")}</Link>
        </Button>
      </div>
      {query.isLoading ? <LoadingState label={t("purchasing.loading")} /> : null}
      {query.isError ? (
        <ErrorState title={t("purchasing.errorTitle")} detail={t("purchasing.loadFailed")} />
      ) : null}
      {query.isSuccess && query.data.items.length === 0 ? (
        <EmptyState
          title={t("purchasing.directEmpty")}
          detail={t("purchasing.directEmptyDetail")}
        />
      ) : null}
      <ul className="m-0 flex list-none flex-col gap-2 p-0">
        {(query.data?.items ?? []).map((item) => (
          <li key={item.directPurchaseReceiptId}>
            <Link
              to={`/purchasing/direct-purchases/${item.directPurchaseReceiptId}`}
              className="block rounded-md border border-border p-3 no-underline text-inherit"
              data-testid={`direct-row-${item.directPurchaseReceiptId}`}
            >
              <div className="font-medium">{item.receiptNumber}</div>
              <p className="mt-1 mb-0 text-[length:var(--exits-text-sm)] text-muted">
                {item.purchaseDate} · {item.sourceNameSnapshot ?? t("purchasing.sourceEmpty")} ·{" "}
                {item.totalCost}
              </p>
            </Link>
          </li>
        ))}
      </ul>
      {totalCount > PAGE_SIZE ? (
        <div className="flex flex-wrap items-center gap-2">
          <Button
            type="button"
            variant="ghost"
            className="min-h-11"
            disabled={page <= 1}
            onClick={() => setPage((p) => Math.max(1, p - 1))}
          >
            {t("purchasing.prevPage")}
          </Button>
          <span className="text-[length:var(--exits-text-sm)]">
            {t("purchasing.pageLabel")
              .replace("{page}", String(page))
              .replace("{totalPages}", String(totalPages))}
          </span>
          <Button
            type="button"
            variant="ghost"
            className="min-h-11"
            disabled={page >= totalPages}
            onClick={() => setPage((p) => p + 1)}
          >
            {t("purchasing.nextPage")}
          </Button>
        </div>
      ) : null}
    </div>
  );
}
