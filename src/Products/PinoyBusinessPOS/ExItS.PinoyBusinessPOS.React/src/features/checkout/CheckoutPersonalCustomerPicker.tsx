import { useState } from "react";
import { Link } from "react-router-dom";
import { UserRoundSearch } from "lucide-react";
import { resolvePublicUserId } from "@/api/platform/public-identity-client";
import { PlatformApiError } from "@/api/platform/platform-http";
import {
  findCustomerByLinkedPersonalPublicUserId,
  type CheckoutCustomerSearchItem,
} from "@/api/pos/pos-customers-client";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { PosApiError } from "@/api/pos/pos-http";
import { Button } from "@/components/ui/button";
import { QrScanOrEnter } from "@/features/qr/QrScanOrEnter";
import { useI18n } from "@/i18n/I18nProvider";

export type CheckoutCustomerOption = {
  customerId: string;
  displayName: string;
  mobileNumber?: string | null;
  status: string;
};

type Props = {
  workspace: PosWorkspaceScope;
  disabled?: boolean;
  canLinkCustomer: boolean;
  returnTo: string;
  onCustomerSelected: (customer: CheckoutCustomerOption) => void;
};

function toCheckoutOption(item: CheckoutCustomerSearchItem): CheckoutCustomerOption {
  return {
    customerId: item.customerId,
    displayName: item.displayName,
    mobileNumber: item.mobileNumber,
    status: item.status,
  };
}

/**
 * Checkout customer selection via Personal QR / ExItS ID — selection only, never creates customers.
 */
export function CheckoutPersonalCustomerPicker({
  workspace,
  disabled,
  canLinkCustomer,
  returnTo,
  onCustomerSelected,
}: Props) {
  const { t } = useI18n();
  const [busy, setBusy] = useState(false);
  const [resolvedPublicId, setResolvedPublicId] = useState<string | null>(null);
  const [resolvedDisplayName, setResolvedDisplayName] = useState<string | null>(null);
  const [notLinked, setNotLinked] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function resolveAndLookup(subjectOrPayload: string) {
    setBusy(true);
    setError(null);
    setNotLinked(false);
    setResolvedPublicId(null);
    setResolvedDisplayName(null);
    try {
      const resolved = await resolvePublicUserId(subjectOrPayload, "SaleCustomer");
      setResolvedPublicId(resolved.publicUserId);
      setResolvedDisplayName(resolved.displayName);

      const correlated = await findCustomerByLinkedPersonalPublicUserId(
        workspace,
        resolved.publicUserId,
      );
      if (!correlated) {
        setNotLinked(true);
        return;
      }

      onCustomerSelected(toCheckoutOption(correlated));
      setNotLinked(false);
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
    >
      <div className="flex items-start gap-2">
        <UserRoundSearch className="mt-0.5 size-5 shrink-0 text-primary" aria-hidden />
        <div className="min-w-0 flex-1">
          <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
            {t("checkout.personalCustomerTitle")}
          </p>
          <p className="mb-0 mt-0.5 text-[length:var(--exits-text-xs)] text-muted">
            {t("checkout.personalCustomerHint")}
          </p>
        </div>
      </div>

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
            <p className="m-0 text-[length:var(--exits-text-sm)]">{resolvedDisplayName}</p>
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
    </div>
  );
}
