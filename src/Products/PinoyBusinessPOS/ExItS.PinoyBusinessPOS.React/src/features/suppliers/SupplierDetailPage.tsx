import { useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Ban,
  BookOpen,
  Check,
  ClipboardList,
  Link2,
  MapPinned,
  Pencil,
  RotateCcw,
  X,
} from "lucide-react";
import {
  canManagePurchasing,
  canManageSuppliers,
  canViewPurchasing,
} from "@/access/pos-capabilities";
import {
  isRelationshipActive,
  cancelConnectionRequest,
  isRelationshipPending,
  listRelationships,
  updateSupplierLocation,
} from "@/api/pos/pos-connected-suppliers-client";
import { PosApiError } from "@/api/pos/pos-http";
import {
  activateSupplier,
  deactivateSupplier,
  getSupplier,
  isConnectedSupplier,
} from "@/api/pos/pos-suppliers-client";
import {
  lookupPublicStoreBranches,
  type PublicStoreBranchLocationDto,
} from "@/api/platform/public-store-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { pageBackNav } from "@/navigation/page-back-nav";
import { StatusChip } from "@/components/exits/StatusChip";
import { describeSupplierError } from "@/features/suppliers/supplier-errors";
import { SupplierCreditSection } from "@/features/suppliers/SupplierCreditSection";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

function displayValue(value: string | null | undefined): string {
  const trimmed = value?.trim();
  return trimmed ? trimmed : "—";
}

function hasValue(value: string | null | undefined): boolean {
  return Boolean(value?.trim());
}

type DetailField = {
  label: string;
  value: string | null | undefined;
  testId?: string;
  preWrap?: boolean;
};

function DetailFields({ fields }: { fields: ReadonlyArray<DetailField> }) {
  const visible = fields.filter((field) => hasValue(field.value));
  if (visible.length === 0) {
    return null;
  }
  return (
    <dl className="supplier-detail-fields m-0">
      {visible.map((field) => (
        <div key={field.label} className="supplier-detail-fields__item">
          <dt className="supplier-detail-fields__label">{field.label}</dt>
          <dd
            className={
              field.preWrap
                ? "supplier-detail-fields__value whitespace-pre-wrap"
                : "supplier-detail-fields__value"
            }
            data-testid={field.testId}
          >
            {displayValue(field.value)}
          </dd>
        </div>
      ))}
    </dl>
  );
}

