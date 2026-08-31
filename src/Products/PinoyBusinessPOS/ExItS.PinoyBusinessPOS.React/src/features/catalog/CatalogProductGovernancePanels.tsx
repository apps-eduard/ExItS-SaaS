import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Loader2 } from "lucide-react";
import { listOrganizationBranches, type PlatformBranch } from "@/api/platform/platform-auth-client";
import {
  getProductBranchAvailability,
  promoteCatalogProduct,
  setBranchProductAvailability,
} from "@/api/pos/pos-catalog-client";
import type {
  CatalogProductScopeCode,
  PosCatalogProductDto,
} from "@/api/pos/pos-catalog-types";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { PosApiError } from "@/api/pos/pos-http";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import {
  isBranchLocalProduct,
  isOrganizationStandardProduct,
  normalizeCatalogProductScope,
} from "@/features/catalog/catalog-product-scope";
import { useI18n } from "@/i18n/I18nProvider";
import { useToast } from "@/components/exits/ToastProvider";

export function CatalogCreateScopeFields(props: {
  canGovern: boolean;
  createScope: CatalogProductScopeCode;
  onCreateScopeChange: (scope: CatalogProductScopeCode) => void;
  branchName: string | null;
  branchId: string | null;
}) {
  const { t } = useI18n();
  const { canGovern, createScope, onCreateScopeChange, branchName, branchId } = props;

  if (!canGovern) {
    return (
      <div className="catalog-form-field--full" data-testid="catalog-create-scope">
        <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
          {t("catalog.governance.productType")}
        </p>
        <p className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
          {t("catalog.governance.branchProduct")}
          {branchName ? ` · ${branchName}` : ""}
        </p>
        <p className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
          {t("catalog.governance.productTypeBranchFixedHint").replace(
            "{branch}",
            branchName ?? branchId ?? "—",
          )}
        </p>
      </div>
    );
  }

  return (
    <fieldset className="catalog-form-field--full m-0 min-w-0 border-0 p-0" data-testid="catalog-create-scope">
      <legend className="m-0 mb-2 text-[length:var(--exits-text-sm)] font-semibold">
        {t("catalog.governance.productType")}
      </legend>
      <div className="flex flex-col gap-2">
        <label className="flex cursor-pointer gap-3 rounded-[var(--exits-radius-md)] border border-[color:var(--exits-border)] px-3 py-2.5">
          <input
            type="radio"
            name="catalogCreateScope"
            value="OrganizationStandard"
            checked={createScope === "OrganizationStandard"}
            data-testid="catalog-create-scope-OrganizationStandard"
            onChange={() => onCreateScopeChange("OrganizationStandard")}
          />
          <span>
            <span className="block font-semibold">{t("catalog.governance.organizationProduct")}</span>
            <span className="mt-0.5 block text-[length:var(--exits-text-sm)] text-muted">
              {t("catalog.governance.productTypeOrganizationHint")}
            </span>
          </span>
        </label>
        <label className="flex cursor-pointer gap-3 rounded-[var(--exits-radius-md)] border border-[color:var(--exits-border)] px-3 py-2.5">
          <input
            type="radio"
            name="catalogCreateScope"
            value="BranchLocal"
            checked={createScope === "BranchLocal"}
            data-testid="catalog-create-scope-BranchLocal"
            onChange={() => onCreateScopeChange("BranchLocal")}
          />
          <span>
            <span className="block font-semibold">
              {t("catalog.governance.branchProduct")}
              {branchName ? ` · ${branchName}` : ""}
            </span>
            <span className="mt-0.5 block text-[length:var(--exits-text-sm)] text-muted">
              {t("catalog.governance.productTypeBranchHint")}
            </span>
          </span>
        </label>
      </div>
      {createScope === "BranchLocal" && !branchId ? (
        <p className="m-0 mt-2 text-[length:var(--exits-text-sm)] text-destructive" data-testid="catalog-branch-required">
          {t("catalog.governance.branchRequiredForBranchProduct")}
        </p>
      ) : null}
    </fieldset>
  );
}

