import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useMutation, useQueries, useQuery, useQueryClient } from "@tanstack/react-query";
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
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { OnlineRequiredCard } from "@/components/exits/OnlineRequiredCard";
import { PageHeader } from "@/components/exits/PageHeader";
import { SearchField } from "@/components/exits/SearchField";
import { StatusChip } from "@/components/exits/StatusChip";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { useI18n } from "@/i18n/I18nProvider";
import { ONLINE_REQUIRED_CODES } from "@/offline/online-required";
import type { MessageKey } from "@/i18n/messages";
import { useWorkspace } from "@/workspace/WorkspaceProvider";
import { cn } from "@/lib/cn";

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
    setStep("preview");
  }

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  if (!online) {
    return (
      <div className="flex flex-col gap-4" data-testid="catalog-templates-page">
        <PageHeader title={t("catalogImport.title")} description={t("catalogImport.lede")} />
        <OnlineRequiredCard code={ONLINE_REQUIRED_CODES.CatalogImport} />
        <Button asChild variant="ghost" className="min-h-11 self-start">
          <Link to="/catalog">{t("catalogImport.backToProducts")}</Link>
        </Button>
      </div>
    );
  }

  return (
    <div className="catalog-import-page flex min-w-0 flex-col gap-3" data-testid="catalog-templates-page">
      <PageHeader title={t("catalogImport.title")} description={t("catalogImport.lede")} />

      <nav
        aria-label={t("catalogImport.stepsAria")}
        className="catalog-import-steps flex min-w-0 items-center gap-1.5 overflow-x-auto overscroll-x-contain pb-0.5"
      >
        {STEPS.map((id, index) => {
          const active = step === id;
          const stepIndex = STEPS.indexOf(step);
          const done = index < stepIndex;
          const reachable =
            id === "choose" ||
            (id === "preview" && selectedId) ||
            (id === "confirm" && selectedId && step !== "choose");
          return (
            <button
              key={id}
              type="button"
              disabled={!reachable}
              data-testid={`catalog-template-step-${id}`}
              aria-current={active ? "step" : undefined}
              className={cn(
                "catalog-import-steps__chip inline-flex shrink-0 items-center gap-1.5 rounded-full border px-2.5 text-[length:var(--exits-text-xs)] font-medium whitespace-nowrap transition-[background-color,border-color,color] duration-[var(--exits-motion-fast)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-45",
                active
                  ? "catalog-import-steps__chip--active border-primary bg-[color-mix(in_srgb,var(--exits-primary)_14%,var(--exits-surface))] text-primary"
                  : done
                    ? "border-primary/40 bg-surface text-foreground"
                    : "border-border bg-surface text-muted",
              )}
              onClick={() => reachable && setStep(id)}
            >
              <span
                className={cn(
                  "catalog-import-steps__index inline-flex size-4 items-center justify-center rounded-full text-[0.65rem] font-semibold",
                  active
                    ? "bg-primary text-primary-foreground"
                    : done
                      ? "bg-[color-mix(in_srgb,var(--exits-primary)_30%,var(--exits-surface))] text-primary"
                      : "bg-[var(--exits-surface-muted)] text-muted",
                )}
                aria-hidden
              >
                {index + 1}
              </span>
              {t(STEP_LABEL_KEYS[id])}
            </button>
          );
        })}
      </nav>

      {step === "choose" ? (
        <section className="flex flex-col gap-3" data-testid="catalog-template-choose">
          <div className="catalog-import-toolbar flex min-w-0 flex-wrap items-center gap-2">
            <SearchField
              label={t("catalogImport.searchTemplates")}
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              onClear={() => setSearch("")}
              placeholder={t("catalogImport.searchTemplates")}
              containerClassName="catalog-import-page__search min-w-0 flex-1"
            />
            <Button
              type="button"
              variant="ghost"
              className="catalog-import-toolbar__refresh min-h-9 shrink-0"
              onClick={() => void templatesQuery.refetch()}
              disabled={templatesQuery.isFetching}
            >
              {t("catalogImport.refresh")}
            </Button>
          </div>
          {templatesQuery.isLoading ? <LoadingState label={t("loading.label")} /> : null}
          {templatesQuery.isError ? (
            <ErrorState
              title={t("error.title")}
              detail={(templatesQuery.error as Error).message}
            />
          ) : null}
          {templatesQuery.isSuccess && templatesQuery.data.items.length === 0 ? (
            <EmptyState
              title={t("catalogImport.emptyTemplates")}
              detail={t("catalogImport.emptyTemplatesDetail")}
            />
          ) : null}
          <ul className="catalog-import-templates m-0 flex list-none flex-col gap-2 p-0">
            {templatesQuery.data?.items.map((item) => {
              const status = statusById.get(item.id);
              return (
                <li key={item.id}>
                  <article className="catalog-import-template-card">
                    <div className="catalog-import-template-card__body min-w-0">
                      <p className="catalog-import-template-card__title m-0 truncate font-semibold">
                        {item.name}
                      </p>
                      <p className="catalog-import-template-card__meta mb-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
                        {item.primaryBusinessType} · {t("catalogImport.firstBatchCount")}:{" "}
                        {item.firstBatchCount}
                      </p>
                      {item.description ? (
                        <p className="catalog-import-template-card__desc mb-0 mt-1 line-clamp-2 text-[length:var(--exits-text-sm)] text-muted">
                          {item.description}
                        </p>
                      ) : null}
                      <div className="mt-2">
                        <StatusChip>{statusLabel(status, t)}</StatusChip>
                      </div>
                    </div>
                    <Button
                      type="button"
                      className="catalog-import-template-card__select min-h-9 shrink-0"
                      data-testid={`catalog-template-select-${item.id}`}
                      onClick={() => selectTemplate(item)}
                    >
                      {t("catalogImport.select")}
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
          <Card className="p-3">
            <p className="m-0 font-semibold">{selectedSummary.name}</p>
            <p className="mb-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
              {selectedSummary.primaryBusinessType}
            </p>
            <p className="mb-0 mt-2 text-[length:var(--exits-text-sm)]">
              {t("catalogImport.localOwnership")}
            </p>
            {selectedStatusQuery.data ? (
              <p className="mb-0 mt-2 text-[length:var(--exits-text-sm)] text-muted">
                {statusLabel(selectedStatusQuery.data, t)}
              </p>
            ) : null}
          </Card>
          <SearchField
            label={t("catalogImport.searchPreview")}
            value={previewSearch}
            onChange={(event) => setPreviewSearch(event.target.value)}
            onClear={() => setPreviewSearch("")}
            placeholder={t("catalogImport.searchPreview")}
            containerClassName="catalog-import-page__search"
          />
          {detailQuery.isLoading || importedQuery.isLoading ? (
            <LoadingState label={t("loading.label")} />
          ) : null}
          {detailQuery.isError ? (
            <ErrorState title={t("error.title")} detail={(detailQuery.error as Error).message} />
          ) : null}
          <ul className="m-0 flex list-none flex-col gap-2 p-0">
            {previewProducts.map((product) => {
              const already = importedSet.has(product.globalProductId);
              return (
                <li key={product.id}>
                  <Card
                    className={cn("p-3", already && "opacity-70")}
                    data-testid={`catalog-template-preview-row-${product.globalProductId}`}
                  >
                    <div className="flex items-start justify-between gap-2">
                      <div className="min-w-0">
                        <p className="m-0 truncate font-semibold">
                          {product.productName ?? t("catalogImport.unnamedProduct")}
                        </p>
                        <p className="mb-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
                          {[product.categoryName, product.unit, product.brand]
                            .filter(Boolean)
                            .join(" · ")}
                        </p>
                        {product.sellingPrice != null ? (
                          <p className="mb-0 mt-1 text-[length:var(--exits-text-sm)]">
                            {t("catalogImport.suggestedPrice")}: {product.sellingPrice}
                          </p>
                        ) : null}
                      </div>
                      {already ? (
                        <StatusChip>{t("catalogImport.alreadyAdded")}</StatusChip>
                      ) : null}
                    </div>
                  </Card>
                </li>
              );
            })}
          </ul>
          <div className="sticky bottom-0 z-10 flex flex-wrap gap-2 border-t border-border bg-[var(--exits-bg)] py-3">
            <Button
              type="button"
              variant="ghost"
              className="min-h-11"
              onClick={() => setStep("choose")}
            >
              {t("catalogImport.back")}
            </Button>
            <Button
              type="button"
              className="min-h-11"
              data-testid="catalog-template-continue-confirm"
              onClick={() => setStep("confirm")}
            >
              {t("catalogImport.continueConfirm")}
            </Button>
          </div>
        </section>
      ) : null}

      {step === "confirm" && selectedSummary ? (
        <section className="flex flex-col gap-3" data-testid="catalog-template-confirm">
          <Card className="flex flex-col gap-2 p-3">
            <p className="m-0 font-semibold">{selectedSummary.name}</p>
            <ul className="m-0 list-disc space-y-1 pl-5 text-[length:var(--exits-text-sm)] text-muted">
              <li>{t("catalogImport.confirmStockZero")}</li>
              <li>{t("catalogImport.confirmPricesEditable")}</li>
              <li>{t("catalogImport.confirmDuplicates")}</li>
              <li>{t("catalogImport.confirmOpeningStock")}</li>
            </ul>
            <label className="mt-2 flex min-h-11 items-start gap-3 text-[length:var(--exits-text-sm)]">
              <input
                type="checkbox"
                className="mt-1 size-5"
                checked={confirmed}
                data-testid="catalog-template-confirm-checkbox"
                onChange={(event) => setConfirmed(event.target.checked)}
              />
              <span>{t("catalogImport.confirmCheckbox")}</span>
            </label>
          </Card>
          {startError ? <ErrorState title={t("error.title")} detail={startError} /> : null}
          <div className="sticky bottom-0 z-10 flex flex-wrap gap-2 border-t border-border bg-[var(--exits-bg)] py-3">
            <Button
              type="button"
              variant="ghost"
              className="min-h-11"
              onClick={() => setStep("preview")}
            >
              {t("catalogImport.back")}
            </Button>
            <Button
              type="button"
              className="min-h-11"
              data-testid="catalog-template-start-import"
              disabled={!confirmed || startMutation.isPending}
              onClick={() => {
                setStartError(null);
                startMutation.mutate();
              }}
            >
              {startMutation.isPending
                ? t("catalogImport.starting")
                : t("catalogImport.startImport")}
            </Button>
          </div>
        </section>
      ) : null}
    </div>
  );
}
