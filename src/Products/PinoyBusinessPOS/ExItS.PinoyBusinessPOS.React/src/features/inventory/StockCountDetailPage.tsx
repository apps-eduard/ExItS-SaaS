import { useEffect, useMemo, useRef, useState } from "react";
import { useLocation, useNavigate, useParams } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { canManageInventory } from "@/access/pos-capabilities";
import { PosApiError } from "@/api/pos/pos-http";
import {
  cancelStockCount,
  completeStockCount,
  getStockCount,
  startStockCount,
  updateStockCount,
  type StockCountDto,
  type StockCountLineDto,
} from "@/api/pos/pos-stock-count-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { StickyActionBar } from "@/components/exits/FoundationStates";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { SearchField } from "@/components/exits/SearchField";
import { StatusChip } from "@/components/exits/StatusChip";
import { useBrowserOnline } from "@/connectivity/browser-online";
import {
  formatStockCountDate,
  formatStockCountTimestamp,
  formatVariance,
  lineMatchesFilter,
  parseCountedQuantity,
  previewVariance,
  stockCountStatusLabelKey,
  stockCountStatusTone,
  summarizeCountLines,
  type StockCountLineFilter,
} from "@/features/inventory/stock-count-labels";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type ViewMode = "detail" | "review";

function countedMapFromServer(count: StockCountDto): Record<string, string> {
  const map: Record<string, string> = {};
  for (const line of count.lines) {
    if (line.countedQuantity != null) {
      map[line.productId] = String(line.countedQuantity);
    }
  }
  return map;
}

