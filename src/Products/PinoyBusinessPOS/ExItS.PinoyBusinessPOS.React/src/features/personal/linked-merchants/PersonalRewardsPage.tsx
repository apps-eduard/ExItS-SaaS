import { PageHeader } from "@/components/exits/PageHeader";
import { useI18n } from "@/i18n/I18nProvider";
import { personalPageBackNav } from "@/navigation/page-back-nav";

/** MAUI parity placeholder for extended-history unlock entry (P24-WP10). */
export function PersonalRewardsPage() {
  const { t } = useI18n();

  return (
    <div className="personal-page exits-page flex min-w-0 flex-col gap-3" data-testid="personal-rewards-page">
      <PageHeader
        title={t("personal.rewards.title")}
        description={t("personal.rewards.lede")}
        backTo={personalPageBackNav.merchants.to}
        backLabel={t(personalPageBackNav.merchants.labelKey)}
        backTestId="page-header-back-rewards"
      />
      <section className="catalog-form-section exits-animate-panel personal-section gap-2">
        <h2 className="catalog-form-section__title text-muted">{t("personal.rewards.title")}</h2>
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
          {t("personal.rewards.comingSoon")}
        </p>
      </section>
    </div>
  );
}
