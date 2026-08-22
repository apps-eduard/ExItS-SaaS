import { useMemo, useState, type FormEvent, type ReactNode } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { Pencil } from "lucide-react";
import { PLATFORM_PERMISSIONS } from "@/api/authorization/authorization-types";
import { classifyGlobalCatalogMutationFailure } from "@/api/global-catalog/global-catalog-errors";
import {
  PRODUCT_SELLING_MODES,
  PRODUCT_UNITS,
  type GlobalProductDetail,
  type ProductSellingMode,
  type ProductUnit,
} from "@/api/global-catalog/global-catalog-types";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusIndicator } from "@/components/exits/StatusIndicator";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { BusinessTypeMultiSelect } from "@/features/global-catalog/BusinessTypeMultiSelect";
import {
  formatGlobalCatalogInstant,
  globalCatalogControlClass,
  globalCatalogStatusTone,
} from "@/features/global-catalog/global-catalog-presentation";
import {
  globalCatalogMutationDetail,
  globalCatalogMutationMessageKey,
} from "@/features/global-catalog/global-catalog-mutation-feedback";
import { ProductImagePanel } from "@/features/global-catalog/ProductImagePanel";
import { ProductLifecycleActions } from "@/features/global-catalog/ProductLifecycleActions";
import { useGlobalBusinessTypesQuery } from "@/features/global-catalog/use-global-business-types-query";
import { useGlobalCategoryLookupQuery } from "@/features/global-catalog/use-global-category-queries";
import { useGlobalCatalogMutations } from "@/features/global-catalog/use-global-catalog-mutations";
import { useGlobalProductDetailQuery } from "@/features/global-catalog/use-global-product-queries";
import { ShellNotFoundPage } from "@/features/overview/ShellNotFoundPage";
import { useAuthorization } from "@/hooks/use-authorization";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";

const STATUS_LABELS = {
  Draft: "globalCatalog.status.Draft",
  Active: "globalCatalog.status.Active",
  Archived: "globalCatalog.status.Archived",
} as const;

