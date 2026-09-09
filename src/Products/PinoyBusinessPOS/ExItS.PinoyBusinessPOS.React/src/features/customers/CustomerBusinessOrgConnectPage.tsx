import { useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Building2, CheckCircle2, Loader2, Send } from "lucide-react";
import {
  listBusinessCustomers,
  listRelationships,
} from "@/api/pos/pos-connected-suppliers-client";
import { createCustomer, listCustomers } from "@/api/pos/pos-customers-client";
import { PosApiError } from "@/api/pos/pos-http";
import { resolvePublicOrganizationId } from "@/api/platform/public-identity-client";
import { PlatformApiError } from "@/api/platform/platform-http";
import { Button } from "@/components/ui/button";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { QrScanOrEnter } from "@/features/qr/QrScanOrEnter";
import { parseConnectedSupplierScanPayload } from "@/features/suppliers/connected-supplier-scan";
import { useI18n } from "@/i18n/I18nProvider";
import { ExItsQrParseError } from "@/lib/exits-qr/envelope";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const GUID_ONLY = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;

type ResolvedBuyerOrg = {
  publicOrganizationId: string;
  organizationId: string;
  displayName: string;
  alsoSupplier: boolean;
  existingConnectionId: string | null;
  existingCustomerId: string | null;
};

/**
 * Find/connect an ExItS Organization as a Business Customer.
 * Reuses canonical org identity; never duplicates an existing POS customer or connection.
 */
export function CustomerBusinessOrgConnectPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { boundWorkspace } = useWorkspace();
  const [error, setError] = useState<string | null>(null);
  const [resolving, setResolving] = useState(false);
  const [saving, setSaving] = useState(false);
  const [resolved, setResolved] = useState<ResolvedBuyerOrg | null>(null);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  async function resolveOrg(raw: string) {
    const trimmed = raw.trim();
    setError(null);
    setResolved(null);
    if (!trimmed) {
      setError(t("customers.orgIdRequired"));
      return;
    }
    if (GUID_ONLY.test(trimmed)) {
      setError(t("connected.guidRejected"));
      return;
    }

    let publicOrganizationId = trimmed;
    try {
      const scan = parseConnectedSupplierScanPayload(trimmed);
      publicOrganizationId = scan.publicOrganizationId;
    } catch (err) {
      if (!(err instanceof ExItsQrParseError) && !/^ORG\d{6}$/i.test(trimmed)) {
        setError(t("customers.orgIdRequired"));
        return;
      }
      if (!/^ORG\d{6}$/i.test(trimmed) && !/^ORG/i.test(trimmed)) {
        // bare typed ORG id still allowed
      }
    }

    setResolving(true);
    try {
      const org = await resolvePublicOrganizationId(publicOrganizationId, "Organization");
      if (org.organizationId.toLowerCase() === workspace!.organizationId.toLowerCase()) {
        setError(t("customers.orgSelfRejected"));
        return;
      }

      const [connections, suppliers, customersPage] = await Promise.all([
        listBusinessCustomers(workspace!),
        listRelationships(workspace!, "buyer"),
        listCustomers(workspace!, { pageSize: 100, status: "Active" }),
      ]);

      const existingConnection =
        connections.find(
          (c) => c.buyerOrganizationId.toLowerCase() === org.organizationId.toLowerCase(),
        ) ?? null;
      const existingCustomer =
        customersPage.items.find(
          (c) =>
            c.linkedBuyerOrganizationId?.toLowerCase() === org.organizationId.toLowerCase(),
        ) ?? null;
      const alsoSupplier = suppliers.some(
        (r) =>
          r.status === "Active" &&
          r.supplierOrganizationId.toLowerCase() === org.organizationId.toLowerCase(),
      );

      setResolved({
        publicOrganizationId: org.publicOrganizationId,
        organizationId: org.organizationId,
        displayName: org.displayName,
        alsoSupplier,
        existingConnectionId: existingConnection?.connectionId ?? null,
        existingCustomerId: existingCustomer?.customerId ?? null,
      });
    } catch (err) {
      setError(
        err instanceof PlatformApiError || err instanceof PosApiError
          ? err.message
          : err instanceof Error
            ? err.message
            : t("error.detail"),
      );
    } finally {
      setResolving(false);
    }
  }

  async function connectOrOpen() {
    if (!resolved || !workspace) return;

    if (resolved.existingConnectionId) {
      navigate(`/customers/business/${resolved.existingConnectionId}`, { replace: true });
      return;
    }
    if (resolved.existingCustomerId) {
      navigate(`/customers/${resolved.existingCustomerId}`, { replace: true });
      return;
    }

    setSaving(true);
    setError(null);
    try {
      const created = await createCustomer(workspace, {
        displayName: resolved.displayName,
        partyKind: "Business",
        linkedBuyerOrganizationId: resolved.organizationId,
        linkedBuyerPublicOrganizationId: resolved.publicOrganizationId,
      });
      navigate(`/customers/${created.customerId}`, { replace: true });
    } catch (err) {
      setError(
        err instanceof PosApiError
          ? err.message
          : err instanceof Error
            ? err.message
            : t("error.detail"),
      );
    } finally {
      setSaving(false);
    }
  }

  const actionLabel = resolved?.existingConnectionId
    ? t("customers.orgOpenExistingConnection")
    : resolved?.existingCustomerId
      ? t("customers.orgOpenExistingCustomer")
      : t("customers.orgConnectAsCustomer");

  return (
    <div
      className="flex min-w-0 flex-col gap-4"
      data-testid="customer-business-org-connect"
    >
      <PageHeader
        title={t("customers.addBusinessOrganization")}
        description={t("customers.addBusinessOrganizationLede")}
        backTo="/customers/new/business"
        backLabel={t("customers.addBusiness")}
        backTestId="page-header-back-customer-business"
      />

      <ExitsChipBar
        variant="steps"
        ariaLabel={t("customers.addStepsAria")}
        testId="customer-org-connect-steps"
        items={[
          { key: "choose", label: t("customers.addStepChoose"), state: "done" },
          { key: "business", label: t("customers.addStepBusiness"), state: "done" },
          { key: "connect", label: t("customers.addStepConnect"), state: "active" },
        ]}
      />

      <QrScanOrEnter
        expectedPurpose="organization"
        disabled={resolving || saving}
        parseRawPayload={(raw) => {
          const parsed = parseConnectedSupplierScanPayload(raw);
          return parsed.publicOrganizationId;
        }}
        onResolvedPayload={(value) => {
          void resolveOrg(value);
        }}
        onManualCleared={() => {
          setResolved(null);
          setError(null);
        }}
      />

      {error ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]" role="alert">
          {error}
        </p>
      ) : null}

      {resolved ? (
        <section
          className="catalog-form-section exits-animate-panel gap-3"
          data-testid="customer-org-resolved"
        >
          <div className="flex items-start gap-3">
            <span className="flex size-10 shrink-0 items-center justify-center rounded-full bg-[color-mix(in_srgb,var(--exits-border)_60%,transparent)] text-muted">
              <Building2 className="size-5" aria-hidden />
            </span>
            <div className="min-w-0 flex-1">
              <p className="m-0 font-semibold">{resolved.displayName}</p>
              <p className="mb-0 mt-1 font-mono text-[length:var(--exits-text-sm)] text-muted">
                {resolved.publicOrganizationId}
              </p>
              <div className="mt-2 flex flex-wrap gap-2">
                <StatusChip tone="info">{t("customers.badge.exitsOrganization")}</StatusChip>
                {resolved.existingConnectionId || resolved.existingCustomerId ? (
                  <StatusChip tone="success">{t("customers.badge.connected")}</StatusChip>
                ) : null}
                {resolved.alsoSupplier ? (
                  <StatusChip tone="warning">{t("customers.badge.alsoSupplier")}</StatusChip>
                ) : null}
              </div>
            </div>
          </div>

          <Button
            type="button"
            data-testid="customer-org-connect-submit"
            disabled={saving}
            onClick={() => void connectOrOpen()}
          >
            {saving ? (
              <Loader2 className="size-4 animate-spin" aria-hidden />
            ) : resolved.existingConnectionId || resolved.existingCustomerId ? (
              <CheckCircle2 className="size-4" aria-hidden />
            ) : (
              <Send className="size-4" aria-hidden />
            )}
            {actionLabel}
          </Button>
        </section>
      ) : null}
    </div>
  );
}
