import { useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useQueryClient } from "@tanstack/react-query";
import { Building2, CheckCircle2, Send } from "lucide-react";
import {
  inviteBusinessCustomerConnection,
  listBusinessCustomers,
  listRelationships,
} from "@/api/pos/pos-connected-suppliers-client";
import { listCustomers } from "@/api/pos/pos-customers-client";
import { PosApiError } from "@/api/pos/pos-http";
import { resolvePublicOrganizationId } from "@/api/platform/public-identity-client";
import { PlatformApiError } from "@/api/platform/platform-http";
import { Button } from "@/components/ui/button";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { useToast } from "@/components/exits/ToastProvider";
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
  existingConnectionStatus: string | null;
  existingActionRequired: boolean;
  existingCustomerId: string | null;
};

/**
 * Invite an ExItS Organization as a Business Customer (seller-initiated Pending).
 * Never creates a POSCustomer or an immediately Active relationship.
 */
export function CustomerBusinessOrgConnectPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { showToast } = useToast();
  const { boundWorkspace } = useWorkspace();
  const [error, setError] = useState<string | null>(null);
  const [resolving, setResolving] = useState(false);
  const [sending, setSending] = useState(false);
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
        existingConnectionStatus: existingConnection?.relationshipStatus ?? null,
        existingActionRequired: existingConnection?.actionRequired ?? false,
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
      if (resolved.existingActionRequired) {
        navigate("/suppliers/connected/requests", { replace: true });
        return;
      }
      navigate(`/customers/business/${resolved.existingConnectionId}`, { replace: true });
      return;
    }
    if (resolved.existingCustomerId) {
      navigate(`/customers/${resolved.existingCustomerId}`, { replace: true });
      return;
    }

    setSending(true);
    setError(null);
    try {
      await inviteBusinessCustomerConnection(workspace, {
        buyerPublicOrganizationIdOrQrPayload: resolved.publicOrganizationId,
        buyerOrganizationId: resolved.organizationId,
        supplierBranchId: workspace.branchId,
      });
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ["connected-suppliers"] }),
        queryClient.invalidateQueries({ queryKey: ["business-customers"] }),
        queryClient.invalidateQueries({ queryKey: ["customers"] }),
        queryClient.invalidateQueries({ queryKey: ["checkout-customers"] }),
        queryClient.invalidateQueries({ queryKey: ["organization", "notifications"] }),
      ]);
      showToast(t("customers.orgInviteSent"), "success");
      navigate("/customers?kind=businesses", { replace: true });
    } catch (err) {
      if (err instanceof PosApiError) {
        const code = err.errorCode ?? "";
        if (code.includes("pending_buyer_request_exists")) {
          setError(t("customers.orgPendingBuyerRequest"));
          return;
        }
        if (code.includes("duplicate")) {
          setError(err.problem.detail ?? t("customers.orgInviteAlreadyPending"));
          return;
        }
        setError(err.problem.detail ?? err.message);
        return;
      }
      setError(err instanceof Error ? err.message : t("error.detail"));
    } finally {
      setSending(false);
    }
  }

  const statusLower = resolved?.existingConnectionStatus?.toLowerCase() ?? "";
  const actionLabel = resolved?.existingConnectionId
    ? resolved.existingActionRequired
      ? t("customers.orgReviewIncomingRequest")
      : statusLower === "pending"
        ? t("customers.orgOpenPendingConnection")
        : t("customers.orgOpenExistingConnection")
    : resolved?.existingCustomerId
      ? t("customers.orgOpenExistingCustomer")
      : t("customers.orgSendConnectionRequest");

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
        disabled={resolving || sending}
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

      {error && error === t("customers.orgPendingBuyerRequest") ? (
        <Button
          type="button"
          variant="outline"
          data-testid="customer-org-review-incoming"
          onClick={() => navigate("/suppliers/connected/requests")}
        >
          {t("customers.orgReviewIncomingRequest")}
        </Button>
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
                {resolved.existingConnectionId ? (
                  <StatusChip tone={statusLower === "active" ? "success" : "warning"}>
                    {resolved.existingConnectionStatus ?? t("customers.badge.connected")}
                  </StatusChip>
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
            disabled={sending}
            onClick={() => void connectOrOpen()}
          >
            {resolved.existingConnectionId || resolved.existingCustomerId ? (
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
