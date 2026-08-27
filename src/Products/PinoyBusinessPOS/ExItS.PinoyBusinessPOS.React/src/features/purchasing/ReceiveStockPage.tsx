import { useEffect, useMemo, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { canManageInventory } from "@/access/pos-capabilities";
import { listCatalogProducts } from "@/api/pos/pos-catalog-client";
import type { PosCatalogProductDto } from "@/api/pos/pos-catalog-types";
import { createDirectPurchaseReceipt } from "@/api/pos/pos-direct-purchase-receipts-client";
import { PosApiError } from "@/api/pos/pos-http";
import { listSuppliers } from "@/api/pos/pos-suppliers-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { pageBackNav } from "@/navigation/page-back-nav";
import { SearchField } from "@/components/exits/SearchField";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { isLikelyNetworkFailure } from "@/connectivity/network-failure";
import { useI18n } from "@/i18n/I18nProvider";
import { createSecureMutationId } from "@/lib/secure-mutation-id";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type DraftLine = {
  productId: string;
  name: string;
  uom: string;
  tracksExpiration: boolean;
  quantity: number;
  unitCost: number;
  expiryDate: string | null;
  lotNumber: string | null;
};

function todayIsoDate(): string {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
}

export function ReceiveStockPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const online = useBrowserOnline();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const allowManage = canManageInventory(sessionGrant);

  const [purchaseDate, setPurchaseDate] = useState(todayIsoDate);
  const [supplierId, setSupplierId] = useState("");
  const [sourceName, setSourceName] = useState("");
  const [referenceNumber, setReferenceNumber] = useState("");
  const [notes, setNotes] = useState("");
  const [search, setSearch] = useState("");
  const [debounced, setDebounced] = useState("");
  const [lines, setLines] = useState<DraftLine[]>([]);
  const [sheetProduct, setSheetProduct] = useState<PosCatalogProductDto | null>(null);
  const [qtyText, setQtyText] = useState("1");
  const [costText, setCostText] = useState("");
  const [expiry, setExpiry] = useState("");
  const [lot, setLot] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [reviewing, setReviewing] = useState(false);
  const [statusLocked, setStatusLocked] = useState(false);
  const idempotencyKeyRef = useRef<string | null>(null);

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

  const suppliersQuery = useQuery({
    queryKey: ["suppliers", "direct-buy", workspace?.organizationId],
    enabled: Boolean(workspace) && online && allowManage,
    queryFn: ({ signal }) => listSuppliers(workspace!, { status: "Active", pageSize: 100 }, signal),
  });

  const productsQuery = useQuery({
    queryKey: ["catalog-products", "direct-buy", workspace?.organizationId, debounced],
    enabled: Boolean(workspace) && online && allowManage && debounced.length > 0,
    queryFn: ({ signal }) =>
      listCatalogProducts(
        workspace!,
        { search: debounced, status: "Active", pageSize: 20 },
        signal,
      ),
  });

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  function beginAdd(product: PosCatalogProductDto) {
    if (product.isTracked === false) {
      setError(t("purchasing.receiveStockNotTracked"));
      return;
    }
    setError(null);
    setSheetProduct(product);
    const existing = lines.find((l) => l.productId === product.productId);
    setQtyText(existing ? String(existing.quantity) : "1");
    setCostText(existing ? String(existing.unitCost) : "");
    setExpiry(existing?.expiryDate ?? "");
    setLot(existing?.lotNumber ?? "");
  }

  function saveSheet() {
    if (!sheetProduct) {
      return;
    }
    const qty = Number(qtyText);
    const cost = Number(costText);
    if (!Number.isFinite(qty) || qty <= 0 || !Number.isFinite(cost) || cost < 0) {
      setError(t("purchasing.invalidLine"));
      return;
    }
    const tracksExpiration = sheetProduct.tracksExpiration === true;
    if (tracksExpiration && !expiry.trim()) {
      setError(t("purchasing.expiryRequired"));
      return;
    }
    const draft: DraftLine = {
      productId: sheetProduct.productId,
      name: sheetProduct.name,
      uom: sheetProduct.unitOfMeasure,
      tracksExpiration,
      quantity: qty,
      unitCost: cost,
      expiryDate: tracksExpiration ? expiry.trim() : null,
      lotNumber: tracksExpiration && lot.trim() ? lot.trim() : null,
    };
    setLines((prev) => {
      const without = prev.filter((l) => l.productId !== draft.productId);
      return [...without, draft];
    });
    setSheetProduct(null);
    setError(null);
  }

  async function confirm() {
    if (!workspace || !allowManage || !online || saving || statusLocked || lines.length === 0) {
      return;
    }
    if (!idempotencyKeyRef.current) {
      const generated = createSecureMutationId();
      if (!generated.ok) {
        setError(t("purchasing.directSaveFailed"));
        return;
      }
      idempotencyKeyRef.current = generated.id;
    }
    const idempotencyKey = idempotencyKeyRef.current;
    setSaving(true);
    setError(null);
    try {
      const receipt = await createDirectPurchaseReceipt(workspace, {
        purchaseDate,
        supplierId: supplierId || null,
        sourceName: sourceName.trim() || null,
        referenceNumber: referenceNumber.trim() || null,
        notes: notes.trim() || null,
        idempotencyKey,
        lines: lines.map((line) => ({
          productId: line.productId,
          quantity: line.quantity,
          unitCost: line.unitCost,
          expiryDate: line.expiryDate,
          lotNumber: line.lotNumber,
        })),
      });
      idempotencyKeyRef.current = null;
      navigate(`/purchasing/direct-purchases/${receipt.directPurchaseReceiptId}`, {
        replace: true,
      });
    } catch (err) {
      // No GET-by-idempotency-key API. Sticky key makes a same-payload retry safe;
      // if transport is still down, lock the form instead of inviting a new key.
      if (isLikelyNetworkFailure(err)) {
        setError(t("checkout.confirmingTransaction"));
        try {
          const receipt = await createDirectPurchaseReceipt(workspace, {
            purchaseDate,
            supplierId: supplierId || null,
            sourceName: sourceName.trim() || null,
            referenceNumber: referenceNumber.trim() || null,
            notes: notes.trim() || null,
            idempotencyKey,
            lines: lines.map((line) => ({
              productId: line.productId,
              quantity: line.quantity,
              unitCost: line.unitCost,
              expiryDate: line.expiryDate,
              lotNumber: line.lotNumber,
            })),
          });
          idempotencyKeyRef.current = null;
          navigate(`/purchasing/direct-purchases/${receipt.directPurchaseReceiptId}`, {
            replace: true,
          });
          return;
        } catch (retryErr) {
          if (isLikelyNetworkFailure(retryErr)) {
            setStatusLocked(true);
            setError(t("checkout.transactionStatusUnknown"));
            return;
          }
          setError(
            retryErr instanceof PosApiError
              ? (retryErr.problem.detail ?? t("purchasing.directSaveFailed"))
              : t("purchasing.directSaveFailed"),
          );
          return;
        }
      }
      setError(
        err instanceof PosApiError
          ? (err.problem.detail ?? t("purchasing.directSaveFailed"))
          : t("purchasing.directSaveFailed"),
      );
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="receive-stock-page">
      <PageHeader
        title={t("purchasing.receiveStock")}
        description={t("purchasing.receiveStockLede")}
        backTo={pageBackNav.purchasing.to}
        backLabel={t(pageBackNav.purchasing.labelKey)}
        backTestId="page-header-back-purchasing"
      />
      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
        {t("purchasing.receiveStockHelper")}
      </p>
      {!online ? (
        <Card>
          <p className="m-0">{t("purchasing.offline")}</p>
        </Card>
      ) : null}
      {!allowManage ? (
        <Card>
          <p className="m-0">{t("purchasing.inventoryManageDenied")}</p>
        </Card>
      ) : null}

      {!reviewing ? (
        <>
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("purchasing.purchaseDate")}
            <input
              type="date"
              className="min-h-11 rounded-md border border-border bg-background px-3"
              value={purchaseDate}
              onChange={(e) => setPurchaseDate(e.target.value)}
              data-testid="direct-purchase-date"
            />
          </label>
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("purchasing.boughtFrom")}
            <select
              className="min-h-11 rounded-md border border-border bg-background px-3"
              value={supplierId}
              onChange={(e) => {
                setSupplierId(e.target.value);
                const match = suppliersQuery.data?.items.find(
                  (s) => s.supplierId === e.target.value,
                );
                if (match) {
                  setSourceName(match.name);
                }
              }}
              data-testid="direct-supplier"
            >
              <option value="">{t("purchasing.useAnotherSource")}</option>
              {(suppliersQuery.data?.items ?? []).map((s) => (
                <option key={s.supplierId} value={s.supplierId}>
                  {s.name}
                </option>
              ))}
            </select>
          </label>
          {!supplierId ? (
            <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
              {t("purchasing.sourceName")}
              <input
                className="min-h-11 rounded-md border border-border bg-background px-3"
                value={sourceName}
                onChange={(e) => setSourceName(e.target.value)}
                placeholder={t("purchasing.sourcePlaceholder")}
                data-testid="direct-source-name"
              />
            </label>
          ) : null}
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("purchasing.reference")}
            <input
              className="min-h-11 rounded-md border border-border bg-background px-3"
              value={referenceNumber}
              onChange={(e) => setReferenceNumber(e.target.value)}
            />
          </label>
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("purchasing.notes")}
            <textarea
              className="min-h-20 rounded-md border border-border bg-background px-3 py-2"
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
            />
          </label>

          <SearchField
            label={t("purchasing.productSearch")}
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            onClear={() => setSearch("")}
            placeholder={t("purchasing.productSearch")}
            data-testid="direct-product-search"
          />
          {(productsQuery.data?.items ?? []).length === 0 && debounced ? (
            <EmptyState
              title={t("purchasing.noProducts")}
              detail={t("purchasing.noProductsDetail")}
            />
          ) : null}
          <ul className="m-0 flex list-none flex-col gap-1 p-0">
            {(productsQuery.data?.items ?? []).map((p) => (
              <li key={p.productId}>
                <button
                  type="button"
                  className="min-h-11 w-full rounded-md border border-border bg-background px-3 text-left"
                  onClick={() => beginAdd(p)}
                  data-testid={`direct-product-${p.productId}`}
                >
                  {p.name}
                </button>
              </li>
            ))}
          </ul>

          {sheetProduct ? (
            <Card data-testid="direct-add-sheet">
              <p className="mt-0 font-medium">{sheetProduct.name}</p>
              <div className="grid gap-2 sm:grid-cols-2">
                <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                  {t("purchasing.qty")}
                  <input
                    className="min-h-11 rounded-md border border-border bg-background px-3"
                    value={qtyText}
                    onChange={(e) => setQtyText(e.target.value)}
                    data-testid="direct-line-qty"
                  />
                </label>
                <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                  {t("purchasing.unitCost")}
                  <input
                    className="min-h-11 rounded-md border border-border bg-background px-3"
                    value={costText}
                    onChange={(e) => setCostText(e.target.value)}
                    data-testid="direct-line-cost"
                  />
                </label>
                {sheetProduct.tracksExpiration ? (
                  <>
                    <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                      {t("purchasing.expiryDate")}
                      <input
                        type="date"
                        className="min-h-11 rounded-md border border-border bg-background px-3"
                        value={expiry}
                        onChange={(e) => setExpiry(e.target.value)}
                        data-testid="direct-line-expiry"
                      />
                    </label>
                    <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                      {t("purchasing.lotNumber")}
                      <input
                        className="min-h-11 rounded-md border border-border bg-background px-3"
                        value={lot}
                        onChange={(e) => setLot(e.target.value)}
                        data-testid="direct-line-lot"
                      />
                    </label>
                  </>
                ) : null}
              </div>
              <div className="mt-3 flex flex-wrap gap-2">
                <Button
                  type="button"
                  className="min-h-11"
                  onClick={saveSheet}
                  data-testid="direct-save-line"
                >
                  {t("purchasing.addLine")}
                </Button>
                <Button
                  type="button"
                  variant="ghost"
                  className="min-h-11"
                  onClick={() => setSheetProduct(null)}
                >
                  {t("purchasing.cancel")}
                </Button>
              </div>
            </Card>
          ) : null}

          <section>
            <h2 className="m-0 mb-2 text-[length:var(--exits-text-md)] font-medium">
              {t("purchasing.draft")}
            </h2>
            {lines.length === 0 ? (
              <p className="m-0 text-muted">{t("purchasing.draftEmpty")}</p>
            ) : (
              <ul className="m-0 flex list-none flex-col gap-2 p-0">
                {lines.map((line) => (
                  <li key={line.productId} className="rounded-md border border-border p-3">
                    <div className="font-medium">{line.name}</div>
                    <div className="text-[length:var(--exits-text-sm)] text-muted">
                      {line.quantity} {line.uom} · {line.unitCost}
                      {line.expiryDate ? ` · ${line.expiryDate}` : ""}
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </section>

          <Button
            type="button"
            className="min-h-11"
            disabled={lines.length === 0 || !allowManage || !online}
            onClick={() => setReviewing(true)}
            data-testid="direct-review"
          >
            {t("purchasing.reviewDirect")}
          </Button>
        </>
      ) : (
        <Card data-testid="direct-review-sheet">
          <p className="mt-0">{t("purchasing.willIncreaseStock")}</p>
          <ul className="m-0 flex list-none flex-col gap-2 p-0">
            {lines.map((line) => (
              <li key={line.productId}>
                {line.name}: {line.quantity} @ {line.unitCost}
                {line.expiryDate ? ` · exp ${line.expiryDate}` : ""}
              </li>
            ))}
          </ul>
          <div className="mt-3 flex flex-wrap gap-2">
            <Button
              type="button"
              variant="ghost"
              className="min-h-11"
              onClick={() => setReviewing(false)}
            >
              {t("purchasing.backToReceipt")}
            </Button>
            <Button
              type="button"
              className="min-h-11"
              disabled={saving || statusLocked}
              onClick={() => void confirm()}
              data-testid="direct-confirm"
            >
              {saving ? t("purchasing.saving") : t("purchasing.confirmDirect")}
            </Button>
          </div>
        </Card>
      )}

      {error ? (
        <Card data-testid="direct-error">
          <p className="m-0 text-destructive">{error}</p>
        </Card>
      ) : null}
    </div>
  );
}
