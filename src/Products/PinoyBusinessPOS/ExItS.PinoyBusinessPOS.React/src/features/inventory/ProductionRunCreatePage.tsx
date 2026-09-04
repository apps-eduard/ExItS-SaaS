import { useEffect, useMemo, useRef, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { canManageInventory } from "@/access/pos-capabilities";
import { getCatalogProduct } from "@/api/pos/pos-catalog-client";
import { getInventoryProduct } from "@/api/pos/pos-inventory-client";
import { PosApiError } from "@/api/pos/pos-http";
import {
  createProductionRun,
  getProductionDefinition,
  listProductionDefinitions,
  type ProductionDefinitionDto,
} from "@/api/pos/pos-production-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { StickyActionBar } from "@/components/exits/FoundationStates";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { isLikelyNetworkFailure } from "@/connectivity/network-failure";
import {
  productionScaleFactor,
  scaleProductionQuantity,
} from "@/features/inventory/production-labels";
import { useI18n } from "@/i18n/I18nProvider";
import { createSecureMutationId } from "@/lib/secure-mutation-id";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type MaterialPreview = {
  materialProductId: string;
  name: string;
  uom: string;
  expected: number;
  actual: number;
  available: number | null;
};

export function ProductionRunCreatePage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const preselectDefinitionId = searchParams.get("definitionId")?.trim() || null;
  const online = useBrowserOnline();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const allowManage = canManageInventory(sessionGrant);

  const [definitionId, setDefinitionId] = useState<string | null>(preselectDefinitionId);
  const [outputQuantity, setOutputQuantity] = useState("");
  const [actualByProduct, setActualByProduct] = useState<Record<string, string>>({});
  const [notes, setNotes] = useState("");
  const [referenceNumber, setReferenceNumber] = useState("");
  const [expirationDate, setExpirationDate] = useState("");
  const [lotNumber, setLotNumber] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [statusLocked, setStatusLocked] = useState(false);
  const runIdRef = useRef<string | null>(null);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const definitionsQuery = useQuery({
    queryKey: ["production-definitions", "active", workspace?.organizationId],
    enabled: Boolean(workspace) && online && allowManage,
    queryFn: ({ signal }) =>
      listProductionDefinitions(
        workspace!,
        { page: 1, pageSize: 50, isActive: true },
        signal,
      ),
  });

  const definitionQuery = useQuery({
    queryKey: ["production-definition", workspace?.organizationId, definitionId],
    enabled: Boolean(workspace) && Boolean(definitionId) && online,
    queryFn: ({ signal }) => getProductionDefinition(workspace!, definitionId!, signal),
  });

  const definition = definitionQuery.data ?? null;

  useEffect(() => {
    if (!definition) {
      return;
    }
    setOutputQuantity(String(definition.outputQuantityEntered));
    const next: Record<string, string> = {};
    for (const component of definition.components) {
      next[component.materialProductId] = String(component.quantityEntered);
    }
    setActualByProduct(next);
  }, [definition?.productionDefinitionId]);

  const outputProductQuery = useQuery({
    queryKey: ["catalog-product", workspace?.organizationId, definition?.outputProductId],
    enabled: Boolean(workspace) && Boolean(definition?.outputProductId) && online,
    queryFn: ({ signal }) => getCatalogProduct(workspace!, definition!.outputProductId, signal),
  });

  const materialPreviewQuery = useQuery({
    queryKey: [
      "production-run-preview",
      workspace?.organizationId,
      definition?.productionDefinitionId,
      outputQuantity,
    ],
    enabled: Boolean(workspace) && Boolean(definition) && online,
    queryFn: async ({ signal }) => {
      const def = definition!;
      const outQty = Number(outputQuantity);
      const scale = productionScaleFactor(def.outputQuantityEntered, outQty);
      if (scale == null) {
        return [] as Array<{
          materialProductId: string;
          name: string;
          uom: string;
          expected: number;
          available: number | null;
        }>;
      }
      const rows: Array<{
        materialProductId: string;
        name: string;
        uom: string;
        expected: number;
        available: number | null;
      }> = [];
      for (const component of def.components) {
        let name = component.materialProductId;
        let uom = "";
        let available: number | null = null;
        try {
          const [product, inventory] = await Promise.all([
            getCatalogProduct(workspace!, component.materialProductId, signal),
            getInventoryProduct(workspace!, component.materialProductId, signal).catch(() => null),
          ]);
          name = product.name;
          uom = product.unitOfMeasure;
          available = inventory?.onHandQuantity ?? null;
        } catch {
          // keep id fallback
        }
        rows.push({
          materialProductId: component.materialProductId,
          name,
          uom,
          expected: scaleProductionQuantity(component.quantityEntered, scale),
          available,
        });
      }
      return rows;
    },
  });

  const materials: MaterialPreview[] = useMemo(() => {
    return (materialPreviewQuery.data ?? []).map((row) => {
      const actualRaw = actualByProduct[row.materialProductId];
      const actualParsed = actualRaw != null ? Number(actualRaw) : row.expected;
      return {
        ...row,
        actual: Number.isFinite(actualParsed) ? actualParsed : row.expected,
      };
    });
  }, [materialPreviewQuery.data, actualByProduct]);
  const tracksExpiration = outputProductQuery.data?.tracksExpiration === true;

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  function onSelectDefinition(id: string) {
    setDefinitionId(id);
    setError(null);
    setStatusLocked(false);
    runIdRef.current = null;
  }

  function syncActualsToExpected(def: ProductionDefinitionDto, outQty: number) {
    const scale = productionScaleFactor(def.outputQuantityEntered, outQty);
    if (scale == null) {
      return;
    }
    const next: Record<string, string> = {};
    for (const component of def.components) {
      next[component.materialProductId] = String(
        scaleProductionQuantity(component.quantityEntered, scale),
      );
    }
    setActualByProduct(next);
  }

  async function submit() {
    if (!workspace || !allowManage || !online || saving || statusLocked || !definitionId || !definition) {
      return;
    }
    const outQty = Number(outputQuantity);
    if (!Number.isFinite(outQty) || outQty <= 0) {
      setError(t("production.produce.invalidQuantity"));
      return;
    }
    if (tracksExpiration && !expirationDate.trim()) {
      setError(t("production.produce.expirationRequired"));
      return;
    }
    for (const row of materials) {
      if (!Number.isFinite(row.actual) || row.actual <= 0) {
        setError(t("production.produce.invalidQuantity"));
        return;
      }
    }

    if (!runIdRef.current) {
      const generated = createSecureMutationId();
      if (!generated.ok) {
        setError(t("production.produce.saveFailed"));
        return;
      }
      runIdRef.current = generated.id;
    }
    const productionRunId = runIdRef.current;
    setSaving(true);
    setError(null);

    const overrides = materials
      .filter((row) => {
        const expected = row.expected;
        return Math.abs(row.actual - expected) > 1e-9;
      })
      .map((row) => ({
        materialProductId: row.materialProductId,
        actualQuantity: row.actual,
      }));

    const body = {
      productionDefinitionId: definitionId,
      outputQuantity: outQty,
      notes: notes.trim() || null,
      referenceNumber: referenceNumber.trim() || null,
      outputExpirationDate: expirationDate.trim() || null,
      outputLotNumber: lotNumber.trim() || null,
      materialOverrides: overrides.length > 0 ? overrides : null,
      productionRunId,
    };

    try {
      const created = await createProductionRun(workspace, body);
      runIdRef.current = null;
      navigate(`/inventory/production/runs/${created.productionRunId}`, { replace: true });
    } catch (err) {
      if (isLikelyNetworkFailure(err)) {
        setError(t("checkout.confirmingTransaction"));
        try {
          const created = await createProductionRun(workspace, body);
          runIdRef.current = null;
          navigate(`/inventory/production/runs/${created.productionRunId}`, { replace: true });
          return;
        } catch (retryErr) {
          if (isLikelyNetworkFailure(retryErr)) {
            setStatusLocked(true);
            setError(t("checkout.transactionStatusUnknown"));
            return;
          }
          setError(
            retryErr instanceof PosApiError
              ? (retryErr.problem.detail ?? t("production.produce.saveFailed"))
              : t("production.produce.saveFailed"),
          );
          return;
        }
      }
      setError(
        err instanceof PosApiError
          ? (err.problem.detail ?? t("production.produce.saveFailed"))
          : t("production.produce.saveFailed"),
      );
    } finally {
      setSaving(false);
    }
  }

  const activeDefinitions = definitionsQuery.data?.items ?? [];

  return (
    <div
      className="production-run-create-page exits-page flex min-w-0 flex-col gap-3 pb-4"
      data-testid="production-run-create-page"
    >
      <PageHeader
        title={t("production.produce.title")}
        description={t("production.produce.lede")}
        backTo="/inventory/production"
        backLabel={t("production.backHome")}
        backTestId="page-header-back-production"
      />

      {!online ? (
        <Card>
          <p className="m-0">{t("production.offline")}</p>
        </Card>
      ) : null}
      {!allowManage ? (
        <Card>
          <p className="m-0">{t("production.manageDenied")}</p>
        </Card>
      ) : null}

      {error ? <ErrorState title={t("production.errorTitle")} detail={error} /> : null}

      <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
        {t("production.produce.selectSetup")}
        <select
          className="exits-select"
          value={definitionId ?? ""}
          onChange={(e) => onSelectDefinition(e.target.value)}
          disabled={!allowManage || statusLocked}
          data-testid="production-run-definition"
        >
          <option value="">{t("production.produce.chooseSetup")}</option>
          {activeDefinitions.map((item) => (
            <option key={item.productionDefinitionId} value={item.productionDefinitionId}>
              {item.name}
            </option>
          ))}
        </select>
      </label>

      {definitionsQuery.isSuccess && activeDefinitions.length === 0 ? (
        <EmptyState
          title={t("production.setups.empty")}
          detail={t("production.produce.noActiveSetups")}
        />
      ) : null}

      {definition ? (
        <>
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("production.produce.outputQuantity")}
            <input
              type="number"
              min={0}
              step="any"
              className="rounded-md border border-border bg-background px-3"
              value={outputQuantity}
              onChange={(e) => {
                const raw = e.target.value;
                setOutputQuantity(raw);
                const qty = Number(raw);
                if (Number.isFinite(qty) && qty > 0) {
                  syncActualsToExpected(definition, qty);
                }
              }}
              disabled={!allowManage || statusLocked}
              data-testid="production-run-output-qty"
            />
          </label>
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("production.produce.scaleHint")}
          </p>

          <section className="flex flex-col gap-2">
            <h2 className="m-0 text-[length:var(--exits-text-md)] font-medium">
              {t("production.produce.materials")}
            </h2>
            {materialPreviewQuery.isLoading ? (
              <LoadingState label={t("production.loading")} />
            ) : null}
            <ul className="m-0 flex list-none flex-col gap-2 p-0">
              {materials.map((row) => {
                const short =
                  row.available != null && row.actual > row.available;
                return (
                  <li key={row.materialProductId}>
                    <Card className="flex flex-col gap-2 p-3">
                      <div className="font-medium">{row.name}</div>
                      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                        {t("production.produce.expected")}: {row.expected} {row.uom}
                        {row.available != null
                          ? ` · ${t("production.produce.available")}: ${row.available} ${row.uom}`
                          : ""}
                      </p>
                      {short ? (
                        <p
                          className="m-0 text-[length:var(--exits-text-sm)] text-destructive"
                          data-testid={`production-availability-short-${row.materialProductId}`}
                        >
                          {t("production.produce.availabilityShort").replace(
                            "{quantity}",
                            `${row.available} ${row.uom}`.trim(),
                          )}
                        </p>
                      ) : row.available != null ? (
                        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                          {t("production.produce.availabilityOk")}
                        </p>
                      ) : null}
                      <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                        {t("production.produce.actual")}
                        <input
                          type="number"
                          min={0}
                          step="any"
                          className="rounded-md border border-border bg-background px-3"
                          value={
                            actualByProduct[row.materialProductId] ?? String(row.actual)
                          }
                          onChange={(e) =>
                            setActualByProduct((prev) => ({
                              ...prev,
                              [row.materialProductId]: e.target.value,
                            }))
                          }
                          disabled={!allowManage || statusLocked}
                          data-testid={`production-run-actual-${row.materialProductId}`}
                        />
                      </label>
                    </Card>
                  </li>
                );
              })}
            </ul>
          </section>

          {tracksExpiration ? (
            <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
              {t("production.produce.expiration")}
              <input
                type="date"
                className="rounded-md border border-border bg-background px-3"
                value={expirationDate}
                onChange={(e) => setExpirationDate(e.target.value)}
                disabled={!allowManage || statusLocked}
                data-testid="production-run-expiration"
              />
            </label>
          ) : null}

          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("production.produce.lotNumber")}
            <input
              className="rounded-md border border-border bg-background px-3"
              value={lotNumber}
              onChange={(e) => setLotNumber(e.target.value)}
              disabled={!allowManage || statusLocked}
              placeholder={t("production.produce.optional")}
            />
          </label>

          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("production.produce.reference")}
            <input
              className="rounded-md border border-border bg-background px-3"
              value={referenceNumber}
              onChange={(e) => setReferenceNumber(e.target.value)}
              disabled={!allowManage || statusLocked}
              placeholder={t("production.produce.optional")}
            />
          </label>

          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("production.produce.notes")}
            <textarea
              className="min-h-20 rounded-md border border-border bg-background px-3 py-2"
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              disabled={!allowManage || statusLocked}
              placeholder={t("production.produce.optional")}
            />
          </label>
        </>
      ) : null}

      <StickyActionBar>
        <Button
          type="button"
          className="w-full"
          disabled={
            !allowManage ||
            !online ||
            saving ||
            statusLocked ||
            !definitionId ||
            materials.length === 0
          }
          onClick={() => void submit()}
          data-testid="production-run-submit"
        >
          {saving ? t("production.produce.submitting") : t("production.produce.submit")}
        </Button>
      </StickyActionBar>
    </div>
  );
}
