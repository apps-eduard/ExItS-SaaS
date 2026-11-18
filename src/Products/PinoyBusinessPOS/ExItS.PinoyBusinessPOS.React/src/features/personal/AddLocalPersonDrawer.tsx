import { IdCard, Loader2, UserPlus } from "lucide-react";
import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { BottomSheet } from "@/components/exits/SheetDialog";
import { ExitsOrgEhgButton } from "@/components/exits/ExitsOrgEhgButton";
import { ExitsOrgEhgInput } from "@/components/exits/ExitsOrgEhgInput";
import { Notice } from "@/components/exits/Notice";
import { useCreateContactMutation } from "@/features/personal/people-queries";
import { useI18n } from "@/i18n/I18nProvider";

type AddLocalPersonDrawerProps = {
  open: boolean;
  onClose: () => void;
  onSwitchToExits?: () => void;
};

/**
 * Add local person — bottom drawer with Org EHG input / button glass.
 */
export function AddLocalPersonDrawer({
  open,
  onClose,
  onSwitchToExits,
}: AddLocalPersonDrawerProps) {
  const { t } = useI18n();
  const navigate = useNavigate();
  const createMutation = useCreateContactMutation();
  const [displayName, setDisplayName] = useState("");
  const [phone, setPhone] = useState("");
  const [email, setEmail] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (!open) {
      return;
    }
    setDisplayName("");
    setPhone("");
    setEmail("");
    setError(null);
    const outer = window.requestAnimationFrame(() => {
      window.requestAnimationFrame(() => {
        document.getElementById("person-display-name")?.focus();
      });
    });
    return () => window.cancelAnimationFrame(outer);
  }, [open]);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);
    const name = displayName.trim();
    if (!name) {
      setError(t("people.localAdd.nameRequired"));
      return;
    }
    setSaving(true);
    try {
      const contact = await createMutation.mutateAsync({
        displayName: name,
        phone: phone.trim() || null,
        email: email.trim() || null,
      });
      onClose();
      void navigate(`/personal/people/${contact.id}`, { replace: true });
    } catch (err) {
      setError(err instanceof Error ? err.message : t("error.body"));
    } finally {
      setSaving(false);
    }
  }

  return (
    <BottomSheet
      open={open}
      onClose={() => {
        if (saving) {
          return;
        }
        onClose();
      }}
      title={t("people.addLocal")}
      panelId="people-add-local-sheet"
      testId="people-add-dialog"
      closeLabel={t("people.add.cancel")}
      closeTestId="people-add-dialog-close"
      presentation="sheet"
      panelClassName="people-add-local-sheet"
    >
      <form
        className="person-form-page people-add-local-sheet__form flex min-w-0 flex-col gap-3"
        data-testid="person-form-page"
        onSubmit={(event) => void onSubmit(event)}
      >
        {onSwitchToExits ? (
          <Notice tone="info" testId="people-add-local-hint">
            {t("people.formLedeWalkIn")}{" "}
            <button
              type="button"
              className="inline-flex items-center gap-1 align-baseline font-bold text-primary"
              data-testid="people-add-switch-exits"
              disabled={saving}
              onClick={onSwitchToExits}
            >
              <IdCard className="size-4 shrink-0" aria-hidden="true" />
              {t("people.addExits")}
            </button>
          </Notice>
        ) : null}

        {error ? (
          <Notice tone="danger" testId="people-add-error">
            {error}
          </Notice>
        ) : null}

        <div className="people-add-local-sheet__fields flex min-w-0 flex-col gap-3">
          <ExitsOrgEhgInput
            label={t("people.displayName")}
            id="person-display-name"
            name="personDisplayName"
            testId="person-display-name"
            autoComplete="name"
            value={displayName}
            disabled={saving}
            required
            onChange={setDisplayName}
          />
          <ExitsOrgEhgInput
            label={t("people.localAdd.phone")}
            id="person-phone"
            name="personPhone"
            testId="person-phone"
            inputMode="tel"
            autoComplete="tel"
            value={phone}
            disabled={saving}
            onChange={setPhone}
          />
          <ExitsOrgEhgInput
            label={t("people.localAdd.email")}
            id="person-email"
            name="personEmail"
            testId="person-email"
            type="email"
            inputMode="email"
            autoComplete="email"
            value={email}
            disabled={saving}
            onChange={setEmail}
          />
        </div>

        <div className="people-add-local-sheet__actions flex flex-wrap items-center justify-end gap-2 pt-1">
          <ExitsOrgEhgButton
            type="button"
            intent="danger"
            appearance="flat"
            size="md"
            testId="person-add-cancel"
            disabled={saving}
            onClick={onClose}
          >
            {t("people.add.cancel")}
          </ExitsOrgEhgButton>
          <ExitsOrgEhgButton
            type="submit"
            intent="primary"
            appearance="flat"
            size="md"
            className="min-w-[8rem]"
            testId="person-save"
            disabled={saving}
          >
            {saving ? (
              <>
                <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
                {t("people.saving")}
              </>
            ) : (
              <>
                <UserPlus className="size-4 shrink-0" aria-hidden="true" />
                {t("people.addLocal")}
              </>
            )}
          </ExitsOrgEhgButton>
        </div>
      </form>
    </BottomSheet>
  );
}
