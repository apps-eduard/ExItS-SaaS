import { IdCard, UserPlus } from "lucide-react";
import { ExitsModal } from "@/components/exits/ExitsModal";
import { PersonCreateForm, type PersonCreateKind } from "@/features/personal/PersonFormPage";
import { useI18n } from "@/i18n/I18nProvider";

export function AddPersonDialog({
  open,
  initialKind = null,
  linkPublicId = null,
  onOpenChange,
  onSwitchToExits,
  onSwitchToLocal,
}: {
  open: boolean;
  initialKind?: PersonCreateKind | null;
  linkPublicId?: string | null;
  onOpenChange: (open: boolean) => void;
  onSwitchToExits?: () => void;
  onSwitchToLocal?: () => void;
}) {
  const { t } = useI18n();
  const title =
    initialKind === "exits"
      ? t("people.addExits")
      : initialKind === "walkin"
        ? t("people.addLocal")
        : t("people.newTitle");

  return (
    <ExitsModal
      open={open}
      onOpenChange={onOpenChange}
      title={title}
      testId="people-add-dialog"
      id="people-add-dialog"
      closeLabel={t("personal.utang.closeRecordDialog")}
      closeOnOutsideClick={false}
      size="lg"
      fullHeightOnCompact
      ribbon
      ribbonIcon={initialKind === "exits" ? IdCard : UserPlus}
    >
      <PersonCreateForm
        key={`${initialKind ?? "pick"}-${linkPublicId ?? ""}`}
        embedded
        lockKind={initialKind !== null}
        initialKind={initialKind}
        linkPublicId={linkPublicId}
        onCancel={() => onOpenChange(false)}
        onSwitchToExits={onSwitchToExits}
        onSwitchToLocal={onSwitchToLocal}
      />
    </ExitsModal>
  );
}