export function CatalogManagedByOrganizationBanner() {
  const { t } = useI18n();
  return (
    <div
      className="catalog-governance-banner rounded-[var(--exits-radius-md)] border border-[color:var(--exits-border)] bg-[color:var(--exits-surface)] px-3 py-2.5"
      data-testid="catalog-managed-by-organization"
      role="status"
    >
      <p className="m-0 font-semibold">{t("catalog.governance.managedByOrganization")}</p>
      <p className="mb-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
        {t("catalog.governance.managedByOrganizationLede")}
      </p>
    </div>
  );
}

export function CatalogProductScopeSummary(props: {
  product: Pick<PosCatalogProductDto, "scope" | "originBranchId" | "isOfferedAtBranch">;
  branchNameById: Map<string, string>;
  currentBranchId: string | null;
  canGovern: boolean;
}) {
  const { t } = useI18n();
  const { product, branchNameById, currentBranchId, canGovern } = props;
  const scope = normalizeCatalogProductScope(product.scope);
  const originName = product.originBranchId
    ? (branchNameById.get(product.originBranchId) ?? null)
    : null;

  if (scope === "OrganizationStandard") {
    return (
      <div className="flex flex-col gap-1" data-testid="catalog-edit-scope-summary">
        <span
          className="catalog-product-row__badge catalog-product-row__badge--scope w-fit"
          data-testid="catalog-product-scope-badge"
        >
          {t("catalog.governance.organizationProduct")}
        </span>
        {!canGovern ? (
          product.isOfferedAtBranch === false ? (
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="catalog-branch-offering-readonly">
              {t("catalog.governance.notOfferedAtThisBranch")}
            </p>
          ) : (
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="catalog-branch-offering-readonly">
              {t("catalog.governance.availableAtThisBranch")}
            </p>
          )
        ) : null}
        {originName ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("catalog.governance.originallyCreatedAt").replace("{branch}", originName)}
          </p>
        ) : null}
      </div>
    );
  }

  if (scope === "BranchLocal") {
    const sameBranch =
      product.originBranchId && currentBranchId && product.originBranchId === currentBranchId;
    return (
      <div className="flex flex-col gap-1" data-testid="catalog-edit-scope-summary">
        <span
          className="catalog-product-row__badge catalog-product-row__badge--scope w-fit"
          data-testid="catalog-product-scope-badge"
        >
          {sameBranch
            ? t("catalog.governance.branchProductThisBranch")
            : originName
              ? t("catalog.governance.branchProductOrigin").replace("{branch}", originName)
              : t("catalog.governance.branchProduct")}
        </span>
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
          {t("catalog.governance.availableAtOriginOnly")}
        </p>
      </div>
    );
  }

  return null;
}

