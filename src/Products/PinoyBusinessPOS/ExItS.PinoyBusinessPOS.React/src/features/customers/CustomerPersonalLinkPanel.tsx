import { useEffect, useRef, useState } from "react";
import { Link } from "react-router-dom";
import { Loader2, TriangleAlert, UserRoundCheck } from "lucide-react";
import {
  resolvePublicUserId,
  type ResolvedPublicUserDto,
} from "@/api/platform/public-identity-client";
import { PlatformApiError } from "@/api/platform/platform-http";
import { Button } from "@/components/ui/button";
import { QrScanOrEnter } from "@/features/qr/QrScanOrEnter";
import { useI18n } from "@/i18n/I18nProvider";

export type SelectedPersonalIdentity = {
  publicUserId: string;
  userIdentityId: string;
  displayName: string;
  maskedEmail: string | null;
};

export type ExistingPersonalCustomerMatch = {
  customerId: string;
  displayName: string;
};

/** @deprecated Use SelectedPersonalIdentity — resolve is identification-only until Save & send. */
export type PendingPersonalCustomerLink = SelectedPersonalIdentity & {
  platformBusinessCustomerId?: string;
  linkRequestId?: string | null;
};

/** @deprecated Use SelectedPersonalIdentity */
export type ConfirmedPersonalLink = PendingPersonalCustomerLink;

type Props = {
  disabled?: boolean;
  initialSubject?: string | null;
  /** Set after POS lookup when this ExItS ID is already a customer. */
  existingMatch?: ExistingPersonalCustomerMatch | null;
  checkingExisting?: boolean;
  onResolved?: (user: ResolvedPublicUserDto) => void;
  onCleared: () => void;
};

/**
 * Organization customer create: resolve Personal QR/ID. Save on the form sends the link request.
 */
export function CustomerPersonalLinkPanel({
  disabled,
  initialSubject,
  existingMatch = null,
  checkingExisting = false,
  onResolved,
  onCleared,
}: Props) {
  const { t } = useI18n();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const seededInitialSubject = useRef(false);

  async function onPayload(subjectOrPayload: string) {
    setBusy(true);
    setError(null);
    try {
      const user = await resolvePublicUserId(subjectOrPayload, "SaleCustomer");
      onResolved?.(user);
    } catch (err) {
      onCleared();
      setError(
        err instanceof PlatformApiError ? err.message : t("customers.personalLink.resolveFailed"),
      );
    } finally {
      setBusy(false);
    }
  }

  useEffect(() => {
    const seed = initialSubject?.trim();
    if (!seed || seededInitialSubject.current) {
      return;
    }
    seededInitialSubject.current = true;
    void onPayload(seed);
    // eslint-disable-next-line react-hooks/exhaustive-deps -- intentional one-shot seed
  }, [initialSubject]);

  const alreadyInContacts = Boolean(existingMatch);

  return (
    <section className="catalog-form-section exits-animate-panel" data-testid="customer-personal-link-panel">
      <div className="flex items-start gap-2">
        <span className="customer-personal-link__icon" aria-hidden>
          <UserRoundCheck />
        </span>
        <div className="min-w-0 flex-1">
          <h2 className="catalog-form-section__title">{t("customers.personalLink.title")}</h2>
          <p className="mb-0 mt-0.5 text-[length:var(--exits-text-sm)] text-muted">
            {t("customers.personalLink.lede")}
          </p>
        </div>
      </div>

      <QrScanOrEnter
        expectedPurpose="personal"
        disabled={disabled || busy}
        onResolvedPayload={(value) => void onPayload(value)}
        onManualCleared={onCleared}
      />

      {checkingExisting && !alreadyInContacts ? (
        <p
          className="m-0 inline-flex items-center gap-2 text-[length:var(--exits-text-sm)] text-muted"
          data-testid="customer-personal-link-checking"
        >
          <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
          {t("loading.label")}
        </p>
      ) : null}

      {alreadyInContacts && existingMatch ? (
        <div
          className="exits-alert exits-alert--warning customer-already-in-contacts"
          data-testid="customer-already-in-contacts"
          role="alert"
        >
          <TriangleAlert className="exits-alert__icon size-5 shrink-0" aria-hidden />
          <div className="exits-alert__content">
            <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
              {t("customers.alreadyInContacts").replace("{name}", existingMatch.displayName)}
            </p>
            <Button asChild className="w-full sm:w-auto">
              <Link
                to={`/customers/${existingMatch.customerId}`}
                data-testid="customer-already-in-contacts-open"
              >
                {t("customers.openExisting")}
              </Link>
            </Button>
          </div>
        </div>
      ) : null}

      {error ? (
        <p
          className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]"
          data-testid="customer-personal-link-error"
          role="alert"
        >
          {error}
        </p>
      ) : null}
    </section>
  );
}
