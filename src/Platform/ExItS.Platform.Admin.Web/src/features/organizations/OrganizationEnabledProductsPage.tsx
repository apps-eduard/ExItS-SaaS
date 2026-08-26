import { useMemo, useState } from "react";
import { useParams } from "react-router-dom";
import { PLATFORM_PERMISSIONS } from "@/api/authorization/authorization-types";
import { parseOrganizationId } from "@/api/organizations/organization-id";
import type { EnabledProduct } from "@/api/organizations/organization-types";
import { PRODUCT_LOCAL_ROLE_CODES } from "@/api/organizations/enabled-products-client";
import { PlatformApiError } from "@/api/platform-http";
import { Alert } from "@/components/ui/alert";
import { Card } from "@/components/ui/card";
import { ConfirmActionDialog } from "@/components/exits/ConfirmActionDialog";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusIndicator } from "@/components/exits/StatusIndicator";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { organizationMutationFailureCopy } from "@/features/organizations/organization-mutation-feedback";
import {
  useAssignProductLocalRoleMutation,
  useLaunchProductMutation,
  useRevokeProductLocalRoleMutation,
} from "@/features/organizations/use-organization-mutations";
import {
  useOrganizationEnabledProductsQuery,
  useOrganizationMembersQuery,
  useOrganizationProductLocalRolesQuery,
} from "@/features/organizations/use-organization-workspace-queries";
import { useAuthorization } from "@/hooks/use-authorization";
import { usePreferences } from "@/hooks/use-preferences";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";

const controlClass =
  "h-[var(--exits-control-height)] min-h-[var(--exits-touch-target-min)] rounded-[var(--exits-density-radius)] border border-input bg-surface px-3 text-[length:var(--exits-text-sm)] text-foreground";

function dedupeProducts(products: EnabledProduct[]): EnabledProduct[] {
  const seen = new Set<string>();
  return products.filter((product) => {
    const key = product.productId ?? product.productKey ?? product.productCode;
    if (seen.has(key)) {
      return false;
    }
    seen.add(key);
    return true;
  });
}

function isForbidden(error: unknown): boolean {
  return error instanceof PlatformApiError && (error.status === 401 || error.status === 403);
}

