import { useMemo, useState } from "react";
import { useParams, useSearchParams } from "react-router-dom";
import { PLATFORM_PERMISSIONS } from "@/api/authorization/authorization-types";
import { parseOrganizationId } from "@/api/organizations/organization-id";
import {
  evaluateProductAccess,
  type ProductAccessUrlState,
} from "@/api/organizations/product-access-client";
import { PRODUCT_ACCESS_PAGE_SIZE } from "@/api/organizations/organization-types";
import { PlatformApiError } from "@/api/platform-http";
import { AdminTable } from "@/components/exits/AdminTable";
import { Alert } from "@/components/ui/alert";
import { ErrorState } from "@/components/exits/ErrorState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusIndicator } from "@/components/exits/StatusIndicator";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { Button } from "@/components/ui/button";
import { ConfirmActionDialog } from "@/components/exits/ConfirmActionDialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { organizationMutationFailureCopy } from "@/features/organizations/organization-mutation-feedback";
import {
  useGrantProductAccessMutation,
  useRevokeProductAccessMutation,
} from "@/features/organizations/use-organization-mutations";
import { useOrganizationProductAccessQuery } from "@/features/organizations/use-organization-workspace-queries";
import { useAuthorization } from "@/hooks/use-authorization";
import { usePreferences } from "@/hooks/use-preferences";
import { useSession } from "@/hooks/use-session";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import { env } from "@/lib/env";

const controlClass =
  "h-[var(--exits-control-height)] min-h-[var(--exits-touch-target-min)] rounded-[var(--exits-density-radius)] border border-input bg-surface px-3 text-[length:var(--exits-text-sm)] text-foreground";

function parseProductAccessSearchParams(searchParams: URLSearchParams): ProductAccessUrlState {
  const page = Number.parseInt(searchParams.get("page") ?? "1", 10);
  const status = searchParams.get("status") ?? "";
  return {
    page: Number.isFinite(page) && page > 0 ? page : 1,
    status: status === "Active" || status === "Revoked" ? status : "",
  };
}

function productAccessSearchParams(state: ProductAccessUrlState): URLSearchParams {
  const params = new URLSearchParams();
  if (state.page > 1) {
    params.set("page", String(state.page));
  }
  if (state.status) {
    params.set("status", state.status);
  }
  return params;
}

function isForbidden(error: unknown): boolean {
  return error instanceof PlatformApiError && (error.status === 401 || error.status === 403);
}

