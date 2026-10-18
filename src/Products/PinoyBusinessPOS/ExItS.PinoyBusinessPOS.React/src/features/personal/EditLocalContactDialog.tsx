import { useState } from "react";
import { Pencil } from "lucide-react";
import { PlatformApiError } from "@/api/platform/platform-http";
import { ExitsModal } from "@/components/exits/ExitsModal";
import { ExitsOutlineInput } from "@/components/exits/ExitsOutlineField";
import { Button } from "@/components/ui/button";
import { useUpdatePersonalContactMutation } from "@/features/personal/people-queries";
import { useI18n } from "@/i18n/I18nProvider";

function SavePenIcon({ className }: { className?: string }) {
  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
      aria-hidden="true"
      data-testid="utang-edit-contact-save-icon"
    >
      <path d="M13.33 13H8a1 1 0 0 0-1 1v7" />
      <path d="M14.363 17.634a2 2 0 0 0-.506.854l-.837 2.87a.5.5 0 0 0 .62.62l2.87-.837a2 2 0 0 0 .854-.506l4.013-4.009a1 1 0 1 0-3.004-3.004z" />
      <path d="M7 3v4a1 1 0 0 0 1 1h7" />
      <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h10.2a2 2 0 0 1 1.4.6l3.8 3.8a2 2 0 0 1 .6 1.4v.3" />
    </svg>
  );
}

export function EditLocalContactDialog({
  open,
  contact,
  onOpenChange,
}: {
  open: boolean;
  contact: { id: string; displayName: string; phone?: string | null; email?: string | null };
  onOpenChange: (open: boolean) => void;
}) {
  const { t } = useI18n();
  const updateContact = useUpdatePersonalContactMutation();
  const [name, setName] = useState(contact.displayName);
  const [phone, setPhone] = useState(contact.phone ?? "");
  const [email, setEmail] = useState(contact.email ?? "");
  const [error, setError] = useState<string | null>(null);

  return (
    <ExitsModal
      open={open}
      onOpenChange={onOpenChange}
      title={t("personal.utang.qaEditContact")}
      testId="utang-edit-contact-dialog"
      closeLabel={t("personal.utang.closeRecordDialog")}
      busy={updateContact.isPending}
      ribbon
      ribbonIcon={Pencil}
      onSubmit={() => {
        if (!name.trim()) {
          setError(t("people.localAdd.nameRequired"));
          return;
        }
        setError(null);
        updateContact.mutate(
          {
            contactId: contact.id,
            body: {
              displayName: name.trim(),
              phone: phone.trim() || null,
              email: email.trim() || null,
            },
          },
          {
            onSuccess: () => onOpenChange(false),
            onError: (err) =>
              setError(err instanceof PlatformApiError ? err.message : t("people.loadError")),
          },
        );
      }}
      footer={
        <>
          <Button
            type="button"
            intent="danger"
            appearance="gradient"
            emphasis="strong"
            disabled={updateContact.isPending}
            data-testid="utang-edit-contact-cancel"
            onClick={() => onOpenChange(false)}
          >
            {t("people.add.cancel")}
          </Button>
          <Button
            type="submit"
            intent="primary"
            appearance="glossy"
            disabled={updateContact.isPending}
            data-testid="utang-edit-contact-save"
          >
            <SavePenIcon className="size-4 shrink-0" />
            {t("people.edit.save")}
          </Button>
        </>
      }
    >
      <div className="flex min-w-0 flex-col gap-3">
        <ExitsOutlineInput
          id="utang-edit-contact-name"
          label={t("people.displayName")}
          value={name}
          onChange={setName}
          testId="utang-edit-contact-name"
          data-exits-modal-autofocus="true"
          required
          autoComplete="name"
        />
        <ExitsOutlineInput
          id="utang-edit-contact-phone"
          label={t("people.column.contact")}
          value={phone}
          onChange={setPhone}
          testId="utang-edit-contact-phone"
          autoComplete="tel"
        />
        <ExitsOutlineInput
          id="utang-edit-contact-email"
          label={t("people.column.email")}
          value={email}
          onChange={setEmail}
          testId="utang-edit-contact-email"
          type="email"
          autoComplete="email"
        />
        {error ? (
          <p role="alert" className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]">
            {error}
          </p>
        ) : null}
      </div>
    </ExitsModal>
  );
}
