import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  hasOrganizationManagementAuthority,
  isOrganizationOwnerMembership,
} from "@/access/pos-capabilities";
import {
  addComplianceRegistrationRecord,
  COMPLIANCE_REGISTRATION_TYPES,
  getComplianceActivationReadiness,
  getOrganizationComplianceProfile,
  listBranchComplianceProfiles,
  listComplianceRegistrationRecords,
  listOrganizationBranches,
  submitComplianceReadinessForReview,
  updateRegisteredTaxpayer,
  upsertBranchComplianceProfile,
  type BranchComplianceProfile,
  type ComplianceRegistrationType,
  type OrganizationBranchSummary,
} from "@/api/platform/organization-compliance-client";
import { PlatformApiError } from "@/api/platform/platform-http";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { ExperienceAccessDeniedPage } from "@/features/role/ExperienceAccessDeniedPage";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function orgTaxComplianceQueryKey(organizationId: string) {
  return ["org", "tax-compliance", organizationId] as const;
}

function registrationLabelKey(type: string): MessageKey {
  switch (type) {
    case "PosPermitToUse":
      return "orgTax.reg.PosPermitToUse";
    case "CasRegistration":
      return "orgTax.reg.CasRegistration";
    case "EisCertification":
      return "orgTax.reg.EisCertification";
    case "EisPermitToTransmit":
      return "orgTax.reg.EisPermitToTransmit";
    default:
      return "orgTax.reg.Other";
  }
}

function problemText(error: unknown, fallback: string): string {
  if (error instanceof PlatformApiError) {
    return error.problem.detail ?? error.problem.title ?? fallback;
  }
  return fallback;
}

function isForbidden(error: unknown): boolean {
  return error instanceof PlatformApiError && error.status === 403;
}

type BranchRow = {
  branchId: string;
  name: string;
  code: string;
  birBranchCode: string | null;
  setupStatus: string;
  notes: string | null;
};

function joinBranches(
  branches: OrganizationBranchSummary[],
  profiles: BranchComplianceProfile[],
): BranchRow[] {
  const byBranch = new Map(profiles.map((profile) => [profile.organizationBranchId, profile]));
  return branches
    .filter((branch) => branch.status.toLowerCase() !== "archived")
    .map((branch) => {
      const profile = byBranch.get(branch.id);
      return {
        branchId: branch.id,
        name: branch.name,
        code: branch.code,
        birBranchCode: profile?.birBranchCode ?? null,
        setupStatus: profile?.setupStatus ?? "NotConfigured",
        notes: profile?.notes ?? null,
      };
    });
}

