import { useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useMutation, useQuery } from "@tanstack/react-query";
import {
  getPersonalSubscriptionPayment,
  processPersonalSubscriptionPaymentSimulator,
  SUBSCRIPTION_CARD_SIMULATOR,
  type SubscriptionPaymentChannel,
} from "@/api/platform/subscription-payment-client";
import { PlatformApiError } from "@/api/platform/platform-http";
import { Button } from "@/components/ui/button";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { Notice } from "@/components/exits/Notice";
import { PageHeader } from "@/components/exits/PageHeader";
import {
  formatPaymentMoney,
  paymentResultPath,
} from "@/features/subscription-checkout/checkout-helpers";
import { useI18n } from "@/i18n/I18nProvider";

type ChannelSlug = "gcash" | "maya" | "card";

function channelFromSlug(slug: string | undefined): SubscriptionPaymentChannel | null {
  switch ((slug ?? "").toLowerCase()) {
    case "gcash":
      return "GCash";
    case "maya":
      return "Maya";
    case "card":
      return "Card";
    default:
      return null;
  }
}

export function SubscriptionPaymentSimulatorPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { paymentId = "", channel: channelSlug } = useParams();
  const channel = channelFromSlug(channelSlug);

  const [cardNumber, setCardNumber] = useState("");
  const [cardExpiry, setCardExpiry] = useState("");
  const [cardName, setCardName] = useState("");
  const [cardCvv, setCardCvv] = useState("");
  const [ewalletOutcome, setEwalletOutcome] = useState<"succeed" | "fail" | "processing">(
    "succeed",
  );
  const [formError, setFormError] = useState<string | null>(null);

  const paymentQuery = useQuery({
    queryKey: ["subscription-payment", "personal", paymentId],
    queryFn: ({ signal }) => getPersonalSubscriptionPayment(paymentId, signal),
    enabled: Boolean(paymentId && channel),
  });

  const processMutation = useMutation({
    mutationFn: async () => {
      if (!channel) {
        throw new Error(t("subscriptionCheckout.invalidChannel"));
      }
      if (channel === "Card") {
        const digits = cardNumber.replace(/\D/g, "");
        if (digits.length < 13) {
          throw new Error(t("subscriptionCheckout.card.invalidNumber"));
        }
        if (!cardExpiry.trim() || !cardName.trim() || !cardCvv.trim()) {
          throw new Error(t("subscriptionCheckout.card.requiredFields"));
        }
      }

      const result = await processPersonalSubscriptionPaymentSimulator(paymentId, {
        channel,
        cardNumber: channel === "Card" ? cardNumber.replace(/\D/g, "") : null,
        cardExpiry: channel === "Card" ? cardExpiry.trim() : null,
        cardName: channel === "Card" ? cardName.trim() : null,
        cardCvv: channel === "Card" ? cardCvv.trim() : null,
        simulationOutcome: channel === "Card" ? null : ewalletOutcome,
      });

      // Clear sensitive card fields immediately after submit (never retain CVV/PAN).
      setCardNumber("");
      setCardExpiry("");
      setCardName("");
      setCardCvv("");

      return result;
    },
    onSuccess: () => {
      navigate(paymentResultPath(paymentId), { replace: true });
    },
    onError: (error) => {
      if (error instanceof PlatformApiError) {
        setFormError(error.problem.detail ?? error.message);
        return;
      }
      setFormError(error instanceof Error ? error.message : t("subscriptionCheckout.processFailed"));
    },
  });

  if (!channel) {
    return (
      <div className="exits-page flex flex-col gap-3">
        <PageHeader title={t("subscriptionCheckout.simulatorTitle")} />
        <ErrorState
          title={t("subscriptionCheckout.errorTitle")}
          detail={t("subscriptionCheckout.invalidChannel")}
        />
      </div>
    );
  }

  if (paymentQuery.isPending) {
    return <LoadingSkeleton label={t("subscriptionCheckout.loading")} />;
  }

  if (paymentQuery.isError || !paymentQuery.data) {
    return (
      <div className="exits-page flex flex-col gap-3">
        <PageHeader title={t("subscriptionCheckout.simulatorTitle")} />
        <ErrorState
          title={t("subscriptionCheckout.errorTitle")}
          detail={t("subscriptionCheckout.errorDetail")}
        />
      </div>
    );
  }

  const payment = paymentQuery.data;
  const channelLabel =
    channel === "GCash"
      ? t("subscriptionCheckout.method.gcash")
      : channel === "Maya"
        ? t("subscriptionCheckout.method.maya")
        : t("subscriptionCheckout.method.card");

  function autofillSuccess() {
    setCardNumber(SUBSCRIPTION_CARD_SIMULATOR.successPan);
    setCardExpiry(SUBSCRIPTION_CARD_SIMULATOR.autofillExpiry);
    setCardName(SUBSCRIPTION_CARD_SIMULATOR.autofillName);
    setCardCvv(SUBSCRIPTION_CARD_SIMULATOR.autofillCvv);
  }

  function autofillDecline() {
    setCardNumber(SUBSCRIPTION_CARD_SIMULATOR.declinePan);
    setCardExpiry(SUBSCRIPTION_CARD_SIMULATOR.autofillExpiry);
    setCardName(SUBSCRIPTION_CARD_SIMULATOR.autofillName);
    setCardCvv(SUBSCRIPTION_CARD_SIMULATOR.autofillCvv);
  }

  function autofillProcessing() {
    setCardNumber(SUBSCRIPTION_CARD_SIMULATOR.pendingPan);
    setCardExpiry(SUBSCRIPTION_CARD_SIMULATOR.autofillExpiry);
    setCardName(SUBSCRIPTION_CARD_SIMULATOR.autofillName);
    setCardCvv(SUBSCRIPTION_CARD_SIMULATOR.autofillCvv);
  }

  return (
    <div
      className="exits-page mx-auto flex w-full max-w-lg flex-col gap-4"
      data-testid={`subscription-simulator-${(channelSlug as ChannelSlug) ?? "unknown"}`}
    >
      <PageHeader
        title={t("subscriptionCheckout.simulatorTitle").replace("{channel}", channelLabel)}
        description={t("subscriptionCheckout.simulatorLede")}
        backTo={`/subscription-checkout/${paymentId}`}
        backLabel={t("subscriptionCheckout.backToMethods")}
        backTestId="page-header-back-checkout-methods"
      />

      <Notice tone="warning" testId="subscription-simulator-test-banner" title={t("subscriptionCheckout.testBannerTitle")}>
        {t("subscriptionCheckout.testBannerBody")}
      </Notice>

      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
        {t("subscriptionCheckout.payingAmount").replace(
          "{amount}",
          formatPaymentMoney(payment.finalAmount, payment.currencyCode),
        )}
      </p>

      {channel === "Card" ? (
        <form
          className="exits-list__card flex flex-col gap-3 p-4"
          data-testid="subscription-card-form"
          onSubmit={(e) => {
            e.preventDefault();
            setFormError(null);
            processMutation.mutate();
          }}
        >
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            <span>{t("subscriptionCheckout.card.name")}</span>
            <input
              className="exits-input"
              autoComplete="cc-name"
              value={cardName}
              onChange={(e) => setCardName(e.target.value)}
              data-testid="card-name"
            />
          </label>
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            <span>{t("subscriptionCheckout.card.number")}</span>
            <input
              className="exits-input"
              inputMode="numeric"
              autoComplete="cc-number"
              value={cardNumber}
              onChange={(e) => setCardNumber(e.target.value)}
              data-testid="card-number"
            />
          </label>
          <div className="grid grid-cols-2 gap-3">
            <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
              <span>{t("subscriptionCheckout.card.expiry")}</span>
              <input
                className="exits-input"
                autoComplete="cc-exp"
                placeholder="MM/YY"
                value={cardExpiry}
                onChange={(e) => setCardExpiry(e.target.value)}
                data-testid="card-expiry"
              />
            </label>
            <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
              <span>{t("subscriptionCheckout.card.cvv")}</span>
              <input
                className="exits-input"
                inputMode="numeric"
                autoComplete="cc-csc"
                value={cardCvv}
                onChange={(e) => setCardCvv(e.target.value)}
                data-testid="card-cvv"
              />
            </label>
          </div>

          <div className="flex flex-col gap-1.5">
            <p className="m-0 text-[length:var(--exits-text-xs)] font-semibold text-muted">
              {t("subscriptionCheckout.card.testScenarios")}
            </p>
            <div className="flex flex-wrap gap-2">
              <Button type="button" variant="ghost" onClick={autofillSuccess} data-testid="card-autofill-success">
                {t("subscriptionCheckout.card.scenarioSuccess")}
              </Button>
              <Button type="button" variant="ghost" onClick={autofillDecline} data-testid="card-autofill-decline">
                {t("subscriptionCheckout.card.scenarioDecline")}
              </Button>
              <Button type="button" variant="ghost" onClick={autofillProcessing} data-testid="card-autofill-processing">
                {t("subscriptionCheckout.card.scenarioProcessing")}
              </Button>
            </div>
            <ul className="m-0 list-disc ps-4 text-[length:var(--exits-text-xs)] text-muted">
              <li>{t("subscriptionCheckout.card.ruleSuccess")}</li>
              <li>{t("subscriptionCheckout.card.ruleDecline")}</li>
              <li>{t("subscriptionCheckout.card.ruleProcessing")}</li>
            </ul>
          </div>

          {formError ? (
            <Notice tone="danger" testId="subscription-simulator-error">
              {formError}
            </Notice>
          ) : null}

          <Button type="submit" disabled={processMutation.isPending} data-testid="card-pay-submit">
            {processMutation.isPending
              ? t("subscriptionCheckout.processing")
              : t("subscriptionCheckout.payNow")}
          </Button>
        </form>
      ) : (
        <div className="exits-list__card flex flex-col gap-3 p-4" data-testid="subscription-ewallet-form">
          <p className="m-0 text-[length:var(--exits-text-sm)]">
            {t("subscriptionCheckout.ewalletHint").replace("{channel}", channelLabel)}
          </p>
          <fieldset className="m-0 flex flex-col gap-2 border-0 p-0">
            <legend className="mb-1 text-[length:var(--exits-text-sm)] font-medium">
              {t("subscriptionCheckout.simulationOutcome")}
            </legend>
            {(
              [
                ["succeed", t("subscriptionCheckout.outcome.succeed")],
                ["fail", t("subscriptionCheckout.outcome.fail")],
                ["processing", t("subscriptionCheckout.outcome.processing")],
              ] as const
            ).map(([value, label]) => (
              <label key={value} className="flex items-center gap-2 text-[length:var(--exits-text-sm)]">
                <input
                  type="radio"
                  name="ewallet-outcome"
                  checked={ewalletOutcome === value}
                  onChange={() => setEwalletOutcome(value)}
                  data-testid={`ewallet-outcome-${value}`}
                />
                {label}
              </label>
            ))}
          </fieldset>

          {formError ? (
            <Notice tone="danger" testId="subscription-simulator-error">
              {formError}
            </Notice>
          ) : null}

          <Button
            type="button"
            disabled={processMutation.isPending}
            data-testid="ewallet-pay-submit"
            onClick={() => {
              setFormError(null);
              processMutation.mutate();
            }}
          >
            {processMutation.isPending
              ? t("subscriptionCheckout.processing")
              : t("subscriptionCheckout.confirmSimulatedPay")}
          </Button>
        </div>
      )}
    </div>
  );
}
