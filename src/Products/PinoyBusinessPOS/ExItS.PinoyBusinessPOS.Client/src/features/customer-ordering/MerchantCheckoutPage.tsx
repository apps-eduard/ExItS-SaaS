import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { Package, Smartphone, Truck, Wallet } from "lucide-react";
import { ensurePersonalBuyerPosToken } from "@/api/platform/personal-buyer-token";
import { PosApiError } from "@/api/pos/pos-http";
import { describePosApiError } from "@/access/pos-commercial-errors";
import {
  getCustomerStorefront,
  isInsufficientStockError,
  placeCustomerOrder,
  quoteCustomerDelivery,
  sellerWorkspace,
} from "@/api/pos/pos-customer-orders-client";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { usePersonalMerchantCart } from "@/features/customer-ordering/PersonalMerchantCartProvider";
import {
  CheckoutPlaceButton,
  SegmentedOption,
} from "@/features/customer-ordering/personal-commerce-ui";
import {
  eligibleBranches,
  FulfillmentDelivery,
  FulfillmentPickup,
  PAYMENT_METHOD_CODES,
  resolveFulfillmentSelection,
} from "@/features/customer-ordering/personal-merchant-cart";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { useSession } from "@/session/SessionProvider";
import { personalPageBackNav } from "@/navigation/page-back-nav";

function money(n: number): string {
  return `₱${n.toFixed(2)}`;
}

function newClientOrderId(): string {
  return crypto.randomUUID();
}

function paymentLabel(code: string, t: (key: MessageKey) => string): string {
  if (code === "Cash") return t("orders.paymentCash");
  if (code === "ManualGCash") return t("orders.paymentGCash");
  return t("orders.paymentUtang");
}

