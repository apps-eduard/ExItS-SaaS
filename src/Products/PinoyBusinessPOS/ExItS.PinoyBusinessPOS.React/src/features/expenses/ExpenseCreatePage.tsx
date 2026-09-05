import { useMemo, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { canManageExpenses } from "@/access/pos-capabilities";
import {
  EXPENSE_DESCRIPTION_MAX,
  EXPENSE_GCASH_REFERENCE_MAX,
  EXPENSE_PAYEE_MAX,
  EXPENSE_PAYMENT_METHODS,
  expenseWorkspaceScope,
  listExpenseCategories,
  recordExpense,
  type ExpensePaymentMethodCode,
} from "@/api/pos/pos-expense-client";
import { PosApiError } from "@/api/pos/pos-http";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { StickyActionBar } from "@/components/exits/FoundationStates";
import { LoadingState } from "@/components/exits/LoadingState";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { PageHeader } from "@/components/exits/PageHeader";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { isLikelyNetworkFailure } from "@/connectivity/network-failure";
import { expensePaymentLabelKey, todayExpenseDateInput } from "@/features/expenses/expense-labels";
import { useI18n } from "@/i18n/I18nProvider";
import { createSecureMutationId } from "@/lib/secure-mutation-id";
import {
  formatMoneyAmountInput,
  normalizeMoneyAmountTyping,
  parseMoneyAmountInput,
} from "@/lib/money-input";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

function initialAmountText(raw: string | null): string {
  if (!raw?.trim()) {
    return "";
  }
  const parsed = parseMoneyAmountInput(raw);
  return parsed === null ? normalizeMoneyAmountTyping(raw) : formatMoneyAmountInput(parsed);
}

export function ExpenseCreatePage() {
  const { t } = useI18n();
  const [searchParams] = useSearchParams();
  const online = useBrowserOnline();
  const queryClient = useQueryClient();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const allowManage = canManageExpenses(sessionGrant);

  const organizationId = boundWorkspace?.organizationId ?? null;
  const workspace = useMemo(
    () => (organizationId ? expenseWorkspaceScope(organizationId) : null),
    [organizationId],
  );

  const categoriesQuery = useQuery({
    queryKey: ["expense-categories", organizationId, "Active"],
    enabled: Boolean(workspace) && online && allowManage,
    queryFn: ({ signal }) =>
      listExpenseCategories(workspace!, { status: "Active", page: 1, pageSize: 100 }, signal),
  });

  const [categoryId, setCategoryId] = useState(searchParams.get("categoryId") ?? "");
  const [amountText, setAmountText] = useState(() => initialAmountText(searchParams.get("amount")));
  const [paymentMethod, setPaymentMethod] = useState<ExpensePaymentMethodCode>(
    (searchParams.get("paymentMethod") as ExpensePaymentMethodCode) || "Cash",
  );
  const [expenseDate, setExpenseDate] = useState(
    searchParams.get("expenseDate") || todayExpenseDateInput(),
  );
  const [description, setDescription] = useState(searchParams.get("description") ?? "");
  const [payee, setPayee] = useState(searchParams.get("payee") ?? "");
  const [gCashReference, setGCashReference] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [recordedId, setRecordedId] = useState<string | null>(null);
  const [recordedNumber, setRecordedNumber] = useState<string | null>(null);
  const [recordedAmount, setRecordedAmount] = useState<number | null>(null);

  if (!allowManage) {
    return (
      <ErrorState title={t("expense.errorTitle")} detail={t("expense.manageDenied")} />
    );
  }
  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  const activeCategories = categoriesQuery.data?.items ?? [];
  const amount = parseMoneyAmountInput(amountText);

  async function onSubmit() {
    if (!workspace || submitting || !online) {
      return;
    }
    setError(null);
    if (!categoryId) {
      setError(t("expense.validation.categoryRequired"));
      return;
    }
    if (amount === null || amount <= 0) {
      setError(t("expense.validation.amountInvalid"));
      return;
    }
    if (!description.trim()) {
      setError(t("expense.validation.descriptionRequired"));
      return;
    }
    if (description.trim().length > EXPENSE_DESCRIPTION_MAX) {
      setError(t("expense.validation.descriptionTooLong"));
      return;
    }
    if (payee.trim().length > EXPENSE_PAYEE_MAX) {
      setError(t("expense.validation.payeeTooLong"));
      return;
    }
    if (
      paymentMethod === "ManualGCash" &&
      gCashReference.trim().length > EXPENSE_GCASH_REFERENCE_MAX
    ) {
      setError(t("expense.validation.gCashRefTooLong"));
      return;
    }
    if (!expenseDate) {
      setError(t("expense.validation.dateRequired"));
      return;
    }

    setSubmitting(true);
    try {
      const generated = createSecureMutationId();
      if (!generated.ok) {
        setError(t("expense.recordFailed"));
        return;
      }
      const created = await recordExpense(workspace, {
        expenseId: generated.id,
        categoryId,
        paymentMethod,
        amount,
        description: description.trim(),
        expenseDate,
        payee: payee.trim() || null,
        gCashReference: paymentMethod === "ManualGCash" ? gCashReference.trim() || null : null,
      });
      setRecordedId(created.expenseId);
      setRecordedNumber(created.expenseNumber);
      setRecordedAmount(created.amount);
      await queryClient.invalidateQueries({ queryKey: ["expenses"] });
      await queryClient.invalidateQueries({ queryKey: ["expenses-summary"] });
    } catch (err) {
      if (isLikelyNetworkFailure(err)) {
        setError(t("expense.networkError"));
      } else if (err instanceof PosApiError) {
        setError(err.problem.detail ?? t("expense.recordFailed"));
      } else {
        setError(t("expense.recordFailed"));
      }
    } finally {
      setSubmitting(false);
    }
  }

  if (recordedId && recordedNumber != null && recordedAmount != null) {
    return (
      <div className="exits-page flex min-w-0 flex-col gap-4" data-testid="expense-record-success">
        <PageHeader
          title={t("expense.recordedTitle")}
          description={t("expense.recordedLede")}
          backTo="/expenses"
          backLabel={t("expense.backList")}
        />
        <Card className="flex flex-col gap-2 p-4">
          <p className="m-0 font-semibold" data-testid="expense-recorded-number">
            {recordedNumber}
          </p>
          <p className="m-0" data-testid="expense-recorded-amount">
            <MoneyDisplay amount={recordedAmount} />
          </p>
        </Card>
        <div className="flex flex-wrap gap-2">
          <Button asChild data-testid="expense-view-recorded">
            <Link to={`/expenses/${recordedId}`}>{t("expense.viewExpense")}</Link>
          </Button>
          <Button
            type="button"
            variant="outline"
            data-testid="expense-record-another"
            onClick={() => {
              setRecordedId(null);
              setRecordedNumber(null);
              setRecordedAmount(null);
              setAmountText("");
              setDescription("");
              setPayee("");
              setGCashReference("");
              setExpenseDate(todayExpenseDateInput());
            }}
          >
            {t("expense.recordAnother")}
          </Button>
          <Button asChild variant="ghost">
            <Link to="/expenses">{t("expense.backList")}</Link>
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div className="exits-page flex min-w-0 flex-col gap-4 pb-24" data-testid="expense-create-page">
      <PageHeader
        title={t("expense.recordExpense")}
        description={t("expense.recordLede")}
        backTo="/expenses"
        backLabel={t("expense.backList")}
        backTestId="page-header-back-expenses"
      />

      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="expense-immutable-hint">
        {t("expense.immutableHint")}
      </p>

      {!online ? (
        <ErrorState title={t("expense.errorTitle")} detail={t("expense.offline")} />
      ) : null}

      {categoriesQuery.isLoading ? <LoadingState label={t("expense.loadingCategories")} /> : null}
      {categoriesQuery.isError ? (
        <ErrorState title={t("expense.errorTitle")} detail={t("expense.categoriesLoadFailed")} />
      ) : null}
      {categoriesQuery.isSuccess && activeCategories.length === 0 ? (
        <div data-testid="expense-no-categories">
          <EmptyState
            title={t("expense.noActiveCategories")}
            detail={t("expense.noActiveCategoriesDetail")}
          />
          <div className="mt-3">
            <Button asChild data-testid="expense-create-category-cta">
              <Link to="/expenses/categories">{t("expense.createCategory")}</Link>
            </Button>
          </div>
        </div>
      ) : null}

      {error ? <ErrorState title={t("expense.errorTitle")} detail={error} /> : null}

      {activeCategories.length > 0 ? (
        <div className="grid gap-4 lg:grid-cols-2">
          <Card className="flex flex-col gap-3 p-3">
            <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
              <span className="font-medium">{t("expense.category")}</span>
              <select
                className="exits-select"
                value={categoryId}
                onChange={(e) => setCategoryId(e.target.value)}
                data-testid="expense-category"
              >
                <option value="">{t("expense.categoryPlaceholder")}</option>
                {activeCategories.map((cat) => (
                  <option key={cat.categoryId} value={cat.categoryId}>
                    {cat.name}
                  </option>
                ))}
              </select>
            </label>

            <fieldset className="m-0 border-0 p-0">
              <legend className="mb-2 text-[length:var(--exits-text-sm)] font-medium">
                {t("expense.paymentMethod")}
              </legend>
              <div className="flex flex-wrap gap-2" data-testid="expense-payment-method">
                {EXPENSE_PAYMENT_METHODS.map((method) => (
                  <Button
                    key={method}
                    type="button"
                    variant={paymentMethod === method ? "default" : "outline"}
                    data-testid={`expense-payment-${method}`}
                    aria-pressed={paymentMethod === method}
                    onClick={() => setPaymentMethod(method)}
                  >
                    {t(expensePaymentLabelKey(method))}
                  </Button>
                ))}
              </div>
            </fieldset>

            <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
              <span className="font-medium">{t("expense.expenseDate")}</span>
              <input
                type="date"
                className="exits-input"
                value={expenseDate}
                onChange={(e) => setExpenseDate(e.target.value)}
                data-testid="expense-date"
              />
            </label>
          </Card>

          <Card className="flex flex-col gap-3 p-3">
            <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
              <span className="font-medium">{t("expense.amount")}</span>
              <input
                inputMode="decimal"
                className="exits-input tabular-nums"
                value={amountText}
                onChange={(e) => setAmountText(normalizeMoneyAmountTyping(e.target.value))}
                onBlur={() => {
                  const parsed = parseMoneyAmountInput(amountText);
                  if (parsed !== null) {
                    setAmountText(formatMoneyAmountInput(parsed));
                  }
                }}
                placeholder="0.00"
                data-testid="expense-amount"
                autoComplete="off"
              />
            </label>

            <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
              <span className="font-medium">
                {t("expense.payee")}{" "}
                <span className="font-normal text-muted">({t("expense.optional")})</span>
              </span>
              <input
                className="exits-input"
                value={payee}
                maxLength={EXPENSE_PAYEE_MAX}
                onChange={(e) => setPayee(e.target.value)}
                data-testid="expense-payee"
              />
            </label>

            {paymentMethod === "ManualGCash" ? (
              <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                <span className="font-medium">
                  {t("expense.gCashReference")}{" "}
                  <span className="font-normal text-muted">({t("expense.optional")})</span>
                </span>
                <input
                  className="exits-input"
                  value={gCashReference}
                  maxLength={EXPENSE_GCASH_REFERENCE_MAX}
                  onChange={(e) => setGCashReference(e.target.value)}
                  data-testid="expense-gcash-reference"
                />
              </label>
            ) : null}
          </Card>

          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)] lg:col-span-2">
            <span className="font-medium">{t("expense.description")}</span>
            <textarea
              className="exits-input min-h-24"
              value={description}
              maxLength={EXPENSE_DESCRIPTION_MAX}
              onChange={(e) => setDescription(e.target.value)}
              data-testid="expense-description"
            />
            <span className="text-muted">
              {description.trim().length}/{EXPENSE_DESCRIPTION_MAX}
            </span>
          </label>
        </div>
      ) : null}

      {activeCategories.length > 0 ? (
        <StickyActionBar>
          <Button
            type="button"
            className="w-full sm:w-auto"
            disabled={!online || submitting}
            onClick={() => void onSubmit()}
            data-testid="expense-submit"
          >
            {submitting ? t("expense.recording") : t("expense.recordExpense")}
          </Button>
        </StickyActionBar>
      ) : null}
    </div>
  );
}
