import { useEffect, useMemo, useRef, useState } from "react";
import { useParams } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Building2, Link2Off, Percent, Share2, Tag } from "lucide-react";
import { canGovernOrganizationCatalog, canManageSuppliers } from "@/access/pos-capabilities";
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
import { isBranchLocalProduct } from "@/features/catalog/catalog-product-scope";
import { useI18n } from "@/i18n/I18nProvider";
import { formatPeso } from "@/lib/format-money";
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
  const [percentValue, setPercentValue] = useState("");
  const [percentMode, setPercentMode] = useState<"discount" | "increase">("discount");
  const [message, setMessage] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const buyerPriceInputRef = useRef<HTMLInputElement>(null);
  const percentInputRef = useRef<HTMLInputElement>(null);

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

  const allowManage =
    canManageSuppliers(sessionGrant) && canGovernOrganizationCatalog(sessionGrant);

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
    if (!workspace || !relationshipId || !allowManage || selected.size !== 1 || busy) {
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

  async function applyPercentChange() {
    if (!workspace || !relationshipId || !allowManage || selected.size < 2 || busy) {
      return;
    }
    const percent = Number(percentValue);
    const maxPercent = percentMode === "discount" ? 100 : 1000;
    if (!Number.isFinite(percent) || percent < 0 || percent > maxPercent) {
      setMessage(
        percentMode === "discount"
          ? t("connected.discountPercentInvalid")
          : t("connected.increasePercentInvalid"),
      );
      return;
    }
    setBusy(true);
    setMessage(null);
    try {
      const input = {
        mode: percentMode === "discount" ? ("DiscountPercent" as const) : ("MarkupPercent" as const),
        productIds: [...selected],
        percent,
      };
      await previewBuyerProductPricing(workspace, relationshipId, input);
      const applied = await applyBuyerProductPricing(workspace, relationshipId, input);
      setMessage(t("connected.priceApplied").replace("{count}", String(applied.affectedCount)));
      setSelected(new Set());
      setPercentValue("");
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

  const shareItems = useMemo(
    () =>
      (query.data?.items ?? []).filter(
        (item) => !isBranchLocalProduct({ scope: item.scope ?? undefined }),
      ),
    [query.data?.items],
  );

  const pageProductIds = useMemo(
    () => shareItems.map((item) => item.supplierProductId),
    [shareItems],
  );
  const allPageSelected =
    pageProductIds.length > 0 && pageProductIds.every((id) => selected.has(id));
  const somePageSelected = pageProductIds.some((id) => selected.has(id));

  function toggleSelectAllPage() {
    setSelected((current) => {
      const next = new Set(current);
      const allSelected =
        pageProductIds.length > 0 && pageProductIds.every((id) => current.has(id));
      if (allSelected) {
        for (const id of pageProductIds) {
          next.delete(id);
        }
      } else {
        for (const id of pageProductIds) {
          next.add(id);
        }
      }
      return next;
    });
  }

  const selectedShareItems = useMemo(
    () => shareItems.filter((item) => selected.has(item.supplierProductId)),
    [shareItems, selected],
  );

  const singleSelectedItem = selectedShareItems.length === 1 ? selectedShareItems[0] : null;
  const singleSellingPrice =
    singleSelectedItem == null
      ? null
      : singleSelectedItem.sellingPrice != null && singleSelectedItem.sellingPrice > 0
        ? singleSelectedItem.sellingPrice
        : singleSelectedItem.defaultPoPrice ?? null;

  useEffect(() => {
    const count = selected.size;
    if (count === 1) {
      setPercentValue("");
      const item = shareItems.find((row) => selected.has(row.supplierProductId));
      const selling =
        item == null
          ? null
          : item.sellingPrice != null && item.sellingPrice > 0
            ? item.sellingPrice
            : item.defaultPoPrice ?? null;
      setBuyerPrice(selling != null ? String(selling) : "");
      const frame = window.requestAnimationFrame(() => {
        buyerPriceInputRef.current?.focus();
        buyerPriceInputRef.current?.select();
      });
      return () => window.cancelAnimationFrame(frame);
    }
    if (count >= 2) {
      setBuyerPrice("");
      const frame = window.requestAnimationFrame(() => {
        percentInputRef.current?.focus();
        percentInputRef.current?.select();
      });
      return () => window.cancelAnimationFrame(frame);
    }
    setBuyerPrice("");
    setPercentValue("");
  }, [selected, shareItems]);

  if (!workspace || !relationshipId) {
    return <LoadingState label={t("session.loading")} />;
  }

  const totalPages = Math.max(
    1,
    Math.ceil((query.data?.matchingCount ?? 0) / (query.data?.pageSize ?? PAGE_SIZE)),
  );

  return (
    <div
      className="connected-share-page flex min-w-0 flex-col gap-4"
      data-testid="connected-shared-products-page"
    >
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
      <div className="connected-share-toolbar flex min-w-0 flex-wrap items-center gap-2">
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
        <SearchField
          label={t("connected.searchProducts")}
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          onClear={() => setSearch("")}
          placeholder={t("connected.searchProducts")}
          data-testid="connected-share-search"
          containerClassName="connected-share-toolbar__search min-w-[10rem] flex-1"
        />
      </div>
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
      {query.isSuccess && shareItems.length === 0 ? (
        <EmptyState
              variant="filtered"
              align="center"
              icon={<Building2 className="size-5" strokeWidth={1.75} />}
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
      {allowManage && shareItems.length > 0 ? (
        <div className="connected-share-select-all-bar" data-testid="connected-share-select-all-bar">
          <label className="connected-share-select-all-bar__label">
            <input
              type="checkbox"
              className="size-5"
              checked={allPageSelected}
              ref={(el) => {
                if (el) {
                  el.indeterminate = somePageSelected && !allPageSelected;
                }
              }}
              onChange={toggleSelectAllPage}
              data-testid="connected-share-select-all-mobile"
            />
            <span>
              {allPageSelected
                ? t("connected.deselectAllPage")
                : t("connected.selectAllPage").replace("{count}", String(shareItems.length))}
            </span>
          </label>
        </div>
      ) : null}
      <div
        className={
          allowManage
            ? "connected-share-table connected-share-table--selectable"
            : "connected-share-table"
        }
        data-testid="connected-share-list"
      >
        <div className="connected-share-table__head">
          {allowManage ? (
            <span className="connected-share-table__check">
              <input
                type="checkbox"
                className="size-5"
                checked={allPageSelected}
                ref={(el) => {
                  if (el) {
                    el.indeterminate = somePageSelected && !allPageSelected;
                  }
                }}
                onChange={toggleSelectAllPage}
                data-testid="connected-share-select-all"
                aria-label={
                  allPageSelected
                    ? t("connected.deselectAllPage")
                    : t("connected.selectAllPage").replace("{count}", String(shareItems.length))
                }
              />
            </span>
          ) : null}
          <span>{t("connected.colProduct")}</span>
          <span>{t("connected.colStatus")}</span>
          <span className="connected-share-table__price-head">{t("connected.listPrice")}</span>
          <span className="connected-share-table__price-head">{t("connected.customerPrice")}</span>
        </div>
        <ul className="connected-share-table__list">
          {shareItems.map((item) => {
            const customerPrice =
              item.effectiveSupplierOrderPrice
              ?? item.buyerSpecificPoPrice
              ?? null;
            const listPrice =
              item.sellingPrice != null && item.sellingPrice > 0
                ? item.sellingPrice
                : item.defaultPoPrice;
            const statusLabel = item.isShared
              ? t("connected.shared")
              : query.data?.catalogSharingMode === "AllEligible"
                ? t("connected.excluded")
                : t("connected.notShared");
            return (
              <li key={item.supplierProductId}>
                <label
                  className="connected-share-table__row"
                  data-testid={`connected-share-row-${item.supplierProductId}`}
                >
                  {allowManage ? (
                    <span className="connected-share-table__check">
                      <input
                        type="checkbox"
                        className="size-5"
                        checked={selected.has(item.supplierProductId)}
                        onChange={() => toggle(item.supplierProductId)}
                        data-testid={`connected-share-check-${item.supplierProductId}`}
                      />
                    </span>
                  ) : null}
                  <span className="connected-share-table__product">
                    <span className="connected-share-table__name">
                      {item.nameSnapshot ?? item.supplierProductId}
                    </span>
                    {item.skuSnapshot ? (
                      <span className="connected-share-table__sku">{item.skuSnapshot}</span>
                    ) : null}
                  </span>
                  <span className="connected-share-table__status">
                    <StatusChip tone={item.isShared ? "success" : "warning"}>
                      {statusLabel}
                    </StatusChip>
                  </span>
                  <span
                    className="connected-share-table__price"
                    data-label={t("connected.listPrice")}
                  >
                    {listPrice != null ? formatPeso(listPrice) : t("connected.noListPrice")}
                  </span>
                  <span
                    className="connected-share-table__price"
                    data-label={t("connected.customerPrice")}
                    data-testid={`connected-customer-price-${item.supplierProductId}`}
                  >
                    {customerPrice != null ? formatPeso(customerPrice) : "—"}
                  </span>
                </label>
              </li>
            );
          })}
        </ul>
      </div>
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
              disabled={page <= 1}
              data-testid="connected-share-prev"
              onClick={() => setPage((current) => Math.max(1, current - 1))}
            >
              {t("suppliers.prevPage")}
            </Button>
            <Button
              type="button"
              variant="ghost"
              disabled={page >= totalPages}
              data-testid="connected-share-next"
              onClick={() => setPage((current) => current + 1)}
            >
              {t("suppliers.nextPage")}
            </Button>
          </div>
        </div>
      ) : null}
      {allowManage && selected.size > 0 ? (
        <div className="connected-share-bulk-bar" data-testid="connected-bulk-actions">
          <p className="connected-share-bulk-bar__count m-0">
            {t("connected.bulkSelectedCount").replace("{count}", String(selected.size))}
          </p>
          <Button
            type="button"
            disabled={busy}
            data-testid="connected-bulk-share"
            onClick={() => void runBulk("Share")}
          >
            <Share2 className="size-4 shrink-0" aria-hidden />
            {t("connected.bulkShare")}
          </Button>
          <Button
            type="button"
            variant="outline"
            disabled={busy}
            data-testid="connected-bulk-unshare"
            onClick={() => void runBulk("Unshare")}
          >
            <Link2Off className="size-4 shrink-0" aria-hidden />
            {t("connected.bulkUnshare")}
          </Button>
          {selected.size === 1 ? (
            <>
              {singleSellingPrice != null ? (
                <span
                  className="connected-share-bulk-bar__selling-ref"
                  data-testid="connected-buyer-selling-ref"
                >
                  {t("connected.buyerPriceSellingRef").replace(
                    "{price}",
                    formatPeso(singleSellingPrice),
                  )}
                </span>
              ) : null}
              <label
                className="connected-share-bulk-bar__price-label"
                htmlFor="connected-buyer-price-input"
              >
                {t("connected.buyerPrice")}
              </label>
              <input
                id="connected-buyer-price-input"
                ref={buyerPriceInputRef}
                className="connected-share-bulk-bar__price-input"
                inputMode="decimal"
                value={buyerPrice}
                onChange={(event) => setBuyerPrice(event.target.value)}
                data-testid="connected-buyer-price-input"
              />
              <Button
                type="button"
                variant="outline"
                disabled={busy}
                data-testid="connected-apply-buyer-price"
                onClick={() => void applyFixedPrice()}
              >
                <Tag className="size-4 shrink-0" aria-hidden />
                {t("connected.applyBuyerPrice")}
              </Button>
            </>
          ) : (
            <>
              <div
                className="connected-share-bulk-bar__percent-mode"
                role="group"
                aria-label={t("connected.percentModeLabel")}
              >
                <button
                  type="button"
                  className={
                    percentMode === "discount"
                      ? "connected-share-bulk-bar__mode-btn connected-share-bulk-bar__mode-btn--active"
                      : "connected-share-bulk-bar__mode-btn"
                  }
                  aria-pressed={percentMode === "discount"}
                  data-testid="connected-percent-mode-discount"
                  onClick={() => setPercentMode("discount")}
                >
                  {t("connected.percentModeDiscount")}
                </button>
                <button
                  type="button"
                  className={
                    percentMode === "increase"
                      ? "connected-share-bulk-bar__mode-btn connected-share-bulk-bar__mode-btn--active"
                      : "connected-share-bulk-bar__mode-btn"
                  }
                  aria-pressed={percentMode === "increase"}
                  data-testid="connected-percent-mode-increase"
                  onClick={() => setPercentMode("increase")}
                >
                  {t("connected.percentModeIncrease")}
                </button>
              </div>
              <label
                className="connected-share-bulk-bar__price-label"
                htmlFor="connected-percent-input"
                title={
                  percentMode === "discount"
                    ? t("connected.discountPercentHelp")
                    : t("connected.increasePercentHelp")
                }
              >
                {percentMode === "discount"
                  ? t("connected.discountPercent")
                  : t("connected.increasePercent")}
              </label>
              <input
                id="connected-percent-input"
                ref={percentInputRef}
                className="connected-share-bulk-bar__price-input connected-share-bulk-bar__price-input--percent"
                inputMode="decimal"
                value={percentValue}
                onChange={(event) => setPercentValue(event.target.value)}
                data-testid="connected-percent-input"
                placeholder={percentMode === "discount" ? "0–100" : "0–1000"}
                title={
                  percentMode === "discount"
                    ? t("connected.discountPercentHelp")
                    : t("connected.increasePercentHelp")
                }
              />
              <Button
                type="button"
                variant="outline"
                disabled={busy}
                data-testid="connected-apply-percent"
                onClick={() => void applyPercentChange()}
                title={
                  percentMode === "discount"
                    ? t("connected.discountPercentHelp")
                    : t("connected.increasePercentHelp")
                }
              >
                <Percent className="size-4 shrink-0" aria-hidden />
                {percentMode === "discount"
                  ? t("connected.applyDiscountPercent")
                  : t("connected.applyIncreasePercent")}
              </Button>
            </>
          )}
        </div>
      ) : null}
    </div>
  );
}
