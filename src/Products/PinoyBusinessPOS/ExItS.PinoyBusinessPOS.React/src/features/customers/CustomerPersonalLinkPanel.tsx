import { useEffect, useRef, useState } from "react";
import { Loader2, UserRoundCheck } from "lucide-react";
import {
  createBusinessCustomerWithPersonalLink,
  resolvePublicUserId,
  type ResolvedPublicUserDto,
} from "@/api/platform/public-identity-client";
import { PlatformApiError } from "@/api/platform/platform-http";
import { Button } from "@/components/ui/button";
import { QrScanOrEnter } from "@/features/qr/QrScanOrEnter";
import { useI18n } from "@/i18n/I18nProvider";

export type PendingPersonalCustomerLink = {
  publicUserId: string;
  userIdentityId: string;
  displayName: string;
  platformBusinessCustomerId: string;
  linkRequestId: string | null;
};

/** @deprecated Use PendingPersonalCustomerLink — create sends a pending request, not an active link. */
export type ConfirmedPersonalLink = PendingPersonalCustomerLink;

type Props = {
  organizationId: string;
  displayName: string;
  phone: string;
  notes: string;
  disabled?: boolean;
  initialSubject?: string | null;
  /** When lookup succeeds, parent may prefill Basics from the resolved Personal profile. */
  onResolved?: (user: ResolvedPublicUserDto) => void;
  onLinkRequestCreated: (link: PendingPersonalCustomerLink) => void;
  onCleared: () => void;
};

/**
 * Organization customer create: resolve Personal QR/ID → confirm → pending CustomerLinkRequest.
 * Does not activate LinkedCustomerAppUser (Personal must Accept).
 */
export function CustomerPersonalLinkPanel({
  organizationId,
  displayName,
  phone,
  notes,
  disabled,
  initialSubject,
  onResolved,
  onLinkRequestCreated,
  onCleared,
}: Props) {
  const { t } = useI18n();
  const [resolved, setResolved] = useState<ResolvedPublicUserDto | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [requestSent, setRequestSent] = useState(false);
  const seededInitialSubject = useRef(false);

  async function onPayload(subjectOrPayload: string) {
    setBusy(true);
    setError(null);
    setRequestSent(false);
    try {
      const user = await resolvePublicUserId(subjectOrPayload, "SaleCustomer");
      setResolved(user);
      onResolved?.(user);
    } catch (err) {
      setResolved(null);
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
    // Seed once from checkout deep-link; onPayload is stable for the initial resolve only.
    // eslint-disable-next-line react-hooks/exhaustive-deps -- intentional one-shot seed
  }, [initialSubject]);

  async function confirmLink() {
    if (!resolved) return;
    // Prefer the Basics field when filled; otherwise use the looked-up Personal display name.
    const name = displayName.trim() || resolved.displayName.trim();
    if (!name) {
      setError(t("customers.personalLink.nameRequired"));
      return;
    }
    setBusy(true);
    setError(null);
    try {
      const taggedNotes = notes.trim()
        ? `${notes.trim()}\nexits-id:${resolved.publicUserId}`
        : `exits-id:${resolved.publicUserId}`;
      const result = await createBusinessCustomerWithPersonalLink(organizationId, {
        displayName: name,
        phone: phone.trim() || null,
        notes: taggedNotes,
        owningProductCode: "PinoyBusinessPOS",
        publicUserId: resolved.publicUserId,
        targetUserIdentityId: resolved.userIdentityId,
      });
      setRequestSent(true);
      onLinkRequestCreated({
        publicUserId: resolved.publicUserId,
        userIdentityId: resolved.userIdentityId,
        displayName: name,
        platformBusinessCustomerId: result.customerId,
        linkRequestId: result.linkRequestId,
      });
    } catch (err) {
      setError(
        err instanceof PlatformApiError ? err.message : t("customers.personalLink.createFailed"),
      );
    } finally {
      setBusy(false);
    }
  }

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

      {!requestSent ? (
        <QrScanOrEnter
          expectedPurpose="personal"
          disabled={disabled || busy}
          onResolvedPayload={(value) => void onPayload(value)}
        />
      ) : null}

      {resolved && !requestSent ? (
        <div
          className="customer-personal-link__confirm"
          data-testid="customer-personal-link-confirm"
        >
          <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
            {resolved.displayName}
          </p>
          <p className="m-0 break-all text-[length:var(--exits-text-xs)] text-muted">
            {resolved.publicUserId}
          </p>
          {resolved.maskedEmail ? (
            <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
              {resolved.maskedEmail}
            </p>
          ) : null}
          <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
            {t("customers.personalLink.confirmHint")}
          </p>
          <div className="mt-2 flex flex-wrap gap-2">
            <Button
              type="button"
              variant="outline"
              className="min-h-11"
              data-testid="customer-personal-link-confirm-btn"
              disabled={disabled || busy}
              onClick={() => void confirmLink()}
            >
              {busy ? (
                <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
              ) : (
                <UserRoundCheck className="size-4 shrink-0" aria-hidden />
              )}
              {busy ? t("customers.personalLink.sending") : t("customers.personalLink.confirm")}
            </Button>
            <Button
              type="button"
              variant="ghost"
              className="min-h-11"
              data-testid="customer-personal-link-cancel"
              disabled={busy}
              onClick={() => {
                setResolved(null);
                onCleared();
              }}
            >
              {t("qr.clear")}
            </Button>
          </div>
        </div>
      ) : null}

      {requestSent ? (
        <p
          className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-success)]"
          data-testid="customer-personal-link-sent"
        >
          {t("customers.personalLink.sent")}
        </p>
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
