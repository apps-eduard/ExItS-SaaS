import { useEffect, useState, type ReactNode } from "react";
import { Ban, Loader2, Plus, RotateCcw, Save } from "lucide-react";
import { useNavigate, useParams, Link } from "react-router-dom";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";

import { canGovernOrganizationCatalog } from "@/access/pos-capabilities";
import { listOrganizationBranches } from "@/api/platform/platform-auth-client";
import {
  checkCatalogProductNameConflict,
  createCatalogBrand,
  createCatalogCategory,
  createCatalogProduct,
  deactivateCatalogProduct,
  getCatalogProduct,
  listCatalogBrands,
  listCatalogCategories,
  reactivateCatalogProduct,
  updateCatalogProduct,
  uploadCatalogProductImage,
} from "@/api/pos/pos-catalog-client";

import type {
  CatalogProductNameConflictDto,
  CatalogProductScopeCode,
  PosCatalogProductDto,
  PosProductBrandDto,
} from "@/api/pos/pos-catalog-types";

import {
  DEFAULT_CATALOG_SELLING_MODE,
  DEFAULT_CATALOG_SELLING_PRICE,
  DEFAULT_CATALOG_UNIT_OF_MEASURE,
  POS_SELLING_MODE_CODES,
  POS_UNIT_OF_MEASURE_CODES,
  type PosSellingModeCode,
  type PosUnitOfMeasureCode,
} from "@/api/pos/pos-catalog-options";

import { PosApiError } from "@/api/pos/pos-http";

import { Button } from "@/components/ui/button";

import { Input } from "@/components/ui/input";

import { ErrorState } from "@/components/exits/ErrorState";

import { LoadingState } from "@/components/exits/LoadingState";

import { OnlineRequiredCard } from "@/components/exits/OnlineRequiredCard";

import { PageHeader } from "@/components/exits/PageHeader";

import { StatusChip } from "@/components/exits/StatusChip";

import { useBrowserOnline } from "@/connectivity/browser-online";

import { pageBackNav } from "@/navigation/page-back-nav";

import { ONLINE_REQUIRED_CODES } from "@/offline/online-required";

import {
  createEmptyUnitDraft,
  draftsToUnitInputs,
  unitsFromDto,
  validateUnitDrafts,
  type ProductUnitDraft,
} from "@/features/catalog/product-unit-drafts";

import {
  businessUsageLabelKey,
  isSellFloorBusinessUsage,
  resolveBusinessUsage,
  type ProductBusinessUsage,
} from "@/features/catalog/product-business-usage";

import { ProductBusinessUsageSelector } from "@/features/catalog/ProductBusinessUsageSelector";
import {
  CatalogBranchAvailabilitySection,
  CatalogCreateScopeFields,
  CatalogManagedByOrganizationBanner,
  CatalogProductScopeSummary,
  CatalogPromoteControls,
} from "@/features/catalog/CatalogProductGovernancePanels";
import { BranchProductPricingPanel } from "@/features/catalog/BranchProductPricingPanel";
import {
  isBranchLocalProduct,
  isOrganizationStandardProduct,
  isStandardMasterReadOnlyForActor,
} from "@/features/catalog/catalog-product-scope";

import {
  buildEnableInventoryBody,
  computeOpeningStockValue,
  validateOpeningStockInput,
} from "@/features/catalog/opening-stock-helpers";

import { enableInventoryTracking } from "@/api/pos/pos-inventory-client";

import { useI18n } from "@/i18n/I18nProvider";

import { useWorkspace } from "@/workspace/WorkspaceProvider";
import { usePosWorkspaceScope } from "@/workspace/use-pos-workspace-scope";

function FormSelect({
  label,

  name,

  value,

  disabled,

  testId,

  onChange,

  children,
}: {
  label: string;

  name: string;

  value: string;

  disabled?: boolean;

  testId?: string;

  onChange: (value: string) => void;

  children: ReactNode;
}) {
  const fieldId = name;

  return (
    <label className="catalog-form-field flex min-w-0 flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
      {label}

      <select
        id={fieldId}

        name={name}

        data-testid={testId}

        className="catalog-form-select"

        value={value}

        disabled={disabled}

        onChange={(event) => onChange(event.target.value)}
      >
        {children}
      </select>
    </label>
  );
}

function FormCheck({
  label,

  checked,

  testId,

  disabled,

  onChange,
}: {
  label: string;

  checked: boolean;

  testId?: string;

  disabled?: boolean;

  onChange: (checked: boolean) => void;
}) {
  return (
    <label className="catalog-form-check catalog-form-field--full">
      <input
        type="checkbox"

        data-testid={testId}

        checked={checked}

        disabled={disabled}

        onChange={(event) => onChange(event.target.checked)}
      />

      {label}
    </label>
  );
}

const NAME_CONFLICT_DEBOUNCE_MS = 250;
const PRODUCT_NAME_CONFLICT_CODE = "pos.catalog.product.name.conflict";

function useDebouncedValue<T>(value: T, delayMs: number): T {
  const [debounced, setDebounced] = useState(value);
  useEffect(() => {
    const timer = window.setTimeout(() => setDebounced(value), delayMs);
    return () => window.clearTimeout(timer);
  }, [delayMs, value]);
  return debounced;
}

function isProductNameConflictError(err: unknown): err is PosApiError {
  return (
    err instanceof PosApiError &&
    err.status === 409 &&
    (err.errorCode === PRODUCT_NAME_CONFLICT_CODE ||
      err.errorCode?.includes("product.name.conflict") === true)
  );
}

