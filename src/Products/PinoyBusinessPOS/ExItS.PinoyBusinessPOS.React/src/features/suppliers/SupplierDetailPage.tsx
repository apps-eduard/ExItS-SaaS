import { useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { canManageSuppliers, canViewPurchasing } from "@/access/pos-capabilities";
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

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="supplier-detail-page">
      <PageHeader
        title={supplier.name}
        description={t("suppliers.detailLede")}
        backTo={pageBackNav.suppliers.to}
        backLabel={t(pageBackNav.suppliers.labelKey)}
        backTestId="page-header-back-suppliers"
      />
      <div className="flex flex-wrap items-center gap-2">
        <StatusChip tone={isActive ? "success" : "warning"}>{supplier.status}</StatusChip>
        <StatusChip tone={connectionChipTone}>{connectionChipLabel}</StatusChip>
      </div>

      {actionError ? (
        <Card data-testid="supplier-action-error">
          <p className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]">
            {actionError}
          </p>
        </Card>
      ) : null}

      {connected ? (
        <Card data-testid="supplier-connected-location">
          <dl className="m-0 grid gap-2 text-[length:var(--exits-text-sm)]">
            <div>
              <dt className="text-muted">{t("connected.connectedBusiness")}</dt>
              <dd className="m-0" data-testid="supplier-connected-business-id">
                {displayValue(connectedBusinessLabel)}
              </dd>
            </div>
            <div>
              <dt className="text-muted">{t("connected.supplierLocation")}</dt>
              <dd className="m-0" data-testid="supplier-connected-location-name">
                {displayValue(supplierLocationLabel)}
              </dd>
            </div>
          </dl>

          {canChangeLocation && !changingLocation ? (
            <div className="mt-3">
              <Button
                type="button"
                variant="ghost"
                className="min-h-11"
                data-testid="supplier-change-location"
                disabled={locationLoading || locationSaving}
                onClick={() => void startChangeLocation()}
              >
                {t("connected.changeSupplierLocation")}
              </Button>
            </div>
          ) : null}

          {canChangeLocation && changingLocation ? (
            <div className="mt-3 flex flex-col gap-3" data-testid="supplier-location-picker">
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
                      className="flex min-h-11 items-center gap-2 text-[length:var(--exits-text-sm)]"
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
                  className="min-h-11"
                  data-testid="supplier-location-save"
                  disabled={locationLoading || locationSaving || !selectedBranchId}
                  onClick={() => void saveSupplierLocation()}
                >
                  {t("suppliers.save")}
                </Button>
                <Button
                  type="button"
                  variant="ghost"
                  className="min-h-11"
                  data-testid="supplier-location-cancel"
                  disabled={locationSaving}
                  onClick={() => resetLocationEditor()}
                >
                  {t("connected.cancel")}
                </Button>
              </div>
            </div>
          ) : null}
        </Card>
      ) : null}

      <Card>
        <dl className="m-0 grid gap-2 text-[length:var(--exits-text-sm)]">
          <div>
            <dt className="text-muted">{t("suppliers.code")}</dt>
            <dd className="m-0" data-testid="supplier-code">
              {supplier.supplierCode}
            </dd>
          </div>
          <div>
            <dt className="text-muted">{t("suppliers.contactPerson")}</dt>
            <dd className="m-0">{displayValue(supplier.contactPerson)}</dd>
          </div>
          <div>
            <dt className="text-muted">{t("suppliers.mobile")}</dt>
            <dd className="m-0">{displayValue(supplier.mobileNumber)}</dd>
          </div>
          <div>
            <dt className="text-muted">{t("suppliers.telephone")}</dt>
            <dd className="m-0">{displayValue(supplier.telephoneNumber)}</dd>
          </div>
          <div>
            <dt className="text-muted">{t("suppliers.email")}</dt>
            <dd className="m-0">{displayValue(supplier.email)}</dd>
          </div>
          <div>
            <dt className="text-muted">{t("suppliers.addressLine1")}</dt>
            <dd className="m-0">{displayValue(supplier.addressLine1)}</dd>
          </div>
          <div>
            <dt className="text-muted">{t("suppliers.addressLine2")}</dt>
            <dd className="m-0">{displayValue(supplier.addressLine2)}</dd>
          </div>
          <div>
            <dt className="text-muted">{t("suppliers.city")}</dt>
            <dd className="m-0">{displayValue(supplier.cityMunicipality)}</dd>
          </div>
          <div>
            <dt className="text-muted">{t("suppliers.province")}</dt>
            <dd className="m-0">{displayValue(supplier.province)}</dd>
          </div>
          <div>
            <dt className="text-muted">{t("suppliers.postalCode")}</dt>
            <dd className="m-0">{displayValue(supplier.postalCode)}</dd>
          </div>
          <div>
            <dt className="text-muted">{t("suppliers.taxNumber")}</dt>
            <dd className="m-0">{displayValue(supplier.taxOrRegistrationNumber)}</dd>
          </div>
          <div>
            <dt className="text-muted">{t("suppliers.notes")}</dt>
            <dd className="m-0 whitespace-pre-wrap">{displayValue(supplier.notes)}</dd>
          </div>
        </dl>
      </Card>

      {connected && relationshipPending ? (
        <Card data-testid="supplier-connected-pending">
          <div className="flex flex-wrap items-center justify-between gap-2">
            <p className="m-0 text-[length:var(--exits-text-sm)]">{t("connected.requestPending")}</p>
            <Button
              type="button"
              variant="ghost"
              className="min-h-11"
              data-testid="supplier-cancel-request"
              disabled={acting}
              onClick={() => void cancelPendingRequest()}
            >
              {t("connected.cancelRequest")}
            </Button>
          </div>
        </Card>
      ) : null}

      {connected && relationshipActive && allowViewPurchasing ? (
        <div
          className="flex flex-wrap gap-2"
          role="group"
          aria-label={t("connected.browseProducts")}
          data-testid="supplier-connected-actions"
        >
          <Button asChild className="min-h-11" data-testid="supplier-browse-catalog">
            <Link to={`/suppliers/${supplierId}/connected-catalog`}>
              {t("connected.browseProducts")}
            </Link>
          </Button>
          <Button
            asChild
            variant="ghost"
            className="min-h-11"
            data-testid="supplier-linked-products"
          >
            <Link to={`/suppliers/${supplierId}/linked-products`}>
              {t("connected.linkedTitle")}
            </Link>
          </Button>
        </div>
      ) : null}

      <SupplierCreditSection supplierId={supplierId} />

      <div className="flex flex-wrap gap-2">
        {allowManage ? (
          <Button asChild className="min-h-11" data-testid="supplier-edit">
            <Link to={`/suppliers/${supplierId}/edit`}>{t("suppliers.edit")}</Link>
          </Button>
        ) : null}
        {allowManage ? (
          <Button
            type="button"
            variant="ghost"
            className="min-h-11"
            data-testid="supplier-toggle-status"
            disabled={acting}
            onClick={() => void toggleStatus()}
          >
            {isActive ? t("suppliers.deactivate") : t("suppliers.activate")}
          </Button>
        ) : null}
      </div>
    </div>
  );
}
