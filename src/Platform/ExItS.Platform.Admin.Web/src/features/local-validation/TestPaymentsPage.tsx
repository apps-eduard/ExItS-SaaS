import { useEffect, useMemo, useState, type FormEvent } from "react";
import { Link } from "react-router-dom";
import { FlaskConical } from "lucide-react";
import { PLATFORM_PERMISSIONS } from "@/api/authorization/authorization-types";
import { createCorrelationId } from "@/api/platform-http";
import type { LocalValidationPaymentSimulationResult } from "@/api/payments/payment-mutations-client";
import { paymentsListHref } from "@/api/payments/payment-client";
import { subscriptionDetailHref } from "@/api/subscriptions/subscription-portfolio-query";
import { PageHeader } from "@/components/exits/PageHeader";
import { ErrorState } from "@/components/exits/ErrorState";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Skeleton } from "@/components/ui/skeleton";
import { useSimulateLocalValidationPaymentMutation } from "@/features/commercial/use-commercial-mutations";
import {
  apiValueForSimulationLabel,
  isGuid,
  TEST_PAYMENT_BILLING_CYCLES,
  TEST_PAYMENT_SIMULATION_OPTIONS,
  type TestPaymentBillingCycle,
  type TestPaymentSimulationLabel,
} from "@/features/local-validation/test-payments-simulations";
import {
  useTestPaymentsOrganizationsQuery,
  useTestPaymentsSubscriptionsQuery,
} from "@/features/local-validation/use-test-payments-queries";
import { ForbiddenState } from "@/features/overview/ForbiddenState";
import { ShellNotFoundPage } from "@/features/overview/ShellNotFoundPage";
import { useAuthorization } from "@/hooks/use-authorization";
import { usePreferences } from "@/hooks/use-preferences";
import { areDevelopmentToolsAllowed } from "@/lib/auth/development-tools";
import { normalizeDiagnosticError } from "@/lib/diagnostics/normalize-diagnostic-error";
import { env } from "@/lib/env";

const controlClass =
  "h-[var(--exits-control-height)] min-h-[var(--exits-touch-target-min)] w-full rounded-[var(--exits-density-radius)] border border-input bg-surface px-3 text-[length:var(--exits-text-sm)] text-foreground";

function useDebouncedValue(value: string, delayMs: number): string {
  const [debounced, setDebounced] = useState(value);
  useEffect(() => {
    const handle = window.setTimeout(() => setDebounced(value), delayMs);
    return () => window.clearTimeout(handle);
  }, [value, delayMs]);
  return debounced;
}

export function TestPaymentsPage() {
  const { t } = usePreferences();
  const authorization = useAuthorization();
  const developmentToolsAllowed = areDevelopmentToolsAllowed();
  const localValidationEnabled = env.localValidationToolsEnabled;
  const canManage =
    authorization.status === "loaded" &&
    authorization.hasPermission(PLATFORM_PERMISSIONS.manageSubscriptions);

  if (authorization.status === "loading") {
    return (
      <section aria-busy="true">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="mt-3 h-16 w-full max-w-xl" />
      </section>
    );
  }

  if (!developmentToolsAllowed) {
    return <ShellNotFoundPage />;
  }

  if (!canManage) {
    return <ForbiddenState requiredPermission={PLATFORM_PERMISSIONS.manageSubscriptions} />;
  }

  return (
    <section className="grid gap-4">
      <PageHeader
        title={t("testPayments.title")}
        description={t("testPayments.description")}
      />
      <Alert title={t("testPayments.warningTitle")} tone="danger">
        {t("testPayments.warningBody")}
      </Alert>
      {!localValidationEnabled ? (
        <Alert title={t("testPayments.unavailableTitle")} tone="info">
          {t("testPayments.unavailableBody")}
        </Alert>
      ) : (
        <TestPaymentsForm enabled={canManage && localValidationEnabled} />
      )}
    </section>
  );
}

