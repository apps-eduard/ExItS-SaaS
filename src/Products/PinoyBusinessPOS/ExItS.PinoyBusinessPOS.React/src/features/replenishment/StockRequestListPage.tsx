import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import {
  listIncomingStockRequests,
  listOutgoingStockRequests,
} from "@/api/pos/pos-stock-requests-client";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { useI18n } from "@/i18n/I18nProvider";
import { pageBackNav } from "@/navigation/page-back-nav";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type Direction = "incoming" | "outgoing";

export function StockRequestListPage({ initialDirection = "incoming" }: { initialDirection?: Direction }) {
  const { t } = useI18n();
  const { boundWorkspace } = useWorkspace();
  const [direction, setDirection] = useState<Direction>(initialDirection);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const query = useQuery({
    queryKey: ["stock-requests", direction, workspace?.organizationId, workspace?.branchId],
    enabled: Boolean(workspace),
    queryFn: ({ signal }) =>
      direction === "incoming"
        ? listIncomingStockRequests(workspace!, 1, 30, signal)
        : listOutgoingStockRequests(workspace!, 1, 30, signal),
  });

  if (!workspace) {
    return <EmptyState title={t("stockRequest.listTitle")} detail={t("stockRequest.needBranch")} />;
  }

  return (
    <div className="exits-page flex flex-col gap-3" data-testid="stock-request-list">
      <PageHeader
        title={t("stockRequest.listTitle")}
        description={t("stockRequest.listLede")}
        backTo={pageBackNav.inventory.to}
        backLabel={t(pageBackNav.inventory.labelKey)}
      />

      <ExitsChipBar
        ariaLabel={t("stockRequest.direction")}
        variant="filter"
        items={[
          {
            key: "incoming",
            label: t("stockRequest.incoming"),
            state: direction === "incoming" ? "active" : "idle",
            onSelect: () => setDirection("incoming"),
          },
          {
            key: "outgoing",
            label: t("stockRequest.outgoing"),
            state: direction === "outgoing" ? "active" : "idle",
            onSelect: () => setDirection("outgoing"),
          },
        ]}
      />

      {query.isLoading ? <LoadingState label={t("stockRequest.loading")} /> : null}
      {query.isError ? (
        <ErrorState title={t("stockRequest.loadError")} detail={t("stockRequest.loadError")} />
      ) : null}

      {!query.isLoading && (query.data?.items.length ?? 0) === 0 ? (
        <EmptyState title={t("stockRequest.empty")} detail={t("stockRequest.emptyDetail")} />
      ) : (
        <ul className="flex flex-col gap-2">
          {(query.data?.items ?? []).map((item) => (
            <li key={item.stockRequestId}>
              <Link
                to={`/inventory/stock-requests/${item.stockRequestId}`}
                className="flex items-center justify-between gap-2 rounded-[var(--exits-radius-md)] border border-border p-3"
                data-testid={`stock-request-row-${item.stockRequestId}`}
              >
                <div className="min-w-0">
                  <div className="font-medium">{item.requestNumber ?? item.stockRequestId.slice(0, 8)}</div>
                  <div className="text-[length:var(--exits-text-sm)] text-muted">
                    {direction === "incoming"
                      ? item.destinationLocationName ?? item.destinationLocationId
                      : item.requestedSourceLocationName ?? item.requestedSourceLocationId}
                    {" · "}
                    {item.lineCount} {t("stockRequest.items")}
                  </div>
                </div>
                <StatusChip tone="info">{item.status}</StatusChip>
              </Link>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
