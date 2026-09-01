import { useEffect, useMemo, useRef, useState } from "react";
import { Link, useLocation, useParams } from "react-router-dom";
import { useInfiniteQuery, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Package } from "lucide-react";
import { getCatalogProduct, updateCatalogProduct } from "@/api/pos/pos-catalog-client";
import {
  enableExpirationTracking,
  getInventoryProduct,
  listProductLots,
} from "@/api/pos/pos-inventory-client";
import { PosApiError } from "@/api/pos/pos-http";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { canDisableExpirationTracking } from "@/features/inventory/inventory-detail-helpers";
import { AssignExpirationLotsForm } from "@/features/inventory/AssignExpirationLotsForm";
import {
  expirationSettingsHighlightClass,
  parseExpirationSettingsFocus,
} from "@/features/inventory/expiration-settings-routes";
import { cn } from "@/lib/cn";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const LOT_PAGE_SIZE = 50;

export function ExpirationSettingsPage() {
  const { t } = useI18n();
  const { productId } = useParams();
  const { search } = useLocation();
  const queryClient = useQueryClient();
  const { boundWorkspace } = useWorkspace();
  const repairCardRef = useRef<HTMLElement>(null);
  const warningCardRef = useRef<HTMLElement>(null);
  const [warningDays, setWarningDays] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [okMessage, setOkMessage] = useState<string | null>(null);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const accountQuery = useQuery({
    queryKey: ["inventory", "product", workspace?.organizationId, workspace?.branchId, productId],
    enabled: Boolean(workspace) && Boolean(productId),
    queryFn: ({ signal }) => getInventoryProduct(workspace!, productId!, signal),
  });

  const tracksExpiration = accountQuery.data?.tracksExpiration === true;

  const lotsQuery = useInfiniteQuery({
    queryKey: ["inventory", "lots", workspace?.organizationId, workspace?.branchId, productId],
    enabled: Boolean(workspace) && Boolean(productId) && tracksExpiration,
    initialPageParam: 1,
    queryFn: ({ pageParam, signal }) =>
      listProductLots(
        workspace!,
        productId!,
        { includeDepleted: false, page: pageParam, pageSize: LOT_PAGE_SIZE },
        signal,
      ),
    getNextPageParam: (lastPage) => {
      const loaded = lastPage.page * lastPage.pageSize;
      return loaded < lastPage.totalCount ? lastPage.page + 1 : undefined;
    },
  });

  const lotTotal = useMemo(() => {
    if (!tracksExpiration) {
      return 0;
    }
    const pages = lotsQuery.data?.pages ?? [];
    if (pages.length === 0) {
      return 0;
    }
    return pages
      .flatMap((page) => page.items)
      .reduce((sum, lot) => sum + (lot.quantityOnHand ?? 0), 0);
  }, [lotsQuery.data, tracksExpiration]);

  const lotsReady = !tracksExpiration || !lotsQuery.isLoading;
  const onHand = accountQuery.data?.onHandQuantity ?? 0;
  const needsRepair =
    tracksExpiration && onHand > 0 && lotsReady && lotTotal === 0 && !lotsQuery.isFetching;

  const focus = parseExpirationSettingsFocus(search);
  const highlightAssign = focus === "assign" && needsRepair;
  const highlightWarning = focus === "warning" && tracksExpiration;

  useEffect(() => {
    const target =
      highlightAssign ? repairCardRef.current : highlightWarning ? warningCardRef.current : null;
    if (!target?.scrollIntoView) {
      return;
    }
    target.scrollIntoView({ behavior: "smooth", block: "nearest" });
  }, [highlightAssign, highlightWarning, needsRepair, tracksExpiration]);

  const displayWarningDays =
    warningDays ?? String(accountQuery.data?.expirationWarningDays ?? 7);

  const resolvedWarningDays =
    !Number.isNaN(Number(displayWarningDays)) && Number(displayWarningDays) > 0
      ? Number(displayWarningDays)
      : (accountQuery.data?.expirationWarningDays ?? 7);

  async function invalidateInventory() {
    await queryClient.invalidateQueries({ queryKey: ["inventory"] });
    await queryClient.invalidateQueries({ queryKey: ["catalog"] });
  }

  const enableMutation = useMutation({
    mutationFn: async () => {
      const days = Number(displayWarningDays);
      return enableExpirationTracking(workspace!, productId!, {
        existingStockLots: [],
        expectedOnHandQuantity: onHand,
        expirationWarningDays: !Number.isNaN(days) && days > 0 ? days : 7,
      });
    },
    onSuccess: async () => {
      setError(null);
      setOkMessage(t("inventory.expirationTrackingEnabled"));
      await invalidateInventory();
    },
    onError: (err) => {
      setOkMessage(null);
      setError(
        err instanceof PosApiError ? (err.problem.detail ?? err.message) : (err as Error).message,
      );
    },
  });

  const disableMutation = useMutation({
    mutationFn: async () => {
      const catalog = await getCatalogProduct(workspace!, productId!);
      return updateCatalogProduct(workspace!, productId!, {
        name: catalog.name,
        unitOfMeasure: catalog.unitOfMeasure,
        sellingPrice: catalog.sellingPrice,
        description: catalog.description,
        sku: catalog.sku,
        barcode: catalog.barcode,
        categoryId: catalog.categoryId,
        brandId: catalog.brandId ?? null,
        sellingMode: catalog.sellingMode,
        canBeSold: catalog.canBeSold,
        expectedUpdatedAtUtc: catalog.updatedAtUtc,
        tracksExpiration: false,
        expirationWarningDays: null,
      });
    },
    onSuccess: async () => {
      setError(null);
      setOkMessage(t("inventory.expirationTrackingDisabled"));
      setWarningDays(null);
      await invalidateInventory();
    },
    onError: (err) => {
      setOkMessage(null);
      setError(
        err instanceof PosApiError ? (err.problem.detail ?? err.message) : (err as Error).message,
      );
    },
  });

  const saveWarningMutation = useMutation({
    mutationFn: async () => {
      const days = Number(displayWarningDays);
      if (Number.isNaN(days) || days <= 0) {
        throw new Error(t("inventory.expirationWarningDaysInvalid"));
      }
      const catalog = await getCatalogProduct(workspace!, productId!);
      return updateCatalogProduct(workspace!, productId!, {
        name: catalog.name,
        unitOfMeasure: catalog.unitOfMeasure,
        sellingPrice: catalog.sellingPrice,
        description: catalog.description,
        sku: catalog.sku,
        barcode: catalog.barcode,
        categoryId: catalog.categoryId,
        brandId: catalog.brandId ?? null,
        sellingMode: catalog.sellingMode,
        canBeSold: catalog.canBeSold,
        expectedUpdatedAtUtc: catalog.updatedAtUtc,
        tracksExpiration: true,
        expirationWarningDays: days,
      });
    },
    onSuccess: async () => {
      setError(null);
      setOkMessage(t("inventory.expirationWarningSaved"));
      setWarningDays(null);
      await invalidateInventory();
    },
    onError: (err) => {
      setOkMessage(null);
      setError(
        err instanceof PosApiError ? (err.problem.detail ?? err.message) : (err as Error).message,
      );
    },
  });

  function onLotsAssigned() {
    setError(null);
    setOkMessage(t("inventory.expirationLotsAssigned"));
    void invalidateInventory();
  }

  function onExpirationEnabled() {
    setError(null);
    setOkMessage(t("inventory.expirationTrackingEnabled"));
    void invalidateInventory();
  }

  if (!workspace || accountQuery.isLoading) {
    return <LoadingState label={t("loading.label")} />;
  }

  const account = accountQuery.data;
  if (!account || !productId) {
    return <ErrorState title={t("error.title")} detail={t("inventory.notFound")} />;
  }

  const disableAllowed = canDisableExpirationTracking(account);

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="expiration-settings-page">
      <PageHeader
        title={t("inventory.expirationSettingsTitle")}
        description={account.name}
        backTo={`/inventory/${productId}`}
        backLabel={account.name}
        backTestId="expiration-settings-back"
      />

      {error ? (
        <p
          className="m-0 rounded-[var(--exits-radius-md)] border border-border px-3 py-2 text-[length:var(--exits-text-sm)] text-destructive"
          data-testid="expiration-settings-error"
          role="alert"
        >
          {error}
        </p>
      ) : null}
      {okMessage ? (
        <p
          className="m-0 rounded-[var(--exits-radius-md)] bg-muted px-3 py-2 text-[length:var(--exits-text-sm)]"
          data-testid="expiration-settings-ok"
          role="status"
        >
          {okMessage}
        </p>
      ) : null}

      <Card className="flex flex-col gap-3 p-3" data-testid="expiration-settings-status">
        <p className="m-0 font-semibold">
          {t("inventory.onHand")}: {account.onHandQuantity} {account.unitOfMeasure}
        </p>
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
          {t("inventory.stockLots")}: {tracksExpiration ? lotTotal : "—"}
        </p>
        <p
          className="m-0 text-[length:var(--exits-text-sm)] font-semibold"
          data-testid="expiration-settings-tracking-status"
        >
          {tracksExpiration
            ? t("inventory.expirationTrackingOn")
            : t("inventory.expirationTrackingOff")}
        </p>
      </Card>

      {needsRepair ? (
        <Card
          ref={repairCardRef}
          className={cn(
            "flex flex-col gap-3 p-3",
            highlightAssign && expirationSettingsHighlightClass,
          )}
          data-testid="expiration-settings-repair-banner"
          data-highlighted={highlightAssign ? "true" : undefined}
        >
          <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
            {t("inventory.expirationSetupRequired")}
          </h2>
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("inventory.expirationSetupRequiredDetail")}
          </p>
          <AssignExpirationLotsForm
            workspace={workspace}
            productId={productId}
            productName={account.name}
            onHandQuantity={account.onHandQuantity}
            unitOfMeasure={account.unitOfMeasure}
            expirationWarningDays={resolvedWarningDays}
            intent="assign"
            onSuccess={onLotsAssigned}
          />
        </Card>
      ) : null}

      {!tracksExpiration ? (
        <Card className="flex flex-col gap-3 p-3">
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("inventory.expirationSettingsEnableHint")}
          </p>
          {onHand > 0 ? (
            <AssignExpirationLotsForm
              workspace={workspace}
              productId={productId}
              productName={account.name}
              onHandQuantity={account.onHandQuantity}
              unitOfMeasure={account.unitOfMeasure}
              expirationWarningDays={resolvedWarningDays}
              intent="enable"
              onSuccess={onExpirationEnabled}
            />
          ) : (
            <Button
              type="button"
              className="min-h-11 w-fit"
              disabled={enableMutation.isPending}
              onClick={() => enableMutation.mutate()}
              data-testid="expiration-settings-enable"
            >
              {t("inventory.enableExpirationTracking")}
            </Button>
          )}
        </Card>
      ) : (
        <Card
          ref={warningCardRef}
          className={cn(
            "flex flex-col gap-3 p-3",
            highlightWarning && expirationSettingsHighlightClass,
          )}
          data-testid="expiration-settings-warning-card"
          data-highlighted={highlightWarning ? "true" : undefined}
        >
          <Input
            label={t("catalog.expirationWarningDays")}
            name="expirationWarningDays"
            inputMode="numeric"
            value={displayWarningDays}
            onChange={(e) => setWarningDays(e.target.value)}
            data-testid="expiration-settings-warning-days"
          />
          <Button
            type="button"
            className="min-h-11 w-fit"
            disabled={saveWarningMutation.isPending || needsRepair}
            onClick={() => saveWarningMutation.mutate()}
            data-testid="expiration-settings-save"
          >
            {t("inventory.saveNearExpiryWarning")}
          </Button>

          <div className="flex flex-col gap-2">
            <Button
              type="button"
              variant="outline"
              className="min-h-11 w-fit"
              disabled={disableMutation.isPending || !disableAllowed}
              onClick={() => disableMutation.mutate()}
              data-testid="expiration-settings-disable"
            >
              {t("inventory.disableExpirationTracking")}
            </Button>
            {!disableAllowed ? (
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                {t("inventory.disableExpirationBlocked")}
              </p>
            ) : null}
          </div>
        </Card>
      )}

      <Link
        to={`/inventory/${productId}`}
        className="inline-flex min-h-11 items-center gap-2 text-[length:var(--exits-text-sm)] font-semibold underline underline-offset-2"
        data-testid="expiration-settings-view-lots"
      >
        <Package className="size-4 shrink-0" aria-hidden />
        {t("inventory.viewStockLots")}
      </Link>
    </div>
  );
}
