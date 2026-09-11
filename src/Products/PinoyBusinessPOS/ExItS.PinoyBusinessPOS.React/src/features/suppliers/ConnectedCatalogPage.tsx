import { useEffect, useMemo, useState } from "react";
import { Link, useParams, useSearchParams } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Check, Plus } from "lucide-react";
import {
  canManageCatalog,
  canManagePurchasing,
  canViewPurchasing,
} from "@/access/pos-capabilities";
import {
  autoLinkExactMatches,
  classifyCatalogReadiness,
  createBuyerProductAndLink,
  linkProduct,
  type CatalogProductReadinessItem,
} from "@/api/pos/pos-connected-suppliers-client";
import { getSupplier, isConnectedSupplier } from "@/api/pos/pos-suppliers-client";
import { PosApiError } from "@/api/pos/pos-http";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { UnderlineTabBar } from "@/components/exits/UnderlineTabBar";
import { SearchField } from "@/components/exits/SearchField";
import { StatusChip } from "@/components/exits/StatusChip";
import { formatUnitOfMeasureLabel } from "@/features/purchasing/purchase-order-create-connected";
import {
  isBulkConnectSelectable,
  partitionBulkConnectSelection,
} from "@/features/suppliers/connected-catalog-bulk";
import {
  countByUserState,
  filterReadinessItems,
  mapBackendStatusToUserState,
  type CatalogReadinessFilter,
  type UserCatalogState,
} from "@/features/suppliers/connected-catalog-readiness";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";
import { formatPeso } from "@/lib/format-money";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const PAGE_SIZE = 25;

function statusTone(state: UserCatalogState): "success" | "info" | "warning" | "danger" {
  switch (state) {
    case "linked":
      return "success";
    case "newProduct":
      return "info";
    case "checkMatch":
      return "warning";
    case "attention":
      return "danger";
    default:
      return "info";
  }
}

function statusLabel(
  t: (key: Parameters<ReturnType<typeof useI18n>["t"]>[0]) => string,
  state: UserCatalogState,
): string {
  switch (state) {
    case "linked":
      return t("connected.statusLinked");
    case "newProduct":
      return t("connected.statusNewProduct");
    case "checkMatch":
      return t("connected.statusCheckMatch");
    case "attention":
      return t("connected.statusAttention");
    default:
      return t("connected.statusPending");
  }
}