export function CatalogPromoteControls(props: {
  workspace: PosWorkspaceScope;
  productId: string;
  enabled: boolean;
}) {
  const { t } = useI18n();
  const { showToast } = useToast();
  const queryClient = useQueryClient();
  const [open, setOpen] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const promoteMutation = useMutation({
    mutationFn: () => promoteCatalogProduct(props.workspace, props.productId),
    onSuccess: async () => {
      setOpen(false);
      setError(null);
      showToast(t("catalog.governance.promoteSuccess"));
      await queryClient.invalidateQueries({ queryKey: ["catalog"] });
    },
    onError: (err) => {
      setError(
        err instanceof PosApiError ? (err.problem.detail ?? err.message) : (err as Error).message,
      );
    },
  });

  if (!props.enabled) {
    return null;
  }

  return (
    <>
      <Button
        type="button"
        variant="outline"
        className="min-h-11"
        data-testid="catalog-promote"
        onClick={() => {
          setError(null);
          setOpen(true);
        }}
      >
        {t("catalog.governance.promote")}
      </Button>
      {open ? (
        <div
          className="fixed inset-0 z-50 flex items-end justify-center bg-black/40 p-4 sm:items-center"
          role="dialog"
          aria-modal="true"
          aria-labelledby="catalog-promote-title"
          data-testid="catalog-promote-dialog"
        >
          <Card className="flex w-full max-w-md flex-col gap-3 p-4">
            <h2 id="catalog-promote-title" className="m-0 text-[length:var(--exits-text-lg)] font-semibold">
              {t("catalog.governance.promoteTitle")}
            </h2>
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("catalog.governance.promoteBody")}
            </p>
            {error ? (
              <p className="m-0 text-[length:var(--exits-text-sm)] text-destructive">{error}</p>
            ) : null}
            <div className="flex flex-wrap gap-2">
              <Button
                type="button"
                className="min-h-11"
                disabled={promoteMutation.isPending}
                data-testid="catalog-promote-confirm"
                onClick={() => promoteMutation.mutate()}
              >
                {promoteMutation.isPending ? (
                  <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
                ) : null}
                {t("catalog.governance.promoteConfirm")}
              </Button>
              <Button
                type="button"
                variant="outline"
                className="min-h-11"
                disabled={promoteMutation.isPending}
                data-testid="catalog-promote-cancel"
                onClick={() => setOpen(false)}
              >
                {t("catalog.governance.promoteCancel")}
              </Button>
            </div>
          </Card>
        </div>
      ) : null}
    </>
  );
}

type BranchOfferRow = {
  branchId: string;
  branchName: string;
  isOffered: boolean;
};

function mergeAvailabilityRows(
  branches: PlatformBranch[],
  explicitRows: Array<{ branchId: string; isOffered: boolean }>,
): BranchOfferRow[] {
  const overrideById = new Map(explicitRows.map((row) => [row.branchId, row.isOffered]));
  return branches
    .filter((branch) => branch.status.toLowerCase() === "active")
    .map((branch) => ({
      branchId: branch.id,
      branchName: branch.name,
      isOffered: overrideById.has(branch.id) ? Boolean(overrideById.get(branch.id)) : true,
    }));
}

