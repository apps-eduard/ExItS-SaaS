import { useEffect, useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
  canManageCatalog,
  canManagePurchasing,
  canViewPurchasing,
} from "@/access/pos-capabilities";
import { listCatalogProducts } from "@/api/pos/pos-catalog-client";
import {
  classifyCatalogReadiness,
  createBuyerProductAndLink,
  linkProduct,
  searchExposedCatalog,
  suggestBuyerProductMatches,
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
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const PAGE_SIZE = 25;

export function ConnectedCatalogPage() {
  const { t } = useI18n();
  const { supplierId } = useParams<{ supplierId: string }>();
  const queryClient = useQueryClient();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");
  const [page, setPage] = useState(1);
  const [readinessFilter, setReadinessFilter] = useState<
    "all" | "Ready" | "New" | "Review" | "Conflict"
  >("all");
  const [message, setMessage] = useState<string | null>(null);
  const [busyKey, setBusyKey] = useState<string | null>(null);
  const [linkPickerExposureId, setLinkPickerExposureId] = useState<string | null>(null);
  const [pickerSearch, setPickerSearch] = useState("");

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

  const catalogQuery = useQuery({
    queryKey: [
      "connected-suppliers",
      "catalog",
      relationshipId,
      debounced,
      page,
      workspace?.organizationId,
    ],
    enabled: Boolean(workspace) && Boolean(relationshipId) && allowView,
    queryFn: ({ signal }) =>
      searchExposedCatalog(
        workspace!,
        relationshipId!,
        { query: debounced || undefined, page, pageSize: PAGE_SIZE },
        signal,
      ),
  });

  const readinessQuery = useQuery({
    queryKey: ["connected-suppliers", "readiness", relationshipId],
    enabled: Boolean(workspace) && Boolean(relationshipId) && allowView,
    queryFn: ({ signal }) => classifyCatalogReadiness(workspace!, relationshipId!, signal),
  });

  const pickerQuery = useQuery({
    queryKey: ["catalog", "picker", workspace?.organizationId, pickerSearch],
    enabled: Boolean(workspace) && Boolean(linkPickerExposureId),
    queryFn: ({ signal }) =>
      listCatalogProducts(
        workspace!,
        { search: pickerSearch || undefined, status: "Active", page: 1, pageSize: 20 },
        signal,
      ),
  });

  const readinessByExposure = useMemo(() => {
    const map = new Map<string, NonNullable<typeof readinessQuery.data>["items"][number]>();
    for (const item of readinessQuery.data?.items ?? []) {
      map.set(item.exposureId, item);
    }
    return map;
  }, [readinessQuery.data]);

  const filteredItems = useMemo(() => {
    const items = catalogQuery.data?.items ?? [];
    if (readinessFilter === "all") {
      return items;
    }
    return items.filter((item) => {
      const status = readinessByExposure.get(item.exposureId)?.status ?? "New";
      return status.toLowerCase() === readinessFilter.toLowerCase();
    });
  }, [catalogQuery.data, readinessByExposure, readinessFilter]);

  async function doLink(exposureId: string, buyerProductId: string) {
    if (!workspace || !relationshipId || !allowLink) {
      return;
    }
    setBusyKey(exposureId);
    setMessage(null);
    try {
      await linkProduct(workspace, relationshipId, { exposureId, buyerProductId });
      setMessage(t("connected.linkSucceeded"));
      setLinkPickerExposureId(null);
      await queryClient.invalidateQueries({ queryKey: ["connected-suppliers"] });
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

  async function doCreateAndLink(exposureId: string, name: string, uom: string, price: number) {
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
        sellingPrice: price > 0 ? price : 1,
      });
      setMessage(t("connected.createAndLinkSucceeded"));
      await queryClient.invalidateQueries({ queryKey: ["connected-suppliers"] });
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

  async function openSuggestionsThenLink(exposureId: string) {
    if (!workspace || !relationshipId || !allowLink) {
      return;
    }
    setBusyKey(`suggest-${exposureId}`);
    try {
      const suggestions = await suggestBuyerProductMatches(workspace, relationshipId, exposureId);
      const first = suggestions.candidates[0];
      if (first) {
        await doLink(exposureId, first.productId);
      } else {
        setLinkPickerExposureId(exposureId);
      }
    } catch {
      setLinkPickerExposureId(exposureId);
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

  const totalPages = Math.max(1, Math.ceil((catalogQuery.data?.totalCount ?? 0) / PAGE_SIZE));
  const denied =
    catalogQuery.isError &&
    catalogQuery.error instanceof PosApiError &&
    (catalogQuery.error.status === 403 || catalogQuery.error.status === 404);

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="connected-catalog-page">
      <PageHeader title={t("connected.catalogTitle")} description={supplierQuery.data.name} />
      <div className="flex flex-wrap gap-2">
        <Button asChild variant="ghost" className="min-h-11" data-testid="connected-open-linked">
          <Link to={`/suppliers/${supplierId}/linked-products`}>
            {t("connected.openLinkedProducts")}
          </Link>
        </Button>
      </div>
      {message ? (
        <Card data-testid="connected-catalog-message">
          <p className="m-0 text-[length:var(--exits-text-sm)]">{message}</p>
        </Card>
      ) : null}
      <SearchField
        label={t("connected.catalogSearch")}
        value={search}
        onChange={(event) => setSearch(event.target.value)}
        onClear={() => setSearch("")}
        placeholder={t("connected.catalogSearch")}
        data-testid="connected-catalog-search"
      />
      {readinessQuery.data ? (
        <UnderlineTabBar
          items={(
            [
              [
                "all",
                readinessQuery.data.ready +
                  readinessQuery.data.new +
                  readinessQuery.data.review +
                  readinessQuery.data.conflict,
                "connected.filterAllCount",
              ],
              ["Ready", readinessQuery.data.ready, "connected.filterReady"],
              ["New", readinessQuery.data.new, "connected.filterNew"],
              ["Review", readinessQuery.data.review, "connected.filterReview"],
              ["Conflict", readinessQuery.data.conflict, "connected.filterAttention"],
            ] as const
          ).map(([value, count, key]) => ({
            key: value,
            label: t(key).replace("{count}", String(count)),
            testId: `connected-ready-${value}`,
          }))}
          activeKey={readinessFilter}
          onChange={(key) =>
            setReadinessFilter(key as "all" | "Ready" | "New" | "Review" | "Conflict")
          }
          ariaLabel={t("connected.readinessFilters")}
          testId="connected-readiness-chips"
        />
      ) : null}
      {catalogQuery.isLoading ? <LoadingState label={t("loading.label")} /> : null}
      {denied ? (
        <ErrorState title={t("error.title")} detail={t("connected.catalogDenied")} />
      ) : null}
      {catalogQuery.isError && !denied ? (
        <ErrorState
          title={t("error.title")}
          detail={
            catalogQuery.error instanceof PosApiError
              ? (catalogQuery.error.problem.detail ?? catalogQuery.error.message)
              : t("connected.loadFailed")
          }
        />
      ) : null}
      {catalogQuery.isSuccess && catalogQuery.data.items.length === 0 && !debounced ? (
        <EmptyState title={t("connected.catalogEmpty")} detail={t("connected.catalogEmptyHelp")} />
      ) : null}
      {catalogQuery.isSuccess && filteredItems.length === 0 && Boolean(debounced) ? (
        <EmptyState
          title={t("connected.catalogNoMatch")}
          detail={t("connected.catalogNoMatchHelp")}
        />
      ) : null}
      <ul className="m-0 grid list-none gap-2 p-0" data-testid="connected-catalog-list">
        {filteredItems.map((item) => {
          const ready = readinessByExposure.get(item.exposureId);
          const status = ready?.status ?? "New";
          return (
            <li key={item.exposureId}>
              <Card className="p-3" data-testid={`connected-catalog-item-${item.exposureId}`}>
                <div className="flex flex-wrap items-start justify-between gap-2">
                  <div className="min-w-0">
                    <p className="m-0 font-semibold">{item.nameSnapshot}</p>
                    <p className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
                      {item.skuSnapshot ?? t("connected.noSku")} · {t("connected.poPrice")}:{" "}
                      {item.effectiveSupplierOrderPrice ?? item.supplierOrderPrice}
                    </p>
                    <div className="mt-2">
                      <StatusChip tone={status === "Ready" ? "success" : "info"}>
                        {status}
                      </StatusChip>
                    </div>
                  </div>
                  <div className="flex flex-wrap gap-2">
                    {allowLink && status !== "Ready" ? (
                      <Button
                        type="button"
                        className="min-h-11"
                        data-testid={`connected-link-${item.exposureId}`}
                        disabled={busyKey != null}
                        onClick={() => void openSuggestionsThenLink(item.exposureId)}
                      >
                        {t("connected.linkExisting")}
                      </Button>
                    ) : null}
                    {allowCreate && status !== "Ready" ? (
                      <Button
                        type="button"
                        variant="ghost"
                        className="min-h-11"
                        data-testid={`connected-create-link-${item.exposureId}`}
                        disabled={busyKey != null}
                        onClick={() =>
                          void doCreateAndLink(
                            item.exposureId,
                            item.nameSnapshot,
                            item.unitOfMeasureCode,
                            item.effectiveSupplierOrderPrice ?? item.supplierOrderPrice,
                          )
                        }
                      >
                        {t("connected.createAndLink")}
                      </Button>
                    ) : null}
                    {!allowLink && !allowCreate ? (
                      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                        {t("connected.catalogPermissionRequired")}
                      </p>
                    ) : null}
                  </div>
                </div>
              </Card>
            </li>
          );
        })}
      </ul>
      {linkPickerExposureId ? (
        <Card data-testid="connected-link-picker">
          <h2 className="m-0 text-[length:var(--exits-text-base)] font-semibold">
            {t("connected.chooseProduct")}
          </h2>
          <SearchField
            label={t("connected.searchProducts")}
            value={pickerSearch}
            onChange={(event) => setPickerSearch(event.target.value)}
            onClear={() => setPickerSearch("")}
            placeholder={t("connected.searchProducts")}
            data-testid="connected-picker-search"
          />
          <ul className="m-0 mt-2 grid list-none gap-2 p-0">
            {pickerQuery.data?.items.map((product) => (
              <li key={product.productId}>
                <Button
                  type="button"
                  variant="ghost"
                  className="min-h-11 w-full justify-start"
                  data-testid={`connected-pick-${product.productId}`}
                  onClick={() => void doLink(linkPickerExposureId, product.productId)}
                >
                  {product.name}
                </Button>
              </li>
            ))}
          </ul>
          <Button
            type="button"
            variant="ghost"
            className="mt-2 min-h-11"
            onClick={() => setLinkPickerExposureId(null)}
          >
            {t("connected.cancel")}
          </Button>
        </Card>
      ) : null}
      {catalogQuery.isSuccess && (catalogQuery.data?.totalCount ?? 0) > 0 ? (
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
              onClick={() => setPage((current) => current + 1)}
            >
              {t("suppliers.nextPage")}
            </Button>
          </div>
        </div>
      ) : null}
      <Button asChild variant="ghost" className="min-h-11 self-start">
        <Link to={`/suppliers/${supplierId}`}>{t("connected.backToSupplier")}</Link>
      </Button>
    </div>
  );
}