export function ProductDetailPage() {
  const { productId = "" } = useParams();
  const { t, language } = usePreferences();
  const authorization = useAuthorization();
  const canView =
    authorization.status === "loaded" &&
    authorization.hasPermission(PLATFORM_PERMISSIONS.viewGlobalCatalog);
  const canManage =
    authorization.status === "loaded" &&
    authorization.hasPermission(PLATFORM_PERMISSIONS.manageGlobalProducts);

  const query = useGlobalProductDetailQuery(productId, canView);
  const lookupQuery = useGlobalCategoryLookupQuery(canView);
  const categoryName = useMemo(() => {
    const categoryId = query.data?.globalCategoryId;
    if (!categoryId) {
      return null;
    }
    return lookupQuery.data?.items.find((item) => item.id === categoryId)?.name ?? categoryId;
  }, [lookupQuery.data?.items, query.data?.globalCategoryId]);

  if (authorization.status === "loading") {
    return (
      <section aria-busy="true">
        <Skeleton className="h-8 w-48" />
      </section>
    );
  }

  if (!canView) {
    return <ShellNotFoundPage />;
  }

  const diagnostic = query.error
    ? normalizeDiagnosticError({ error: query.error, operation: "Load global product" })
    : null;

  return (
    <section className="grid gap-4">
      {query.isPending ? <DashboardWidgetSkeleton rows={4} /> : null}
      {query.isError && diagnostic ? (
        <ErrorState
          diagnostic={diagnostic}
          title={t("globalCatalog.error")}
          headingLevel="h1"
          onRetry={() => void query.refetch()}
        />
      ) : null}
      {query.data ? (
        <>
          <PageHeader
            title={query.data.name}
            description={t("globalCatalog.products.detailDescription")}
            actions={
              canManage ? (
                <Button asChild size="sm" variant="outline">
                  <Link to={`/admin/global-catalog/products/${query.data.id}/edit`}>
                    <Pencil aria-hidden="true" className="mr-1.5 size-4" />
                    {t("globalCatalog.edit")}
                  </Link>
                </Button>
              ) : null
            }
          />
          <div className="grid gap-4 rounded-[var(--exits-density-radius)] border border-border bg-surface p-4 md:grid-cols-2">
            <dl className="grid gap-2 text-[length:var(--exits-text-sm)]">
              <div>
                <dt className="text-muted">{t("globalCatalog.column.status")}</dt>
                <dd className="mt-0.5">
                  <StatusIndicator
                    tone={globalCatalogStatusTone(query.data.status)}
                    label={t(STATUS_LABELS[query.data.status])}
                  />
                </dd>
              </div>
              <div>
                <dt className="text-muted">{t("globalCatalog.column.sku")}</dt>
                <dd className="mt-0.5 font-mono">{query.data.sku}</dd>
              </div>
              <div>
                <dt className="text-muted">{t("globalCatalog.column.barcode")}</dt>
                <dd className="mt-0.5 font-mono">{query.data.barcode ?? "—"}</dd>
              </div>
              <div>
                <dt className="text-muted">{t("globalCatalog.column.brand")}</dt>
                <dd className="mt-0.5">{query.data.brand}</dd>
              </div>
              <div>
                <dt className="text-muted">{t("globalCatalog.column.category")}</dt>
                <dd className="mt-0.5">{categoryName ?? "—"}</dd>
              </div>
              <div>
                <dt className="text-muted">{t("globalCatalog.column.unit")}</dt>
                <dd className="mt-0.5">{query.data.unit}</dd>
              </div>
              <div>
                <dt className="text-muted">{t("globalCatalog.field.sellingMode")}</dt>
                <dd className="mt-0.5">{query.data.sellingMode}</dd>
              </div>
              <div>
                <dt className="text-muted">{t("globalCatalog.column.costPrice")}</dt>
                <dd className="mt-0.5 tabular-nums">
                  {query.data.costPrice != null ? query.data.costPrice.toFixed(2) : "—"}
                </dd>
              </div>
              <div>
                <dt className="text-muted">{t("globalCatalog.column.sellingPrice")}</dt>
                <dd className="mt-0.5 tabular-nums">
                  {query.data.sellingPrice != null ? query.data.sellingPrice.toFixed(2) : "—"}
                </dd>
              </div>
              <div>
                <dt className="text-muted">{t("globalCatalog.column.updated")}</dt>
                <dd className="mt-0.5 tabular-nums">
                  {formatGlobalCatalogInstant(query.data.updatedAtUtc, language) ?? "—"}
                </dd>
              </div>
            </dl>
            <ProductLifecycleActions product={query.data} canManage={canManage} />
          </div>
          <ProductImagePanel product={query.data} canManage={canManage} />
        </>
      ) : null}
    </section>
  );
}

export function ProductFormPage({ mode }: { mode: "create" | "edit" }) {
  const { productId = "" } = useParams();
  const navigate = useNavigate();
  const authorization = useAuthorization();
  const canManage =
    authorization.status === "loaded" &&
    authorization.hasPermission(PLATFORM_PERMISSIONS.manageGlobalProducts);

  if (authorization.status === "loading") {
    return (
      <section aria-busy="true">
        <Skeleton className="h-8 w-48" />
      </section>
    );
  }

  if (!canManage) {
    return <ShellNotFoundPage />;
  }

  if (mode === "edit") {
    return (
      <ProductEditForm
        productId={productId}
        onSaved={(id) => navigate(`/admin/global-catalog/products/${id}`)}
      />
    );
  }

  return <ProductCreateForm onSaved={(id) => navigate(`/admin/global-catalog/products/${id}`)} />;
}

function ProductCreateForm({ onSaved }: { onSaved: (id: string) => void }) {
  const { t } = usePreferences();
  return (
    <ProductFormShell
      title={t("globalCatalog.products.create")}
      description={t("globalCatalog.products.createDescription")}
    >
      <ProductForm mode="create" product={null} onSaved={onSaved} />
    </ProductFormShell>
  );
}