export function CatalogBranchAvailabilitySection(props: {
  workspace: PosWorkspaceScope;
  productId: string;
  product: Pick<PosCatalogProductDto, "scope"> | null | undefined;
  canGovern: boolean;
}) {
  const { t } = useI18n();
  const queryClient = useQueryClient();
  const [pendingDisable, setPendingDisable] = useState<BranchOfferRow | null>(null);
  const [error, setError] = useState<string | null>(null);

  const isStandard = isOrganizationStandardProduct(props.product);
  const isLocal = isBranchLocalProduct(props.product);
  const enabled = props.canGovern && isStandard && Boolean(props.productId);

  const branchesQuery = useQuery({
    queryKey: ["catalog", "org-branches", props.workspace.organizationId],
    enabled,
    staleTime: 60_000,
    queryFn: async () => {
      const result = await listOrganizationBranches(props.workspace.organizationId);
      if (!result.ok) {
        throw new Error("branches");
      }
      return result.branches;
    },
  });

  const availabilityQuery = useQuery({
    queryKey: [
      "catalog",
      "branch-availability",
      props.workspace.organizationId,
      props.workspace.branchId,
      props.productId,
    ],
    enabled,
    queryFn: ({ signal }) =>
      getProductBranchAvailability(props.workspace, props.productId, signal),
  });

  const rows = useMemo(
    () =>
      mergeAvailabilityRows(branchesQuery.data ?? [], availabilityQuery.data?.explicitRows ?? []),
    [branchesQuery.data, availabilityQuery.data?.explicitRows],
  );

  const mutation = useMutation({
    mutationFn: async (input: { branchId: string; isOffered: boolean }) => {
      await setBranchProductAvailability(
        props.workspace,
        props.productId,
        input.branchId,
        { isOffered: input.isOffered },
      );
    },
    onSuccess: async () => {
      setPendingDisable(null);
      setError(null);
      await queryClient.invalidateQueries({
        queryKey: ["catalog", "branch-availability", props.workspace.organizationId],
      });
      await queryClient.invalidateQueries({ queryKey: ["catalog"] });
    },
    onError: (err) => {
      setError(
        err instanceof PosApiError ? (err.problem.detail ?? err.message) : (err as Error).message,
      );
    },
  });

  if (isLocal) {
    return (
      <section className="catalog-form-section exits-animate-panel" data-testid="catalog-branch-availability">
        <h2 className="catalog-form-section__title">{t("catalog.governance.branchAvailability")}</h2>
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
          {t("catalog.governance.availableAtOriginOnly")}
        </p>
      </section>
    );
  }

  if (!enabled) {
    return null;
  }

  return (
    <section className="catalog-form-section exits-animate-panel" data-testid="catalog-branch-availability">
      <h2 className="catalog-form-section__title">{t("catalog.governance.branchAvailability")}</h2>
      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
        {t("catalog.governance.branchAvailabilityHint")}
      </p>
      {error ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-destructive">{error}</p>
      ) : null}
      {branchesQuery.isLoading || availabilityQuery.isLoading ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("loading.label")}</p>
      ) : null}
      <ul className="m-0 grid list-none gap-2 p-0">
        {rows.map((row) => (
          <li key={row.branchId}>
            <label className="flex items-center justify-between gap-3 rounded-[var(--exits-radius-md)] border border-[color:var(--exits-border)] px-3 py-2">
              <span className="min-w-0">
                <span className="block font-semibold">{row.branchName}</span>
                <span className="text-[length:var(--exits-text-sm)] text-muted">
                  {row.isOffered
                    ? t("catalog.governance.offered")
                    : t("catalog.governance.notOffered")}
                </span>
              </span>
              <input
                type="checkbox"
                className="size-5"
                checked={row.isOffered}
                disabled={mutation.isPending}
                data-testid={`catalog-availability-${row.branchId}`}
                onChange={(event) => {
                  const next = event.target.checked;
                  if (!next) {
                    setPendingDisable(row);
                    return;
                  }
                  mutation.mutate({ branchId: row.branchId, isOffered: true });
                }}
              />
            </label>
          </li>
        ))}
      </ul>

      {pendingDisable ? (
        <div
          className="fixed inset-0 z-50 flex items-end justify-center bg-black/40 p-4 sm:items-center"
          role="dialog"
          aria-modal="true"
          aria-labelledby="catalog-stop-offering-title"
          data-testid="catalog-stop-offering-dialog"
        >
          <Card className="flex w-full max-w-md flex-col gap-3 p-4">
            <h2 id="catalog-stop-offering-title" className="m-0 text-[length:var(--exits-text-lg)] font-semibold">
              {t("catalog.governance.stopOfferingTitle")}
            </h2>
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("catalog.governance.stopOfferingBody").replace(
                "{branch}",
                pendingDisable.branchName,
              )}
            </p>
            <div className="flex flex-wrap gap-2">
              <Button
                type="button"
                variant="destructive"
                className="min-h-11"
                disabled={mutation.isPending}
                data-testid="catalog-stop-offering-confirm"
                onClick={() =>
                  mutation.mutate({
                    branchId: pendingDisable.branchId,
                    isOffered: false,
                  })
                }
              >
                {mutation.isPending ? (
                  <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
                ) : null}
                {t("catalog.governance.stopOfferingConfirm")}
              </Button>
              <Button
                type="button"
                variant="outline"
                className="min-h-11"
                disabled={mutation.isPending}
                onClick={() => setPendingDisable(null)}
              >
                {t("catalog.governance.promoteCancel")}
              </Button>
            </div>
          </Card>
        </div>
      ) : null}
    </section>
  );
}
