import { useEffect, useMemo, useRef, useState } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { useQueryClient } from "@tanstack/react-query";
import { ArrowLeft, Banknote, Check, Percent, Plus, UserRound, WalletCards } from "lucide-react";
import {
  canApplyCommercialDiscount,
  canCreateCredit,
  canCreateCustomer,
  canCreateSale,
  canOverrideSalePrice,
  canViewCustomers,
} from "@/access/pos-capabilities";
import { listCustomers, searchCheckoutCustomers } from "@/api/pos/pos-customers-client";
import {
  checkoutSale,
  GCASH_REFERENCE_MAX_LENGTH,
  getSale,
  quoteSale,
  type CheckoutPaymentMethod,
  type CommercialDiscountIntentRequest,
  type PosSaleQuoteDto,
  type SalePriceOverrideIntentRequest,
} from "@/api/pos/pos-sales-client";
import { roundMoney } from "@/cart/sell-cart-helpers";
import { lineAmount, useSessionCart } from "@/cart/SessionCartProvider";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { OnlineRequiredCard } from "@/components/exits/OnlineRequiredCard";
import { OnlineRequiredPageState } from "@/components/exits/OnlineRequiredBoot";
import { PageHeader } from "@/components/exits/PageHeader";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { isLikelyNetworkFailure } from "@/connectivity/network-failure";
import { describeCheckoutSaleError } from "@/features/checkout/checkout-sale-errors";
import { invalidatePosStockQueries } from "@/features/catalog/invalidate-pos-stock-queries";
import { CheckoutCollapsibleSection } from "@/features/checkout/CheckoutCollapsibleSection";
import type { CheckoutCustomerOption } from "@/features/checkout/checkout-customer-option";
import {
  CheckoutCustomerDirectory,
  CheckoutCustomerSelectedCard,
} from "@/features/checkout/CheckoutCustomerDirectory";
import { CheckoutPersonalCustomerPicker } from "@/features/checkout/CheckoutPersonalCustomerPicker";
import { checkoutCustomerTitle } from "@/features/customers/format-pos-customer-label";
import { resolveDisplayedPersonalExItsId } from "@/features/customers/customer-link-status";
import { useOrganizationCustomerLinkOverlay } from "@/features/customers/use-organization-customer-link-overlay";
import {
  CHECKOUT_PAYMENT_ICONS,
  CheckoutPaymentMethodCards,
  type CheckoutUiPaymentChoice,
} from "@/features/checkout/CheckoutPaymentMethodCards";
import {
  mapCartLinesToCheckoutRequest,
  mapCartLinesToOfflineCheckoutRequest,
} from "@/features/checkout/map-cart-to-checkout";
import { mapCartPriceOverridesToRequest } from "@/features/checkout/map-cart-price-overrides";
import { useSellOfflineReadiness } from "@/features/sell/use-sell-offline-readiness";
import { useShiftContext } from "@/features/shifts/ShiftContextProvider";
import { useI18n } from "@/i18n/I18nProvider";
import { OfflineCashSaleRejectedError } from "@/offline/cash-sale-offline";
import { useOfflineSync } from "@/offline/OfflineSyncProvider";
import { ONLINE_REQUIRED_CODES } from "@/offline/online-required";
import {
  loadUsablePriceAuthorities,
  type PriceAuthorityLookup,
} from "@/offline/price-authority-cache";
import { organizationWebAllowsOfflineQueueing } from "@/runtime/organization-web-runtime-policy";
import { createSecureMutationId } from "@/lib/secure-mutation-id";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type DiscountScope = CommercialDiscountIntentRequest["scope"];
type DiscountMethod = CommercialDiscountIntentRequest["method"];
type UiPaymentChoice = CheckoutUiPaymentChoice;

type AppliedDiscount = CommercialDiscountIntentRequest & { localId: string };

