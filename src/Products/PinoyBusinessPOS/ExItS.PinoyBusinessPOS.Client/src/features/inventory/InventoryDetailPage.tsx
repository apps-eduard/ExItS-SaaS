import { useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  adjustInventoryStock,
  disableInventoryTracking,
  enableInventoryTracking,
  getInventoryProduct,
  listInventoryMovements,
} from "@/api/pos/pos-inventory-client";
import { PosApiError } from "@/api/pos/pos-http";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function InventoryDetailPage() {
  const { t } = useI18n();
  const { productId } = useParams();
  const queryClient = useQueryClient();
  const { boundWorkspace } = useWorkspace();
  const [openingQty, setOpeningQty] = useState("0");
  const [adjustQty, setAdjustQty] = useState("");
  const [adjustReason, setAdjustReason] = useState("");
  const [adjustDirection, setAdjustDirection] = useState<"In" | "Out">("In");
  const [error, setError] = useState<string | null>(null);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const accountQuery = useQuery({
    queryKey: ["inventory", "product", workspace?.organizationId, productId],
    enabled: Boolean(workspace) && Boolean(productId),
    queryFn: ({ signal }) => getInventoryProduct(workspace!, productId!, signal),
  });

  const movementsQuery = useQuery({
    queryKey: ["inventory", "movements", workspace?.organizationId, productId],
    enabled: Boolean(workspace) && Boolean(productId),
    queryFn: ({ signal }) => listInventoryMovements(workspace!, productId!, {}, signal),
  });

  async function invalidate() {
    await queryClient.invalidateQueries({ queryKey: ["inventory"] });
  }

  const enableMutation = useMutation({
    mutationFn: () => {
      const qty = Number(openingQty);
      return enableInventoryTracking(workspace!, productId!, {
        openingQuantity: Number.isNaN(qty) || qty <= 0 ? null : qty,
      });
    },
    onSuccess: async () => {
      setError(null);
      await invalidate();
    },
    onError: (err) => {
      setError(
        err instanceof PosApiError ? (err.problem.detail ?? err.message) : (err as Error).message,
      );
    },
  });

  const disableMutation = useMutation({
    mutationFn: () => disableInventoryTracking(workspace!, productId!),
    onSuccess: async () => {
      setError(null);
      await invalidate();
    },
    onError: (err) => {
      setError(
        err instanceof PosApiError ? (err.problem.detail ?? err.message) : (err as Error).message,
      );
    },
  });

  const adjustMutation = useMutation({
    mutationFn: () => {
      const reason = adjustReason.trim();
      if (!reason) {
        throw new Error(t("inventory.reasonRequired"));
      }
      return adjustInventoryStock(workspace!, productId!, {
        direction: adjustDirection,
        quantity: Number(adjustQty),
        reason,
      });
    },
    onSuccess: async () => {
      setAdjustQty("");
      setAdjustReason("");
      setError(null);
      await invalidate();
    },
    onError: (err) => {
      setError(
        err instanceof PosApiError ? (err.problem.detail ?? err.message) : (err as Error).message,
      );
    },
  });

  if (!workspace || accountQuery.isLoading) {
    return <LoadingState label={t("loading.label")} />;
  }

  const account = accountQuery.data;
  if (!account) {
    return <ErrorState title={t("error.title")} detail={t("inventory.notFound")} />;
  }

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="inventory-detail-page">
      <PageHeader title={account.name} description={t("inventory.detailLede")} />
      <Button asChild variant="ghost" className="min-h-11 w-fit">
        <Link to="/inventory">{t("inventory.backList")}</Link>
      </Button>
      {error ? <ErrorState title={t("error.title")} detail={error} /> : null}

      <Card className="p-3" data-testid="inventory-status">
        <p className="m-0 font-semibold">
          {account.isTracked ? t("inventory.tracked") : t("inventory.notTracked")}
        </p>
        <p className="mt-2 mb-0 text-[length:var(--exits-text-sm)] text-muted">
          {account.isTracked
            ? `${t("inventory.onHand")}: ${account.onHandQuantity} ${account.unitOfMeasure}`
            : t("inventory.untrackedHint")}
        </p>
      </Card>

      {!account.isTracked ? (
        <Card className="flex flex-col gap-3 p-3">
          <Input
            label={t("inventory.openingQuantity")}
            name="openingQuantity"
            inputMode="decimal"
            value={openingQty}
            onChange={(e) => setOpeningQty(e.target.value)}
          />
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("inventory.openingHint")}
          </p>
          <Button
            type="button"
            className="min-h-11"
            disabled={enableMutation.isPending}
            onClick={() => enableMutation.mutate()}
            data-testid="inventory-enable"
          >
            {t("inventory.enable")}
          </Button>
        </Card>
      ) : (
        <>
          <Card className="flex flex-col gap-3 p-3">
            <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
              {t("inventory.direction")}
              <select
                className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-background px-3 font-normal"
                value={adjustDirection}
                onChange={(e) => setAdjustDirection(e.target.value as "In" | "Out")}
              >
                <option value="In">{t("inventory.adjustIn")}</option>
                <option value="Out">{t("inventory.adjustOut")}</option>
              </select>
            </label>
            <Input
              label={t("inventory.adjustQuantity")}
              name="adjustQuantity"
              inputMode="decimal"
              value={adjustQty}
              onChange={(e) => setAdjustQty(e.target.value)}
            />
            <Input
              label={t("inventory.reason")}
              name="adjustReason"
              value={adjustReason}
              onChange={(e) => setAdjustReason(e.target.value)}
            />
            <Button
              type="button"
              className="min-h-11"
              disabled={adjustMutation.isPending || !adjustQty.trim()}
              onClick={() => adjustMutation.mutate()}
              data-testid="inventory-adjust"
            >
              {t("inventory.applyAdjustment")}
            </Button>
          </Card>
          <Button
            type="button"
            variant="ghost"
            className="min-h-11 w-fit"
            disabled={disableMutation.isPending}
            onClick={() => disableMutation.mutate()}
            data-testid="inventory-disable"
          >
            {t("inventory.disable")}
          </Button>
        </>
      )}

      <div data-testid="inventory-movements">
        <h2 className="m-0 text-[length:var(--exits-text-lg)] font-semibold">
          {t("inventory.movements")}
        </h2>
        {movementsQuery.isLoading ? <LoadingState label={t("loading.label")} /> : null}
        <ul className="mt-2 mb-0 flex list-none flex-col gap-2 p-0">
          {movementsQuery.data?.items.map((movement) => (
            <li key={movement.movementId}>
              <Card className="p-3">
                <p className="m-0 font-semibold">
                  {movement.movementType} · {movement.quantityEffect}
                </p>
                <p className="mt-1 mb-0 truncate text-[length:var(--exits-text-sm)] text-muted">
                  {movement.reason} · {new Date(movement.recordedAtUtc).toLocaleString()}
                </p>
              </Card>
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}
