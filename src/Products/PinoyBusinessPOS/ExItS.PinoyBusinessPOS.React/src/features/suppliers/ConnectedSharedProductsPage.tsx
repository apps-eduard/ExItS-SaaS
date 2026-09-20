import { useEffect, useMemo, useRef, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Building2, Link2Off, Percent, Share2, Tag, X } from "lucide-react";
import { canGovernOrganizationCatalog, canManageSuppliers } from "@/access/pos-capabilities";
import {
  applyBuyerProductPricing,
  bulkMutateBuyerProductShares,
  getBusinessCustomer,
  previewBuyerProductPricing,
  queryBuyerProductShares,
} from "@/api/pos/pos-connected-suppliers-client";
import { PosApiError } from "@/api/pos/pos-http";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { Notice } from "@/components/exits/Notice";
import { PageHeader } from "@/components/exits/PageHeader";
import { useToast } from "@/components/exits/ToastProvider";
import { UnderlineTabBar } from "@/components/exits/UnderlineTabBar";
import { SearchField } from "@/components/exits/SearchField";
import { StatusChip } from "@/components/exits/StatusChip";
import { isBranchLocalProduct } from "@/features/catalog/catalog-product-scope";
import {
  actionableShareProductIds,
  isShareRowSelectable,
  rowCanShare,
  rowCanStopSharing,
  shareRowHintReason,
  shareRowNeedsPrice,
} from "@/features/suppliers/connected-share-row-actionability";
import { useI18n } from "@/i18n/I18nProvider";
import { formatPeso } from "@/lib/format-money";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const PAGE_SIZE = 25;

function catalogModeLabel(
  mode: string | undefined,
  allEligible: string,
  selectedOnly: string,
): string {
  return mode === "AllEligible" ? allEligible : selectedOnly;
}

function sharingTone(status: string): "success" | "warning" | "neutral" {
  if (status === "Shared") {
    return "success";
  }
  if (status === "Ineligible") {
    return "neutral";
  }
  return "warning";
}

