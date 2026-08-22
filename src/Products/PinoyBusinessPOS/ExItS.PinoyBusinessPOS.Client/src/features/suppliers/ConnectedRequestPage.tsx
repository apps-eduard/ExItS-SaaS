import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { canManageSuppliers } from "@/access/pos-capabilities";
import { requestConnection } from "@/api/pos/pos-connected-suppliers-client";
import { PosApiError } from "@/api/pos/pos-http";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { PageHeader } from "@/components/exits/PageHeader";
import { pageBackNav } from "@/navigation/page-back-nav";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const GUID_ONLY = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;

export function ConnectedRequestPage() {
  const { t } = useI18n();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const [payload, setPayload] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const allowManage = canManageSuppliers(sessionGrant);

  async function submit() {
    if (!workspace || !allowManage || saving) {
      return;
    }
    const trimmed = payload.trim();
    setError(null);
    setSuccess(null);
    if (!trimmed) {
      setError(t("connected.qrRequired"));
      return;
    }
    if (GUID_ONLY.test(trimmed)) {
      setError(t("connected.guidRejected"));
      return;
    }
    setSaving(true);
    try {
      await requestConnection(workspace, {
        supplierPublicOrganizationIdOrQrPayload: trimmed,
      });
      setSuccess(t("connected.requestSent"));
      setPayload("");
    } catch (err) {
      if (err instanceof PosApiError) {
        setError(err.problem.detail ?? err.message ?? t("connected.requestFailed"));
      } else {
        setError(t("connected.requestFailed"));
      }
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="connected-request-page">
      <PageHeader
        title={t("connected.requestTitle")}
        description={t("connected.requestHelp")}
        backTo={pageBackNav.suppliers.to}
        backLabel={t("connected.backToSuppliers")}
        backTestId="page-header-back-suppliers"
      />
      {error ? (
        <Card data-testid="connected-request-error">
          <p className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]">
            {error}
          </p>
        </Card>
      ) : null}
      {success ? (
        <Card data-testid="connected-request-success">
          <p className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-success)]">
            {success}
          </p>
        </Card>
      ) : null}
      <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
        <span>{t("connected.businessQrOrOrgId")}</span>
        <input
          className="min-h-11 rounded-[var(--exits-radius-md)] border border-[var(--exits-border)] bg-[var(--exits-surface)] px-3"
          data-testid="connected-request-input"
          value={payload}
          disabled={!allowManage || saving}
          onChange={(event) => setPayload(event.target.value)}
          placeholder={t("connected.businessQrHint")}
        />
      </label>
      <div className="flex flex-wrap gap-2">
        {allowManage ? (
          <Button
            type="button"
            className="min-h-11"
            data-testid="connected-request-send"
            disabled={saving}
            onClick={() => void submit()}
          >
            {saving ? t("connected.sending") : t("connected.sendRequest")}
          </Button>
        ) : null}
        <Button asChild variant="ghost" className="min-h-11">
          <Link to="/suppliers/connected/requests">{t("connected.incomingRequests")}</Link>
        </Button>
      </div>
    </div>
  );
}
