import { useEffect, useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  AlertTriangle,
  Ban,
  CalendarDays,
  CheckCircle2,
  CircleDollarSign,
  FileText,
  History,
  Pencil,
  Receipt,
  Settings2,
  Wallet,
} from "lucide-react";
import { Link } from "react-router-dom";
import { ActorAttribution } from "@/features/actors/ActorAttribution";
import { useActorDirectory } from "@/features/actors/useActorDirectory";
import {
  creditPolicyConfigureActionLabelKey,
  creditPolicyStatusLabelKey,
  creditPolicyStatusTone,
  outstandingExceedsNewLimit,
  termDaysHelperLabelKey,
} from "@/features/customers/credit-policy";
import {
  type CreditPolicyDialogMode,
  EditCreditTermsModal,
} from "@/features/customers/EditCreditTermsModal";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { LoadingState } from "@/components/exits/LoadingState";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { StatusChip } from "@/components/exits/StatusChip";
import { useI18n } from "@/i18n/I18nProvider";
import {
  formatMoneyAmountInput,
  parseMoneyAmountInput,
} from "@/lib/money-input";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";

type SharedPolicy = {
  status?: string | null;
  creditLimit?: number | null;
  defaultTermDays?: number | null;
  outstandingAmount?: number | null;
  availableCredit?: number | null;
  approvedByUserId?: string | null;
  approvedAtUtc?: string | null;
  expectedUpdatedAtUtc?: string | null;
};

type SharedPolicyHistoryItem = {
  changeId: string;
  action: string;
  previousStatus?: string | null;
  newStatus: string;
  newCreditLimit?: number | null;
  newTermDays?: number | null;
  actorUserId: string;
  reason?: string | null;
  changedAtUtc: string;
};

type SharedPolicyHistoryPaged = {
  items: SharedPolicyHistoryItem[];
  totalCount: number;
  pageSize: number;
};

type SharedUtangSummary = {
  pendingCheckAmount: number;
};

type PersonalProps = {
  kind: "personal";
  customerId: string;
  connectionId?: never;
  testIdPrefix: "customer";
};

type BusinessProps = {
  kind: "business";
  connectionId: string;
  customerId?: never;
  testIdPrefix: "business";
};

type CreditTermsSectionProps = (PersonalProps | BusinessProps) & {
  workspace: PosWorkspaceScope;
  online: boolean;
  canManage: boolean;
  canApprove: boolean;
  canRecordPayment?: boolean;
  canViewStatement?: boolean;
  onRecordPayment?: () => void;
  subjectIdentity?: string | null;
  policyOverride?: SharedPolicy | null;
  titleKey: string;
  hintKeys: {
    notApproved: string;
    pending: string;
    disabled: string;
  };
  checkoutNoteKey: string;
  sectionQueryPrefix: "customers" | "business-customers";
  statementPath: (id: string) => string;
  getPolicy: (
    workspace: PosWorkspaceScope,
    id: string,
    signal?: AbortSignal,
  ) => Promise<SharedPolicy>;
  listPolicyHistory: (
    workspace: PosWorkspaceScope,
    id: string,
    options: { page: number; pageSize: number },
    signal?: AbortSignal,
  ) => Promise<SharedPolicyHistoryPaged>;
  getUtangSummary: (
    workspace: PosWorkspaceScope,
    id: string,
    signal?: AbortSignal,
  ) => Promise<SharedUtangSummary>;
  upsertPolicy: (
    workspace: PosWorkspaceScope,
    id: string,
    input: {
      creditLimit: number;
      defaultTermDays: number;
      reason: string;
      expectedUpdatedAtUtc: string | null;
    },
  ) => Promise<unknown>;
  approvePolicy: (
    workspace: PosWorkspaceScope,
    id: string,
    input: { reason: string; expectedUpdatedAtUtc: string },
  ) => Promise<unknown>;
  disablePolicy: (
    workspace: PosWorkspaceScope,
    id: string,
    input: { reason: string; expectedUpdatedAtUtc: string },
  ) => Promise<unknown>;
  onPolicyLoadError?: (error: unknown, id: string) => void;
};