export function OrganizationProductAccessPage() {
  const { t } = usePreferences();
  const params = useParams();
  const authorization = useAuthorization();
  const { session } = useSession();
  const organizationId = parseOrganizationId(params.organizationId);
  const canView = authorization.hasPermission(PLATFORM_PERMISSIONS.manageProductAccess);
  const canGrant = canView;
  const [searchParams, setSearchParams] = useSearchParams();
  const state = useMemo(() => parseProductAccessSearchParams(searchParams), [searchParams]);
  const query = useOrganizationProductAccessQuery(organizationId, state);
  const grantMutation = useGrantProductAccessMutation();
  const revokeMutation = useRevokeProductAccessMutation();
  const [grantUserId, setGrantUserId] = useState("");
  const [grantProductCode, setGrantProductCode] = useState("");
  const [grantReason, setGrantReason] = useState("");
  const [evalUserId, setEvalUserId] = useState("");
  const [evalProductCode, setEvalProductCode] = useState("");
  const [evalResult, setEvalResult] = useState<Awaited<ReturnType<typeof evaluateProductAccess>> | null>(
    null,
  );
  const [evalError, setEvalError] = useState<string | null>(null);
  const [evalPending, setEvalPending] = useState(false);
  const [formError, setFormError] = useState<{ title: string; detail: string } | null>(null);
  const [revokeId, setRevokeId] = useState<string | null>(null);
  const actor = session?.email ?? "platform-admin";

  if (!organizationId) {
    return null;
  }

  if (!canView) {
    return (
      <section className="grid max-w-3xl gap-4">
        <PageHeader
          title={t("organization.productAccess.title")}
          description={t("organization.productAccess.description")}
        />
        <Alert title={t("organization.productAccess.unauthorized.title")}>
          {t("organization.productAccess.unauthorized.body")}
        </Alert>
      </section>
    );
  }

  const diagnostic = query.error
    ? normalizeDiagnosticError({ error: query.error, operation: "Load product access" })
    : null;
  const totalPages = query.data
    ? Math.max(1, Math.ceil(query.data.totalCount / PRODUCT_ACCESS_PAGE_SIZE))
    : 1;

  function replaceState(patch: Partial<ProductAccessUrlState>) {
    const current = parseProductAccessSearchParams(new URLSearchParams(window.location.search));
    setSearchParams(productAccessSearchParams({ ...current, ...patch }), { replace: true });
  }

  async function grantAccess() {
    if (!organizationId || !canGrant || grantMutation.isPending) {
      return;
    }
    setFormError(null);
    try {
      await grantMutation.mutateAsync({
        organizationId,
        body: {
          userId: grantUserId.trim(),
          productCode: grantProductCode.trim(),
          grantedByActor: actor,
          reason: grantReason.trim() || null,
        },
      });
      setGrantUserId("");
      setGrantProductCode("");
      setGrantReason("");
    } catch (error) {
      setFormError(organizationMutationFailureCopy(error, t));
    }
  }

  async function evaluateAccess() {
    if (!organizationId || evalPending) {
      return;
    }
    setEvalError(null);
    setEvalResult(null);
    setEvalPending(true);
    try {
      const result = await evaluateProductAccess(env.platformApiBaseUrl, {
        userId: evalUserId.trim(),
        organizationId,
        productCode: evalProductCode.trim(),
      });
      setEvalResult(result);
    } catch (error) {
      setEvalError(organizationMutationFailureCopy(error, t).detail);
    } finally {
      setEvalPending(false);
    }
  }

  return (
    <section className="grid max-w-4xl gap-4">
      <PageHeader
        title={t("organization.productAccess.title")}
        description={t("organization.productAccess.description")}
      />
      <Alert title={t("organization.productAccess.warning.title")} tone="info">
        {t("organization.productAccess.warning.body")}
      </Alert>
      {formError ? (
        <Alert title={formError.title} tone="danger">
          {formError.detail}
        </Alert>
      ) : null}
      {canGrant ? (
        <div className="grid gap-3 rounded-[var(--exits-density-radius)] border border-border bg-surface p-4">
          <h2 className="text-[length:var(--exits-text-sm)] font-semibold">
            {t("organization.productAccess.grant.section")}
          </h2>
          <div className="grid gap-3 sm:grid-cols-2">
            <div className="grid gap-1">
              <Label htmlFor="grant-user">{t("organization.productAccess.grant.userId")}</Label>
              <Input id="grant-user" value={grantUserId} onChange={(event) => setGrantUserId(event.target.value)} />
            </div>
            <div className="grid gap-1">
              <Label htmlFor="grant-product">{t("organization.productAccess.grant.productCode")}</Label>
              <Input
                id="grant-product"
                value={grantProductCode}
                onChange={(event) => setGrantProductCode(event.target.value)}
              />
            </div>
            <div className="grid gap-1 sm:col-span-2">
              <Label htmlFor="grant-reason">{t("organization.productAccess.grant.reason")}</Label>
              <Input id="grant-reason" value={grantReason} onChange={(event) => setGrantReason(event.target.value)} />
            </div>
          </div>
          <Button type="button" size="sm" disabled={grantMutation.isPending} onClick={() => void grantAccess()}>
            {t("organization.productAccess.grant.action")}
          </Button>
        </div>
      ) : (
        <p className="text-[length:var(--exits-text-sm)] text-muted">{t("organization.productAccess.readOnly")}</p>
      )}
      <div className="grid gap-3 rounded-[var(--exits-density-radius)] border border-border bg-surface p-4">
        <h2 className="text-[length:var(--exits-text-sm)] font-semibold">
          {t("organization.productAccess.evaluate.section")}
        </h2>
        <div className="grid gap-3 sm:grid-cols-2">
          <div className="grid gap-1">
            <Label htmlFor="eval-user">{t("organization.productAccess.evaluate.userId")}</Label>
            <Input id="eval-user" value={evalUserId} onChange={(event) => setEvalUserId(event.target.value)} />
          </div>
          <div className="grid gap-1">
            <Label htmlFor="eval-product">{t("organization.productAccess.evaluate.productCode")}</Label>
            <Input
              id="eval-product"
              value={evalProductCode}
              onChange={(event) => setEvalProductCode(event.target.value)}
            />
          </div>
        </div>
        <Button type="button" size="sm" variant="secondary" disabled={evalPending} onClick={() => void evaluateAccess()}>
          {t("organization.productAccess.evaluate.action")}
        </Button>
        {evalError ? <Alert title={t("organization.productAccess.evaluate.error")} tone="danger">{evalError}</Alert> : null}
        {evalResult ? (
          <dl className="grid gap-1 text-[length:var(--exits-text-sm)]">
            <div>
              <dt className="text-muted">{t("organization.productAccess.evaluate.allowed")}</dt>
              <dd>{evalResult.allowed ? t("organization.productAccess.evaluate.yes") : t("organization.productAccess.evaluate.no")}</dd>
            </div>
            <div>
              <dt className="text-muted">{t("organization.productAccess.evaluate.reason")}</dt>
              <dd>{evalResult.reasonCode}</dd>
            </div>
          </dl>
        ) : null}
      </div>
      <label className="grid max-w-xs gap-1 text-[length:var(--exits-text-xs)] font-medium text-muted">
        {t("organization.people.status")}
        <select
          className={controlClass}
          value={state.status}
          onChange={(event) =>
            replaceState({
              status: event.target.value as ProductAccessUrlState["status"],
              page: 1,
            })
          }
        >
          <option value="">{t("organization.people.status.all")}</option>
          <option value="Active">{t("organization.productAccess.status.Active")}</option>
          <option value="Revoked">{t("organization.productAccess.status.Revoked")}</option>
        </select>
      </label>
      {query.isPending ? (
        <DashboardWidgetSkeleton rows={5} />
      ) : null}
      {query.isError && isForbidden(query.error) ? (
        <Alert title={t("organization.productAccess.unauthorized.title")}>{t("organization.people.unavailable")}</Alert>
      ) : null}
      {query.isError && !isForbidden(query.error) && diagnostic ? (
        <ErrorState
          diagnostic={diagnostic}
          title={t("organization.productAccess.error")}
          headingLevel="h2"
          onRetry={() => void query.refetch()}
        />
      ) : null}
      {query.data ? (
        <>
          <AdminTable
            caption={t("organization.productAccess.caption")}
            empty={t("organization.productAccess.empty")}
            columns={[
              {
                id: "user",
                header: t("organization.productAccess.column.user"),
                cell: (item) => <span className="font-mono text-[length:var(--exits-text-xs)]">{item.userId}</span>,
              },
              {
                id: "product",
                header: t("organization.productAccess.column.product"),
                cell: (item) => item.productCode,
              },
              {
                id: "status",
                header: t("organization.people.column.status"),
                cell: (item) => (
                  <StatusIndicator
                    tone={item.status === "Active" ? "success" : "danger"}
                    label={item.status}
                  />
                ),
              },
              {
                id: "actions",
                header: t("organization.productAccess.column.actions"),
                cell: (item) =>
                  item.status === "Active" && canGrant ? (
                    <Button type="button" size="sm" variant="outline" onClick={() => setRevokeId(item.id)}>
                      {t("organization.productAccess.revoke.action")}
                    </Button>
                  ) : (
                    "—"
                  ),
              },
            ]}
            rows={query.data.items}
          />
          <div className="flex flex-wrap items-center gap-2">
            <Button
              type="button"
              size="sm"
              variant="outline"
              disabled={state.page <= 1}
              onClick={() => replaceState({ page: state.page - 1 })}
            >
              {t("organizations.previous")}
            </Button>
            <p className="text-[length:var(--exits-text-xs)] text-muted">
              {t("organizations.page")} {state.page} / {totalPages}
            </p>
            <Button
              type="button"
              size="sm"
              variant="outline"
              disabled={state.page >= totalPages}
              onClick={() => replaceState({ page: state.page + 1 })}
            >
              {t("organizations.next")}
            </Button>
          </div>
        </>
      ) : null}
      {revokeId ? (
        <ConfirmActionDialog
          open
          title={t("organization.productAccess.revoke.title")}
          description={t("organization.productAccess.revoke.description")}
          confirmLabel={t("organization.productAccess.revoke.confirm")}
          cancelLabel={t("organization.admin.dialog.dismiss")}
          pendingLabel={t("organization.admin.submitting")}
          destructive
          pending={revokeMutation.isPending}
          onCancel={() => setRevokeId(null)}
          onConfirm={() => {
            if (!organizationId) {
              return;
            }
            void revokeMutation
              .mutateAsync({
                organizationId,
                assignmentId: revokeId,
                body: { revokedByActor: actor, reason: "Revoked from Platform Admin" },
              })
              .finally(() => setRevokeId(null));
          }}
        />
      ) : null}
    </section>
  );
}