function CatalogProductNameConflictPanel({
  conflict,
}: {
  conflict: CatalogProductNameConflictDto;
}) {
  const { t } = useI18n();
  if (!conflict.isDuplicate) {
    return null;
  }

  if (!conflict.canRevealExisting || !conflict.existingProduct) {
    return (
      <div
        className="exits-alert catalog-form-field--full"
        data-testid="catalog-name-conflict"
        role="status"
      >
        <p className="m-0 font-semibold">{t("catalog.duplicate.title")}</p>
        <p className="mb-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
          {t("catalog.duplicate.hiddenForeign")}
        </p>
      </div>
    );
  }

  const existing = conflict.existingProduct;
  const scopeLabel = isOrganizationStandardProduct(existing)
    ? t("catalog.duplicate.organizationProduct")
    : t("catalog.duplicate.branchProduct");
  const inactive = existing.status.trim().toLowerCase() !== "active";
  const notOffered = existing.isOfferedAtBranch === false;

  return (
    <div
      className="exits-alert catalog-form-field--full"
      data-testid="catalog-name-conflict"
      role="status"
    >
      <p className="m-0 font-semibold">{t("catalog.duplicate.title")}</p>
      <p className="mb-0 mt-1 text-[length:var(--exits-text-sm)]" data-testid="catalog-name-conflict-name">
        {existing.name}
      </p>
      <p className="mb-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">{scopeLabel}</p>
      {inactive ? (
        <p className="mb-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
          {t("catalog.duplicate.inactive")}
        </p>
      ) : null}
      {notOffered ? (
        <p className="mb-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
          {t("catalog.duplicate.notOffered")}
        </p>
      ) : null}
      <Link
        to={`/catalog/products/${existing.productId}/edit`}
        className="mt-2 inline-flex min-h-11 items-center text-[length:var(--exits-text-sm)] font-semibold underline"
        data-testid="catalog-name-conflict-use-existing"
      >
        {t("catalog.duplicate.useExisting")}
      </Link>
    </div>
  );
}

