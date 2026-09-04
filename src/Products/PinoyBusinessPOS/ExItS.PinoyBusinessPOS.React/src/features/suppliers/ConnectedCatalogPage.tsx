import { useEffect, useMemo, useState } from "react";
import { Link, useParams, useSearchParams } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
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
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { UnderlineTabBar } from "@/components/exits/UnderlineTabBar";
import { SearchField } from "@/components/exits/SearchField";
import { StatusChip } from "@/components/exits/StatusChip";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";
import { useWorkspace } from "@/workspace/WorkspaceProvider";
import {
  countByUserState,
  filterReadinessItems,
  mapBackendStatusToUserState,
  type CatalogReadinessFilter,
  type UserCatalogState,
} from "@/features/suppliers/connected-catalog-readiness";

const PAGE_SIZE = 25;

function formatPoPrice(value: number): string {
  return `₱${value.toLocaleString("en-PH", {
    minimumFractionDigits: value % 1 === 0 ? 0 : 2,
    maximumFractionDigits: 2,
  })}`;
}

function EvidenceChip({
  matched,
  label,
}: {
  matched: boolean;
  label: string;
}) {
  return (
    <span
      className={cn(
        "inline-flex items-center rounded-full px-2.5 py-1 text-[length:var(--exits-text-xs)] font-semibold",
        matched
          ? "bg-[color-mix(in_srgb,var(--exits-success)_16%,transparent)] text-foreground"
          : "bg-[var(--exits-surface-muted)] text-muted",
      )}
    >
      <span aria-hidden className="mr-1">
        {matched ? "✓" : "–"}
      </span>
      {label}
    </span>
  );
}

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
  const [selectedConflictByExposure, setSelectedConflictByExposure] = useState<
    Record<string, string>
  >({});

  useEffect(() => {
    const handle = window.setTimeout(() => setDebounced(search.trim()), 250);
    return () => window.clearTimeout(handle);
  }, [search]);

  useEffect(() => {
    setPage(1);
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

  async function doCreateAndLink(exposureId: string, name: string, uom: string) {
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
        // Buyer selling price is independent of supplier purchase price.
        sellingPrice: 0,
        businessUsage: "Resale",
      });
      setMessage(t("connected.createAndLinkSucceeded"));
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
      <SearchField
        label={t("connected.catalogSearch")}
        value={search}
        onChange={(event) => setSearch(event.target.value)}
        onClear={() => setSearch("")}
        placeholder={t("connected.catalogSearch")}
        data-testid="connected-catalog-search"
      />
      {readinessQuery.isSuccess ? (
        <UnderlineTabBar
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
      <ul className="m-0 grid list-none gap-3 p-0" data-testid="connected-catalog-list">
        {pageItems.map((item) => {
          const state = mapBackendStatusToUserState(item.status);
          const selectedConflictId =
            selectedConflictByExposure[item.exposureId] ??
            item.conflictCandidates[0]?.productId ??
            null;
          const busy = busyKey != null;
          return (
            <li key={item.exposureId}>
              <Card
                as="article"
                className="grid gap-3 p-3"
                data-testid={`connected-catalog-item-${item.exposureId}`}
              >
                <div className="flex min-w-0 items-start justify-between gap-3">
                  <div className="min-w-0 flex-1">
                    <p className="m-0 text-[length:var(--exits-text-base)] font-semibold leading-snug">
                      {item.supplierName}
                    </p>
                    <p className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
                      {item.supplierSku ?? t("connected.noSku")}
                      {" · "}
                      {item.unitOfMeasureCode}
                    </p>
                    <p className="m-0 mt-1 text-[length:var(--exits-text-sm)] font-medium">
                      <span className="text-muted">{t("connected.poPrice")}</span>
                      {" "}
                      {formatPoPrice(item.poPrice)}
                    </p>
                  </div>
                  <StatusChip tone={statusTone(state)}>{statusLabel(t, state)}</StatusChip>
                </div>

                {state === "checkMatch" ? (
                  <div
                    className="grid gap-2 rounded-[var(--exits-radius-md)] bg-[var(--exits-surface-muted)] px-3 py-2.5"
                    data-testid={`connected-check-match-${item.exposureId}`}
                  >
                    <p className="m-0 text-[length:var(--exits-text-xs)] font-semibold uppercase tracking-wide text-muted">
                      {t("connected.candidateLabel")}
                    </p>
                    <p className="m-0 font-semibold">
                      {item.candidateBuyerProductName ?? t("connected.candidateUnknown")}
                    </p>
                    <div className="flex flex-wrap gap-1.5">
                      <EvidenceChip matched={item.nameMatched} label={t("connected.evidenceName")} />
                      <EvidenceChip matched={item.skuMatched} label={t("connected.evidenceSku")} />
                      <EvidenceChip
                        matched={item.barcodeMatched}
                        label={t("connected.evidenceBarcode")}
                      />
                      <EvidenceChip
                        matched={item.unitCompatible}
                        label={t("connected.evidenceUom")}
                      />
                    </div>
                    {item.matchDetails ? (
                      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                        {item.matchDetails}
                      </p>
                    ) : null}
                  </div>
                ) : null}

                {state === "attention" ? (
                  <div
                    className="grid gap-2"
                    data-testid={`connected-attention-${item.exposureId}`}
                  >
                    <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                      {item.matchDetails || t("connected.conflictHelp")}
                    </p>
                    {item.conflictCandidates.length > 0 ? (
                      <ul className="m-0 grid list-none gap-1.5 p-0" role="radiogroup">
                        {item.conflictCandidates.map((candidate) => {
                          const selected = selectedConflictId === candidate.productId;
                          return (
                            <li key={candidate.productId}>
                              <label
                                className={cn(
                                  "flex min-h-11 cursor-pointer items-center gap-3 rounded-[var(--exits-radius-md)] border px-3 py-2",
                                  selected
                                    ? "border-[var(--exits-primary)] bg-[color-mix(in_srgb,var(--exits-primary)_10%,transparent)]"
                                    : "border-border bg-surface",
                                )}
                              >
                                <input
                                  type="radio"
                                  className="size-4 shrink-0"
                                  name={`conflict-${item.exposureId}`}
                                  checked={selected}
                                  onChange={() =>
                                    setSelectedConflictByExposure((current) => ({
                                      ...current,
                                      [item.exposureId]: candidate.productId,
                                    }))
                                  }
                                  data-testid={`connected-conflict-pick-${candidate.productId}`}
                                />
                                <span className="min-w-0">
                                  <span className="block font-medium">{candidate.name}</span>
                                  <span className="block text-[length:var(--exits-text-sm)] text-muted">
                                    {candidate.sku ? `${candidate.sku} · ` : ""}
                                    {candidate.unitOfMeasureCode}
                                  </span>
                                </span>
                              </label>
                            </li>
                          );
                        })}
                      </ul>
                    ) : (
                      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                        {t("connected.conflictNoCandidates")}
                      </p>
                    )}
                  </div>
                ) : null}

                {state === "newProduct" ? (
                  <p
                    className="m-0 text-[length:var(--exits-text-sm)] text-muted"
                    data-testid={`connected-new-help-${item.exposureId}`}
                  >
                    {t("connected.newProductHelp")}
                  </p>
                ) : null}

                {state === "newProduct" || state === "checkMatch" || state === "attention" ? (
                  <div className="grid gap-2 border-t border-border pt-3">
                    {state === "newProduct" && allowCreate ? (
                      <Button
                        type="button"
                        className="min-h-11 w-full"
                        data-testid={`connected-create-link-${item.exposureId}`}
                        disabled={busy}
                        onClick={() =>
                          void doCreateAndLink(
                            item.exposureId,
                            item.supplierName,
                            item.unitOfMeasureCode,
                          )
                        }
                      >
                        {t("connected.createAndLink")}
                      </Button>
                    ) : null}
                    {state === "checkMatch" && allowLink && item.candidateBuyerProductId ? (
                      <Button
                        type="button"
                        className="min-h-11 w-full"
                        data-testid={`connected-confirm-match-${item.exposureId}`}
                        disabled={busy}
                        onClick={() =>
                          void doLink(item.exposureId, item.candidateBuyerProductId!)
                        }
                      >
                        {t("connected.confirmMatch")}
                      </Button>
                    ) : null}
                    {state === "checkMatch" && allowCreate ? (
                      <Button
                        type="button"
                        variant="ghost"
                        className="min-h-11 w-full"
                        data-testid={`connected-add-as-new-${item.exposureId}`}
                        disabled={busy}
                        onClick={() =>
                          void doCreateAndLink(
                            item.exposureId,
                            item.supplierName,
                            item.unitOfMeasureCode,
                          )
                        }
                      >
                        {t("connected.addAsNew")}
                      </Button>
                    ) : null}
                    {state === "attention" && allowLink && selectedConflictId ? (
                      <Button
                        type="button"
                        className="min-h-11 w-full"
                        data-testid={`connected-link-selected-${item.exposureId}`}
                        disabled={busy}
                        onClick={() => void doLink(item.exposureId, selectedConflictId)}
                      >
                        {t("connected.linkSelected")}
                      </Button>
                    ) : null}
                    {state === "attention" && allowCreate ? (
                      <Button
                        type="button"
                        variant="ghost"
                        className="min-h-11 w-full"
                        data-testid={`connected-add-as-new-${item.exposureId}`}
                        disabled={busy}
                        onClick={() =>
                          void doCreateAndLink(
                            item.exposureId,
                            item.supplierName,
                            item.unitOfMeasureCode,
                          )
                        }
                      >
                        {t("connected.addAsNew")}
                      </Button>
                    ) : null}
                    {!allowLink && !allowCreate ? (
                      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                        {t("connected.catalogPermissionRequired")}
                      </p>
                    ) : null}
                  </div>
                ) : null}

                {state === "unclassified" ? (
                  <p
                    className="m-0 text-[length:var(--exits-text-sm)] text-muted"
                    data-testid={`connected-unclassified-${item.exposureId}`}
                  >
                    {t("connected.statusPending")}
                  </p>
                ) : null}
              </Card>
            </li>
          );
        })}
      </ul>
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
              className="min-h-11"
              disabled={page <= 1}
              data-testid="connected-catalog-prev"
              onClick={() => setPage((current) => Math.max(1, current - 1))}
            >
              {t("suppliers.prevPage")}
            </Button>
            <Button
              type="button"
              variant="ghost"
              className="min-h-11"
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