export function OrgTaxCompliancePage() {
  const { t } = useI18n();
  const queryClient = useQueryClient();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const organizationId = boundWorkspace?.organizationId ?? null;
  const canView = hasOrganizationManagementAuthority(sessionGrant);
  const canEdit = isOrganizationOwnerMembership(sessionGrant);

  const [taxpayerName, setTaxpayerName] = useState("");
  const [tin, setTin] = useState("");
  const [registrationType, setRegistrationType] =
    useState<ComplianceRegistrationType>("PosPermitToUse");
  const [referenceNumber, setReferenceNumber] = useState("");
  const [editingBranchId, setEditingBranchId] = useState<string | null>(null);
  const [branchCode, setBranchCode] = useState("");
  const [branchNotes, setBranchNotes] = useState("");
  const [actionError, setActionError] = useState<string | null>(null);
  const [actionSuccess, setActionSuccess] = useState<string | null>(null);

  const query = useQuery({
    queryKey: organizationId
      ? orgTaxComplianceQueryKey(organizationId)
      : ["org", "tax-compliance", "none"],
    enabled: Boolean(organizationId) && canView,
    queryFn: async ({ signal }) => {
      const id = organizationId!;
      const [profile, readiness, branchProfiles, registrations, branches] = await Promise.all([
        getOrganizationComplianceProfile(id, signal),
        getComplianceActivationReadiness(id, signal),
        listBranchComplianceProfiles(id, signal),
        listComplianceRegistrationRecords(id, signal),
        listOrganizationBranches(id, signal),
      ]);
      return { profile, readiness, branchProfiles, registrations, branches };
    },
  });

  useEffect(() => {
    if (!query.data) {
      return;
    }
    setTaxpayerName(query.data.profile.registeredTaxpayerName ?? "");
    setTin("");
  }, [query.data]);

  const refresh = async () => {
    if (!organizationId) return;
    await queryClient.invalidateQueries({ queryKey: orgTaxComplianceQueryKey(organizationId) });
  };

  const saveTaxpayer = useMutation({
    mutationFn: () =>
      updateRegisteredTaxpayer(organizationId!, {
        registeredTaxpayerName: taxpayerName.trim() || null,
        tin: tin.trim() || null,
      }),
    onSuccess: async () => {
      setActionError(null);
      setActionSuccess(t("orgTax.saved"));
      setTin("");
      await refresh();
    },
    onError: (error) => setActionError(problemText(error, t("orgTax.loadErrorDetail"))),
  });

  const saveBranch = useMutation({
    mutationFn: () =>
      upsertBranchComplianceProfile(organizationId!, editingBranchId!, {
        birBranchCode: branchCode.trim() || null,
        notes: branchNotes.trim() || null,
      }),
    onSuccess: async () => {
      setEditingBranchId(null);
      setActionError(null);
      setActionSuccess(t("orgTax.saved"));
      await refresh();
    },
    onError: (error) => setActionError(problemText(error, t("orgTax.loadErrorDetail"))),
  });

  const addRegistration = useMutation({
    mutationFn: () =>
      addComplianceRegistrationRecord(organizationId!, {
        registrationType,
        referenceNumber: referenceNumber.trim() || null,
      }),
    onSuccess: async () => {
      setReferenceNumber("");
      setActionError(null);
      setActionSuccess(t("orgTax.saved"));
      await refresh();
    },
    onError: (error) => setActionError(problemText(error, t("orgTax.loadErrorDetail"))),
  });

  const submitReadiness = useMutation({
    mutationFn: () => submitComplianceReadinessForReview(organizationId!),
    onSuccess: async () => {
      setActionError(null);
      setActionSuccess(t("orgTax.submitted"));
      await refresh();
    },
    onError: (error) => setActionError(problemText(error, t("orgTax.loadErrorDetail"))),
  });

  if (!organizationId) {
    return (
      <div data-testid="tax-no-organization">
        <PageHeader title={t("orgTax.title")} description={t("orgTax.subtitle")} />
        <EmptyState title={t("orgTax.noOrganization")} />
      </div>
    );
  }

  if (!canView || isForbidden(query.error)) {
    return <ExperienceAccessDeniedPage testId="tax-compliance-denied" />;
  }

  const busy =
    saveTaxpayer.isPending ||
    saveBranch.isPending ||
    addRegistration.isPending ||
    submitReadiness.isPending;

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="tax-compliance-page">
      <PageHeader title={t("orgTax.title")} description={t("orgTax.subtitle")} />
      {query.isLoading ? (
        <div data-testid="tax-loading">
          <LoadingSkeleton />
        </div>
      ) : null}
      {query.isError && !isForbidden(query.error) ? (
        <div data-testid="tax-load-error">
          <ErrorState
            title={t("orgTax.loadError")}
            detail={problemText(query.error, t("orgTax.loadErrorDetail"))}
            error={query.error}
            operation="load tax compliance"
          />
          <Button type="button" onClick={() => void query.refetch()} data-testid="tax-retry">
            {t("orgTax.retry")}
          </Button>
        </div>
      ) : null}
      {actionError ? (
        <p role="alert" data-testid="tax-action-error">
          {actionError}
        </p>
      ) : null}
      {actionSuccess ? (
        <p data-testid="tax-action-success">{actionSuccess}</p>
      ) : null}
      {query.data ? (
        <TaxComplianceBody
          data={query.data}
          canEdit={canEdit}
          busy={busy}
          taxpayerName={taxpayerName}
          tin={tin}
          registrationType={registrationType}
          referenceNumber={referenceNumber}
          editingBranchId={editingBranchId}
          branchCode={branchCode}
          branchNotes={branchNotes}
          onTaxpayerName={setTaxpayerName}
          onTin={setTin}
          onSaveTaxpayer={() => saveTaxpayer.mutate()}
          onRegistrationType={setRegistrationType}
          onReferenceNumber={setReferenceNumber}
          onAddRegistration={() => addRegistration.mutate()}
          onEditBranch={(row) => {
            setEditingBranchId(row.branchId);
            setBranchCode(row.birBranchCode ?? "");
            setBranchNotes(row.notes ?? "");
          }}
          onBranchCode={setBranchCode}
          onBranchNotes={setBranchNotes}
          onSaveBranch={() => saveBranch.mutate()}
          onCancelBranch={() => setEditingBranchId(null)}
          onSubmitReadiness={() => submitReadiness.mutate()}
          t={t}
        />
      ) : null}
    </div>
  );
}

