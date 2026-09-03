import { useMemo, useState } from "react";
import { CheckCircle2, Inbox, PenLine, Send } from "lucide-react";
import { canManageSuppliers } from "@/access/pos-capabilities";
import { requestConnection } from "@/api/pos/pos-connected-suppliers-client";
import { PosApiError } from "@/api/pos/pos-http";
import { PlatformApiError } from "@/api/platform/platform-http";
import { resolvePublicOrganizationId } from "@/api/platform/public-identity-client";
import {
  lookupPublicStoreBranches,
  type PublicStoreBranchLocationDto,
} from "@/api/platform/public-store-client";
import { ExitsChipBar, type ExitsChipItem } from "@/components/exits/ExitsChipBar";
import { PageHeader } from "@/components/exits/PageHeader";
import { QrScanOrEnter } from "@/features/qr/QrScanOrEnter";
import {
  parseConnectedSupplierScanPayload,
  type ConnectedSupplierScanResolution,
} from "@/features/suppliers/connected-supplier-scan";
import { useI18n } from "@/i18n/I18nProvider";
import { ExItsQrParseError } from "@/lib/exits-qr/envelope";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const GUID_ONLY = /^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/;

type ResolvedSupplier = {
  publicOrganizationId: string;
  organizationId: string;
  displayName: string;
  branches: PublicStoreBranchLocationDto[];
  selectedBranchId: string | null;
  fromBranchQr: boolean;
};

export function ConnectedRequestPage() {
  const { t } = useI18n();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const [payload, setPayload] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [resolving, setResolving] = useState(false);
  const [resolved, setResolved] = useState<ResolvedSupplier | null>(null);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const allowManage = canManageSuppliers(sessionGrant);
  const busy = !allowManage || saving || resolving;
  const needsBranchChoice =
    Boolean(resolved) &&
    (resolved?.branches.length ?? 0) > 1 &&
    !resolved?.fromBranchQr;
  const canSend =
    Boolean(resolved?.selectedBranchId) &&
    (!needsBranchChoice || Boolean(resolved?.selectedBranchId));

  function resetResolved() {
    setResolved(null);
  }

  async function resolveScan(raw: string) {
    if (!workspace || !allowManage || resolving) {
      return;
    }
    const trimmed = raw.trim();
    setError(null);
    setSuccess(null);
    resetResolved();
    if (!trimmed) {
      setError(t("connected.qrRequired"));
      return;
    }
    if (GUID_ONLY.test(trimmed)) {
      setError(t("connected.guidRejected"));
      return;
    }

    let scan: ConnectedSupplierScanResolution;
    try {
      scan = parseConnectedSupplierScanPayload(trimmed);
    } catch (err) {
      if (err instanceof ExItsQrParseError) {
        setError(t("connected.qrRequired"));
        return;
      }
      setError(t("connected.qrRequired"));
      return;
    }

    setPayload(scan.publicOrganizationId);
    setResolving(true);
    try {
      const org = await resolvePublicOrganizationId(scan.publicOrganizationId, "Organization");
      const locations = await lookupPublicStoreBranches(org.publicOrganizationId);
      const branches = locations.branches;
      if (branches.length === 0) {
        setError(t("connected.noActiveLocations"));
        return;
      }

      let selectedBranchId: string | null = null;
      let fromBranchQr = false;
      if (scan.supplierBranchId) {
        const match = branches.find((b) => b.branchId === scan.supplierBranchId);
        if (!match) {
          setError(t("connected.branchUnavailable"));
          return;
        }
        selectedBranchId = match.branchId;
        fromBranchQr = true;
      } else if (branches.length === 1) {
        selectedBranchId = branches[0]!.branchId;
      }

      setResolved({
        publicOrganizationId: org.publicOrganizationId,
        organizationId: org.organizationId,
        displayName: locations.displayName || org.displayName || org.publicOrganizationId,
        branches,
        selectedBranchId,
        fromBranchQr,
      });
    } catch (err) {
      if (err instanceof PlatformApiError) {
        setError(err.problem.detail ?? err.message ?? t("connected.requestFailed"));
      } else {
        setError(t("connected.requestFailed"));
      }
    } finally {
      setResolving(false);
    }
  }

  async function submit() {
    if (!workspace || !allowManage || saving || !resolved?.selectedBranchId) {
      return;
    }
    setError(null);
    setSuccess(null);
    setSaving(true);
    try {
      await requestConnection(workspace, {
        supplierPublicOrganizationIdOrQrPayload: resolved.publicOrganizationId,
        supplierOrganizationId: resolved.organizationId,
        supplierBranchId: resolved.selectedBranchId,
      });
      setSuccess(t("connected.requestSent"));
      setPayload("");
      resetResolved();
    } catch (err) {
      if (err instanceof PlatformApiError) {
        setError(err.problem.detail ?? err.message ?? t("connected.requestFailed"));
      } else if (err instanceof PosApiError) {
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
            label: saving ? t("connected.sending") : t("connected.addSupplier"),
            icon: <Send />,
            emphasis: "primary" as const,
            testId: "connected-request-send",
            disabled: saving || !canSend,
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

  const selectedBranch = resolved?.branches.find((b) => b.branchId === resolved.selectedBranchId);

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
          parseRawPayload={(raw) => {
            const parsed = parseConnectedSupplierScanPayload(raw);
            return parsed.supplierBranchId
              ? `https://local/store/${parsed.publicOrganizationId}/b/${parsed.supplierBranchId}`
              : parsed.publicOrganizationId;
          }}
          onResolvedPayload={(value) => {
            void resolveScan(value);
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
              resetResolved();
            }}
            onBlur={() => {
              if (payload.trim()) {
                void resolveScan(payload);
              }
            }}
            placeholder={t("connected.businessQrHint")}
            autoComplete="off"
            spellCheck={false}
          />
        </label>
      </section>

      {resolved ? (
        <section
          className="connected-request-panel flex flex-col gap-3"
          data-testid="connected-supplier-preview"
        >
          <div>
            <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
              {resolved.displayName}
            </h2>
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="connected-supplier-public-id">
              {resolved.publicOrganizationId}
            </p>
          </div>

          {resolved.fromBranchQr && selectedBranch ? (
            <p className="m-0 text-[length:var(--exits-text-sm)]" data-testid="connected-branch-preselected">
              {t("connected.supplierLocationLabel").replace("{name}", selectedBranch.name)}
            </p>
          ) : null}

          {needsBranchChoice ? (
            <fieldset className="m-0 flex flex-col gap-2 border-0 p-0" data-testid="connected-branch-picker">
              <legend className="mb-1 text-[length:var(--exits-text-sm)] font-medium">
                {t("connected.whichLocationSupplies")}
              </legend>
              {resolved.branches.map((branch) => (
                <label
                  key={branch.branchId}
                  className="flex min-h-11 items-center gap-2 text-[length:var(--exits-text-sm)]"
                >
                  <input
                    type="radio"
                    name="supplier-branch"
                    value={branch.branchId}
                    checked={resolved.selectedBranchId === branch.branchId}
                    onChange={() =>
                      setResolved((prev) =>
                        prev ? { ...prev, selectedBranchId: branch.branchId } : prev,
                      )
                    }
                    data-testid={`connected-branch-option-${branch.code}`}
                  />
                  <span>{branch.name}</span>
                </label>
              ))}
            </fieldset>
          ) : null}

          {!needsBranchChoice && selectedBranch && !resolved.fromBranchQr ? (
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="connected-branch-auto">
              {t("connected.supplierLocationLabel").replace("{name}", selectedBranch.name)}
            </p>
          ) : null}
        </section>
      ) : null}

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
