import { Alert } from "@/components/ui/alert";
import { usePreferences } from "@/hooks/use-preferences";

export function PrivacyReadinessBanner() {
  const { t } = usePreferences();
  return (
    <Alert title={t("privacy.readinessBanner.title")} tone="info" data-testid="privacy-readiness-banner">
      {t("privacy.readinessBanner.message")} {t("privacy.noCertificationClaim")}
    </Alert>
  );
}

export function PrivacyDisclaimer() {
  const { t } = usePreferences();
  return (
    <Alert title={t("privacy.disclaimer.title")} tone="danger" data-testid="privacy-disclaimer">
      {t("privacy.disclaimer.message")}
    </Alert>
  );
}
