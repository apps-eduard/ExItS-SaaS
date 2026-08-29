import { useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { canManageExpenses } from "@/access/pos-capabilities";
import {
  EXPENSE_VOID_REASON_MAX,
  expenseWorkspaceScope,
  getExpense,
  voidExpense,
} from "@/api/pos/pos-expense-client";
import { PosApiError } from "@/api/pos/pos-http";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { ActorAttribution } from "@/features/actors/ActorAttribution";
import { useActorDirectory } from "@/features/actors/useActorDirectory";
import { useBrowserOnline } from "@/connectivity/browser-online";
import {
  expensePaymentLabelKey,
  expenseStatusLabelKey,
  formatExpenseDate,
} from "@/features/expenses/expense-labels";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function ExpenseDetailPage() {
  const { t } = useI18n();
  const online = useBrowserOnline();
  const { expenseId } = useParams<{ expenseId: string }>();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const queryClient = useQueryClient();
  const allowManage = canManageExpenses(sessionGrant);
  const [error, setError] = useState<string | null>(null);
  const [voiding, setVoiding] = useState(false);
  const [voidOpen, setVoidOpen] = useState(false);
  const [voidReason, setVoidReason] = useState("");

  const organizationId = boundWorkspace?.organizationId ?? null;
  const workspace = useMemo(
    () => (organizationId ? expenseWorkspaceScope(organizationId) : null),
    [organizationId],
  );

  const query = useQuery({
    queryKey: ["expense", organizationId, expenseId],
    enabled: Boolean(workspace) && Boolean(expenseId) && online,
    queryFn: ({ signal }) => getExpense(workspace!, expenseId!, signal),
  });

  const actors = useActorDirectory(organizationId, [
    query.data?.recordedBy,
    query.data?.voidedBy,
  ]);

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }
  if (!expenseId) {
    return <ErrorState title={t("expense.errorTitle")} detail={t("expense.notFound")} />;
  }
  if (query.isLoading) {
    return <LoadingState label={t("expense.loading")} />;
  }
  if (query.isError || !query.data) {
    return <ErrorState title={t("expense.errorTitle")} detail={t("expense.notFound")} />;
  }

  const entry = query.data;
  const isRecorded = entry.status === "Recorded";
  const isVoided = entry.status === "Voided";

  async function onVoid() {
    if (!workspace || !expenseId || !allowManage || !online || voiding || !isRecorded) {
      return;
    }
    if (!voidReason.trim()) {
      setError(t("expense.validation.voidReasonRequired"));
      return;
    }
    setVoiding(true);
    setError(null);
    try {
      const updated = await voidExpense(workspace, expenseId, voidReason.trim());
      queryClient.setQueryData(["expense", organizationId, expenseId], updated);
      await queryClient.invalidateQueries({ queryKey: ["expenses"] });
      await queryClient.invalidateQueries({ queryKey: ["expenses-summary"] });
      setVoidOpen(false);
      setVoidReason("");
    } catch (err) {
      setError(
        err instanceof PosApiError
          ? (err.problem.detail ?? t("expense.voidFailed"))
          : t("expense.voidFailed"),
      );
    } finally {
      setVoiding(false);
    }
  }

  const replacementQuery = new URLSearchParams({
    categoryId: entry.categoryId,
    description: entry.description,
    payee: entry.payee ?? "",
    expenseDate: entry.expenseDate,
    paymentMethod: entry.paymentMethod,
    amount: String(entry.amount),
  });

  return (
    <div className="exits-page flex min-w-0 flex-col gap-4" data-testid="expense-detail-page">
      <PageHeader
        title={entry.expenseNumber}
        description={t("expense.detailLede")}
        backTo="/expenses"
        backLabel={t("expense.backList")}
        backTestId="page-header-back-expenses"
      />

      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="expense-no-edit">
        {t("expense.noEdit")}
      </p>

      {error ? <ErrorState title={t("expense.errorTitle")} detail={error} /> : null}

      <Card className="flex flex-col gap-3 p-3">
        <div className="flex flex-wrap items-center gap-2">
          <StatusChip tone={isVoided ? "danger" : "success"}>
            {t(expenseStatusLabelKey(entry.status))}
          </StatusChip>
          <span className="text-[length:var(--exits-text-sm)] text-muted">
            {formatExpenseDate(entry.expenseDate)}
          </span>
        </div>
        <p className="m-0 text-[length:var(--exits-text-lg)] font-semibold" data-testid="expense-detail-amount">
          <MoneyDisplay amount={entry.amount} />
        </p>
        <dl className="m-0 grid gap-3 sm:grid-cols-2">
          <div>
            <dt className="text-[length:var(--exits-text-sm)] text-muted">{t("expense.category")}</dt>
            <dd className="m-0" data-testid="expense-detail-category">
              {entry.categoryName ?? t("expense.category.unknown")}
            </dd>
          </div>
          <div>
            <dt className="text-[length:var(--exits-text-sm)] text-muted">
              {t("expense.paymentMethod")}
            </dt>
            <dd className="m-0" data-testid="expense-detail-payment">
              {t(expensePaymentLabelKey(entry.paymentMethod))}
            </dd>
          </div>
          {entry.payee ? (
            <div>
              <dt className="text-[length:var(--exits-text-sm)] text-muted">{t("expense.payee")}</dt>
              <dd className="m-0">{entry.payee}</dd>
            </div>
          ) : null}
          {entry.paymentMethod === "ManualGCash" && entry.gCashReference ? (
            <div>
              <dt className="text-[length:var(--exits-text-sm)] text-muted">
                {t("expense.gCashReference")}
              </dt>
              <dd className="m-0" data-testid="expense-detail-gcash-ref">
                {entry.gCashReference}
              </dd>
            </div>
          ) : null}
        </dl>
        <div>
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("expense.description")}</p>
          <p className="mt-1 mb-0 whitespace-pre-wrap" data-testid="expense-detail-description">
            {entry.description}
          </p>
        </div>
        <ActorAttribution
          labelKey="common.recordedBy"
          actorId={entry.recordedBy}
          occurredAtUtc={entry.recordedAtUtc}
          resolved={actors.resolve(entry.recordedBy)}
          isLoading={actors.isResolving}
          testId="expense-recorded-by"
        />
        {isVoided ? (
          <div className="rounded-[var(--exits-radius-md)] border border-[var(--exits-danger)]/30 bg-[var(--exits-surface-muted)] p-3">
            <p className="m-0 font-medium">{t("expense.voidedSection")}</p>
            {entry.voidReason ? (
              <p className="mt-2 mb-0 whitespace-pre-wrap text-[length:var(--exits-text-sm)]" data-testid="expense-void-reason">
                {entry.voidReason}
              </p>
            ) : null}
            {entry.voidedBy ? (
              <div className="mt-2">
                <ActorAttribution
                  labelKey="common.voidedBy"
                  actorId={entry.voidedBy}
                  occurredAtUtc={entry.voidedAtUtc ?? undefined}
                  resolved={actors.resolve(entry.voidedBy)}
                  isLoading={actors.isResolving}
                  testId="expense-voided-by"
                />
              </div>
            ) : null}
          </div>
        ) : null}
      </Card>

      {allowManage && isRecorded && online ? (
        <div className="flex flex-wrap gap-2">
          <Button
            type="button"
            variant="destructive"
            className="min-h-11"
            data-testid="expense-void-open"
            onClick={() => setVoidOpen(true)}
          >
            {t("expense.voidExpense")}
          </Button>
        </div>
      ) : null}

      {isVoided && allowManage ? (
        <Button asChild variant="outline" className="min-h-11 w-fit" data-testid="expense-record-replacement">
          <Link to={`/expenses/new?${replacementQuery.toString()}`}>
            {t("expense.recordReplacement")}
          </Link>
        </Button>
      ) : null}

      {voidOpen ? (
        <div
          className="fixed inset-0 z-50 flex items-end justify-center bg-black/40 p-4 sm:items-center"
          role="dialog"
          aria-modal="true"
          aria-labelledby="expense-void-title"
          data-testid="expense-void-dialog"
        >
          <Card className="flex w-full max-w-md flex-col gap-3 p-4">
            <h2 id="expense-void-title" className="m-0 text-[length:var(--exits-text-lg)] font-semibold">
              {t("expense.voidTitle")}
            </h2>
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("expense.voidLede")}</p>
            <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
              <span className="font-medium">{t("expense.voidReason")}</span>
              <textarea
                className="min-h-24 rounded-[var(--exits-radius-md)] border border-border bg-background px-3 py-2"
                value={voidReason}
                maxLength={EXPENSE_VOID_REASON_MAX}
                onChange={(e) => setVoidReason(e.target.value)}
                data-testid="expense-void-reason-input"
              />
            </label>
            <div className="flex flex-wrap gap-2">
              <Button
                type="button"
                variant="destructive"
                className="min-h-11"
                disabled={voiding || !voidReason.trim()}
                onClick={() => void onVoid()}
                data-testid="expense-void-confirm"
              >
                {voiding ? t("expense.voiding") : t("expense.voidConfirm")}
              </Button>
              <Button
                type="button"
                variant="outline"
                className="min-h-11"
                disabled={voiding}
                onClick={() => {
                  setVoidOpen(false);
                  setVoidReason("");
                }}
                data-testid="expense-void-cancel"
              >
                {t("expense.cancel")}
              </Button>
            </div>
          </Card>
        </div>
      ) : null}
    </div>
  );
}