export function CatalogProductFormPage({ mode }: { mode: "create" | "edit" }) {
  const { t } = useI18n();

  const navigate = useNavigate();

  const { productId } = useParams();

  const queryClient = useQueryClient();
  const online = useBrowserOnline();

  const workspace = usePosWorkspaceScope();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const canGovern = canGovernOrganizationCatalog(sessionGrant);

  const [createScope, setCreateScope] = useState<CatalogProductScopeCode>(
    canGovern ? "OrganizationStandard" : "BranchLocal",
  );

  const [name, setName] = useState("");

  const [description, setDescription] = useState("");

  const [sku, setSku] = useState("");

  const [barcode, setBarcode] = useState("");

  const [categoryId, setCategoryId] = useState("");

  const [brandId, setBrandId] = useState("");

  const [businessUsage, setBusinessUsage] = useState<ProductBusinessUsage>("Resale");

  const [initialBusinessUsage, setInitialBusinessUsage] =
    useState<ProductBusinessUsage | null>(null);

  const [tracksExpiration, setTracksExpiration] = useState(false);

  const [trackStockQuantity, setTrackStockQuantity] = useState(mode === "create");

  const [addOpeningStock, setAddOpeningStock] = useState(false);

  const [openingQuantity, setOpeningQuantity] = useState("");

  const [openingUnitCost, setOpeningUnitCost] = useState("");

  const [openingExpiryDate, setOpeningExpiryDate] = useState("");

  const [openingBatchLot, setOpeningBatchLot] = useState("");

  const [expirationWarningDays, setExpirationWarningDays] = useState("7");

  const [unitOfMeasure, setUnitOfMeasure] = useState<PosUnitOfMeasureCode>(
    DEFAULT_CATALOG_UNIT_OF_MEASURE,
  );

  const [sellingMode, setSellingMode] = useState<PosSellingModeCode>(DEFAULT_CATALOG_SELLING_MODE);

  const [sellingPrice, setSellingPrice] = useState(String(DEFAULT_CATALOG_SELLING_PRICE));

  const [configurePackages, setConfigurePackages] = useState(false);

  const [unitDrafts, setUnitDrafts] = useState<ProductUnitDraft[]>([]);

  const [expectedUpdatedAtUtc, setExpectedUpdatedAtUtc] = useState<string | null>(null);

  const [error, setError] = useState<string | null>(null);

  const [newCategoryName, setNewCategoryName] = useState("");

  const [newBrandName, setNewBrandName] = useState("");

  const categoriesQuery = useQuery({
    queryKey: ["catalog", "categories", workspace?.organizationId, workspace?.branchId],

    enabled: Boolean(workspace),

    queryFn: ({ signal }) => listCatalogCategories(workspace!, { status: "Active" }, signal),
  });

  const brandsQuery = useQuery({
    queryKey: ["catalog", "brands", workspace?.organizationId, workspace?.branchId],

    enabled: Boolean(workspace),

    queryFn: ({ signal }) => listCatalogBrands(workspace!, { status: "Active" }, signal),
  });

  const productQuery = useQuery({
    queryKey: [
      "catalog",
      "product",
      workspace?.organizationId,
      workspace?.branchId,
      productId,
    ],

    enabled: Boolean(workspace) && mode === "edit" && Boolean(productId),

    queryFn: ({ signal }) => getCatalogProduct(workspace!, productId!, signal),
  });

  const branchesQuery = useQuery({
    queryKey: ["catalog", "org-branches", workspace?.organizationId],
    enabled: Boolean(workspace?.organizationId),
    staleTime: 60_000,
    queryFn: async () => {
      const result = await listOrganizationBranches(workspace!.organizationId);
      if (!result.ok) {
        return [];
      }
      return result.branches;
    },
  });

  const branchNameById = (() => {
    const map = new Map<string, string>();
    for (const branch of branchesQuery.data ?? []) {
      map.set(branch.id, branch.name);
    }
    return map;
  })();

  const readOnly =
    mode === "edit" &&
    isStandardMasterReadOnlyForActor({
      canGovernOrganizationCatalog: canGovern,
      product: productQuery.data,
    });

  const debouncedName = useDebouncedValue(name.trim(), NAME_CONFLICT_DEBOUNCE_MS);
  const nameConflictExcludeId = mode === "edit" ? productId : undefined;

  const nameConflictQuery = useQuery({
    queryKey: [
      "catalog",
      "name-conflict",
      workspace?.organizationId,
      workspace?.branchId,
      debouncedName,
      nameConflictExcludeId ?? null,
    ],
    enabled: Boolean(workspace) && online && !readOnly && debouncedName.length >= 1,
    queryFn: ({ signal }) =>
      checkCatalogProductNameConflict(
        workspace!,
        {
          name: debouncedName,
          excludeProductId: nameConflictExcludeId,
        },
        signal,
      ),
  });

  useEffect(() => {
    if (!productQuery.data) {
      return;
    }

    const product = productQuery.data;

    setName(product.name);

    setDescription(product.description ?? "");

    setSku(product.sku ?? "");

    setBarcode(product.barcode ?? "");

    setCategoryId(product.categoryId ?? "");

    setBrandId(product.brandId ?? "");

    const usage = resolveBusinessUsage(product);
    setBusinessUsage(usage);
    setInitialBusinessUsage(usage);

    setTracksExpiration(product.tracksExpiration === true);

    setTrackStockQuantity(product.isTracked !== false);

    setExpirationWarningDays(String(product.expirationWarningDays ?? 7));

    setUnitOfMeasure(
      (POS_UNIT_OF_MEASURE_CODES.includes(product.unitOfMeasure as PosUnitOfMeasureCode)
        ? product.unitOfMeasure
        : DEFAULT_CATALOG_UNIT_OF_MEASURE) as PosUnitOfMeasureCode,
    );

    setSellingMode(
      (POS_SELLING_MODE_CODES.includes(product.sellingMode as PosSellingModeCode)
        ? product.sellingMode
        : DEFAULT_CATALOG_SELLING_MODE) as PosSellingModeCode,
    );

    setSellingPrice(String(product.sellingPrice ?? DEFAULT_CATALOG_SELLING_PRICE));

    const drafts = unitsFromDto(product.units);

    setUnitDrafts(drafts);

    setConfigurePackages(drafts.length > 0);

    setExpectedUpdatedAtUtc(product.updatedAtUtc);
  }, [productQuery.data]);

  useEffect(() => {
    if (sellingMode === "ByWeight") {
      setUnitOfMeasure("Kilogram");
    }
  }, [sellingMode]);

  useEffect(() => {
    if (!trackStockQuantity && tracksExpiration) {
      setTracksExpiration(false);
    }
  }, [trackStockQuantity, tracksExpiration]);

  const openingStockState = {
    trackStockQuantity,
    addOpeningStock,
    openingQuantity,
    unitCost: openingUnitCost,
    expiryDate: openingExpiryDate,
    batchLot: openingBatchLot,
    tracksExpiration,
  };

  const openingStockValue = computeOpeningStockValue(
    Number(openingQuantity),
    Number(openingUnitCost),
  );

  const existingProduct = productQuery.data;
  const identityFieldsDirty =
    mode === "edit" &&
    existingProduct != null &&
    (name.trim() !== existingProduct.name.trim() ||
      (sku.trim() || "") !== (existingProduct.sku?.trim() ?? "") ||
      (barcode.trim() || "") !== (existingProduct.barcode?.trim() ?? ""));
  const identityMutationRequiresOnline = identityFieldsDirty;
  const createRequiresOnline = mode === "create";
  const blockedOffline =
    !online && (createRequiresOnline || identityMutationRequiresOnline);
  const onlineRequiredCode = createRequiresOnline
    ? ONLINE_REQUIRED_CODES.CatalogProductCreate
    : ONLINE_REQUIRED_CODES.CatalogProductIdentityMutation;
  const isNameDuplicate = nameConflictQuery.data?.isDuplicate === true;

  type SaveResult = { kind: "saved"; product: PosCatalogProductDto };

  const saveMutation = useMutation({
    mutationFn: async (): Promise<SaveResult> => {
      if (!workspace) {
        throw new Error("Workspace required");
      }
      if (readOnly) {
        throw new Error(t("catalog.governance.managedByOrganization"));
      }
      if (mode === "create" && !online) {
        throw new Error(t("online_required.catalog_product_create"));
      }
      if (identityMutationRequiresOnline && !online) {
        throw new Error(t("online_required.catalog_product_identity_mutation"));
      }
      if (nameConflictQuery.data?.isDuplicate) {
        throw new Error(t("catalog.duplicate.title"));
      }

      const price = Number(sellingPrice);

      if (Number.isNaN(price) || price < 0) {
        throw new Error(t("catalog.invalidPrice"));
      }

      if (sellingMode === "ByWeight" && unitOfMeasure !== "Kilogram") {
        throw new Error(t("catalog.byWeightRequiresKg"));
      }

      if (mode === "create") {
        const openingValidation = validateOpeningStockInput(openingStockState);
        if (openingValidation) {
          throw new Error(t(openingValidation));
        }
      }

      let unitsPayload = undefined as ReturnType<typeof draftsToUnitInputs> | undefined;

      if (configurePackages) {
        const validation = validateUnitDrafts(unitDrafts);

        if (validation) {
          throw new Error(t(validation));
        }

        unitsPayload = draftsToUnitInputs(unitDrafts);
      }

      const warningDays = Number(expirationWarningDays);
      const resolvedWarningDays =
        trackStockQuantity && tracksExpiration && !Number.isNaN(warningDays) && warningDays > 0
          ? warningDays
          : null;

      if (mode === "create") {
        const resolvedScope: CatalogProductScopeCode = canGovern
          ? createScope
          : "BranchLocal";
        if (resolvedScope === "BranchLocal" && !workspace.branchId) {
          throw new Error(t("catalog.governance.branchRequiredForBranchProduct"));
        }
        const body = {
          name: name.trim(),
          description: description.trim() || null,
          sku: sku.trim() || null,
          barcode: barcode.trim() || null,
          categoryId: categoryId || null,
          brandId: brandId || null,
          unitOfMeasure,
          sellingPrice: price,
          sellingMode,
          businessUsage,
          units: unitsPayload,
          tracksExpiration: trackStockQuantity && tracksExpiration,
          expirationWarningDays: resolvedWarningDays,
          scope: resolvedScope,
        };

        const product = await createCatalogProduct(workspace, body);

        if (trackStockQuantity) {
          await enableInventoryTracking(
            workspace,
            product.productId,
            buildEnableInventoryBody(openingStockState),
          );
        }

        return { kind: "saved", product };
      }

      const existing = productQuery.data;
      const product = await updateCatalogProduct(workspace, productId!, {
        name: name.trim(),
        description: description.trim() || null,
        sku: sku.trim() || null,
        barcode: barcode.trim() || null,
        categoryId: categoryId || null,
        brandId: brandId || null,
        unitOfMeasure,
        sellingPrice: price,
        sellingMode,
        businessUsage,
        expectedUpdatedAtUtc,
        units: configurePackages ? unitsPayload : undefined,
        tracksExpiration: existing?.tracksExpiration === true,
        expirationWarningDays: existing?.expirationWarningDays ?? null,
      });

      return { kind: "saved", product };
    },

    onSuccess: async (result) => {
      await queryClient.invalidateQueries({ queryKey: ["catalog"] });
      await queryClient.invalidateQueries({ queryKey: ["inventory"] });

      setExpectedUpdatedAtUtc(result.product.updatedAtUtc);

      setError(null);

      if (mode === "create") {
        navigate(`/catalog/products/${result.product.productId}/edit`, { replace: true });
      }
    },

    onError: (err) => {
      if (isProductNameConflictError(err)) {
        setError(null);
        void queryClient.invalidateQueries({
          queryKey: [
            "catalog",
            "name-conflict",
            workspace?.organizationId,
            workspace?.branchId,
          ],
        });
        void nameConflictQuery.refetch();
        return;
      }

      if (err instanceof PosApiError) {
        if (err.status === 409) {
          setError(err.problem.detail ?? t("catalog.conflict"));

          return;
        }

        setError(err.problem.detail ?? err.message);

        return;
      }

      setError((err as Error).message);
    },
  });

  const statusMutation = useMutation({
    mutationFn: async (action: "deactivate" | "reactivate") => {
      if (!workspace || !productId) {
        throw new Error("Workspace required");
      }
      if (action === "deactivate") {
        await deactivateCatalogProduct(workspace, productId);
        return action;
      }
      await reactivateCatalogProduct(workspace, productId);
      return action;
    },
    onSuccess: async (action) => {
      await queryClient.invalidateQueries({ queryKey: ["catalog"] });
      setError(null);
      if (action === "deactivate") {
        navigate("/catalog");
      }
    },
    onError: (err) => {
      setError(
        err instanceof PosApiError ? (err.problem.detail ?? err.message) : (err as Error).message,
      );
    },
  });

  const createCategoryMutation = useMutation({
    mutationFn: async () => {
      if (!workspace || !newCategoryName.trim()) {
        throw new Error("Category name required");
      }
      return createCatalogCategory(workspace, { name: newCategoryName.trim() });
    },
    onSuccess: async (created) => {
      await queryClient.invalidateQueries({ queryKey: ["catalog", "categories"] });
      setCategoryId(created.categoryId);
      setNewCategoryName("");
      setError(null);
    },
    onError: (err) => {
      setError(
        err instanceof PosApiError ? (err.problem.detail ?? err.message) : (err as Error).message,
      );
    },
  });

  const createBrandMutation = useMutation({
    mutationFn: async () => {
      if (!workspace || !newBrandName.trim()) {
        throw new Error("Brand name required");
      }
      return createCatalogBrand(workspace, { name: newBrandName.trim() });
    },
    onSuccess: async (created) => {
      await queryClient.invalidateQueries({ queryKey: ["catalog", "brands"] });
      setBrandId(created.brandId);
      setNewBrandName("");
      setError(null);
    },
    onError: (err) => {
      const isConflict =
        err instanceof PosApiError &&
        (err.errorCode?.includes("brand.name.conflict") ||
          /brand.*already exists/i.test(err.problem.detail ?? err.message));
      if (isConflict) {
        const normalized = newBrandName.trim().toLowerCase();
        const match = brandsQuery.data?.items.find(
          (brand) => brand.name.trim().toLowerCase() === normalized,
        );
        if (match) {
          setBrandId(match.brandId);
          setNewBrandName("");
        }
        setError(t("catalog.brandAlreadyExists"));
        return;
      }
      setError(
        err instanceof PosApiError ? (err.problem.detail ?? err.message) : (err as Error).message,
      );
    },
  });

  function updateDraft(key: string, patch: Partial<ProductUnitDraft>) {
    setUnitDrafts((current) =>
      current.map((draft) => (draft.key === key ? { ...draft, ...patch } : draft)),
    );
  }

  if (!workspace || (mode === "edit" && productQuery.isLoading)) {
    return <LoadingState label={t("loading.label")} />;
  }

  const productStatus = productQuery.data?.status;

  const isActive = productStatus?.toLowerCase() === "active";

  const isOnSellFloor = isSellFloorBusinessUsage(businessUsage);

  const showLeaveSellFloorNote =
    mode === "edit" &&
    initialBusinessUsage != null &&
    isSellFloorBusinessUsage(initialBusinessUsage) &&
    !isOnSellFloor;

  const brandOptions: PosProductBrandDto[] = (() => {
    const active = brandsQuery.data?.items ?? [];
    const currentBrandId = productQuery.data?.brandId;
    const currentBrandName = productQuery.data?.brandName;
    if (
      !currentBrandId ||
      active.some((brand) => brand.brandId === currentBrandId)
    ) {
      return active;
    }
    return [
      ...active,
      {
        brandId: currentBrandId,
        organizationId: productQuery.data?.organizationId ?? workspace.organizationId,
        name: currentBrandName?.trim() || currentBrandId,
        status: "Inactive",
        createdAtUtc: productQuery.data?.createdAtUtc ?? "",
        updatedAtUtc: productQuery.data?.updatedAtUtc ?? "",
      },
    ];
  })();

  return (
    <div
      className="catalog-form-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="catalog-product-form"
    >
      <PageHeader
        title={mode === "create" ? t("catalog.newProduct") : t("catalog.editProduct")}

        subtitle={mode === "edit" && name.trim() ? name.trim() : undefined}

        description={t("catalog.productFormLede")}

        backTo={pageBackNav.catalog.to}

        backLabel={t(pageBackNav.catalog.labelKey)}

        backTestId="page-header-back-catalog"

        trailing={
          mode === "edit" && productStatus ? (
            <span className="flex flex-wrap items-center gap-2">
              <StatusChip tone={isActive ? "success" : "warning"}>{productStatus}</StatusChip>
              <span className="text-[length:var(--exits-text-sm)] text-muted">
                {t("catalog.businessUsage.label")}: {t(businessUsageLabelKey(businessUsage))}
              </span>
            </span>
          ) : undefined
        }
      />

      {error ? <ErrorState title={t("error.title")} detail={error} /> : null}

      {readOnly ? <CatalogManagedByOrganizationBanner /> : null}

      {blockedOffline ? <OnlineRequiredCard code={onlineRequiredCode} /> : null}

      <form
        className="flex flex-col gap-3"

        onSubmit={(event) => {
          event.preventDefault();
          if (readOnly || blockedOffline || isNameDuplicate) {
            return;
          }
          saveMutation.mutate();
        }}
        onKeyDown={(event) => {
          if (readOnly && event.key === "Enter") {
            event.preventDefault();
          }
        }}
      >
        <section className="catalog-form-section exits-animate-panel">
          <h2 className="catalog-form-section__title">{t("catalog.sectionBasics")}</h2>

          <div className="catalog-form-section__grid">
            <div className="catalog-form-field--full">
              <Input
                label={t("catalog.name")}

                name="productName"

                required

                value={name}

                disabled={readOnly}

                onChange={(e) => setName(e.target.value)}
              />
            </div>

            {nameConflictQuery.data?.isDuplicate ? (
              <CatalogProductNameConflictPanel conflict={nameConflictQuery.data} />
            ) : null}

            <div className="catalog-form-field--full">
              <Input
                label={t("catalog.description")}

                name="productDescription"

                value={description}

                disabled={readOnly}

                onChange={(e) => setDescription(e.target.value)}
              />
            </div>

            <Input
              label={t("catalog.sku")}

              name="productSku"

              value={sku}

              disabled={readOnly}

              onChange={(e) => setSku(e.target.value)}
            />

            <Input
              label={t("catalog.barcode")}

              name="productBarcode"

              value={barcode}

              disabled={readOnly}

              onChange={(e) => setBarcode(e.target.value)}
            />

            <FormSelect
              label={t("catalog.category")}

              name="productCategory"

              value={categoryId}

              disabled={readOnly}

              onChange={setCategoryId}
            >
              <option value="">{t("catalog.noCategory")}</option>

              {categoriesQuery.data?.items.map((category) => (
                <option key={category.categoryId} value={category.categoryId}>
                  {category.name}
                </option>
              ))}
            </FormSelect>

            <FormSelect
              label={t("catalog.brand")}

              name="productBrand"

              testId="catalog-product-brand"

              value={brandId}

              disabled={readOnly}

              onChange={setBrandId}
            >
              <option value="">{t("catalog.noBrand")}</option>

              {brandOptions.map((brand) => (
                <option key={brand.brandId} value={brand.brandId}>
                  {brand.name}
                </option>
              ))}
            </FormSelect>
          </div>

          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("catalog.brandOptional")}</p>

          {mode === "create" ? (
            <CatalogCreateScopeFields
              canGovern={canGovern}
              createScope={createScope}
              onCreateScopeChange={setCreateScope}
              branchName={boundWorkspace?.branchName ?? null}
              branchId={workspace.branchId ?? null}
            />
          ) : null}

          {mode === "edit" && productQuery.data ? (
            <CatalogProductScopeSummary
              product={productQuery.data}
              branchNameById={branchNameById}
              currentBranchId={workspace.branchId ?? null}
              canGovern={canGovern}
            />
          ) : null}

          {!readOnly ? (
            <>
          <div className="catalog-form-quick-add">
            <p className="catalog-form-quick-add__label">{t("catalog.sectionCategoryQuickAdd")}</p>
            <div className="catalog-form-quick-add__row">
              <div className="catalog-form-quick-add__field">
                <Input
                  label={t("catalog.newCategoryPlaceholder")}
                  name="inlineCategoryName"
                  value={newCategoryName}
                  onChange={(e) => setNewCategoryName(e.target.value)}
                  placeholder={t("catalog.newCategoryPlaceholder")}
                  onKeyDown={(event) => {
                    if (event.key !== "Enter") {
                      return;
                    }
                    event.preventDefault();
                    if (newCategoryName.trim() && !createCategoryMutation.isPending) {
                      createCategoryMutation.mutate();
                    }
                  }}
                />
              </div>
              <Button
                type="button"
                variant="outline"
                className="catalog-form-quick-add__button"
                data-testid="catalog-add-category"
                disabled={!newCategoryName.trim() || createCategoryMutation.isPending}
                onClick={() => createCategoryMutation.mutate()}
              >
                {createCategoryMutation.isPending ? (
                  <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
                ) : (
                  <Plus className="size-4 shrink-0" aria-hidden />
                )}
                {createCategoryMutation.isPending
                  ? t("catalog.addingCategory")
                  : t("catalog.addCategory")}
              </Button>
            </div>
          </div>

          <div className="catalog-form-quick-add">
            <p className="catalog-form-quick-add__label">{t("catalog.sectionBrandQuickAdd")}</p>
            <div className="catalog-form-quick-add__row">
              <div className="catalog-form-quick-add__field">
                <Input
                  label={t("catalog.newBrandPlaceholder")}
                  name="inlineBrandName"
                  value={newBrandName}
                  onChange={(e) => setNewBrandName(e.target.value)}
                  placeholder={t("catalog.newBrandPlaceholder")}
                  data-testid="catalog-inline-brand-name"
                  onKeyDown={(event) => {
                    if (event.key !== "Enter") {
                      return;
                    }
                    event.preventDefault();
                    if (newBrandName.trim() && !createBrandMutation.isPending) {
                      createBrandMutation.mutate();
                    }
                  }}
                />
              </div>
              <Button
                type="button"
                variant="outline"
                className="catalog-form-quick-add__button"
                data-testid="catalog-add-brand"
                disabled={!newBrandName.trim() || createBrandMutation.isPending}
                onClick={() => createBrandMutation.mutate()}
              >
                {createBrandMutation.isPending ? (
                  <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
                ) : (
                  <Plus className="size-4 shrink-0" aria-hidden />
                )}
                {createBrandMutation.isPending ? t("catalog.addingBrand") : t("catalog.addBrand")}
              </Button>
            </div>
          </div>
            </>
          ) : null}
        </section>

        <section className="catalog-form-section exits-animate-panel">
          <h2 className="catalog-form-section__title">{t("catalog.sectionPricing")}</h2>

          <div className="catalog-form-section__grid">
            <div className="catalog-form-field--full">
              <ProductBusinessUsageSelector
                value={businessUsage}
                onChange={setBusinessUsage}
                disabled={readOnly}
              />
            </div>

            {showLeaveSellFloorNote ? (
              <p
                className="catalog-form-field--full m-0 text-[length:var(--exits-text-sm)] text-muted"
                data-testid="catalog-leave-sell-floor-note"
              >
                {t("catalog.businessUsage.leaveSellFloor")}
              </p>
            ) : null}

            <FormSelect
              label={t("catalog.baseUnit")}

              name="productBaseUom"

              testId="catalog-base-uom"

              value={unitOfMeasure}

              disabled={readOnly || sellingMode === "ByWeight"}

              onChange={(value) => setUnitOfMeasure(value as PosUnitOfMeasureCode)}
            >
              {POS_UNIT_OF_MEASURE_CODES.map((code) => (
                <option key={code} value={code}>
                  {code}
                </option>
              ))}
            </FormSelect>

            <FormSelect
              label={t("catalog.sellingMode")}

              name="productSellingMode"

              testId="catalog-selling-mode"

              value={sellingMode}

              disabled={readOnly}

              onChange={(value) => setSellingMode(value as PosSellingModeCode)}
            >
              {POS_SELLING_MODE_CODES.map((code) => (
                <option key={code} value={code}>
                  {code === "PerItem"
                    ? t("catalog.sellingModePerItem")
                    : t("catalog.sellingModeByWeight")}
                </option>
              ))}
            </FormSelect>

            <Input
              label={t("catalog.baseSellingPrice")}

              name="sellingPrice"

              inputMode="decimal"

              value={sellingPrice}

              disabled={readOnly}

              onChange={(e) => setSellingPrice(e.target.value)}
            />

            <p
              className={
                isOnSellFloor
                  ? "catalog-form-field--full m-0 text-[length:var(--exits-text-sm)] text-muted"
                  : "catalog-form-field--full m-0 text-[length:var(--exits-text-sm)] text-muted opacity-70"
              }
            >
              {t("catalog.baseSellingPriceHint")}
            </p>
          </div>
        </section>

        {mode === "edit" && productId && workspace && productQuery.data ? (
          <BranchProductPricingPanel
            workspace={workspace}
            productId={productId}
            product={productQuery.data}
            canGovern={canGovern}
            branchName={boundWorkspace?.branchName ?? null}
          />
        ) : null}

        <section className="catalog-form-section exits-animate-panel">
          <h2 className="catalog-form-section__title">{t("catalog.sectionInventory")}</h2>

          <div className="catalog-form-section__grid">
            <FormCheck
              label={t("catalog.trackStockQuantity")}
              checked={trackStockQuantity}
              testId="catalog-track-stock-quantity"
              onChange={(next) => {
                setTrackStockQuantity(next);
                if (!next) {
                  setAddOpeningStock(false);
                }
              }}
              disabled={mode === "edit"}
            />

            {trackStockQuantity && mode === "create" ? (
              <>
                <div className="catalog-form-field--full">
                  <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
                    {t("openingStock.title")}{" "}
                    <span className="font-normal text-muted">{t("openingStock.optional")}</span>
                  </p>
                  <FormCheck
                    label={t("openingStock.addNow")}
                    checked={addOpeningStock}
                    testId="catalog-add-opening-stock"
                    onChange={setAddOpeningStock}
                  />
                  <p className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
                    {t("openingStock.helper")}
                  </p>
                </div>

                {addOpeningStock ? (
                  <>
                    <Input
                      label={`${t("openingStock.quantity")} (${unitOfMeasure})`}
                      name="openingQuantity"
                      inputMode="decimal"
                      value={openingQuantity}
                      onChange={(e) => setOpeningQuantity(e.target.value)}
                      data-testid="catalog-opening-quantity"
                    />

                    <p className="catalog-form-field--full m-0 text-[length:var(--exits-text-sm)] text-muted">
                      {t("openingStock.baseUnitHelper").replace("{unit}", unitOfMeasure)}
                    </p>

                    <Input
                      label={`${t("openingStock.unitCost")} (₱ / ${unitOfMeasure})`}
                      name="openingUnitCost"
                      inputMode="decimal"
                      value={openingUnitCost}
                      onChange={(e) => setOpeningUnitCost(e.target.value)}
                      data-testid="catalog-opening-unit-cost"
                    />

                    <p className="catalog-form-field--full m-0 text-[length:var(--exits-text-sm)] text-muted">
                      {t("openingStock.unitCostHelper")}
                    </p>

                    {openingStockValue !== null ? (
                      <p
                        className="catalog-form-field--full m-0 text-[length:var(--exits-text-sm)]"
                        data-testid="catalog-opening-stock-value"
                      >
                        {t("openingStock.value")}: ₱{openingStockValue.toFixed(2)}
                      </p>
                    ) : null}

                    {tracksExpiration ? (
                      <>
                        <Input
                          label={t("openingStock.expiry")}
                          name="openingExpiryDate"
                          type="date"
                          value={openingExpiryDate}
                          onChange={(e) => setOpeningExpiryDate(e.target.value)}
                          data-testid="catalog-opening-expiry"
                        />

                        <Input
                          label={t("openingStock.batch")}
                          name="openingBatchLot"
                          value={openingBatchLot}
                          onChange={(e) => setOpeningBatchLot(e.target.value)}
                          data-testid="catalog-opening-batch"
                        />

                        <p className="catalog-form-field--full m-0 text-[length:var(--exits-text-sm)] text-muted">
                          {t("openingStock.expiryHelper")}
                        </p>
                      </>
                    ) : null}
                  </>
                ) : null}
              </>
            ) : null}

            {mode === "edit" && trackStockQuantity ? (
              <p className="catalog-form-field--full m-0 text-[length:var(--exits-text-sm)] text-muted">
                {t("openingStock.editHint")}
              </p>
            ) : null}
          </div>
        </section>

        <section className="catalog-form-section exits-animate-panel">
          <h2 className="catalog-form-section__title">{t("catalog.sectionExpiration")}</h2>

          <div className="catalog-form-section__grid">
            {mode === "edit" ? (
              <div
                className="catalog-form-field--full flex flex-col gap-2"
                data-testid="catalog-expiration-settings-summary"
              >
                <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
                  {productQuery.data?.tracksExpiration
                    ? t("inventory.expirationTrackingOnWithWarning").replace(
                        "{days}",
                        String(productQuery.data.expirationWarningDays ?? 7),
                      )
                    : t("inventory.expirationTrackingOff")}
                </p>
                <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                  {t("catalog.expirationManagedInSettings")}
                </p>
                {productId ? (
                  <Link
                    to={`/inventory/${productId}/expiration`}
                    className="inline-flex min-h-11 items-center text-[length:var(--exits-text-sm)] font-semibold underline-offset-2 hover:underline"
                    data-testid="catalog-manage-expiration-settings"
                  >
                    {t("inventory.manageExpirationSettings")}
                  </Link>
                ) : null}
              </div>
            ) : (
              <>
                <FormCheck
                  label={t("catalog.tracksExpiration")}
                  checked={tracksExpiration}
                  testId="catalog-tracks-expiration"
                  disabled={!trackStockQuantity}
                  onChange={setTracksExpiration}
                />

                <p className="catalog-form-field--full m-0 text-[length:var(--exits-text-sm)] text-muted">
                  {t("catalog.tracksExpirationHint")}
                </p>

                {tracksExpiration ? (
                  <Input
                    label={t("catalog.expirationWarningDays")}
                    name="expirationWarningDays"
                    inputMode="numeric"
                    value={expirationWarningDays}
                    onChange={(e) => setExpirationWarningDays(e.target.value)}
                    data-testid="catalog-expiration-warning-days"
                  />
                ) : null}

                {tracksExpiration ? (
                  <>
                    <p className="catalog-form-field--full m-0 text-[length:var(--exits-text-sm)] text-muted">
                      {t("catalog.expirationReceivingHint")}
                    </p>

                    <p className="catalog-form-field--full m-0 text-[length:var(--exits-text-sm)] text-muted">
                      {t("catalog.expirationBehaviorHint")}
                    </p>
                  </>
                ) : null}
              </>
            )}
          </div>
        </section>

        <section className="catalog-form-section exits-animate-panel">
          <h2 className="catalog-form-section__title">{t("catalog.sectionPackages")}</h2>

          <FormCheck
            label={t("catalog.configurePackages")}

            checked={configurePackages}

            testId="catalog-configure-packages"

            disabled={readOnly}

            onChange={(next) => {
              setConfigurePackages(next);

              if (next && unitDrafts.length === 0) {
                setUnitDrafts([createEmptyUnitDraft("Purchase"), createEmptyUnitDraft("Sell")]);
              }
            }}
          />

          {configurePackages ? (
            <div
              className="flex flex-col gap-3"
              data-testid="catalog-unit-editor"
              aria-disabled={readOnly || undefined}
            >
              <fieldset disabled={readOnly} className="m-0 min-w-0 border-0 p-0">
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                {t("catalog.packagesLede")}
              </p>

              {unitDrafts.map((draft) => (
                <div key={draft.key} className="catalog-form-unit-card">
                  <div className="flex flex-wrap items-center justify-between gap-2">
                    <span className="font-semibold">{draft.kind}</span>

                    <Button
                      type="button"

                      variant="ghost"

                      className="min-h-11"

                      onClick={() =>
                        setUnitDrafts((current) => current.filter((row) => row.key !== draft.key))
                      }
                    >
                      {t("catalog.removeUnit")}
                    </Button>
                  </div>

                  <Input
                    label={t("catalog.unitDisplayName")}

                    name={`${draft.key}-name`}

                    value={draft.displayName}

                    onChange={(e) => updateDraft(draft.key, { displayName: e.target.value })}
                  />

                  <Input
                    label={t("catalog.unitShortLabel")}

                    name={`${draft.key}-short`}

                    value={draft.shortLabel}

                    onChange={(e) => updateDraft(draft.key, { shortLabel: e.target.value })}
                  />

                  <Input
                    label={t("catalog.multiplierToBase")}

                    name={`${draft.key}-mult`}

                    inputMode="decimal"

                    value={draft.multiplierToBase}

                    onChange={(e) => updateDraft(draft.key, { multiplierToBase: e.target.value })}
                  />

                  {draft.kind === "Sell" ? (
                    <Input
                      label={t("catalog.unitSellingPrice")}

                      name={`${draft.key}-price`}

                      inputMode="decimal"

                      value={draft.sellingPrice}

                      onChange={(e) => updateDraft(draft.key, { sellingPrice: e.target.value })}
                    />
                  ) : null}

                  {draft.kind === "Sell" ? (
                    <>
                      <FormCheck
                        label={t("catalog.allowsCustomQuantity")}

                        checked={draft.allowsCustomQuantity}

                        onChange={(checked) =>
                          updateDraft(draft.key, { allowsCustomQuantity: checked })
                        }
                      />

                      <p className="catalog-form-field--full m-0 text-[length:var(--exits-text-sm)] text-muted">
                        {t("catalog.allowsCustomQuantityHint")}
                      </p>
                    </>
                  ) : null}
                </div>
              ))}

              <div className="flex flex-wrap gap-2">
                <Button
                  type="button"

                  variant="ghost"

                  className="min-h-11"

                  onClick={() =>
                    setUnitDrafts((current) => [...current, createEmptyUnitDraft("Purchase")])
                  }
                >
                  {t("catalog.addPurchaseUnit")}
                </Button>

                <Button
                  type="button"

                  variant="ghost"

                  className="min-h-11"

                  onClick={() =>
                    setUnitDrafts((current) => [...current, createEmptyUnitDraft("Sell")])
                  }
                >
                  {t("catalog.addSellUnit")}
                </Button>
              </div>
              </fieldset>
            </div>
          ) : null}
        </section>

        {mode === "edit" && productId && workspace ? (
          <CatalogBranchAvailabilitySection
            workspace={workspace}
            productId={productId}
            product={productQuery.data}
            canGovern={canGovern}
          />
        ) : null}

        {mode === "edit" && productId && !readOnly ? (
          <section className="catalog-form-section exits-animate-panel">
            <h2 className="catalog-form-section__title">{t("catalog.sectionImage")}</h2>

            <Input
              label={t("catalog.image")}

              name="productImage"

              type="file"

              accept="image/jpeg,image/png,image/webp"

              onChange={(event) => {
                const file = event.target.files?.[0];

                if (!file || !workspace) {
                  return;
                }

                void uploadCatalogProductImage(workspace, productId, file)
                  .then(() => queryClient.invalidateQueries({ queryKey: ["catalog"] }))

                  .catch((err) =>
                    setError(
                      err instanceof PosApiError
                        ? (err.problem.detail ?? err.message)
                        : (err as Error).message,
                    ),
                  );
              }}
            />
          </section>
        ) : null}

        <div className="catalog-form-actions" data-testid="catalog-form-actions">
          {!readOnly ? (
            <div className="catalog-form-actions__primary">
              <Button
                type="submit"
                className="catalog-form-actions__save"
                data-testid="catalog-save"
                disabled={
                  saveMutation.isPending ||
                  statusMutation.isPending ||
                  blockedOffline ||
                  isNameDuplicate ||
                  (mode === "create" &&
                    (canGovern ? createScope : "BranchLocal") === "BranchLocal" &&
                    !workspace.branchId)
                }
              >
                {saveMutation.isPending ? (
                  <>
                    <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
                    {t("catalog.saving")}
                  </>
                ) : (
                  <>
                    <Save className="size-4 shrink-0" aria-hidden />
                    {t("catalog.save")}
                  </>
                )}
              </Button>
            </div>
          ) : null}

          {mode === "edit" &&
          canGovern &&
          productQuery.data &&
          workspace &&
          isBranchLocalProduct(productQuery.data) ? (
            <div className="catalog-form-actions__secondary">
              <CatalogPromoteControls
                workspace={workspace}
                productId={productId!}
                enabled
              />
            </div>
          ) : null}

          {!readOnly && mode === "edit" && productQuery.data?.status === "Active" ? (
            <div className="catalog-form-actions__secondary">
              <Button
                type="button"
                variant="destructive"
                className="catalog-form-actions__danger"
                data-testid="catalog-deactivate"
                disabled={saveMutation.isPending || statusMutation.isPending}
                onClick={() => statusMutation.mutate("deactivate")}
              >
                {statusMutation.isPending ? (
                  <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
                ) : (
                  <Ban className="size-4 shrink-0" aria-hidden />
                )}
                {t("catalog.deactivate")}
              </Button>
            </div>
          ) : null}

          {!readOnly && mode === "edit" && productQuery.data?.status !== "Active" ? (
            <div className="catalog-form-actions__secondary">
              <Button
                type="button"
                variant="outline"
                className="catalog-form-actions__restore"
                data-testid="catalog-reactivate"
                disabled={saveMutation.isPending || statusMutation.isPending}
                onClick={() => statusMutation.mutate("reactivate")}
              >
                {statusMutation.isPending ? (
                  <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
                ) : (
                  <RotateCcw className="size-4 shrink-0" aria-hidden />
                )}
                {t("catalog.reactivate")}
              </Button>
            </div>
          ) : null}
        </div>
      </form>
    </div>
  );
}

export function CatalogProductCreatePage() {
  return <CatalogProductFormPage mode="create" />;
}

export function CatalogProductEditPage() {
  return <CatalogProductFormPage mode="edit" />;
}