export function ConnectedSharedProductsPage() {
  const { t } = useI18n();
  const { toast } = useToast();
  const { relationshipId } = useParams<{ relationshipId: string }>();
  const queryClient = useQueryClient();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");
  const [shareFilter, setShareFilter] = useState<"all" | "shared" | "notShared" | "ineligible">(
    "all",
  );
  const [page, setPage] = useState(1);
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [buyerPrice, setBuyerPrice] = useState("");
  const [percentValue, setPercentValue] = useState("");
  const [percentMode, setPercentMode] = useState<"discount" | "increase">("discount");
  const [message, setMessage] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [summaryDismissed, setSummaryDismissed] = useState(false);
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

  const buyerQuery = useQuery({
    queryKey: ["connected-suppliers", "business-customer", relationshipId, workspace?.organizationId],
    enabled: Boolean(workspace) && Boolean(relationshipId),
    queryFn: ({ signal }) => getBusinessCustomer(workspace!, relationshipId!, signal),
  });

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

  const shareItems = useMemo(
    () =>
      (query.data?.items ?? []).filter(
        (item) => !isBranchLocalProduct({ scope: item.scope ?? undefined }),
      ),
    [query.data?.items],
  );

  const actionablePageIds = useMemo(
    () => actionableShareProductIds(shareItems),
    [shareItems],
  );

  function toggle(productId: string) {
    const item = shareItems.find((row) => row.supplierProductId === productId);
    if (!item || !isShareRowSelectable(item)) {
      return;
    }
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
    const targetIds = shareItems
      .filter((item) => selected.has(item.supplierProductId))
      .filter((item) =>
        operation === "Share" ? rowCanShare(item) : rowCanStopSharing(item),
      )
      .map((item) => item.supplierProductId);
    if (targetIds.length === 0) {
      return;
    }
    setBusy(true);
    setMessage(null);
    try {
      const result = await bulkMutateBuyerProductShares(workspace, relationshipId, {
        operation,
        productIds: targetIds,
      });
      if (result.needsDefaultPo && result.needsDefaultPo.length > 0 && result.affectedCount === 0) {
        setMessage(
          t("connected.bulkNeedsDefaultPo").replace(
            "{count}",
            String(result.needsDefaultPo.length),
          ),
        );
      } else if (operation === "Share") {
        const already = result.alreadySharedCount ?? 0;
        if (result.affectedCount > 0 && already > 0) {
          setMessage(
            t("connected.bulkSharedPartial")
              .replace("{count}", String(result.affectedCount))
              .replace("{already}", String(already)),
          );
        } else if (result.affectedCount > 0) {
          setMessage(t("connected.bulkShared").replace("{count}", String(result.affectedCount)));
        } else if (already > 0) {
          setMessage(t("connected.bulkAlreadyShared").replace("{count}", String(already)));
        } else {
          setMessage(null);
        }
      } else {
        const already = result.alreadyNotSharedCount ?? 0;
        if (result.affectedCount > 0 && already > 0) {
          setMessage(
            t("connected.bulkUnsharedPartial")
              .replace("{count}", String(result.affectedCount))
              .replace("{already}", String(already)),
          );
        } else if (result.affectedCount > 0) {
          setMessage(t("connected.bulkUnshared").replace("{count}", String(result.affectedCount)));
        } else if (already > 0) {
          setMessage(t("connected.bulkAlreadyNotShared").replace("{count}", String(already)));
        } else {
          setMessage(null);
        }
      }
      setSelected(new Set());
      await invalidateAfterShareMutation();
    } catch (err) {
      if (
        err instanceof PosApiError &&
        err.problem.errorCode === "pos.catalog.connected_share_requires_tracked"
      ) {
        toast.error(
          t("catalog.connectedShare.cantShareTitle"),
          err.problem.detail ?? t("catalog.connectedShare.cantShareMessage"),
        );
        setMessage(null);
      } else {
        setMessage(
          err instanceof PosApiError
            ? (err.problem.detail ?? err.message)
            : t("connected.saveSharingFailed"),
        );
      }
    } finally {
      setBusy(false);
    }
  }

  async function invalidateAfterShareMutation() {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ["connected-suppliers", "shares"] }),
      queryClient.invalidateQueries({ queryKey: ["connected-suppliers", "business-customer"] }),
      queryClient.invalidateQueries({ queryKey: ["connected-suppliers", "catalog"] }),
      queryClient.invalidateQueries({ queryKey: ["connected-suppliers", "commerce-readiness"] }),
      queryClient.invalidateQueries({ queryKey: ["business-customers", "commerce-readiness"] }),
      queryClient.invalidateQueries({ queryKey: ["business-customers"] }),
      queryClient.invalidateQueries({ queryKey: ["shell", "needs-attention"] }),
    ]);
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
      await invalidateAfterShareMutation();
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
      await invalidateAfterShareMutation();
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

  const allPageSelected =
    actionablePageIds.length > 0 && actionablePageIds.every((id) => selected.has(id));
  const somePageSelected = actionablePageIds.some((id) => selected.has(id));

  function toggleSelectAllPage() {
    setSelected((current) => {
      const next = new Set(current);
      const allSelected =
        actionablePageIds.length > 0 && actionablePageIds.every((id) => current.has(id));
      if (allSelected) {
        for (const id of actionablePageIds) {
          next.delete(id);
        }
      } else {
        for (const id of actionablePageIds) {
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
      {buyerQuery.data ? (
        <section
          className="connected-share-buyer-context"
          data-testid="connected-share-buyer-context"
        >
          <div className="connected-share-buyer-context__identity">
            <h2 className="connected-share-buyer-context__name">
              {buyerQuery.data.organizationDisplayName}
            </h2>
            <p className="connected-share-buyer-context__meta">
              {t("connected.buyerContext.b2bConnected")}
            </p>
          </div>
          <dl className="connected-share-buyer-context__facts">
            <div>
              <dt>{t("connected.buyerContext.organizationId")}</dt>
              <dd>
                {buyerQuery.data.organizationPublicId?.trim()
                  || buyerQuery.data.buyerOrganizationId}
              </dd>
            </div>
            <div>
              <dt>{t("connected.buyerContext.sellingBranch")}</dt>
              <dd>
                {buyerQuery.data.supplierBranchName?.trim()
                  || t("connected.buyerContext.branchUnset")}
              </dd>
            </div>
            <div>
              <dt>{t("connected.buyerContext.catalogSharing")}</dt>
              <dd>
                {catalogModeLabel(
                  buyerQuery.data.catalogSharingMode,
                  t("customers.business.modeAllEligible"),
                  t("customers.business.modeSelectedOnly"),
                )}
              </dd>
            </div>
            <div>
              <dt>{t("connected.buyerContext.customerPricing")}</dt>
              <dd>
                {buyerQuery.data.customerDiscountPercent != null
                && buyerQuery.data.customerDiscountPercent > 0
                  ? t("connected.customerDiscountBanner").replace(
                      "{percent}",
                      String(buyerQuery.data.customerDiscountPercent),
                    )
                  : t("customers.business.noDiscount")}
              </dd>
            </div>
          </dl>
          <Link
            to={`/customers/business/${relationshipId}`}
            className="connected-share-buyer-context__link"
            data-testid="connected-share-view-customer"
          >
            {t("connected.buyerContext.viewCustomer")}
          </Link>
        </section>
      ) : null}
      <p
        className="m-0 text-[length:var(--exits-text-sm)] text-muted"
        data-testid="connected-exposable-note"
      >
        {t("connected.manageSharedIntent")}
      </p>
      <p
        className="m-0 text-[length:var(--exits-text-sm)] text-muted"
        data-testid="connected-inventory-never-shared"
      >
        {t("connected.inventoryNeverShared")}
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
              ["ineligible", "connected.filterIneligible"],
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
      {query.data && !summaryDismissed ? (
        <div className="relative" data-testid="connected-share-summary">
          <Notice
            tone="info"
            testId="connected-share-summary-notice"
            className="pe-10"
            title={
              query.data.catalogSharingMode === "AllEligible"
                ? t("connected.shareSummaryAllEligible")
                    .replace("{shared}", String(query.data.sharedCount))
                    .replace("{eligible}", String(query.data.eligibleCount))
                : t("connected.shareSummary")
                    .replace("{shared}", String(query.data.sharedCount))
                    .replace("{eligible}", String(query.data.eligibleCount))
            }
          >
            <p>{t("connected.shareSummaryBuyerVisibilityHelp")}</p>
            {query.data.customerDiscountPercent != null
            && query.data.customerDiscountPercent > 0 ? (
              <p className="mt-1">
                {t("connected.customerDiscountBanner").replace(
                  "{percent}",
                  String(query.data.customerDiscountPercent),
                )}
              </p>
            ) : query.data.catalogSharingMode === "AllEligible" ? (
              <p className="mt-1">{t("connected.sellingPriceBaselineBanner")}</p>
            ) : null}
          </Notice>
          <Button
            type="button"
            intent="neutral"
            appearance="ghost"
            size="icon"
            className="absolute end-1 top-1 size-8 shrink-0"
            aria-label={t("connected.shareSummary.dismiss")}
            data-testid="connected-share-summary-dismiss"
            onClick={() => setSummaryDismissed(true)}
          >
            <X className="size-4" aria-hidden />
          </Button>
        </div>
      ) : null}
      {allowManage && actionablePageIds.length > 0 ? (
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
                : t("connected.selectAllPage").replace("{count}", String(actionablePageIds.length))}
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
                disabled={actionablePageIds.length === 0}
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
                    : t("connected.selectAllPage").replace(
                        "{count}",
                        String(actionablePageIds.length),
                      )
                }
              />
            </span>
          ) : null}
          <span>{t("connected.colProduct")}</span>
          <span>{t("connected.colTracking")}</span>
          <span>{t("connected.colSharing")}</span>
          <span className="connected-share-table__price-head">{t("connected.listPrice")}</span>
          <span className="connected-share-table__price-head">{t("connected.customerPrice")}</span>
        </div>
        <ul className="connected-share-table__list">
          {shareItems.map((item) => {
            const customerPrice =
              item.effectiveSupplierOrderPrice
              ?? item.buyerSpecificPoPrice
              ?? item.resolvedPoPrice
              ?? (item.isEffectivelyShared || item.sharingStatus === "Shared"
                ? item.sellingPrice != null && item.sellingPrice > 0
                  ? item.sellingPrice
                  : item.defaultPoPrice
                : null)
              ?? null;
            const listPrice =
              item.sellingPrice != null && item.sellingPrice > 0
                ? item.sellingPrice
                : item.defaultPoPrice;
            const sharingStatus =
              item.sharingStatus
              || (item.isEffectivelyShared
                ? "Shared"
                : item.isExplicitlyExcluded
                  ? query.data?.catalogSharingMode === "AllEligible"
                    ? "Excluded"
                    : "NotShared"
                  : item.isEligible === false
                    ? "Ineligible"
                    : "NotShared");
            const statusLabel =
              sharingStatus === "Shared"
                ? t("connected.shared")
                : sharingStatus === "Excluded"
                  ? t("connected.excluded")
                  : sharingStatus === "Ineligible"
                    ? t("connected.ineligible")
                    : t("connected.notShared");
            const tracked = item.isInventoryTracked === true;
            const selectable = isShareRowSelectable(item);
            const hintReason = shareRowHintReason(item);
            const needsPrice = shareRowNeedsPrice(item);
            const hintText =
              hintReason === "needsTracking"
                ? t("connected.enableTrackingBeforeShare")
                : hintReason === "needsPrice"
                  ? t("connected.setPriceBeforeShare")
                  : undefined;
            return (
              <li key={item.supplierProductId}>
                <div
                  className="connected-share-table__row"
                  data-testid={`connected-share-row-${item.supplierProductId}`}
                >
                  {allowManage ? (
                    <span className="connected-share-table__check">
                      <input
                        type="checkbox"
                        className="size-5"
                        checked={selected.has(item.supplierProductId)}
                        disabled={!selectable}
                        title={hintText}
                        onChange={() => toggle(item.supplierProductId)}
                        data-testid={`connected-share-check-${item.supplierProductId}`}
                        aria-label={item.nameSnapshot ?? item.supplierProductId}
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
                    {needsPrice ? (
                      <span
                        className="connected-share-table__hint"
                        data-testid={`connected-share-needs-price-${item.supplierProductId}`}
                      >
                        {t("connected.needsPrice")}
                      </span>
                    ) : null}
                    {!selectable && hintText && !needsPrice ? (
                      <span
                        className="connected-share-table__hint"
                        data-testid={`connected-share-hint-${item.supplierProductId}`}
                      >
                        {hintText}
                      </span>
                    ) : null}
                  </span>
                  <span
                    className="connected-share-table__tracking"
                    data-label={t("connected.colTracking")}
                    data-testid={`connected-share-tracking-${item.supplierProductId}`}
                  >
                    {tracked ? t("inventory.tracked") : t("inventory.notTracked")}
                  </span>
                  <span
                    className="connected-share-table__status"
                    data-testid={`connected-share-status-${item.supplierProductId}`}
                  >
                    <StatusChip tone={sharingTone(sharingStatus)}>
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
                </div>
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