export function SupplierDetailPage() {
  const { t } = useI18n();
  const { supplierId } = useParams<{ supplierId: string }>();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const queryClient = useQueryClient();
  const [actionError, setActionError] = useState<string | null>(null);
  const [acting, setActing] = useState(false);
  const [changingLocation, setChangingLocation] = useState(false);
  const [locationBranches, setLocationBranches] = useState<PublicStoreBranchLocationDto[]>([]);
  const [selectedBranchId, setSelectedBranchId] = useState<string | null>(null);
  const [locationLoading, setLocationLoading] = useState(false);
  const [locationSaving, setLocationSaving] = useState(false);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const allowManage = canManageSuppliers(sessionGrant);
  const allowViewPurchasing = canViewPurchasing(sessionGrant);
  const allowCreatePurchaseOrder = canManagePurchasing(sessionGrant);

  const supplierQuery = useQuery({
    queryKey: ["suppliers", "detail", workspace?.organizationId, supplierId],
    enabled: Boolean(workspace) && Boolean(supplierId),
    queryFn: ({ signal }) => getSupplier(workspace!, supplierId!, signal),
  });

  const relationshipId = supplierQuery.data?.connectedRelationshipId ?? null;

  const relationshipQuery = useQuery({
    queryKey: ["connected-suppliers", "relationship", relationshipId],
    enabled: Boolean(workspace) && Boolean(relationshipId),
    queryFn: async ({ signal }) => {
      const rows = await listRelationships(workspace!, "buyer", signal);
      return rows.find((row) => row.relationshipId === relationshipId) ?? null;
    },
  });

  if (!workspace || !supplierId) {
    return <LoadingState label={t("session.loading")} />;
  }

  if (supplierQuery.isLoading) {
    return <LoadingState label={t("loading.label")} />;
  }

  if (supplierQuery.isError || !supplierQuery.data) {
    return (
      <ErrorState
        title={t("error.title")}
        detail={
          supplierQuery.error
            ? describeSupplierError(supplierQuery.error, t)
            : t("suppliers.notFound")
        }
      />
    );
  }

  const supplier = supplierQuery.data;
  const connected = isConnectedSupplier(supplier);
  const isActive = supplier.status.toLowerCase() === "active";
  const relationship = relationshipQuery.data;
  const relationshipActive = relationship ? isRelationshipActive(relationship) : false;
  const relationshipPending = relationship ? isRelationshipPending(relationship) : false;
  const connectionChipLabel = relationshipPending
    ? t("connected.requestPending")
    : connected
      ? t("suppliers.connectionConnected")
      : t("suppliers.connectionManual");
  const connectionChipTone = relationshipPending ? "warning" : connected ? "info" : "warning";
  const connectedBusinessLabel =
    relationship?.counterpartyPublicOrganizationId ??
    supplier.connectedBusinessPublicId ??
    supplier.notes;
  const supplierLocationLabel =
    relationship?.supplierBranchName ?? supplier.supplierBranchName;
  const publicOrgIdForBranches =
    relationship?.counterpartyPublicOrganizationId ??
    supplier.connectedBusinessPublicId ??
    null;
  const canChangeLocation =
    allowManage &&
    connected &&
    (relationshipPending || relationshipActive) &&
    Boolean(relationshipId) &&
    Boolean(publicOrgIdForBranches);

  async function toggleStatus() {
    if (!allowManage || acting || !workspace || !supplierId) {
      return;
    }
    setActing(true);
    setActionError(null);
    try {
      if (isActive) {
        await deactivateSupplier(workspace, supplierId);
      } else {
        await activateSupplier(workspace, supplierId);
      }
      await queryClient.invalidateQueries({ queryKey: ["suppliers"] });
    } catch (err) {
      setActionError(describeSupplierError(err, t));
    } finally {
      setActing(false);
    }
  }

  async function cancelPendingRequest() {
    if (!allowManage || acting || !workspace || !relationshipId) {
      return;
    }

    if (!window.confirm(t("connected.cancelRequestConfirm"))) {
      return;
    }

    setActing(true);
    setActionError(null);
    try {
      await cancelConnectionRequest(workspace, relationshipId);
      await queryClient.invalidateQueries({ queryKey: ["suppliers", "detail", workspace.organizationId, supplierId] });
      await queryClient.invalidateQueries({ queryKey: ["connected-suppliers", "relationship", relationshipId] });
      await queryClient.invalidateQueries({ queryKey: ["connected-suppliers"] });
    } catch (err) {
      setActionError(
        err instanceof PosApiError
          ? err.problem.detail ?? err.message ?? t("connected.cancelRequestFailed")
          : t("connected.cancelRequestFailed"),
      );
    } finally {
      setActing(false);
    }
  }

  function resetLocationEditor() {
    setChangingLocation(false);
    setLocationBranches([]);
    setSelectedBranchId(null);
    setLocationLoading(false);
    setLocationSaving(false);
  }

  async function startChangeLocation() {
    if (!canChangeLocation || locationLoading || locationSaving || !publicOrgIdForBranches) {
      return;
    }
    setActionError(null);
    setChangingLocation(true);
    setLocationLoading(true);
    setLocationBranches([]);
    setSelectedBranchId(relationship?.supplierBranchId ?? supplier.supplierBranchId ?? null);
    try {
      const locations = await lookupPublicStoreBranches(publicOrgIdForBranches);
      if (locations.branches.length === 0) {
        setActionError(t("connected.noActiveLocations"));
        resetLocationEditor();
        return;
      }
      setLocationBranches(locations.branches);
      setSelectedBranchId((prev) => {
        if (prev && locations.branches.some((b) => b.branchId === prev)) {
          return prev;
        }
        return locations.branches.length === 1 ? locations.branches[0]!.branchId : null;
      });
    } catch (err) {
      if (err instanceof PosApiError) {
        setActionError(err.problem.detail ?? err.message ?? t("connected.locationChangeFailed"));
      } else {
        setActionError(t("connected.locationChangeFailed"));
      }
      resetLocationEditor();
    } finally {
      setLocationLoading(false);
    }
  }

  async function saveSupplierLocation() {
    if (
      !canChangeLocation ||
      !workspace ||
      !relationshipId ||
      !selectedBranchId ||
      locationSaving ||
      locationLoading
    ) {
      return;
    }
    setLocationSaving(true);
    setActionError(null);
    try {
      await updateSupplierLocation(workspace, relationshipId, selectedBranchId);
      await queryClient.invalidateQueries({ queryKey: ["suppliers"] });
      await queryClient.invalidateQueries({ queryKey: ["connected-suppliers"] });
      resetLocationEditor();
    } catch (err) {
      if (err instanceof PosApiError) {
        setActionError(err.problem.detail ?? err.message ?? t("connected.locationChangeFailed"));
      } else {
        setActionError(t("connected.locationChangeFailed"));
      }
    } finally {
      setLocationSaving(false);
    }
  }

  const localDetailFields: DetailField[] = [
    { label: t("suppliers.code"), value: supplier.supplierCode, testId: "supplier-code" },
    { label: t("suppliers.contactPerson"), value: supplier.contactPerson },
    { label: t("suppliers.mobile"), value: supplier.mobileNumber },
    { label: t("suppliers.telephone"), value: supplier.telephoneNumber },
    { label: t("suppliers.email"), value: supplier.email },
    { label: t("suppliers.addressLine1"), value: supplier.addressLine1 },
    { label: t("suppliers.addressLine2"), value: supplier.addressLine2 },
    { label: t("suppliers.city"), value: supplier.cityMunicipality },
    { label: t("suppliers.province"), value: supplier.province },
    { label: t("suppliers.postalCode"), value: supplier.postalCode },
    { label: t("suppliers.taxNumber"), value: supplier.taxOrRegistrationNumber },
  ];
  const notesDistinctFromOrgId =
    hasValue(supplier.notes)
    && supplier.notes?.trim() !== connectedBusinessLabel?.trim();
  if (!connected || notesDistinctFromOrgId) {
    localDetailFields.push({
      label: t("suppliers.notes"),
      value: supplier.notes,
      preWrap: true,
    });
  }
  const visibleLocalFields = localDetailFields.filter((field) => hasValue(field.value));

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="supplier-detail-page">
      <PageHeader
        title={supplier.name}
        description={t("suppliers.detailLede")}
        backTo={pageBackNav.suppliers.to}
        backLabel={t(pageBackNav.suppliers.labelKey)}
        backTestId="page-header-back-suppliers"
        trailing={
          <div className="flex flex-wrap items-center gap-1.5">
            <StatusChip tone={isActive ? "success" : "warning"}>{supplier.status}</StatusChip>
            <StatusChip tone={connectionChipTone}>{connectionChipLabel}</StatusChip>
          </div>
        }
      />

      {actionError ? (
        <Card data-testid="supplier-action-error">
          <p className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]">
            {actionError}
          </p>
        </Card>
      ) : null}

      {connected ? (
        <Card data-testid="supplier-connected-location" className="grid gap-3">
          <div className="grid gap-3 sm:grid-cols-3">
            <div>
              <p className="m-0 text-[length:var(--exits-text-xs)] font-medium uppercase tracking-wide text-muted">
                {t("connected.connectedBusiness")}
              </p>
              <p
                className="m-0 mt-1 text-[length:var(--exits-text-md)] font-semibold"
                data-testid="supplier-connected-business-id"
              >
                {displayValue(connectedBusinessLabel)}
              </p>
            </div>
            <div>
              <p className="m-0 text-[length:var(--exits-text-xs)] font-medium uppercase tracking-wide text-muted">
                {t("connected.supplierLocation")}
              </p>
              <p
                className="m-0 mt-1 text-[length:var(--exits-text-md)] font-semibold"
                data-testid="supplier-connected-location-name"
              >
                {displayValue(supplierLocationLabel)}
              </p>
            </div>
            <div>
              <p className="m-0 text-[length:var(--exits-text-xs)] font-medium uppercase tracking-wide text-muted">
                {t("suppliers.code")}
              </p>
              <p
                className="m-0 mt-1 text-[length:var(--exits-text-md)] font-semibold"
                data-testid="supplier-code"
              >
                {displayValue(supplier.supplierCode)}
              </p>
            </div>
          </div>

          {visibleLocalFields.some((field) => field.testId !== "supplier-code") ? (
            <div className="border-t border-border pt-3">
              <DetailFields
                fields={localDetailFields.filter((field) => field.testId !== "supplier-code")}
              />
            </div>
          ) : null}

          {canChangeLocation && !changingLocation ? (
            <div>
              <Button
                type="button"
                variant="outline"
                className="supplier-detail-action-btn"
                data-testid="supplier-change-location"
                disabled={locationLoading || locationSaving}
                onClick={() => void startChangeLocation()}
              >
                <MapPinned className="size-4 shrink-0" aria-hidden />
                {t("connected.changeSupplierLocation")}
              </Button>
            </div>
          ) : null}

          {canChangeLocation && changingLocation ? (
            <div className="flex flex-col gap-3" data-testid="supplier-location-picker">
              {locationLoading ? (
                <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                  {t("loading.label")}
                </p>
              ) : (
                <fieldset className="m-0 flex flex-col gap-2 border-0 p-0">
                  <legend className="mb-1 text-[length:var(--exits-text-sm)] font-medium">
                    {t("connected.whichLocationSupplies")}
                  </legend>
                  {locationBranches.map((branch) => (
                    <label
                      key={branch.branchId}
                      className="flex items-center gap-2 text-[length:var(--exits-text-sm)]"
                    >
                      <input
                        type="radio"
                        name="supplier-location-branch"
                        value={branch.branchId}
                        checked={selectedBranchId === branch.branchId}
                        onChange={() => setSelectedBranchId(branch.branchId)}
                        disabled={locationSaving}
                        data-testid={`supplier-location-option-${branch.code}`}
                      />
                      <span>{branch.name}</span>
                    </label>
                  ))}
                </fieldset>
              )}
              <div className="flex flex-wrap gap-2">
                <Button
                  type="button"
                  data-testid="supplier-location-save"
                  disabled={locationLoading || locationSaving || !selectedBranchId}
                  onClick={() => void saveSupplierLocation()}
                >
                  <Check className="size-4 shrink-0" aria-hidden />
                  {t("suppliers.save")}
                </Button>
                <Button
                  type="button"
                  variant="ghost"
                  data-testid="supplier-location-cancel"
                  disabled={locationSaving}
                  onClick={() => resetLocationEditor()}
                >
                  <X className="size-4 shrink-0" aria-hidden />
                  {t("connected.cancel")}
                </Button>
              </div>
            </div>
          ) : null}

          {relationshipPending ? (
            <div
              className="flex flex-wrap items-center justify-between gap-2 border-t border-border pt-3"
              data-testid="supplier-connected-pending"
            >
              <p className="m-0 text-[length:var(--exits-text-sm)]">{t("connected.requestPending")}</p>
              <Button
                type="button"
                variant="destructive"
                data-testid="supplier-cancel-request"
                disabled={acting}
                onClick={() => void cancelPendingRequest()}
              >
                <Ban className="size-4 shrink-0" aria-hidden />
                {t("connected.cancelRequest")}
              </Button>
            </div>
          ) : null}

          {relationshipActive && allowViewPurchasing ? (
            <div
              className="supplier-detail-actions flex flex-wrap gap-2 border-t border-border pt-3"
              role="group"
              aria-label={t("connected.browseProducts")}
              data-testid="supplier-connected-actions"
            >
              <Button asChild className="supplier-detail-action-btn" data-testid="supplier-browse-catalog">
                <Link to={`/suppliers/${supplierId}/connected-catalog`}>
                  <BookOpen className="size-4 shrink-0" aria-hidden />
                  {t("connected.browseProducts")}
                </Link>
              </Button>
              <Button asChild variant="outline" className="supplier-detail-action-btn" data-testid="supplier-linked-products">
                <Link to={`/suppliers/${supplierId}/linked-products`}>
                  <Link2 className="size-4 shrink-0" aria-hidden />
                  {t("connected.linkedTitle")}
                </Link>
              </Button>
              {allowCreatePurchaseOrder ? (
                <Button
                  asChild
                  variant="outline"
                  className="supplier-detail-action-btn"
                  data-testid="supplier-create-purchase-order"
                >
                  <Link to={`/purchasing/new?supplierId=${encodeURIComponent(supplierId)}`}>
                    <ClipboardList className="size-4 shrink-0" aria-hidden />
                    {t("connected.createPurchaseOrder")}
                  </Link>
                </Button>
              ) : null}
            </div>
          ) : null}
        </Card>
      ) : (
        <Card>
          <h2 className="m-0 mb-3 text-[length:var(--exits-text-sm)] font-semibold">
            {t("suppliers.detailsHeading")}
          </h2>
          {visibleLocalFields.length > 0 ? (
            <DetailFields fields={localDetailFields} />
          ) : (
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("suppliers.noLocalDetails")}
            </p>
          )}
        </Card>
      )}

      <SupplierCreditSection supplierId={supplierId} />

      <div className="supplier-detail-actions flex flex-wrap gap-2">
        {allowManage ? (
          <Button asChild variant="outline" className="supplier-detail-action-btn" data-testid="supplier-edit">
            <Link to={`/suppliers/${supplierId}/edit`}>
              <Pencil className="size-4 shrink-0" aria-hidden />
              {t("suppliers.edit")}
            </Link>
          </Button>
        ) : null}
        {allowManage ? (
          <Button
            type="button"
            variant={isActive ? "destructive" : "outline"}
            className="supplier-detail-action-btn"
            data-testid="supplier-toggle-status"
            disabled={acting}
            onClick={() => void toggleStatus()}
          >
            {isActive ? (
              <Ban className="size-4 shrink-0" aria-hidden />
            ) : (
              <RotateCcw className="size-4 shrink-0" aria-hidden />
            )}
            {isActive ? t("suppliers.deactivate") : t("suppliers.activate")}
          </Button>
        ) : null}
      </div>
    </div>
  );
}
