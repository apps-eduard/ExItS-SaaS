import { useMemo, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { canManageCatalog, canManagePurchasing } from "@/access/pos-capabilities";
import { listCatalogProducts } from "@/api/pos/pos-catalog-client";
import {
  createBuyerProductAndLink,
  linkProduct,
  searchExposedCatalog,
} from "@/api/pos/pos-connected-suppliers-client";
import { PosApiError } from "@/api/pos/pos-http";
import {
  getConnectedReceivingReadiness,
  getPurchaseOrder,
} from "@/api/pos/pos-purchase-orders-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { SearchField } from "@/components/exits/SearchField";
import { StatusChip } from "@/components/exits/StatusChip";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function PrepareConnectedProductsPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { purchaseOrderId } = useParams<{ purchaseOrderId: string }>();
  const queryClient = useQueryClient();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const [error, setError] = useState<string | null>(null);
  const [busyKey, setBusyKey] = useState<string | null>(null);
  const [createFor, setCreateFor] = useState<{
    supplierProductId: string;
    name: string;
    uom: string;
    purchasePrice: number;
  } | null>(null);
  const [sellingPriceText, setSellingPriceText] = useState("");
  const [pickerFor, setPickerFor] = useState<string | null>(null);
  const [pickerSearch, setPickerSearch] = useState("");

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const allowManage = canManagePurchasing(sessionGrant);
  const allowCreate = allowManage && canManageCatalog(sessionGrant);

  const poQuery = useQuery({
    queryKey: ["purchase-order", workspace?.organizationId, purchaseOrderId],
    enabled: Boolean(workspace) && Boolean(purchaseOrderId),
    queryFn: ({ signal }) => getPurchaseOrder(workspace!, purchaseOrderId!, signal),
  });

  const readinessQuery = useQuery({
    queryKey: ["po-receiving-readiness", workspace?.organizationId, purchaseOrderId],
    enabled: Boolean(workspace) && Boolean(purchaseOrderId),
    queryFn: ({ signal }) => getConnectedReceivingReadiness(workspace!, purchaseOrderId!, signal),
  });

  const relationshipId = readinessQuery.data?.relationshipId ?? null;

  const catalogQuery = useQuery({
    queryKey: ["connected-catalog-exposures", relationshipId, workspace?.organizationId],
    enabled: Boolean(workspace) && Boolean(relationshipId),
    queryFn: ({ signal }) =>
      searchExposedCatalog(workspace!, relationshipId!, { page: 1, pageSize: 100 }, signal),
  });

  const pickerProductsQuery = useQuery({
    queryKey: ["catalog-products", "prepare-link", workspace?.organizationId, pickerSearch],
    enabled: Boolean(workspace) && Boolean(pickerFor) && pickerSearch.trim().length > 0,
    queryFn: ({ signal }) =>
      listCatalogProducts(
        workspace!,
        { search: pickerSearch.trim(), status: "Active", pageSize: 20 },
        signal,
      ),
  });

  if (!workspace || !purchaseOrderId) {
    return <LoadingState label={t("session.loading")} />;
  }

  if (poQuery.isLoading || readinessQuery.isLoading) {
    return <LoadingState label={t("loading.label")} />;
  }

  if (poQuery.isError || readinessQuery.isError || !poQuery.data || !readinessQuery.data) {
    return (
      <ErrorState
        title={t("error.title")}
        detail={
          readinessQuery.error instanceof PosApiError
            ? (readinessQuery.error.problem.detail ?? readinessQuery.error.message)
            : t("purchasing.loadFailed")
        }
      />
    );
  }

  const readiness = readinessQuery.data;
  const needs = readiness.items.filter((item) => item.needsSetup);

  async function refresh() {
    await queryClient.invalidateQueries({
      queryKey: ["po-receiving-readiness", workspace.organizationId, purchaseOrderId],
    });
    await queryClient.invalidateQueries({
      queryKey: ["purchase-order", workspace.organizationId, purchaseOrderId],
    });
    await readinessQuery.refetch();
  }

  async function doLink(supplierProductId: string, buyerProductId: string) {
    if (!relationshipId || !allowManage) {
      return;
    }
    const exposure = catalogQuery.data?.items.find((x) => x.productId === supplierProductId);
    if (!exposure) {
      setError(t("purchasing.prepareExposureMissing"));
      return;
    }
    setBusyKey(`link-${supplierProductId}`);
    setError(null);
    try {
      await linkProduct(workspace, relationshipId, {
        buyerProductId,
        exposureId: exposure.exposureId,
        purchaseOrderId,
      });
      setPickerFor(null);
      await refresh();
    } catch (err) {
      setError(
        err instanceof PosApiError
          ? (err.problem.detail ?? err.message)
          : t("purchasing.prepareLinkFailed"),
      );
    } finally {
      setBusyKey(null);
    }
  }

  async function doCreate() {
    if (!createFor || !relationshipId || !allowCreate) {
      return;
    }
    const exposure = catalogQuery.data?.items.find(
      (x) => x.productId === createFor.supplierProductId,
    );
    if (!exposure) {
      setError(t("purchasing.prepareExposureMissing"));
      return;
    }
    const selling = Number(sellingPriceText);
    if (!Number.isFinite(selling) || selling < 0) {
      setError(t("purchasing.prepareSellingPriceInvalid"));
      return;
    }
    setBusyKey(`create-${createFor.supplierProductId}`);
    setError(null);
    try {
      await createBuyerProductAndLink(workspace, relationshipId, {
        exposureId: exposure.exposureId,
        name: createFor.name,
        unitOfMeasure: createFor.uom,
        sellingPrice: selling,
        purchaseOrderId,
      });
      setCreateFor(null);
      setSellingPriceText("");
      await refresh();
    } catch (err) {
      setError(
        err instanceof PosApiError
          ? (err.problem.detail ?? err.message)
          : t("purchasing.prepareCreateFailed"),
      );
    } finally {
      setBusyKey(null);
    }
  }

  return (
    <div className="flex flex-col gap-4 pb-24" data-testid="prepare-products-page">
      <PageHeader
        title={t("purchasing.prepareProductsTitle")}
        subtitle={t("purchasing.prepareProductsHelp").replace(
          "{count}",
          String(readiness.needsSetupCount),
        )}
        backTo={`/purchasing/${purchaseOrderId}`}
      />
      {error ? (
        <ErrorState title={t("error.title")} detail={error} />
      ) : null}
      {needs.length === 0 ? (
        <EmptyState
          title={t("purchasing.prepareAllReady")}
          detail={t("purchasing.prepareAllReadyHelp")}
        />
      ) : null}
      <ul className="m-0 grid list-none gap-3 p-0 md:grid-cols-2">
        {needs.map((item) => (
          <li key={item.supplierProductId}>
            <Card className="flex flex-col gap-3 p-3" data-testid={`prepare-item-${item.supplierProductId}`}>
              <div className="flex flex-wrap items-start justify-between gap-2">
                <div>
                  <p className="m-0 font-semibold">{item.supplierName}</p>
                  <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                    {item.supplierSku}
                    {item.supplierSku ? " · " : null}
                    {item.purchaseUnitPrice} / {item.unitOfMeasureCode}
                  </p>
                </div>
                <StatusChip tone={item.status === "Review" ? "warning" : "neutral"}>
                  {item.status === "Review"
                    ? t("purchasing.prepareMatchFound")
                    : t("purchasing.prepareNewProduct")}
                </StatusChip>
              </div>
              {item.candidateBuyerProductName ? (
                <p className="m-0 text-[length:var(--exits-text-sm)]">
                  {t("purchasing.prepareYourProduct")}: {item.candidateBuyerProductName}
                </p>
              ) : null}
              <div className="flex flex-wrap gap-2">
                {item.candidateBuyerProductId && allowManage ? (
                  <Button
                    type="button"
                    className="min-h-11"
                    disabled={busyKey != null}
                    onClick={() => void doLink(item.supplierProductId, item.candidateBuyerProductId!)}
                  >
                    {t("purchasing.prepareLinkThis")}
                  </Button>
                ) : null}
                {allowManage ? (
                  <Button
                    type="button"
                    variant="ghost"
                    className="min-h-11"
                    disabled={busyKey != null}
                    onClick={() => {
                      setPickerFor(item.supplierProductId);
                      setPickerSearch("");
                    }}
                  >
                    {t("purchasing.prepareMatchExisting")}
                  </Button>
                ) : null}
                {allowCreate ? (
                  <Button
                    type="button"
                    className="min-h-11"
                    disabled={busyKey != null}
                    onClick={() => {
                      setCreateFor({
                        supplierProductId: item.supplierProductId,
                        name: item.supplierName,
                        uom: item.unitOfMeasureCode,
                        purchasePrice: item.purchaseUnitPrice,
                      });
                      setSellingPriceText("");
                    }}
                  >
                    {t("purchasing.prepareCreateProduct")}
                  </Button>
                ) : null}
              </div>
            </Card>
          </li>
        ))}
      </ul>

      {pickerFor ? (
        <Card className="flex flex-col gap-3 p-3" data-testid="prepare-link-picker">
          <p className="m-0 font-medium">{t("purchasing.prepareChooseExisting")}</p>
          <SearchField
            value={pickerSearch}
            onChange={setPickerSearch}
            placeholder={t("purchasing.prepareSearchProducts")}
          />
          <ul className="m-0 grid list-none gap-2 p-0">
            {(pickerProductsQuery.data?.items ?? []).map((product) => (
              <li key={product.productId}>
                <Button
                  type="button"
                  variant="ghost"
                  className="min-h-11 w-full justify-start"
                  disabled={busyKey != null}
                  onClick={() => void doLink(pickerFor, product.productId)}
                >
                  {product.name}
                </Button>
              </li>
            ))}
          </ul>
          <Button type="button" variant="ghost" onClick={() => setPickerFor(null)}>
            {t("common.cancel")}
          </Button>
        </Card>
      ) : null}

      {createFor ? (
        <Card className="flex flex-col gap-3 p-3" data-testid="prepare-create-form">
          <p className="m-0 font-medium">{t("purchasing.prepareCreateFromSupplier")}</p>
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("purchasing.prepareSupplierProduct")}: {createFor.name}
          </p>
          <p className="m-0 text-[length:var(--exits-text-sm)]">
            {t("purchasing.preparePurchaseCost")}: {createFor.purchasePrice} / {createFor.uom}
          </p>
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            <span>{t("purchasing.prepareYourSellingPrice")}</span>
            <input
              className="min-h-11 rounded-md border border-border px-3"
              inputMode="decimal"
              value={sellingPriceText}
              onChange={(event) => setSellingPriceText(event.target.value)}
              data-testid="prepare-selling-price"
            />
          </label>
          <div className="flex flex-wrap gap-2">
            <Button type="button" variant="ghost" onClick={() => setCreateFor(null)}>
              {t("common.cancel")}
            </Button>
            <Button
              type="button"
              className="min-h-11"
              disabled={busyKey != null}
              onClick={() => void doCreate()}
              data-testid="prepare-create-link"
            >
              {t("purchasing.prepareCreateAndLink")}
            </Button>
          </div>
        </Card>
      ) : null}

      <div className="sticky bottom-0 z-10 border-t border-border bg-background p-3">
        {readiness.canReceive ? (
          <Button
            type="button"
            className="min-h-11 w-full"
            data-testid="prepare-continue-receive"
            onClick={() => navigate(`/purchasing/${purchaseOrderId}/receive`)}
          >
            {t("purchasing.continueToReceiving")}
          </Button>
        ) : (
          <p className="m-0 text-center text-[length:var(--exits-text-sm)] text-muted">
            {t("purchasing.prepareFinishSetup")}
          </p>
        )}
        <div className="mt-2 text-center">
          <Link to={`/purchasing/${purchaseOrderId}`}>{t("purchasing.backToOrder")}</Link>
        </div>
      </div>
    </div>
  );
}
