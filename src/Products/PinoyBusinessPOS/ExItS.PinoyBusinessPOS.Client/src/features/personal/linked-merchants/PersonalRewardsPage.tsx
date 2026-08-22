import { PageHeader } from "@/components/exits/PageHeader";
import { Card } from "@/components/ui/card";
import { useI18n } from "@/i18n/I18nProvider";

/** MAUI parity placeholder for extended-history unlock entry (P24-WP10). */
export function PersonalRewardsPage() {
  const { t } = useI18n();

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="personal-rewards-page">
      <PageHeader title={t("personal.rewards.title")} description={t("personal.rewards.lede")} />
      <Card>
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
          {t("personal.rewards.comingSoon")}
        </p>
      </Card>
    </div>
  );
}
