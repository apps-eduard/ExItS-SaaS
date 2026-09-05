import { useEffect, useMemo, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ArrowRightLeft, Plus, RotateCcw, X } from "lucide-react";
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
  sourceLotId: string | null;
  lotNumber: string | null;
  expirationDate: string | null;
};

export function InventoryTransferCreatePage() {
  const { t } = useI18n();
  const navigate = useNavigate();
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

  const pickerQuery = useQuery({
    queryKey: [
      "inventory",
      "transfer-picker",
      workspace?.organizationId,
      workspace?.branchId,
      debounced,
    ],
    enabled: Boolean(workspace) && online && allowManage && multiBranch,
    queryFn: ({ signal }) =>
      listInventory(
        workspace!,
        { search: debounced || undefined, pageSize: 40, tracked: true },
        signal,
      ),
  });

  const pickerRows = useMemo(
    () => (pickerQuery.data?.items ?? []).filter((row) => row.isTracked),
    [pickerQuery.data?.items],
  );

  async function ensureLots(productId: string, tracksExpiration: boolean) {
    if (!workspace || !tracksExpiration || lotsCache[productId]) {
      return;
    }
    try {
      const result = await listProductLots(workspace, productId, { pageSize: 50 });
      setLotsCache((prev) => ({ ...prev, [productId]: result.items }));
    } catch {
      setLotsCache((prev) => ({ ...prev, [productId]: [] }));
    }
  }

  async function addLine(row: PosInventoryAccountDto) {
    const tracksExpiration = row.tracksExpiration === true;
    await ensureLots(row.productId, tracksExpiration);
    const qtyParsed = parseTransferQuantity(qtyByProduct[row.productId] ?? "");
    if (qtyParsed === "empty" || qtyParsed === "invalid") {
      setError(t("transfer.invalidQuantity"));
      return;
    }
    let sourceLotId: string | null = null;
    let lotNumber: string | null = null;
    let expirationDate: string | null = null;
    if (tracksExpiration) {
      const lotId = lotByProduct[row.productId]?.trim() || "";
      if (!lotId) {
        setError(t("transfer.lotRequired"));
        return;
      }
      const lots = lotsCache[row.productId] ?? [];
      const lot = lots.find((l) => l.lotId === lotId);
      if (!lot) {
        setError(t("transfer.lotRequired"));
        return;
      }
      sourceLotId = lot.lotId;
      lotNumber = lot.lotNumber ?? null;
      expirationDate = lot.expirationDate ?? null;
    }

    const key = `${row.productId}:${sourceLotId ?? "none"}`;
    setError(null);
    setLines((prev) => {
      const existingIndex = prev.findIndex((l) => l.key === key);
      if (existingIndex >= 0) {
        const next = [...prev];
        next[existingIndex] = { ...next[existingIndex], quantity: qtyParsed };
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
          sourceLotId,
          lotNumber,
          expirationDate,
        },
      ];
    });
    setQtyByProduct((prev) => ({ ...prev, [row.productId]: "" }));
  }

  function removeLine(key: string) {
    setLines((prev) => prev.filter((l) => l.key !== key));
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

  async function saveDraft() {
    if (!workspace || !boundWorkspace?.branchId || !allowManage || !online || saving) {
      return;
    }
    if (!destinationBranchId) {
      setError(t("transfer.destinationRequired"));
      return;
    }
    if (destinationBranchId === boundWorkspace.branchId) {
      setError(t("transfer.sameBranch"));
      return;
    }
    if (lines.length === 0) {
      setError(t("transfer.draftEmpty"));
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
      setError(
        err instanceof PosApiError
          ? (err.problem.detail ?? t("transfer.saveFailed"))
          : t("transfer.saveFailed"),
      );
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
            {lines.map((line) => (
              <li
                key={line.key}
                className="flex items-center gap-2 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 py-2"
                data-testid={`transfer-line-${line.key}`}
              >
                <div className="min-w-0 flex-1">
                  <p className="m-0 truncate text-[length:var(--exits-text-sm)] font-medium">{line.name}</p>
                  <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                    {line.quantity} {line.unitOfMeasure}
                    {line.lotNumber || line.expirationDate
                      ? ` · ${t("transfer.lot")}: ${line.lotNumber ?? "—"} · ${t("transfer.expiry")}: ${line.expirationDate ?? "—"}`
                      : ""}
                  </p>
                </div>
                <Button
                  type="button"
                  variant="ghost"
                  size="icon"
                  className="shrink-0"
                  aria-label={t("transfer.remove")}
                  onClick={() => removeLine(line.key)}
                  data-testid={`transfer-remove-${line.key}`}
                >
                  <X className="size-4" aria-hidden />
                </Button>
              </li>
            ))}
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
              return (
                <li
                  key={row.productId}
                  className="rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 py-2"
                >
                  <div className="flex min-w-0 items-center gap-2">
                    <div className="min-w-0 flex-1">
                      <p className="m-0 truncate text-[length:var(--exits-text-sm)] font-medium text-foreground">
                        {row.name}
                      </p>
                      <p className="m-0 truncate text-[length:var(--exits-text-xs)] text-muted">
                        {row.unitOfMeasure}
                        {tracksExpiration ? ` · ${t("transfer.tracksExpiry")}` : ""}
                      </p>
                    </div>
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
                      disabled={!online}
                      aria-label={t("transfer.addProduct")}
                      onClick={() => void addLine(row)}
                      data-testid={`transfer-add-${row.productId}`}
                    >
                      <Plus className="size-4" aria-hidden />
                    </Button>
                  </div>
                  {tracksExpiration ? (
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
                        <option key={lot.lotId} value={lot.lotId}>
                          {(lot.lotNumber ?? t("transfer.lot")) +
                            ` · ${lot.expirationDate ?? "—"} · ${lot.quantityOnHand}`}
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
          disabled={!online || saving || lines.length === 0 || !destinationBranchId}
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
