import { useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { canManageSuppliers } from "@/access/pos-capabilities";
import {
  activateSupplier,
  deactivateSupplier,
  getSupplier,
  isConnectedSupplier,
} from "@/api/pos/pos-suppliers-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { describeSupplierError } from "@/features/suppliers/supplier-errors";
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

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const allowManage = canManageSuppliers(sessionGrant);

  const supplierQuery = useQuery({
    queryKey: ["suppliers", "detail", workspace?.organizationId, supplierId],
    enabled: Boolean(workspace) && Boolean(supplierId),
    queryFn: ({ signal }) => getSupplier(workspace!, supplierId!, signal),
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

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="supplier-detail-page">
      <PageHeader title={supplier.name} description={t("suppliers.detailLede")} />
      <div className="flex flex-wrap items-center gap-2">
        <StatusChip tone={isActive ? "success" : "warning"}>{supplier.status}</StatusChip>
        <StatusChip tone={connected ? "info" : "warning"}>
          {connected ? t("suppliers.connectionConnected") : t("suppliers.connectionManual")}
        </StatusChip>
      </div>

      {actionError ? (
        <Card data-testid="supplier-action-error">
          <p className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]">
            {actionError}
          </p>
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
        <Button asChild variant="ghost" className="min-h-11">
          <Link to="/suppliers">{t("suppliers.back")}</Link>
        </Button>
      </div>
    </div>
  );
}
