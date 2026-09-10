import { useEffect, useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
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

export type BusinessCreditPolicySectionProps = {
  workspace: PosWorkspaceScope;
  connectionId: string;
  online: boolean;
  canManage: boolean;
  canApprove: boolean;
  /** When set, skip fetch (used by unit tests). */
  policyOverride?: PosBusinessCustomerCreditPolicy | null;
};

type DialogMode = "configure" | "approve" | "disable" | null;

function parseMoney(raw: string): number | null {
  const trimmed = raw.trim();
  if (!/^\d+(\.\d{1,2})?$/.test(trimmed)) {
    return null;
  }
  const value = Number(trimmed);
  return Number.isFinite(value) ? value : null;
}

export function BusinessCreditPolicySection({
  workspace,
  connectionId,
  online,
  canManage,
  canApprove,
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
      const creditLimit = parseMoney(limitText);
      if (creditLimit === null) {
        throw new Error(t("customers.creditPolicy.invalidLimit"));
      }
      const days = termCustom ? termDays : termDays;
      if (!Number.isInteger(days) || days < 1 || days > 365) {
        throw new Error(t("customers.creditPolicy.invalidTerm"));
      }
      return upsertBusinessCustomerCreditPolicy(workspace, connectionId, {
        creditLimit,
        defaultTermDays: days,
        reason: reason.trim() || null,
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
    setLimitText(limit != null ? String(limit) : "");
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

  const parsedLimit = parseMoney(limitText);
  const showOutstandingWarning =
    dialog === "configure" &&
    parsedLimit !== null &&
    outstandingExceedsNewLimit(outstanding, parsedLimit);
  const showReapprovalWarning = dialog === "configure" && status === "Approved";
  const termHelperKey = termDaysHelperLabelKey(termCustom ? termDays : termDays);
  const busy =
    upsertMutation.isPending || approveMutation.isPending || disableMutation.isPending;

  const canShowApprove = canApprove && status === "PendingApproval" && online;
  const canShowDisable =
    canManage && (status === "PendingApproval" || status === "Approved") && online;
  const canShowConfigure = canManage && online;

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
      {status === "Approved" ? (
        <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
          {t("customers.business.creditPolicy.checkoutNote")}
        </p>
      ) : null}

      <dl
        className="m-0 grid gap-2 text-[length:var(--exits-text-sm)] sm:grid-cols-2"
        data-testid="business-credit-policy-summary"
      >
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

      <div className="flex flex-wrap gap-2">
        {canShowConfigure ? (
          <Button
            type="button"
            variant="outline"
            data-testid="business-credit-policy-configure"
            onClick={openConfigure}
          >
            {status === "NotConfigured"
              ? t("customers.creditPolicy.configure")
              : t("customers.creditPolicy.edit")}
          </Button>
        ) : null}
        {canShowApprove ? (
          <Button
            type="button"
            variant="outline"
            data-testid="business-credit-policy-approve"
            onClick={openApprove}
          >
            {t("customers.creditPolicy.approve")}
          </Button>
        ) : null}
        {canShowDisable ? (
          <Button
            type="button"
            variant="outline"
            data-testid="business-credit-policy-disable"
            onClick={openDisable}
          >
            {t("customers.creditPolicy.disable")}
          </Button>
        ) : null}
        <Button
          type="button"
          variant="ghost"
          data-testid="business-credit-policy-history-toggle"
          onClick={() => {
            setHistoryOpen((open) => !open);
            setHistoryPage(1);
          }}
        >
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
                      {t("common.reason")}
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
              className="m-0 mb-2 text-[length:var(--exits-text-base)] font-semibold"
            >
              {dialog === "configure"
                ? status === "NotConfigured"
                  ? t("customers.creditPolicy.configure")
                  : t("customers.creditPolicy.edit")
                : dialog === "approve"
                  ? t("customers.creditPolicy.approve")
                  : t("customers.creditPolicy.disable")}
            </h2>

            {dialog === "configure" ? (
              <div className="grid gap-3">
                {showReapprovalWarning ? (
                  <p
                    className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-warning,var(--exits-danger))]"
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
                  <input
                    type="text"
                    inputMode="decimal"
                    className="rounded-md border border-border bg-background px-3"
                    value={limitText}
                    onChange={(e) => setLimitText(e.target.value)}
                    data-testid="business-credit-policy-limit-input"
                  />
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
                        {days === 90
                          ? `${days} (${t("customers.creditPolicy.termAbout3Months")})`
                          : String(days)}
                      </Button>
                    ))}
                    <Button
                      type="button"
                      variant={termCustom ? "default" : "outline"}
                      onClick={() => setTermCustom(true)}
                      data-testid="business-credit-policy-term-custom"
                    >
                      {t("customers.creditPolicy.termCustom")}
                    </Button>
                  </div>
                  {termCustom ? (
                    <input
                      type="number"
                      min={1}
                      max={365}
                      className="mt-2 w-full rounded-md border border-border bg-background px-3"
                      value={termDays}
                      onChange={(e) => setTermDays(Number(e.target.value) || 1)}
                      data-testid="business-credit-policy-term-custom-input"
                    />
                  ) : null}
                  {termHelperKey ? (
                    <p className="mb-0 mt-1 text-[length:var(--exits-text-xs)] text-muted">
                      {t(termHelperKey)}
                    </p>
                  ) : null}
                </fieldset>
                <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                  {t("common.reason")}
                  <textarea
                    className="min-h-20 rounded-md border border-border bg-background px-3 py-2"
                    value={reason}
                    maxLength={512}
                    onChange={(e) => setReason(e.target.value)}
                    data-testid="business-credit-policy-reason"
                  />
                </label>
              </div>
            ) : (
              <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                {t("common.reason")}
                <textarea
                  className="min-h-20 rounded-md border border-border bg-background px-3 py-2"
                  value={reason}
                  maxLength={512}
                  onChange={(e) => setReason(e.target.value)}
                  data-testid="business-credit-policy-reason"
                />
              </label>
            )}

            {formError ? (
              <p className="mt-3 mb-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]">
                {formError}
              </p>
            ) : null}

            <div className="mt-4 flex flex-wrap gap-2">
              <Button
                type="button"
                variant="ghost"
                disabled={busy}
                onClick={() => setDialog(null)}
                data-testid="business-credit-policy-dialog-cancel"
              >
                {t("customers.creditPolicy.cancel")}
              </Button>
              <Button
                type="button"
                disabled={busy}
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
                {t("customers.creditPolicy.save")}
              </Button>
            </div>
          </Card>
        </div>
      ) : null}
    </Card>
  );
}