export function MerchantCheckoutPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
  const online = useBrowserOnline();
  const { session } = useSession();
  const { organizationId = "" } = useParams();
  const { cart, merchandiseSubtotal, clearAll } = usePersonalMerchantCart();

  const [fulfillmentType, setFulfillmentType] = useState(FulfillmentPickup);
  const [branchId, setBranchId] = useState<string | null>(null);
  const [paymentMethod, setPaymentMethod] = useState<string>(PAYMENT_METHOD_CODES[0]);
  const [recipientName, setRecipientName] = useState(session?.displayName ?? "");
  const [recipientPhone, setRecipientPhone] = useState("");
  const [addressLine1, setAddressLine1] = useState("");
  const [addressLine2, setAddressLine2] = useState("");
  const [city, setCity] = useState("");
  const [deliveryNotes, setDeliveryNotes] = useState("");
  const [latitude, setLatitude] = useState("");
  const [longitude, setLongitude] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [stockConflict, setStockConflict] = useState(false);
  const [tokenReady, setTokenReady] = useState(false);

  useEffect(() => {
    void ensurePersonalBuyerPosToken().then((r) => setTokenReady(r.ok));
  }, []);

  const workspace = useMemo(
    () => (organizationId ? sellerWorkspace(organizationId, branchId) : null),
    [organizationId, branchId],
  );

  const storefrontQuery = useQuery({
    queryKey: ["storefront", "checkout", organizationId],
    enabled: Boolean(workspace) && tokenReady && online,
    queryFn: ({ signal }) =>
      getCustomerStorefront(workspace!, organizationId, { pageSize: 1 }, signal),
  });

  const selection = useMemo(() => {
    if (!storefrontQuery.data) {
      return null;
    }
    return resolveFulfillmentSelection(
      storefrontQuery.data.branches,
      storefrontQuery.data.canCustomerDelivery,
      fulfillmentType,
      branchId,
    );
  }, [storefrontQuery.data, fulfillmentType, branchId]);

  useEffect(() => {
    if (selection?.branchId && selection.branchId !== branchId) {
      setBranchId(selection.branchId);
    }
    if (selection?.fulfillmentType && selection.fulfillmentType !== fulfillmentType) {
      setFulfillmentType(selection.fulfillmentType);
    }
  }, [selection, branchId, fulfillmentType]);

  const latNum = Number.parseFloat(latitude);
  const lngNum = Number.parseFloat(longitude);
  const coordsValid =
    Number.isFinite(latNum) &&
    Number.isFinite(lngNum) &&
    latNum >= -90 &&
    latNum <= 90 &&
    lngNum >= -180 &&
    lngNum <= 180;

  const quoteQuery = useQuery({
    queryKey: [
      "delivery-quote",
      organizationId,
      branchId,
      merchandiseSubtotal,
      latitude,
      longitude,
    ],
    enabled:
      Boolean(workspace) &&
      tokenReady &&
      selection?.fulfillmentType === FulfillmentDelivery &&
      Boolean(branchId) &&
      coordsValid &&
      cart.lines.length > 0,
    queryFn: () =>
      quoteCustomerDelivery(workspace!, organizationId, {
        fulfillmentBranchId: branchId!,
        merchandiseSubtotal,
        destinationLatitude: latNum,
        destinationLongitude: lngNum,
      }),
  });

  const branches = storefrontQuery.data
    ? eligibleBranches(
        storefrontQuery.data.branches,
        storefrontQuery.data.canCustomerDelivery,
        selection?.fulfillmentType ?? fulfillmentType,
      )
    : [];

  async function refreshStorefrontAfterStockConflict() {
    setStockConflict(true);
    await storefrontQuery.refetch();
  }

  async function placeOrder() {
    if (!workspace || !selection?.branchId || !selection.canPlace || cart.lines.length === 0) {
      return;
    }
    if (!session?.userId) {
      setError(t("orders.missingBuyerIdentity"));
      return;
    }

    const isDelivery = selection.fulfillmentType === FulfillmentDelivery;
    if (isDelivery) {
      if (!recipientName.trim() || !addressLine1.trim() || !coordsValid) {
        setError(t("orders.deliveryFieldsRequired"));
        return;
      }
      if (!quoteQuery.data?.available) {
        setError(quoteQuery.data?.unavailableReason ?? t("orders.deliveryUnavailable"));
        return;
      }
    }

    setBusy(true);
    setError(null);
    setStockConflict(false);
    try {
      const order = await placeCustomerOrder(workspace, organizationId, {
        fulfillmentType: selection.fulfillmentType,
        fulfillmentBranchId: selection.branchId,
        customerPartyType: "Personal",
        customerDisplayName: session.displayName ?? session.email ?? "Customer",
        customerPlatformUserId: session.userId,
        lines: cart.lines.map((l) => ({
          productId: l.productId,
          quantity: l.quantity,
          discount: 0,
        })),
        delivery: isDelivery
          ? {
              recipientName: recipientName.trim(),
              recipientPhone: recipientPhone.trim() || null,
              addressLine1: addressLine1.trim(),
              addressLine2: addressLine2.trim() || null,
              city: city.trim() || null,
              deliveryNotes: deliveryNotes.trim() || null,
              destinationLatitude: latNum,
              destinationLongitude: lngNum,
            }
          : null,
        clientOrderId: newClientOrderId(),
        paymentMethod,
      });
      clearAll();
      navigate(`/personal/orders/${order.orderId}`);
    } catch (err) {
      if (isInsufficientStockError(err)) {
        await refreshStorefrontAfterStockConflict();
        setError(t("orders.stockConflict"));
      } else if (err instanceof PosApiError) {
        setError(describePosApiError(err, t, "error.detail"));
      } else {
        setError(err instanceof Error ? err.message : t("orders.error"));
      }
    } finally {
      setBusy(false);
    }
  }

  const pageShell =
    "personal-page personal-commerce-page merchant-checkout-page exits-page flex min-w-0 flex-col gap-4";

  if (!tokenReady || (online && storefrontQuery.isLoading)) {
    return <LoadingState label={t("loading.label")} />;
  }

  if (!online) {
    return (
      <div className={pageShell} data-testid="merchant-checkout-offline">
        <PageHeader
          title={t("orders.checkoutTitle")}
          backTo={
            organizationId
              ? `/personal/linked-merchants/${organizationId}/shop`
              : personalPageBackNav.merchants.to
          }
          backLabel={
            organizationId ? t("orders.backToShop") : t(personalPageBackNav.merchants.labelKey)
          }
          backTestId="page-header-back-checkout"
        />
        <EmptyState
          title={t("offline.internetRequiredTitle")}
          detail={t("offline.internetRequiredDetail")}
        />
      </div>
    );
  }

  if (cart.lines.length === 0 || cart.sellerOrganizationId !== organizationId) {
    return (
      <div className={pageShell} data-testid="checkout-empty">
        <PageHeader
          title={t("orders.checkoutTitle")}
          backTo={`/personal/linked-merchants/${organizationId}/shop`}
          backLabel={t("orders.backToShop")}
          backTestId="page-header-back-checkout"
        />
        <EmptyState title={t("orders.cartEmptyTitle")} detail={t("orders.cartEmptyDetail")} />
      </div>
    );
  }

  if (storefrontQuery.isError || !storefrontQuery.data || !selection) {
    return (
      <div className={pageShell}>
        <PageHeader
          title={t("orders.checkoutTitle")}
          backTo={
            organizationId
              ? `/personal/linked-merchants/${organizationId}/shop`
              : personalPageBackNav.merchants.to
          }
          backLabel={
            organizationId ? t("orders.backToShop") : t(personalPageBackNav.merchants.labelKey)
          }
          backTestId="page-header-back-checkout"
        />
        <ErrorState
          title={t("orders.error")}
          detail={
            storefrontQuery.error instanceof Error
              ? storefrontQuery.error.message
              : t("error.detail")
          }
        />
        <Button
          type="button"
          className="min-h-11 w-fit"
          onClick={() => void storefrontQuery.refetch()}
        >
          {t("orders.retry")}
        </Button>
      </div>
    );
  }

  const deliveryFee =
    selection.fulfillmentType === FulfillmentDelivery && quoteQuery.data?.available
      ? quoteQuery.data.deliveryFee
      : 0;
  const total = Math.round((merchandiseSubtotal + deliveryFee) * 100) / 100;

  return (
    <div className={pageShell} data-testid="merchant-checkout-page">
      <PageHeader
        title={t("orders.checkoutTitle")}
        description={storefrontQuery.data.organizationDisplayName}
        backTo={`/personal/linked-merchants/${organizationId}/shop`}
        backLabel={t("orders.backToShop")}
        backTestId="page-header-back-checkout"
      />

      {error ? (
        <div className="flex flex-col gap-2">
          <ErrorState
            title={stockConflict ? t("orders.stockConflictTitle") : t("orders.error")}
            detail={error}
          />
          {stockConflict ? (
            <Button
              type="button"
              variant="ghost"
              className="min-h-11 w-fit"
              data-testid="stock-conflict-refresh"
              onClick={() => void storefrontQuery.refetch()}
            >
              {t("orders.refreshStorefront")}
            </Button>
          ) : null}
        </div>
      ) : null}

      <div className="pc-checkout-stack">
        <section className="pc-checkout-section" data-testid="checkout-lines">
          <h2 className="pc-checkout-section__title">{t("orders.viewDetails")}</h2>
          {cart.lines.map((line) => (
            <div key={line.productId} className="pc-checkout-line">
              <span className="pc-checkout-line__name">
                {line.name}
                <span className="pc-checkout-line__qty"> × {line.quantity}</span>
              </span>
              <span className="pc-checkout-line__amount">
                {money(Math.round(line.unitPrice * line.quantity * 100) / 100)}
              </span>
            </div>
          ))}
        </section>

        <section className="pc-checkout-section">
          <h2 className="pc-checkout-section__title">{t("orders.fulfillmentType")}</h2>
          {selection.showFulfillmentToggle ? (
            <div className="pc-segmented" role="radiogroup" aria-label={t("orders.fulfillmentType")}>
              <SegmentedOption
                pressed={selection.fulfillmentType === FulfillmentPickup}
                testId="fulfillment-pickup"
                onClick={() => setFulfillmentType(FulfillmentPickup)}
              >
                <Package className="size-4 shrink-0" aria-hidden />
                {t("orders.pickup")}
              </SegmentedOption>
              <SegmentedOption
                pressed={selection.fulfillmentType === FulfillmentDelivery}
                testId="fulfillment-delivery"
                onClick={() => setFulfillmentType(FulfillmentDelivery)}
              >
                <Truck className="size-4 shrink-0" aria-hidden />
                {t("orders.delivery")}
              </SegmentedOption>
            </div>
          ) : (
            <p className="m-0 text-[length:var(--exits-text-sm)]">
              {selection.fulfillmentType === FulfillmentDelivery
                ? t("orders.delivery")
                : t("orders.pickup")}
            </p>
          )}

          {selection.showBranchSelector ? (
            <label className="pc-field">
              <span className="pc-field__label">{t("orders.branch")}</span>
              <select
                className="pc-field__control"
                data-testid="checkout-branch-select"
                value={branchId ?? ""}
                onChange={(e) => setBranchId(e.target.value || null)}
              >
                {branches.map((b) => (
                  <option key={b.branchId} value={b.branchId}>
                    {b.name}
                  </option>
                ))}
              </select>
            </label>
          ) : null}

          {selection.fulfillmentType === FulfillmentDelivery ? (
            <div className="flex flex-col gap-3" data-testid="delivery-fields">
              <label className="pc-field">
                <span className="pc-field__label">{t("orders.recipientName")}</span>
                <input
                  className="pc-field__control"
                  value={recipientName}
                  onChange={(e) => setRecipientName(e.target.value)}
                  data-testid="delivery-recipient"
                />
              </label>
              <label className="pc-field">
                <span className="pc-field__label">{t("orders.recipientPhone")}</span>
                <input
                  className="pc-field__control"
                  value={recipientPhone}
                  onChange={(e) => setRecipientPhone(e.target.value)}
                />
              </label>
              <label className="pc-field">
                <span className="pc-field__label">{t("orders.addressLine1")}</span>
                <input
                  className="pc-field__control"
                  value={addressLine1}
                  onChange={(e) => setAddressLine1(e.target.value)}
                  data-testid="delivery-address"
                />
              </label>
              <label className="pc-field">
                <span className="pc-field__label">{t("orders.addressLine2")}</span>
                <input
                  className="pc-field__control"
                  value={addressLine2}
                  onChange={(e) => setAddressLine2(e.target.value)}
                />
              </label>
              <label className="pc-field">
                <span className="pc-field__label">{t("orders.city")}</span>
                <input
                  className="pc-field__control"
                  value={city}
                  onChange={(e) => setCity(e.target.value)}
                />
              </label>
              <label className="pc-field">
                <span className="pc-field__label">{t("orders.deliveryNotes")}</span>
                <input
                  className="pc-field__control"
                  value={deliveryNotes}
                  onChange={(e) => setDeliveryNotes(e.target.value)}
                />
              </label>
              <div className="grid grid-cols-2 gap-2">
                <label className="pc-field">
                  <span className="pc-field__label">{t("orders.latitude")}</span>
                  <input
                    className="pc-field__control"
                    value={latitude}
                    onChange={(e) => setLatitude(e.target.value)}
                    data-testid="delivery-lat"
                  />
                </label>
                <label className="pc-field">
                  <span className="pc-field__label">{t("orders.longitude")}</span>
                  <input
                    className="pc-field__control"
                    value={longitude}
                    onChange={(e) => setLongitude(e.target.value)}
                    data-testid="delivery-lng"
                  />
                </label>
              </div>
              {quoteQuery.isFetching ? <LoadingState label={t("orders.quotingFee")} /> : null}
              {quoteQuery.data ? (
                <p className="m-0 text-[length:var(--exits-text-sm)]" data-testid="delivery-fee-quote">
                  {quoteQuery.data.available
                    ? `${t("orders.deliveryFee")}: ${money(quoteQuery.data.deliveryFee)}`
                    : (quoteQuery.data.unavailableReason ?? t("orders.deliveryUnavailable"))}
                </p>
              ) : null}
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                {t("orders.feeServerAuthoritative")}
              </p>
            </div>
          ) : null}
        </section>

        <section className="pc-checkout-section">
          <h2 className="pc-checkout-section__title">{t("orders.paymentMethod")}</h2>
          <div
            className="pc-segmented pc-segmented--payment"
            role="radiogroup"
            aria-label={t("orders.paymentMethod")}
          >
            {PAYMENT_METHOD_CODES.map((code) => (
              <SegmentedOption
                key={code}
                pressed={paymentMethod === code}
                testId={`payment-${code.toLowerCase()}`}
                onClick={() => setPaymentMethod(code)}
              >
                {code === "Cash" ? (
                  <Wallet className="size-4 shrink-0" aria-hidden />
                ) : code === "ManualGCash" ? (
                  <Smartphone className="size-4 shrink-0" aria-hidden />
                ) : (
                  <Wallet className="size-4 shrink-0" aria-hidden />
                )}
                {paymentLabel(code, t)}
              </SegmentedOption>
            ))}
          </div>
        </section>

        <section className="pc-checkout-section" data-testid="checkout-totals">
          <h2 className="pc-checkout-section__title">{t("orders.total")}</h2>
          <div className="pc-checkout-totals">
            <div className="pc-checkout-totals__row">
              <span>{t("orders.subtotal")}</span>
              <strong>{money(merchandiseSubtotal)}</strong>
            </div>
            <div className="pc-checkout-totals__row">
              <span>{t("orders.deliveryFee")}</span>
              <strong>{money(deliveryFee)}</strong>
            </div>
            <div className="pc-checkout-totals__row pc-checkout-totals__row--grand">
              <span>{t("orders.total")}</span>
              <strong>{money(total)}</strong>
            </div>
          </div>
        </section>
      </div>

      <CheckoutPlaceButton
        label={t("orders.placeOrder")}
        busyLabel={t("orders.placing")}
        busy={busy}
        disabled={!selection.canPlace}
        onClick={() => void placeOrder()}
      />
    </div>
  );
}
