import { useEffect, useMemo, useState } from "react";
import { useParams } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { canManageSuppliers } from "@/access/pos-capabilities";
import {
  applyBuyerProductPricing,
  bulkMutateBuyerProductShares,
  previewBuyerProductPricing,
  queryBuyerProductShares,
} from "@/api/pos/pos-connected-suppliers-client";
import { PosApiError } from "@/api/pos/pos-http";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { UnderlineTabBar } from "@/components/exits/UnderlineTabBar";
import { SearchField } from "@/components/exits/SearchField";
import { StatusChip } from "@/components/exits/StatusChip";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const PAGE_SIZE = 25;

export function ConnectedSharedProductsPage() {
  const { t } = useI18n();
  const { relationshipId } = useParams<{ relationshipId: string }>();
  const queryClient = useQueryClient();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");
  const [shareFilter, setShareFilter] = useState<"all" | "shared" | "notShared">("all");
  const [page, setPage] = useState(1);
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [buyerPrice, setBuyerPrice] = useState("");
  const [message, setMessage] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    const handle = window.setTimeout(() => setDebounced(search.trim()), 250);
    return () => window.clearTimeout(handle);
  }, [search]);

  useEffect(() => {
    setPage(1);
    setSelected(new Set());
  }, [debounced, shareFilter]);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const allowManage = canManageSuppliers(sessionGrant);

  const query = useQuery({
    queryKey: [
      "connected-suppliers",
      "shares",
      relationshipId,
      debounced,
      shareFilter,
      page,
      workspace?.organizationId,
    ],
    enabled: Boolean(workspace) && Boolean(relationshipId),
    queryFn: ({ signal }) =>
      queryBuyerProductShares(
        workspace!,
        relationshipId!,
        {
          query: debounced || undefined,
          shareFilter,
          page,
          pageSize: PAGE_SIZE,
        },
        signal,
      ),
  });

  function toggle(productId: string) {
    setSelected((current) => {
      const next = new Set(current);
      if (next.has(productId)) {
        next.delete(productId);
      } else {
        next.add(productId);
      }
      return next;
    });
  }

  async function runBulk(operation: "Share" | "Unshare") {
    if (!workspace || !relationshipId || !allowManage || selected.size === 0 || busy) {
      return;
    }
    setBusy(true);
    setMessage(null);
    try {
      const result = await bulkMutateBuyerProductShares(workspace, relationshipId, {
        operation,
        productIds: [...selected],
      });
      setMessage(t("connected.bulkAffected").replace("{count}", String(result.affectedCount)));
      setSelected(new Set());
      await queryClient.invalidateQueries({ queryKey: ["connected-suppliers", "shares"] });
    } catch (err) {
      setMessage(
        err instanceof PosApiError
          ? (err.problem.detail ?? err.message)
          : t("connected.saveSharingFailed"),
      );
    } finally {
      setBusy(false);
    }
  }

  async function applyFixedPrice() {
    if (!workspace || !relationshipId || !allowManage || selected.size === 0 || busy) {
      return;
    }
    const price = Number(buyerPrice);
    if (!Number.isFinite(price) || price < 0) {
      setMessage(t("connected.buyerPriceInvalid"));
      return;
    }
    setBusy(true);
    setMessage(null);
    try {
      const input = {
        mode: "FixedPrice" as const,
        productIds: [...selected],
        fixedPrice: price,
      };
      await previewBuyerProductPricing(workspace, relationshipId, input);
      const applied = await applyBuyerProductPricing(workspace, relationshipId, input);
      setMessage(t("connected.priceApplied").replace("{count}", String(applied.affectedCount)));
      setSelected(new Set());
      setBuyerPrice("");
      await queryClient.invalidateQueries({ queryKey: ["connected-suppliers", "shares"] });
    } catch (err) {
      setMessage(
        err instanceof PosApiError
          ? (err.problem.detail ?? err.message)
          : t("connected.saveSharingFailed"),
      );
    } finally {
      setBusy(false);
    }
  }

  if (!workspace || !relationshipId) {
    return <LoadingState label={t("session.loading")} />;
  }

  const totalPages = Math.max(
    1,
    Math.ceil((query.data?.matchingCount ?? 0) / (query.data?.pageSize ?? PAGE_SIZE)),
  );

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="connected-shared-products-page">
      <PageHeader
        title={t("connected.manageSharedTitle")}
        description={t("connected.manageSharedHelp")}
        backTo={`/customers/business/${relationshipId}`}
        backLabel={t("connected.backToBuyer")}
        backTestId="page-header-back-suppliers"
      />
      <p
        className="m-0 text-[length:var(--exits-text-sm)] text-muted"
        data-testid="connected-exposable-note"
      >
        {t("connected.exposableNotSharedNote")}
      </p>
      {message ? (
        <Card data-testid="connected-share-message">
          <p className="m-0 text-[length:var(--exits-text-sm)]">{message}</p>
        </Card>
      ) : null}
      <SearchField
        label={t("connected.searchProducts")}
        value={search}
        onChange={(event) => setSearch(event.target.value)}
        onClear={() => setSearch("")}
        placeholder={t("connected.searchProducts")}
        data-testid="connected-share-search"
      />
      <UnderlineTabBar
        items={(
          [
            ["all", "connected.filterAll"],
            ["shared", "connected.filterShared"],
            ["notShared", "connected.filterNotShared"],
          ] as const
        ).map(([value, key]) => ({
          key: value,
          label: t(key),
          testId: `connected-filter-${value}`,
        }))}
        activeKey={shareFilter}
        onChange={(key) => setShareFilter(key as typeof shareFilter)}
        ariaLabel={t("connected.shareFilter")}
      />
      {query.isLoading ? <LoadingState label={t("loading.label")} /> : null}
      {query.isError ? (
        <ErrorState
          title={t("error.title")}
          detail={
            query.error instanceof PosApiError
              ? (query.error.problem.detail ?? query.error.message)
              : t("connected.loadFailed")
          }
        />
      ) : null}
      {query.isSuccess && query.data.items.length === 0 ? (
        <EmptyState
          title={t("connected.noProductsForFilter")}
          detail={t("connected.noProductsForFilterHelp")}
        />
      ) : null}
      {query.data ? (
        <div className="flex min-w-0 flex-col gap-1" data-testid="connected-share-summary">
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {query.data.catalogSharingMode === "AllEligible"
              ? t("connected.shareSummaryAllEligible")
                  .replace("{shared}", String(query.data.sharedCount))
                  .replace("{eligible}", String(query.data.eligibleCount))
              : t("connected.shareSummary")
                  .replace("{shared}", String(query.data.sharedCount))
                  .replace("{eligible}", String(query.data.eligibleCount))}
          </p>
          {query.data.customerDiscountPercent != null
          && query.data.customerDiscountPercent > 0 ? (
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("connected.customerDiscountBanner").replace(
                "{percent}",
                String(query.data.customerDiscountPercent),
              )}
            </p>
          ) : query.data.catalogSharingMode === "AllEligible" ? (
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("connected.sellingPriceBaselineBanner")}
            </p>
          ) : null}
        </div>
      ) : null}
      <ul className="m-0 grid list-none gap-2 p-0" data-testid="connected-share-list">
        {query.data?.items.map((item) => {
          const customerPrice =
            item.effectiveSupplierOrderPrice
            ?? item.buyerSpecificPoPrice
            ?? null;
          const listPrice =
            item.sellingPrice != null && item.sellingPrice > 0
              ? item.sellingPrice
              : item.defaultPoPrice;
          return (
          <li key={item.supplierProductId}>
            <Card className="p-3">
              <label className="flex cursor-pointer items-start gap-3">
                {allowManage ? (
                  <input
                    type="checkbox"
                    className="mt-1 size-5"
                    checked={selected.has(item.supplierProductId)}
                    onChange={() => toggle(item.supplierProductId)}
                    data-testid={`connected-share-check-${item.supplierProductId}`}
                  />
                ) : null}
                <span className="min-w-0 flex-1">
                  <span className="block font-semibold">
                    {item.nameSnapshot ?? item.supplierProductId}
                  </span>
                  <span className="mt-1 flex flex-wrap items-center gap-2 text-[length:var(--exits-text-sm)] text-muted">
                    {item.skuSnapshot}
                    <StatusChip tone={item.isShared ? "success" : "warning"}>
                      {item.isShared
                        ? t("connected.shared")
                        : query.data.catalogSharingMode === "AllEligible"
                          ? t("connected.excluded")
                          : t("connected.notShared")}
                    </StatusChip>
                    {customerPrice != null ? (
                      <span data-testid={`connected-customer-price-${item.supplierProductId}`}>
                        {t("connected.customerPrice")}: {customerPrice}
                      </span>
                    ) : null}
                    {listPrice != null ? (
                      <span>
                        {t("connected.listPrice")}: {listPrice}
                      </span>
                    ) : (
                      <span>{t("connected.noListPrice")}</span>
                    )}
                  </span>
                </span>
              </label>
            </Card>
          </li>
          );
        })}
      </ul>
      {allowManage && selected.size > 0 ? (
        <div className="flex flex-wrap gap-2" data-testid="connected-bulk-actions">
          <Button
            type="button"
            className="min-h-11"
            disabled={busy}
            data-testid="connected-bulk-share"
            onClick={() => void runBulk("Share")}
          >
            {t("connected.bulkShare")}
          </Button>
          <Button
            type="button"
            variant="ghost"
            className="min-h-11"
            disabled={busy}
            data-testid="connected-bulk-unshare"
            onClick={() => void runBulk("Unshare")}
          >
            {t("connected.bulkUnshare")}
          </Button>
          <label className="flex items-center gap-2 text-[length:var(--exits-text-sm)]">
            <span>{t("connected.buyerPrice")}</span>
            <input
              className="min-h-11 w-28 rounded-[var(--exits-radius-md)] border border-[var(--exits-border)] px-2"
              inputMode="decimal"
              value={buyerPrice}
              onChange={(event) => setBuyerPrice(event.target.value)}
              data-testid="connected-buyer-price-input"
            />
          </label>
          <Button
            type="button"
            variant="ghost"
            className="min-h-11"
            disabled={busy}
            data-testid="connected-apply-buyer-price"
            onClick={() => void applyFixedPrice()}
          >
            {t("connected.applyBuyerPrice")}
          </Button>
        </div>
      ) : null}
      {query.isSuccess && (query.data?.matchingCount ?? 0) > 0 ? (
        <div className="flex flex-wrap items-center justify-between gap-2">
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("suppliers.pageLabel")
              .replace("{page}", String(page))
              .replace("{totalPages}", String(totalPages))}
          </p>
          <div className="flex gap-2">
            <Button
              type="button"
              variant="ghost"
              className="min-h-11"
              disabled={page <= 1}
              data-testid="connected-share-prev"
              onClick={() => setPage((current) => Math.max(1, current - 1))}
            >
              {t("suppliers.prevPage")}
            </Button>
            <Button
              type="button"
              variant="ghost"
              className="min-h-11"
              disabled={page >= totalPages}
              data-testid="connected-share-next"
              onClick={() => setPage((current) => current + 1)}
            >
              {t("suppliers.nextPage")}
            </Button>
          </div>
        </div>
      ) : null}
    </div>
  );
}
