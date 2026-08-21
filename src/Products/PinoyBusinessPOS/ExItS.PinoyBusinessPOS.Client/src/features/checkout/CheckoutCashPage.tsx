import { useEffect, useMemo, useRef, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { canApplyCommercialDiscount, canCreateSale } from "@/access/pos-capabilities";
import {
  checkoutSale,
  quoteSale,
  type CommercialDiscountIntentRequest,
  type PosSaleQuoteDto,
} from "@/api/pos/pos-sales-client";
import { roundMoney } from "@/cart/sell-cart-helpers";
import { lineAmount, useSessionCart } from "@/cart/SessionCartProvider";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { PageHeader } from "@/components/exits/PageHeader";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { describeCheckoutSaleError } from "@/features/checkout/checkout-sale-errors";
import { mapCartLinesToCheckoutRequest } from "@/features/checkout/map-cart-to-checkout";
import { useShiftContext } from "@/features/shifts/ShiftContextProvider";
import { useI18n } from "@/i18n/I18nProvider";
import { isPosDeviceReadyForMoney } from "@/workspace/pos-device-context";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type DiscountScope = CommercialDiscountIntentRequest["scope"];
type DiscountMethod = CommercialDiscountIntentRequest["method"];

type AppliedDiscount = CommercialDiscountIntentRequest & { localId: string };

function newSaleId(): string {
  return crypto.randomUUID();
}

function newLocalId(): string {
  return crypto.randomUUID();
}

function parseCashTender(raw: string): number | null {
  const trimmed = raw.trim();
  if (!/^\d+(\.\d{1,2})?$/.test(trimmed)) {
    return null;
  }
  const value = Number(trimmed);
  if (!Number.isFinite(value) || value < 0) {
    return null;
  }
  return value;
}

function parseDiscountValue(raw: string): number | null {
  const trimmed = raw.trim();
  if (!/^\d+(\.\d{1,2})?$/.test(trimmed)) {
    return null;
  }
  const value = Number(trimmed);
  if (!Number.isFinite(value) || value <= 0) {
    return null;
  }
  return value;
}

export function CheckoutCashPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { boundWorkspace, sessionGrant, posDevice } = useWorkspace();
  const cart = useSessionCart();
  const { readiness, currentShift, refresh } = useShiftContext();

  const [cashReceived, setCashReceived] = useState("");
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [appliedDiscounts, setAppliedDiscounts] = useState<AppliedDiscount[]>([]);
  const [discountScope, setDiscountScope] = useState<DiscountScope>("Sale");
  const [discountMethod, setDiscountMethod] = useState<DiscountMethod>("Percentage");
  const [discountValue, setDiscountValue] = useState("");
  const [discountReason, setDiscountReason] = useState("");
  const [discountLineNumber, setDiscountLineNumber] = useState(1);
  const [discountFormError, setDiscountFormError] = useState<string | null>(null);
  const [quote, setQuote] = useState<PosSaleQuoteDto | null>(null);
  const [quoteLoading, setQuoteLoading] = useState(false);
  const [quoteError, setQuoteError] = useState<string | null>(null);
  const attemptSaleIdRef = useRef<string>(newSaleId());
  const submittingRef = useRef(false);
  const completedRef = useRef(false);
  const lastSeededTotalRef = useRef<number | null>(null);
  const tenderEditedRef = useRef(false);

  const workspaceScope = useMemo(() => {
    if (!boundWorkspace?.branchId) {
      return null;
    }
    return {
      organizationId: boundWorkspace.organizationId,
      branchId: boundWorkspace.branchId,
    };
  }, [boundWorkspace]);

  const allowSale = canCreateSale(sessionGrant);
  const allowDiscount = canApplyCommercialDiscount(sessionGrant);
  const moneyReady = readiness.moneyPostReady === true;
  const deviceReady = isPosDeviceReadyForMoney(posDevice);

  const discountIntents = useMemo(
    () =>
      appliedDiscounts.map((item) => {
        const intent: CommercialDiscountIntentRequest = {
          scope: item.scope,
          method: item.method,
          value: item.value,
          reason: item.reason,
        };
        if (item.productId) {
          intent.productId = item.productId;
        }
        if (item.lineNumber !== undefined) {
          intent.lineNumber = item.lineNumber;
        }
        return intent;
      }),
    [appliedDiscounts],
  );
  const discountSignature = useMemo(() => JSON.stringify(discountIntents), [discountIntents]);
  const cartSignature = useMemo(
    () =>
      JSON.stringify(
        cart.lines.map((line) => ({
          productId: line.productId,
          quantity: line.quantity,
          productUnitId: line.productUnitId,
        })),
      ),
    [cart.lines],
  );

  const amountToPay = quote?.total ?? cart.subtotal;
  const totalAmount = quote?.grossSubtotal ?? cart.subtotal;
  const discountTotal = quote?.discountTotal ?? 0;
  const zeroTotal = amountToPay <= 1e-9;
  const parsedTender = zeroTotal ? 0 : parseCashTender(cashReceived);
  const tenderOk = zeroTotal || (parsedTender !== null && parsedTender + 1e-9 >= amountToPay);
  const changeAdvisory =
    !zeroTotal && tenderOk && parsedTender !== null
      ? roundMoney(Math.max(0, parsedTender - amountToPay))
      : zeroTotal
        ? 0
        : null;

  useEffect(() => {
    // Fail-closed gate pages must stay visible even with an empty cart.
    if (!moneyReady || !deviceReady || !readiness.shiftGateReady) {
      return;
    }
    if (completedRef.current) {
      return;
    }
    if (cart.lineCount === 0 && !saving) {
      navigate("/sell", { replace: true });
    }
  }, [cart.lineCount, deviceReady, moneyReady, navigate, readiness.shiftGateReady, saving]);

  useEffect(() => {
    if (!workspaceScope || cart.lineCount === 0 || !moneyReady || !deviceReady) {
      return;
    }

    const controller = new AbortController();
    const timer = window.setTimeout(() => {
      setQuoteLoading(true);
      setQuoteError(null);
      const intents = JSON.parse(discountSignature) as CommercialDiscountIntentRequest[];
      void quoteSale(
        workspaceScope,
        {
          lines: mapCartLinesToCheckoutRequest(cart.lines),
          paymentMethod: "Cash",
          discounts: allowDiscount && intents.length > 0 ? intents : undefined,
        },
        controller.signal,
      )
        .then((next) => {
          if (!controller.signal.aborted) {
            setQuote(next);
            setQuoteLoading(false);
          }
        })
        .catch((error) => {
          if (controller.signal.aborted) {
            return;
          }
          setQuote(null);
          setQuoteLoading(false);
          setQuoteError(describeCheckoutSaleError(error, t));
        });
    }, 200);

    return () => {
      controller.abort();
      window.clearTimeout(timer);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps -- cartSignature/discountSignature track content without array-identity thrash
  }, [allowDiscount, cartSignature, deviceReady, discountSignature, moneyReady, t, workspaceScope]);

  useEffect(() => {
    if (!quote) {
      return;
    }
    if (zeroTotal) {
      setCashReceived("0");
      lastSeededTotalRef.current = 0;
      tenderEditedRef.current = false;
      return;
    }
    if (lastSeededTotalRef.current === null) {
      if (!tenderEditedRef.current) {
        setCashReceived(amountToPay.toFixed(2));
      }
      lastSeededTotalRef.current = amountToPay;
      return;
    }
    if (Math.abs(lastSeededTotalRef.current - amountToPay) > 1e-9) {
      setCashReceived(amountToPay.toFixed(2));
      lastSeededTotalRef.current = amountToPay;
      tenderEditedRef.current = false;
    }
  }, [amountToPay, quote, zeroTotal]);

  function addDiscount() {
    setDiscountFormError(null);
    const reason = discountReason.trim();
    if (!reason) {
      setDiscountFormError(t("checkout.discountReasonRequired"));
      return;
    }
    const value = parseDiscountValue(discountValue);
    if (value === null) {
      setDiscountFormError(t("checkout.discountValueInvalid"));
      return;
    }
    if (discountScope === "Line") {
      if (discountLineNumber < 1 || discountLineNumber > cart.lines.length) {
        setDiscountFormError(t("checkout.discountValueInvalid"));
        return;
      }
    }

    const intent: AppliedDiscount = {
      localId: newLocalId(),
      scope: discountScope,
      method: discountMethod,
      value,
      reason,
      ...(discountScope === "Line" ? { lineNumber: discountLineNumber } : {}),
    };
    setAppliedDiscounts((prev) => [...prev, intent]);
    setDiscountValue("");
    setDiscountReason("");
  }

  function removeDiscount(localId: string) {
    setAppliedDiscounts((prev) => prev.filter((item) => item.localId !== localId));
  }

  if (!allowSale) {
    return (
      <div data-testid="checkout-denied" className="flex flex-col gap-3">
        <PageHeader title={t("checkout.title")} description={t("checkout.deniedDetail")} />
        <Button asChild variant="ghost" className="min-h-11 w-fit">
          <Link to="/">{t("notFound.home")}</Link>
        </Button>
      </div>
    );
  }

  if (!moneyReady || !deviceReady || !readiness.shiftGateReady || !readiness.shiftId) {
    const deviceBlocked = !deviceReady;
    return (
      <div data-testid="checkout-blocked" className="flex min-w-0 flex-col gap-4">
        <PageHeader title={t("checkout.title")} description={t("checkout.blockedLede")} />
        <Card data-testid="checkout-gate-message">
          <p className="m-0 text-[length:var(--exits-text-sm)]">
            {deviceBlocked
              ? t("checkout.blockedDevice")
              : readiness.status === "blocked_no_shift" || readiness.status === "blocked_closed"
                ? t("checkout.blockedShift")
                : t("checkout.blockedGeneric")}
          </p>
          <div className="mt-3 flex flex-wrap gap-2">
            {deviceBlocked ? (
              <Button asChild className="min-h-11" data-testid="checkout-register-device">
                <Link to="/devices/register">{t("checkout.registerDevice")}</Link>
              </Button>
            ) : (
              <Button asChild className="min-h-11" data-testid="checkout-open-shift">
                <Link to="/shifts/open">{t("shift.openTitle")}</Link>
              </Button>
            )}
            <Button asChild variant="ghost" className="min-h-11">
              <Link to="/sell">{t("checkout.backToCart")}</Link>
            </Button>
          </div>
        </Card>
      </div>
    );
  }

  if (!workspaceScope || cart.lineCount === 0) {
    return null;
  }

  async function onConfirm() {
    if (submittingRef.current || saving || !workspaceScope || !readiness.shiftId) {
      return;
    }
    if (quoteError || !quote) {
      setSubmitError(quoteError ?? t("checkout.quoteError"));
      return;
    }
    if (!zeroTotal && (parsedTender === null || !tenderOk)) {
      setSubmitError(t("checkout.tenderInvalid"));
      return;
    }

    submittingRef.current = true;
    setSaving(true);
    setSubmitError(null);

    const saleId = attemptSaleIdRef.current;
    const lines = mapCartLinesToCheckoutRequest(cart.lines);
    const amountTendered = zeroTotal ? 0 : Number((parsedTender as number).toFixed(2));

    try {
      await refresh();
      const sale = await checkoutSale(workspaceScope, {
        lines,
        paymentMethod: "Cash",
        amountTendered,
        saleId,
        shiftId: readiness.shiftId,
        discounts: allowDiscount && discountIntents.length > 0 ? discountIntents : undefined,
      });
      completedRef.current = true;
      cart.clear();
      attemptSaleIdRef.current = newSaleId();
      navigate(`/sell/sales/${sale.saleId}/summary`, { replace: true });
    } catch (error) {
      setSubmitError(describeCheckoutSaleError(error, t));
    } finally {
      submittingRef.current = false;
      setSaving(false);
    }
  }

  return (
    <div data-testid="checkout-cash-page" className="flex min-w-0 flex-col gap-4">
      <PageHeader title={t("checkout.title")} description={t("checkout.cashLede")} />

      {submitError ? (
        <Card data-testid="checkout-error">
          <p className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]">
            {submitError}
          </p>
        </Card>
      ) : null}

      <Card>
        <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
          {t("checkout.orderPreview")}
        </h2>
        <ul className="mb-0 mt-2 list-none space-y-2 p-0">
          {cart.lines.map((line, index) => (
            <li
              key={line.lineKey}
              className="flex items-start justify-between gap-2 text-[length:var(--exits-text-sm)]"
            >
              <span className="min-w-0 truncate">
                {index + 1}. {line.name} × {line.quantity} {line.unitLabel}
              </span>
              <MoneyDisplay amount={lineAmount(line)} />
            </li>
          ))}
        </ul>
        {currentShift ? (
          <p className="mb-0 mt-3 text-[length:var(--exits-text-xs)] text-muted">
            {t("checkout.shiftHint")
              .replace("{shift}", currentShift.shiftNumber)
              .replace("{register}", currentShift.registerCode ?? "—")}
          </p>
        ) : null}
      </Card>

      {allowDiscount ? (
        <Card data-testid="checkout-discount-panel">
          <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
            {t("checkout.discountSection")}
          </h2>
          <p className="mb-0 mt-1 text-[length:var(--exits-text-xs)] text-muted">
            {t("checkout.discountLede")}
          </p>

          {appliedDiscounts.length === 0 ? (
            <p
              data-testid="checkout-discount-empty"
              className="mb-0 mt-3 text-[length:var(--exits-text-sm)] text-muted"
            >
              {t("checkout.discountEmpty")}
            </p>
          ) : (
            <ul className="mb-0 mt-3 list-none space-y-2 p-0" data-testid="checkout-discount-list">
              {appliedDiscounts.map((item) => (
                <li
                  key={item.localId}
                  className="flex items-start justify-between gap-2 text-[length:var(--exits-text-sm)]"
                  data-testid={`checkout-discount-item-${item.localId}`}
                >
                  <span className="min-w-0">
                    {item.scope === "Sale"
                      ? t("checkout.discountScopeSale")
                      : `${t("checkout.discountScopeLine")} #${item.lineNumber}`}{" "}
                    · {item.method === "Percentage" ? `${item.value}%` : item.value} · {item.reason}
                  </span>
                  <Button
                    type="button"
                    variant="ghost"
                    className="min-h-9 shrink-0"
                    data-testid={`checkout-discount-remove-${item.localId}`}
                    disabled={saving}
                    onClick={() => removeDiscount(item.localId)}
                  >
                    {t("checkout.discountRemove")}
                  </Button>
                </li>
              ))}
            </ul>
          )}

          <div className="mt-3 grid gap-2 sm:grid-cols-2">
            <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
              {t("checkout.discountScope")}
              <select
                data-testid="checkout-discount-scope"
                className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
                value={discountScope}
                disabled={saving}
                onChange={(event) => setDiscountScope(event.target.value as DiscountScope)}
              >
                <option value="Sale">{t("checkout.discountScopeSale")}</option>
                <option value="Line">{t("checkout.discountScopeLine")}</option>
              </select>
            </label>
            <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
              {t("checkout.discountMethod")}
              <select
                data-testid="checkout-discount-method"
                className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
                value={discountMethod}
                disabled={saving}
                onChange={(event) => setDiscountMethod(event.target.value as DiscountMethod)}
              >
                <option value="Percentage">{t("checkout.discountMethodPercent")}</option>
                <option value="FixedAmount">{t("checkout.discountMethodFixed")}</option>
              </select>
            </label>
            {discountScope === "Line" ? (
              <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                {t("checkout.discountLine")}
                <select
                  data-testid="checkout-discount-line"
                  className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
                  value={discountLineNumber}
                  disabled={saving}
                  onChange={(event) => setDiscountLineNumber(Number(event.target.value))}
                >
                  {cart.lines.map((line, index) => (
                    <option key={line.lineKey} value={index + 1}>
                      {index + 1}. {line.name}
                    </option>
                  ))}
                </select>
              </label>
            ) : null}
            <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
              {t("checkout.discountValue")}
              <input
                data-testid="checkout-discount-value"
                type="number"
                inputMode="decimal"
                min={0}
                step="0.01"
                value={discountValue}
                disabled={saving}
                className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 tabular-nums"
                onChange={(event) => setDiscountValue(event.target.value)}
              />
            </label>
            <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)] sm:col-span-2">
              {t("checkout.discountReason")}
              <input
                data-testid="checkout-discount-reason"
                type="text"
                value={discountReason}
                disabled={saving}
                className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
                onChange={(event) => setDiscountReason(event.target.value)}
              />
            </label>
          </div>
          {discountFormError ? (
            <p
              data-testid="checkout-discount-form-error"
              className="mb-0 mt-2 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]"
            >
              {discountFormError}
            </p>
          ) : null}
          <Button
            type="button"
            className="mt-3 min-h-11"
            data-testid="checkout-discount-add"
            disabled={saving}
            onClick={addDiscount}
          >
            {t("checkout.discountAdd")}
          </Button>
        </Card>
      ) : null}

      <Card data-testid="checkout-money-summary">
        {quoteLoading ? (
          <p
            data-testid="checkout-quote-loading"
            className="mb-2 text-[length:var(--exits-text-xs)] text-muted"
          >
            {t("checkout.quoteLoading")}
          </p>
        ) : null}
        {quoteError ? (
          <p
            data-testid="checkout-quote-error"
            className="mb-2 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]"
          >
            {quoteError}
          </p>
        ) : null}
        <p
          data-testid="checkout-total-amount"
          className="m-0 flex justify-between gap-2 text-[length:var(--exits-text-sm)]"
        >
          <span className="text-muted">{t("checkout.totalAmount")}</span>
          <MoneyDisplay amount={totalAmount} />
        </p>
        <p
          data-testid="checkout-discount-total"
          className="m-0 mt-1 flex justify-between gap-2 text-[length:var(--exits-text-sm)]"
        >
          <span className="text-muted">{t("checkout.discount")}</span>
          <span>
            {discountTotal > 0 ? "−" : null}
            <MoneyDisplay amount={discountTotal} />
          </span>
        </p>
        <p
          data-testid="checkout-amount-to-pay"
          className="mb-0 mt-2 flex justify-between gap-2 text-[length:var(--exits-text-md)] font-semibold"
        >
          <span>{t("checkout.amountToPay")}</span>
          <MoneyDisplay amount={amountToPay} />
        </p>
        {/* Compat for RMAP-11 selectors */}
        <span data-testid="checkout-total" className="sr-only">
          {amountToPay}
        </span>
        {zeroTotal ? (
          <p
            data-testid="checkout-no-payment-required"
            className="mb-0 mt-3 text-[length:var(--exits-text-sm)] font-medium"
          >
            {t("checkout.noPaymentRequired")}
          </p>
        ) : null}
      </Card>

      {!zeroTotal ? (
        <Card>
          <label
            className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]"
            htmlFor="checkout-cash-received"
          >
            {t("checkout.cashReceived")}
            <input
              id="checkout-cash-received"
              data-testid="checkout-cash-received"
              type="number"
              inputMode="decimal"
              min={0}
              step="0.01"
              value={cashReceived}
              disabled={saving}
              className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 tabular-nums"
              onChange={(event) => {
                tenderEditedRef.current = true;
                setCashReceived(event.target.value);
              }}
            />
          </label>
          <p data-testid="checkout-change" className="mb-0 mt-3 text-[length:var(--exits-text-sm)]">
            {t("checkout.change")}:{" "}
            {changeAdvisory === null ? (
              <span className="text-muted">—</span>
            ) : (
              <MoneyDisplay amount={changeAdvisory} />
            )}
          </p>
          <p className="mb-0 mt-1 text-[length:var(--exits-text-xs)] text-muted">
            {t("checkout.changeAdvisory")}
          </p>
        </Card>
      ) : (
        <Card data-testid="checkout-zero-tender">
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("checkout.cashReceived")}: <MoneyDisplay amount={0} />
          </p>
          <p data-testid="checkout-change" className="mb-0 mt-2 text-[length:var(--exits-text-sm)]">
            {t("checkout.change")}: <MoneyDisplay amount={0} />
          </p>
        </Card>
      )}

      <div className="flex flex-wrap gap-2">
        <Button
          type="button"
          data-testid="checkout-confirm"
          className="min-h-11"
          disabled={saving || !tenderOk || cart.lineCount === 0 || !quote || Boolean(quoteError)}
          onClick={() => void onConfirm()}
        >
          {saving ? t("checkout.confirming") : t("checkout.confirmCash")}
        </Button>
        <Button asChild type="button" variant="ghost" className="min-h-11" disabled={saving}>
          <Link to="/sell">{t("checkout.backToCart")}</Link>
        </Button>
      </div>
    </div>
  );
}