export function ConnectedCatalogPage() {
  const { t } = useI18n();
  const { supplierId } = useParams<{ supplierId: string }>();
  const [searchParams] = useSearchParams();
  const queryClient = useQueryClient();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");
  const [page, setPage] = useState(1);
  const [readinessFilter, setReadinessFilter] = useState<CatalogReadinessFilter>(() => {
    const setup = searchParams.get("setup");
    if (
      setup === "newProduct" ||
      setup === "checkMatch" ||
      setup === "attention" ||
      setup === "linked" ||
      setup === "all"
    ) {
      return setup;
    }
    return "all";
  });
  const [message, setMessage] = useState<string | null>(null);
  const [busyKey, setBusyKey] = useState<string | null>(null);
  const [bulkBusy, setBulkBusy] = useState(false);
  const [selected, setSelected] = useState<Set<string>>(() => new Set());
  const [selectedConflictByExposure, setSelectedConflictByExposure] = useState<
    Record<string, string>
  >({});

  useEffect(() => {
    const handle = window.setTimeout(() => setDebounced(search.trim()), 250);
    return () => window.clearTimeout(handle);
  }, [search]);

  useEffect(() => {
    setPage(1);
    setSelected(new Set());
  }, [debounced, readinessFilter]);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const allowView = canViewPurchasing(sessionGrant);
  const allowLink = canManagePurchasing(sessionGrant);
  const allowCreate = canManagePurchasing(sessionGrant) && canManageCatalog(sessionGrant);

  const supplierQuery = useQuery({
    queryKey: ["suppliers", "detail", workspace?.organizationId, supplierId],
    enabled: Boolean(workspace) && Boolean(supplierId),
    queryFn: ({ signal }) => getSupplier(workspace!, supplierId!, signal),
  });

  const relationshipId = supplierQuery.data?.connectedRelationshipId ?? null;

  const readinessQuery = useQuery({
    queryKey: ["connected-suppliers", "readiness", relationshipId],
    enabled: Boolean(workspace) && Boolean(relationshipId) && allowView,
    queryFn: ({ signal }) => classifyCatalogReadiness(workspace!, relationshipId!, signal),
  });

  useEffect(() => {
    if (!workspace || !relationshipId || !allowLink) {
      return;
    }

    let cancelled = false;
    void (async () => {
      try {
        // Explicit command (not Classify GET). Idempotent; Strict Mode may invoke twice in dev.
        const result = await autoLinkExactMatches(workspace, relationshipId);
        if (cancelled) {
          return;
        }
        if (result.linkedNow > 0) {
          setMessage(
            t("connected.autoLinkedBanner").replace("{count}", String(result.linkedNow)),
          );
        }
        await queryClient.invalidateQueries({
          queryKey: ["connected-suppliers", "readiness", relationshipId],
        });
      } catch {
        // Auto-link is best-effort; classify remains authoritative and read-only.
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [allowLink, queryClient, relationshipId, t, workspace]);

  const counts = useMemo(() => countByUserState(readinessQuery.data), [readinessQuery.data]);

  const filteredItems = useMemo(() => {
    if (!readinessQuery.data) {
      return [] as CatalogProductReadinessItem[];
    }
    return filterReadinessItems(readinessQuery.data.items, readinessFilter, debounced);
  }, [debounced, readinessFilter, readinessQuery.data]);

  const totalPages = Math.max(1, Math.ceil(filteredItems.length / PAGE_SIZE));
  const pageItems = useMemo(() => {
    const start = (page - 1) * PAGE_SIZE;
    return filteredItems.slice(start, start + PAGE_SIZE);
  }, [filteredItems, page]);

  const selectablePageItems = useMemo(
    () => pageItems.filter(isBulkConnectSelectable),
    [pageItems],
  );
  const allSelectableSelected =
    selectablePageItems.length > 0
    && selectablePageItems.every((item) => selected.has(item.exposureId));
  const bulkPartition = useMemo(
    () => partitionBulkConnectSelection(filteredItems, selected),
    [filteredItems, selected],
  );

  function toggleSelected(exposureId: string) {
    setSelected((current) => {
      const next = new Set(current);
      if (next.has(exposureId)) {
        next.delete(exposureId);
      } else {
        next.add(exposureId);
      }
      return next;
    });
  }

  function toggleSelectAllPage() {
    setSelected((current) => {
      const next = new Set(current);
      if (allSelectableSelected) {
        for (const item of selectablePageItems) {
          next.delete(item.exposureId);
        }
      } else {
        for (const item of selectablePageItems) {
          next.add(item.exposureId);
        }
      }
      return next;
    });
  }

  async function refreshAfterMutation() {
    await queryClient.invalidateQueries({ queryKey: ["connected-suppliers"] });
  }

  async function doLink(exposureId: string, buyerProductId: string) {
    if (!workspace || !relationshipId || !allowLink) {
      return;
    }
    setBusyKey(exposureId);
    setMessage(null);
    try {
      await linkProduct(workspace, relationshipId, { exposureId, buyerProductId });
      setMessage(t("connected.linkSucceeded"));
      setSelected((current) => {
        const next = new Set(current);
        next.delete(exposureId);
        return next;
      });
      await refreshAfterMutation();
    } catch (err) {
      setMessage(
        err instanceof PosApiError
          ? (err.problem.detail ?? err.message)
          : t("connected.linkFailed"),
      );
    } finally {
      setBusyKey(null);
    }
  }

  async function doCreateAndLink(
    exposureId: string,
    name: string,
    uom: string,
    supplierPoPrice: number,
  ) {
    if (!workspace || !relationshipId || !allowCreate) {
      return;
    }
    setBusyKey(`create-${exposureId}`);
    setMessage(null);
    try {
      await createBuyerProductAndLink(workspace, relationshipId, {
        exposureId,
        name,
        unitOfMeasure: uom,
        // Seed a starting sell price from the shared PO price so the product is sellable;
        // buyer can change it later in catalog.
        sellingPrice: Number.isFinite(supplierPoPrice) && supplierPoPrice > 0 ? supplierPoPrice : 0,
        businessUsage: "Resale",
      });
      setMessage(t("connected.createAndLinkSucceeded"));
      setSelected((current) => {
        const next = new Set(current);
        next.delete(exposureId);
        return next;
      });
      await refreshAfterMutation();
    } catch (err) {
      setMessage(
        err instanceof PosApiError
          ? (err.problem.detail ?? err.message)
          : t("connected.createAndLinkFailed"),
      );
    } finally {
      setBusyKey(null);
    }
  }

  async function runBulkConfirmMatches() {
    if (!workspace || !relationshipId || !allowLink || bulkBusy) {
      return;
    }
    const targets = bulkPartition.confirmMatch;
    if (targets.length === 0) {
      return;
    }
    setBulkBusy(true);
    setMessage(null);
    let ok = 0;
    let failed = 0;
    for (const item of targets) {
      const buyerProductId = item.candidateBuyerProductId;
      if (!buyerProductId) {
        failed += 1;
        continue;
      }
      try {
        await linkProduct(workspace, relationshipId, {
          exposureId: item.exposureId,
          buyerProductId,
        });
        ok += 1;
      } catch {
        failed += 1;
      }
    }
    setSelected(new Set());
    setMessage(
      t("connected.bulkConfirmResult")
        .replace("{ok}", String(ok))
        .replace("{failed}", String(failed)),
    );
    await refreshAfterMutation();
    setBulkBusy(false);
  }

  async function runBulkAddAsNew() {
    if (!workspace || !relationshipId || !allowCreate || bulkBusy) {
      return;
    }
    const targets = bulkPartition.addAsNew;
    if (targets.length === 0) {
      return;
    }
    setBulkBusy(true);
    setMessage(null);
    let ok = 0;
    let failed = 0;
    for (const item of targets) {
      try {
        await createBuyerProductAndLink(workspace, relationshipId, {
          exposureId: item.exposureId,
          name: item.supplierName,
          unitOfMeasure: item.unitOfMeasureCode,
          sellingPrice:
            Number.isFinite(item.poPrice) && item.poPrice > 0 ? item.poPrice : 0,
          businessUsage: "Resale",
        });
        ok += 1;
      } catch {
        failed += 1;
      }
    }
    setSelected(new Set());
    setMessage(
      t("connected.bulkAddAsNewResult")
        .replace("{ok}", String(ok))
        .replace("{failed}", String(failed)),
    );
    await refreshAfterMutation();
    setBulkBusy(false);
  }

  if (!workspace || !supplierId) {
    return <LoadingState label={t("session.loading")} />;
  }

  if (supplierQuery.isLoading) {
    return <LoadingState label={t("loading.label")} />;
  }

  if (supplierQuery.isError || !supplierQuery.data) {
    return <ErrorState title={t("error.title")} detail={t("suppliers.notFound")} />;
  }

  if (!isConnectedSupplier(supplierQuery.data) || !relationshipId) {
    return (
      <EmptyState
        title={t("connected.relationshipMissing")}
        detail={t("connected.relationshipMissingHelp")}
      />
    );
  }

  if (!allowView) {
    return <ErrorState title={t("error.title")} detail={t("connected.catalogDenied")} />;
  }

  const denied =
    readinessQuery.isError &&
    readinessQuery.error instanceof PosApiError &&
    (readinessQuery.error.status === 403 || readinessQuery.error.status === 404);

  return (
    <div className="flex min-w-0 flex-col gap-3" data-testid="connected-catalog-page">
      <PageHeader
        title={t("connected.catalogTitle")}
        description={t("connected.catalogHelp")}
        subtitle={supplierQuery.data.name}
        backTo={`/suppliers/${supplierId}`}
        backLabel={t("connected.backToSupplier")}
        backTestId="page-header-back-suppliers"
        trailing={
          <Link
            to={`/suppliers/${supplierId}/linked-products`}
            className="text-[length:var(--exits-text-sm)] font-semibold text-foreground underline-offset-4 hover:underline"
            data-testid="connected-open-linked"
          >
            {t("connected.openLinkedProducts")}
          </Link>
        }
      />
      {message ? (
        <p
          className="m-0 rounded-[var(--exits-radius-md)] bg-[color-mix(in_srgb,var(--exits-success)_12%,transparent)] px-3 py-2 text-[length:var(--exits-text-sm)]"
          data-testid="connected-catalog-message"
          role="status"
        >
          {message}
        </p>
      ) : null}
      <div className="po-setup-filter-row">
        {readinessQuery.isSuccess ? (
          <UnderlineTabBar
            className="po-setup-filter-tabs"
            items={(
              [
                ["all", counts.all, "connected.filterAllCount"],
                ["newProduct", counts.newProduct, "connected.filterNewProducts"],
                ["checkMatch", counts.checkMatch, "connected.filterCheckMatch"],
                ["attention", counts.attention, "connected.filterAttention"],
                ["linked", counts.linked, "connected.filterLinked"],
              ] as const
            ).map(([value, count, key]) => ({
              key: value,
              label: t(key).replace("{count}", String(count)),
              testId: `connected-ready-${value}`,
            }))}
            activeKey={readinessFilter}
            onChange={(key) => setReadinessFilter(key as CatalogReadinessFilter)}
            ariaLabel={t("connected.readinessFilters")}
            testId="connected-readiness-chips"
          />
        ) : null}
        <SearchField
          label={t("connected.catalogSearch")}
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          onClear={() => setSearch("")}
          placeholder={t("connected.catalogSearch")}
          data-testid="connected-catalog-search"
          containerClassName="po-setup-filter-search"
        />
      </div>
      {readinessQuery.isLoading ? <LoadingState label={t("loading.label")} /> : null}
      {denied ? (
        <ErrorState title={t("error.title")} detail={t("connected.catalogDenied")} />
      ) : null}
      {readinessQuery.isError && !denied ? (
        <ErrorState
          title={t("error.title")}
          detail={
            readinessQuery.error instanceof PosApiError
              ? (readinessQuery.error.problem.detail ?? readinessQuery.error.message)
              : t("connected.loadFailed")
          }
        />
      ) : null}
      {readinessQuery.isSuccess && readinessQuery.data.items.length === 0 && !debounced ? (
        <EmptyState title={t("connected.catalogEmpty")} detail={t("connected.catalogEmptyHelp")} />
      ) : null}
      {readinessQuery.isSuccess && filteredItems.length === 0 && Boolean(debounced) ? (
        <EmptyState
          title={t("connected.catalogNoMatch")}
          detail={t("connected.catalogNoMatchHelp")}
        />
      ) : null}
      {readinessQuery.isSuccess && pageItems.length > 0 ? (
        <div
          className={
            readinessFilter === "checkMatch" || readinessFilter === "attention"
              ? "po-order-table po-order-table--setup po-order-table--setup-pair po-order-table--catalog"
              : "po-order-table po-order-table--setup po-order-table--catalog"
          }
          data-testid="connected-catalog-list"
        >
          <div className="po-order-table__head">
            <span className="po-order-table__check-head">
              {selectablePageItems.length > 0 ? (
                <input
                  type="checkbox"
                  className="size-4"
                  checked={allSelectableSelected}
                  disabled={bulkBusy}
                  aria-label={
                    allSelectableSelected
                      ? t("connected.deselectAllPage")
                      : t("connected.selectAllPage").replace(
                          "{count}",
                          String(selectablePageItems.length),
                        )
                  }
                  data-testid="connected-catalog-select-all"
                  onChange={toggleSelectAllPage}
                />
              ) : null}
            </span>
            <span>{t("purchasing.colProduct")}</span>
            <span>{t("purchasing.colSku")}</span>
            <span>{t("purchasing.colUnit")}</span>
            <span className="po-order-table__price-head">{t("purchasing.colPrice")}</span>
            <span>{t("connected.colStatus")}</span>
            <span className="po-order-table__action-head">{t("purchasing.colAction")}</span>
          </div>
          <ul className="po-order-table__list">
            {pageItems.map((item) => {
              const state = mapBackendStatusToUserState(item.status);
              const selectedConflictId =
                selectedConflictByExposure[item.exposureId] ??
                item.conflictCandidates[0]?.productId ??
                null;
              const busy = busyKey != null || bulkBusy;
              const canSelect = isBulkConnectSelectable(item);
              const isSelected = selected.has(item.exposureId);
              return (
                <li key={item.exposureId}>
                  <div
                    className="po-order-table__row"
                    data-testid={`connected-catalog-item-${item.exposureId}`}
                  >
                    <span className="po-order-table__check">
                      {canSelect ? (
                        <input
                          type="checkbox"
                          className="size-4"
                          checked={isSelected}
                          disabled={bulkBusy}
                          aria-label={item.supplierName}
                          data-testid={`connected-catalog-select-${item.exposureId}`}
                          onChange={() => toggleSelected(item.exposureId)}
                        />
                      ) : null}
                    </span>
                    <span className="po-order-table__product">
                      <span className="po-order-table__name">{item.supplierName}</span>
                      {state === "checkMatch" ? (
                        <span
                          className="mt-0.5 block text-[length:var(--exits-text-xs)] text-muted"
                          data-testid={`connected-check-match-${item.exposureId}`}
                        >
                          {t("connected.candidateLabel")}
                          {": "}
                          {item.candidateBuyerProductName ?? t("connected.candidateUnknown")}
                        </span>
                      ) : null}
                      {state === "newProduct" ? (
                        <span
                          className="mt-0.5 block text-[length:var(--exits-text-xs)] text-muted"
                          data-testid={`connected-new-help-${item.exposureId}`}
                        >
                          {t("connected.newProductHelp")}
                        </span>
                      ) : null}
                      {state === "attention" ? (
                        <div
                          className="mt-1 grid gap-1.5"
                          data-testid={`connected-attention-${item.exposureId}`}
                        >
                          <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                            {item.matchDetails || t("connected.conflictHelp")}
                          </p>
                          {item.conflictCandidates.length > 0 ? (
                            <ul className="m-0 grid list-none gap-1 p-0" role="radiogroup">
                              {item.conflictCandidates.map((candidate) => {
                                const picked = selectedConflictId === candidate.productId;
                                return (
                                  <li key={candidate.productId}>
                                    <label
                                      className={cn(
                                        "flex cursor-pointer items-center gap-2 rounded-[var(--exits-radius-md)] border px-2 py-1",
                                        picked
                                          ? "border-[var(--exits-primary)] bg-[color-mix(in_srgb,var(--exits-primary)_10%,transparent)]"
                                          : "border-border bg-surface",
                                      )}
                                    >
                                      <input
                                        type="radio"
                                        className="size-3.5 shrink-0"
                                        name={`conflict-${item.exposureId}`}
                                        checked={picked}
                                        onChange={() =>
                                          setSelectedConflictByExposure((current) => ({
                                            ...current,
                                            [item.exposureId]: candidate.productId,
                                          }))
                                        }
                                        data-testid={`connected-conflict-pick-${candidate.productId}`}
                                      />
                                      <span className="min-w-0 text-[length:var(--exits-text-xs)]">
                                        <span className="block font-medium">{candidate.name}</span>
                                        <span className="block text-muted">
                                          {candidate.sku ? `${candidate.sku} · ` : ""}
                                          {formatUnitOfMeasureLabel(candidate.unitOfMeasureCode)}
                                        </span>
                                      </span>
                                    </label>
                                  </li>
                                );
                              })}
                            </ul>
                          ) : (
                            <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                              {t("connected.conflictNoCandidates")}
                            </p>
                          )}
                        </div>
                      ) : null}
                      {state === "unclassified" ? (
                        <span
                          className="mt-0.5 block text-[length:var(--exits-text-xs)] text-muted"
                          data-testid={`connected-unclassified-${item.exposureId}`}
                        >
                          {t("connected.statusPending")}
                        </span>
                      ) : null}
                    </span>
                    <span className="po-order-table__sku">
                      {item.supplierSku ?? t("connected.noSku")}
                    </span>
                    <span className="po-order-table__unit">
                      {formatUnitOfMeasureLabel(item.unitOfMeasureCode)}
                    </span>
                    <span className="po-order-table__price tabular-nums">
                      {Number.isFinite(item.poPrice) ? formatPeso(item.poPrice) : "—"}
                    </span>
                    <span className="po-order-table__status">
                      <StatusChip tone={statusTone(state)}>{statusLabel(t, state)}</StatusChip>
                    </span>
                    <span
                      className={
                        state === "checkMatch"
                          ? "po-order-table__action po-order-table__action--pair"
                          : "po-order-table__action po-order-table__action--stack"
                      }
                    >
                      {state === "newProduct" && allowCreate ? (
                        <Button
                          type="button"
                          variant="outline"
                          className="po-setup-connect-btn po-setup-connect-btn--add"
                          data-testid={`connected-create-link-${item.exposureId}`}
                          disabled={busy}
                          onClick={() =>
                            void doCreateAndLink(
                              item.exposureId,
                              item.supplierName,
                              item.unitOfMeasureCode,
                              item.poPrice,
                            )
                          }
                        >
                          <Plus className="size-3.5 shrink-0" aria-hidden />
                          {t("connected.createAndLink")}
                        </Button>
                      ) : null}
                      {state === "checkMatch" && allowLink && item.candidateBuyerProductId ? (
                        <Button
                          type="button"
                          variant="outline"
                          className="po-setup-connect-btn po-setup-connect-btn--confirm"
                          data-testid={`connected-confirm-match-${item.exposureId}`}
                          disabled={busy}
                          onClick={() =>
                            void doLink(item.exposureId, item.candidateBuyerProductId!)
                          }
                        >
                          <Check className="size-3.5 shrink-0" aria-hidden />
                          {t("connected.confirmMatch")}
                        </Button>
                      ) : null}
                      {state === "checkMatch" && allowCreate ? (
                        <Button
                          type="button"
                          variant="outline"
                          className="po-setup-connect-btn po-setup-connect-btn--new"
                          data-testid={`connected-add-as-new-${item.exposureId}`}
                          disabled={busy}
                          onClick={() =>
                            void doCreateAndLink(
                              item.exposureId,
                              item.supplierName,
                              item.unitOfMeasureCode,
                              item.poPrice,
                            )
                          }
                        >
                          <Plus className="size-3.5 shrink-0" aria-hidden />
                          {t("connected.addAsNew")}
                        </Button>
                      ) : null}
                      {state === "attention" && allowLink && selectedConflictId ? (
                        <Button
                          type="button"
                          variant="outline"
                          className="po-setup-connect-btn po-setup-connect-btn--confirm"
                          data-testid={`connected-link-selected-${item.exposureId}`}
                          disabled={busy}
                          onClick={() => void doLink(item.exposureId, selectedConflictId)}
                        >
                          <Check className="size-3.5 shrink-0" aria-hidden />
                          {t("connected.linkSelected")}
                        </Button>
                      ) : null}
                      {state === "attention" && allowCreate ? (
                        <Button
                          type="button"
                          variant="outline"
                          className="po-setup-connect-btn po-setup-connect-btn--new"
                          data-testid={`connected-add-as-new-${item.exposureId}`}
                          disabled={busy}
                          onClick={() =>
                            void doCreateAndLink(
                              item.exposureId,
                              item.supplierName,
                              item.unitOfMeasureCode,
                              item.poPrice,
                            )
                          }
                        >
                          <Plus className="size-3.5 shrink-0" aria-hidden />
                          {t("connected.addAsNew")}
                        </Button>
                      ) : null}
                      {!allowLink && !allowCreate && state !== "linked" ? (
                        <span className="text-[length:var(--exits-text-xs)] text-muted">
                          {t("connected.catalogPermissionRequired")}
                        </span>
                      ) : null}
                    </span>
                  </div>
                </li>
              );
            })}
          </ul>
        </div>
      ) : null}
      {selected.size > 0 ? (
        <div className="connected-share-bulk-bar" data-testid="connected-catalog-bulk-bar">
          <span className="connected-share-bulk-bar__count">
            {t("connected.bulkSelectedCount").replace("{count}", String(selected.size))}
          </span>
          {allowLink && bulkPartition.confirmMatch.length > 0 ? (
            <Button
              type="button"
              variant="outline"
              className="po-setup-connect-btn po-setup-connect-btn--confirm po-setup-bulk-btn"
              disabled={bulkBusy}
              data-testid="connected-bulk-confirm-matches"
              onClick={() => void runBulkConfirmMatches()}
            >
              <Check className="size-3.5 shrink-0" aria-hidden />
              {t("connected.bulkConfirmMatches").replace(
                "{count}",
                String(bulkPartition.confirmMatch.length),
              )}
            </Button>
          ) : null}
          {allowCreate && bulkPartition.addAsNew.length > 0 ? (
            <Button
              type="button"
              variant="outline"
              className="po-setup-connect-btn po-setup-connect-btn--new po-setup-bulk-btn"
              disabled={bulkBusy}
              data-testid="connected-bulk-add-as-new"
              onClick={() => void runBulkAddAsNew()}
            >
              <Plus className="size-3.5 shrink-0" aria-hidden />
              {t("connected.bulkAddAsNew").replace("{count}", String(bulkPartition.addAsNew.length))}
            </Button>
          ) : null}
          <Button
            type="button"
            variant="ghost"
            disabled={bulkBusy}
            data-testid="connected-bulk-clear"
            onClick={() => setSelected(new Set())}
          >
            {t("connected.deselectAllPage")}
          </Button>
        </div>
      ) : null}
      {readinessQuery.isSuccess && filteredItems.length > 0 ? (
        <div className="flex flex-wrap items-center justify-between gap-2 pb-2">
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
              data-testid="connected-catalog-prev"
              onClick={() => setPage((current) => Math.max(1, current - 1))}
            >
              {t("suppliers.prevPage")}
            </Button>
            <Button
              type="button"
              variant="ghost"
              disabled={page >= totalPages}
              data-testid="connected-catalog-next"
              onClick={() => setPage((current) => Math.min(totalPages, current + 1))}
            >
              {t("suppliers.nextPage")}
            </Button>
          </div>
        </div>
      ) : null}
    </div>
  );
}
