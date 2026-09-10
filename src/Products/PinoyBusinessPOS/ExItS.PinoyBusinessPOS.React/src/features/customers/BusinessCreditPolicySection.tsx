import { useEffect, useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Ban, CheckCircle2, History, Pencil, Save, Settings2, X } from "lucide-react";
import {
  approveBusinessCustomerCreditPolicy,
  disableBusinessCustomerCreditPolicy,
  getBusinessCustomerCreditPolicy,
  listBusinessCustomerCreditPolicyHistory,
  upsertBusinessCustomerCreditPolicy,
  type PosBusinessCustomerCreditPolicy,
} from "@/api/pos/pos-business-credit-policy-client";
import { PosApiError, type PosWorkspaceScope } from "@/api/pos/pos-http";
import { ActorAttribution } from "@/features/actors/ActorAttribution";
import { useActorDirectory } from "@/features/actors/useActorDirectory";
import {
  CREDIT_TERM_PRESETS,
  creditPolicyConfigureActionLabelKey,
  creditPolicyConfigureSubmitLabelKey,
  creditPolicyStatusLabelKey,
  creditPolicyStatusTone,
  outstandingExceedsNewLimit,
  termDaysHelperLabelKey,
} from "@/features/customers/credit-policy";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { LoadingState } from "@/components/exits/LoadingState";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { StatusChip } from "@/components/exits/StatusChip";
import { useI18n } from "@/i18n/I18nProvider";
import {
  formatMoneyAmountInput,
  normalizeMoneyAmountTyping,
  parseMoneyAmountInput,
} from "@/lib/money-input";

export type BusinessCreditPolicySectionProps = {
  workspace: PosWorkspaceScope;
  connectionId: string;
  online: boolean;
  canManage: boolean;
  canApprove: boolean;
  /** Display under dialog title, e.g. "Kizy Bakery · ORG436352". */
  subjectIdentity?: string | null;
  /** When set, skip fetch (used by unit tests). */
  policyOverride?: PosBusinessCustomerCreditPolicy | null;
};

type DialogMode = "configure" | "approve" | "disable" | null;

