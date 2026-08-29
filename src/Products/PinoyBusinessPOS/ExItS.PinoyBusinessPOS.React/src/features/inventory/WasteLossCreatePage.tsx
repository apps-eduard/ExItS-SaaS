import { useEffect, useMemo, useRef, useState } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { canManageInventory } from "@/access/pos-capabilities";
import { listCatalogProducts, getCatalogProduct } from "@/api/pos/pos-catalog-client";
import type { PosCatalogProductDto } from "@/api/pos/pos-catalog-types";
import {
  getInventoryProduct,
  listInventory,
  listProductLots,
  type PosInventoryAccountDto,
  type PosInventoryLotDto,
} from "@/api/pos/pos-inventory-client";
import { PosApiError } from "@/api/pos/pos-http";
import {
  createWasteLoss,
  WASTE_LOSS_REASONS,
  type WasteLossReasonCode,
} from "@/api/pos/pos-waste-loss-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { StickyActionBar } from "@/components/exits/FoundationStates";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { SearchField } from "@/components/exits/SearchField";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { isLikelyNetworkFailure } from "@/connectivity/network-failure";
import {
  businessUsageLabelKey,
  resolveBusinessUsage,
} from "@/features/catalog/product-business-usage";
import {
  isWasteLossReasonCode,
  parseWasteLossPrefillQuantity,
} from "@/features/inventory/expired-waste-quick-flow";
import { InventoryLotList } from "@/features/inventory/InventoryLotList";
import { resolveLotExpiryLabel } from "@/features/inventory/inventory-lot-status";
import {
  sortLotsForWasteLoss,
  wasteLossReasonLabelKey,
} from "@/features/inventory/waste-loss-labels";
import { useI18n } from "@/i18n/I18nProvider";
import { createSecureMutationId } from "@/lib/secure-mutation-id";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type DraftLine = {
  productId: string;
  name: string;
  uom: string;
  quantity: number;
  available: number;
  tracksExpiration: boolean;
  inventoryLotId: string;
  lots: PosInventoryLotDto[];
};

type PickerRow = {
  productId: string;
  name: string;
  uom: string;
  onHand: number;
  usageLabel: string;
  isTracked: boolean;
  tracksExpiration: boolean;
};

type ExactLotPrefillState =
  | { status: "idle" }
  | { status: "loading" }
  | { status: "ready"; lot: PosInventoryLotDto; productName: string; uom: string }
  | { status: "zero"; productName: string }
  | { status: "missing" };

function formatLotStatus(
  lot: PosInventoryLotDto,
  t: ReturnType<typeof useI18n>["t"],
): string {
  const label = resolveLotExpiryLabel(lot.expiryStatus, lot.expirationDate);
  switch (label.kind) {
    case "expired":
      return t("inventory.statusExpired");
    case "expiresToday":
      return t("inventory.statusExpiresToday");
    case "expiresInDays":
      return t("inventory.statusExpiresInDays").replace("{days}", String(label.days));
    case "ok":
      return t("inventory.statusGood");
    default:
      return label.status;
  }
}

function initialReasonFromParams(
  reasonRaw: string | null,
  source: string | null,
): WasteLossReasonCode {
  if (isWasteLossReasonCode(reasonRaw)) {
    return reasonRaw;
  }
  if (source === "expiration") {
    return "Expired";
  }
  return "Spoiled";
}

