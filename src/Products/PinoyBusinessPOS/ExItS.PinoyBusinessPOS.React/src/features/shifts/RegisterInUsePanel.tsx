import { Link } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { useI18n } from "@/i18n/I18nProvider";

type RegisterInUsePanelProps = {
  registerCode: string;
  registerName: string;
  openedByDisplayName: string | null;
  chooseRegisterHref?: string;
  onChooseRegister?: () => void;
  testId?: string;
};

/**
 * Blocked sell/open-shift state when the selected register already has another actor's Open shift.
 */
export function RegisterInUsePanel({
  registerCode,
  registerName,
  openedByDisplayName,
  chooseRegisterHref = "/registers",
  onChooseRegister,
  testId = "register-in-use-panel",
}: RegisterInUsePanelProps) {
  const { t } = useI18n();
  const registerLabel = `${registerCode} — ${registerName}`;
  const opener = openedByDisplayName?.trim() || t("shift.registerInUseUnknownOpener");

  return (
    <Card className="flex flex-col gap-3 p-4" data-testid={testId}>
      <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
        {t("shift.registerInUseTitle")}
      </h2>
      <p
        className="m-0 text-[length:var(--exits-text-sm)] text-muted"
        data-testid={`${testId}-detail`}
      >
        {t("shift.registerInUseDetail")
          .replace("{register}", registerLabel)
          .replace("{name}", opener)}
      </p>
      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
        {t("shift.registerInUseHelp")}
      </p>
      {onChooseRegister ? (
        <Button
          type="button"
          className="min-h-11"
          data-testid={`${testId}-choose-register`}
          onClick={onChooseRegister}
        >
          {t("shift.chooseAnotherRegister")}
        </Button>
      ) : (
        <Button asChild className="min-h-11" data-testid={`${testId}-choose-register`}>
          <Link to={chooseRegisterHref}>{t("shift.chooseAnotherRegister")}</Link>
        </Button>
      )}
    </Card>
  );
}
