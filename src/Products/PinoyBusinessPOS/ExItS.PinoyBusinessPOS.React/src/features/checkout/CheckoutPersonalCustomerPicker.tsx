import { useState } from "react";
import { Link } from "react-router-dom";
import { ChevronDown, UserRoundSearch } from "lucide-react";
import { resolvePublicUserId } from "@/api/platform/public-identity-client";
import { PlatformApiError } from "@/api/platform/platform-http";
import type { CheckoutCustomerSearchItem } from "@/api/pos/pos-customers-client";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { PosApiError } from "@/api/pos/pos-http";
import { Button } from "@/components/ui/button";
import type { CheckoutCustomerOption } from "@/features/checkout/checkout-customer-option";
import { findExistingCheckoutCustomerForPersonalId } from "@/features/checkout/find-existing-checkout-customer";
import { QrScanOrEnter } from "@/features/qr/QrScanOrEnter";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";

export type { CheckoutCustomerOption };

type Props = {
  workspace: PosWorkspaceScope;
  disabled?: boolean;
  canLinkCustomer: boolean;
  returnTo: string;
  /** When a customer is already on the sale, hide lookup and do not send requests. */
  selectedCustomerId?: string | null;
  onCustomerSelected: (customer: CheckoutCustomerOption) => void;
};

function toCheckoutOption(
  item: CheckoutCustomerSearchItem,
  extras?: {
    linkedPersonalPublicUserId?: string | null;
    resolvedPersonalDisplayName?: string | null;
  },
): CheckoutCustomerOption {
  return {
    customerId: item.customerId,
    displayName: item.displayName,
    mobileNumber: item.mobileNumber,
    status: item.status,
    linkedPersonalPublicUserId: extras?.linkedPersonalPublicUserId ?? null,
    resolvedPersonalDisplayName: extras?.resolvedPersonalDisplayName ?? null,
  };
}

/**
 * Checkout customer selection via Personal QR / ExItS ID — selection only, never creates customers.
 * Hidden by default. If the Personal ID already has a POS contact, select that row; do not add/link.
 */
export function CheckoutPersonalCustomerPicker({
  workspace,
  disabled,
  canLinkCustomer,
  returnTo,
  selectedCustomerId,
  onCustomerSelected,
}: Props) {
  const { t } = useI18n();
  const [open, setOpen] = useState(false);
  const [busy, setBusy] = useState(false);
  const [resolvedPublicId, setResolvedPublicId] = useState<string | null>(null);
  const [resolvedDisplayName, setResolvedDisplayName] = useState<string | null>(null);
  const [notLinked, setNotLinked] = useState(false);
  const [error, setError] = useState<string | null>(null);

  if (selectedCustomerId) {
    return null;
  }

  async function resolveAndLookup(subjectOrPayload: string) {
    if (disabled) {
      return;
    }
    setBusy(true);
    setError(null);
    setNotLinked(false);
    setResolvedPublicId(null);
    setResolvedDisplayName(null);
    try {
      const resolved = await resolvePublicUserId(subjectOrPayload, "SaleCustomer");
      setResolvedPublicId(resolved.publicUserId);
      setResolvedDisplayName(resolved.displayName);

      const existing = await findExistingCheckoutCustomerForPersonalId(
        workspace,
        resolved.publicUserId,
      );
      if (existing) {
        onCustomerSelected(
          toCheckoutOption(existing, {
            linkedPersonalPublicUserId: resolved.publicUserId,
            resolvedPersonalDisplayName: resolved.displayName,
          }),
        );
        setNotLinked(false);
        setOpen(false);
        return;
      }

      setNotLinked(true);
    } catch (err) {
      if (err instanceof PlatformApiError || err instanceof PosApiError) {
        setError(err.message || t("checkout.personalResolveFailed"));
        return;
      }
      setError(t("checkout.personalResolveFailed"));
    } finally {
      setBusy(false);
    }
  }

  function clearState() {
    setResolvedPublicId(null);
    setResolvedDisplayName(null);
    setNotLinked(false);
    setError(null);
  }

  const linkHref =
    resolvedPublicId && canLinkCustomer
      ? `/customers/new?linkPublicId=${encodeURIComponent(resolvedPublicId)}&returnTo=${encodeURIComponent(returnTo)}`
      : null;

  return (
    <div
      className="mt-3 flex flex-col gap-3 rounded-[var(--exits-radius-md)] border border-border bg-[var(--exits-surface-muted)] p-3"
      data-testid="checkout-personal-customer-picker"
      data-open={open ? "true" : "false"}
    >
      <button
        type="button"
        className="checkout-personal-lookup-toggle"
        data-testid="checkout-personal-customer-picker-toggle"
        aria-expanded={open}
        disabled={disabled || busy}
        onClick={() => setOpen((current) => !current)}
      >
        <UserRoundSearch className="size-5 shrink-0 text-primary" aria-hidden />
        <span className="min-w-0 flex-1 text-left">
          <span className="block text-[length:var(--exits-text-sm)] font-semibold">
            {t("checkout.personalCustomerTitle")}
          </span>
          <span className="mt-0.5 block text-[length:var(--exits-text-xs)] text-muted">
            {open ? t("checkout.personalCustomerHint") : t("checkout.personalCustomerShowHint")}
          </span>
        </span>
        <ChevronDown
          className={cn("size-4 shrink-0 text-muted transition-transform", open && "rotate-180")}
          aria-hidden
        />
      </button>

      {open ? (
        <>
          <QrScanOrEnter
            expectedPurpose="personal"
            disabled={disabled || busy}
            onResolvedPayload={(value) => void resolveAndLookup(value)}
          />

          {notLinked && resolvedPublicId ? (
            <div
              className="flex flex-col gap-2 rounded border border-[var(--exits-border)] bg-surface p-3"
              data-testid="checkout-personal-not-linked"
            >
              <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
                {t("checkout.customerNotLinked")}
              </p>
              {resolvedDisplayName ? (
                <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
                  {resolvedDisplayName}
                </p>
              ) : null}
              <p className="m-0 break-all text-[length:var(--exits-text-xs)] text-muted">
                {resolvedPublicId}
              </p>
              <div className="flex flex-wrap gap-2">
                {linkHref ? (
                  <Button asChild className="min-h-11" data-testid="checkout-personal-add-link">
                    <Link to={linkHref}>{t("checkout.addLinkCustomer")}</Link>
                  </Button>
                ) : (
                  <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                    {t("checkout.customerNotLinkedDenied")}
                  </p>
                )}
                <Button
                  type="button"
                  variant="ghost"
                  className="min-h-11"
                  data-testid="checkout-personal-not-linked-cancel"
                  disabled={disabled || busy}
                  onClick={clearState}
                >
                  {t("checkout.customerNotLinkedCancel")}
                </Button>
              </div>
            </div>
          ) : null}

          {error ? (
            <p
              className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]"
              data-testid="checkout-personal-customer-error"
              role="alert"
            >
              {error}
            </p>
          ) : null}
        </>
      ) : null}
    </div>
  );
}
