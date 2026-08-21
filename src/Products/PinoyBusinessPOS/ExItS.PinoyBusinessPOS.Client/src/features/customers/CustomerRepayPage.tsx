import { useMemo, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import {
  createCustomerRepayment,
  getCustomer,
  getCustomerCreditSummary,
} from "@/api/pos/pos-customers-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { PageHeader } from "@/components/exits/PageHeader";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

function parsePaymentAmount(raw: string): number | null {
  const trimmed = raw.trim();
  if (!trimmed) {
    return null;
  }
  const value = Number(trimmed);
  if (!Number.isFinite(value) || value <= 0) {
    return null;
  }
  return Math.round(value * 100) / 100;
}

export function CustomerRepayPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { customerId } = useParams<{ customerId: string }>();
  const { boundWorkspace } = useWorkspace();
  const [paymentAmount, setPaymentAmount] = useState("");
  const [remarks, setRemarks] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const customerQuery = useQuery({
    queryKey: ["customers", "detail", workspace?.organizationId, customerId],
    enabled: Boolean(workspace) && Boolean(customerId),
    queryFn: ({ signal }) => getCustomer(workspace!, customerId!, signal),
  });

  const summaryQuery = useQuery({
    queryKey: ["customers", "credit-summary", workspace?.organizationId, customerId],
    enabled: Boolean(workspace) && Boolean(customerId),
    queryFn: ({ signal }) => getCustomerCreditSummary(workspace!, customerId!, signal),
  });

  if (!workspace || !customerId) {
    return <LoadingState label={t("session.loading")} />;
  }

  if (customerQuery.isLoading || summaryQuery.isLoading) {
    return <LoadingState label={t("loading.label")} />;
  }

  if (customerQuery.isError || !customerQuery.data) {
    return (
      <ErrorState
        title={t("error.title")}
        detail={(customerQuery.error as Error | undefined)?.message ?? t("customers.notFound")}
      />
    );
  }

  const amountOwed = summaryQuery.data?.outstandingAmount ?? 0;
  const parsed = parsePaymentAmount(paymentAmount);
  const remainingPreview =
    parsed === null ? null : Math.round(Math.max(0, amountOwed - parsed) * 100) / 100;

  async function onSubmit() {
    if (!workspace || !customerId) {
      return;
    }
    const amount = parsePaymentAmount(paymentAmount);
    if (amount === null) {
      setError(t("customers.paymentInvalid"));
      return;
    }
    if (amount - amountOwed > 1e-9) {
      setError(t("customers.paymentExceeds"));
      return;
    }
    setSaving(true);
    setError(null);
    try {
      await createCustomerRepayment(workspace, customerId, {
        amount,
        remarks,
      });
      navigate(`/customers/${customerId}`, { replace: true });
    } catch (err) {
      setError(err instanceof Error ? err.message : t("error.detail"));
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="customer-repay-page">
      <PageHeader
        title={t("customers.repayTitle")}
        description={t("customers.repayLede").replace("{name}", customerQuery.data.displayName)}
      />

      <Card data-testid="customer-repay-owed">
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
          {t("customers.amountOwed")}
        </p>
        <p className="mb-0 mt-1 font-semibold">
          <MoneyDisplay amount={amountOwed} />
        </p>
      </Card>

      {error ? (
        <Card data-testid="customer-repay-error">
          <p className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]">
            {error}
          </p>
        </Card>
      ) : null}

      <Card className="flex flex-col gap-3">
        <label
          className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]"
          htmlFor="customer-payment-amount"
        >
          {t("customers.payment")}
          <input
            id="customer-payment-amount"
            data-testid="customer-payment-amount"
            type="number"
            min="0.01"
            step="0.01"
            inputMode="decimal"
            className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
            value={paymentAmount}
            disabled={saving || amountOwed <= 0}
            onChange={(event) => setPaymentAmount(event.target.value)}
          />
        </label>
        <label
          className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]"
          htmlFor="customer-payment-remarks"
        >
          {t("customers.paymentNotes")}
          <input
            id="customer-payment-remarks"
            data-testid="customer-payment-remarks"
            className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
            value={remarks}
            disabled={saving}
            onChange={(event) => setRemarks(event.target.value)}
          />
        </label>
        {remainingPreview !== null ? (
          <p
            className="mb-0 text-[length:var(--exits-text-sm)]"
            data-testid="customer-remaining-balance"
          >
            {t("customers.remainingBalance")}: <MoneyDisplay amount={remainingPreview} />
          </p>
        ) : null}
      </Card>

      <div className="flex flex-wrap gap-2">
        <Button
          type="button"
          className="min-h-11"
          data-testid="customer-payment-submit"
          disabled={saving || amountOwed <= 0}
          onClick={() => void onSubmit()}
        >
          {saving ? t("customers.saving") : t("customers.recordPayment")}
        </Button>
        <Button asChild variant="ghost" className="min-h-11" disabled={saving}>
          <Link to={`/customers/${customerId}`}>{t("customers.backDetail")}</Link>
        </Button>
      </div>
    </div>
  );
}
