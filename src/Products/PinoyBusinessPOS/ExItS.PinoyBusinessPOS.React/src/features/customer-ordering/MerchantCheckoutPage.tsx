import { useEffect, useMemo, useRef, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { Package, Smartphone, Truck, Users, Wallet } from "lucide-react";
import { ensurePersonalBuyerPosToken } from "@/api/platform/personal-buyer-token";
import { GCASH_REFERENCE_MAX_LENGTH } from "@/api/pos/pos-sales-client";
import { PosApiError } from "@/api/pos/pos-http";
import { describePosApiError } from "@/access/pos-commercial-errors";
import {
  getCustomerStorefront,
  isCustomerOrderingUnavailable,
  isInsufficientStockError,
  placeCustomerOrder,
  quoteCustomerDelivery,
  sellerWorkspace,
  type CustomerStorefrontProductDto,
} from "@/api/pos/pos-customer-orders-client";
import type { PosCatalogProductDto } from "@/api/pos/pos-catalog-types";
import { getLinkedCustomerStatement } from "@/api/pos/pos-linked-customers-client";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { usePersonalMerchantCart } from "@/features/customer-ordering/PersonalMerchantCartProvider";
import { ShopOrderCart } from "@/features/customer-ordering/ShopOrderCart";
import { SellWeightEntryDialog } from "@/features/sell/SellWeightEntryDialog";
import { OrderingUnavailablePanel } from "@/features/customer-ordering/OrderingUnavailablePanel";
import { PersonalCommerceNav } from "@/features/customer-ordering/PersonalCommerceNav";
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
import {
  personalCustomerRelationshipLabel,
  personalStoreDisplayName,
} from "@/features/customer-ordering/format-personal-store-label";
import { useLinkedMerchantShopContext } from "@/features/customer-ordering/useLinkedMerchantShopContext";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { useSession } from "@/session/SessionProvider";
import { personalPageBackNav } from "@/navigation/page-back-nav";

import { createSecureMutationId } from "@/lib/secure-mutation-id";

function money(n: number): string {
  return `₱${n.toFixed(2)}`;
}

function localDateIso(date = new Date()): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

function toWeightDialogProduct(
  product: CustomerStorefrontProductDto,
  organizationId: string,
): PosCatalogProductDto {
  return {
    productId: product.productId,
    organizationId,
    name: product.name,
    sku: product.sku ?? null,
    unitOfMeasure: product.unitOfMeasure,
    sellingMode: "ByWeight",
    sellingPrice: product.unitPrice,
    effectiveSellingPrice: product.unitPrice,
    status: "Active",
    createdAtUtc: "1970-01-01T00:00:00.000Z",
    updatedAtUtc: "1970-01-01T00:00:00.000Z",
    isTracked: product.tracksInventory,
    onHandQuantity: product.availableQuantity ?? undefined,
    stockStatus: product.availabilityStatus,
  };
}

function newClientOrderId(): string | null {
  const generated = createSecureMutationId();
  return generated.ok ? generated.id : null;
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
  const { cart, merchandiseSubtotal, clearAll, clearLines, setQuantity } = usePersonalMerchantCart();
  const [weightProduct, setWeightProduct] = useState<CustomerStorefrontProductDto | null>(null);

  const [fulfillmentType, setFulfillmentType] = useState(FulfillmentPickup);
  const [branchId, setBranchId] = useState<string | null>(null);
  const [deliveryServiceAreaId, setDeliveryServiceAreaId] = useState<string | null>(null);
  const [paymentMethod, setPaymentMethod] = useState<string>(PAYMENT_METHOD_CODES[0]);
  const [gcashReference, setGcashReference] = useState("");
  const [recipientName, setRecipientName] = useState(session?.displayName ?? "");
  const [recipientPhone, setRecipientPhone] = useState("");
  const [addressLine1, setAddressLine1] = useState("");
  const [addressLine2, setAddressLine2] = useState("");
  const [city, setCity] = useState("");
  const [deliveryNotes, setDeliveryNotes] = useState("");
  const [latitude, setLatitude] = useState("");
  const [longitude, setLongitude] = useState("");
  const [pickupDate, setPickupDate] = useState(localDateIso);
  const [pickupTime, setPickupTime] = useState("");
  const [pickupTimeOpen, setPickupTimeOpen] = useState(false);
  const pickupDateRef = useRef<HTMLInputElement>(null);
  const pickupTimeRef = useRef<HTMLInputElement>(null);
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

  // Always resolve Platform business-customer id for place (and statement link).
  // Shop page uses an inverted "only when unavailable" gate; that must not be reused here.
  const merchantContextQuery = useLinkedMerchantShopContext(
    organizationId,
    Boolean(organizationId) && tokenReady && online,
  );

  const storefrontQuery = useQuery({
    queryKey: [
      "storefront",
      "checkout",
      organizationId,
      merchantContextQuery.data?.businessCustomerId,
    ],
    enabled:
      Boolean(workspace) &&
      tokenReady &&
      online &&
      Boolean(merchantContextQuery.data?.businessCustomerId),
    queryFn: ({ signal }) =>
      getCustomerStorefront(
        workspace!,
        organizationId,
        {
          pageSize: 1,
          platformBusinessCustomerId: merchantContextQuery.data!.businessCustomerId ?? undefined,
        },
        signal,
      ),
    meta: { suppressGlobalError: true, operation: "load checkout storefront" },
  });

  const checkoutOrderingUnavailable =
    storefrontQuery.isError && isCustomerOrderingUnavailable(storefrontQuery.error);

  const creditProjectionQuery = useQuery({
    queryKey: [
      "linked-customer-statement",
      "checkout-utang",
      organizationId,
      merchantContextQuery.data?.businessCustomerId,
    ],
    enabled:
      Boolean(organizationId) &&
      tokenReady &&
      online &&
      Boolean(merchantContextQuery.data?.businessCustomerId),
    queryFn: ({ signal }) => {
      const businessCustomerId = merchantContextQuery.data?.businessCustomerId;
      if (!businessCustomerId) {
        return Promise.reject(new Error("Missing business customer."));
      }
      return getLinkedCustomerStatement(organizationId, businessCustomerId, { signal });
    },
    meta: { suppressGlobalError: true, operation: "load checkout utang projection" },
  });

  const utangProjection = creditProjectionQuery.data;
  const utangAvailable =
    utangProjection != null &&
    utangProjection.creditStatus === "Approved" &&
    utangProjection.availableCredit > 0;
  const orderTotalEstimate = merchandiseSubtotal; // delivery fee added later; server remains authority
  const utangInsufficient =
    utangAvailable && orderTotalEstimate > utangProjection.availableCredit;

  const checkoutStoreName =
    personalStoreDisplayName(
      storefrontQuery.data?.organizationDisplayName ??
        merchantContextQuery.data?.organizationDisplayName,
    ) || t("personal.shopLink");
  const relationshipLabel = personalCustomerRelationshipLabel(
    merchantContextQuery.data?.customerDisplayName,
    session?.displayName,
  );

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

  const selectedBranch = useMemo(() => {
    if (!storefrontQuery.data || !branchId) {
      return null;
    }
    return storefrontQuery.data.branches.find((b) => b.branchId === branchId) ?? null;
  }, [storefrontQuery.data, branchId]);

  const deliveryAreas = selectedBranch?.deliveryServiceAreas ?? [];
  const deliveryAreaIdsKey = deliveryAreas.map((area) => area.id).join(",");

  useEffect(() => {
    if (selection?.fulfillmentType !== FulfillmentDelivery) {
      setDeliveryServiceAreaId(null);
      return;
    }
    if (!deliveryAreaIdsKey) {
      setDeliveryServiceAreaId(null);
      return;
    }
    const ids = deliveryAreaIdsKey.split(",");
    if (deliveryServiceAreaId && ids.includes(deliveryServiceAreaId)) {
      return;
    }
    setDeliveryServiceAreaId(ids[0] ?? null);
  }, [selection?.fulfillmentType, deliveryAreaIdsKey, deliveryServiceAreaId]);

  const latNum = Number.parseFloat(latitude);
  const lngNum = Number.parseFloat(longitude);
  const coordsValid =
    Number.isFinite(latNum) &&
    Number.isFinite(lngNum) &&
    latNum >= -90 &&
    latNum <= 90 &&
    lngNum >= -180 &&
    lngNum <= 180;

  const platformBusinessCustomerId = merchantContextQuery.data?.businessCustomerId ?? null;

  const quoteQuery = useQuery({
    queryKey: [
      "delivery-quote",
      organizationId,
      branchId,
      deliveryServiceAreaId,
      merchandiseSubtotal,
      latitude,
      longitude,
      platformBusinessCustomerId,
    ],
    enabled:
      Boolean(workspace) &&
      tokenReady &&
      selection?.fulfillmentType === FulfillmentDelivery &&
      Boolean(branchId) &&
      Boolean(deliveryServiceAreaId) &&
      coordsValid &&
      cart.lines.length > 0,
    queryFn: () =>
      quoteCustomerDelivery(workspace!, organizationId, {
        fulfillmentBranchId: branchId!,
        merchandiseSubtotal,
        destinationLatitude: latNum,
        destinationLongitude: lngNum,
        deliveryServiceAreaId,
        platformBusinessCustomerId,
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
    if (!workspace || !selection?.branchId || cart.lines.length === 0) {
      return;
    }
    if (!session?.userId) {
      setError(t("orders.missingBuyerIdentity"));
      return;
    }

    const platformBusinessCustomerId = merchantContextQuery.data?.businessCustomerId;
    if (!platformBusinessCustomerId) {
      setError(t("orders.missingLinkedCustomer"));
      return;
    }

    const isDelivery = selection.fulfillmentType === FulfillmentDelivery;
    const gcashReferenceTrimmed = gcashReference.trim();
    if (paymentMethod === "ManualGCash" && gcashReferenceTrimmed.length === 0) {
      setError(t("checkout.gcashReferenceRequired"));
      return;
    }

    if (isDelivery) {
      if (!recipientName.trim() || !addressLine1.trim() || !coordsValid) {
        setError(t("orders.deliveryFieldsRequired"));
        return;
      }
      if (!deliveryServiceAreaId) {
        setError(
          deliveryAreas.length === 0
            ? t("orders.deliveryAreaEmpty")
            : t("orders.deliveryAreaRequired"),
        );
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
    const clientOrderId = newClientOrderId();
    if (!clientOrderId) {
      setError(t("checkout.errorSecureId"));
      setBusy(false);
      return;
    }
    try {
      const order = await placeCustomerOrder(workspace, organizationId, {
        fulfillmentType: selection.fulfillmentType,
        fulfillmentBranchId: selection.branchId,
        customerPartyType: "Personal",
        customerDisplayName: session.displayName ?? session.email ?? "Customer",
        customerPlatformUserId: session.userId,
        platformBusinessCustomerId,
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
              deliveryServiceAreaId,
            }
          : null,
        clientOrderId,
        paymentMethod,
        paymentReference: paymentMethod === "ManualGCash" ? gcashReferenceTrimmed : null,
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
              variant="setup"
              align="center"
              icon={<Users className="size-5" strokeWidth={1.75} />}
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
        <EmptyState
              align="center"
              icon={<Users className="size-5" strokeWidth={1.75} />} title={t("orders.cartEmptyTitle")} detail={t("orders.cartEmptyDetail")} />
      </div>
    );
  }

  if (checkoutOrderingUnavailable) {
    return (
      <div className={pageShell} data-testid="checkout-ordering-unavailable">
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
        <PersonalCommerceNav active="stores" />
        <OrderingUnavailablePanel
          storeName={checkoutStoreName}
          relationshipLabel={relationshipLabel}
          statementTo={merchantContextQuery.data?.statementTo}
        />
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
          className="w-fit"
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
        description={checkoutStoreName}
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
              className="w-fit"
              data-testid="stock-conflict-refresh"
              onClick={() => void storefrontQuery.refetch()}
            >
              {t("orders.refreshStorefront")}
            </Button>
          ) : null}
        </div>
      ) : null}

      <div className="pc-checkout-layout">
        <aside className="sell-cart-shell pc-checkout-cart" data-testid="checkout-lines" aria-label={t("sell.cartLabel")}>
          <ShopOrderCart
            lines={cart.lines}
            productsById={
              new Map(
                (storefrontQuery.data?.products ?? []).map((product) => [product.productId, product]),
              )
            }
            workspace={workspace}
            sellerOrganizationId={organizationId}
            subtotal={merchandiseSubtotal}
            showPayButton={false}
            onChangeQuantity={setQuantity}
            onEditWeight={setWeightProduct}
            onClear={clearLines}
          />
        </aside>
        <div className="pc-checkout-side">
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

          {selection.fulfillmentType === FulfillmentPickup ? (
            <div className="flex flex-col gap-3" data-testid="pickup-schedule">
              {selectedBranch?.storeStatusMessage?.trim() ? (
                <p className="m-0 text-[length:var(--exits-text-sm)]" data-testid="checkout-store-availability">
                  {selectedBranch.storeStatusMessage.trim()}
                </p>
              ) : null}
              <div className="flex flex-wrap items-end gap-3">
                <Button
                  type="button"
                  intent="primary"
                  appearance="ghost"
                  className="w-fit shrink-0"
                  aria-expanded={pickupTimeOpen}
                  data-testid="checkout-set-pickup-time"
                  onClick={() => {
                    setPickupTimeOpen((open) => {
                      const next = !open;
                      if (next) {
                        window.setTimeout(
                          () => pickupTimeRef.current?.showPicker?.() ?? pickupTimeRef.current?.focus(),
                          0,
                        );
                      }
                      return next;
                    });
                  }}
                >
                  {t("orders.setPickupTime")}
                </Button>
                {pickupTimeOpen ? (
                  <>
                    <label className="pc-field min-w-[9rem] flex-1">
                      <span className="pc-field__label">{t("orders.pickupTime")}</span>
                      <input
                        ref={pickupTimeRef}
                        className="pc-field__control"
                        type="time"
                        value={pickupTime}
                        data-testid="checkout-pickup-time"
                        onChange={(event) => setPickupTime(event.target.value)}
                      />
                    </label>
                    <label className="pc-field min-w-[9rem] flex-1">
                      <span className="pc-field__label">{t("orders.pickupDate")}</span>
                      <input
                        ref={pickupDateRef}
                        className="pc-field__control"
                        type="date"
                        min={localDateIso()}
                        value={pickupDate}
                        data-testid="checkout-pickup-date"
                        onChange={(event) => {
                          const today = localDateIso();
                          const next = event.target.value;
                          setPickupDate(!next || next < today ? today : next);
                        }}
                      />
                    </label>
                  </>
                ) : null}
              </div>
            </div>
          ) : null}

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
                <span className="pc-field__label">{t("orders.deliveryArea")}</span>
                {deliveryAreas.length === 0 ? (
                  <p
                    className="m-0 text-[length:var(--exits-text-sm)] text-muted"
                    data-testid="delivery-area-empty"
                  >
                    {t("orders.deliveryAreaEmpty")}
                  </p>
                ) : (
                  <select
                    className="pc-field__control"
                    data-testid="checkout-delivery-area-select"
                    value={deliveryServiceAreaId ?? ""}
                    onChange={(e) => setDeliveryServiceAreaId(e.target.value || null)}
                  >
                    {deliveryAreas.map((area) => {
                      const label = [area.cityMunicipalityName, area.regionOrProvinceName]
                        .filter(Boolean)
                        .join(", ");
                      return (
                        <option key={area.id} value={area.id}>
                          {label}
                        </option>
                      );
                    })}
                  </select>
                )}
              </label>
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
            {PAYMENT_METHOD_CODES.filter((code) => code !== "Utang" || utangAvailable).map((code) => (
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
          {paymentMethod === "ManualGCash" ? (
            <label className="pc-field mt-3" htmlFor="checkout-gcash-reference">
              <span className="pc-field__label inline-flex flex-wrap items-baseline gap-1">
                {t("checkout.paymentReference")}
                <span className="text-[length:var(--exits-text-xs)] font-semibold text-[var(--exits-danger)]">
                  {t("checkout.fieldRequired")}
                </span>
              </span>
              <input
                id="checkout-gcash-reference"
                className="pc-field__control"
                data-testid="checkout-gcash-reference"
                type="text"
                required
                aria-required
                maxLength={GCASH_REFERENCE_MAX_LENGTH}
                autoComplete="off"
                value={gcashReference}
                disabled={busy}
                onChange={(event) => setGcashReference(event.target.value)}
              />
              <span className="text-[length:var(--exits-text-xs)] text-muted">
                {t("checkout.paymentReferenceHint")}
              </span>
            </label>
          ) : null}
          {paymentMethod === "Utang" && utangAvailable && utangProjection ? (
            <dl
              className="mt-2 grid grid-cols-2 gap-x-3 gap-y-1 text-sm"
              data-testid="checkout-utang-projection"
            >
              <dt>{t("orders.utangCreditLimit")}</dt>
              <dd>{money(utangProjection.creditLimit ?? 0)}</dd>
              <dt>{t("orders.utangOutstanding")}</dt>
              <dd>{money(utangProjection.outstandingBalance)}</dd>
              <dt>{t("orders.utangPending")}</dt>
              <dd>{money(utangProjection.pendingOnlineUtangCommitment)}</dd>
              <dt>{t("orders.utangAvailable")}</dt>
              <dd>{money(utangProjection.availableCredit)}</dd>
            </dl>
          ) : null}
          {paymentMethod === "Utang" && utangInsufficient ? (
            <p className="m-0 text-sm text-destructive" data-testid="checkout-utang-insufficient">
              {t("orders.utangInsufficient")}
            </p>
          ) : null}
        </section>

        <section className="pc-checkout-section" data-testid="checkout-totals">
          <h2 className="pc-checkout-section__title">{t("orders.orderSummary")}</h2>
          <div className="pc-checkout-totals">
            <div className="pc-checkout-totals__row">
              <span>{t("orders.subtotal")}</span>
              <strong>{money(merchandiseSubtotal)}</strong>
            </div>
            <div className="pc-checkout-totals__row">
              <span>{t("orders.deliveryFee")}</span>
              <strong>{money(deliveryFee)}</strong>
            </div>
            {quoteQuery.data?.distanceExceptionApplied ? (
              <p
                className="m-0 text-[length:var(--exits-text-sm)] text-muted"
                data-testid="checkout-distance-exception"
              >
                {t("customers.delivery.extendedApproved")}
              </p>
            ) : null}
            <div className="pc-checkout-totals__row pc-checkout-totals__row--grand">
              <span>{t("orders.total")}</span>
              <strong>{money(total)}</strong>
            </div>
          </div>
        </section>
        {!selection.canPlace && selection.fulfillmentType === FulfillmentDelivery ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="checkout-place-unavailable">
            {selectedBranch?.onlineOrdersPaused
              ? t("orders.placePaused")
              : t("orders.placeDeliveryUnavailable")}
          </p>
        ) : null}
        {!selection.canPlace &&
        selection.fulfillmentType === FulfillmentPickup &&
        selectedBranch?.onlineOrdersPaused ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="checkout-place-unavailable">
            {t("orders.placePaused")}
          </p>
        ) : null}
        <CheckoutPlaceButton
          label={t("orders.placeOrder")}
          busyLabel={t("orders.placing")}
          busy={busy || merchantContextQuery.isLoading}
          disabled={
            merchantContextQuery.isLoading
            || (paymentMethod === "Utang" && utangInsufficient)
            || (paymentMethod === "ManualGCash" && gcashReference.trim().length === 0)
          }
          onClick={() => void placeOrder()}
        />
        </div>
      </div>
      <SellWeightEntryDialog
        open={weightProduct != null}
        product={weightProduct ? toWeightDialogProduct(weightProduct, organizationId) : null}
        initialKilograms={weightProduct ? (cart.lines.find((line) => line.productId === weightProduct.productId)?.quantity ?? null) : null}
        maxKilograms={weightProduct?.tracksInventory ? (weightProduct.availableQuantity ?? null) : null}
        onConfirm={(kilograms) => {
          if (weightProduct) {
            setQuantity(weightProduct, kilograms);
          }
          setWeightProduct(null);
        }}
        onRemove={
          weightProduct
            ? () => {
                setQuantity(weightProduct, 0);
                setWeightProduct(null);
              }
            : undefined
        }
        onCancel={() => setWeightProduct(null)}
      />
    </div>
  );
}
