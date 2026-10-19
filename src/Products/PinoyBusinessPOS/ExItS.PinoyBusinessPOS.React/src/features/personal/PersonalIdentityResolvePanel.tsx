import { useEffect, useRef, useState } from "react";
import { Button } from "@/components/ui/button";
import { QrScanOrEnter } from "@/features/qr/QrScanOrEnter";
import { useI18n } from "@/i18n/I18nProvider";
import type { PersonalContactDto, ResolvedPublicUserDto } from "@/api/platform/personal-types";

type Props = {
  disabled?: boolean;
  initialSubject?: string | null;
  resolved: ResolvedPublicUserDto | null;
  existingContact: PersonalContactDto | null;
  busy?: boolean;
  onResolve: (subjectOrPayload: string) => void;
  onClear: () => void;
};

/** Personal People create: resolve Personal QR/ID and confirm identity before add. */
export function PersonalIdentityResolvePanel({
  disabled,
  initialSubject,
  resolved,
  existingContact,
  busy,
  onResolve,
  onClear,
}: Props) {
  const { t } = useI18n();
  const [seeded, setSeeded] = useState(false);
  const seededInitialSubject = useRef(false);

  useEffect(() => {
    const seed = initialSubject?.trim();
    if (!seed || seededInitialSubject.current || seeded) {
      return;
    }
    seededInitialSubject.current = true;
    setSeeded(true);
    onResolve(seed);
  }, [initialSubject, onResolve, seeded]);

  return (
    <section className="flex min-w-0 flex-col gap-3" data-testid="personal-identity-resolve-panel">
      {!resolved ? (
        <QrScanOrEnter
          expectedPurpose="personal"
          entryChooser="cards"
          disabled={disabled || busy}
          onResolvedPayload={(value) => onResolve(value)}
        />
      ) : null}

      {resolved ? (
        <div
          className="customer-personal-link__confirm"
          data-testid="identity-confirmation"
        >
          <h3 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
            {existingContact
              ? t("people.add.alreadyAddedTitle")
              : t("people.add.identityFound")}
          </h3>
          <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">{resolved.displayName}</p>
          <p className="m-0 break-all text-[length:var(--exits-text-xs)] text-muted">
            {resolved.publicUserId}
          </p>
          {resolved.maskedEmail ? (
            <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">{resolved.maskedEmail}</p>
          ) : null}
          {!existingContact ? (
            <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
              {t("people.identityPanel.confirmHint")}
            </p>
          ) : null}
          <div className="mt-2 flex flex-wrap gap-2">
            <Button
              type="button"
              variant="ghost"
              data-testid="personal-identity-clear"
              disabled={busy}
              onClick={onClear}
            >
              {t("qr.clear")}
            </Button>
          </div>
        </div>
      ) : null}
    </section>
  );
}
