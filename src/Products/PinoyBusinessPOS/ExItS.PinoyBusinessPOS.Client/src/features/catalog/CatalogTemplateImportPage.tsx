import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useMutation, useQueries, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  ArrowLeft,
  ArrowRight,
  Check,
  ChevronRight,
  Loader2,
  RotateCcw,
  Upload,
} from "lucide-react";
import {
  getPublishedTemplate,
  listPublishedTemplates,
} from "@/api/platform/merchant-catalog-client";
import {
  getTemplateImportStatus,
  importTemplateBatch,
  importTemplateNextBatch,
  listImportedGlobalProducts,
} from "@/api/pos/pos-catalog-import-client";
import type { PlatformMerchantCatalogTemplateSummary } from "@/api/platform/merchant-catalog-types";
import type { PosTemplateImportStatus } from "@/api/pos/pos-catalog-import-types";
import { PosApiError } from "@/api/pos/pos-http";
import { PlatformApiError } from "@/api/platform/platform-http";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { LoadingState } from "@/components/exits/LoadingState";
import { OnlineRequiredCard } from "@/components/exits/OnlineRequiredCard";
import { PageHeader } from "@/components/exits/PageHeader";
import { SearchField } from "@/components/exits/SearchField";
import { StatusChip } from "@/components/exits/StatusChip";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { useI18n } from "@/i18n/I18nProvider";
import { ONLINE_REQUIRED_CODES } from "@/offline/online-required";
import type { MessageKey } from "@/i18n/messages";
import { formatPeso } from "@/lib/format-money";
import { cn } from "@/lib/cn";
import { pageBackNav } from "@/navigation/page-back-nav";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type WizardStep = "choose" | "preview" | "confirm";

const STEPS: WizardStep[] = ["choose", "preview", "confirm"];

const STEP_LABEL_KEYS: Record<WizardStep, MessageKey> = {
  choose: "catalogImport.step.choose",
  preview: "catalogImport.step.preview",
  confirm: "catalogImport.step.confirm",
};

function statusLabel(
  status: PosTemplateImportStatus | undefined,
  t: (key: MessageKey) => string,
): string {
  if (!status) return t("catalogImport.statusUnknown");
  if (status.canImportFirstBatch) return t("catalogImport.statusReadyFirst");
  if (status.canImportNextBatch) {
    return t("catalogImport.statusNextBatch").replace(
      "{count}",
      String(status.nextBatchSizeEstimate),
    );
  }
  if (status.firstBatchComplete && !status.hasSubsequentBatches) {
    return t("catalogImport.statusComplete");
  }
  if (status.firstBatchComplete) return t("catalogImport.statusFirstDone");
  return t("catalogImport.statusPartial");
}

function statusTone(status: PosTemplateImportStatus | undefined): "info" | "success" | "warning" {
  if (!status) return "info";
  if (status.canImportFirstBatch || status.canImportNextBatch) return "success";
  if (status.firstBatchComplete && !status.hasSubsequentBatches) return "warning";
  return "info";
}

