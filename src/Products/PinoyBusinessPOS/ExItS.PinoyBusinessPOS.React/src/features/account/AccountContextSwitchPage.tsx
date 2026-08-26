import { AccountContextSwitchScreen } from "@/features/account/AccountContextSwitchScreen";
import { useI18n } from "@/i18n/I18nProvider";

/** Neutral holding screen while account profile / workspace context changes. */
export function AccountContextSwitchPage() {
  const { t } = useI18n();
  return <AccountContextSwitchScreen label={t("accountClass.switchingLabel")} />;
}
