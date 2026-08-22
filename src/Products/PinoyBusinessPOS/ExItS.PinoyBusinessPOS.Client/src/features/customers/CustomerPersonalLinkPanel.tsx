import { useEffect, useRef, useState } from "react";
import { UserRoundCheck } from "lucide-react";
import {
  createBusinessCustomerWithPersonalLink,
  resolvePublicUserId,
  type ResolvedPublicUserDto,
} from "@/api/platform/public-identity-client";
import { PlatformApiError } from "@/api/platform/platform-http";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { QrScanOrEnter } from "@/features/qr/QrScanOrEnter";
import { useI18n } from "@/i18n/I18nProvider";

export type ConfirmedPersonalLink = {
  publicUserId: string;
  userIdentityId: string;
  displayName: string;
  platformBusinessCustomerId: string;
  linkRequestId: string | null;
};

type Props = {
  organizationId: string;
  displayName: string;
  phone: string;
  notes: string;
  disabled?: boolean;
  initialSubject?: string | null;
  onLinked: (link: ConfirmedPersonalLink) => void;
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
  onLinked,
  onCleared,
}: Props) {
  const { t } = useI18n();
  const [resolved, setResolved] = useState<ResolvedPublicUserDto | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [linkSent, setLinkSent] = useState(false);
  const seededInitialSubject = useRef(false);

  async function onPayload(subjectOrPayload: string) {
    setBusy(true);
    setError(null);
    setLinkSent(false);
    try {
      const user = await resolvePublicUserId(subjectOrPayload, "SaleCustomer");
      setResolved(user);
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
    if (!displayName.trim()) {
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
        displayName: displayName.trim(),
        phone: phone.trim() || null,
        notes: taggedNotes,
        owningProductCode: "PinoyBusinessPOS",
        publicUserId: resolved.publicUserId,
        targetUserIdentityId: resolved.userIdentityId,
      });
      setLinkSent(true);
      onLinked({
        publicUserId: resolved.publicUserId,
        userIdentityId: resolved.userIdentityId,
        displayName: resolved.displayName,
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
    <Card className="flex flex-col gap-3 p-3" data-testid="customer-personal-link-panel">
      <div className="flex items-start gap-2">
        <UserRoundCheck className="mt-0.5 size-5 shrink-0 text-primary" aria-hidden />
        <div className="min-w-0 flex-1">
          <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
            {t("customers.personalLink.title")}
          </p>
          <p className="mb-0 mt-0.5 text-[length:var(--exits-text-xs)] text-muted">
            {t("customers.personalLink.lede")}
          </p>
        </div>
      </div>

      {!linkSent ? (
        <QrScanOrEnter
          expectedPurpose="personal"
          disabled={disabled || busy}
          onResolvedPayload={(value) => void onPayload(value)}
        />
      ) : null}

      {resolved && !linkSent ? (
        <div
          className="flex flex-col gap-2 rounded border border-[var(--exits-border)] p-3"
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
          <div className="flex flex-wrap gap-2">
            <Button
              type="button"
              className="min-h-11"
              data-testid="customer-personal-link-confirm-btn"
              disabled={disabled || busy}
              onClick={() => void confirmLink()}
            >
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

      {linkSent ? (
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
    </Card>
  );
}