export function CatalogTemplateImportPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const online = useBrowserOnline();
  const { boundWorkspace } = useWorkspace();
  const [step, setStep] = useState<WizardStep>("choose");
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [previewSearch, setPreviewSearch] = useState("");
  const [confirmed, setConfirmed] = useState(false);
  const [startError, setStartError] = useState<string | null>(null);

  useEffect(() => {
    const handle = window.setTimeout(() => setDebounced(search.trim()), 250);
    return () => window.clearTimeout(handle);
  }, [search]);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const templatesQuery = useQuery({
    queryKey: ["merchant-catalog", "templates", debounced],
    enabled: online && Boolean(workspace),
    queryFn: ({ signal }) =>
      listPublishedTemplates({ search: debounced || undefined, pageSize: 40 }, signal),
  });

  const templateIds = useMemo(
    () => templatesQuery.data?.items.map((item) => item.id) ?? [],
    [templatesQuery.data?.items],
  );

  const statusQueries = useQueries({
    queries: templateIds.map((templateId) => ({
      queryKey: ["catalog-import", "template-status", workspace?.organizationId, templateId],
      enabled: online && Boolean(workspace) && step === "choose",
      queryFn: ({ signal }: { signal: AbortSignal }) =>
        getTemplateImportStatus(workspace!, templateId, signal),
      staleTime: 30_000,
    })),
  });

  const statusById = useMemo(() => {
    const map = new Map<string, PosTemplateImportStatus>();
    templateIds.forEach((id, index) => {
      const data = statusQueries[index]?.data;
      if (data) map.set(id, data);
    });
    return map;
  }, [statusQueries, templateIds]);

  const selectedSummary = templatesQuery.data?.items.find((item) => item.id === selectedId) ?? null;

  const detailQuery = useQuery({
    queryKey: ["merchant-catalog", "template", selectedId],
    enabled: online && Boolean(selectedId) && (step === "preview" || step === "confirm"),
    queryFn: ({ signal }) => getPublishedTemplate(selectedId!, signal),
  });

  const selectedStatusQuery = useQuery({
    queryKey: ["catalog-import", "template-status", workspace?.organizationId, selectedId],
    enabled: online && Boolean(workspace) && Boolean(selectedId),
    queryFn: ({ signal }) => getTemplateImportStatus(workspace!, selectedId!, signal),
  });

  const previewProducts = useMemo(() => {
    const products = detailQuery.data?.products ?? [];
    const status = selectedStatusQuery.data;
    const batchFilter = status?.canImportNextBatch
      ? (p: { isFirstBatch: boolean }) => !p.isFirstBatch
      : (p: { isFirstBatch: boolean }) => p.isFirstBatch;
    const filtered = products.filter(batchFilter);
    const q = previewSearch.trim().toLowerCase();
    if (!q) return filtered;
    return filtered.filter((product) => {
      const haystack = [
        product.productName,
        product.sku,
        product.barcode,
        product.brand,
        product.categoryName,
      ]
        .filter(Boolean)
        .join(" ")
        .toLowerCase();
      return haystack.includes(q);
    });
  }, [detailQuery.data?.products, previewSearch, selectedStatusQuery.data]);

  const previewIds = previewProducts.map((p) => p.globalProductId);

  const importedQuery = useQuery({
    queryKey: ["catalog-import", "imported", workspace?.organizationId, previewIds.join(",")],
    enabled: online && Boolean(workspace) && previewIds.length > 0 && step !== "choose",
    queryFn: ({ signal }) => listImportedGlobalProducts(workspace!, previewIds, signal),
  });

  const importedSet = useMemo(
    () => new Set(importedQuery.data?.importedIds ?? []),
    [importedQuery.data?.importedIds],
  );

  const startMutation = useMutation({
    mutationFn: async () => {
      if (!workspace || !selectedId) throw new Error("Missing workspace");
      const status = selectedStatusQuery.data;
      if (status?.canImportNextBatch) {
        return importTemplateNextBatch(workspace, selectedId, {
          batchNumber: status.suggestedNextBatchNumber,
        });
      }
      return importTemplateBatch(workspace, {
        platformTemplateId: selectedId,
        batchNumber: 1,
      });
    },
    onSuccess: (job) => {
      void queryClient.invalidateQueries({ queryKey: ["catalog-import"] });
      navigate(`/catalog/import-jobs/${job.jobId}`);
    },
    onError: (error) => {
      if (error instanceof PosApiError || error instanceof PlatformApiError) {
        setStartError(error.message);
        return;
      }
      setStartError(error instanceof Error ? error.message : t("error.title"));
    },
  });

  function selectTemplate(item: PlatformMerchantCatalogTemplateSummary) {
    setSelectedId(item.id);
    setConfirmed(false);
    setStartError(null);
    setPreviewSearch("");
    setStep("preview");
  }

  const pageHeader = (
    <PageHeader
      title={t("catalogImport.title")}
      description={t("catalogImport.lede")}
      backTo={pageBackNav.catalog.to}
      backLabel={t(pageBackNav.catalog.labelKey)}
      backTestId="page-header-back-catalog"
    />
  );

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  if (!online) {
    return (
      <div
        className="catalog-import-page exits-page flex min-w-0 flex-col gap-3"
        data-testid="catalog-templates-page"
      >
        {pageHeader}
        <OnlineRequiredCard code={ONLINE_REQUIRED_CODES.CatalogImport} />
      </div>
    );
  }

  const confirmItems = [
    t("catalogImport.confirmStockZero"),
    t("catalogImport.confirmPricesEditable"),
    t("catalogImport.confirmDuplicates"),
    t("catalogImport.confirmOpeningStock"),
  ] as const;

  return (
    <div
      className="catalog-import-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="catalog-templates-page"
    >
      {pageHeader}

      <ExitsChipBar
        variant="steps"
        ariaLabel={t("catalogImport.stepsAria")}
        testId="catalog-import-steps"
        className="exits-animate-toolbar"
        items={STEPS.map((id, index) => {
          const active = step === id;
          const stepIndex = STEPS.indexOf(step);
          const done = index < stepIndex;
          const reachable =
            id === "choose" ||
            (id === "preview" && Boolean(selectedId)) ||
            (id === "confirm" && Boolean(selectedId) && step !== "choose");
          return {
            key: id,
            label: t(STEP_LABEL_KEYS[id]),
            state: active ? "active" : done ? "done" : "idle",
            disabled: !reachable,
            testId: `catalog-template-step-${id}`,
            onSelect: () => setStep(id),
          };
        })}
      />

      {step === "choose" ? (
        <section className="flex flex-col gap-3" data-testid="catalog-template-choose">
          <div className="catalog-import-toolbar exits-animate-toolbar flex min-w-0 flex-wrap items-center gap-2">
            <SearchField
              label={t("catalogImport.searchTemplates")}
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              onClear={() => setSearch("")}
              placeholder={t("catalogImport.searchTemplates")}
              containerClassName="catalog-import-page__search exits-page__search min-w-0 flex-1"
            />
            <Button
              type="button"
              variant="outline"
              className="catalog-import-toolbar__refresh min-h-11 shrink-0"
              onClick={() => void templatesQuery.refetch()}
              disabled={templatesQuery.isFetching}
            >
              <RotateCcw
                className={cn("size-4 shrink-0", templatesQuery.isFetching && "animate-spin")}
                aria-hidden
              />
              {t("catalogImport.refresh")}
            </Button>
          </div>

          {templatesQuery.isLoading ? <LoadingState label={t("loading.label")} /> : null}
          {templatesQuery.isError ? (
            <ErrorState title={t("error.title")} detail={(templatesQuery.error as Error).message} />
          ) : null}
          {templatesQuery.isSuccess && templatesQuery.data.items.length === 0 ? (
            <EmptyState
              title={t("catalogImport.emptyTemplates")}
              detail={t("catalogImport.emptyTemplatesDetail")}
            />
          ) : null}

          <ul className="exits-list catalog-import-templates m-0 grid list-none gap-2 p-0">
            {templatesQuery.data?.items.map((item) => {
              const status = statusById.get(item.id);
              return (
                <li key={item.id}>
                  <article className="catalog-import-template-card exits-list__card">
                    <div className="catalog-import-template-card__body min-w-0 flex-1">
                      <p className="catalog-import-template-card__title m-0 truncate font-semibold">
                        {item.name}
                      </p>
                      <p className="catalog-import-template-card__meta mb-0 mt-1 truncate text-[length:var(--exits-text-sm)] text-muted">
                        {item.primaryBusinessType} · {t("catalogImport.firstBatchCount")}:{" "}
                        {item.firstBatchCount}
                      </p>
                      {item.description ? (
                        <p className="catalog-import-template-card__desc mb-0 mt-1 line-clamp-2 text-[length:var(--exits-text-sm)] text-muted">
                          {item.description}
                        </p>
                      ) : null}
                      <div className="mt-2">
                        <StatusChip tone={statusTone(status)}>{statusLabel(status, t)}</StatusChip>
                      </div>
                    </div>
                    <Button
                      type="button"
                      variant="outline"
                      className="catalog-import-template-card__select min-h-11 shrink-0"
                      data-testid={`catalog-template-select-${item.id}`}
                      onClick={() => selectTemplate(item)}
                    >
                      {t("catalogImport.select")}
                      <ChevronRight className="size-4 shrink-0" aria-hidden />
                    </Button>
                  </article>
                </li>
              );
            })}
          </ul>
        </section>
      ) : null}

      {step === "preview" && selectedSummary ? (
        <section className="flex flex-col gap-3" data-testid="catalog-template-preview">
          <article className="catalog-import-summary exits-animate-panel">
            <div className="catalog-import-summary__header">
              <div className="min-w-0 flex-1">
                <h2 className="catalog-import-summary__title m-0 truncate">
                  {selectedSummary.name}
                </h2>
                <p className="catalog-import-summary__meta m-0 mt-1 truncate text-[length:var(--exits-text-sm)] text-muted">
                  {selectedSummary.primaryBusinessType}
                </p>
              </div>
              {selectedStatusQuery.data ? (
                <StatusChip tone={statusTone(selectedStatusQuery.data)}>
                  {statusLabel(selectedStatusQuery.data, t)}
                </StatusChip>
              ) : null}
            </div>
            <p className="catalog-import-summary__lede m-0 text-[length:var(--exits-text-sm)] leading-relaxed text-muted">
              {t("catalogImport.localOwnership")}
            </p>
            <p className="catalog-import-summary__count m-0 text-[length:var(--exits-text-sm)] font-semibold">
              {t("catalogImport.previewBatchCount").replace(
                "{count}",
                String(previewProducts.length),
              )}
            </p>
          </article>

          <SearchField
            label={t("catalogImport.searchPreview")}
            value={previewSearch}
            onChange={(event) => setPreviewSearch(event.target.value)}
            onClear={() => setPreviewSearch("")}
            placeholder={t("catalogImport.searchPreview")}
            containerClassName="catalog-import-page__search exits-page__search"
          />

          {detailQuery.isLoading || importedQuery.isLoading ? (
            <LoadingState label={t("loading.label")} />
          ) : null}
          {detailQuery.isError ? (
            <ErrorState title={t("error.title")} detail={(detailQuery.error as Error).message} />
          ) : null}

          {detailQuery.isSuccess && previewProducts.length === 0 ? (
            <EmptyState
              title={t("catalogImport.emptyPreview")}
              detail={t("catalogImport.emptyPreviewDetail")}
            />
          ) : null}

          <ul className="exits-list m-0 grid list-none gap-2 p-0">
            {previewProducts.map((product) => {
              const already = importedSet.has(product.globalProductId);
              const meta = [product.categoryName, product.unit, product.brand]
                .filter(Boolean)
                .join(" · ");
              return (
                <li key={product.id}>
                  <article
                    className={cn(
                      "catalog-import-product-row exits-list__card",
                      already && "catalog-import-product-row--added",
                    )}
                    data-testid={`catalog-template-preview-row-${product.globalProductId}`}
                  >
                    <div className="catalog-import-product-row__main min-w-0">
                      <p className="exits-list__name m-0 truncate font-semibold">
                        {product.productName ?? t("catalogImport.unnamedProduct")}
                      </p>
                      {meta ? (
                        <p className="catalog-import-product-row__meta m-0 mt-1 truncate text-[length:var(--exits-text-sm)] text-muted">
                          {meta}
                        </p>
                      ) : null}
                    </div>
                    <div className="catalog-import-product-row__aside">
                      {product.sellingPrice != null ? (
                        <span className="catalog-import-product-row__price">
                          {formatPeso(product.sellingPrice)}
                        </span>
                      ) : null}
                      {already ? (
                        <StatusChip tone="warning">{t("catalogImport.alreadyAdded")}</StatusChip>
                      ) : null}
                    </div>
                  </article>
                </li>
              );
            })}
          </ul>

          <div className="catalog-form-actions">
            <div className="catalog-form-actions__primary">
              <Button
                type="button"
                variant="ghost"
                className="catalog-form-actions__restore min-h-11 w-full sm:w-auto"
                onClick={() => setStep("choose")}
              >
                <ArrowLeft className="size-4 shrink-0" aria-hidden />
                {t("catalogImport.back")}
              </Button>
            </div>
            <div className="catalog-form-actions__secondary">
              <Button
                type="button"
                className="catalog-form-actions__save min-h-11"
                data-testid="catalog-template-continue-confirm"
                onClick={() => setStep("confirm")}
              >
                {t("catalogImport.continueConfirm")}
                <ArrowRight className="size-4 shrink-0" aria-hidden />
              </Button>
            </div>
          </div>
        </section>
      ) : null}

      {step === "confirm" && selectedSummary ? (
        <section className="flex flex-col gap-3" data-testid="catalog-template-confirm">
          <article className="catalog-import-confirm-panel exits-animate-panel">
            <h2 className="catalog-import-confirm-panel__title m-0">
              {t("catalogImport.confirmTitle")}
            </h2>
            <p className="catalog-import-confirm-panel__template m-0 mt-1 font-semibold">
              {selectedSummary.name}
            </p>
            <ul className="catalog-import-confirm-panel__list m-0 list-none p-0">
              {confirmItems.map((item) => (
                <li key={item} className="catalog-import-confirm-panel__item">
                  <Check
                    className="catalog-import-confirm-panel__icon size-4 shrink-0"
                    aria-hidden
                  />
                  <span>{item}</span>
                </li>
              ))}
            </ul>
            <label className="catalog-form-check catalog-import-confirm-panel__ack mt-3">
              <input
                type="checkbox"
                checked={confirmed}
                data-testid="catalog-template-confirm-checkbox"
                onChange={(event) => setConfirmed(event.target.checked)}
              />
              <span>{t("catalogImport.confirmCheckbox")}</span>
            </label>
          </article>

          {startError ? <ErrorState title={t("error.title")} detail={startError} /> : null}

          <div className="catalog-form-actions">
            <div className="catalog-form-actions__primary">
              <Button
                type="button"
                variant="ghost"
                className="catalog-form-actions__restore min-h-11 w-full sm:w-auto"
                onClick={() => setStep("preview")}
              >
                <ArrowLeft className="size-4 shrink-0" aria-hidden />
                {t("catalogImport.back")}
              </Button>
            </div>
            <div className="catalog-form-actions__secondary">
              <Button
                type="button"
                className="catalog-form-actions__save min-h-11"
                data-testid="catalog-template-start-import"
                disabled={!confirmed || startMutation.isPending}
                onClick={() => {
                  setStartError(null);
                  startMutation.mutate();
                }}
              >
                {startMutation.isPending ? (
                  <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
                ) : (
                  <Upload className="size-4 shrink-0" aria-hidden />
                )}
                {startMutation.isPending
                  ? t("catalogImport.starting")
                  : t("catalogImport.startImport")}
              </Button>
            </div>
          </div>
        </section>
      ) : null}
    </div>
  );
}
