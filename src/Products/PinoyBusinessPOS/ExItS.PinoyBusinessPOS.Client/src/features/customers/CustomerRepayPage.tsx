import { useEffect, useMemo, useState } from "react";
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
import { useBrowserOnline } from "@/connectivity/browser-online";
import { describePosApiError } from "@/access/pos-commercial-errors";
import { useI18n } from "@/i18n/I18nProvider";
import { createSecureMutationId } from "@/lib/secure-mutation-id";
import {
  cacheCustomer,
  cacheCustomerCreditSummary,
  getCachedCustomer,
  getCachedCustomerCreditSummary,
} from "@/offline/customer-cache";
import {
  enqueueOfflineCustomerRepayment,
  OfflineCustomerRejectedError,
} from "@/offline/customer-offline";
import { useOfflineSync } from "@/offline/OfflineSyncProvider";
import { useOrganizationOfflineContext } from "@/offline/organization-offline-context";
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
  const online = useBrowserOnline();
  const offlineContext = useOrganizationOfflineContext();
  const { refreshCounts } = useOfflineSync();
  const [paymentAmount, setPaymentAmount] = useState("");
  const [remarks, setRemarks] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [cachedName, setCachedName] = useState<string | null>(null);
  const [cachedOwed, setCachedOwed] = useState<number | null>(null);

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const customerQuery = useQuery({
    queryKey: ["customers", "detail", workspace?.organizationId, customerId],
    enabled: Boolean(workspace) && Boolean(customerId) && online,
    queryFn: ({ signal }) => getCustomer(workspace!, customerId!, signal),
  });

  const summaryQuery = useQuery({
    queryKey: ["customers", "credit-summary", workspace?.organizationId, customerId],
    enabled: Boolean(workspace) && Boolean(customerId) && online,
    queryFn: ({ signal }) => getCustomerCreditSummary(workspace!, customerId!, signal),
  });

  // Write-through so the balance a cashier saw online is the balance shown offline. The server
  // still re-checks the payment against the live balance on sync.
  useEffect(() => {
    if (!offlineContext || !online) {
      return;
    }
    if (customerQuery.data) {
      void cacheCustomer(offlineContext.db, offlineContext.scopeBinding, customerQuery.data).catch(
        () => {},
      );
    }
    if (summaryQuery.data) {
      void cacheCustomerCreditSummary(
        offlineContext.db,
        offlineContext.scopeBinding,
        summaryQuery.data,
      ).catch(() => {});
    }
  }, [customerQuery.data, offlineContext, online, summaryQuery.data]);

  useEffect(() => {
    if (!offlineContext || online || !customerId) {
      return;
    }
    let cancelled = false;
    void Promise.all([
      getCachedCustomer(offlineContext.db, offlineContext.scopeBinding, customerId),
      getCachedCustomerCreditSummary(offlineContext.db, offlineContext.scopeBinding, customerId),
    ]).then(([customer, summary]) => {
      if (cancelled) {
        return;
      }
      setCachedName(customer?.displayName ?? null);
      setCachedOwed(summary?.outstandingAmount ?? null);
    });
    return () => {
      cancelled = true;
    };
  }, [customerId, offlineContext, online]);

  if (!workspace || !customerId) {
    return <LoadingState label={t("session.loading")} />;
  }

  if (customerQuery.isLoading || summaryQuery.isLoading) {
    return <LoadingState label={t("loading.label")} />;
  }

  const displayName = customerQuery.data?.displayName ?? cachedName;

  if (!displayName) {
    return (
      <ErrorState
        title={t("error.title")}
        detail={
          online
            ? ((customerQuery.error as Error | undefined)?.message ?? t("customers.notFound"))
            : t("offline.customerNotCached")
        }
      />
    );
  }

  const amountOwed = summaryQuery.data?.outstandingAmount ?? cachedOwed ?? 0;
  const usingCachedBalance = !online && summaryQuery.data === undefined && cachedOwed !== null;
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
      if (!online) {
        await queuePaymentOffline(amount);
        return;
      }
      await createCustomerRepayment(workspace, customerId, {
        amount,
        remarks,
      });
      navigate(`/customers/${customerId}`, { replace: true });
    } catch (err) {
      setError(describePosApiError(err, t, "error.detail"));
    } finally {
      setSaving(false);
    }
  }

  /**
   * Queue the payment with a client-chosen repaymentId. The server adopts that id and honours the
   * idempotency key, so a retry records one payment; the server — not this device — still decides
   * whether the amount is acceptable against the live balance.
   */
  async function queuePaymentOffline(amount: number) {
    if (!offlineContext || !customerId) {
      setError(t("offline.paymentEnqueueFailed"));
      return;
    }
    const generated = createSecureMutationId();
    if (!generated.ok) {
      setError(t("offline.paymentEnqueueFailed"));
      return;
    }
    try {
      await enqueueOfflineCustomerRepayment({
        ...offlineContext,
        customerId,
        repaymentId: generated.id,
        repayment: { amount, remarks },
      });
      await refreshCounts();
      navigate(`/customers/${customerId}`, { replace: true });
    } catch (err) {
      setError(
        err instanceof OfflineCustomerRejectedError
          ? err.message
          : t("offline.paymentEnqueueFailed"),
      );
    }
  }

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="customer-repay-page">
      <PageHeader
        title={t("customers.repayTitle")}
        description={t("customers.repayLede").replace("{name}", displayName)}
      />

      <Card data-testid="customer-repay-owed">
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
          {t("customers.amountOwed")}
        </p>
        <p className="mb-0 mt-1 font-semibold">
          <MoneyDisplay amount={amountOwed} />
        </p>
        {usingCachedBalance ? (
          <p
            className="mb-0 mt-1 text-[length:var(--exits-text-xs)] text-muted"
            data-testid="customer-repay-cached-balance"
          >
            {t("offline.cachedBalanceNotice")}
          </p>
        ) : null}
      </Card>

      {!online ? (
        <Card data-testid="customer-repay-offline-notice">
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("offline.paymentWillQueue")}
          </p>
        </Card>
      ) : null}

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