function allocateSecureId(): string | null {
  const generated = createSecureMutationId();
  return generated.ok ? generated.id : null;
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

/** Checkout page — Cash / GCash (ManualGCash) / Utang. File kept as CheckoutCashPage for route stability. */
export function CheckoutCashPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const location = useLocation();
  const queryClient = useQueryClient();
  const { boundWorkspace, sessionGrant, deviceEnforcementEnabled } = useWorkspace();
  const cart = useSessionCart();
  const { readiness, currentShift, refresh } = useShiftContext();
  const sellReadiness = useSellOfflineReadiness();
  const { refreshCounts } = useOfflineSync();
  const online = sellReadiness.online;
  const customerLinkOverlay = useOrganizationCustomerLinkOverlay(boundWorkspace?.organizationId);

  const [paymentChoice, setPaymentChoice] = useState<UiPaymentChoice>("Cash");
  const [paymentMethodOpen, setPaymentMethodOpen] = useState(false);
  const [cashReceived, setCashReceived] = useState("");
  const [gcashReference, setGcashReference] = useState("");
  const [customerSearch, setCustomerSearch] = useState("");
  const [customers, setCustomers] = useState<CheckoutCustomerOption[]>([]);
  const [customersLoading, setCustomersLoading] = useState(false);
  const [selectedCustomer, setSelectedCustomer] = useState<CheckoutCustomerOption | null>(null);
  const [customerPanelOpen, setCustomerPanelOpen] = useState(false);
  const [dueDate, setDueDate] = useState("");
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [appliedDiscounts, setAppliedDiscounts] = useState<AppliedDiscount[]>([]);
  const [discountsDroppedOffline, setDiscountsDroppedOffline] = useState(false);
  const [discountFormOpen, setDiscountFormOpen] = useState(false);
  const [discountScope, setDiscountScope] = useState<DiscountScope>("Sale");
  const [discountMethod, setDiscountMethod] = useState<DiscountMethod>("Percentage");
  const [discountValue, setDiscountValue] = useState("");
  const [discountReason, setDiscountReason] = useState("");
  const [discountLineNumber, setDiscountLineNumber] = useState(1);
  const [discountFormError, setDiscountFormError] = useState<string | null>(null);
  const [priceAuthorities, setPriceAuthorities] = useState<PriceAuthorityLookup | null>(null);
  const [quote, setQuote] = useState<PosSaleQuoteDto | null>(null);
  const [quoteLoading, setQuoteLoading] = useState(false);
  const [quoteError, setQuoteError] = useState<string | null>(null);
  const attemptSaleIdRef = useRef<string | null>(null);
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
  const allowOverride = canOverrideSalePrice(sessionGrant);
  const allowViewCustomers = canViewCustomers(sessionGrant);
  const allowCreateCredit = canCreateCredit(sessionGrant);
  const allowCreateCustomer = canCreateCustomer(sessionGrant);
  /** Cashier Utang may use narrow checkout-search; management list still requires ViewCustomers. */
  const allowCheckoutCustomerSearch = allowSale;
  const moneyReady = sellReadiness.moneyPostReady === true;
  const deviceReady = sellReadiness.deviceReady;
  const shiftGateReady = sellReadiness.shiftGateReady;
  const shiftId = sellReadiness.shiftId;
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
  const priceOverrideIntents = useMemo(
    () => (allowOverride ? mapCartPriceOverridesToRequest(cart.lines) : []),
    [allowOverride, cart.lines],
  );
  const priceOverrideSignature = useMemo(
    () => JSON.stringify(priceOverrideIntents),
    [priceOverrideIntents],
  );
  const cartSignature = useMemo(
    () =>
      JSON.stringify(
        cart.lines.map((line) => ({
          productId: line.productId,
          quantity: line.quantity,
          productUnitId: line.productUnitId,
          priceOverride: line.priceOverride ?? null,
        })),
      ),
    [cart.lines],
  );

  /** Offline keeps Cash only: no provider reference, no live credit decision, no server money math. */
  const offlineDiscountBlocked = !online && discountIntents.length > 0;
  const offlineOverrideBlocked = !online && priceOverrideIntents.length > 0;
  const offlineBlocked = offlineDiscountBlocked || offlineOverrideBlocked;
  const showDiscountPanel = allowDiscount && online;
  const offlineContext = sellReadiness.offlineContext;
  const offlineDb = offlineContext?.db ?? null;

  /**
   * Offline price leases (RMAP-21 Review Repair 01). While offline the cart is priced by leases the
   * server signed before the network dropped, so the amount the customer pays is the amount the
   * server will record — reconnecting cannot reprice a sale that has already been paid for.
   */
  useEffect(() => {
    if (online || !offlineDb) {
      setPriceAuthorities(null);
      return;
    }
    let cancelled = false;
    void loadUsablePriceAuthorities(offlineDb)
      .then((loaded) => {
        if (!cancelled) {
          setPriceAuthorities(loaded);
        }
      })
      .catch(() => {
        // An unreadable lease cache must block the sale, not price it from the device.
        if (!cancelled) {
          setPriceAuthorities(new Map());
        }
      });
    return () => {
      cancelled = true;
    };
  }, [offlineDb, online]);

  const offlineMapping = useMemo(() => {
    if (online || !priceAuthorities) {
      return null;
    }
    return mapCartLinesToOfflineCheckoutRequest(cart.lines, priceAuthorities);
  }, [cart.lines, online, priceAuthorities]);

  const offlinePricesLoading = !online && priceAuthorities === null;
  const offlinePriceGateBlocked = offlineMapping !== null && !offlineMapping.ok;
  const offlineLeaseTotal = offlineMapping?.ok === true ? offlineMapping.total : null;

  const amountToPay = quote?.total ?? offlineLeaseTotal ?? cart.subtotal;
  const totalAmount = quote?.grossSubtotal ?? offlineLeaseTotal ?? cart.subtotal;
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
    if (!moneyReady || !deviceReady || !shiftGateReady) {
      return;
    }
    if (completedRef.current) {
      return;
    }
    if (cart.lineCount === 0 && !saving) {
      navigate("/sell", { replace: true });
    }
  }, [cart.lineCount, deviceReady, moneyReady, navigate, saving, shiftGateReady]);

  /**
   * Offline has no provider reference and no live credit decision, so Cash is the only choice.
   * A quote captured before the network dropped is discarded — offline money falls back to the
   * cart subtotal, and the server still recomputes every amount when the sale syncs.
   *
   * Discount intents are dropped rather than kept: only the server quote ever applied them, so
   * holding them while charging the undiscounted subtotal would show the cashier a discount the
   * customer is not getting. The drop is announced, never silent.
   */
  useEffect(() => {
    if (online) {
      setDiscountsDroppedOffline(false);
      return;
    }
    setPaymentChoice("Cash");
    setPaymentMethodOpen(false);
    setSelectedCustomer(null);
    setCustomerPanelOpen(false);
    setDiscountFormOpen(false);
    setQuote(null);
    setQuoteError(null);
    setQuoteLoading(false);
    setAppliedDiscounts((current) => {
      if (current.length === 0) {
        return current;
      }
      setDiscountsDroppedOffline(true);
      return [];
    });
  }, [online]);

  useEffect(() => {
    if (!online || !workspaceScope || cart.lineCount === 0 || !moneyReady || !deviceReady) {
      return;
    }

    const controller = new AbortController();
    const timer = window.setTimeout(() => {
      setQuoteLoading(true);
      setQuoteError(null);
      const intents = JSON.parse(discountSignature) as CommercialDiscountIntentRequest[];
      const overrides = JSON.parse(priceOverrideSignature) as SalePriceOverrideIntentRequest[];
      void quoteSale(
        workspaceScope,
        {
          lines: mapCartLinesToCheckoutRequest(cart.lines),
          paymentMethod: "Cash",
          discounts: allowDiscount && intents.length > 0 ? intents : undefined,
          priceOverrides: allowOverride && overrides.length > 0 ? overrides : undefined,
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
    // eslint-disable-next-line react-hooks/exhaustive-deps -- cartSignature/discountSignature/priceOverrideSignature track content without array-identity thrash
  }, [
    allowDiscount,
    allowOverride,
    cartSignature,
    deviceReady,
    discountSignature,
    moneyReady,
    online,
    priceOverrideSignature,
    t,
    workspaceScope,
  ]);

  useEffect(() => {
    if (paymentChoice !== "Cash") {
      return;
    }
    if (zeroTotal) {
      setCashReceived("0");
      lastSeededTotalRef.current = 0;
      tenderEditedRef.current = false;
      return;
    }
    // Leave cash received empty by default — cashier taps Exact or types tender.
    if (lastSeededTotalRef.current === null) {
      lastSeededTotalRef.current = amountToPay;
      return;
    }
    if (Math.abs(lastSeededTotalRef.current - amountToPay) > 1e-9) {
      // Amount changed (e.g. discount) — clear auto-filled/exact only if cashier has not typed.
      if (!tenderEditedRef.current) {
        setCashReceived("");
      }
      lastSeededTotalRef.current = amountToPay;
    }
  }, [amountToPay, paymentChoice, zeroTotal]);

  useEffect(() => {
    if (!online || !workspaceScope) {
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
    if (selectedCustomer) {
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
              linkedPersonalPublicUserId: resolveDisplayedPersonalExItsId({
                linkedPersonalPublicUserId: c.linkedPersonalPublicUserId,
                notes: c.notes,
              }),
              platformBusinessCustomerId: c.platformBusinessCustomerId ?? null,
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
    online,
    paymentChoice,
    selectedCustomer,
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

    const localId = allocateSecureId();
    if (!localId) {
      setDiscountFormError(t("checkout.errorSecureId"));
      return;
    }

    const intent: AppliedDiscount = {
      localId,
      scope: discountScope,
      method: discountMethod,
      value,
      reason,
      ...(discountScope === "Line" ? { lineNumber: discountLineNumber } : {}),
    };
    setAppliedDiscounts((prev) => [...prev, intent]);
    setDiscountValue("");
    setDiscountReason("");
    setDiscountFormOpen(false);
  }

  const paymentMethodLabel =
    paymentChoice === "GCash"
      ? t("checkout.paymentGCashManual")
      : paymentChoice === "Utang"
        ? t("checkout.paymentUtang")
        : t("checkout.paymentCash");

  function removeDiscount(localId: string) {
    setAppliedDiscounts((prev) => prev.filter((item) => item.localId !== localId));
  }

  if (!allowSale) {
    return (
      <div data-testid="checkout-denied" className="flex flex-col gap-3">
        <PageHeader title={t("checkout.title")} description={t("checkout.deniedDetail")} />
        <Button asChild variant="ghost" className="w-fit">
          <Link to="/">{t("notFound.home")}</Link>
        </Button>
      </div>
    );
  }

  if (!moneyReady || !deviceReady || !shiftGateReady || !shiftId) {
    // While PWA device enforcement is paused, never present a register-device block.
    const deviceBlocked = !deviceReady && deviceEnforcementEnabled !== false;
    return (
      <div data-testid="checkout-blocked" className="flex min-w-0 flex-col gap-4">
        <PageHeader title={t("checkout.title")} description={t("checkout.blockedLede")} />
        <Card data-testid="checkout-gate-message">
          <p className="m-0 text-[length:var(--exits-text-sm)]">
            {!online
              ? t("offline.notReady")
              : deviceBlocked
                ? t("checkout.blockedDevice")
                : readiness.status === "blocked_no_shift" || readiness.status === "blocked_closed"
                  ? t("checkout.blockedShift")
                  : t("checkout.blockedGeneric")}
          </p>
          {!online ? (
            <OnlineRequiredCard
              className="mt-3"
              testId="checkout-offline-gate-required"
              code={
                deviceBlocked
                  ? ONLINE_REQUIRED_CODES.DeviceRegister
                  : ONLINE_REQUIRED_CODES.OpenShift
              }
            />
          ) : null}
          <div className="mt-3 flex flex-wrap gap-2">
            {online ? (
              deviceBlocked ? (
                <Button asChild data-testid="checkout-register-device">
                  <Link to="/devices/register">{t("checkout.registerDevice")}</Link>
                </Button>
              ) : (
                <Button asChild data-testid="checkout-open-shift">
                  <Link to="/shifts/open">{t("shift.openTitle")}</Link>
                </Button>
              )
            ) : null}
            <Button asChild variant="ghost">
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
    if (submittingRef.current || saving || !workspaceScope || !shiftId) {
      return;
    }
    if (online && (quoteError || !quote)) {
      setSubmitError(quoteError ?? t("checkout.quoteError"));
      return;
    }
    if (offlineDiscountBlocked) {
      setSubmitError(t("offline.blockedDiscount"));
      return;
    }
    if (offlineOverrideBlocked) {
      setSubmitError(t("offline.blockedPriceOverride"));
      return;
    }
    if (!online && paymentChoice !== "Cash") {
      setSubmitError(t("offline.cashOnlyDetail"));
      return;
    }
    if (!online && (offlinePricesLoading || !offlineMapping?.ok)) {
      setSubmitError(t("offline.priceRefreshRequired"));
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

    if (!attemptSaleIdRef.current) {
      attemptSaleIdRef.current = allocateSecureId();
    }
    const saleId = attemptSaleIdRef.current;
    if (!saleId) {
      setSubmitError(t("checkout.errorSecureId"));
      submittingRef.current = false;
      setSaving(false);
      return;
    }
    const lines = mapCartLinesToCheckoutRequest(cart.lines);

    if (!online) {
      // Organization Web/PWA is online-only — never enqueue or report offline success.
      if (!organizationWebAllowsOfflineQueueing()) {
        setSubmitError(t("connectivity.actionRequiresInternet"));
        submittingRef.current = false;
        setSaving(false);
        return;
      }
      try {
        if (!offlineContext || offlineMapping?.ok !== true) {
          setSubmitError(t("offline.priceRefreshRequired"));
          return;
        }
        const { enqueueOfflineCashSale } = await import("@/offline/cash-sale-offline");
        await enqueueOfflineCashSale({
          db: offlineContext.db,
          scopeBinding: offlineContext.scopeBinding,
          userId: offlineContext.userId,
          organizationId: offlineContext.organizationId,
          branchId: offlineContext.branchId,
          installationDeviceId: offlineContext.installationDeviceId,
          posDeviceId: offlineContext.posDeviceId,
          saleId,
          shiftId,
          lines: offlineMapping.lines,
          amountTendered: zeroTotal ? 0 : Number((parsedTender as number).toFixed(2)),
        });
        await refreshCounts();
        completedRef.current = true;
        cart.clear();
        attemptSaleIdRef.current = allocateSecureId();
        navigate(`/sell/offline-queued/${saleId}`, { replace: true });
      } catch (error) {
        const leaseRejected =
          error instanceof OfflineCashSaleRejectedError &&
          error.code.startsWith("offline.sale.price_authority");
        setSubmitError(t(leaseRejected ? "offline.priceRefreshRequired" : "offline.enqueueFailed"));
      } finally {
        submittingRef.current = false;
        setSaving(false);
      }
      return;
    }

    try {
      await refresh();
      const sale = await checkoutSale(workspaceScope, {
        lines,
        paymentMethod: apiPaymentMethod,
        saleId,
        shiftId,
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
        priceOverrides:
          allowOverride && priceOverrideIntents.length > 0 ? priceOverrideIntents : undefined,
      });
      completedRef.current = true;
      cart.clear();
      attemptSaleIdRef.current = allocateSecureId();
      await invalidatePosStockQueries(queryClient);
      navigate(`/sell/sales/${sale.saleId}/summary`, { replace: true });
    } catch (error) {
      if (isLikelyNetworkFailure(error) && workspaceScope) {
        // Ambiguous money outcome: request may have committed before the response was lost.
        // Do not invite an unsafe duplicate retry — look up by saleId (idempotency key).
        setSubmitError(t("checkout.confirmingTransaction"));
        try {
          const confirmed = await getSale(workspaceScope, saleId);
          completedRef.current = true;
          cart.clear();
          attemptSaleIdRef.current = allocateSecureId();
          await invalidatePosStockQueries(queryClient);
          navigate(`/sell/sales/${confirmed.saleId}/summary`, { replace: true });
          return;
        } catch (lookupError) {
          if (isLikelyNetworkFailure(lookupError)) {
            // Keep the same saleId so a later retry reuses idempotency headers.
            setSubmitError(t("checkout.transactionStatusUnknown"));
            return;
          }
          // Lookup reached the server and the sale is absent / rejected — safe to describe failure.
          setSubmitError(describeCheckoutSaleError(lookupError, t));
          return;
        }
      }
      setSubmitError(describeCheckoutSaleError(error, t));
    } finally {
      submittingRef.current = false;
      setSaving(false);
    }
  }

  const confirmDisabled =
    saving ||
    cart.lineCount === 0 ||
    offlineBlocked ||
    !online ||
    (online && (!quote || Boolean(quoteError))) ||
    offlinePricesLoading ||
    offlinePriceGateBlocked ||
    !tenderOk ||
    !gcashRefOk ||
    utangBlockedZero ||
    utangNeedsCustomerLookup ||
    !utangCustomerOk ||
    !utangCreditOk;

  return (
    <div data-testid="checkout-cash-page" className="checkout-cash-page">
      <div className="checkout-cash-page__scroll">
      <PageHeader
        title={t("checkout.title")}
        description={t("checkout.cashLede")}
        subtitle={
          currentShift
            ? t("checkout.shiftHint")
                .replace("{shift}", currentShift.shiftNumber)
                .replace("{register}", currentShift.registerCode ?? "—")
            : undefined
        }
      />

      {!online ? (
        <OnlineRequiredPageState
          title={t("checkout.title")}
          detail={t("connectivity.actionRequiresInternet")}
          testId="checkout-online-required"
        />
      ) : null}

      {offlinePriceGateBlocked && organizationWebAllowsOfflineQueueing() ? (
        <Card data-testid="checkout-offline-price-authority-required">
          <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
            {t("offline.priceRefreshRequiredTitle")}
          </p>
          <p className="mb-0 mt-1 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]">
            {t("offline.priceRefreshRequired")}
          </p>
        </Card>
      ) : null}

      {!online && (discountsDroppedOffline || offlineDiscountBlocked) ? (
        <OnlineRequiredCard
          testId="checkout-offline-discount-blocked"
          code={ONLINE_REQUIRED_CODES.CommercialDiscount}
        />
      ) : null}

      {offlineOverrideBlocked ? (
        <OnlineRequiredCard
          testId="checkout-offline-price-override-blocked"
          code={ONLINE_REQUIRED_CODES.PriceOverride}
        />
      ) : null}

      {submitError ? (
        <Card data-testid="checkout-error">
          <p className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]">
            {submitError}
          </p>
        </Card>
      ) : null}

      <Card data-testid="checkout-money-summary" className="checkout-sale-preview">
        <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
          {t("checkout.orderPreview")}
        </h2>
        <ul className="checkout-sale-preview__lines">
          {cart.lines.map((line, index) => (
            <li
              key={line.lineKey}
              className="flex items-start justify-between gap-2 text-[length:var(--exits-text-sm)]"
              data-testid={`checkout-line-${line.lineKey}`}
            >
              <span className="min-w-0">
                <span className="truncate">
                  {index + 1}. {line.name} × {line.quantity} {line.unitLabel}
                </span>
                {line.priceOverride ? (
                  <span
                    className="mt-0.5 block text-[length:var(--exits-text-xs)] text-muted"
                    data-testid={`checkout-line-price-changed-${line.lineKey}`}
                  >
                    {t("sell.priceChanged")} · {t("sell.regularPrice")}: ₱
                    {line.unitPrice.toFixed(2)}
                  </span>
                ) : null}
              </span>
              <MoneyDisplay amount={lineAmount(line)} />
            </li>
          ))}
        </ul>
        {priceOverrideIntents.length > 0 ? (
          <p
            data-testid="checkout-price-override-note"
            className="mb-0 mt-3 text-[length:var(--exits-text-xs)] text-muted"
          >
            {t("checkout.priceOverrideNote")}
          </p>
        ) : null}

        <div className="checkout-sale-preview__totals">
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
            className="m-0 flex justify-between gap-2 text-[length:var(--exits-text-sm)]"
          >
            <span className="text-muted">{t("checkout.discount")}</span>
            <span>
              {discountTotal > 0 ? "−" : null}
              <MoneyDisplay amount={discountTotal} />
            </span>
          </p>
          <p
            data-testid="checkout-amount-to-pay"
            className="mb-0 flex justify-between gap-2 text-[length:var(--exits-text-md)] font-semibold"
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
        </div>
      </Card>

      <Card data-testid="checkout-payment-method" className="checkout-section-card">
        <CheckoutCollapsibleSection
          testId="checkout-payment-collapse"
          title={t("checkout.paymentMethod")}
          expandLabel={t("checkout.paymentMethodChoose")}
          summary={paymentMethodLabel}
          open={paymentMethodOpen}
          onOpenChange={setPaymentMethodOpen}
          icon={WalletCards}
          disabled={saving}
        >
          <CheckoutPaymentMethodCards
            value={paymentChoice}
            groupLabel={t("checkout.paymentMethod")}
            onChange={(next) => {
              setPaymentChoice(next);
              setSubmitError(null);
              setPaymentMethodOpen(false);
            }}
            options={[
              {
                value: "Cash",
                label: t("checkout.paymentCash"),
                Icon: CHECKOUT_PAYMENT_ICONS.Cash,
                testId: "checkout-pay-cash",
                disabled: saving,
              },
              {
                value: "GCash",
                label: t("checkout.paymentGCashManual"),
                Icon: CHECKOUT_PAYMENT_ICONS.GCash,
                testId: "checkout-pay-gcash",
                disabled: saving || !online,
              },
              {
                value: "Utang",
                label: t("checkout.paymentUtang"),
                Icon: CHECKOUT_PAYMENT_ICONS.Utang,
                testId: "checkout-pay-utang",
                disabled: saving || !online,
              },
            ]}
          />
          {!online ? (
            <p
              data-testid="checkout-offline-method-hint"
              className="mb-0 mt-2 text-[length:var(--exits-text-xs)] text-muted"
            >
              {t("offline.requiredGCash")} {t("offline.requiredUtang")}
            </p>
          ) : null}
        </CheckoutCollapsibleSection>
        {/* Prove Card / Debit / provider GCash are not offered */}
        <span data-testid="checkout-no-card" className="sr-only">
          no-card
        </span>
        <span data-testid="checkout-no-debit" className="sr-only">
          no-debit
        </span>
        <span data-testid="checkout-no-provider-gcash" className="sr-only">
          no-provider-gcash
        </span>
      </Card>

      {showDiscountPanel ? (
        <Card data-testid="checkout-discount-panel" className="checkout-section-card">
          <CheckoutCollapsibleSection
            testId="checkout-discount-collapse"
            title={t("checkout.discountSection")}
            expandLabel={t("checkout.discountAdd")}
            summary={
              appliedDiscounts.length > 0
                ? t("checkout.discountAppliedCount").replace(
                    "{count}",
                    String(appliedDiscounts.length),
                  )
                : undefined
            }
            open={discountFormOpen}
            onOpenChange={(next) => {
              setDiscountFormOpen(next);
              if (!next) {
                setDiscountFormError(null);
              }
            }}
            icon={Percent}
            disabled={saving}
          >
            <p className="mb-0 text-[length:var(--exits-text-xs)] text-muted">
              {t("checkout.discountLede")}
            </p>

            <div className="mt-3 grid gap-2 sm:grid-cols-2">
              <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                {t("checkout.discountScope")}
                <select
                  data-testid="checkout-discount-scope"
                  className="exits-select"
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
                  className="exits-select"
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
                    className="exits-select"
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
                  className="rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 tabular-nums"
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
                  className="rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 tabular-nums"
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
            <div className="mt-3 flex flex-wrap gap-2">
              <Button
                type="button"
                className="checkout-discount-apply flex-1 sm:flex-none"
                data-testid="checkout-discount-add"
                disabled={saving}
                onClick={addDiscount}
              >
                <Plus className="size-4 shrink-0" aria-hidden />
                {t("checkout.discountApply")}
              </Button>
              <Button
                type="button"
                variant="ghost"
                data-testid="checkout-discount-cancel"
                disabled={saving}
                onClick={() => {
                  setDiscountFormOpen(false);
                  setDiscountFormError(null);
                }}
              >
                {t("checkout.discountCancel")}
              </Button>
            </div>
          </CheckoutCollapsibleSection>

          {appliedDiscounts.length === 0 && !discountFormOpen ? (
            <p
              data-testid="checkout-discount-empty"
              className="mb-0 mt-1.5 text-[length:var(--exits-text-sm)] text-muted"
            >
              {t("checkout.discountEmpty")}
            </p>
          ) : null}

          {appliedDiscounts.length > 0 ? (
            <ul className="mb-0 mt-3 list-none space-y-2 p-0" data-testid="checkout-discount-list">
              {appliedDiscounts.map((item) => (
                <li
                  key={item.localId}
                  className="checkout-discount-chip flex items-start justify-between gap-2 text-[length:var(--exits-text-sm)]"
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
          ) : null}
        </Card>
      ) : null}

      {(paymentChoice === "Cash" || paymentChoice === "GCash") && allowViewCustomers && online ? (
        <Card data-testid="checkout-optional-customer-panel" className="checkout-section-card">
          <CheckoutCollapsibleSection
            testId="checkout-optional-customer-collapse"
            title={t("checkout.customerSection")}
            expandLabel={t("checkout.addCustomer")}
            summary={
              selectedCustomer
                ? checkoutCustomerTitle(selectedCustomer, t("checkout.walkInCustomer"))
                : undefined
            }
            open={customerPanelOpen}
            onOpenChange={setCustomerPanelOpen}
            icon={UserRound}
            disabled={saving}
            trailing={
              selectedCustomer && !customerPanelOpen ? (
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
              ) : null
            }
          >
            <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
              {t("checkout.optionalCustomerHint")}
            </p>
            {selectedCustomer ? (
              <div className="mt-3">
                <CheckoutCustomerSelectedCard
                  customer={selectedCustomer}
                  overlay={customerLinkOverlay}
                  disabled={saving}
                  onClear={() => setSelectedCustomer(null)}
                />
              </div>
            ) : null}
            {workspaceScope && !selectedCustomer ? (
              <CheckoutPersonalCustomerPicker
                workspace={workspaceScope}
                disabled={saving}
                canLinkCustomer={allowCreateCustomer}
                returnTo={location.pathname}
                onCustomerSelected={(customer) => {
                  setSelectedCustomer(customer);
                  setCustomerPanelOpen(false);
                }}
              />
            ) : null}
            {!selectedCustomer ? (
              <CheckoutCustomerDirectory
                searchId="checkout-optional-customer-search"
                searchTestId="checkout-optional-customer-search"
                searchLabel={t("checkout.optionalCustomerSearch")}
                searchValue={customerSearch}
                onSearchChange={setCustomerSearch}
                customers={customers}
                customersLoading={customersLoading}
                selectedCustomer={selectedCustomer}
                overlay={customerLinkOverlay}
                disabled={saving}
                onSelect={(customer) => {
                  setSelectedCustomer(customer);
                  setCustomerPanelOpen(false);
                }}
              />
            ) : null}
          </CheckoutCollapsibleSection>
        </Card>
      ) : null}

      {paymentChoice === "Utang" ? (
        <Card
          data-testid="checkout-utang-panel"
          className="checkout-section-card checkout-detail-panel exits-animate-panel"
        >
          <h2 className="m-0 inline-flex flex-wrap items-baseline gap-1.5 text-[length:var(--exits-text-md)] font-semibold">
            {t("checkout.customerSection")}
            <span className="text-[length:var(--exits-text-xs)] font-semibold text-[var(--exits-danger)]">
              {t("checkout.fieldRequired")}
            </span>
          </h2>
          <p className="checkout-utang-panel__lede text-[length:var(--exits-text-xs)] text-muted">
            {t("checkout.utangDebtHint")} {t("checkout.utangCustomerRequired")}
          </p>
          {utangNeedsCustomerLookup ? (
            <p
              data-testid="checkout-utang-customer-denied"
              className="mb-0 mt-2 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]"
            >
              {t("checkout.utangCustomerDenied")}
            </p>
          ) : (
            <>
              {selectedCustomer ? (
                <div className="mt-2">
                  <CheckoutCustomerSelectedCard
                    customer={selectedCustomer}
                    overlay={customerLinkOverlay}
                    disabled={saving}
                    onClear={() => setSelectedCustomer(null)}
                  />
                </div>
              ) : null}
              {workspaceScope && !selectedCustomer ? (
                <CheckoutPersonalCustomerPicker
                  workspace={workspaceScope}
                  disabled={saving}
                  canLinkCustomer={allowCreateCustomer}
                  returnTo={location.pathname}
                  onCustomerSelected={setSelectedCustomer}
                />
              ) : null}
              {!selectedCustomer ? (
                <CheckoutCustomerDirectory
                  searchId="checkout-customer-search"
                  searchTestId="checkout-customer-search"
                  searchLabel={t("checkout.utangCustomerSearch")}
                  searchValue={customerSearch}
                  onSearchChange={setCustomerSearch}
                  customers={customers}
                  customersLoading={customersLoading}
                  selectedCustomer={selectedCustomer}
                  overlay={customerLinkOverlay}
                  disabled={saving}
                  onSelect={setSelectedCustomer}
                />
              ) : null}
              <label
                className="mt-2 flex flex-col gap-1 text-[length:var(--exits-text-sm)]"
                htmlFor="checkout-utang-due-date"
              >
                {t("checkout.utangDueDate")}
                <input
                  id="checkout-utang-due-date"
                  data-testid="checkout-utang-due-date"
                  type="date"
                  value={dueDate}
                  disabled={saving}
                  className="rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
                  onChange={(event) => setDueDate(event.target.value)}
                />
              </label>
            </>
          )}
        </Card>
      ) : null}

      </div>

      <div className="checkout-tender-dock" data-testid="checkout-tender-dock">
      {paymentChoice === "Cash" ? (
        !zeroTotal ? (
          <Card className="checkout-detail-panel exits-animate-panel" key="checkout-cash-tender">
            <label
              className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]"
              htmlFor="checkout-cash-received"
            >
              <span className="checkout-cash-received-label">
                <span className="checkout-collapsible__icon" aria-hidden>
                  <Banknote className="size-4" strokeWidth={2} />
                </span>
                {t("checkout.cashReceived")}
              </span>
              <span className="checkout-cash-received-row">
                <input
                  id="checkout-cash-received"
                  data-testid="checkout-cash-received"
                  type="number"
                  inputMode="decimal"
                  min={0}
                  step="0.01"
                  value={cashReceived}
                  disabled={saving}
                  className="checkout-cash-received-row__input rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 tabular-nums"
                  onChange={(event) => {
                    tenderEditedRef.current = true;
                    setCashReceived(event.target.value);
                  }}
                />
                <Button
                  type="button"
                  variant="outline"
                  className="checkout-cash-received-row__exact shrink-0"
                  data-testid="checkout-cash-exact"
                  disabled={saving}
                  onClick={() => {
                    tenderEditedRef.current = true;
                    setCashReceived(amountToPay.toFixed(2));
                  }}
                >
                  {t("checkout.cashExact")}
                </Button>
              </span>
            </label>
            <p
              data-testid="checkout-change"
              className="mb-0 mt-1.5 text-[length:var(--exits-text-sm)]"
            >
              {t("checkout.change")}:{" "}
              {changeAdvisory === null ? (
                <span className="text-muted">—</span>
              ) : (
                <MoneyDisplay amount={changeAdvisory} />
              )}
            </p>
            <p className="mb-0 mt-0.5 text-[length:var(--exits-text-xs)] text-muted">
              {t("checkout.changeAdvisory")}
            </p>
          </Card>
        ) : (
          <Card
            data-testid="checkout-zero-tender"
            className="checkout-detail-panel exits-animate-panel"
            key="checkout-zero-tender"
          >
            <p className="checkout-cash-received-label m-0 text-[length:var(--exits-text-sm)] text-muted">
              <span className="checkout-collapsible__icon" aria-hidden>
                <Banknote className="size-4" strokeWidth={2} />
              </span>
              <span>
                {t("checkout.cashReceived")}: <MoneyDisplay amount={0} />
              </span>
            </p>
            <p
              data-testid="checkout-change"
              className="mb-0 mt-1.5 text-[length:var(--exits-text-sm)]"
            >
              {t("checkout.change")}: <MoneyDisplay amount={0} />
            </p>
          </Card>
        )
      ) : null}

      {paymentChoice === "GCash" && !zeroTotal ? (
        <Card
          data-testid="checkout-gcash-panel"
          className="checkout-detail-panel checkout-gcash-under-method exits-animate-panel"
        >
          <label
            className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]"
            htmlFor="checkout-gcash-reference"
          >
            <span className="inline-flex flex-wrap items-baseline gap-1">
              {t("checkout.gcashReference")}
              <span className="text-[length:var(--exits-text-xs)] font-semibold text-[var(--exits-danger)]">
                {t("checkout.fieldRequired")}
              </span>
            </span>
            <input
              id="checkout-gcash-reference"
              data-testid="checkout-gcash-reference"
              type="text"
              required
              aria-required="true"
              maxLength={GCASH_REFERENCE_MAX_LENGTH}
              value={gcashReference}
              disabled={saving}
              className="rounded-[var(--exits-radius-md)] border border-border bg-surface px-3"
              onChange={(event) => setGcashReference(event.target.value)}
            />
          </label>
          <p className="mb-0 mt-1.5 text-[length:var(--exits-text-xs)] text-muted">
            {t("checkout.gcashReferenceHint")}
          </p>
        </Card>
      ) : null}

      <div className="checkout-actions">
        <Button
          type="button"
          data-testid="checkout-confirm"
          className="checkout-actions__btn h-auto"
          disabled={confirmDisabled}
          onClick={() => void onConfirm()}
        >
          <Check className="size-4 shrink-0" aria-hidden />
          {saving ? t("checkout.confirming") : t("checkout.confirmSale")}
        </Button>
        <Button
          asChild
          type="button"
          variant="outline"
          className="checkout-actions__btn h-auto"
          disabled={saving}
        >
          <Link to="/sell">
            <ArrowLeft className="size-4 shrink-0" aria-hidden />
            {t("checkout.backToCart")}
          </Link>
        </Button>
      </div>
      </div>
    </div>
  );
}