function TaxComplianceBody({
  data,
  canEdit,
  busy,
  taxpayerName,
  tin,
  registrationType,
  referenceNumber,
  editingBranchId,
  branchCode,
  branchNotes,
  onTaxpayerName,
  onTin,
  onSaveTaxpayer,
  onRegistrationType,
  onReferenceNumber,
  onAddRegistration,
  onEditBranch,
  onBranchCode,
  onBranchNotes,
  onSaveBranch,
  onCancelBranch,
  onSubmitReadiness,
  t,
}: {
  data: {
    profile: {
      registeredTaxpayerName: string | null;
      maskedTin: string | null;
      setupStatus: string;
      complianceEligibilityStatus: string;
      taxConfigurationEnabled: boolean;
    };
    readiness: {
      overallStatus: string;
      isReadyForTaxDocumentActivation: boolean;
      blockingReasons: string[];
      checklist: { code: string; label: string; done: boolean }[];
    };
    branches: OrganizationBranchSummary[];
    branchProfiles: BranchComplianceProfile[];
    registrations: {
      id: string;
      registrationType: string;
      referenceNumber: string | null;
      status: string;
      recordedAtUtc: string;
    }[];
  };
  canEdit: boolean;
  busy: boolean;
  taxpayerName: string;
  tin: string;
  registrationType: ComplianceRegistrationType;
  referenceNumber: string;
  editingBranchId: string | null;
  branchCode: string;
  branchNotes: string;
  onTaxpayerName: (value: string) => void;
  onTin: (value: string) => void;
  onSaveTaxpayer: () => void;
  onRegistrationType: (value: ComplianceRegistrationType) => void;
  onReferenceNumber: (value: string) => void;
  onAddRegistration: () => void;
  onEditBranch: (row: BranchRow) => void;
  onBranchCode: (value: string) => void;
  onBranchNotes: (value: string) => void;
  onSaveBranch: () => void;
  onCancelBranch: () => void;
  onSubmitReadiness: () => void;
  t: (key: MessageKey) => string;
}) {
  const done = data.readiness.checklist.filter((item) => item.done).length;
  const rows = joinBranches(data.branches, data.branchProfiles);
  return (
    <>
      <Card>
        <CardHeader>
          <CardTitle>{t("orgTax.readinessTitle")}</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-col gap-2">
          <StatusChip data-testid="tax-overall-status">{data.readiness.overallStatus}</StatusChip>
          <p data-testid="tax-progress">
            {t("orgTax.progress")
              .replace("{done}", String(done))
              .replace("{total}", String(data.readiness.checklist.length))}
          </p>
        </CardContent>
      </Card>
      {data.readiness.blockingReasons.length > 0 ? (
        <Card data-testid="tax-blocking">
          <CardHeader>
            <CardTitle>{t("orgTax.blockingTitle")}</CardTitle>
          </CardHeader>
          <CardContent>
            <ul>
              {data.readiness.blockingReasons.map((reason) => (
                <li key={reason}>{reason}</li>
              ))}
            </ul>
          </CardContent>
        </Card>
      ) : null}
      <Card>
        <CardHeader>
          <CardTitle>{t("orgTax.businessRegistration")}</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-col gap-3">
          <p>
            {t("orgTax.registeredName")}: {data.profile.registeredTaxpayerName ?? "—"}
          </p>
          <p data-testid="tax-masked-tin">
            {t("orgTax.maskedTin")}: {data.profile.maskedTin ?? "—"}
          </p>
          <p>
            {t("orgTax.setupStatus")}: {data.profile.setupStatus}
          </p>
          {canEdit ? (
            <form
              className="flex flex-col gap-3"
              onSubmit={(event) => {
                event.preventDefault();
                onSaveTaxpayer();
              }}
            >
              <Input
                label={t("orgTax.registeredName")}
                value={taxpayerName}
                disabled={busy}
                onChange={(event) => onTaxpayerName(event.target.value)}
              />
              <Input
                label={t("orgTax.tinInput")}
                value={tin}
                disabled={busy}
                placeholder={t("orgTax.tinPlaceholder")}
                autoComplete="off"
                data-testid="tax-tin-input"
                onChange={(event) => onTin(event.target.value)}
              />
              <p className="text-[length:var(--exits-text-sm)] text-muted">{t("orgTax.tinPrivacy")}</p>
              <Button type="submit" disabled={busy} data-testid="tax-save-taxpayer">
                {t("orgTax.save")}
              </Button>
            </form>
          ) : null}
        </CardContent>
      </Card>
      <Card>
        <CardHeader>
          <CardTitle>{t("orgTax.branches")}</CardTitle>
        </CardHeader>
        <CardContent>
          {rows.length === 0 ? (
            <EmptyState title={t("orgTax.branchesEmpty")} />
          ) : (
            <div className="min-w-0 overflow-x-auto">
              <table className="w-full text-start">
                <thead>
                  <tr>
                    <th>{t("orgTax.branchName")}</th>
                    <th>{t("orgTax.birBranchCode")}</th>
                    <th>{t("orgTax.setupStatus")}</th>
                    {canEdit ? <th /> : null}
                  </tr>
                </thead>
                <tbody>
                  {rows.map((row) => (
                    <tr key={row.branchId} data-testid="tax-branch-row">
                      <td>
                        {row.name} ({row.code})
                      </td>
                      <td>{row.birBranchCode ?? "—"}</td>
                      <td>{row.setupStatus}</td>
                      {canEdit ? (
                        <td>
                          <Button
                            type="button"
                            disabled={busy}
                            onClick={() => onEditBranch(row)}
                            data-testid={`tax-edit-branch-${row.branchId}`}
                          >
                            {t("orgTax.edit")}
                          </Button>
                        </td>
                      ) : null}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
          {canEdit && editingBranchId ? (
            <form
              className="mt-3 flex flex-col gap-3"
              data-testid="tax-branch-form"
              onSubmit={(event) => {
                event.preventDefault();
                onSaveBranch();
              }}
            >
              <Input
                label={t("orgTax.birBranchCode")}
                value={branchCode}
                disabled={busy}
                onChange={(event) => onBranchCode(event.target.value)}
              />
              <Input
                label={t("orgTax.branchNotes")}
                value={branchNotes}
                disabled={busy}
                onChange={(event) => onBranchNotes(event.target.value)}
              />
              <div className="flex flex-wrap gap-2">
                <Button type="submit" disabled={busy} data-testid="tax-save-branch">
                  {t("orgTax.save")}
                </Button>
                <Button type="button" variant="ghost" disabled={busy} onClick={onCancelBranch}>
                  {t("orgTax.cancel")}
                </Button>
              </div>
            </form>
          ) : null}
        </CardContent>
      </Card>
      <Card>
        <CardHeader>
          <CardTitle>{t("orgTax.registrations")}</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-col gap-3">
          {data.registrations.length === 0 ? (
            <EmptyState title={t("orgTax.registrationsEmpty")} />
          ) : (
            <div className="min-w-0 overflow-x-auto">
              <table className="w-full text-start">
                <thead>
                  <tr>
                    <th>{t("orgTax.registrationType")}</th>
                    <th>{t("orgTax.referenceNumber")}</th>
                    <th>{t("orgTax.status")}</th>
                    <th>{t("orgTax.recordedAt")}</th>
                  </tr>
                </thead>
                <tbody>
                  {data.registrations.map((record) => (
                    <tr key={record.id} data-testid="tax-registration-row">
                      <td>{t(registrationLabelKey(record.registrationType))}</td>
                      <td>{record.referenceNumber ?? "—"}</td>
                      <td>{record.status}</td>
                      <td>
                        {record.recordedAtUtc
                          ? new Date(record.recordedAtUtc).toLocaleString()
                          : "—"}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
          {canEdit ? (
            <form
              className="flex flex-col gap-3"
              onSubmit={(event) => {
                event.preventDefault();
                onAddRegistration();
              }}
            >
              <label className="flex flex-col gap-1.5">
                {t("orgTax.registrationType")}
                <select
                  value={registrationType}
                  disabled={busy}
                  onChange={(event) =>
                    onRegistrationType(event.target.value as ComplianceRegistrationType)
                  }
                >
                  {COMPLIANCE_REGISTRATION_TYPES.map((type) => (
                    <option key={type} value={type}>
                      {t(registrationLabelKey(type))}
                    </option>
                  ))}
                </select>
              </label>
              <Input
                label={t("orgTax.referenceNumber")}
                value={referenceNumber}
                disabled={busy}
                onChange={(event) => onReferenceNumber(event.target.value)}
              />
              <Button type="submit" disabled={busy} data-testid="tax-add-registration">
                {t("orgTax.addRegistration")}
              </Button>
            </form>
          ) : null}
        </CardContent>
      </Card>
      <Card>
        <CardHeader>
          <CardTitle>{t("orgTax.exitsStatus")}</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-col gap-3">
          <p data-testid="tax-eligibility">{data.profile.complianceEligibilityStatus}</p>
          <p data-testid="tax-configuration">
            {data.profile.taxConfigurationEnabled
              ? t("orgTax.taxConfigurationOn")
              : t("orgTax.taxConfigurationOff")}
          </p>
          <p data-testid="tax-issuance">{t("orgTax.issuanceUnavailable")}</p>
          <p data-testid="tax-activation">
            {data.readiness.isReadyForTaxDocumentActivation
              ? t("orgTax.readyYes")
              : t("orgTax.readyNo")}
          </p>
          <ul data-testid="tax-checklist">
            {data.readiness.checklist.map((item) => (
              <li key={item.code || item.label}>
                {item.label}: {item.done ? t("orgTax.done") : t("orgTax.pending")}
              </li>
            ))}
          </ul>
          {canEdit ? (
            <Button
              type="button"
              disabled={busy}
              onClick={onSubmitReadiness}
              data-testid="tax-submit-readiness"
            >
              {t("orgTax.submitReadiness")}
            </Button>
          ) : null}
          <Link to="/org/documents-printing">{t("orgTax.documentsLink")}</Link>
          <Link to="/org/sales-documents" data-testid="tax-sales-documents-link">
            {t("orgTax.salesDocumentsLink")}
          </Link>
        </CardContent>
      </Card>
    </>
  );
}