export function WasteLossCreatePage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [searchParams] = useSearchParams();
  const preselectProductId = searchParams.get("productId")?.trim() || null;
  const prefillLotId = searchParams.get("lotId")?.trim() || null;
  const prefillSource = searchParams.get("source")?.trim() === "expiration" ? "expiration" : null;
  const reasonParam = searchParams.get("reason")?.trim() || null;
  // Query quantity is UI convenience only — never used as submit authority.
  void parseWasteLossPrefillQuantity(searchParams.get("quantity"));

  const online = useBrowserOnline();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const allowManage = canManageInventory(sessionGrant);
  const fromExpiration = prefillSource === "expiration" && Boolean(prefillLotId);

  const [reason, setReason] = useState<WasteLossReasonCode>(() =>
    initialReasonFromParams(reasonParam, prefillSource),
  );
  const [notes, setNotes] = useState("");
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");
  const [lines, setLines] = useState<DraftLine[]>([]);
  const [qtyByProduct, setQtyByProduct] = useState<Record<string, string>>({});
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [statusLocked, setStatusLocked] = useState(false);
  const [exactLotPrefill, setExactLotPrefill] = useState<ExactLotPrefillState>({
    status: prefillLotId ? "loading" : "idle",
  });
  const [lotNotExpiredNotice, setLotNotExpiredNotice] = useState(false);
  const wasteLossIdRef = useRef<string | null>(null);
  const preselectDoneKeyRef = useRef<string | null>(null);

  const prioritizeExpiredLots = reason === "Expired";

  useEffect(() => {
    const handle = window.setTimeout(() => setDebounced(search.trim()), 250);
    return () => window.clearTimeout(handle);
  }, [search]);

  useEffect(() => {
    if (!prioritizeExpiredLots) {
      return;
    }
    setLines((prev) =>
      prev.map((line) =>
        line.tracksExpiration
          ? { ...line, lots: sortLotsForWasteLoss(line.lots, true) }
          : line,
      ),
    );
  }, [prioritizeExpiredLots]);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const inventoryQuery = useQuery({
    queryKey: [
      "inventory",
      "waste-loss-picker",
      workspace?.organizationId,
      workspace?.branchId,
      debounced,
    ],
    enabled: Boolean(workspace) && online && allowManage,
    queryFn: ({ signal }) =>
      listInventory(workspace!, { search: debounced || undefined, pageSize: 40 }, signal),
  });

  const catalogQuery = useQuery({
    queryKey: ["catalog-products", "waste-loss-picker", workspace?.organizationId, debounced],
    enabled: Boolean(workspace) && online && allowManage,
    queryFn: ({ signal }) =>
      listCatalogProducts(
        workspace!,
        { search: debounced || undefined, status: "Active", pageSize: 40 },
        signal,
      ),
  });

  const pickerRows = useMemo(() => {
    const inventoryById = new Map<string, PosInventoryAccountDto>();
    for (const item of inventoryQuery.data?.items ?? []) {
      inventoryById.set(item.productId, item);
    }

    const catalogById = new Map<string, PosCatalogProductDto>();
    for (const item of catalogQuery.data?.items ?? []) {
      catalogById.set(item.productId, item);
    }

    const ids = new Set<string>([...inventoryById.keys(), ...catalogById.keys()]);
    const rows: PickerRow[] = [];

    for (const productId of ids) {
      const inv = inventoryById.get(productId);
      const cat = catalogById.get(productId);
      const isTracked = inv?.isTracked ?? cat?.isTracked === true;
      if (!isTracked) {
        continue;
      }

      const usage = resolveBusinessUsage(
        cat ?? {
          canBeSold: inv?.productStatus === "Active" ? true : undefined,
        },
      );

      rows.push({
        productId,
        name: cat?.name ?? inv?.name ?? productId,
        uom: cat?.unitOfMeasure ?? inv?.unitOfMeasure ?? "",
        onHand: inv?.onHandQuantity ?? 0,
        usageLabel: t(businessUsageLabelKey(usage)),
        isTracked,
        tracksExpiration: inv?.tracksExpiration === true || cat?.tracksExpiration === true,
      });
    }

    rows.sort((a, b) => a.name.localeCompare(b.name));
    return rows;
  }, [inventoryQuery.data?.items, catalogQuery.data?.items, t]);

  useEffect(() => {
    if (!workspace || !preselectProductId || !allowManage || !online) {
      return;
    }

    const prefillKey = [
      workspace.organizationId,
      workspace.branchId,
      preselectProductId,
      prefillLotId ?? "",
      reasonParam ?? "",
      prefillSource ?? "",
    ].join("|");

    if (preselectDoneKeyRef.current === prefillKey) {
      return;
    }

    let cancelled = false;
    void (async () => {
      if (prefillLotId) {
        setExactLotPrefill({ status: "loading" });
        setLotNotExpiredNotice(false);
        setLines([]);
        setError(null);
        try {
          const [inv, cat, lotsPage] = await Promise.all([
            getInventoryProduct(workspace, preselectProductId),
            getCatalogProduct(workspace, preselectProductId).catch(() => null),
            listProductLots(workspace, preselectProductId, { pageSize: 50 }),
          ]);
          if (cancelled) {
            return;
          }
          if (!inv.isTracked) {
            preselectDoneKeyRef.current = prefillKey;
            setExactLotPrefill({ status: "missing" });
            return;
          }

          const lot = lotsPage.items.find((entry) => entry.lotId === prefillLotId);
          if (!lot || lot.productId !== preselectProductId) {
            preselectDoneKeyRef.current = prefillKey;
            setExactLotPrefill({ status: "missing" });
            return;
          }

          const productName = cat?.name ?? inv.name;
          const uom = cat?.unitOfMeasure ?? inv.unitOfMeasure;

          if (!(lot.quantityOnHand > 0)) {
            preselectDoneKeyRef.current = prefillKey;
            setExactLotPrefill({ status: "zero", productName });
            return;
          }

          const expiryLabel = resolveLotExpiryLabel(lot.expiryStatus, lot.expirationDate);
          if (prefillSource === "expiration" && expiryLabel.kind !== "expired") {
            setLotNotExpiredNotice(true);
          }

          if (isWasteLossReasonCode(reasonParam)) {
            setReason(reasonParam);
          } else if (prefillSource === "expiration") {
            setReason("Expired");
          }

          const qty = lot.quantityOnHand;
          const lots = sortLotsForWasteLoss(lotsPage.items, true);
          setLines([
            {
              productId: inv.productId,
              name: productName,
              uom,
              quantity: qty,
              available: inv.onHandQuantity,
              tracksExpiration: true,
              inventoryLotId: lot.lotId,
              lots,
            },
          ]);
          setQtyByProduct({ [inv.productId]: String(qty) });
          preselectDoneKeyRef.current = prefillKey;
          setExactLotPrefill({ status: "ready", lot, productName, uom });
        } catch {
          if (!cancelled) {
            preselectDoneKeyRef.current = prefillKey;
            setExactLotPrefill({ status: "missing" });
          }
        }
        return;
      }

      try {
        const [inv, cat] = await Promise.all([
          getInventoryProduct(workspace, preselectProductId),
          getCatalogProduct(workspace, preselectProductId).catch(() => null),
        ]);
        if (cancelled || !inv.isTracked) {
          return;
        }
        if (isWasteLossReasonCode(reasonParam)) {
          setReason(reasonParam);
        }
        preselectDoneKeyRef.current = prefillKey;
        await addProductLine({
          productId: inv.productId,
          name: cat?.name ?? inv.name,
          uom: cat?.unitOfMeasure ?? inv.unitOfMeasure,
          onHand: inv.onHandQuantity,
          usageLabel: "",
          isTracked: true,
          tracksExpiration: inv.tracksExpiration === true || cat?.tracksExpiration === true,
        });
      } catch {
        // Product-only preselect is best-effort; keep form usable.
      }
    })();

    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps -- prefill once per workspace/params
  }, [
    workspace,
    preselectProductId,
    prefillLotId,
    reasonParam,
    prefillSource,
    allowManage,
    online,
  ]);

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  async function loadLots(productId: string): Promise<PosInventoryLotDto[]> {
    const result = await listProductLots(workspace!, productId, { pageSize: 50 });
    return sortLotsForWasteLoss(result.items, prioritizeExpiredLots);
  }

  async function addProductLine(row: PickerRow) {
    let lots: PosInventoryLotDto[] = [];
    if (row.tracksExpiration) {
      lots = await loadLots(row.productId);
    }
    setLines((prev) => {
      const without = prev.filter((line) => line.productId !== row.productId);
      const existing = prev.find((line) => line.productId === row.productId);
      const quantity = existing?.quantity ?? 1;
      return [
        ...without,
        {
          productId: row.productId,
          name: row.name,
          uom: row.uom,
          quantity,
          available: row.onHand,
          tracksExpiration: row.tracksExpiration,
          inventoryLotId: existing?.inventoryLotId ?? "",
          lots,
        },
      ];
    });
  }

  function upsertLine(row: PickerRow, quantity: number) {
    setLines((prev) => {
      const existing = prev.find((line) => line.productId === row.productId);
      const without = prev.filter((line) => line.productId !== row.productId);
      return [
        ...without,
        {
          productId: row.productId,
          name: row.name,
          uom: row.uom,
          quantity,
          available: row.onHand,
          tracksExpiration: row.tracksExpiration,
          inventoryLotId: existing?.inventoryLotId ?? "",
          lots: existing?.lots ?? [],
        },
      ];
    });
  }

  async function addOrUpdateFromPicker(row: PickerRow) {
    setError(null);
    const raw = qtyByProduct[row.productId] ?? "1";
    const qty = Number(raw);
    if (!Number.isFinite(qty) || qty <= 0) {
      setError(t("wasteLoss.invalidQuantity"));
      return;
    }
    if (qty > row.onHand) {
      setError(
        t("wasteLoss.onlyAvailable").replace("{quantity}", `${row.onHand} ${row.uom}`.trim()),
      );
      return;
    }
    const existing = lines.find((line) => line.productId === row.productId);
    const nextQty = existing ? existing.quantity + qty : qty;
    if (nextQty > row.onHand) {
      setError(
        t("wasteLoss.onlyAvailable").replace("{quantity}", `${row.onHand} ${row.uom}`.trim()),
      );
      return;
    }
    if (!existing) {
      await addProductLine(row);
      setLines((prev) =>
        prev.map((line) =>
          line.productId === row.productId ? { ...line, quantity: qty } : line,
        ),
      );
    } else {
      upsertLine(row, nextQty);
    }
    setQtyByProduct((prev) => ({ ...prev, [row.productId]: "1" }));
  }

  function updateLineQty(productId: string, raw: string) {
    setQtyByProduct((prev) => ({ ...prev, [productId]: raw }));
    const qty = Number(raw);
    if (!Number.isFinite(qty) || qty <= 0) {
      return;
    }
    setLines((prev) =>
      prev.map((line) => (line.productId === productId ? { ...line, quantity: qty } : line)),
    );
  }

  function updateLineLot(productId: string, inventoryLotId: string) {
    setLines((prev) =>
      prev.map((line) =>
        line.productId === productId ? { ...line, inventoryLotId } : line,
      ),
    );
  }

  function removeLine(productId: string) {
    setLines((prev) => prev.filter((line) => line.productId !== productId));
  }

  function validateLines(): boolean {
    if (reason === "Other" && !notes.trim()) {
      setError(t("wasteLoss.notesRequired"));
      return false;
    }
    for (const line of lines) {
      if (line.quantity <= 0) {
        setError(t("wasteLoss.invalidQuantity"));
        return false;
      }
      if (line.quantity > line.available) {
        setError(
          t("wasteLoss.onlyAvailable").replace(
            "{quantity}",
            `${line.available} ${line.uom}`.trim(),
          ),
        );
        return false;
      }
      if (line.tracksExpiration && !line.inventoryLotId) {
        setError(t("wasteLoss.lotRequired"));
        return false;
      }
      if (line.tracksExpiration && line.inventoryLotId) {
        const lot = line.lots.find((entry) => entry.lotId === line.inventoryLotId);
        if (lot && line.quantity > lot.quantityOnHand) {
          setError(
            t("wasteLoss.lotQuantityExceeded").replace(
              "{quantity}",
              `${lot.quantityOnHand} ${line.uom}`.trim(),
            ),
          );
          return false;
        }
      }
    }
    return true;
  }

  async function submit() {
    if (!workspace || !allowManage || !online || saving || statusLocked || lines.length === 0) {
      return;
    }
    if (exactLotPrefill.status === "zero" || exactLotPrefill.status === "missing") {
      return;
    }
    if (!validateLines()) {
      return;
    }

    if (!wasteLossIdRef.current) {
      const generated = createSecureMutationId();
      if (!generated.ok) {
        setError(t("wasteLoss.saveFailed"));
        return;
      }
      wasteLossIdRef.current = generated.id;
    }
    const wasteLossId = wasteLossIdRef.current;
    setSaving(true);
    setError(null);

    const payload = {
      reason,
      notes: notes.trim() || null,
      wasteLossId,
      lines: lines.map((line) => ({
        productId: line.productId,
        quantity: line.quantity,
        inventoryLotId: line.tracksExpiration ? line.inventoryLotId : null,
      })),
    };

    try {
      const created = await createWasteLoss(workspace, payload);
      wasteLossIdRef.current = null;
      await queryClient.invalidateQueries({ queryKey: ["inventory"] });
      await queryClient.invalidateQueries({ queryKey: ["waste-loss"] });
      navigate(`/inventory/waste-loss/${created.wasteLossId}`, { replace: true });
    } catch (err) {
      if (isLikelyNetworkFailure(err)) {
        setError(t("checkout.confirmingTransaction"));
        try {
          const created = await createWasteLoss(workspace, payload);
          wasteLossIdRef.current = null;
          await queryClient.invalidateQueries({ queryKey: ["inventory"] });
          await queryClient.invalidateQueries({ queryKey: ["waste-loss"] });
          navigate(`/inventory/waste-loss/${created.wasteLossId}`, { replace: true });
          return;
        } catch (retryErr) {
          if (isLikelyNetworkFailure(retryErr)) {
            setStatusLocked(true);
            setError(t("checkout.transactionStatusUnknown"));
            return;
          }
          setError(
            retryErr instanceof PosApiError
              ? (retryErr.problem.detail ?? t("wasteLoss.saveFailed"))
              : t("wasteLoss.saveFailed"),
          );
          return;
        }
      }
      setError(
        err instanceof PosApiError
          ? (err.problem.detail ?? t("wasteLoss.saveFailed"))
          : t("wasteLoss.saveFailed"),
      );
    } finally {
      setSaving(false);
    }
  }

  const backTo = fromExpiration ? "/inventory/expiration" : "/inventory/waste-loss";
  const backLabel = fromExpiration ? t("wasteLoss.backToExpiration") : t("wasteLoss.backList");
  const pageTitle = fromExpiration ? t("wasteLoss.recordExpiredTitle") : t("wasteLoss.recordTitle");
  const blockSubmit =
    exactLotPrefill.status === "loading" ||
    exactLotPrefill.status === "zero" ||
    exactLotPrefill.status === "missing";

  return (
    <div
      className="waste-loss-create-page exits-page flex min-w-0 flex-col gap-3 pb-4"
      data-testid="waste-loss-create-page"
      data-quick-flow-source={prefillSource ?? undefined}
    >
      <PageHeader
        title={pageTitle}
        description={t("wasteLoss.notASale")}
        backTo={backTo}
        backLabel={backLabel}
        backTestId={fromExpiration ? "page-header-back-expiration" : "page-header-back-waste-loss"}
      />

      {!online ? (
        <Card>
          <p className="m-0">{t("wasteLoss.offline")}</p>
        </Card>
      ) : null}
      {!allowManage ? (
        <Card>
          <p className="m-0">{t("wasteLoss.manageDenied")}</p>
        </Card>
      ) : null}

      {exactLotPrefill.status === "loading" ? (
        <LoadingState label={t("wasteLoss.loading")} />
      ) : null}

      {exactLotPrefill.status === "missing" ? (
        <Card data-testid="waste-loss-lot-unavailable">
          <p className="m-0 font-medium">{t("wasteLoss.lotNoLongerAvailable")}</p>
          <div className="mt-3 flex flex-wrap gap-2">
            <Button asChild variant="outline" className="min-h-11" data-testid="waste-loss-back-expiration">
              <Link to="/inventory/expiration">{t("wasteLoss.backToExpiration")}</Link>
            </Button>
            <Button asChild className="min-h-11" data-testid="waste-loss-start-fresh">
              <Link to="/inventory/waste-loss/new">{t("wasteLoss.recordWasteLoss")}</Link>
            </Button>
          </div>
        </Card>
      ) : null}

      {exactLotPrefill.status === "zero" ? (
        <Card data-testid="waste-loss-lot-zero">
          <p className="m-0 font-medium">{t("wasteLoss.noStockRemainsInLot")}</p>
          <div className="mt-3 flex flex-wrap gap-2">
            <Button asChild variant="outline" className="min-h-11" data-testid="waste-loss-back-expiration">
              <Link to="/inventory/expiration">{t("wasteLoss.backToExpiration")}</Link>
            </Button>
          </div>
        </Card>
      ) : null}

      {exactLotPrefill.status === "ready" ? (
        <Card data-testid="waste-loss-expired-context">
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("wasteLoss.confirmPhysicalQuantity")}
          </p>
          <p className="m-0 mt-2 font-semibold text-foreground">{exactLotPrefill.productName}</p>
          <p className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
            {t("wasteLoss.lotLabel")}: {exactLotPrefill.lot.lotNumber ?? exactLotPrefill.lot.lotId}
          </p>
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("wasteLoss.expiredOn")}: {exactLotPrefill.lot.expirationDate}
          </p>
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="waste-loss-lot-available">
            {t("wasteLoss.availableInLot")}: {exactLotPrefill.lot.quantityOnHand}{" "}
            {exactLotPrefill.uom}
          </p>
          {lotNotExpiredNotice ? (
            <p
              className="m-0 mt-2 text-[length:var(--exits-text-sm)] text-[var(--exits-warning,var(--exits-danger))]"
              data-testid="waste-loss-lot-not-expired-notice"
            >
              {t("wasteLoss.lotNoLongerExpired")}
            </p>
          ) : null}
        </Card>
      ) : null}

      {error ? <ErrorState title={t("wasteLoss.errorTitle")} detail={error} /> : null}

      {!blockSubmit ? (
        <>
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("wasteLoss.reason")}
            <select
              className="min-h-11 rounded-md border border-border bg-background px-3"
              value={reason}
              onChange={(e) => setReason(e.target.value as WasteLossReasonCode)}
              disabled={!allowManage || statusLocked}
              data-testid="waste-loss-reason"
            >
              {WASTE_LOSS_REASONS.map((code) => (
                <option key={code} value={code}>
                  {t(wasteLossReasonLabelKey(code))}
                </option>
              ))}
            </select>
          </label>

          {reason === "Expired" ? (
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {fromExpiration
                ? t("wasteLoss.expiredWriteOffHint")
                : t("wasteLoss.expiredLotsFirst")}
            </p>
          ) : null}

          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("wasteLoss.notes")}
            <textarea
              className="min-h-20 rounded-md border border-border bg-background px-3 py-2"
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              disabled={!allowManage || statusLocked}
              placeholder={
                reason === "Other"
                  ? t("wasteLoss.notesRequiredPlaceholder")
                  : t("wasteLoss.notesOptional")
              }
              data-testid="waste-loss-notes"
            />
          </label>

          <section className="flex flex-col gap-2" data-testid="waste-loss-draft-lines">
            <h2 className="m-0 text-[length:var(--exits-text-md)] font-medium">
              {t("wasteLoss.wastedStock")}
            </h2>
            {lines.length === 0 ? (
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                {t("wasteLoss.draftEmpty")}
              </p>
            ) : (
              <ul className="m-0 flex list-none flex-col gap-2 p-0">
                {lines.map((line) => (
                  <li key={line.productId}>
                    <Card className="flex flex-col gap-2 p-3">
                      <div className="font-medium">{line.name}</div>
                      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                        {t("wasteLoss.available")}: {line.available} {line.uom}
                      </p>
                      <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                        {t("wasteLoss.quantityWasted")}
                        <input
                          type="number"
                          min={0}
                          step="any"
                          className="min-h-11 rounded-md border border-border bg-background px-3"
                          value={qtyByProduct[line.productId] ?? String(line.quantity)}
                          onChange={(e) => updateLineQty(line.productId, e.target.value)}
                          disabled={statusLocked}
                          data-testid={`waste-loss-line-qty-${line.productId}`}
                        />
                      </label>
                      {line.tracksExpiration ? (
                        <div
                          className="flex flex-col gap-2"
                          data-testid={`waste-loss-lots-${line.productId}`}
                        >
                          <p className="m-0 text-[length:var(--exits-text-sm)] font-medium">
                            {t("wasteLoss.selectLot")}
                          </p>
                          {line.lots.length === 0 ? (
                            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                              {t("inventory.lotsEmpty")}
                            </p>
                          ) : (
                            <InventoryLotList
                              lots={line.lots}
                              unitOfMeasure={line.uom}
                              formatStatus={(lot) => formatLotStatus(lot, t)}
                              selectable
                              selectedLotId={line.inventoryLotId}
                              onSelectLot={(lotId) => updateLineLot(line.productId, lotId)}
                              namePrefix={`waste-loss-lot-${line.productId}`}
                            />
                          )}
                        </div>
                      ) : null}
                      {!fromExpiration ? (
                        <Button
                          type="button"
                          variant="ghost"
                          className="min-h-11 w-fit"
                          onClick={() => removeLine(line.productId)}
                          disabled={statusLocked}
                        >
                          {t("wasteLoss.removeLine")}
                        </Button>
                      ) : null}
                    </Card>
                  </li>
                ))}
              </ul>
            )}
          </section>

          {!fromExpiration ? (
            <section className="flex flex-col gap-2">
              <h2 className="m-0 text-[length:var(--exits-text-md)] font-medium">
                {t("wasteLoss.addProduct")}
              </h2>
              <SearchField
                label={t("wasteLoss.searchProducts")}
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                onClear={() => setSearch("")}
                placeholder={t("wasteLoss.searchProducts")}
                data-testid="waste-loss-product-search"
              />
              {pickerRows.length === 0 && (debounced || inventoryQuery.isSuccess) ? (
                <EmptyState
                  title={t("wasteLoss.noProducts")}
                  detail={t("wasteLoss.noProductsDetail")}
                />
              ) : null}
              <ul
                className="m-0 flex list-none flex-col gap-2 p-0"
                data-testid="waste-loss-product-picker"
              >
                {pickerRows.map((row) => (
                  <li key={row.productId}>
                    <Card className="flex flex-col gap-2 p-3">
                      <div className="min-w-0">
                        <div className="font-medium">{row.name}</div>
                        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                          {row.usageLabel} · {t("wasteLoss.available")}: {row.onHand} {row.uom}
                        </p>
                      </div>
                      <div className="flex flex-wrap items-end gap-2">
                        <label className="flex min-w-[5.5rem] flex-1 flex-col gap-1 text-[length:var(--exits-text-sm)]">
                          {t("wasteLoss.quantityWasted")}
                          <input
                            type="number"
                            min={0}
                            step="any"
                            className="min-h-11 rounded-md border border-border bg-background px-3"
                            value={qtyByProduct[row.productId] ?? "1"}
                            onChange={(e) =>
                              setQtyByProduct((prev) => ({
                                ...prev,
                                [row.productId]: e.target.value,
                              }))
                            }
                            disabled={statusLocked || !allowManage}
                            data-testid={`waste-loss-picker-qty-${row.productId}`}
                          />
                        </label>
                        <Button
                          type="button"
                          className="min-h-11"
                          disabled={!allowManage || !online || statusLocked || row.onHand <= 0}
                          onClick={() => void addOrUpdateFromPicker(row)}
                          data-testid={`waste-loss-add-${row.productId}`}
                        >
                          {t("wasteLoss.addProduct")}
                        </Button>
                      </div>
                    </Card>
                  </li>
                ))}
              </ul>
            </section>
          ) : null}

          <StickyActionBar>
            <Button
              type="button"
              className="min-h-11 w-full"
              disabled={
                !allowManage ||
                !online ||
                saving ||
                statusLocked ||
                lines.length === 0 ||
                blockSubmit
              }
              onClick={() => void submit()}
              data-testid="waste-loss-submit"
            >
              {saving ? t("wasteLoss.recording") : t("wasteLoss.recordWasteLoss")}
            </Button>
          </StickyActionBar>
        </>
      ) : null}
    </div>
  );
}
