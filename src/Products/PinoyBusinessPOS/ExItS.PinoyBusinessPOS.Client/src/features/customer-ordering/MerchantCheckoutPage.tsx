import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { ensurePersonalBuyerPosToken } from "@/api/platform/personal-buyer-token";
import { PosApiError } from "@/api/pos/pos-http";
import {
  getCustomerStorefront,
  isInsufficientStockError,
  placeCustomerOrder,
  quoteCustomerDelivery,
  sellerWorkspace,
} from "@/api/pos/pos-customer-orders-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { usePersonalMerchantCart } from "@/features/customer-ordering/PersonalMerchantCartProvider";
import {
  eligibleBranches,
  FulfillmentDelivery,
  FulfillmentPickup,
  PAYMENT_METHOD_CODES,
  resolveFulfillmentSelection,
} from "@/features/customer-ordering/personal-merchant-cart";
import { useI18n } from "@/i18n/I18nProvider";
import { useSession } from "@/session/SessionProvider";

function money(n: number): string {
  return `₱${n.toFixed(2)}`;
}

function newClientOrderId(): string {
  return crypto.randomUUID();
}

export function MerchantCheckoutPage() {
  const { t } = useI18n();
  const navigate = useNavigate();
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
    enabled: Boolean(workspace) && tokenReady,
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
        setError(err.problem.detail ?? err.message);
      } else {
        setError(err instanceof Error ? err.message : t("orders.error"));
      }
    } finally {
      setBusy(false);
    }
  }

  if (!tokenReady || storefrontQuery.isLoading) {
    return <LoadingState label={t("loading.label")} />;
  }

  if (cart.lines.length === 0 || cart.sellerOrganizationId !== organizationId) {
    return (
      <div className="flex min-w-0 flex-col gap-4" data-testid="checkout-empty">
        <EmptyState title={t("orders.cartEmptyTitle")} detail={t("orders.cartEmptyDetail")} />
        <Button asChild className="min-h-11 w-fit">
          <Link to={`/personal/linked-merchants/${organizationId}/shop`}>
            {t("orders.backToShop")}
          </Link>
        </Button>
      </div>
    );
  }

  if (storefrontQuery.isError || !storefrontQuery.data || !selection) {
    return (
      <div className="flex min-w-0 flex-col gap-4">
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
    <div className="flex min-w-0 flex-col gap-4" data-testid="merchant-checkout-page">
      <PageHeader
        title={t("orders.checkoutTitle")}
        description={storefrontQuery.data.organizationDisplayName}
      />
      <Button asChild variant="ghost" className="min-h-11 w-fit">
        <Link to={`/personal/linked-merchants/${organizationId}/shop`}>
          {t("orders.backToShop")}
        </Link>
      </Button>

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

      <Card className="flex flex-col gap-2" data-testid="checkout-lines">
        {cart.lines.map((line) => (
          <div key={line.productId} className="flex justify-between gap-2">
            <span>
              {line.name} × {line.quantity}
            </span>
            <strong>{money(Math.round(line.unitPrice * line.quantity * 100) / 100)}</strong>
          </div>
        ))}
      </Card>

      {selection.showFulfillmentToggle ? (
        <div className="flex flex-wrap gap-2" role="group" aria-label={t("orders.fulfillmentType")}>
          <Button
            type="button"
            variant={selection.fulfillmentType === FulfillmentPickup ? "default" : "ghost"}
            className="min-h-11"
            data-testid="fulfillment-pickup"
            onClick={() => setFulfillmentType(FulfillmentPickup)}
          >
            {t("orders.pickup")}
          </Button>
          <Button
            type="button"
            variant={selection.fulfillmentType === FulfillmentDelivery ? "default" : "ghost"}
            className="min-h-11"
            data-testid="fulfillment-delivery"
            onClick={() => setFulfillmentType(FulfillmentDelivery)}
          >
            {t("orders.delivery")}
          </Button>
        </div>
      ) : null}

      {selection.showBranchSelector ? (
        <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
          <span>{t("orders.branch")}</span>
          <select
            className="min-h-11 rounded border px-3"
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
        <Card className="flex flex-col gap-3" data-testid="delivery-fields">
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            <span>{t("orders.recipientName")}</span>
            <input
              className="min-h-11 rounded border px-3"
              value={recipientName}
              onChange={(e) => setRecipientName(e.target.value)}
              data-testid="delivery-recipient"
            />
          </label>
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            <span>{t("orders.recipientPhone")}</span>
            <input
              className="min-h-11 rounded border px-3"
              value={recipientPhone}
              onChange={(e) => setRecipientPhone(e.target.value)}
            />
          </label>
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            <span>{t("orders.addressLine1")}</span>
            <input
              className="min-h-11 rounded border px-3"
              value={addressLine1}
              onChange={(e) => setAddressLine1(e.target.value)}
              data-testid="delivery-address"
            />
          </label>
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            <span>{t("orders.addressLine2")}</span>
            <input
              className="min-h-11 rounded border px-3"
              value={addressLine2}
              onChange={(e) => setAddressLine2(e.target.value)}
            />
          </label>
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            <span>{t("orders.city")}</span>
            <input
              className="min-h-11 rounded border px-3"
              value={city}
              onChange={(e) => setCity(e.target.value)}
            />
          </label>
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            <span>{t("orders.deliveryNotes")}</span>
            <input
              className="min-h-11 rounded border px-3"
              value={deliveryNotes}
              onChange={(e) => setDeliveryNotes(e.target.value)}
            />
          </label>
          <div className="grid grid-cols-2 gap-2">
            <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
              <span>{t("orders.latitude")}</span>
              <input
                className="min-h-11 rounded border px-3"
                value={latitude}
                onChange={(e) => setLatitude(e.target.value)}
                data-testid="delivery-lat"
              />
            </label>
            <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
              <span>{t("orders.longitude")}</span>
              <input
                className="min-h-11 rounded border px-3"
                value={longitude}
                onChange={(e) => setLongitude(e.target.value)}
                data-testid="delivery-lng"
              />
            </label>
          </div>
          {quoteQuery.isFetching ? <LoadingState label={t("orders.quotingFee")} /> : null}
          {quoteQuery.data ? (
            <p className="m-0" data-testid="delivery-fee-quote">
              {quoteQuery.data.available
                ? `${t("orders.deliveryFee")}: ${money(quoteQuery.data.deliveryFee)}`
                : (quoteQuery.data.unavailableReason ?? t("orders.deliveryUnavailable"))}
            </p>
          ) : null}
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("orders.feeServerAuthoritative")}
          </p>
        </Card>
      ) : null}

      <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
        <span>{t("orders.paymentMethod")}</span>
        <select
          className="min-h-11 rounded border px-3"
          value={paymentMethod}
          onChange={(e) => setPaymentMethod(e.target.value)}
          data-testid="payment-method"
        >
          {PAYMENT_METHOD_CODES.map((code) => (
            <option key={code} value={code}>
              {t(
                code === "Cash"
                  ? "orders.paymentCash"
                  : code === "ManualGCash"
                    ? "orders.paymentGCash"
                    : "orders.paymentUtang",
              )}
            </option>
          ))}
        </select>
      </label>

      <Card className="flex flex-col gap-1" data-testid="checkout-totals">
        <div className="flex justify-between">
          <span>{t("orders.subtotal")}</span>
          <strong>{money(merchandiseSubtotal)}</strong>
        </div>
        <div className="flex justify-between">
          <span>{t("orders.deliveryFee")}</span>
          <strong>{money(deliveryFee)}</strong>
        </div>
        <div className="flex justify-between">
          <span>{t("orders.total")}</span>
          <strong>{money(total)}</strong>
        </div>
      </Card>

      <Button
        type="button"
        className="min-h-11"
        data-testid="place-order"
        disabled={busy || !selection.canPlace}
        onClick={() => void placeOrder()}
      >
        {busy ? t("orders.placing") : t("orders.placeOrder")}
      </Button>
    </div>
  );
}