function TestPaymentsForm({ enabled }: { enabled: boolean }) {
  const { t } = usePreferences();
  const simulate = useSimulateLocalValidationPaymentMutation();

  const [organizationId, setOrganizationId] = useState("");
  const [organizationSearch, setOrganizationSearch] = useState("");
  const [subscriptionId, setSubscriptionId] = useState("");
  const [amount, setAmount] = useState("999");
  const [currencyCode, setCurrencyCode] = useState("PHP");
  const [billingCycle, setBillingCycle] = useState<TestPaymentBillingCycle>("Monthly");
  const [simulation, setSimulation] = useState<TestPaymentSimulationLabel>("Succeeded");
  const [validationError, setValidationError] = useState<string | null>(null);
  const [result, setResult] = useState<LocalValidationPaymentSimulationResult | null>(null);
  const [lastError, setLastError] = useState<unknown>(null);

  const debouncedOrgSearch = useDebouncedValue(organizationSearch, 250);
  const organizationsQuery = useTestPaymentsOrganizationsQuery(debouncedOrgSearch, enabled);
  const subscriptionsQuery = useTestPaymentsSubscriptionsQuery(
    isGuid(organizationId) ? organizationId : null,
    enabled,
  );

  const diagnostic = lastError
    ? normalizeDiagnosticError({ error: lastError, operation: "Simulate test payment" })
    : null;

  const selectedOrgLabel = useMemo(() => {
    const match = organizationsQuery.data?.items.find((item) => item.id === organizationId);
    return match ? `${match.displayName} (${match.slug})` : null;
  }, [organizationId, organizationsQuery.data?.items]);

  function resetResultState() {
    setResult(null);
    setLastError(null);
    setValidationError(null);
  }

  function onOrganizationChange(nextId: string) {
    setOrganizationId(nextId);
    setSubscriptionId("");
    resetResultState();
    if (nextId) {
      const match = organizationsQuery.data?.items.find((item) => item.id === nextId);
      if (match) {
        setOrganizationSearch(match.displayName);
      }
    }
  }

  function onSubmit(event: FormEvent) {
    event.preventDefault();
    resetResultState();

    if (!isGuid(organizationId) || !isGuid(subscriptionId)) {
      setValidationError(t("testPayments.validation.ids"));
      return;
    }

    const amountNumber = Number(amount);
    if (!Number.isFinite(amountNumber) || amountNumber < 0) {
      setValidationError(t("testPayments.validation.amount"));
      return;
    }

    if (!currencyCode.trim()) {
      setValidationError(t("testPayments.validation.currency"));
      return;
    }

    const idempotencyKey = createCorrelationId().replace(/-/g, "");
    void simulate
      .mutateAsync({
        body: {
          simulation: apiValueForSimulationLabel(simulation),
          organizationId: organizationId.trim(),
          subscriptionId: subscriptionId.trim(),
          amount: amountNumber,
          currencyCode: currencyCode.trim().toUpperCase(),
          idempotencyKey,
          purpose: "admin-test",
          billingCycle,
        },
        localValidationToolsEnabled: env.localValidationToolsEnabled,
      })
      .then((payload) => {
        setResult(payload);
      })
      .catch((error: unknown) => {
        setLastError(error);
      });
  }

  return (
    <div className="grid max-w-3xl gap-4">
      <form
        className="grid gap-3 rounded-[var(--exits-density-radius)] border border-border bg-surface p-4"
        onSubmit={onSubmit}
        noValidate
      >
        <div className="flex items-center gap-2 text-[length:var(--exits-text-sm)] font-medium text-foreground">
          <FlaskConical className="size-4 text-destructive" aria-hidden="true" />
          {t("testPayments.formTitle")}
        </div>

        <div className="grid gap-1">
          <Label htmlFor="test-pay-org-search">{t("testPayments.field.organizationSearch")}</Label>
          <Input
            id="test-pay-org-search"
            className={controlClass}
            value={organizationSearch}
            disabled={simulate.isPending}
            placeholder={t("testPayments.field.organizationSearch")}
            onChange={(event) => {
              setOrganizationSearch(event.target.value);
              resetResultState();
            }}
            autoComplete="off"
          />
          <Label htmlFor="test-pay-org">{t("testPayments.field.organization")}</Label>
          <select
            id="test-pay-org"
            className={controlClass}
            value={organizationId}
            disabled={simulate.isPending || organizationsQuery.isPending}
            onChange={(event) => onOrganizationChange(event.target.value)}
          >
            <option value="">{t("testPayments.field.organizationPlaceholder")}</option>
            {organizationsQuery.data?.items.map((org) => (
              <option key={org.id} value={org.id}>
                {org.displayName} · {org.slug} · {org.status}
              </option>
            ))}
          </select>
          {selectedOrgLabel ? (
            <p className="text-[length:var(--exits-text-xs)] text-muted">{selectedOrgLabel}</p>
          ) : null}
          {organizationsQuery.isError ? (
            <p className="text-[length:var(--exits-text-xs)] text-destructive" role="alert">
              {t("testPayments.organizationsLoadError")}
            </p>
          ) : null}
        </div>

        <div className="grid gap-1">
          <Label htmlFor="test-pay-subscription">{t("testPayments.field.subscription")}</Label>
          <select
            id="test-pay-subscription"
            className={controlClass}
            value={subscriptionId}
            disabled={
              simulate.isPending || !isGuid(organizationId) || subscriptionsQuery.isPending
            }
            onChange={(event) => {
              setSubscriptionId(event.target.value);
              resetResultState();
            }}
          >
            <option value="">
              {!isGuid(organizationId)
                ? t("testPayments.field.subscriptionNeedOrg")
                : t("testPayments.field.subscriptionPlaceholder")}
            </option>
            {subscriptionsQuery.data?.items.map((sub) => (
              <option key={sub.id} value={sub.id}>
                {(sub.productDisplayName || sub.productCode) + " · " + sub.status + " · " + sub.id.slice(0, 8)}
              </option>
            ))}
          </select>
          {subscriptionsQuery.isError ? (
            <p className="text-[length:var(--exits-text-xs)] text-destructive" role="alert">
              {t("testPayments.subscriptionsLoadError")}
            </p>
          ) : null}
          {isGuid(organizationId) &&
          subscriptionsQuery.isSuccess &&
          (subscriptionsQuery.data?.items.length ?? 0) === 0 ? (
            <p className="text-[length:var(--exits-text-xs)] text-muted" role="status">
              {t("testPayments.subscriptionsEmpty")}
            </p>
          ) : null}
        </div>

        <div className="grid gap-3 sm:grid-cols-3">
          <div className="grid gap-1">
            <Label htmlFor="test-pay-amount">{t("testPayments.field.amount")}</Label>
            <Input
              id="test-pay-amount"
              className={controlClass}
              type="number"
              min={0}
              step="0.01"
              value={amount}
              disabled={simulate.isPending}
              onChange={(event) => {
                setAmount(event.target.value);
                resetResultState();
              }}
            />
          </div>
          <div className="grid gap-1">
            <Label htmlFor="test-pay-currency">{t("testPayments.field.currency")}</Label>
            <Input
              id="test-pay-currency"
              className={controlClass}
              value={currencyCode}
              disabled={simulate.isPending}
              maxLength={8}
              onChange={(event) => {
                setCurrencyCode(event.target.value);
                resetResultState();
              }}
            />
          </div>
          <div className="grid gap-1">
            <Label htmlFor="test-pay-cycle">{t("testPayments.field.billingCycle")}</Label>
            <select
              id="test-pay-cycle"
              className={controlClass}
              value={billingCycle}
              disabled={simulate.isPending}
              onChange={(event) => {
                setBillingCycle(event.target.value as TestPaymentBillingCycle);
                resetResultState();
              }}
            >
              {TEST_PAYMENT_BILLING_CYCLES.map((cycle) => (
                <option key={cycle} value={cycle}>
                  {cycle}
                </option>
              ))}
            </select>
          </div>
        </div>

        <fieldset className="grid gap-2">
          <legend className="text-[length:var(--exits-text-xs)] font-medium text-muted">
            {t("testPayments.field.simulation")}
          </legend>
          <div className="flex flex-wrap gap-2">
            {TEST_PAYMENT_SIMULATION_OPTIONS.map((option) => (
              <Button
                key={option.label}
                type="button"
                size="sm"
                variant={simulation === option.label ? "default" : "outline"}
                disabled={simulate.isPending}
                aria-pressed={simulation === option.label}
                onClick={() => {
                  setSimulation(option.label);
                  resetResultState();
                }}
              >
                {option.label}
              </Button>
            ))}
          </div>
          <p className="text-[length:var(--exits-text-xs)] text-muted">{t("testPayments.noCardData")}</p>
        </fieldset>

        {validationError ? (
          <p className="text-[length:var(--exits-text-sm)] text-destructive" role="alert">
            {validationError}
          </p>
        ) : null}

        <div className="flex flex-wrap gap-2">
          <Button type="submit" disabled={simulate.isPending}>
            {simulate.isPending ? t("testPayments.submitting") : t("testPayments.submit")}
          </Button>
        </div>
      </form>

      {diagnostic ? (
        <ErrorState
          diagnostic={diagnostic}
          title={t("testPayments.errorTitle")}
          headingLevel="h2"
          onRetry={() => {
            setLastError(null);
          }}
        />
      ) : null}

      {result ? (
        <div
          className="grid gap-2 rounded-[var(--exits-density-radius)] border border-border bg-surface p-4"
          role="status"
        >
          <h2 className="text-[length:var(--exits-text-base)] font-semibold text-foreground">
            {t("testPayments.resultTitle")}
          </h2>
          <dl className="grid gap-1 text-[length:var(--exits-text-sm)]">
            <div className="flex flex-wrap gap-2">
              <dt className="text-muted">{t("testPayments.result.status")}</dt>
              <dd className="font-medium">{result.status}</dd>
            </div>
            <div className="flex flex-wrap gap-2">
              <dt className="text-muted">{t("testPayments.result.provider")}</dt>
              <dd className="font-mono text-[length:var(--exits-text-xs)]">{result.provider}</dd>
            </div>
            <div className="flex flex-wrap gap-2">
              <dt className="text-muted">{t("testPayments.result.reference")}</dt>
              <dd className="font-mono text-[length:var(--exits-text-xs)]">{result.providerReference}</dd>
            </div>
            <div className="flex flex-wrap gap-2">
              <dt className="text-muted">{t("testPayments.result.amount")}</dt>
              <dd>
                {result.amount} {result.currencyCode}
              </dd>
            </div>
            {result.failureCode || result.failureMessage ? (
              <div className="mt-1 rounded-[var(--exits-density-radius)] border border-destructive/40 bg-[var(--exits-danger-bg)] p-2">
                <p className="font-medium text-destructive">
                  {result.failureCode ?? t("testPayments.result.failure")}
                </p>
                {result.failureMessage ? (
                  <p className="mt-0.5 text-[length:var(--exits-text-sm)] text-foreground">
                    {result.failureMessage}
                  </p>
                ) : null}
              </div>
            ) : null}
          </dl>
          <div className="mt-2 flex flex-wrap gap-2">
            {isGuid(subscriptionId) ? (
              <Button asChild size="sm" variant="outline">
                <Link to={subscriptionDetailHref(subscriptionId)}>
                  {t("testPayments.link.subscription")}
                </Link>
              </Button>
            ) : null}
            <Button asChild size="sm" variant="outline">
              <Link to={paymentsListHref()}>{t("testPayments.link.payments")}</Link>
            </Button>
          </div>
          <p className="text-[length:var(--exits-text-xs)] text-muted">{t("testPayments.result.note")}</p>
        </div>
      ) : null}
    </div>
  );
}