function ProductEditForm({
  productId,
  onSaved,
}: {
  productId: string;
  onSaved: (id: string) => void;
}) {
  const { t } = usePreferences();
  const query = useGlobalProductDetailQuery(productId, true);
  const diagnostic = query.error
    ? normalizeDiagnosticError({ error: query.error, operation: "Load global product" })
    : null;

  return (
    <ProductFormShell
      title={t("globalCatalog.products.edit")}
      description={t("globalCatalog.products.editDescription")}
    >
      {query.isPending ? <DashboardWidgetSkeleton rows={6} /> : null}
      {query.isError && diagnostic ? (
        <ErrorState
          diagnostic={diagnostic}
          title={t("globalCatalog.error")}
          headingLevel="h2"
          onRetry={() => void query.refetch()}
        />
      ) : null}
      {query.data ? <ProductForm mode="edit" product={query.data} onSaved={onSaved} /> : null}
      {query.data ? <ProductImagePanel product={query.data} canManage /> : null}
    </ProductFormShell>
  );
}

function ProductFormShell({
  title,
  description,
  children,
}: {
  title: string;
  description: string;
  children: ReactNode;
}) {
  return (
    <section className="grid gap-4">
      <PageHeader title={title} description={description} />
      {children}
    </section>
  );
}

function ProductForm({
  mode,
  product,
  onSaved,
}: {
  mode: "create" | "edit";
  product: GlobalProductDetail | null;
  onSaved: (id: string) => void;
}) {
  const { t } = usePreferences();
  const navigate = useNavigate();
  const businessTypesQuery = useGlobalBusinessTypesQuery(true);
  const lookupQuery = useGlobalCategoryLookupQuery(true);
  const detailQuery = useGlobalProductDetailQuery(product?.id ?? "", mode === "edit");
  const { createProduct, updateProduct } = useGlobalCatalogMutations();
  const [name, setName] = useState(product?.name ?? "");
  const [sku, setSku] = useState(product?.sku ?? "");
  const [barcode, setBarcode] = useState(product?.barcode ?? "");
  const [brand, setBrand] = useState(product?.brand ?? "");
  const [globalCategoryId, setGlobalCategoryId] = useState(product?.globalCategoryId ?? "");
  const [unit, setUnit] = useState<ProductUnit>(product?.unit ?? "Piece");
  const [sellingMode, setSellingMode] = useState<ProductSellingMode>(
    product?.sellingMode ?? "PerItem",
  );
  const [costPrice, setCostPrice] = useState(
    product?.costPrice != null ? String(product.costPrice) : "",
  );
  const [sellingPrice, setSellingPrice] = useState(
    product?.sellingPrice != null ? String(product.sellingPrice) : "",
  );
  const [businessTypeIds, setBusinessTypeIds] = useState<string[]>(
    product?.businessTypeIds ?? [],
  );
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    setErrorMessage(null);
    const payload = {
      name: name.trim(),
      sku: sku.trim(),
      barcode: barcode.trim() || undefined,
      brand: brand.trim(),
      globalCategoryId,
      unit,
      sellingMode,
      costPrice: costPrice.trim() ? Number(costPrice) : undefined,
      sellingPrice: sellingPrice.trim() ? Number(sellingPrice) : undefined,
      businessTypeIds,
    };
    if (!payload.name || !payload.sku || !payload.brand || !payload.globalCategoryId) {
      setErrorMessage(t("globalCatalog.validation.productRequired"));
      return;
    }
    try {
      if (mode === "create") {
        const created = await createProduct.mutateAsync(payload);
        onSaved(created.id);
        return;
      }
      const expectedUpdatedAtUtc = detailQuery.data?.updatedAtUtc ?? product?.updatedAtUtc;
      if (!product || !expectedUpdatedAtUtc) {
        setErrorMessage(t("globalCatalog.mutation.error.unknown"));
        return;
      }
      const updated = await updateProduct.mutateAsync({
        productId: product.id,
        input: { ...payload, expectedUpdatedAtUtc },
      });
      onSaved(updated.id);
    } catch (error) {
      const failure = classifyGlobalCatalogMutationFailure(error);
      if (failure.kind === "conflict" && mode === "edit") {
        await detailQuery.refetch();
      }
      setErrorMessage(globalCatalogMutationDetail(failure) ?? t(globalCatalogMutationMessageKey(failure)));
    }
  }

  const pending = createProduct.isPending || updateProduct.isPending;

  return (
    <form className="grid max-w-2xl gap-3" onSubmit={(event) => void onSubmit(event)}>
      <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted">
        {t("globalCatalog.field.name")}
        <input
          className="h-[var(--exits-control-height)] rounded-[var(--exits-density-radius)] border border-input bg-surface px-3"
          value={name}
          onChange={(event) => setName(event.target.value)}
          required
        />
      </label>
      <div className="grid gap-3 sm:grid-cols-2">
        <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted">
          {t("globalCatalog.column.sku")}
          <input
            className="h-[var(--exits-control-height)] rounded-[var(--exits-density-radius)] border border-input bg-surface px-3 font-mono"
            value={sku}
            onChange={(event) => setSku(event.target.value)}
            required
          />
        </label>
        <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted">
          {t("globalCatalog.column.barcode")}
          <input
            className="h-[var(--exits-control-height)] rounded-[var(--exits-density-radius)] border border-input bg-surface px-3 font-mono"
            value={barcode}
            onChange={(event) => setBarcode(event.target.value)}
          />
        </label>
      </div>
      <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted">
        {t("globalCatalog.column.brand")}
        <input
          className="h-[var(--exits-control-height)] rounded-[var(--exits-density-radius)] border border-input bg-surface px-3"
          value={brand}
          onChange={(event) => setBrand(event.target.value)}
          required
        />
      </label>
      <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted">
        {t("globalCatalog.column.category")}
        <select
          className={globalCatalogControlClass}
          value={globalCategoryId}
          required
          onChange={(event) => setGlobalCategoryId(event.target.value)}
        >
          <option value="">{t("globalCatalog.category.select")}</option>
          {(lookupQuery.data?.items ?? []).map((item) => (
            <option key={item.id} value={item.id}>
              {item.name}
            </option>
          ))}
        </select>
      </label>
      <div className="grid gap-3 sm:grid-cols-2">
        <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted">
          {t("globalCatalog.column.unit")}
          <select
            className={globalCatalogControlClass}
            value={unit}
            onChange={(event) => setUnit(event.target.value as ProductUnit)}
          >
            {PRODUCT_UNITS.map((item) => (
              <option key={item} value={item}>
                {item}
              </option>
            ))}
          </select>
        </label>
        <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted">
          {t("globalCatalog.field.sellingMode")}
          <select
            className={globalCatalogControlClass}
            value={sellingMode}
            onChange={(event) => setSellingMode(event.target.value as ProductSellingMode)}
          >
            {PRODUCT_SELLING_MODES.map((item) => (
              <option key={item} value={item}>
                {item}
              </option>
            ))}
          </select>
        </label>
      </div>
      <div className="grid gap-3 sm:grid-cols-2">
        <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted">
          {t("globalCatalog.column.costPrice")}
          <input
            className="h-[var(--exits-control-height)] rounded-[var(--exits-density-radius)] border border-input bg-surface px-3"
            inputMode="decimal"
            value={costPrice}
            onChange={(event) => setCostPrice(event.target.value)}
          />
        </label>
        <label className="grid gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted">
          {t("globalCatalog.column.sellingPrice")}
          <input
            className="h-[var(--exits-control-height)] rounded-[var(--exits-density-radius)] border border-input bg-surface px-3"
            inputMode="decimal"
            value={sellingPrice}
            onChange={(event) => setSellingPrice(event.target.value)}
          />
        </label>
      </div>
      <BusinessTypeMultiSelect
        id="product-business-types"
        options={businessTypesQuery.data?.items ?? []}
        value={businessTypeIds}
        disabled={businessTypesQuery.isPending}
        onChange={setBusinessTypeIds}
      />
      {errorMessage ? (
        <p className="text-[length:var(--exits-text-sm)] text-danger" role="alert">
          {errorMessage}
        </p>
      ) : null}
      <div className="flex flex-wrap gap-2">
        <Button type="submit" size="sm" disabled={pending} aria-busy={pending}>
          {pending ? t("globalCatalog.saving") : t("globalCatalog.save")}
        </Button>
        <Button type="button" size="sm" variant="outline" onClick={() => navigate(-1)}>
          {t("globalCatalog.cancel")}
        </Button>
      </div>
    </form>
  );
}
