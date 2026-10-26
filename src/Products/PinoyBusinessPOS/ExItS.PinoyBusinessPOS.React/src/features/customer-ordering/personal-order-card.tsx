import {
  Banknote,
  CalendarCheck2,
  CalendarDays,
  CheckCircle2,
  CircleAlert,
  Clock3,
  HandCoins,
  Package,
  ShoppingBag,
  Smartphone,
  Truck,
  type LucideIcon,
} from "lucide-react";
import type { MessageKey } from "@/i18n/messages";
import { cn } from "@/lib/cn";

export function orderCardToneClass(tone: "success" | "danger" | "warning" | "primary" | string): string {
  if (tone === "success") return "pc-order-card--success";
  if (tone === "danger" || tone === "warning") return "pc-order-card--warn";
  return "pc-order-card--primary";
}

export function isDeliveryFulfillment(type: string): boolean {
  return type.localeCompare("Delivery", undefined, { sensitivity: "accent" }) === 0;
}

export function fulfillmentLabel(type: string, t: (key: MessageKey) => string): string {
  if (isDeliveryFulfillment(type)) return t("orders.delivery");
  if (type.localeCompare("Pickup", undefined, { sensitivity: "accent" }) === 0) {
    return t("orders.pickup");
  }
  return type;
}

export function paymentMethodLabel(code: string, t: (key: MessageKey) => string): string {
  if (code === "Cash") return t("orders.paymentCash");
  if (code === "ManualGCash") return t("orders.paymentGCashShort");
  return t("orders.paymentUtang");
}

/**
 * Display payment status. Utang uses the credit lifecycle labels:
 * On Credit | Partially Paid | Paid | Credit Pending | Credit Declined.
 */
export function paymentStatusLabel(
  status: string,
  t: (key: MessageKey) => string,
  paymentMethod?: string,
): string {
  if (paymentMethod === "Utang") {
    if (status === "Paid") return t("orders.paymentPaid");
    if (status === "PartiallyPaid") return t("orders.paymentPartiallyPaid");
    if (status === "Pending" || status === "CreditPending") {
      return t("orders.paymentCreditPending");
    }
    if (status === "Declined" || status === "CreditDeclined") {
      return t("orders.paymentCreditDeclined");
    }
    return t("orders.paymentOnCredit");
  }
  if (status === "Paid") return t("orders.paymentPaid");
  if (status === "Pending") return t("orders.paymentAwaitingVerification");
  return t("orders.paymentUnpaid");
}

function paymentStatusTone(status: string, paymentMethod?: string): string {
  if (paymentMethod === "Utang") {
    if (status === "Paid") return "success";
    if (status === "PartiallyPaid") return "warn";
    if (status === "Pending" || status === "CreditPending") return "warn";
    if (status === "Declined" || status === "CreditDeclined") return "warn";
    return "primary";
  }
  if (status === "Paid") return "success";
  if (status === "Pending") return "warn";
  return "warn";
}

function paymentMethodTone(code: string): string {
  if (code === "Cash") return "success";
  if (code === "ManualGCash") return "info";
  return "primary";
}

function PaymentStatusIcon({
  status,
  paymentMethod,
}: {
  status: string;
  paymentMethod?: string;
}) {
  let Icon: LucideIcon = CircleAlert;
  if (paymentMethod === "Utang") {
    if (status === "Paid") Icon = CheckCircle2;
    else if (status === "PartiallyPaid") Icon = HandCoins;
    else if (status === "Pending" || status === "CreditPending") Icon = Clock3;
    else if (status === "Declined" || status === "CreditDeclined") Icon = CircleAlert;
    else Icon = HandCoins;
  } else if (status === "Paid") {
    Icon = CheckCircle2;
  } else if (status === "Pending") {
    Icon = Clock3;
  }
  return <Icon className="pc-order-card__fact-icon" aria-hidden />;
}

function PaymentMethodIcon({ code }: { code: string }) {
  let Icon: LucideIcon = Banknote;
  if (code === "ManualGCash") Icon = Smartphone;
  else if (code === "Utang") Icon = HandCoins;
  return <Icon className="pc-order-card__fact-icon" aria-hidden />;
}

function FulfillmentTypeIcon({ type }: { type: string }) {
  const Icon = isDeliveryFulfillment(type) ? Truck : ShoppingBag;
  return <Icon className="pc-order-card__fact-icon" aria-hidden />;
}

export function PersonalOrderFactChips({
  paymentStatus,
  paymentMethod,
  itemCount,
  fulfillmentType,
  itemsLabel,
  t,
}: {
  paymentStatus: string;
  paymentMethod: string;
  itemCount: number;
  fulfillmentType: string;
  itemsLabel: string;
  t: (key: MessageKey) => string;
}) {
  return (
    <div className="pc-order-card__meta-row" data-testid="my-order-card-facts">
      <span
        className={cn(
          "pc-order-card__fact",
          `pc-order-card__fact--${paymentStatusTone(paymentStatus, paymentMethod)}`,
        )}
        data-testid="order-payment-status"
      >
        <PaymentStatusIcon status={paymentStatus} paymentMethod={paymentMethod} />
        <span className="pc-order-card__fact-text truncate">
          {paymentStatusLabel(paymentStatus, t, paymentMethod)}
        </span>
      </span>
      <span
        className={cn("pc-order-card__fact", `pc-order-card__fact--${paymentMethodTone(paymentMethod)}`)}
        data-testid="order-payment-method"
      >
        <PaymentMethodIcon code={paymentMethod} />
        <span className="pc-order-card__fact-text truncate">{paymentMethodLabel(paymentMethod, t)}</span>
      </span>
      <span className="pc-order-card__fact pc-order-card__fact--primary">
        <Package className="pc-order-card__fact-icon" aria-hidden />
        <span className="pc-order-card__fact-text truncate">
          {itemCount} {itemsLabel}
        </span>
      </span>
      <span
        className={cn(
          "pc-order-card__fact",
          isDeliveryFulfillment(fulfillmentType) ? "pc-order-card__fact--owe" : "pc-order-card__fact--info",
        )}
        data-testid="order-fulfillment-type"
      >
        <FulfillmentTypeIcon type={fulfillmentType} />
        <span className="pc-order-card__fact-text truncate">{fulfillmentLabel(fulfillmentType, t)}</span>
      </span>
    </div>
  );
}

export function PersonalOrderTimelineRow({
  icon,
  label,
  value,
  tone = "muted",
  testId,
}: {
  icon: "preferred" | "ready" | "completed";
  label: string;
  value: string;
  tone?: "muted" | "ready" | "completed";
  testId: string;
}) {
  const Icon =
    icon === "preferred" ? CalendarDays : icon === "ready" ? Clock3 : CalendarCheck2;
  return (
    <div
      className={cn(
        "pc-order-card__timeline",
        tone === "ready" && "pc-order-card__timeline--ready",
        tone === "completed" && "pc-order-card__timeline--completed",
      )}
      data-testid={testId}
    >
      <span className="pc-order-card__timeline-icon" aria-hidden>
        <Icon className="pc-order-card__timeline-glyph" />
      </span>
      <div className="pc-order-card__timeline-copy">
        <span className="pc-order-card__timeline-label">{label}</span>
        <span className="pc-order-card__timeline-value">{value}</span>
      </div>
    </div>
  );
}
