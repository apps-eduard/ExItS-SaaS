import { useEffect, useMemo, useRef, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import {
  canApplyCommercialDiscount,
  canCreateCredit,
  canCreateSale,
  canViewCustomers,
} from "@/access/pos-capabilities";
import { listCustomers, searchCheckoutCustomers } from "@/api/pos/pos-customers-client";
import {
  checkoutSale,
  GCASH_REFERENCE_MAX_LENGTH,
  quoteSale,
  type CheckoutPaymentMethod,
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
type UiPaymentChoice = "Cash" | "GCash" | "Utang";

type AppliedDiscount = CommercialDiscountIntentRequest & { localId: string };

type CheckoutCustomerOption = {
  customerId: string;
  displayName: string;
  mobileNumber?: string | null;
  status: string;
};

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

function toApiPaymentMethod(choice: UiPaymentChoice): CheckoutPaymentMethod {
  if (choice === "GCash") {
    return "ManualGCash";
  }
  return choice;
}

function confirmLabelKey(
  choice: UiPaymentChoice,
): "checkout.confirmCash" | "checkout.confirmGCash" | "checkout.confirmUtang" {
  if (choice === "GCash") {
    return "checkout.confirmGCash";
  }
  if (choice === "Utang") {
    return "checkout.confirmUtang";
  }
  return "checkout.confirmCash";
}

/** Checkout page — Cash / GCash (ManualGCash) / Utang. File kept as CheckoutCashPage for route stability. */
export function CheckoutCashPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const { boundWorkspace, sessionGrant, posDevice } = useWorkspace();
  const cart = useSessionCart();
  const { readiness, currentShift, refresh } = useShiftContext();

  const [paymentChoice, setPaymentChoice] = useState<UiPaymentChoice>("Cash");
  const [cashReceived, setCashReceived] = useState("");
  const [gcashReference, setGcashReference] = useState("");
  const [customerSearch, setCustomerSearch] = useState("");
  const [customers, setCustomers] = useState<CheckoutCustomerOption[]>([]);
  const [customersLoading, setCustomersLoading] = useState(false);
  const [selectedCustomer, setSelectedCustomer] = useState<CheckoutCustomerOption | null>(null);
  const [dueDate, setDueDate] = useState("");
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
  const allowViewCustomers = canViewCustomers(sessionGrant);
  const allowCreateCredit = canCreateCredit(sessionGrant);
  /** Cashier Utang may use narrow checkout-search; management list still requires ViewCustomers. */
  const allowCheckoutCustomerSearch = allowSale;
  const moneyReady = readiness.moneyPostReady === true;
  const deviceReady = isPosDeviceReadyForMoney(posDevice);
  const apiPaymentMethod = toApiPaymentMethod(paymentChoice);

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
  const tenderOk =
    paymentChoice !== "Cash" ||
    zeroTotal ||
    (parsedTender !== null && parsedTender + 1e-9 >= amountToPay);
  const changeAdvisory =
    paymentChoice === "Cash" && !zeroTotal && tenderOk && parsedTender !== null
      ? roundMoney(Math.max(0, parsedTender - amountToPay))
      : paymentChoice === "Cash" && zeroTotal
        ? 0
        : null;

  const gcashRefTrimmed = gcashReference.trim();
  const gcashRefOk =
    paymentChoice !== "GCash" ||
    zeroTotal ||
    (gcashRefTrimmed.length > 0 && gcashRefTrimmed.length <= GCASH_REFERENCE_MAX_LENGTH);

  const utangBlockedZero = paymentChoice === "Utang" && zeroTotal;
  const utangNeedsCustomerLookup =
    paymentChoice === "Utang" && !(allowCheckoutCustomerSearch && allowCreateCredit);
  const utangCustomerOk =
    paymentChoice !== "Utang" ||
    (allowCheckoutCustomerSearch && allowCreateCredit && selectedCustomer != null);
  const utangCreditOk = paymentChoice !== "Utang" || allowCreateCredit;

  useEffect(() => {
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
    if (paymentChoice !== "Cash" || !quote) {
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
  }, [amountToPay, paymentChoice, quote, zeroTotal]);

  useEffect(() => {
    if (!workspaceScope) {
      return;
    }
    const isUtang = paymentChoice === "Utang";
    const isOptionalCashCustomer =
      (paymentChoice === "Cash" || paymentChoice === "GCash") && allowViewCustomers;
    if (!isUtang && !isOptionalCashCustomer) {
      return;
    }
    if (isUtang && !(allowCheckoutCustomerSearch && allowCreateCredit)) {
      return;
    }

    const controller = new AbortController();
    const timer = window.setTimeout(() => {
      const trimmed = customerSearch.trim();
      // Checkout-search requires non-blank search; Owner/Manager full list may load without search.
      if (isUtang && !allowViewCustomers && !trimmed) {
        setCustomers([]);
        setCustomersLoading(false);
        return;
      }

      setCustomersLoading(true);
      const load = allowViewCustomers
        ? listCustomers(
            workspaceScope,
            { status: "Active", search: trimmed || undefined, pageSize: 20 },
            controller.signal,
          ).then((page) =>
            page.items.map((c) => ({
              customerId: c.customerId,
              displayName: c.displayName,
              mobileNumber: c.mobileNumber,
              status: c.status,
            })),
          )
        : searchCheckoutCustomers(
            workspaceScope,
            { search: trimmed, pageSize: 20 },
            controller.signal,
          ).then((page) => page.items);

      void load
        .then((items) => {
          if (!controller.signal.aborted) {
            setCustomers(items);
            setCustomersLoading(false);
          }
        })
        .catch(() => {
          if (!controller.signal.aborted) {
            setCustomers([]);
            setCustomersLoading(false);
          }
        });
    }, 250);

    return () => {
      controller.abort();
      window.clearTimeout(timer);
    };
  }, [
    allowCheckoutCustomerSearch,
    allowCreateCredit,
    allowViewCustomers,
    customerSearch,
    paymentChoice,
    workspaceScope,
  ]);

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
    if (paymentChoice === "Cash" && !zeroTotal && (parsedTender === null || !tenderOk)) {
      setSubmitError(t("checkout.tenderInvalid"));
      return;
    }
    if (paymentChoice === "GCash" && !zeroTotal && !gcashRefOk) {
      setSubmitError(t("checkout.gcashReferenceRequired"));
      return;
    }
    if (utangBlockedZero) {
      setSubmitError(t("checkout.utangZeroBlocked"));
      return;
    }
    if (utangNeedsCustomerLookup) {
      setSubmitError(t("checkout.utangCustomerDenied"));
      return;
    }
    if (paymentChoice === "Utang" && !utangCustomerOk) {
      setSubmitError(t("checkout.utangCustomerRequired"));
      return;
    }

    submittingRef.current = true;
    setSaving(true);
    setSubmitError(null);

    const saleId = attemptSaleIdRef.current;
    const lines = mapCartLinesToCheckoutRequest(cart.lines);

    try {
      await refresh();
      const sale = await checkoutSale(workspaceScope, {
        lines,
        paymentMethod: apiPaymentMethod,
        saleId,
        shiftId: readiness.shiftId,
        ...(paymentChoice === "Cash"
          ? { amountTendered: zeroTotal ? 0 : Number((parsedTender as number).toFixed(2)) }
          : {}),
        ...(paymentChoice === "GCash" && !zeroTotal && gcashRefTrimmed
          ? { gCashReference: gcashRefTrimmed.slice(0, GCASH_REFERENCE_MAX_LENGTH) }
          : {}),
        ...(selectedCustomer && (paymentChoice === "Utang" || allowViewCustomers)
          ? {
              customerId: selectedCustomer.customerId,
              ...(paymentChoice === "Utang" && dueDate.trim() ? { dueDate: dueDate.trim() } : {}),
            }
          : {}),
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

  const confirmDisabled =
    saving ||
    cart.lineCount === 0 ||
    !quote ||
    Boolean(quoteError) ||
    !tenderOk ||
    !gcashRefOk ||
    utangBlockedZero ||
    utangNeedsCustomerLookup ||
    !utangCustomerOk ||
    !utangCreditOk;

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

      <Card data-testid="checkout-payment-method">
        <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
          {t("checkout.paymentMethod")}
        </h2>
        <div
          className="mt-3 flex flex-wrap gap-2"
          role="radiogroup"
          aria-label={t("checkout.paymentMethod")}
        >
          {(
            [
              ["Cash", "checkout.paymentCash"],
              ["GCash", "checkout.paymentGCash"],
              ["Utang", "checkout.paymentUtang"],
            ] as const
          ).map(([value, labelKey]) => (
            <Button
              key={value}
              type="button"
              variant={paymentChoice === value ? "default" : "ghost"}
              className="min-h-11"
              data-testid={`checkout-pay-${value.toLowerCase()}`}
              aria-pressed={paymentChoice === value}
              disabled={saving}
              onClick={() => {
                setPaymentChoice(value);
                setSubmitError(null);
              }}
            >
              {t(labelKey)}
            </Button>
          ))}
        </div>
        {/* Prove Card / provider GCash are not offered */}
        <span data-testid="checkout-no-card" className="sr-only">
          no-card
        </span>
        <span data-testid="checkout-no-provider-gcash" className="sr-only">
          no-provider-gcash
        </span>
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
        <span data-testid="checkout-total" className="sr-only">
          {amountToPay}
        </span>
        {zeroTotal && paymentChoice !== "Utang" ? (
          <p
            data-testid="checkout-no-payment-required"
            className="mb-0 mt-3 text-[length:var(--exits-text-sm)] font-medium"
          >
            {t("checkout.noPaymentRequired")}
          </p>
        ) : null}
        {utangBlockedZero ? (
          <p
            data-testid="checkout-utang-zero-blocked"
            className="mb-0 mt-3 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]"
          >
            {t("checkout.utangZeroBlocked")}
          </p>
        ) : null}
      </Card>

      {paymentChoice === "Cash" ? (
        !zeroTotal ? (
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
            <p
              data-testid="checkout-change"
              className="mb-0 mt-3 text-[length:var(--exits-text-sm)]"
            >
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
            <p
              data-testid="checkout-change"
              className="mb-0 mt-2 text-[length:var(--exits-text-sm)]"
            >
              {t("checkout.change")}: <MoneyDisplay amount={0} />
            </p>
          </Card>
        )
      ) : null}

      {paymentChoice === "GCash" && !zeroTotal ? (
        <Card data-testid="checkout-gcash-panel">
          <label
            className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]"
            htmlFor="checkout-gcash-reference"
          >
            {t("checkout.gcashReference")}
            <input
              id="checkout-gcash-reference"
              data-testid="checkout-gcash-reference"
              type="text"
              maxLength={GCASH_REFERENCE_MAX_LENGTH}
              value={gcashReference}
              disabled={saving}
              className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
              onChange={(event) => setGcashReference(event.target.value)}
            />
          </label>
          <p className="mb-0 mt-2 text-[length:var(--exits-text-xs)] text-muted">
            {t("checkout.gcashReferenceHint")}
          </p>
        </Card>
      ) : null}

      {(paymentChoice === "Cash" || paymentChoice === "GCash") && allowViewCustomers ? (
        <Card data-testid="checkout-optional-customer-panel">
          <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
            {t("checkout.optionalCustomerHint")}
          </p>
          <label
            className="mt-3 flex flex-col gap-1 text-[length:var(--exits-text-sm)]"
            htmlFor="checkout-optional-customer-search"
          >
            {t("checkout.optionalCustomerSearch")}
            <input
              id="checkout-optional-customer-search"
              data-testid="checkout-optional-customer-search"
              type="search"
              value={customerSearch}
              disabled={saving}
              className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
              onChange={(event) => setCustomerSearch(event.target.value)}
            />
          </label>
          {selectedCustomer ? (
            <div
              data-testid="checkout-customer-selected"
              className="mt-3 flex items-center justify-between gap-2 text-[length:var(--exits-text-sm)]"
            >
              <span>
                {t("checkout.utangCustomer")}: <strong>{selectedCustomer.displayName}</strong>
              </span>
              <Button
                type="button"
                variant="ghost"
                className="min-h-9"
                data-testid="checkout-customer-clear"
                disabled={saving}
                onClick={() => setSelectedCustomer(null)}
              >
                {t("checkout.customerClear")}
              </Button>
            </div>
          ) : null}
          {customersLoading ? (
            <p className="mb-0 mt-2 text-[length:var(--exits-text-xs)] text-muted">
              {t("checkout.customerLoading")}
            </p>
          ) : customers.length === 0 ? (
            <p
              data-testid="checkout-customer-empty"
              className="mb-0 mt-2 text-[length:var(--exits-text-sm)] text-muted"
            >
              {t("checkout.customerEmpty")}
            </p>
          ) : (
            <ul className="mb-0 mt-2 list-none space-y-1 p-0" data-testid="checkout-customer-list">
              {customers.map((customer) => (
                <li key={customer.customerId}>
                  <Button
                    type="button"
                    variant={
                      selectedCustomer?.customerId === customer.customerId ? "default" : "ghost"
                    }
                    className="min-h-11 w-full justify-start"
                    data-testid={`checkout-customer-${customer.customerId}`}
                    disabled={saving}
                    onClick={() => setSelectedCustomer(customer)}
                  >
                    {customer.displayName}
                    {customer.mobileNumber ? ` · ${customer.mobileNumber}` : ""}
                  </Button>
                </li>
              ))}
            </ul>
          )}
        </Card>
      ) : null}

      {paymentChoice === "Utang" ? (
        <Card data-testid="checkout-utang-panel">
          <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
            {t("checkout.utangDebtHint")}
          </p>
          {utangNeedsCustomerLookup ? (
            <p
              data-testid="checkout-utang-customer-denied"
              className="mb-0 mt-3 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]"
            >
              {t("checkout.utangCustomerDenied")}
            </p>
          ) : (
            <>
              <label
                className="mt-3 flex flex-col gap-1 text-[length:var(--exits-text-sm)]"
                htmlFor="checkout-customer-search"
              >
                {t("checkout.utangCustomerSearch")}
                <input
                  id="checkout-customer-search"
                  data-testid="checkout-customer-search"
                  type="search"
                  value={customerSearch}
                  disabled={saving}
                  className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
                  onChange={(event) => setCustomerSearch(event.target.value)}
                />
              </label>
              {selectedCustomer ? (
                <div
                  data-testid="checkout-customer-selected"
                  className="mt-3 flex items-center justify-between gap-2 text-[length:var(--exits-text-sm)]"
                >
                  <span>
                    {t("checkout.utangCustomer")}: <strong>{selectedCustomer.displayName}</strong>
                  </span>
                  <Button
                    type="button"
                    variant="ghost"
                    className="min-h-9"
                    data-testid="checkout-customer-clear"
                    disabled={saving}
                    onClick={() => setSelectedCustomer(null)}
                  >
                    {t("checkout.customerClear")}
                  </Button>
                </div>
              ) : null}
              {customersLoading ? (
                <p className="mb-0 mt-2 text-[length:var(--exits-text-xs)] text-muted">
                  {t("checkout.customerLoading")}
                </p>
              ) : customers.length === 0 ? (
                <p
                  data-testid="checkout-customer-empty"
                  className="mb-0 mt-2 text-[length:var(--exits-text-sm)] text-muted"
                >
                  {t("checkout.customerEmpty")}
                </p>
              ) : (
                <ul
                  className="mb-0 mt-2 list-none space-y-1 p-0"
                  data-testid="checkout-customer-list"
                >
                  {customers.map((customer) => (
                    <li key={customer.customerId}>
                      <Button
                        type="button"
                        variant={
                          selectedCustomer?.customerId === customer.customerId ? "default" : "ghost"
                        }
                        className="min-h-11 w-full justify-start"
                        data-testid={`checkout-customer-${customer.customerId}`}
                        disabled={saving}
                        onClick={() => setSelectedCustomer(customer)}
                      >
                        {customer.displayName}
                        {customer.mobileNumber ? ` · ${customer.mobileNumber}` : ""}
                      </Button>
                    </li>
                  ))}
                </ul>
              )}
              <label
                className="mt-3 flex flex-col gap-1 text-[length:var(--exits-text-sm)]"
                htmlFor="checkout-utang-due-date"
              >
                {t("checkout.utangDueDate")}
                <input
                  id="checkout-utang-due-date"
                  data-testid="checkout-utang-due-date"
                  type="date"
                  value={dueDate}
                  disabled={saving}
                  className="min-h-11 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
                  onChange={(event) => setDueDate(event.target.value)}
                />
              </label>
            </>
          )}
        </Card>
      ) : null}

      <div className="flex flex-wrap gap-2">
        <Button
          type="button"
          data-testid="checkout-confirm"
          className="min-h-11"
          disabled={confirmDisabled}
          onClick={() => void onConfirm()}
        >
          {saving ? t("checkout.confirming") : t(confirmLabelKey(paymentChoice))}
        </Button>
        <Button asChild type="button" variant="ghost" className="min-h-11" disabled={saving}>
          <Link to="/sell">{t("checkout.backToCart")}</Link>
        </Button>
      </div>
    </div>
  );
}