export function CreditTermsSection({
  workspace,
  online,
  canManage,
  canApprove,
  canRecordPayment = false,
  canViewStatement = false,
  onRecordPayment,
  subjectIdentity = null,
  policyOverride,
  titleKey,
  hintKeys,
  checkoutNoteKey,
  sectionQueryPrefix,
  statementPath,
  getPolicy,
  listPolicyHistory,
  getUtangSummary,
  upsertPolicy,
  approvePolicy,
  disablePolicy,
  onPolicyLoadError,
  ...entity
}: CreditTermsSectionProps) {
  const { t } = useI18n();
  const queryClient = useQueryClient();
  const [dialog, setDialog] = useState<CreditPolicyDialogMode | null>(null);
  const [historyOpen, setHistoryOpen] = useState(false);
  const [historyPage, setHistoryPage] = useState(1);
  const [limitText, setLimitText] = useState("");
  const [termDays, setTermDays] = useState(30);
  const [termCustom, setTermCustom] = useState(false);
  const [reason, setReason] = useState("");
  const [formError, setFormError] = useState<string | null>(null);

  const id = entity.kind === "personal" ? entity.customerId : entity.connectionId;
  const testIdPrefix = entity.testIdPrefix;
  const useOverride = policyOverride !== undefined;
  const hasEntityId = Boolean(id?.trim());
  const hasWorkspace = Boolean(workspace.organizationId);

  const policyQuery = useQuery({
    queryKey: [sectionQueryPrefix, "credit-policy", workspace.organizationId, id],
    enabled: online && !useOverride && hasEntityId && hasWorkspace,
    queryFn: ({ signal }) => getPolicy(workspace, id, signal),
  });

  const historyQuery = useQuery({
    queryKey: [
      sectionQueryPrefix,
      "credit-policy-history",
      workspace.organizationId,
      id,
      historyPage,
    ],
    enabled: online && historyOpen && !useOverride && hasEntityId && hasWorkspace,
    queryFn: ({ signal }) =>
      listPolicyHistory(workspace, id, { page: historyPage, pageSize: 20 }, signal),
  });

  const summaryQuery = useQuery({
    queryKey: [sectionQueryPrefix, "utang-summary", workspace.organizationId, id],
    enabled: online && !useOverride && hasEntityId && hasWorkspace,
    queryFn: ({ signal }) => getUtangSummary(workspace, id, signal),
  });

  useEffect(() => {
    if (!policyQuery.isError || !policyQuery.error || !onPolicyLoadError) {
      return;
    }
    onPolicyLoadError(policyQuery.error, id);
  }, [id, onPolicyLoadError, policyQuery.error, policyQuery.isError]);

  const policy = useOverride ? policyOverride : policyQuery.data;
  const status = policy?.status ?? "NotConfigured";
  const outstanding = policy?.outstandingAmount ?? 0;
  const available = policy?.availableCredit ?? 0;
  const limit = policy?.creditLimit ?? null;
  const term = policy?.defaultTermDays ?? null;
  const pendingCheckAmount = summaryQuery.data?.pendingCheckAmount ?? 0;

  const actorIds = useMemo(() => {
    const ids: string[] = [];
    if (policy?.approvedByUserId) {
      ids.push(policy.approvedByUserId);
    }
    for (const item of historyQuery.data?.items ?? []) {
      ids.push(item.actorUserId);
    }
    return ids;
  }, [historyQuery.data?.items, policy?.approvedByUserId]);
  const actors = useActorDirectory(workspace.organizationId, actorIds);

  const invalidate = async () => {
    await queryClient.invalidateQueries({
      queryKey: [sectionQueryPrefix, "credit-policy", workspace.organizationId, id],
    });
    await queryClient.invalidateQueries({
      queryKey: [sectionQueryPrefix, "credit-policy-history", workspace.organizationId, id],
    });
    await queryClient.invalidateQueries({
      queryKey: [sectionQueryPrefix, "utang-summary", workspace.organizationId, id],
    });
  };

  const upsertMutation = useMutation({
    mutationFn: () => {
      const creditLimit = parseMoneyAmountInput(limitText);
      if (creditLimit === null) {
        throw new Error(t("customers.creditPolicy.invalidLimit"));
      }
      const days = termDays;
      if (!Number.isInteger(days) || days < 1 || days > 365) {
        throw new Error(t("customers.creditPolicy.invalidTerm"));
      }
      const trimmedReason = reason.trim();
      if (!trimmedReason) {
        throw new Error(t("customers.creditPolicy.reasonRequired"));
      }
      return upsertPolicy(workspace, id, {
        creditLimit,
        defaultTermDays: days,
        reason: trimmedReason,
        expectedUpdatedAtUtc: policy?.expectedUpdatedAtUtc ?? null,
      });
    },
    onSuccess: async () => {
      setDialog(null);
      setFormError(null);
      await invalidate();
    },
    onError: (err) => {
      setFormError(err instanceof Error ? err.message : t("error.detail"));
    },
  });

  const approveMutation = useMutation({
    mutationFn: () => {
      if (!policy?.expectedUpdatedAtUtc) {
        throw new Error(t("customers.creditPolicy.concurrencyReload"));
      }
      const trimmed = reason.trim();
      if (!trimmed) {
        throw new Error(t("customers.creditPolicy.reasonRequired"));
      }
      return approvePolicy(workspace, id, {
        reason: trimmed,
        expectedUpdatedAtUtc: policy.expectedUpdatedAtUtc,
      });
    },
    onSuccess: async () => {
      setDialog(null);
      setFormError(null);
      await invalidate();
    },
    onError: (err) => {
      setFormError(err instanceof Error ? err.message : t("error.detail"));
    },
  });

  const disableMutation = useMutation({
    mutationFn: () => {
      if (!policy?.expectedUpdatedAtUtc) {
        throw new Error(t("customers.creditPolicy.concurrencyReload"));
      }
      const trimmed = reason.trim();
      if (!trimmed) {
        throw new Error(t("customers.creditPolicy.reasonRequired"));
      }
      return disablePolicy(workspace, id, {
        reason: trimmed,
        expectedUpdatedAtUtc: policy.expectedUpdatedAtUtc,
      });
    },
    onSuccess: async () => {
      setDialog(null);
      setFormError(null);
      await invalidate();
    },
    onError: (err) => {
      setFormError(err instanceof Error ? err.message : t("error.detail"));
    },
  });

  function openConfigure() {
    const presetMatch = term != null && [7, 15, 30, 60, 90].includes(term);
    setLimitText(limit != null ? formatMoneyAmountInput(limit) : "");
    setTermDays(term ?? 30);
    setTermCustom(term != null && !presetMatch);
    setReason("");
    setFormError(null);
    setDialog("configure");
  }

  function openApprove() {
    setReason("");
    setFormError(null);
    setDialog("approve");
  }

  function openDisable() {
    setReason("");
    setFormError(null);
    setDialog("disable");
  }

  const parsedLimit = parseMoneyAmountInput(limitText);
  const showOutstandingWarning =
    dialog === "configure" &&
    parsedLimit !== null &&
    outstandingExceedsNewLimit(outstanding, parsedLimit);
  const showReapprovalWarning = dialog === "configure" && status === "Approved";
  const busy =
    upsertMutation.isPending || approveMutation.isPending || disableMutation.isPending;

  const configureHasChanges =
    status === "NotConfigured" ||
    status === "Disabled" ||
    (parsedLimit !== null && (parsedLimit !== (limit ?? null) || termDays !== (term ?? null)));

  const configureCanSubmit =
    !busy &&
    parsedLimit !== null &&
    Number.isInteger(termDays) &&
    termDays >= 1 &&
    termDays <= 365 &&
    reason.trim().length > 0 &&
    configureHasChanges;

  const reasonDialogCanSubmit = !busy && reason.trim().length > 0;

  const canShowApprove = canApprove && status === "PendingApproval" && online;
  const canShowDisable =
    canManage && (status === "PendingApproval" || status === "Approved") && online;
  const canShowConfigure = canManage && online;
  const configureUsesSettingsIcon = status === "NotConfigured" || status === "Disabled";

  if (!online && !useOverride) {
    return (
      <Card
        data-testid={`${testIdPrefix}-credit-policy-section`}
        className="flex flex-col gap-2 p-4"
      >
        <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">{t(titleKey)}</h2>
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
          {t("customers.creditPolicy.offline")}
        </p>
      </Card>
    );
  }

  if (!useOverride && policyQuery.isLoading) {
    return (
      <Card
        data-testid={`${testIdPrefix}-credit-policy-section`}
        className="flex flex-col gap-2 p-4"
      >
        <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">{t(titleKey)}</h2>
        <LoadingState label={t("loading.label")} />
      </Card>
    );
  }

  if (!useOverride && policyQuery.isError) {
    return (
      <Card
        data-testid={`${testIdPrefix}-credit-policy-section`}
        className="flex flex-col gap-2 p-4"
      >
        <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">{t(titleKey)}</h2>
        <p className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]">
          {t("customers.creditPolicy.loadFailed")}
        </p>
        <Button
          type="button"
          variant="outline"
          className="self-start"
          data-testid={`${testIdPrefix}-credit-policy-retry`}
          onClick={() => void policyQuery.refetch()}
        >
          {t("customers.creditPolicy.retry")}
        </Button>
      </Card>
    );
  }

  return (
    <Card data-testid={`${testIdPrefix}-credit-policy-section`} className="flex flex-col gap-3 p-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">{t(titleKey)}</h2>
        <span data-testid={`${testIdPrefix}-credit-policy-status`}>
          <StatusChip tone={creditPolicyStatusTone(status)}>
            {t(creditPolicyStatusLabelKey(status))}
          </StatusChip>
        </span>
      </div>

      {status === "NotConfigured" ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t(hintKeys.notApproved)}</p>
      ) : null}
      {status === "PendingApproval" ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t(hintKeys.pending)}</p>
      ) : null}
      {status === "Disabled" ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t(hintKeys.disabled)}</p>
      ) : null}

      <dl
        className="m-0 grid gap-2 text-[length:var(--exits-text-sm)]"
        data-testid={`${testIdPrefix}-credit-policy-summary`}
      >
        <div data-testid={`${testIdPrefix}-credit-policy-utang-allowed`}>
          <dt className="text-muted">{t("customers.creditPolicy.utangAllowed")}</dt>
          <dd className="m-0">
            <StatusChip tone={status === "Approved" ? "success" : "danger"}>
              {status === "Approved"
                ? t("customers.creditPolicy.utangAllowedYes")
                : t("customers.creditPolicy.utangAllowedNo")}
            </StatusChip>
          </dd>
        </div>
        <div className="branch-mgmt-overview__grid">
          <div className="branch-mgmt-overview__item">
            <dt>
              <CircleDollarSign
                className="branch-mgmt-overview__icon credit-policy-stat-icon credit-policy-stat-icon--limit"
                aria-hidden
              />
              {t("customers.creditPolicy.limit")}
            </dt>
            <dd className="tabular-nums" data-testid={`${testIdPrefix}-credit-policy-limit`}>
              {limit == null ? "—" : <MoneyDisplay amount={limit} />}
            </dd>
          </div>
          <div className="branch-mgmt-overview__item">
            <dt>
              <Receipt
                className="branch-mgmt-overview__icon credit-policy-stat-icon credit-policy-stat-icon--outstanding"
                aria-hidden
              />
              {t("customers.creditPolicy.outstanding")}
            </dt>
            <dd className="tabular-nums">
              <MoneyDisplay amount={outstanding} />
            </dd>
          </div>
          <div className="branch-mgmt-overview__item">
            <dt>
              <Wallet
                className="branch-mgmt-overview__icon credit-policy-stat-icon credit-policy-stat-icon--available"
                aria-hidden
              />
              {t("customers.creditPolicy.available")}
            </dt>
            <dd className="tabular-nums" data-testid={`${testIdPrefix}-credit-policy-available`}>
              <MoneyDisplay amount={available} />
            </dd>
          </div>
          <div className="branch-mgmt-overview__item">
            <dt>
              <CalendarDays
                className="branch-mgmt-overview__icon credit-policy-stat-icon credit-policy-stat-icon--term"
                aria-hidden
              />
              {t("customers.creditPolicy.term")}
            </dt>
            <dd data-testid={`${testIdPrefix}-credit-policy-term`}>
              {term == null
                ? "—"
                : t("customers.creditPolicy.termDays").replace("{days}", String(term))}
              {termDaysHelperLabelKey(term) ? (
                <span className="ml-1 text-muted">({t(termDaysHelperLabelKey(term)!)})</span>
              ) : null}
            </dd>
          </div>
          {pendingCheckAmount > 0 ? (
            <div
              className="branch-mgmt-overview__item"
              data-testid={`${testIdPrefix}-credit-policy-pending-checks`}
            >
              <dt>
                <AlertTriangle
                  className="branch-mgmt-overview__icon credit-policy-stat-icon credit-policy-stat-icon--outstanding"
                  aria-hidden
                />
                {t("customers.pendingChecks")}
              </dt>
              <dd className="tabular-nums">
                <MoneyDisplay amount={pendingCheckAmount} />
              </dd>
            </div>
          ) : null}
        </div>
        {policy?.approvedByUserId || policy?.approvedAtUtc ? (
          <div>
            <ActorAttribution
              labelKey="customers.creditPolicy.approvedBy"
              actorId={policy.approvedByUserId}
              occurredAtUtc={policy.approvedAtUtc}
              resolved={actors.resolve(policy.approvedByUserId)}
              isLoading={actors.isResolving}
              className="min-h-0"
              testId={`${testIdPrefix}-credit-policy-approved-by`}
            />
          </div>
        ) : null}
      </dl>

      {status === "Approved" ? (
        <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">{t(checkoutNoteKey)}</p>
      ) : null}

      <div className="flex flex-wrap gap-2">
        {canRecordPayment ? (
          <Button
            type="button"
            variant="success"
            data-testid={`${testIdPrefix}-credit-policy-repay`}
            disabled={!onRecordPayment}
            onClick={() => onRecordPayment?.()}
          >
            <Wallet className="size-4 shrink-0" aria-hidden />
            {t("customers.recordPayment")}
          </Button>
        ) : null}
        {canViewStatement && online ? (
          <Button asChild variant="info" data-testid={`${testIdPrefix}-credit-policy-statement`}>
            <Link to={statementPath(id)}>
              <FileText className="size-4 shrink-0" aria-hidden />
              {t("customers.viewStatement")}
            </Link>
          </Button>
        ) : null}
        {canShowConfigure ? (
          <Button
            type="button"
            variant={configureUsesSettingsIcon ? "outline" : "default"}
            data-testid={`${testIdPrefix}-credit-policy-configure`}
            onClick={openConfigure}
          >
            {configureUsesSettingsIcon ? (
              <Settings2 className="size-4 shrink-0" aria-hidden />
            ) : (
              <Pencil className="size-4 shrink-0" aria-hidden />
            )}
            {t(creditPolicyConfigureActionLabelKey(status))}
          </Button>
        ) : null}
        {canShowApprove ? (
          <Button
            type="button"
            variant="outline"
            className="border-[color-mix(in_srgb,var(--exits-success)_40%,var(--exits-border))] bg-[color-mix(in_srgb,var(--exits-success)_12%,var(--exits-surface))] text-[var(--exits-success)] hover:border-[color-mix(in_srgb,var(--exits-success)_55%,var(--exits-border))] hover:bg-[color-mix(in_srgb,var(--exits-success)_18%,var(--exits-surface))]"
            data-testid={`${testIdPrefix}-credit-policy-approve`}
            onClick={openApprove}
          >
            <CheckCircle2 className="size-4 shrink-0" aria-hidden />
            {t("customers.creditPolicy.approveCredit")}
          </Button>
        ) : null}
        {canShowDisable ? (
          <Button
            type="button"
            variant="outline"
            className="border-[color-mix(in_srgb,var(--exits-danger)_40%,var(--exits-border))] bg-[color-mix(in_srgb,var(--exits-danger)_12%,var(--exits-surface))] text-[var(--exits-danger)] hover:border-[color-mix(in_srgb,var(--exits-danger)_55%,var(--exits-border))] hover:bg-[color-mix(in_srgb,var(--exits-danger)_18%,var(--exits-surface))]"
            data-testid={`${testIdPrefix}-credit-policy-disable`}
            onClick={openDisable}
          >
            <Ban className="size-4 shrink-0" aria-hidden />
            {t("customers.creditPolicy.pauseCredit")}
          </Button>
        ) : null}
        <Button
          type="button"
          variant="outline"
          className="border-[color-mix(in_srgb,var(--exits-info)_40%,var(--exits-border))] bg-[color-mix(in_srgb,var(--exits-info)_12%,var(--exits-surface))] text-[var(--exits-info)] hover:border-[color-mix(in_srgb,var(--exits-info)_55%,var(--exits-border))] hover:bg-[color-mix(in_srgb,var(--exits-info)_18%,var(--exits-surface))]"
          data-testid={`${testIdPrefix}-credit-policy-history-toggle`}
          onClick={() => {
            setHistoryOpen((open) => !open);
            setHistoryPage(1);
          }}
        >
          <History className="size-4 shrink-0" aria-hidden />
          {historyOpen ? t("customers.creditPolicy.hideHistory") : t("customers.creditPolicy.history")}
        </Button>
      </div>

      {historyOpen ? (
        <div className="flex flex-col gap-2" data-testid={`${testIdPrefix}-credit-policy-history`}>
          {historyQuery.isLoading ? <LoadingState label={t("loading.label")} /> : null}
          {historyQuery.isSuccess && historyQuery.data.items.length === 0 ? (
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("customers.creditPolicy.historyEmpty")}
            </p>
          ) : null}
          {historyQuery.data && historyQuery.data.items.length > 0 ? (
            <div className="min-w-0 overflow-x-auto">
              <table className="w-full min-w-[28rem] border-collapse text-left text-[length:var(--exits-text-sm)]">
                <thead>
                  <tr className="border-b border-border">
                    <th className="px-2 py-1.5 text-[length:var(--exits-text-xs)] font-medium text-muted">
                      {t("customers.creditPolicy.historyAction")}
                    </th>
                    <th className="px-2 py-1.5 text-[length:var(--exits-text-xs)] font-medium text-muted">
                      {t("customers.creditPolicy.historyStatus")}
                    </th>
                    <th className="px-2 py-1.5 text-[length:var(--exits-text-xs)] font-medium text-muted">
                      {t("customers.creditPolicy.limit")}
                    </th>
                    <th className="px-2 py-1.5 text-[length:var(--exits-text-xs)] font-medium text-muted">
                      {t("customers.creditPolicy.reasonForChange")}
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {historyQuery.data.items.map((change) => (
                    <tr key={change.changeId} className="border-b border-border last:border-b-0">
                      <td className="px-2 py-1.5 align-top">
                        <div>{change.action}</div>
                        <ActorAttribution
                          labelKey="common.recordedBy"
                          actorId={change.actorUserId}
                          occurredAtUtc={change.changedAtUtc}
                          resolved={actors.resolve(change.actorUserId)}
                          isLoading={actors.isResolving}
                          className="min-h-0 mt-1"
                        />
                      </td>
                      <td className="px-2 py-1.5 align-top whitespace-nowrap">
                        {change.previousStatus ? `${change.previousStatus} → ` : ""}
                        {change.newStatus}
                      </td>
                      <td className="px-2 py-1.5 align-top tabular-nums">
                        {change.newCreditLimit == null ? "—" : <MoneyDisplay amount={change.newCreditLimit} />}
                        {change.newTermDays != null ? ` / ${change.newTermDays}d` : null}
                      </td>
                      <td className="px-2 py-1.5 align-top">{change.reason || "—"}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          ) : null}
          {historyQuery.data && historyQuery.data.totalCount > historyQuery.data.pageSize ? (
            <div className="flex flex-wrap gap-2">
              <Button
                type="button"
                variant="outline"
                disabled={historyPage <= 1}
                onClick={() => setHistoryPage((p) => Math.max(1, p - 1))}
              >
                {t("customers.creditPolicy.prevPage")}
              </Button>
              <Button
                type="button"
                variant="outline"
                disabled={historyPage * historyQuery.data.pageSize >= historyQuery.data.totalCount}
                onClick={() => setHistoryPage((p) => p + 1)}
              >
                {t("customers.creditPolicy.nextPage")}
              </Button>
            </div>
          ) : null}
        </div>
      ) : null}

      <EditCreditTermsModal
        open={dialog !== null}
        mode={dialog}
        subjectIdentity={subjectIdentity}
        status={status}
        limitText={limitText}
        setLimitText={setLimitText}
        termDays={termDays}
        setTermDays={setTermDays}
        termCustom={termCustom}
        setTermCustom={setTermCustom}
        reason={reason}
        setReason={setReason}
        showOutstandingWarning={showOutstandingWarning}
        showReapprovalWarning={showReapprovalWarning}
        busy={busy}
        formError={formError}
        canSubmit={dialog === "configure" ? configureCanSubmit : reasonDialogCanSubmit}
        testIdPrefix={testIdPrefix}
        onCancel={() => setDialog(null)}
        onSubmit={() => {
          setFormError(null);
          if (dialog === "configure") {
            upsertMutation.mutate();
          } else if (dialog === "approve") {
            approveMutation.mutate();
          } else if (dialog === "disable") {
            disableMutation.mutate();
          }
        }}
      />
    </Card>
  );
}