export function StockCountDetailPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const location = useLocation();
  const { stockCountId = "" } = useParams();
  const online = useBrowserOnline();
  const queryClient = useQueryClient();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const allowManage = canManageInventory(sessionGrant);

  const [localError, setLocalError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [mode, setMode] = useState<ViewMode>("detail");
  const [lineSearch, setLineSearch] = useState("");
  const [lineFilter, setLineFilter] = useState<StockCountLineFilter>("all");
  const [countedByProduct, setCountedByProduct] = useState<Record<string, string>>({});
  const [draftEditing, setDraftEditing] = useState(false);
  const [draftTitle, setDraftTitle] = useState("");
  const [draftDate, setDraftDate] = useState("");
  const [draftNotes, setDraftNotes] = useState("");
  const inputRefs = useRef<Record<string, HTMLInputElement | null>>({});
  const hydratedIdRef = useRef<string | null>(null);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const query = useQuery({
    queryKey: ["stock-count", workspace?.organizationId, stockCountId],
    enabled: Boolean(workspace) && Boolean(stockCountId) && online,
    queryFn: ({ signal }) => getStockCount(workspace!, stockCountId, signal),
  });

  const count = query.data;

  useEffect(() => {
    const flash = (location.state as { flash?: string } | null)?.flash;
    if (flash === "created") {
      setSuccess(t("stockCount.createdSuccess"));
      navigate(location.pathname, { replace: true, state: {} });
    }
  }, [location.pathname, location.state, navigate, t]);

  useEffect(() => {
    if (!count) {
      return;
    }
    if (hydratedIdRef.current === count.stockCountId && count.status === "InProgress") {
      return;
    }
    hydratedIdRef.current = count.stockCountId;
    setCountedByProduct(countedMapFromServer(count));
    setDraftTitle(count.title);
    setDraftDate(count.countDate.slice(0, 10));
    setDraftNotes(count.notes ?? "");
    setDraftEditing(false);
    setMode("detail");
  }, [count]);

  const summary = useMemo(
    () => (count ? summarizeCountLines(count.lines, countedByProduct) : null),
    [count, countedByProduct],
  );

  const visibleLines = useMemo(() => {
    if (!count) {
      return [] as StockCountLineDto[];
    }
    const q = lineSearch.trim().toLowerCase();
    return count.lines.filter((line) => {
      if (!lineMatchesFilter(line, countedByProduct[line.productId], lineFilter)) {
        return false;
      }
      if (!q) {
        return true;
      }
      return line.productName.toLowerCase().includes(q);
    });
  }, [count, countedByProduct, lineFilter, lineSearch]);

  async function refreshAfter(mutation: () => Promise<StockCountDto>, successMessage: string) {
    if (!workspace || busy) {
      return;
    }
    setBusy(true);
    setLocalError(null);
    try {
      const updated = await mutation();
      hydratedIdRef.current = null;
      queryClient.setQueryData(["stock-count", workspace.organizationId, stockCountId], updated);
      await queryClient.invalidateQueries({ queryKey: ["stock-counts"] });
      await queryClient.invalidateQueries({ queryKey: ["inventory"] });
      setSuccess(successMessage);
      setMode("detail");
    } catch (err) {
      setLocalError(
        err instanceof PosApiError
          ? (err.problem.detail ?? t("stockCount.actionFailed"))
          : t("stockCount.actionFailed"),
      );
    } finally {
      setBusy(false);
    }
  }

  async function onStart() {
    if (!workspace || !count) {
      return;
    }
    if (!window.confirm(t("stockCount.startConfirm"))) {
      return;
    }
    await refreshAfter(() => startStockCount(workspace, count.stockCountId), t("stockCount.startedSuccess"));
  }

  async function onCancel() {
    if (!workspace || !count) {
      return;
    }
    if (!window.confirm(t("stockCount.cancelConfirm"))) {
      return;
    }
    await refreshAfter(() => cancelStockCount(workspace, count.stockCountId), t("stockCount.cancelledSuccess"));
  }

  async function onSaveDraftMeta() {
    if (!workspace || !count || count.status !== "Draft") {
      return;
    }
    const trimmed = draftTitle.trim();
    if (!trimmed) {
      setLocalError(t("stockCount.titleRequired"));
      return;
    }
    await refreshAfter(
      () =>
        updateStockCount(workspace, count.stockCountId, {
          title: trimmed,
          countDate: draftDate || null,
          notes: draftNotes,
          lines: count.lines.map((line) => ({ productId: line.productId })),
        }),
      t("stockCount.draftSaved"),
    );
    setDraftEditing(false);
  }

  async function onSaveProgress() {
    if (!workspace || !count || count.status !== "InProgress") {
      return;
    }
    const lines: Array<{ productId: string; countedQuantity: number | null }> = [];
    for (const line of count.lines) {
      const parsed = parseCountedQuantity(countedByProduct[line.productId] ?? "");
      if (parsed === "invalid") {
        setLocalError(t("stockCount.invalidQuantity"));
        return;
      }
      lines.push({ productId: line.productId, countedQuantity: parsed });
    }
    await refreshAfter(
      () => updateStockCount(workspace, count.stockCountId, { lines }),
      t("stockCount.progressSaved"),
    );
  }

  async function onComplete() {
    if (!workspace || !count) {
      return;
    }
    if (!window.confirm(t("stockCount.completeConfirm"))) {
      return;
    }
    // Persist latest counted values before complete
    const lines: Array<{ productId: string; countedQuantity: number | null }> = [];
    for (const line of count.lines) {
      const parsed = parseCountedQuantity(countedByProduct[line.productId] ?? "");
      if (parsed === "invalid") {
        setLocalError(t("stockCount.invalidQuantity"));
        return;
      }
      if (parsed === null) {
        setLocalError(t("stockCount.allMustBeCounted"));
        return;
      }
      lines.push({ productId: line.productId, countedQuantity: parsed });
    }
    setBusy(true);
    setLocalError(null);
    try {
      await updateStockCount(workspace, count.stockCountId, { lines });
      const updated = await completeStockCount(workspace, count.stockCountId);
      hydratedIdRef.current = null;
      queryClient.setQueryData(["stock-count", workspace.organizationId, stockCountId], updated);
      await queryClient.invalidateQueries({ queryKey: ["stock-counts"] });
      await queryClient.invalidateQueries({ queryKey: ["inventory"] });
      setSuccess(t("stockCount.completedSuccess"));
      setMode("detail");
    } catch (err) {
      setLocalError(
        err instanceof PosApiError
          ? (err.problem.detail ?? t("stockCount.actionFailed"))
          : t("stockCount.actionFailed"),
      );
    } finally {
      setBusy(false);
    }
  }

  function focusNextUncounted(fromProductId: string) {
    if (!count) {
      return;
    }
    const ordered = visibleLines.length > 0 ? visibleLines : count.lines;
    const start = ordered.findIndex((l) => l.productId === fromProductId);
    const searchOrder = [...ordered.slice(start + 1), ...ordered.slice(0, start + 1)];
    for (const line of searchOrder) {
      if (line.productId === fromProductId) {
        continue;
      }
      const text = countedByProduct[line.productId] ?? "";
      if (text.trim() === "") {
        inputRefs.current[line.productId]?.focus();
        return;
      }
    }
  }

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  if (query.isLoading) {
    return <LoadingState label={t("stockCount.loading")} />;
  }

  if (query.isError || !count) {
    return (
      <div className="exits-page flex min-w-0 flex-col gap-3" data-testid="stock-count-detail-missing">
        <PageHeader
          title={t("stockCount.title")}
          backTo="/inventory/stock-counts"
          backLabel={t("stockCount.backList")}
          backTestId="page-header-back-stock-counts"
        />
        <ErrorState title={t("stockCount.errorTitle")} detail={t("stockCount.notFound")} />
      </div>
    );
  }

  const isDraft = count.status === "Draft";
  const isInProgress = count.status === "InProgress";
  const isCompleted = count.status === "Completed";
  const isCancelled = count.status === "Cancelled";
  const readOnly = isCompleted || isCancelled;
  const canMutate = allowManage && online && !busy;

  if (mode === "review" && isInProgress && summary) {
    return (
      <div
        className="stock-count-review-page exits-page flex min-w-0 flex-col gap-3 pb-4"
        data-testid="stock-count-review-page"
      >
        <PageHeader
          title={t("stockCount.reviewTitle")}
          description={t("stockCount.reviewLede")}
          backTo={`/inventory/stock-counts/${count.stockCountId}`}
          backLabel={t("stockCount.backToCount")}
          backTestId="page-header-back-stock-count"
        />
        {localError ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-danger" role="alert">
            {localError}
          </p>
        ) : null}
        <Card className="flex flex-col gap-2 p-4">
          <p className="m-0 font-medium">
            {t("stockCount.productsCounted").replace("{count}", String(summary.counted))}
          </p>
          <p className="m-0 text-[length:var(--exits-text-sm)]">
            {t("stockCount.matched")}: {summary.matched}
          </p>
          <p className="m-0 text-[length:var(--exits-text-sm)]">
            {t("stockCount.lowerThanSystem")}: {summary.lower} {t("stockCount.products")}
          </p>
          <p className="m-0 text-[length:var(--exits-text-sm)]">
            {t("stockCount.higherThanSystem")}: {summary.higher} {t("stockCount.products")}
          </p>
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("stockCount.inventoryWillAdjust")}
          </p>
          {summary.remaining > 0 ? (
            <p className="m-0 text-[length:var(--exits-text-sm)] text-danger" data-testid="stock-count-remaining">
              {t("stockCount.productsRemaining").replace("{count}", String(summary.remaining))}
            </p>
          ) : (
            <p className="m-0 text-[length:var(--exits-text-sm)] text-success" data-testid="stock-count-all-counted">
              {t("stockCount.allProductsCounted")}
            </p>
          )}
        </Card>

        <ul className="m-0 flex list-none flex-col gap-2 p-0" data-testid="stock-count-review-lines">
          {count.lines.map((line) => {
            const text = countedByProduct[line.productId] ?? "";
            const variance = previewVariance(line.systemOnHandSnapshot, text);
            return (
              <li key={line.lineId}>
                <Card
                  className={cn(
                    "flex flex-col gap-1 p-3",
                    variance != null && variance !== 0 && "border-[color-mix(in_srgb,var(--exits-warning)_40%,var(--border))]",
                  )}
                  data-testid={`stock-count-review-line-${line.productId}`}
                >
                  <p className="m-0 font-medium">{line.productName}</p>
                  <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                    {t("stockCount.systemStock")}: {line.systemOnHandSnapshot ?? "—"} {line.unitOfMeasure}
                  </p>
                  <p className="m-0 text-[length:var(--exits-text-sm)]">
                    {t("stockCount.countedQuantity")}: {text.trim() === "" ? "—" : text} {line.unitOfMeasure}
                  </p>
                  <p className="m-0 text-[length:var(--exits-text-sm)]">
                    {t("stockCount.difference")}: {formatVariance(variance)}
                    {variance === 0 ? ` · ${t("stockCount.noAdjustment")}` : null}
                  </p>
                </Card>
              </li>
            );
          })}
        </ul>

        <StickyActionBar>
          <div className="flex w-full flex-col gap-2 sm:flex-row">
            <Button
              type="button"
              variant="outline"
              className="min-h-11 flex-1"
              disabled={busy}
              onClick={() => setMode("detail")}
              data-testid="stock-count-back-to-count"
            >
              {t("stockCount.backToCount")}
            </Button>
            <Button
              type="button"
              className="min-h-11 flex-1"
              disabled={!canMutate || (summary?.remaining ?? 1) > 0}
              onClick={() => void onComplete()}
              data-testid="stock-count-complete"
            >
              {busy ? t("stockCount.completing") : t("stockCount.complete")}
            </Button>
          </div>
        </StickyActionBar>
      </div>
    );
  }

  return (
    <div
      className="stock-count-detail-page exits-page flex min-w-0 flex-col gap-3 pb-4"
      data-testid="stock-count-detail-page"
      data-status={count.status}
    >
      <PageHeader
        title={count.title}
        description={
          count.countNumber
            ? `${t("stockCount.countNumber")}: ${count.countNumber}`
            : t("stockCount.draftNumber")
        }
        backTo="/inventory/stock-counts"
        backLabel={t("stockCount.backList")}
        backTestId="page-header-back-stock-counts"
      />

      {!online ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("stockCount.offline")}</p>
      ) : null}

      {success ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-success" data-testid="stock-count-success">
          {success}
        </p>
      ) : null}
      {localError ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-danger" role="alert" data-testid="stock-count-local-error">
          {localError}
        </p>
      ) : null}

      <section className="flex flex-col gap-2" aria-labelledby="stock-count-info">
        <div className="flex flex-wrap items-center gap-2">
          <StatusChip tone={stockCountStatusTone(count.status)}>
            {t(stockCountStatusLabelKey(count.status))}
          </StatusChip>
          {isCompleted ? (
            <span className="text-[length:var(--exits-text-sm)] text-success" data-testid="stock-count-reconciled">
              {t("stockCount.inventoryReconciled")}
            </span>
          ) : null}
        </div>
        <dl className="m-0 grid gap-2 text-[length:var(--exits-text-sm)] sm:grid-cols-2">
          <div>
            <dt className="text-muted">{t("stockCount.countDate")}</dt>
            <dd className="m-0 font-medium">{formatStockCountDate(count.countDate)}</dd>
          </div>
          <div>
            <dt className="text-muted">{t("stockCount.products")}</dt>
            <dd className="m-0 font-medium">{count.lines.length}</dd>
          </div>
          {count.startedAtUtc ? (
            <div>
              <dt className="text-muted">{t("stockCount.started")}</dt>
              <dd className="m-0">{formatStockCountTimestamp(count.startedAtUtc)}</dd>
            </div>
          ) : null}
          {count.completedAtUtc ? (
            <div>
              <dt className="text-muted">{t("stockCount.completed")}</dt>
              <dd className="m-0">{formatStockCountTimestamp(count.completedAtUtc)}</dd>
            </div>
          ) : null}
          {count.cancelledAtUtc ? (
            <div>
              <dt className="text-muted">{t("stockCount.cancelled")}</dt>
              <dd className="m-0">{formatStockCountTimestamp(count.cancelledAtUtc)}</dd>
            </div>
          ) : null}
        </dl>
        {count.notes ? (
          <p className="m-0 text-[length:var(--exits-text-sm)]" data-testid="stock-count-notes-display">
            {count.notes}
          </p>
        ) : null}
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("stockCount.orgScopeNote")}</p>
      </section>

      {isDraft && allowManage ? (
        <section className="flex flex-col gap-2" data-testid="stock-count-draft-actions">
          {draftEditing ? (
            <>
              <label className="flex flex-col gap-1">
                <span className="text-[length:var(--exits-text-sm)] font-medium">{t("stockCount.fieldTitle")}</span>
                <input
                  className="exits-input min-h-11"
                  value={draftTitle}
                  onChange={(e) => setDraftTitle(e.target.value)}
                  maxLength={80}
                  data-testid="stock-count-edit-title"
                />
              </label>
              <label className="flex flex-col gap-1">
                <span className="text-[length:var(--exits-text-sm)] font-medium">{t("stockCount.countDate")}</span>
                <input
                  type="date"
                  className="exits-input min-h-11"
                  value={draftDate}
                  onChange={(e) => setDraftDate(e.target.value)}
                  data-testid="stock-count-edit-date"
                />
              </label>
              <label className="flex flex-col gap-1">
                <span className="text-[length:var(--exits-text-sm)] font-medium">{t("stockCount.notes")}</span>
                <textarea
                  className="exits-input min-h-20"
                  value={draftNotes}
                  onChange={(e) => setDraftNotes(e.target.value)}
                  maxLength={512}
                  data-testid="stock-count-edit-notes"
                />
              </label>
              <div className="flex flex-wrap gap-2">
                <Button
                  type="button"
                  className="min-h-11"
                  disabled={!canMutate}
                  onClick={() => void onSaveDraftMeta()}
                  data-testid="stock-count-save-draft-meta"
                >
                  {t("stockCount.saveDraft")}
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  className="min-h-11"
                  onClick={() => setDraftEditing(false)}
                >
                  {t("stockCount.backToCount")}
                </Button>
              </div>
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                {t("stockCount.editProductsHint")}
              </p>
              <Button
                type="button"
                variant="outline"
                className="min-h-11 w-full sm:w-auto"
                onClick={() => navigate("/inventory/stock-counts/new")}
                data-testid="stock-count-recreate-hint"
              >
                {t("stockCount.addProducts")}
              </Button>
            </>
          ) : (
            <div className="flex flex-wrap gap-2">
              <Button
                type="button"
                variant="outline"
                className="min-h-11"
                disabled={!canMutate}
                onClick={() => setDraftEditing(true)}
                data-testid="stock-count-edit"
              >
                {t("stockCount.edit")}
              </Button>
              <Button
                type="button"
                className="min-h-11"
                disabled={!canMutate || count.lines.length === 0}
                onClick={() => void onStart()}
                data-testid="stock-count-start"
              >
                {t("stockCount.start")}
              </Button>
              <Button
                type="button"
                variant="outline"
                className="min-h-11"
                disabled={!canMutate}
                onClick={() => void onCancel()}
                data-testid="stock-count-cancel"
              >
                {t("stockCount.cancel")}
              </Button>
            </div>
          )}
        </section>
      ) : null}

      {isInProgress ? (
        <section className="flex flex-col gap-2" data-testid="stock-count-in-progress">
          {summary ? (
            <p className="m-0 text-[length:var(--exits-text-sm)]" data-testid="stock-count-progress">
              {t("stockCount.progressCounted")
                .replace("{counted}", String(summary.counted))
                .replace("{total}", String(summary.total))}
              {summary.remaining > 0
                ? ` · ${t("stockCount.productsRemaining").replace("{count}", String(summary.remaining))}`
                : ` · ${t("stockCount.allProductsCounted")}`}
            </p>
          ) : null}

          <SearchField
            label={t("stockCount.searchWithinCount")}
            value={lineSearch}
            onChange={(e) => setLineSearch(e.target.value)}
            onClear={() => setLineSearch("")}
            placeholder={t("stockCount.searchWithinCount")}
            data-testid="stock-count-line-search"
          />

          <ExitsChipBar
            variant="filter"
            ariaLabel={t("stockCount.filter.lines")}
            testId="stock-count-line-filters"
            items={[
              {
                key: "all",
                label: t("stockCount.filter.all"),
                state: lineFilter === "all" ? "active" : "idle",
                onSelect: () => setLineFilter("all"),
                testId: "stock-count-line-filter-all",
              },
              {
                key: "notCounted",
                label: t("stockCount.filter.notCounted"),
                state: lineFilter === "notCounted" ? "active" : "idle",
                onSelect: () => setLineFilter("notCounted"),
                testId: "stock-count-line-filter-not-counted",
              },
              {
                key: "hasDifference",
                label: t("stockCount.filter.hasDifference"),
                state: lineFilter === "hasDifference" ? "active" : "idle",
                onSelect: () => setLineFilter("hasDifference"),
                testId: "stock-count-line-filter-diff",
              },
              {
                key: "matched",
                label: t("stockCount.filter.matched"),
                state: lineFilter === "matched" ? "active" : "idle",
                onSelect: () => setLineFilter("matched"),
                testId: "stock-count-line-filter-matched",
              },
            ]}
          />

          {/* Desktop / tablet table */}
          <div className="hidden md:block overflow-x-auto" data-testid="stock-count-table">
            <table className="w-full min-w-[36rem] border-collapse text-left text-[length:var(--exits-text-sm)]">
              <thead>
                <tr className="border-b border-border">
                  <th className="px-2 py-2 font-medium">{t("stockCount.product")}</th>
                  <th className="px-2 py-2 font-medium">{t("stockCount.systemStock")}</th>
                  <th className="px-2 py-2 font-medium">{t("stockCount.physicalCount")}</th>
                  <th className="px-2 py-2 font-medium">{t("stockCount.difference")}</th>
                </tr>
              </thead>
              <tbody>
                {visibleLines.map((line) => {
                  const text = countedByProduct[line.productId] ?? "";
                  const variance = previewVariance(line.systemOnHandSnapshot, text);
                  return (
                    <tr
                      key={line.lineId}
                      className="border-b border-border"
                      data-testid={`stock-count-row-line-${line.productId}`}
                    >
                      <td className="px-2 py-2 font-medium">
                        {line.productName}
                        <span className="ml-1 text-muted">{line.unitOfMeasure}</span>
                      </td>
                      <td className="px-2 py-2" data-testid={`stock-count-system-${line.productId}`}>
                        {line.systemOnHandSnapshot ?? "—"}
                      </td>
                      <td className="px-2 py-2">
                        <input
                          ref={(el) => {
                            inputRefs.current[line.productId] = el;
                          }}
                          className="exits-input min-h-11 w-28"
                          inputMode="decimal"
                          value={text}
                          disabled={readOnly || !allowManage || !online || busy}
                          onChange={(e) =>
                            setCountedByProduct((prev) => ({
                              ...prev,
                              [line.productId]: e.target.value,
                            }))
                          }
                          onKeyDown={(e) => {
                            if (e.key === "Enter") {
                              e.preventDefault();
                              focusNextUncounted(line.productId);
                            }
                          }}
                          aria-label={`${t("stockCount.physicalCount")} ${line.productName}`}
                          data-testid={`stock-count-qty-${line.productId}`}
                        />
                      </td>
                      <td
                        className={cn(
                          "px-2 py-2 font-medium",
                          variance != null && variance < 0 && "text-danger",
                          variance != null && variance > 0 && "text-success",
                        )}
                        data-testid={`stock-count-variance-${line.productId}`}
                      >
                        <span aria-label={formatVariance(variance)}>{formatVariance(variance)}</span>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>

          {/* Mobile cards */}
          <ul className="m-0 flex list-none flex-col gap-3 p-0 md:hidden" data-testid="stock-count-mobile-lines">
            {visibleLines.map((line) => {
              const text = countedByProduct[line.productId] ?? "";
              const variance = previewVariance(line.systemOnHandSnapshot, text);
              return (
                <li key={line.lineId}>
                  <Card className="flex flex-col gap-2 p-3" data-testid={`stock-count-mobile-line-${line.productId}`}>
                    <p className="m-0 text-[length:var(--exits-text-base)] font-semibold">{line.productName}</p>
                    <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                      {t("stockCount.systemStock")}: {line.systemOnHandSnapshot ?? "—"} {line.unitOfMeasure}
                    </p>
                    <label className="flex flex-col gap-1">
                      <span className="text-[length:var(--exits-text-sm)] font-medium">
                        {t("stockCount.physicalCount")}
                      </span>
                      <input
                        ref={(el) => {
                          inputRefs.current[line.productId] = el;
                        }}
                        className="exits-input min-h-12 text-[length:var(--exits-text-lg)]"
                        inputMode="decimal"
                        value={text}
                        disabled={readOnly || !allowManage || !online || busy}
                        onChange={(e) =>
                          setCountedByProduct((prev) => ({
                            ...prev,
                            [line.productId]: e.target.value,
                          }))
                        }
                        onKeyDown={(e) => {
                          if (e.key === "Enter") {
                            e.preventDefault();
                            focusNextUncounted(line.productId);
                          }
                        }}
                        data-testid={`stock-count-qty-mobile-${line.productId}`}
                      />
                    </label>
                    <p
                      className={cn(
                        "m-0 text-[length:var(--exits-text-sm)] font-medium",
                        variance != null && variance < 0 && "text-danger",
                        variance != null && variance > 0 && "text-success",
                      )}
                    >
                      {t("stockCount.difference")}: {formatVariance(variance)}
                    </p>
                  </Card>
                </li>
              );
            })}
          </ul>

          {visibleLines.length === 0 ? (
            <EmptyState title={t("stockCount.noMatchingLines")} detail={t("stockCount.noMatchingLinesDetail")} />
          ) : null}

          {allowManage ? (
            <StickyActionBar>
              <div className="flex w-full flex-col gap-2 sm:flex-row">
                <Button
                  type="button"
                  variant="outline"
                  className="min-h-11 flex-1"
                  disabled={!canMutate}
                  onClick={() => void onSaveProgress()}
                  data-testid="stock-count-save-progress"
                >
                  {busy ? t("stockCount.saving") : t("stockCount.saveProgress")}
                </Button>
                <Button
                  type="button"
                  className="min-h-11 flex-1"
                  disabled={!canMutate}
                  onClick={() => {
                    setLocalError(null);
                    setMode("review");
                  }}
                  data-testid="stock-count-review"
                >
                  {t("stockCount.reviewComplete")}
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  className="min-h-11 flex-1"
                  disabled={!canMutate}
                  onClick={() => void onCancel()}
                  data-testid="stock-count-cancel-in-progress"
                >
                  {t("stockCount.cancel")}
                </Button>
              </div>
            </StickyActionBar>
          ) : null}
        </section>
      ) : null}

      {(isDraft || readOnly) && !isInProgress ? (
        <section aria-labelledby="stock-count-lines-heading">
          <h2 id="stock-count-lines-heading" className="m-0 mb-2 text-[length:var(--exits-text-base)] font-semibold">
            {t("stockCount.products")}
          </h2>
          <ul className="m-0 flex list-none flex-col gap-2 p-0" data-testid="stock-count-lines-readonly">
            {count.lines.map((line) => (
              <li key={line.lineId}>
                <Card
                  className={cn(
                    "flex flex-col gap-1 p-3",
                    line.variance != null &&
                      line.variance !== 0 &&
                      "border-[color-mix(in_srgb,var(--exits-warning)_40%,var(--border))]",
                  )}
                  data-testid={`stock-count-line-${line.lineId}`}
                >
                  <div className="flex flex-wrap items-start justify-between gap-2">
                    <div className="min-w-0">
                      <p className="m-0 font-medium">{line.productName}</p>
                      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                        {line.unitOfMeasure}
                        {line.systemOnHandSnapshot != null
                          ? ` · ${t("stockCount.systemStock")}: ${line.systemOnHandSnapshot}`
                          : ""}
                        {line.countedQuantity != null
                          ? ` · ${t("stockCount.countedQuantity")}: ${line.countedQuantity}`
                          : ""}
                        {line.variance != null
                          ? ` · ${t("stockCount.difference")}: ${formatVariance(line.variance)}`
                          : ""}
                      </p>
                    </div>
                    {isDraft && allowManage && count.lines.length > 1 ? (
                      <Button
                        type="button"
                        variant="outline"
                        className="min-h-11"
                        disabled={!canMutate}
                        onClick={() => {
                          void refreshAfter(
                            () =>
                              updateStockCount(workspace!, count.stockCountId, {
                                title: count.title,
                                countDate: count.countDate.slice(0, 10),
                                notes: count.notes ?? null,
                                lines: count.lines
                                  .filter((l) => l.productId !== line.productId)
                                  .map((l) => ({ productId: l.productId })),
                              }),
                            t("stockCount.draftSaved"),
                          );
                        }}
                        data-testid={`stock-count-draft-remove-${line.productId}`}
                      >
                        {t("stockCount.removeProduct")}
                      </Button>
                    ) : null}
                  </div>
                </Card>
              </li>
            ))}
          </ul>
        </section>
      ) : null}

      {isCancelled && allowManage ? null : null}
    </div>
  );
}
