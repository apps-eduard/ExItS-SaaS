import { useEffect, useRef, useState } from "react";
import { Loader2, UserRoundCheck } from "lucide-react";
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
  selected: SelectedPersonalIdentity | null;
  /** When lookup succeeds, parent may prefill Basics from the resolved Personal profile. */
  onResolved?: (user: ResolvedPublicUserDto) => void;
  onSelected: (identity: SelectedPersonalIdentity) => void;
  onCleared: () => void;
};

/**
 * Organization customer create: resolve Personal QR/ID → select identity for later Save & send.
 * Does not create BusinessCustomer or CustomerLinkRequest (Save on the form does that).
 */
export function CustomerPersonalLinkPanel({
  disabled,
  initialSubject,
  selected,
  onResolved,
  onSelected,
  onCleared,
}: Props) {
  const { t } = useI18n();
  const [preview, setPreview] = useState<ResolvedPublicUserDto | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const seededInitialSubject = useRef(false);

  async function onPayload(subjectOrPayload: string) {
    setBusy(true);
    setError(null);
    try {
      const user = await resolvePublicUserId(subjectOrPayload, "SaleCustomer");
      setPreview(user);
      onResolved?.(user);
    } catch (err) {
      setPreview(null);
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

  function useAccount() {
    if (!preview) return;
    onSelected({
      publicUserId: preview.publicUserId,
      userIdentityId: preview.userIdentityId,
      displayName: preview.displayName.trim(),
      maskedEmail: preview.maskedEmail ?? null,
    });
    setError(null);
  }

  const showLookup = !selected;

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

      {showLookup ? (
        <QrScanOrEnter
          expectedPurpose="personal"
          disabled={disabled || busy}
          onResolvedPayload={(value) => void onPayload(value)}
        />
      ) : null}

      {preview && !selected ? (
        <div
          className="customer-personal-link__confirm"
          data-testid="customer-personal-link-confirm"
        >
          <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
            {t("customers.personalLink.foundTitle")}
          </p>
          <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">{preview.displayName}</p>
          <p className="m-0 break-all text-[length:var(--exits-text-xs)] text-muted">
            {preview.publicUserId}
          </p>
          {preview.maskedEmail ? (
            <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">{preview.maskedEmail}</p>
          ) : null}
          <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
            {t("customers.personalLink.confirmHint").replace("{name}", preview.displayName)}
          </p>
          <div className="mt-2 flex flex-wrap gap-2">
            <Button
              type="button"
              variant="outline"
              className="min-h-11"
              data-testid="customer-personal-link-confirm-btn"
              disabled={disabled || busy}
              onClick={useAccount}
            >
              {busy ? (
                <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
              ) : (
                <UserRoundCheck className="size-4 shrink-0" aria-hidden />
              )}
              {t("customers.personalLink.useAccount")}
            </Button>
            <Button
              type="button"
              variant="ghost"
              className="min-h-11"
              data-testid="customer-personal-link-cancel"
              disabled={busy}
              onClick={() => {
                setPreview(null);
                onCleared();
              }}
            >
              {t("qr.clear")}
            </Button>
          </div>
        </div>
      ) : null}

      {selected ? (
        <div
          className="customer-personal-link__confirm"
          data-testid="customer-personal-link-selected"
        >
          <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
            {t("customers.personalLink.selectedTitle")}
          </p>
          <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">{selected.displayName}</p>
          <p className="m-0 break-all text-[length:var(--exits-text-xs)] text-muted">
            {selected.publicUserId}
          </p>
          <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
            {t("customers.personalLink.confirmHint").replace("{name}", selected.displayName)}
          </p>
          <Button
            type="button"
            variant="ghost"
            className="mt-2 min-h-11"
            data-testid="customer-personal-link-change"
            disabled={disabled || busy}
            onClick={() => {
              setPreview(null);
              onCleared();
            }}
          >
            {t("customers.personalLink.changeAccount")}
          </Button>
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
