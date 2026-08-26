import { useMemo, useState } from "react";
import { CheckCircle2, Inbox, PenLine, Send } from "lucide-react";
import { canManageSuppliers } from "@/access/pos-capabilities";
import { requestConnection } from "@/api/pos/pos-connected-suppliers-client";
import { PosApiError } from "@/api/pos/pos-http";
import { ExitsChipBar, type ExitsChipItem } from "@/components/exits/ExitsChipBar";
import { PageHeader } from "@/components/exits/PageHeader";
import { QrScanOrEnter } from "@/features/qr/QrScanOrEnter";
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
  const busy = !allowManage || saving;
  const hasPayload = Boolean(payload.trim());

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

  const actionItems: ExitsChipItem[] = [
    ...(allowManage
      ? [
          {
            key: "send",
            label: saving ? t("connected.sending") : t("connected.sendRequest"),
            icon: <Send />,
            emphasis: "primary" as const,
            testId: "connected-request-send",
            disabled: saving || !hasPayload,
            onSelect: () => {
              void submit();
            },
          },
        ]
      : []),
    {
      key: "incoming",
      label: t("connected.incomingRequests"),
      icon: <Inbox />,
      href: "/suppliers/connected/requests",
      testId: "connected-request-incoming",
    },
    {
      key: "manual",
      label: t("connected.enterDetailsInstead"),
      icon: <PenLine />,
      href: "/suppliers/new/manual",
      testId: "connected-request-manual",
    },
  ];

  return (
    <div className="connected-request-page flex min-w-0 flex-col gap-3" data-testid="connected-request-page">
      <PageHeader
        title={t("connected.requestTitle")}
        description={t("connected.requestHelp")}
        backTo="/suppliers/new"
        backLabel={t("suppliers.add")}
        backTestId="page-header-back-suppliers"
      />

      <ExitsChipBar
        variant="steps"
        ariaLabel={t("suppliers.addStepsAria")}
        testId="supplier-add-steps"
        items={[
          { key: "choose", label: t("suppliers.addStepChoose"), state: "done" },
          { key: "scan", label: t("connected.scanOrSearchTitle"), state: "active" },
        ]}
      />

      {error ? (
        <div className="exits-alert exits-alert--error" data-testid="connected-request-error" role="alert">
          <p className="m-0 text-[length:var(--exits-text-sm)]">{error}</p>
        </div>
      ) : null}
      {success ? (
        <div className="exits-alert exits-alert--success" data-testid="connected-request-success" role="status">
          <CheckCircle2 className="size-4 shrink-0" aria-hidden />
          <p className="m-0 text-[length:var(--exits-text-sm)]">{success}</p>
        </div>
      ) : null}

      <section className="connected-request-panel">
        <h2 className="connected-request-panel__title m-0">{t("connected.scanOrSearchTitle")}</h2>
        <p className="connected-request-panel__help m-0">{t("connected.scanOrSearchHelp")}</p>
        <QrScanOrEnter
          expectedPurpose="organization"
          disabled={busy}
          onResolvedPayload={(value) => {
            setPayload(value);
            setError(null);
            setSuccess(null);
          }}
        />

        <label className="mt-1 flex min-w-0 flex-col gap-1 text-[length:var(--exits-text-sm)]">
          <span className="font-medium">{t("connected.businessQrOrOrgId")}</span>
          <input
            className="connected-request-input min-h-11 w-full rounded-[var(--exits-radius-md)] border border-[var(--exits-border)] bg-[var(--exits-surface)] px-3 uppercase"
            data-testid="connected-request-input"
            value={payload}
            disabled={busy}
            onChange={(event) => {
              setPayload(event.target.value);
              setError(null);
              setSuccess(null);
            }}
            placeholder={t("connected.businessQrHint")}
            autoComplete="off"
            spellCheck={false}
          />
          {hasPayload ? (
            <span className="connected-request-selected" data-testid="connected-request-selected">
              {t("connected.selectedId").replace("{id}", payload.trim().toUpperCase())}
            </span>
          ) : null}
        </label>
      </section>

      <ExitsChipBar
        variant="actions"
        ariaLabel={t("connected.requestTitle")}
        testId="connected-request-actions"
        className="connected-request-actions"
        items={actionItems}
      />
    </div>
  );
}