export function OrganizationEnabledProductsPage() {
  const { t } = usePreferences();
  const params = useParams();
  const authorization = useAuthorization();
  const organizationId = parseOrganizationId(params.organizationId);
  const canView = authorization.hasPermission(PLATFORM_PERMISSIONS.manageProductAccess);
  const canManageRoles = authorization.hasPermission(PLATFORM_PERMISSIONS.manageMemberships);
  const productsQuery = useOrganizationEnabledProductsQuery(organizationId);
  const rolesQuery = useOrganizationProductLocalRolesQuery(canManageRoles ? organizationId : null);
  const membersQuery = useOrganizationMembersQuery(canManageRoles ? organizationId : null, {
    page: 1,
    status: "Active",
  });
  const launchMutation = useLaunchProductMutation();
  const assignMutation = useAssignProductLocalRoleMutation();
  const revokeMutation = useRevokeProductLocalRoleMutation();
  const [formError, setFormError] = useState<string | null>(null);
  const [selectedStaffUserId, setSelectedStaffUserId] = useState("");
  const [selectedProductCode, setSelectedProductCode] = useState("");
  const [selectedRoleCode, setSelectedRoleCode] = useState<string>(PRODUCT_LOCAL_ROLE_CODES[0] ?? "Owner");
  const [revokeGrantId, setRevokeGrantId] = useState<string | null>(null);

  const products = useMemo(
    () => (productsQuery.data ? dedupeProducts(productsQuery.data) : []),
    [productsQuery.data],
  );

  if (!organizationId) {
    return null;
  }

  if (!canView) {
    return (
      <section className="grid max-w-3xl gap-4">
        <PageHeader
          title={t("organization.enabledProducts.title")}
          description={t("organization.enabledProducts.description")}
        />
        <Alert title={t("organization.enabledProducts.unauthorized.title")}>
          {t("organization.enabledProducts.unauthorized.body")}
        </Alert>
      </section>
    );
  }

  const diagnostic = productsQuery.error
    ? normalizeDiagnosticError({ error: productsQuery.error, operation: "Load enabled products" })
    : null;

  async function launchProduct(product: EnabledProduct) {
    if (!organizationId || launchMutation.isPending) {
      return;
    }
    setFormError(null);
    try {
      const result = await launchMutation.mutateAsync({
        organizationId,
        productCode: product.productKey ?? product.productCode,
      });
      if (result.launchPath) {
        window.location.assign(result.launchPath);
      }
    } catch (error) {
      setFormError(organizationMutationFailureCopy(error, t).detail);
    }
  }

  async function assignRole() {
    if (!organizationId || assignMutation.isPending) {
      return;
    }
    setFormError(null);
    try {
      await assignMutation.mutateAsync({
        organizationId,
        body: {
          userIdentityId: selectedStaffUserId,
          productCode: selectedProductCode,
          roleCode: selectedRoleCode,
          reason: "Assigned from Platform Admin",
        },
      });
    } catch (error) {
      setFormError(organizationMutationFailureCopy(error, t).detail);
    }
  }

  return (
    <section className="grid max-w-4xl gap-4">
      <PageHeader
        title={t("organization.enabledProducts.title")}
        description={t("organization.enabledProducts.description")}
      />
      <Alert title={t("organization.enabledProducts.warning.title")} tone="info">
        {t("organization.enabledProducts.warning.body")}
      </Alert>
      {formError ? <Alert title={t("organization.admin.mutation.error.unknown")} tone="danger">{formError}</Alert> : null}
      {productsQuery.isPending ? <DashboardWidgetSkeleton rows={4} /> : null}
      {productsQuery.isError && isForbidden(productsQuery.error) ? (
        <Alert title={t("organization.enabledProducts.unauthorized.title")}>
          {t("organization.people.unavailable")}
        </Alert>
      ) : null}
      {productsQuery.isError && !isForbidden(productsQuery.error) && diagnostic ? (
        <ErrorState
          diagnostic={diagnostic}
          title={t("organization.enabledProducts.error")}
          headingLevel="h2"
          onRetry={() => void productsQuery.refetch()}
        />
      ) : null}
      {productsQuery.data ? (
        products.length === 0 ? (
          <p className="text-[length:var(--exits-text-sm)] text-muted">{t("organization.enabledProducts.empty")}</p>
        ) : (
          <ul className="grid gap-3 sm:grid-cols-2">
            {products.map((product) => (
              <li key={product.productId ?? product.productCode}>
                <Card className="grid gap-2 p-4">
                  <p className="font-semibold">{product.productDisplayName ?? product.displayName}</p>
                  <dl className="grid gap-1 text-[length:var(--exits-text-sm)]">
                    <div>
                      <dt className="text-muted">{t("organization.enabledProducts.entitlement")}</dt>
                      <dd>
                        <StatusIndicator
                          tone={product.entitlementActive ? "success" : "warning"}
                          label={
                            product.entitlementStatus ??
                            (product.entitlementActive
                              ? t("organization.enabledProducts.entitlementEnabled")
                              : t("organization.enabledProducts.entitlementDisabled"))
                          }
                        />
                      </dd>
                    </div>
                    <div>
                      <dt className="text-muted">{t("organization.enabledProducts.role")}</dt>
                      <dd>{product.productRole ?? product.productLocalRoleCode ?? "—"}</dd>
                    </div>
                  </dl>
                  <div className="flex flex-wrap gap-2">
                    {product.canLaunch ? (
                      <Button type="button" size="sm" onClick={() => void launchProduct(product)}>
                        {t("organization.enabledProducts.launch")}
                      </Button>
                    ) : (
                      <Button type="button" size="sm" variant="outline" disabled>
                        {t("organization.enabledProducts.launchDenied")}
                      </Button>
                    )}
                  </div>
                  {!product.canLaunch && product.denialReasonDisplay ? (
                    <p className="text-[length:var(--exits-text-xs)] text-muted">{product.denialReasonDisplay}</p>
                  ) : null}
                </Card>
              </li>
            ))}
          </ul>
        )
      ) : null}
      {canManageRoles ? (
        <div className="grid gap-3 rounded-[var(--exits-density-radius)] border border-border bg-surface p-4">
          <h2 className="text-[length:var(--exits-text-sm)] font-semibold">
            {t("organization.enabledProducts.assign.section")}
          </h2>
          <div className="grid gap-3 sm:grid-cols-3">
            <div className="grid gap-1">
              <Label htmlFor="role-staff">{t("organization.enabledProducts.assign.staff")}</Label>
              <select
                id="role-staff"
                className={controlClass}
                value={selectedStaffUserId}
                onChange={(event) => setSelectedStaffUserId(event.target.value)}
              >
                <option value="">{t("organization.enabledProducts.assign.staffPlaceholder")}</option>
                {(membersQuery.data?.items ?? []).map((member) => (
                  <option key={member.id} value={member.userId}>
                    {member.displayName || member.email || member.userId}
                  </option>
                ))}
              </select>
            </div>
            <div className="grid gap-1">
              <Label htmlFor="role-product">{t("organization.enabledProducts.assign.product")}</Label>
              <select
                id="role-product"
                className={controlClass}
                value={selectedProductCode}
                onChange={(event) => setSelectedProductCode(event.target.value)}
              >
                <option value="">{t("organization.enabledProducts.assign.productPlaceholder")}</option>
                {products.map((product) => (
                  <option key={product.productCode} value={product.productCode}>
                    {product.productDisplayName ?? product.displayName}
                  </option>
                ))}
              </select>
            </div>
            <div className="grid gap-1">
              <Label htmlFor="role-code">{t("organization.enabledProducts.assign.role")}</Label>
              <select
                id="role-code"
                className={controlClass}
                value={selectedRoleCode}
                onChange={(event) => setSelectedRoleCode(event.target.value)}
              >
                {PRODUCT_LOCAL_ROLE_CODES.map((role) => (
                  <option key={role} value={role}>
                    {role}
                  </option>
                ))}
              </select>
            </div>
          </div>
          <Button type="button" size="sm" disabled={assignMutation.isPending} onClick={() => void assignRole()}>
            {t("organization.enabledProducts.assign.action")}
          </Button>
          {rolesQuery.data && rolesQuery.data.length > 0 ? (
            <ul className="grid gap-2 text-[length:var(--exits-text-sm)]">
              {rolesQuery.data.map((grant) => (
                <li
                  key={grant.id}
                  className="flex flex-wrap items-center justify-between gap-2 rounded-[var(--exits-density-radius)] border border-border px-3 py-2"
                >
                  <span>
                    {grant.userDisplayName ?? grant.userIdentityId} · {grant.productCode} ·{" "}
                    {grant.roleDisplay ?? grant.roleCode}
                  </span>
                  {grant.status === "Active" ? (
                    <Button type="button" size="sm" variant="outline" onClick={() => setRevokeGrantId(grant.id)}>
                      {t("organization.enabledProducts.revoke.action")}
                    </Button>
                  ) : null}
                </li>
              ))}
            </ul>
          ) : null}
        </div>
      ) : null}
      {revokeGrantId ? (
        <ConfirmActionDialog
          open
          title={t("organization.enabledProducts.revoke.title")}
          description={t("organization.enabledProducts.revoke.description")}
          confirmLabel={t("organization.enabledProducts.revoke.confirm")}
          cancelLabel={t("organization.admin.dialog.dismiss")}
          pendingLabel={t("organization.admin.submitting")}
          destructive
          pending={revokeMutation.isPending}
          onCancel={() => setRevokeGrantId(null)}
          onConfirm={() => {
            if (!organizationId) {
              return;
            }
            void revokeMutation
              .mutateAsync({
                organizationId,
                grantId: revokeGrantId,
                body: { reason: "Revoked from Platform Admin" },
              })
              .finally(() => setRevokeGrantId(null));
          }}
        />
      ) : null}
    </section>
  );
}
