import {
  CREDIT_ALLOW_DISABLE_REASON,
  CREDIT_ALLOW_ENABLE_REASON,
  creditPolicyConfigureActionLabelKey,
  creditPolicyHasReusableTerms,
  creditPolicyStatusLabelKey,
  creditPolicyStatusTone,
  isCreditAllowSwitchOn,
  outstandingExceedsNewLimit,
  resolveSellerCreditDisplayStatus,
  termDaysHelperLabelKey,
} from "@/features/customers/credit-policy";
import {
  type CreditPolicyDialogMode,
  EditCreditTermsModal,
} from "@/features/customers/EditCreditTermsModal";
import { ConfirmActionDialog } from "@/components/exits/ConfirmActionDialog";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Switch } from "@/components/ui/switch";
import { LoadingState } from "@/components/exits/LoadingState";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { StatusChip } from "@/components/exits/StatusChip";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import {
  formatMoneyAmountInput,
  parseMoneyAmountInput,
} from "@/lib/money-input";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import {
  AlertTriangle,
  CalendarDays,
  CircleDollarSign,
  ClipboardList,
  FileText,
  History,
  Pencil,
  Receipt,
  Wallet,
} from "lucide-react";
import { Link } from "react-router-dom";
import { ActorAttribution } from "@/features/actors/ActorAttribution";
import { useActorDirectory } from "@/features/actors/useActorDirectory";
import {
  computeSupplierCreditExposure,
  formatUtilizationPercent,
} from "@/features/suppliers/supplier-credit-exposure";
import { formatPeso } from "@/lib/format-money";
import { useEffect, useMemo, useRef, useState } from "react";
import { useInfiniteQuery, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";

const CREDIT_POLICY_HISTORY_PAGE_SIZE = 10;

type SharedPolicy = {
  status?: string | null;
  creditLimit?: number | null;
  defaultTermDays?: number | null;
  outstandingAmount?: number | null;
  reservedByActivePos?: number | null;
  availableCredit?: number | null;
  approvedByUserId?: string | null;
  approvedAtUtc?: string | null;
  expectedUpdatedAtUtc?: string | null;
  hasEverBeenApproved?: boolean | null;
  sellerDisplayStatus?: string | null;
  buyerDisplayStatus?: string | null;
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
  outstandingAmount?: number;
  overdueAmount?: number;
  openReceivableCount?: number;
  openReceivableTotal?: number;
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
  /** When set, Open receivables card focuses the on-page Receivables section. */
  onOpenReceivables?: () => void;
  subjectIdentity?: string | null;
  policyOverride?: SharedPolicy | null;
  titleKey: string;
  hintKeys: {
    notApproved: string;
    pending: string;
    disabled: string;
    allowCreditOff: MessageKey | string;
    allowCreditNeedsSetup: MessageKey | string;
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
  ) => Promise<SharedPolicy>;
  approvePolicy: (
    workspace: PosWorkspaceScope,
    id: string,
    input: { reason: string; expectedUpdatedAtUtc: string },
  ) => Promise<SharedPolicy>;
  disablePolicy: (
    workspace: PosWorkspaceScope,
    id: string,
    input: { reason: string; expectedUpdatedAtUtc: string },
  ) => Promise<SharedPolicy>;
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
  onOpenReceivables,
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
  const [limitText, setLimitText] = useState("");
  const [termDays, setTermDays] = useState(30);
  const [termCustom, setTermCustom] = useState(false);
  const [reason, setReason] = useState("");
  const [formError, setFormError] = useState<string | null>(null);
  const [allowCreditConfirm, setAllowCreditConfirm] = useState<"enable" | "disable" | null>(null);
  const autoActivateTokenRef = useRef<string | null>(null);

  const id = entity.kind === "personal" ? entity.customerId : entity.connectionId;
  const testIdPrefix = entity.testIdPrefix;
  const useOverride = policyOverride !== undefined;
  const hasEntityId = Boolean(id?.trim());
  const hasWorkspace = Boolean(workspace.organizationId);
  const historyQueryKey = [
    sectionQueryPrefix,
    "credit-policy-history",
    workspace.organizationId,
    id,
  ] as const;

  const policyQuery = useQuery({
    queryKey: [sectionQueryPrefix, "credit-policy", workspace.organizationId, id],
    enabled: online && !useOverride && hasEntityId && hasWorkspace,
    queryFn: ({ signal }) => getPolicy(workspace, id, signal),
  });

  const historyQuery = useInfiniteQuery({
    queryKey: historyQueryKey,
    enabled: online && historyOpen && !useOverride && hasEntityId && hasWorkspace,
    initialPageParam: 1,
    queryFn: ({ pageParam, signal }) =>
      listPolicyHistory(
        workspace,
        id,
        { page: pageParam, pageSize: CREDIT_POLICY_HISTORY_PAGE_SIZE },
        signal,
      ),
    getNextPageParam: (lastPage) => {
      const loaded = lastPage.page * lastPage.pageSize;
      return loaded < lastPage.totalCount ? lastPage.page + 1 : undefined;
    },
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
  // Prefer live utang/credit summary (matches Amount owed / modal) over policy snapshot.
  const outstanding =
    typeof summaryQuery.data?.outstandingAmount === "number"
      ? summaryQuery.data.outstandingAmount
      : (policy?.outstandingAmount ?? 0);
  const overdue =
    typeof summaryQuery.data?.overdueAmount === "number"
      ? summaryQuery.data.overdueAmount
      : 0;
  const reservedByActivePos = policy?.reservedByActivePos ?? 0;
  const available = policy?.availableCredit ?? 0;
  const limit = policy?.creditLimit ?? null;
  const term = policy?.defaultTermDays ?? null;
  const pendingCheckAmount = summaryQuery.data?.pendingCheckAmount ?? 0;
  const openReceivableCount = summaryQuery.data?.openReceivableCount ?? 0;
  const openReceivableTotal = summaryQuery.data?.openReceivableTotal ?? 0;
  const showOpenReceivablesLink =
    entity.kind === "business" &&
    online &&
    (openReceivableCount > 0 || outstanding > 1e-9 || openReceivableTotal > 1e-9);
  const openReceivablesDisplayTotal =
    openReceivableTotal > 1e-9 ? openReceivableTotal : outstanding;
  const showRecordPayment = canRecordPayment && outstanding > 1e-9;
  const creditExposure = computeSupplierCreditExposure({
    approvedCreditLimit: status === "Approved" ? limit : null,
    outstanding,
    reservedByActivePos: entity.kind === "business" ? reservedByActivePos : 0,
  });
  const availableUtilizationCaption =
    creditExposure.hasApprovedLimit &&
    creditExposure.utilizationPercent != null &&
    limit != null
      ? t("customers.creditPolicy.usedOfLimit")
          .replace("{percent}", formatUtilizationPercent(creditExposure.utilizationPercent))
          .replace("{limit}", formatPeso(limit))
      : null;

  const historyItems = useMemo(
    () => historyQuery.data?.pages.flatMap((page) => page.items) ?? [],
    [historyQuery.data?.pages],
  );
  const historyTotalCount = historyQuery.data?.pages[0]?.totalCount ?? 0;
  const historyLoadedCount = historyItems.length;
  const historyHasMore = Boolean(historyQuery.hasNextPage);

  const actorIds = useMemo(() => {
    const ids: string[] = [];
    if (policy?.approvedByUserId) {
      ids.push(policy.approvedByUserId);
    }
    for (const item of historyItems) {
      ids.push(item.actorUserId);
    }
    return ids;
  }, [historyItems, policy?.approvedByUserId]);
  const actors = useActorDirectory(workspace.organizationId, actorIds);

  const policyQueryKey = [sectionQueryPrefix, "credit-policy", workspace.organizationId, id] as const;

  const syncPolicyCache = (saved: SharedPolicy) => {
    if (!useOverride) {
      queryClient.setQueryData(policyQueryKey, saved);
    }
  };

  const invalidate = async () => {
    await queryClient.invalidateQueries({ queryKey: policyQueryKey });
    await queryClient.invalidateQueries({ queryKey: historyQueryKey });
    await queryClient.invalidateQueries({
      queryKey: [sectionQueryPrefix, "utang-summary", workspace.organizationId, id],
    });
    // Buyer Supplier Credit reads the same policy via buyer-credit-policy key.
    await queryClient.invalidateQueries({
      queryKey: ["connected-suppliers", "buyer-credit-policy"],
    });
    await queryClient.invalidateQueries({ queryKey: ["connected-suppliers"] });
  };

  function toggleHistory() {
    if (historyOpen) {
      setHistoryOpen(false);
      void queryClient.removeQueries({ queryKey: historyQueryKey });
      return;
    }
    setHistoryOpen(true);
  }

  /** Approve PendingApproval → Active, retrying once with a fresh concurrency token. */
  async function activatePendingWithRetry(
    seedToken: string | null | undefined,
  ): Promise<SharedPolicy> {
    const tryApprove = async (token: string) =>
      approvePolicy(workspace, id, {
        reason: CREDIT_ALLOW_ENABLE_REASON,
        expectedUpdatedAtUtc: token,
      });

    if (seedToken) {
      try {
        return await tryApprove(seedToken);
      } catch {
        // Fall through to fresh read + retry.
      }
    }

    const latest = await getPolicy(workspace, id);
    const latestStatus = (latest.status ?? "").trim();
    if (latestStatus === "Approved") {
      return latest;
    }
    if (latestStatus !== "PendingApproval" || !latest.expectedUpdatedAtUtc) {
      throw new Error(t("customers.creditPolicy.concurrencyReload"));
    }
    return tryApprove(latest.expectedUpdatedAtUtc);
  }

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
    onSuccess: async (saved) => {
      setDialog(null);
      setFormError(null);
      syncPolicyCache(saved);
      const savedStatus = (saved.status ?? "").trim();
      if (canApprove && savedStatus === "PendingApproval") {
        try {
          const approved = await activatePendingWithRetry(saved.expectedUpdatedAtUtc);
          syncPolicyCache(approved);
        } catch (err) {
          setFormError(err instanceof Error ? err.message : t("error.detail"));
          await invalidate();
          return;
        }
      }
      await invalidate();
    },
    onError: (err) => {
      setFormError(err instanceof Error ? err.message : t("error.detail"));
    },
  });

  const approveMutation = useMutation({
    mutationFn: async (approveReason?: string) => {
      const trimmed = (approveReason ?? (reason.trim() || CREDIT_ALLOW_ENABLE_REASON)).trim();
      if (!trimmed) {
        throw new Error(t("customers.creditPolicy.reasonRequired"));
      }
      // Prefer fresh token so switch re-enable / heal never depends on a stale render.
      if (!useOverride) {
        return activatePendingWithRetry(policy?.expectedUpdatedAtUtc);
      }
      if (!policy?.expectedUpdatedAtUtc) {
        throw new Error(t("customers.creditPolicy.concurrencyReload"));
      }
      return approvePolicy(workspace, id, {
        reason: trimmed,
        expectedUpdatedAtUtc: policy.expectedUpdatedAtUtc,
      });
    },
    onSuccess: async (saved) => {
      setDialog(null);
      setFormError(null);
      syncPolicyCache(saved);
      await invalidate();
    },
    onError: (err) => {
      setFormError(err instanceof Error ? err.message : t("error.detail"));
      void invalidate();
    },
  });

  const disableMutation = useMutation({
    mutationFn: (disableReason?: string) => {
      if (!policy?.expectedUpdatedAtUtc) {
        throw new Error(t("customers.creditPolicy.concurrencyReload"));
      }
      const trimmed = (disableReason ?? (reason.trim() || CREDIT_ALLOW_DISABLE_REASON)).trim();
      if (!trimmed) {
        throw new Error(t("customers.creditPolicy.reasonRequired"));
      }
      return disablePolicy(workspace, id, {
        reason: trimmed,
        expectedUpdatedAtUtc: policy.expectedUpdatedAtUtc,
      });
    },
    onSuccess: async (saved) => {
      setDialog(null);
      setFormError(null);
      syncPolicyCache(saved);
      autoActivateTokenRef.current = null;
      await invalidate();
    },
    onError: (err) => {
      setFormError(err instanceof Error ? err.message : t("error.detail"));
    },
  });

  const enableFromDisabledMutation = useMutation({
    mutationFn: async () => {
      // Always use the latest concurrency token after Disable; never reuse the pre-OFF token.
      const latest = useOverride ? policy : await getPolicy(workspace, id);
      if (!latest || !creditPolicyHasReusableTerms(latest)) {
        throw new Error(t("customers.creditPolicy.concurrencyReload"));
      }
      if (!latest.expectedUpdatedAtUtc) {
        throw new Error(t("customers.creditPolicy.concurrencyReload"));
      }
      const saved = await upsertPolicy(workspace, id, {
        creditLimit: latest.creditLimit!,
        defaultTermDays: latest.defaultTermDays!,
        reason: CREDIT_ALLOW_ENABLE_REASON,
        expectedUpdatedAtUtc: latest.expectedUpdatedAtUtc,
      });
      const savedStatus = (saved.status ?? "").trim();
      if (canApprove && savedStatus === "PendingApproval") {
        return activatePendingWithRetry(saved.expectedUpdatedAtUtc);
      }
      return saved;
    },
    onSuccess: async (saved) => {
      setFormError(null);
      syncPolicyCache(saved);
      await invalidate();
    },
    onError: (err) => {
      setFormError(err instanceof Error ? err.message : t("error.detail"));
      void invalidate();
    },
  });

  // Approve button retired: when Allow credit is ON with terms already set, finish → Active.
  useEffect(() => {
    if (useOverride || !online || !canApprove || !hasEntityId || !hasWorkspace) {
      return;
    }
    if (status !== "PendingApproval" || !creditPolicyHasReusableTerms(policy)) {
      return;
    }
    const token = policy?.expectedUpdatedAtUtc ?? null;
    if (!token || autoActivateTokenRef.current === token) {
      return;
    }
    if (
      approveMutation.isPending ||
      enableFromDisabledMutation.isPending ||
      upsertMutation.isPending ||
      disableMutation.isPending
    ) {
      return;
    }
    autoActivateTokenRef.current = token;
    approveMutation.mutate(CREDIT_ALLOW_ENABLE_REASON);
    // Intentionally depend on status/token only — mutate identity is stable enough for one-shot heal.
    // eslint-disable-next-line react-hooks/exhaustive-deps -- avoid re-entry on mutation object identity
  }, [
    canApprove,
    disableMutation.isPending,
    enableFromDisabledMutation.isPending,
    hasEntityId,
    hasWorkspace,
    online,
    policy?.expectedUpdatedAtUtc,
    status,
    upsertMutation.isPending,
    useOverride,
    policy,
    approveMutation.isPending,
  ]);

  function openConfigure() {
    const presetMatch = term != null && [7, 15, 30, 60, 90].includes(term);
    setLimitText(limit != null ? formatMoneyAmountInput(limit) : "");
    setTermDays(term ?? 30);
    setTermCustom(term != null && !presetMatch);
    setReason("");
    setFormError(null);
    setDialog("configure");
  }

  function handleAllowCreditChange(next: boolean) {
    setFormError(null);
    if (!next) {
      if (!canManage || (status !== "Approved" && status !== "PendingApproval")) {
        return;
      }
      setAllowCreditConfirm("disable");
      return;
    }
    if (status === "NotConfigured") {
      if (!canManage) {
        return;
      }
      setAllowCreditConfirm("enable");
      return;
    }
    if (status === "Disabled") {
      if (!canManage) {
        return;
      }
      setAllowCreditConfirm("enable");
      return;
    }
    if (status === "PendingApproval") {
      if (!canApprove) {
        return;
      }
      setAllowCreditConfirm("enable");
    }
  }

  function confirmAllowCreditChange() {
    const intent = allowCreditConfirm;
    setAllowCreditConfirm(null);
    if (intent === "disable") {
      disableMutation.mutate(CREDIT_ALLOW_DISABLE_REASON);
      return;
    }
    if (intent !== "enable") {
      return;
    }
    if (status === "NotConfigured") {
      openConfigure();
      return;
    }
    if (status === "Disabled") {
      if (creditPolicyHasReusableTerms(policy)) {
        enableFromDisabledMutation.mutate();
      } else {
        openConfigure();
      }
      return;
    }
    if (status === "PendingApproval") {
      approveMutation.mutate(CREDIT_ALLOW_ENABLE_REASON);
    }
  }

  const parsedLimit = parseMoneyAmountInput(limitText);
  const showOutstandingWarning =
    dialog === "configure" &&
    parsedLimit !== null &&
    outstandingExceedsNewLimit(outstanding, parsedLimit);
  const showReapprovalWarning = dialog === "configure" && status === "Approved";
  const busy =
    upsertMutation.isPending ||
    approveMutation.isPending ||
    disableMutation.isPending ||
    enableFromDisabledMutation.isPending;

  const allowCreditOn = isCreditAllowSwitchOn(status);
  const hasEverBeenApproved =
    policy?.hasEverBeenApproved === true ||
    (policy?.hasEverBeenApproved == null &&
      (status === "Approved" || Boolean(policy?.approvedAtUtc)));
  const sellerDisplayStatus = resolveSellerCreditDisplayStatus({
    status,
    hasEverBeenApproved,
    sellerDisplayStatus: policy?.sellerDisplayStatus,
  });
  const pauseResumeMode = hasEverBeenApproved;
  const allowCreditSwitchDisabled =
    !online ||
    busy ||
    (allowCreditOn
      ? !canManage
      : status === "PendingApproval"
        ? !canApprove
        : !canManage);

  const allowCreditSwitchHint = !allowCreditOn
    ? t(hintKeys.allowCreditOff)
    : status === "PendingApproval" && !creditPolicyHasReusableTerms(policy)
      ? t(hintKeys.allowCreditNeedsSetup)
      : null;

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

  const canShowConfigure = canManage && online && allowCreditOn;

  if (!online && !useOverride) {
    return (
      <Card
        id={`${testIdPrefix}-credit-policy-section`}
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
        id={`${testIdPrefix}-credit-policy-section`}
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
        id={`${testIdPrefix}-credit-policy-section`}
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
    <Card
      id={`${testIdPrefix}-credit-policy-section`}
      data-testid={`${testIdPrefix}-credit-policy-section`}
      className="flex flex-col gap-3 p-4"
    >
      <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">{t(titleKey)}</h2>

      <div className="flex flex-col gap-1.5">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div className="flex min-w-0 flex-wrap items-center gap-2">
            <span
              className="text-[length:var(--exits-text-sm)] font-medium"
              id={`${testIdPrefix}-credit-policy-allow-credit-label`}
            >
              {t("customers.creditPolicy.allowCredit")}
            </span>
            <Switch
              checked={allowCreditOn}
              disabled={allowCreditSwitchDisabled}
              aria-busy={busy || undefined}
              aria-labelledby={`${testIdPrefix}-credit-policy-allow-credit-label`}
              data-testid={`${testIdPrefix}-credit-policy-allow-credit`}
              onCheckedChange={handleAllowCreditChange}
            />
          </div>
          <span data-testid={`${testIdPrefix}-credit-policy-status`}>
            <StatusChip tone={creditPolicyStatusTone(sellerDisplayStatus)}>
              {t(creditPolicyStatusLabelKey(sellerDisplayStatus))}
            </StatusChip>
          </span>
        </div>
        {allowCreditSwitchHint ? (
          <p
            className="m-0 text-[length:var(--exits-text-sm)] text-muted"
            data-testid={`${testIdPrefix}-credit-policy-allow-credit-hint`}
          >
            {allowCreditSwitchHint}
          </p>
        ) : null}
      </div>

      {formError && dialog === null ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]">{formError}</p>
      ) : null}

      {allowCreditOn ? (
        <dl
          className="m-0 grid gap-2 text-[length:var(--exits-text-sm)]"
          data-testid={`${testIdPrefix}-credit-policy-summary`}
        >
          <div
            className={
              entity.kind === "business"
                ? "branch-mgmt-overview__grid branch-mgmt-overview__grid--credit-business"
                : "branch-mgmt-overview__grid"
            }
          >
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
            <dd className="tabular-nums" data-testid={`${testIdPrefix}-credit-policy-outstanding`}>
              <MoneyDisplay amount={outstanding} />
            </dd>
          </div>
          <div className="branch-mgmt-overview__item">
            <dt>
              <AlertTriangle
                className="branch-mgmt-overview__icon credit-policy-stat-icon credit-policy-stat-icon--overdue"
                aria-hidden
              />
              {t("customers.creditPolicy.overdue")}
            </dt>
            <dd className="tabular-nums" data-testid={`${testIdPrefix}-credit-policy-overdue`}>
              <MoneyDisplay amount={overdue} />
            </dd>
          </div>
          {entity.kind === "business" ? (
            <div className="branch-mgmt-overview__item">
              <dt>
                <ClipboardList
                  className="branch-mgmt-overview__icon credit-policy-stat-icon credit-policy-stat-icon--reserved"
                  aria-hidden
                />
                {t("customers.creditPolicy.reservedActivePos")}
              </dt>
              <dd
                className="tabular-nums"
                data-testid={`${testIdPrefix}-credit-policy-reserved`}
              >
                <MoneyDisplay amount={reservedByActivePos} />
              </dd>
            </div>
          ) : null}
          <div className="branch-mgmt-overview__item credit-policy-available-card">
            <dt>
              <Wallet
                className="branch-mgmt-overview__icon credit-policy-stat-icon credit-policy-stat-icon--available"
                aria-hidden
              />
              {t("customers.creditPolicy.available")}
            </dt>
            <div className="credit-policy-available__amount-row">
              <dd
                className="tabular-nums"
                data-testid={`${testIdPrefix}-credit-policy-available`}
              >
                <MoneyDisplay amount={available} />
              </dd>
              {availableUtilizationCaption ? (
                <p
                  className="credit-policy-available__caption m-0"
                  data-testid={`${testIdPrefix}-credit-policy-utilization-caption`}
                >
                  {availableUtilizationCaption}
                </p>
              ) : null}
            </div>
            {creditExposure.progressPercent != null ? (
              <div
                className="credit-policy-available__track"
                role="progressbar"
                aria-valuemin={0}
                aria-valuemax={100}
                aria-valuenow={Math.round(creditExposure.progressPercent)}
                aria-label={t("customers.creditPolicy.utilizationProgress")}
                data-testid={`${testIdPrefix}-credit-policy-utilization-bar`}
                data-over-limit={creditExposure.isOverLimit ? "true" : "false"}
              >
                <span
                  className="credit-policy-available__fill"
                  style={{ width: `${creditExposure.progressPercent}%` }}
                />
              </div>
            ) : null}
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
          {showOpenReceivablesLink ? (
            <div
              className="branch-mgmt-overview__item"
              data-testid={`${testIdPrefix}-credit-policy-open-receivables`}
            >
              <dt>
                <ClipboardList
                  className="branch-mgmt-overview__icon credit-policy-stat-icon credit-policy-stat-icon--reserved"
                  aria-hidden
                />
                {t("customers.receivables.openTitle")}
              </dt>
              <dd className="m-0">
                {onOpenReceivables ? (
                  <button
                    type="button"
                    className="inline-flex flex-col gap-0.5 border-0 bg-transparent p-0 text-left text-[inherit] underline-offset-2 hover:underline cursor-pointer"
                    data-testid={`${testIdPrefix}-credit-policy-open-receivables-link`}
                    onClick={() => onOpenReceivables()}
                  >
                    <span className="tabular-nums font-medium">
                      {t("customers.receivables.openCount").replace(
                        "{count}",
                        String(openReceivableCount),
                      )}
                    </span>
                    <span className="tabular-nums">
                      <MoneyDisplay amount={openReceivablesDisplayTotal} />
                    </span>
                  </button>
                ) : (
                  <Link
                    to={`/customers/business/${id}/receivables?filter=open`}
                    className="inline-flex flex-col gap-0.5 text-[inherit] no-underline hover:underline"
                    data-testid={`${testIdPrefix}-credit-policy-open-receivables-link`}
                  >
                    <span className="tabular-nums font-medium">
                      {t("customers.receivables.openCount").replace(
                        "{count}",
                        String(openReceivableCount),
                      )}
                    </span>
                    <span className="tabular-nums">
                      <MoneyDisplay amount={openReceivablesDisplayTotal} />
                    </span>
                  </Link>
                )}
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
      ) : null}

      {status === "Approved" ? (
        <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">{t(checkoutNoteKey)}</p>
      ) : null}

      <div className="flex flex-wrap gap-2">
        {showRecordPayment ? (
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
            variant="default"
            data-testid={`${testIdPrefix}-credit-policy-configure`}
            onClick={openConfigure}
          >
            <Pencil className="size-4 shrink-0" aria-hidden />
            {t(creditPolicyConfigureActionLabelKey(status))}
          </Button>
        ) : null}
        <Button
          type="button"
          variant="outline"
          className="border-[color-mix(in_srgb,var(--exits-info)_40%,var(--exits-border))] bg-[color-mix(in_srgb,var(--exits-info)_12%,var(--exits-surface))] text-[var(--exits-info)] hover:border-[color-mix(in_srgb,var(--exits-info)_55%,var(--exits-border))] hover:bg-[color-mix(in_srgb,var(--exits-info)_18%,var(--exits-surface))]"
          data-testid={`${testIdPrefix}-credit-policy-history-toggle`}
          onClick={toggleHistory}
        >
          <History className="size-4 shrink-0" aria-hidden />
          {historyOpen ? t("customers.creditPolicy.hideHistory") : t("customers.creditPolicy.history")}
        </Button>
      </div>

      {historyOpen ? (
        <div className="flex flex-col gap-2" data-testid={`${testIdPrefix}-credit-policy-history`}>
          {historyQuery.isLoading ? <LoadingState label={t("loading.label")} /> : null}
          {historyQuery.isSuccess && historyLoadedCount === 0 ? (
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("customers.creditPolicy.historyEmpty")}
            </p>
          ) : null}
          {historyLoadedCount > 0 ? (
            <>
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
                    {historyItems.map((change) => (
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
                          {change.newTermDays != null ? ` / ${change.newTermDays}d` : null}
                        </td>
                        <td className="px-2 py-1.5 align-top">{change.reason || "—"}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              <div className="flex flex-wrap items-center justify-between gap-2">
                <p
                  className="m-0 text-[length:var(--exits-text-xs)] text-muted"
                  data-testid={`${testIdPrefix}-credit-policy-history-count`}
                >
                  {t("customers.creditPolicy.historyShowing")
                    .replace("{shown}", String(historyLoadedCount))
                    .replace("{total}", String(historyTotalCount))}
                </p>
                {historyHasMore ? (
                  <Button
                    type="button"
                    variant="outline"
                    data-testid={`${testIdPrefix}-credit-policy-history-load-more`}
                    disabled={historyQuery.isFetchingNextPage}
                    onClick={() => void historyQuery.fetchNextPage()}
                  >
                    {historyQuery.isFetchingNextPage
                      ? t("loading.label")
                      : t("customers.creditPolicy.loadMoreHistory")}
                  </Button>
                ) : historyTotalCount > 0 ? (
                  <p
                    className="m-0 text-[length:var(--exits-text-xs)] text-muted"
                    data-testid={`${testIdPrefix}-credit-policy-history-end`}
                  >
                    {t("customers.creditPolicy.historyEnd")}
                  </p>
                ) : null}
              </div>
            </>
          ) : null}
        </div>
      ) : null}

      <ConfirmActionDialog
        open={allowCreditConfirm != null}
        variant={allowCreditConfirm === "disable" ? "warning" : "info"}
        title={
          allowCreditConfirm === "disable"
            ? pauseResumeMode
              ? t("customers.creditPolicy.confirmPauseTitle")
              : t("customers.creditPolicy.confirmDisableTitle")
            : pauseResumeMode
              ? t("customers.creditPolicy.confirmResumeTitle")
              : t("customers.creditPolicy.confirmEnableTitle")
        }
        description={
          allowCreditConfirm === "disable"
            ? pauseResumeMode
              ? t("customers.creditPolicy.confirmPauseDetail")
              : t("customers.creditPolicy.confirmDisableDetail")
            : pauseResumeMode
              ? t("customers.creditPolicy.confirmResumeDetail")
              : t("customers.creditPolicy.confirmEnableDetail")
        }
        confirmLabel={
          allowCreditConfirm === "disable"
            ? pauseResumeMode
              ? t("customers.creditPolicy.confirmPauseConfirm")
              : t("customers.creditPolicy.confirmDisableConfirm")
            : pauseResumeMode
              ? t("customers.creditPolicy.confirmResumeConfirm")
              : t("customers.creditPolicy.confirmEnableConfirm")
        }
        cancelLabel={t("customers.creditPolicy.cancel")}
        pending={busy}
        testId={`${testIdPrefix}-credit-policy-allow-credit-confirm`}
        onCancel={() => {
          if (!busy) {
            setAllowCreditConfirm(null);
          }
        }}
        onConfirm={confirmAllowCreditChange}
      />

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
        canSubmit={configureCanSubmit}
        testIdPrefix={testIdPrefix}
        onCancel={() => setDialog(null)}
        onSubmit={() => {
          setFormError(null);
          if (dialog === "configure") {
            upsertMutation.mutate();
          }
        }}
      />
    </Card>
  );
}