export function BusinessCreditPolicySection({
  workspace,
  connectionId,
  online,
  canManage,
  canApprove,
  subjectIdentity = null,
  policyOverride,
}: BusinessCreditPolicySectionProps) {
  const { t } = useI18n();
  const queryClient = useQueryClient();
  const [dialog, setDialog] = useState<DialogMode>(null);
  const [historyOpen, setHistoryOpen] = useState(false);
  const [historyPage, setHistoryPage] = useState(1);
  const [limitText, setLimitText] = useState("");
  const [termDays, setTermDays] = useState(30);
  const [termCustom, setTermCustom] = useState(false);
  const [reason, setReason] = useState("");
  const [formError, setFormError] = useState<string | null>(null);

  const useOverride = policyOverride !== undefined;
  const hasConnectionId = Boolean(connectionId?.trim());
  const policyQuery = useQuery({
    queryKey: ["business-customers", "credit-policy", workspace.organizationId, connectionId],
    enabled: online && !useOverride && hasConnectionId && Boolean(workspace.organizationId),
    queryFn: ({ signal }) => getBusinessCustomerCreditPolicy(workspace, connectionId, signal),
  });

  const historyQuery = useQuery({
    queryKey: [
      "business-customers",
      "credit-policy-history",
      workspace.organizationId,
      connectionId,
      historyPage,
    ],
    enabled: online && historyOpen && !useOverride && hasConnectionId && Boolean(workspace.organizationId),
    queryFn: ({ signal }) =>
      listBusinessCustomerCreditPolicyHistory(
        workspace,
        connectionId,
        { page: historyPage, pageSize: 20 },
        signal,
      ),
  });

  useEffect(() => {
    if (!import.meta.env.DEV || !policyQuery.isError || !policyQuery.error) {
      return;
    }
    const err = policyQuery.error;
    if (err instanceof PosApiError) {
      console.warn("[business-credit-policy] load failed", {
        status: err.status,
        errorCode: err.errorCode,
        detail: err.problem.detail,
        connectionId,
        path: `/api/v1/pos/connected-suppliers/business-customers/${connectionId}/credit-policy`,
      });
    } else {
      console.warn("[business-credit-policy] load failed", err);
    }
  }, [connectionId, policyQuery.error, policyQuery.isError]);

  const policy = useOverride ? policyOverride : policyQuery.data;
  const status = policy?.status ?? "NotConfigured";
  const outstanding = policy?.outstandingAmount ?? 0;
  const available = policy?.availableCredit ?? 0;
  const limit = policy?.creditLimit ?? null;
  const term = policy?.defaultTermDays ?? null;

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
      queryKey: ["business-customers", "credit-policy", workspace.organizationId, connectionId],
    });
    await queryClient.invalidateQueries({
      queryKey: ["business-customers", "credit-policy-history", workspace.organizationId, connectionId],
    });
    await queryClient.invalidateQueries({
      queryKey: ["business-customers", "credit-policy"],
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
      return upsertBusinessCustomerCreditPolicy(workspace, connectionId, {
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
      return approveBusinessCustomerCreditPolicy(workspace, connectionId, {
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
      return disableBusinessCustomerCreditPolicy(workspace, connectionId, {
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
    const presetMatch =
      term != null && (CREDIT_TERM_PRESETS as readonly number[]).includes(term);
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
      <Card data-testid="business-credit-policy-section" className="flex flex-col gap-2 p-4">
        <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
          {t("customers.business.creditPolicy.title")}
        </h2>
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
          {t("customers.creditPolicy.offline")}
        </p>
      </Card>
    );
  }

  if (!useOverride && policyQuery.isLoading) {
    return (
      <Card data-testid="business-credit-policy-section" className="flex flex-col gap-2 p-4">
        <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
          {t("customers.business.creditPolicy.title")}
        </h2>
        <LoadingState label={t("loading.label")} />
      </Card>
    );
  }

  if (!useOverride && policyQuery.isError) {
    return (
      <Card data-testid="business-credit-policy-section" className="flex flex-col gap-2 p-4">
        <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
          {t("customers.business.creditPolicy.title")}
        </h2>
        <p className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]">
          {t("customers.creditPolicy.loadFailed")}
        </p>
        <Button
          type="button"
          variant="outline"
          className="self-start"
          data-testid="business-credit-policy-retry"
          onClick={() => void policyQuery.refetch()}
        >
          {t("customers.creditPolicy.retry")}
        </Button>
      </Card>
    );
  }

  return (
    <Card data-testid="business-credit-policy-section" className="flex flex-col gap-3 p-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
          {t("customers.business.creditPolicy.title")}
        </h2>
        <span data-testid="business-credit-policy-status">
          <StatusChip tone={creditPolicyStatusTone(status)}>
            {t(creditPolicyStatusLabelKey(status))}
          </StatusChip>
        </span>
      </div>

      {status === "NotConfigured" ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
          {t("customers.business.creditPolicy.notApprovedHint")}
        </p>
      ) : null}
      {status === "PendingApproval" ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
          {t("customers.business.creditPolicy.pendingHint")}
        </p>
      ) : null}
      {status === "Disabled" ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
          {t("customers.business.creditPolicy.disabledHint")}
        </p>
      ) : null}

      <dl
        className="m-0 grid gap-2 text-[length:var(--exits-text-sm)] sm:grid-cols-2"
        data-testid="business-credit-policy-summary"
      >
        {status === "Approved" ? (
          <div className="sm:col-span-2" data-testid="business-credit-policy-utang-allowed">
            <dt className="text-muted">{t("customers.creditPolicy.utangAllowed")}</dt>
            <dd className="m-0 font-semibold">{t("customers.creditPolicy.utangAllowedYes")}</dd>
          </div>
        ) : null}
        <div>
          <dt className="text-muted">{t("customers.creditPolicy.limit")}</dt>
          <dd className="m-0 font-semibold tabular-nums" data-testid="business-credit-policy-limit">
            {limit == null ? "—" : <MoneyDisplay amount={limit} />}
          </dd>
        </div>
        <div>
          <dt className="text-muted">{t("customers.creditPolicy.outstanding")}</dt>
          <dd className="m-0 font-semibold tabular-nums">
            <MoneyDisplay amount={outstanding} />
          </dd>
        </div>
        <div>
          <dt className="text-muted">{t("customers.creditPolicy.available")}</dt>
          <dd
            className="m-0 font-semibold tabular-nums"
            data-testid="business-credit-policy-available"
          >
            <MoneyDisplay amount={available} />
          </dd>
        </div>
        <div>
          <dt className="text-muted">{t("customers.creditPolicy.term")}</dt>
          <dd className="m-0" data-testid="business-credit-policy-term">
            {term == null
              ? "—"
              : t("customers.creditPolicy.termDays").replace("{days}", String(term))}
            {termDaysHelperLabelKey(term) ? (
              <span className="ml-1 text-muted">({t(termDaysHelperLabelKey(term)!)})</span>
            ) : null}
          </dd>
        </div>
        {policy?.approvedByUserId || policy?.approvedAtUtc ? (
          <div className="sm:col-span-2">
            <dt className="text-muted">{t("customers.creditPolicy.approvedBy")}</dt>
            <dd className="m-0">
              <ActorAttribution
                labelKey="customers.creditPolicy.approvedBy"
                actorId={policy.approvedByUserId}
                occurredAtUtc={policy.approvedAtUtc}
                resolved={actors.resolve(policy.approvedByUserId)}
                isLoading={actors.isResolving}
                className="min-h-0"
                testId="business-credit-policy-approved-by"
              />
            </dd>
          </div>
        ) : null}
      </dl>

      {status === "Approved" ? (
        <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
          {t("customers.business.creditPolicy.checkoutNote")}
        </p>
      ) : null}

      <div className="flex flex-wrap gap-2">
        {canShowConfigure ? (
          <Button
            type="button"
            variant="outline"
            data-testid="business-credit-policy-configure"
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
            data-testid="business-credit-policy-approve"
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
            data-testid="business-credit-policy-disable"
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
          data-testid="business-credit-policy-history-toggle"
          onClick={() => {
            setHistoryOpen((open) => !open);
            setHistoryPage(1);
          }}
        >
          <History className="size-4 shrink-0" aria-hidden />
          {historyOpen
            ? t("customers.creditPolicy.hideHistory")
            : t("customers.creditPolicy.history")}
        </Button>
      </div>

      {historyOpen ? (
        <div className="flex flex-col gap-2" data-testid="business-credit-policy-history">
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
                        {change.newCreditLimit == null ? (
                          "—"
                        ) : (
                          <MoneyDisplay amount={change.newCreditLimit} />
                        )}
                        {change.newTermDays != null
                          ? ` / ${change.newTermDays}d`
                          : null}
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
                disabled={
                  historyPage * historyQuery.data.pageSize >= historyQuery.data.totalCount
                }
                onClick={() => setHistoryPage((p) => p + 1)}
              >
                {t("customers.creditPolicy.nextPage")}
              </Button>
            </div>
          ) : null}
        </div>
      ) : null}

      {dialog ? (
        <div
          className="fixed inset-0 z-50 flex items-end justify-center bg-black/40 p-4 sm:items-center"
          role="dialog"
          aria-modal="true"
          aria-labelledby="business-credit-policy-dialog-title"
          data-testid={`business-credit-policy-dialog-${dialog}`}
        >
          <Card className="w-full max-w-md">
            <h2
              id="business-credit-policy-dialog-title"
              className="m-0 mb-1 text-[length:var(--exits-text-base)] font-semibold"
            >
              {dialog === "configure"
                ? t(creditPolicyConfigureActionLabelKey(status))
                : dialog === "approve"
                  ? t("customers.creditPolicy.approveCredit")
                  : t("customers.creditPolicy.pauseCredit")}
            </h2>
            {subjectIdentity ? (
              <p
                className="m-0 mb-3 text-[length:var(--exits-text-sm)] text-muted"
                data-testid="business-credit-policy-dialog-subject"
              >
                {subjectIdentity}
              </p>
            ) : (
              <div className="mb-2" />
            )}

            {dialog === "configure" ? (
              <div className="grid gap-3">
                {showReapprovalWarning ? (
                  <p
                    className="m-0 whitespace-pre-line text-[length:var(--exits-text-sm)] text-[var(--exits-warning,var(--exits-danger))]"
                    data-testid="business-credit-policy-reapproval-warning"
                  >
                    {t("customers.creditPolicy.reapprovalWarning")}
                  </p>
                ) : null}
                {showOutstandingWarning ? (
                  <p
                    className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]"
                    data-testid="business-credit-policy-outstanding-warning"
                  >
                    {t("customers.creditPolicy.outstandingOverLimitWarning")}
                  </p>
                ) : null}
                <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                  {t("customers.creditPolicy.limit")}
                  <div className="flex items-center gap-1 rounded-md border border-border bg-background px-3">
                    <span className="text-muted" aria-hidden>
                      ₱
                    </span>
                    <input
                      type="text"
                      inputMode="decimal"
                      className="min-w-0 flex-1 border-0 bg-transparent py-2 outline-none tabular-nums"
                      value={limitText}
                      onChange={(e) => setLimitText(normalizeMoneyAmountTyping(e.target.value))}
                      onBlur={() => {
                        if (parsedLimit !== null) {
                          setLimitText(formatMoneyAmountInput(parsedLimit));
                        }
                      }}
                      data-testid="business-credit-policy-limit-input"
                    />
                  </div>
                  <span className="text-[length:var(--exits-text-xs)] text-muted">
                    {t("customers.creditPolicy.limitHelper")}
                  </span>
                </label>
                <fieldset className="m-0 border-0 p-0">
                  <legend className="mb-1 text-[length:var(--exits-text-sm)]">
                    {t("customers.creditPolicy.term")}
                  </legend>
                  <div className="flex flex-wrap gap-2">
                    {CREDIT_TERM_PRESETS.map((days) => (
                      <Button
                        key={days}
                        type="button"
                        variant={!termCustom && termDays === days ? "default" : "outline"}
                        onClick={() => {
                          setTermCustom(false);
                          setTermDays(days);
                        }}
                        data-testid={`business-credit-policy-term-${days}`}
                      >
                        {String(days)}
                      </Button>
                    ))}
                    <Button
                      type="button"
                      variant={termCustom ? "default" : "outline"}
                      className={
                        termCustom
                          ? undefined
                          : "border-[color-mix(in_srgb,var(--exits-info)_40%,var(--exits-border))] bg-[color-mix(in_srgb,var(--exits-info)_12%,var(--exits-surface))] text-[var(--exits-info)] hover:border-[color-mix(in_srgb,var(--exits-info)_55%,var(--exits-border))] hover:bg-[color-mix(in_srgb,var(--exits-info)_18%,var(--exits-surface))]"
                      }
                      onClick={() => setTermCustom(true)}
                      data-testid="business-credit-policy-term-custom"
                    >
                      <Pencil className="size-4 shrink-0" aria-hidden />
                      {t("customers.creditPolicy.termCustom")}
                    </Button>
                  </div>
                  <p className="mb-0 mt-1 text-[length:var(--exits-text-xs)] text-muted">
                    {t("customers.creditPolicy.termExampleHelper")}
                  </p>
                  {termCustom ? (
                    <div className="mt-2 flex flex-col gap-1">
                      <span className="text-[length:var(--exits-text-sm)]">
                        {t("customers.creditPolicy.customTerm")}
                      </span>
                      <div className="flex items-center gap-2">
                        <input
                          type="number"
                          min={1}
                          max={365}
                          className="w-28 rounded-md border border-border bg-background px-3"
                          value={termDays}
                          onChange={(e) => setTermDays(Number(e.target.value) || 1)}
                          data-testid="business-credit-policy-term-custom-input"
                        />
                        <span className="text-[length:var(--exits-text-sm)] text-muted">
                          {t("customers.creditPolicy.termDaysUnit")}
                        </span>
                      </div>
                    </div>
                  ) : null}
                </fieldset>
                <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                  {t("customers.creditPolicy.reasonForChange")}
                  <textarea
                    className="min-h-20 rounded-md border border-border bg-background px-3 py-2"
                    value={reason}
                    maxLength={512}
                    onChange={(e) => setReason(e.target.value)}
                    data-testid="business-credit-policy-reason"
                  />
                  <span className="text-[length:var(--exits-text-xs)] text-muted">
                    {t("customers.creditPolicy.reasonForChangeHelper")}
                  </span>
                </label>
              </div>
            ) : (
              <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                {t("customers.creditPolicy.reasonForChange")}
                <textarea
                  className="min-h-20 rounded-md border border-border bg-background px-3 py-2"
                  value={reason}
                  maxLength={512}
                  onChange={(e) => setReason(e.target.value)}
                  data-testid="business-credit-policy-reason"
                />
                <span className="text-[length:var(--exits-text-xs)] text-muted">
                  {t("customers.creditPolicy.reasonForChangeHelper")}
                </span>
              </label>
            )}

            {formError ? (
              <p className="mt-3 mb-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]">
                {formError}
              </p>
            ) : null}

            <div className="mt-4 flex flex-wrap justify-end gap-2">
              <Button
                type="button"
                variant="outline"
                disabled={busy}
                onClick={() => setDialog(null)}
                data-testid="business-credit-policy-dialog-cancel"
              >
                <X className="size-4 shrink-0" aria-hidden />
                {t("customers.creditPolicy.cancel")}
              </Button>
              <Button
                type="button"
                disabled={dialog === "configure" ? !configureCanSubmit : !reasonDialogCanSubmit}
                data-testid="business-credit-policy-dialog-submit"
                onClick={() => {
                  setFormError(null);
                  if (dialog === "configure") {
                    upsertMutation.mutate();
                  } else if (dialog === "approve") {
                    approveMutation.mutate();
                  } else {
                    disableMutation.mutate();
                  }
                }}
              >
                {dialog === "configure" ? (
                  <Save className="size-4 shrink-0" aria-hidden />
                ) : dialog === "approve" ? (
                  <CheckCircle2 className="size-4 shrink-0" aria-hidden />
                ) : (
                  <Ban className="size-4 shrink-0" aria-hidden />
                )}
                {dialog === "configure"
                  ? t(creditPolicyConfigureSubmitLabelKey(status))
                  : t("customers.creditPolicy.save")}
              </Button>
            </div>
          </Card>
        </div>
      ) : null}
    </Card>
  );
}
