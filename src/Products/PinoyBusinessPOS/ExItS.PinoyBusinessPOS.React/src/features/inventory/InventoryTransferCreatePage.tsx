import { useEffect, useMemo, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { ArrowRightLeft, Minus, Plus, RotateCcw, X } from "lucide-react";
import { canManageInventory } from "@/access/pos-capabilities";
import {
  listInventory,
  listProductLots,
  type PosInventoryAccountDto,
  type PosInventoryLotDto,
} from "@/api/pos/pos-inventory-client";
import { PosApiError } from "@/api/pos/pos-http";
import { createInventoryTransfer } from "@/api/pos/pos-inventory-transfer-client";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { StickyActionBar } from "@/components/exits/FoundationStates";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { SearchField } from "@/components/exits/SearchField";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { parseTransferQuantity } from "@/features/inventory/inventory-transfer-labels";
import {
  canAddTransferQuantity,
  evaluateTransferLineStock,
  lotDemandExcludingLine,
  productDemandExcludingLine,
  type TransferLineStockIssue,
} from "@/features/inventory/inventory-transfer-stock-guard";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";
import { createSecureMutationId } from "@/lib/secure-mutation-id";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const qtyFieldClassName = cn(
  "box-border h-[var(--exits-control-height)] min-h-[var(--exits-control-height)]",
  "w-full min-w-0 rounded-[var(--exits-radius-md)] border border-border bg-surface",
  "px-[var(--exits-control-padding-x)] text-[length:var(--exits-text-md)] font-normal text-foreground",
  "outline-none transition-[border-color,box-shadow] duration-[var(--exits-motion-fast)]",
  "placeholder:text-[var(--exits-text-subtle)] hover:border-[var(--exits-border-strong)]",
  "focus-visible:border-[var(--exits-ring)] focus-visible:ring-2 focus-visible:ring-[var(--exits-ring)]",
  "exits-input--no-spin",
);

type DraftLine = {
  key: string;
  productId: string;
  name: string;
  unitOfMeasure: string;
  quantity: number;
  tracksExpiration: boolean;
  isTracked: boolean;
  sourceLotId: string | null;
  lotNumber: string | null;
  expirationDate: string | null;
  availableQuantity: number;
  lotAvailableQuantity: number | null;
};

function lineKeyOf(line: { productId: string; sourceLotId: string | null }) {
  return `${line.productId}:${line.sourceLotId ?? "none"}`;
}

export function InventoryTransferCreatePage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const online = useBrowserOnline();
  const { boundWorkspace, sessionGrant, workspaces } = useWorkspace();
  const allowManage = canManageInventory(sessionGrant);

  const [destinationBranchId, setDestinationBranchId] = useState("");
  const [notes, setNotes] = useState("");
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");
  const [lines, setLines] = useState<DraftLine[]>([]);
  const [qtyByProduct, setQtyByProduct] = useState<Record<string, string>>({});
  const [lotByProduct, setLotByProduct] = useState<Record<string, string>>({});
  const [lotsCache, setLotsCache] = useState<Record<string, PosInventoryLotDto[]>>({});
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const operationIdRef = useRef<string | null>(null);

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

  const orgBranches = useMemo(() => {
    const org = workspaces.find((w) => w.organizationId === boundWorkspace?.organizationId);
    return (org?.branches ?? []).filter((b) => b.isActive);
  }, [workspaces, boundWorkspace?.organizationId]);

  const destinations = useMemo(
    () => orgBranches.filter((b) => b.branchId !== boundWorkspace?.branchId),
    [orgBranches, boundWorkspace?.branchId],
  );

  const multiBranch = orgBranches.length >= 2;
  const sourceName = boundWorkspace?.branchName ?? t("transfer.sourceBranch");

  const pickerQueryKey = [
    "inventory",
    "transfer-picker",
    workspace?.organizationId,
    workspace?.branchId,
    debounced,
  ] as const;

  const pickerQuery = useQuery({
    queryKey: pickerQueryKey,
    enabled: Boolean(workspace) && online && allowManage && multiBranch,
    queryFn: ({ signal }) =>
      listInventory(
        workspace!,
        { search: debounced || undefined, pageSize: 40, tracked: true },
        signal,
      ),
  });

  const availabilityByProduct = useMemo(() => {
    const map = new Map<string, number>();
    for (const row of pickerQuery.data?.items ?? []) {
      if (row.isTracked) {
        map.set(row.productId, Math.max(0, row.onHandQuantity));
      }
    }
    return map;
  }, [pickerQuery.data?.items]);

  // Refresh line availability when source-branch inventory query updates.
  useEffect(() => {
    if (availabilityByProduct.size === 0) {
      return;
    }
    setLines((prev) =>
      prev.map((line) => {
        const nextAvailable = availabilityByProduct.get(line.productId);
        if (nextAvailable === undefined || nextAvailable === line.availableQuantity) {
          return line;
        }
        return { ...line, availableQuantity: nextAvailable };
      }),
    );
  }, [availabilityByProduct]);

  const pickerRows = useMemo(
    () => (pickerQuery.data?.items ?? []).filter((row) => row.isTracked),
    [pickerQuery.data?.items],
  );

  function stockIssueMessage(issue: TransferLineStockIssue, available: number, uom: string): string {
    switch (issue) {
      case "untracked":
        return t("transfer.notTracked");
      case "out_of_stock":
      case "lot_out_of_stock":
        return t("transfer.outOfStock");
      case "over_stock":
      case "lot_over_stock":
        return t("transfer.onlyAvailableAtSource")
          .replace("{qty}", String(available))
          .replace("{uom}", uom)
          .replace("{branch}", sourceName);
      case "invalid_qty":
        return t("transfer.invalidQuantity");
    }
  }

  function formatAvailable(qty: number, uom: string) {
    return t("transfer.available").replace("{qty}", String(qty)).replace("{uom}", uom);
  }

  async function ensureLots(productId: string, tracksExpiration: boolean) {
    if (!workspace || !tracksExpiration || lotsCache[productId]) {
      return lotsCache[productId] ?? [];
    }
    try {
      const result = await listProductLots(workspace, productId, { pageSize: 50 });
      setLotsCache((prev) => ({ ...prev, [productId]: result.items }));
      return result.items;
    } catch {
      setLotsCache((prev) => ({ ...prev, [productId]: [] }));
      return [];
    }
  }

  async function addLine(row: PosInventoryAccountDto) {
    const tracksExpiration = row.tracksExpiration === true;
    const availableQuantity = Math.max(0, row.onHandQuantity);
    const lots = await ensureLots(row.productId, tracksExpiration);
    const qtyParsed = parseTransferQuantity(qtyByProduct[row.productId] ?? "");
    if (qtyParsed === "empty" || qtyParsed === "invalid") {
      setError(t("transfer.invalidQuantity"));
      return;
    }
    let sourceLotId: string | null = null;
    let lotNumber: string | null = null;
    let expirationDate: string | null = null;
    let lotAvailableQuantity: number | null = null;
    if (tracksExpiration) {
      const lotId = lotByProduct[row.productId]?.trim() || "";
      if (!lotId) {
        setError(t("transfer.lotRequired"));
        return;
      }
      const lot = lots.find((l) => l.lotId === lotId) ?? lotsCache[row.productId]?.find((l) => l.lotId === lotId);
      if (!lot) {
        setError(t("transfer.lotRequired"));
        return;
      }
      sourceLotId = lot.lotId;
      lotNumber = lot.lotNumber ?? null;
      expirationDate = lot.expirationDate ?? null;
      lotAvailableQuantity = Math.max(0, lot.quantityOnHand);
    }

    if (availableQuantity <= 0 || (tracksExpiration && (lotAvailableQuantity ?? 0) <= 0)) {
      setError(t("transfer.outOfStock"));
      return;
    }

    const key = lineKeyOf({ productId: row.productId, sourceLotId });
    const existingProductDemand = productDemandExcludingLine(lines, row.productId, key);
    const existingLotDemand =
      sourceLotId != null ? lotDemandExcludingLine(lines, sourceLotId, key) : 0;
    const issue = canAddTransferQuantity({
      quantity: qtyParsed,
      availableQuantity,
      lotAvailableQuantity,
      tracksExpiration,
      existingProductDemand,
      existingLotDemand,
    });
    if (issue) {
      const cap =
        tracksExpiration && lotAvailableQuantity != null
          ? Math.min(availableQuantity, lotAvailableQuantity)
          : availableQuantity;
      setError(stockIssueMessage(issue, cap, row.unitOfMeasure));
      return;
    }

    setError(null);
    setLines((prev) => {
      const existingIndex = prev.findIndex((l) => l.key === key);
      if (existingIndex >= 0) {
        const next = [...prev];
        next[existingIndex] = {
          ...next[existingIndex],
          quantity: qtyParsed,
          availableQuantity,
          lotAvailableQuantity,
        };
        return next;
      }
      return [
        ...prev,
        {
          key,
          productId: row.productId,
          name: row.name,
          unitOfMeasure: row.unitOfMeasure,
          quantity: qtyParsed,
          tracksExpiration,
          isTracked: row.isTracked,
          sourceLotId,
          lotNumber,
          expirationDate,
          availableQuantity,
          lotAvailableQuantity,
        },
      ];
    });
    setQtyByProduct((prev) => ({ ...prev, [row.productId]: "" }));
  }

  function removeLine(key: string) {
    setLines((prev) => prev.filter((l) => l.key !== key));
  }

  function updateLineQuantity(key: string, raw: string) {
    const parsed = parseTransferQuantity(raw);
    if (parsed === "empty" || parsed === "invalid") {
      setLines((prev) =>
        prev.map((line) => (line.key === key ? { ...line, quantity: 0 } : line)),
      );
      setError(t("transfer.invalidQuantity"));
      return;
    }
    setLines((prev) => {
      const target = prev.find((l) => l.key === key);
      if (!target) {
        return prev;
      }
      const nextLine = { ...target, quantity: parsed };
      const issue = evaluateTransferLineStock(nextLine, prev);
      if (issue) {
        const cap =
          nextLine.lotAvailableQuantity != null
            ? Math.min(nextLine.availableQuantity, nextLine.lotAvailableQuantity)
            : nextLine.availableQuantity;
        setError(stockIssueMessage(issue, cap, nextLine.unitOfMeasure));
      } else {
        setError(null);
      }
      return prev.map((line) => (line.key === key ? nextLine : line));
    });
  }

  function stepLineQuantity(key: string, delta: number) {
    const line = lines.find((l) => l.key === key);
    if (!line) {
      return;
    }
    const next = Math.max(0, Math.round((line.quantity + delta) * 10000) / 10000);
    updateLineQuantity(key, String(next));
  }

  function resetForm() {
    if (saving) {
      return;
    }
    setDestinationBranchId("");
    setNotes("");
    setSearch("");
    setDebounced("");
    setLines([]);
    setQtyByProduct({});
    setLotByProduct({});
    setError(null);
    operationIdRef.current = null;
  }

  const lineIssues = useMemo(() => {
    const map = new Map<string, TransferLineStockIssue>();
    for (const line of lines) {
      const issue = evaluateTransferLineStock(line, lines);
      if (issue) {
        map.set(line.key, issue);
      }
    }
    return map;
  }, [lines]);

  const createBlockedReason = useMemo(() => {
    if (lines.length === 0) {
      return t("transfer.draftEmpty");
    }
    if (!destinationBranchId) {
      return t("transfer.destinationRequired");
    }
    for (const line of lines) {
      const issue = lineIssues.get(line.key);
      if (!issue) {
        continue;
      }
      const cap =
        line.lotAvailableQuantity != null
          ? Math.min(line.availableQuantity, line.lotAvailableQuantity)
          : line.availableQuantity;
      return stockIssueMessage(issue, cap, line.unitOfMeasure);
    }
    return null;
    // stockIssueMessage uses t/sourceName; intentional.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [lines, lineIssues, destinationBranchId, sourceName, t]);

  async function refreshAvailability() {
    await queryClient.invalidateQueries({ queryKey: ["inventory", "transfer-picker"] });
    setLotsCache({});
  }

  async function saveDraft() {
    if (!workspace || !boundWorkspace?.branchId || !allowManage || !online || saving) {
      return;
    }
    if (createBlockedReason) {
      setError(createBlockedReason);
      return;
    }
    if (destinationBranchId === boundWorkspace.branchId) {
      setError(t("transfer.sameBranch"));
      return;
    }
    if (!operationIdRef.current) {
      const generated = createSecureMutationId();
      if (!generated.ok) {
        setError(t("transfer.saveFailed"));
        return;
      }
      operationIdRef.current = generated.id;
    }
    setSaving(true);
    setError(null);
    try {
      const created = await createInventoryTransfer(workspace, {
        sourceBranchId: boundWorkspace.branchId,
        destinationBranchId,
        notes: notes.trim() || null,
        operationId: operationIdRef.current,
        lines: lines.map((line) => ({
          productId: line.productId,
          quantity: line.quantity,
          sourceLotId: line.sourceLotId,
        })),
      });
      operationIdRef.current = null;
      navigate(`/inventory/transfers/${created.transferId}`, {
        replace: true,
        state: { flash: "created" },
      });
    } catch (err) {
      const detail =
        err instanceof PosApiError
          ? (err.problem.detail ?? t("transfer.saveFailed"))
          : t("transfer.saveFailed");
      setError(detail);
      await refreshAvailability();
      // Keep entered lines for correction after server rejection.
    } finally {
      setSaving(false);
    }
  }

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  if (!allowManage) {
    return (
      <div className="exits-page flex min-w-0 flex-col gap-3" data-testid="transfer-create-denied">
        <PageHeader
          title={t("transfer.newTitle")}
          backTo="/inventory/transfers"
          backLabel={t("transfer.backList")}
          backTestId="page-header-back-transfers"
        />
        <ErrorState title={t("transfer.errorTitle")} detail={t("transfer.manageDenied")} />
      </div>
    );
  }

  if (!multiBranch) {
    return (
      <div className="exits-page flex min-w-0 flex-col gap-3" data-testid="transfer-create-single-branch">
        <PageHeader
          title={t("transfer.newTitle")}
          backTo="/inventory/transfers"
          backLabel={t("transfer.backList")}
          backTestId="page-header-back-transfers"
        />
        <EmptyState title={t("transfer.requiresTwoBranches")} detail={t("transfer.singleBranchDetail")} />
      </div>
    );
  }

  const createDisabled =
    !online || saving || Boolean(createBlockedReason) || lines.length === 0 || !destinationBranchId;

  return (
    <div
      className="inventory-transfer-create-page exits-page flex min-w-0 flex-col gap-3 pb-4"
      data-testid="inventory-transfer-create-page"
    >
      <PageHeader
        title={t("transfer.newTitle")}
        description={t("transfer.newLede")}
        backTo="/inventory/transfers"
        backLabel={t("transfer.backList")}
        backTestId="page-header-back-transfers"
      />

      {!online ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("transfer.offline")}</p>
      ) : null}

      {error ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-danger" role="alert" data-testid="transfer-create-error">
          {error}
        </p>
      ) : null}

      {createBlockedReason && lines.length > 0 && destinationBranchId ? (
        <p
          className="m-0 text-[length:var(--exits-text-sm)] text-muted"
          data-testid="transfer-create-blocked-reason"
        >
          {createBlockedReason}
        </p>
      ) : null}

      <div className="grid gap-3 sm:grid-cols-2 sm:gap-4">
        <div className="flex min-w-0 flex-col gap-1">
          <span className="exits-type-label">{t("transfer.fromBranch")}</span>
          <p
            className="m-0 text-[length:var(--exits-text-md)] font-medium text-foreground"
            data-testid="transfer-source-branch"
          >
            {sourceName}
          </p>
          <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">{t("transfer.sourceFixedHint")}</p>
        </div>

        <label className="flex min-w-0 flex-col gap-1">
          <span className="exits-type-label">{t("transfer.toBranch")}</span>
          <select
            className="exits-select"
            value={destinationBranchId}
            onChange={(e) => setDestinationBranchId(e.target.value)}
            data-testid="transfer-destination-branch"
          >
            <option value="">{t("transfer.selectDestination")}</option>
            {destinations.map((branch) => (
              <option key={branch.branchId} value={branch.branchId}>
                {branch.name}
                {branch.secondaryLine ? ` — ${branch.secondaryLine}` : ""}
              </option>
            ))}
          </select>
        </label>
      </div>

      <label className="flex min-w-0 flex-col gap-1">
        <span className="exits-type-label">
          {t("transfer.notes")}{" "}
          <span className="font-normal text-muted">({t("transfer.notesOptional")})</span>
        </span>
        <textarea
          className="exits-input resize-y [min-height:unset]"
          rows={2}
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          maxLength={512}
          data-testid="transfer-notes"
        />
      </label>

      <section className="flex flex-col gap-1.5" data-testid="transfer-draft-lines">
        <h2 className="m-0 text-[length:var(--exits-text-sm)] font-semibold text-foreground">
          {t("transfer.items")}
        </h2>
        {lines.length === 0 ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("transfer.itemsEmpty")}</p>
        ) : (
          <ul className="m-0 grid list-none grid-cols-1 gap-2 p-0 md:grid-cols-2">
            {lines.map((line) => {
              const issue = lineIssues.get(line.key);
              const maxQty =
                line.lotAvailableQuantity != null
                  ? Math.min(line.availableQuantity, line.lotAvailableQuantity)
                  : line.availableQuantity;
              const canDecrease = line.quantity > 1;
              const canIncrease = line.quantity < maxQty;
              return (
                <li
                  key={line.key}
                  className="flex flex-wrap items-center gap-2 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 py-2"
                  data-testid={`transfer-line-${line.key}`}
                >
                  <div className="min-w-0 flex-1">
                    <p className="m-0 truncate text-[length:var(--exits-text-sm)] font-medium">{line.name}</p>
                    <p
                      className={cn(
                        "m-0 text-[length:var(--exits-text-xs)]",
                        maxQty <= 0 || issue ? "text-danger" : "text-muted",
                      )}
                      data-testid={`transfer-line-available-${line.key}`}
                    >
                      {maxQty <= 0
                        ? t("transfer.outOfStock")
                        : formatAvailable(maxQty, line.unitOfMeasure)}
                      {line.lotNumber || line.expirationDate
                        ? ` · ${t("transfer.lot")}: ${line.lotNumber ?? "—"} · ${t("transfer.expiry")}: ${line.expirationDate ?? "—"}`
                        : ""}
                    </p>
                  </div>
                  <div className="flex shrink-0 items-center gap-1.5">
                    <Button
                      type="button"
                      size="icon"
                      variant="outline"
                      className={cn(
                        "size-9 shrink-0 rounded-full border-border text-foreground",
                        canDecrease
                          ? "hover:border-[var(--exits-border-strong)] hover:bg-[var(--exits-surface-muted)]"
                          : "text-muted opacity-50",
                      )}
                      disabled={!canDecrease}
                      aria-label={t("transfer.decreaseQuantity")}
                      onClick={() => stepLineQuantity(line.key, -1)}
                      data-testid={`transfer-line-dec-${line.key}`}
                    >
                      <Minus className="size-4" aria-hidden />
                    </Button>
                    <input
                      type="number"
                      inputMode="decimal"
                      step="any"
                      min={0}
                      className={cn(qtyFieldClassName, "w-[4.25rem] shrink-0 text-center")}
                      value={line.quantity > 0 ? String(line.quantity) : ""}
                      onChange={(e) => updateLineQuantity(line.key, e.target.value)}
                      data-testid={`transfer-line-qty-${line.key}`}
                      aria-invalid={Boolean(issue)}
                    />
                    <Button
                      type="button"
                      size="icon"
                      variant={canIncrease ? "default" : "outline"}
                      className={cn(
                        "size-9 shrink-0 rounded-full",
                        !canIncrease && "text-muted opacity-50",
                      )}
                      disabled={!canIncrease}
                      aria-label={t("transfer.increaseQuantity")}
                      onClick={() => stepLineQuantity(line.key, 1)}
                      data-testid={`transfer-line-inc-${line.key}`}
                    >
                      <Plus className="size-4" aria-hidden />
                    </Button>
                  </div>
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    className="shrink-0 rounded-full"
                    aria-label={t("transfer.remove")}
                    onClick={() => removeLine(line.key)}
                    data-testid={`transfer-remove-${line.key}`}
                  >
                    <X className="size-4" aria-hidden />
                  </Button>
                </li>
              );
            })}
          </ul>
        )}
      </section>

      <section className="flex flex-col gap-2">
        <div className="flex min-w-0 flex-col gap-1">
          <h2 className="m-0 text-[length:var(--exits-text-sm)] font-semibold text-foreground">
            {t("transfer.addProducts")}
          </h2>
          <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">{t("transfer.baseUomHint")}</p>
        </div>
        <SearchField
          label={t("transfer.searchProducts")}
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          onClear={() => setSearch("")}
          placeholder={t("transfer.searchProducts")}
          data-testid="transfer-product-search"
        />
        {pickerQuery.isLoading ? <LoadingState label={t("transfer.loading")} /> : null}
        {!pickerQuery.isLoading && pickerRows.length === 0 ? (
          <EmptyState title={t("transfer.noProducts")} detail={t("transfer.noProductsDetail")} />
        ) : null}
        {pickerRows.length > 0 ? (
          <ul
            className="m-0 grid list-none grid-cols-1 gap-2 p-0 md:grid-cols-2"
            data-testid="transfer-product-picker"
          >
            {pickerRows.map((row) => {
              const tracksExpiration = row.tracksExpiration === true;
              const lots = lotsCache[row.productId] ?? [];
              const available = Math.max(0, row.onHandQuantity);
              const outOfStock = available <= 0;
              const selectedLotId = lotByProduct[row.productId] ?? "";
              const selectedLot = lots.find((l) => l.lotId === selectedLotId);
              const lotOut =
                tracksExpiration && selectedLot != null && selectedLot.quantityOnHand <= 0;
              const addDisabled = !online || outOfStock || lotOut;
              return (
                <li
                  key={row.productId}
                  className="rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 py-2"
                  data-testid={`transfer-picker-row-${row.productId}`}
                >
                  <div className="flex min-w-0 items-center gap-2">
                    <div className="min-w-0 flex-1">
                      <p className="m-0 truncate text-[length:var(--exits-text-sm)] font-medium text-foreground">
                        {row.name}
                      </p>
                      <p
                        className={cn(
                          "m-0 truncate text-[length:var(--exits-text-xs)]",
                          outOfStock ? "text-danger" : "text-muted",
                        )}
                        data-testid={`transfer-picker-available-${row.productId}`}
                      >
                        {outOfStock
                          ? t("transfer.outOfStock")
                          : formatAvailable(available, row.unitOfMeasure)}
                        {tracksExpiration ? ` · ${t("transfer.tracksExpiry")}` : ""}
                      </p>
                    </div>
                    {outOfStock ? (
                      <span
                        className="shrink-0 text-[length:var(--exits-text-xs)] text-muted"
                        data-testid={`transfer-picker-unavailable-${row.productId}`}
                      >
                        {t("transfer.unavailable")}
                      </span>
                    ) : (
                      <>
                        <label className="sr-only" htmlFor={`transfer-qty-${row.productId}`}>
                          {t("transfer.quantity")}
                        </label>
                        <input
                          id={`transfer-qty-${row.productId}`}
                          type="number"
                          inputMode="decimal"
                          step="any"
                          min={0}
                          className={cn(qtyFieldClassName, "w-[5.5rem] shrink-0")}
                          placeholder={t("transfer.quantity")}
                          value={qtyByProduct[row.productId] ?? ""}
                          onChange={(e) =>
                            setQtyByProduct((prev) => ({ ...prev, [row.productId]: e.target.value }))
                          }
                          data-testid={`transfer-picker-qty-${row.productId}`}
                        />
                        <Button
                          type="button"
                          size="icon"
                          className="shrink-0 rounded-full"
                          disabled={addDisabled}
                          aria-label={t("transfer.addProduct")}
                          onClick={() => void addLine(row)}
                          data-testid={`transfer-add-${row.productId}`}
                        >
                          <Plus className="size-4" aria-hidden />
                        </Button>
                      </>
                    )}
                  </div>
                  {tracksExpiration && !outOfStock ? (
                    <select
                      className="exits-select mt-2"
                      value={lotByProduct[row.productId] ?? ""}
                      onFocus={() => void ensureLots(row.productId, true)}
                      onChange={(e) =>
                        setLotByProduct((prev) => ({ ...prev, [row.productId]: e.target.value }))
                      }
                      data-testid={`transfer-lot-${row.productId}`}
                    >
                      <option value="">{t("transfer.selectLot")}</option>
                      {lots.map((lot) => (
                        <option key={lot.lotId} value={lot.lotId} disabled={lot.quantityOnHand <= 0}>
                          {(lot.lotNumber ?? t("transfer.lot")) +
                            ` · ${lot.expirationDate ?? "—"} · ${lot.quantityOnHand}`}
                          {lot.quantityOnHand <= 0 ? ` (${t("transfer.outOfStock")})` : ""}
                        </option>
                      ))}
                    </select>
                  ) : null}
                </li>
              );
            })}
          </ul>
        ) : null}
      </section>

      <StickyActionBar className="justify-end shadow-[0_-4px_24px_color-mix(in_srgb,var(--exits-foreground)_8%,transparent)]">
        <Button
          type="button"
          variant="outline"
          disabled={saving}
          onClick={() => navigate("/inventory/transfers")}
          data-testid="transfer-cancel-create"
        >
          <X className="size-4 shrink-0" aria-hidden />
          {t("transfer.cancelCreate")}
        </Button>
        <Button
          type="button"
          variant="outline"
          disabled={saving}
          onClick={resetForm}
          data-testid="transfer-reset-create"
        >
          <RotateCcw className="size-4 shrink-0" aria-hidden />
          {t("transfer.resetCreate")}
        </Button>
        <Button
          type="button"
          disabled={createDisabled}
          onClick={() => void saveDraft()}
          data-testid="transfer-save-draft"
        >
          <ArrowRightLeft className="size-4 shrink-0" aria-hidden />
          {saving ? t("transfer.saving") : t("transfer.saveDraft")}
        </Button>
      </StickyActionBar>
    </div>
  );
}
