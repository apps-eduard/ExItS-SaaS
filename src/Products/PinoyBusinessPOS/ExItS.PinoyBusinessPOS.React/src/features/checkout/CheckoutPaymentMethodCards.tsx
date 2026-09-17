import type { LucideIcon } from "lucide-react";
import {
  Banknote,
  Building2,
  FileText,
  NotebookPen,
  Smartphone,
  Wallet,
} from "lucide-react";
import { cn } from "@/lib/cn";

export type CheckoutUiPaymentChoice =
  | "Cash"
  | "GCash"
  | "Utang"
  | "BankTransfer"
  | "Check"
  | "ManualMaya";

export type CheckoutPaymentOption = {
  value: CheckoutUiPaymentChoice;
  label: string;
  hint?: string;
  Icon: LucideIcon;
  testId: string;
  disabled?: boolean;
};

type CheckoutPaymentMethodCardsProps = {
  value: CheckoutUiPaymentChoice;
  onChange: (next: CheckoutUiPaymentChoice) => void;
  options: CheckoutPaymentOption[];
  groupLabel: string;
};

/**
 * Visual payment-method selector. Options are filtered by plan entitlement,
 * org settings, and branch availability before reaching this component.
 * Online/provider channels are never offered here.
 */
export function CheckoutPaymentMethodCards({
  value,
  onChange,
  options,
  groupLabel,
}: CheckoutPaymentMethodCardsProps) {
  return (
    <div
      className="checkout-pay-cards"
      role="radiogroup"
      aria-label={groupLabel}
      data-testid="checkout-payment-cards"
    >
      {options.map((option) => {
        const selected = value === option.value;
        const optionDisabled = option.disabled === true;
        return (
          <button
            key={option.value}
            type="button"
            role="radio"
            aria-checked={selected}
            disabled={optionDisabled}
            data-testid={option.testId}
            className={cn(
              "checkout-pay-card",
              selected && "checkout-pay-card--selected",
              optionDisabled && "checkout-pay-card--disabled",
            )}
            onClick={() => {
              if (!optionDisabled) {
                onChange(option.value);
              }
            }}
          >
            <span className="checkout-pay-card__icon" aria-hidden>
              <option.Icon className="size-7" strokeWidth={1.75} />
            </span>
            <span className="checkout-pay-card__label">{option.label}</span>
            {option.hint ? <span className="checkout-pay-card__hint">{option.hint}</span> : null}
          </button>
        );
      })}
    </div>
  );
}

export const CHECKOUT_PAYMENT_ICONS = {
  Cash: Banknote,
  GCash: Smartphone,
  Utang: NotebookPen,
  BankTransfer: Building2,
  Check: FileText,
  ManualMaya: Wallet,
} as const;
