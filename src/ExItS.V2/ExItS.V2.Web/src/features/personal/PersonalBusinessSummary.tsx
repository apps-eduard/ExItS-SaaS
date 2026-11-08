import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import type { PersonalBusinessCard } from "@/api/personal-businesses";
import { Button } from "@/components/ui/button";
import { MaterialIcon } from "@/features/personal/MaterialIcon";

export function PersonalBusinessSummary({
  items,
}: {
  items: PersonalBusinessCard[];
}) {
  const { t } = useTranslation();
  if (items.length === 0) return null;

  const owned = items.filter((item) => item.kind === "owned");
  const workplaces = items.filter((item) => item.kind === "workplace");

  return (
    <section
      className="catalog-form-section exits-animate-panel personal-section personal-owned-businesses gap-3"
      aria-label={t("personal.home.yourBusinesses")}
      data-testid="personal-business-summary"
    >
      {owned.length > 0 ? (
        <ul className="personal-owned-businesses__list m-0 grid list-none gap-2 p-0">
          {owned.map((row, index) => (
            <li
              key={`owned-${row.organizationId}`}
              className="personal-home-step-card personal-home-step-card--primary personal-owned-businesses__entry"
              data-testid={`personal-business-owned-${row.organizationId}`}
            >
              <div className="personal-home-step-card__rail" aria-hidden>
                <span className="personal-home-step-card__icon-wrap">
                  <MaterialIcon name="store" className="personal-home-step-card__icon" />
                </span>
                <div className="personal-home-step-card__label">
                  {row.roleDisplay || t("personal.home.ownerRole")}
                </div>
              </div>
              <div className="personal-home-step-card__panel">
                {index === 0 ? (
                  <div className="personal-owned-businesses__head">
                    <h3 className="personal-owned-businesses__title personal-owned-businesses__title--primary">
                      {t("personal.home.yourBusinesses")}
                    </h3>
                    <p className="personal-owned-businesses__lede">
                      {t("personal.home.yourBusinessesLede")}
                    </p>
                  </div>
                ) : null}
                <div className="personal-owned-businesses__copy">
                  <p className="personal-home-step-card__title personal-owned-businesses__name">
                    {row.displayName}
                  </p>
                  {row.planDisplayName || row.subscriptionStatus ? (
                    <p className="personal-owned-businesses__meta">
                      {[row.planDisplayName, row.subscriptionStatus]
                        .filter(Boolean)
                        .join(" · ")}
                    </p>
                  ) : null}
                </div>
                <div className="personal-owned-businesses__actions personal-owned-businesses__actions--owned">
                  <Button
                    type="button"
                    variant="outline"
                    className="personal-owned-businesses__open personal-owned-businesses__login personal-owned-businesses__action--glass"
                    data-testid={`personal-business-open-${row.organizationId}`}
                    onClick={() =>
                      toast.message(t("home.comingSoon"), {
                        description: t("homeData.quickActionUnavailable"),
                      })
                    }
                  >
                    {t("personal.home.openBusiness")}
                  </Button>
                </div>
              </div>
            </li>
          ))}
        </ul>
      ) : null}

      {workplaces.length > 0 ? (
        <ul className="personal-owned-businesses__list m-0 grid list-none gap-2 p-0">
          {workplaces.map((row, index) => (
            <li
              key={`work-${row.organizationId}`}
              className="personal-home-step-card personal-home-step-card--secondary personal-owned-businesses__entry"
              data-testid={`personal-workplace-${row.organizationId}`}
            >
              <div className="personal-home-step-card__rail" aria-hidden>
                <span className="personal-home-step-card__icon-wrap">
                  <MaterialIcon name="business_center" className="personal-home-step-card__icon" />
                </span>
                <div className="personal-home-step-card__label">
                  {row.roleDisplay || t("personal.home.staffRole")}
                </div>
              </div>
              <div className="personal-home-step-card__panel">
                {index === 0 ? (
                  <div className="personal-owned-businesses__head">
                    <h3 className="personal-owned-businesses__title">
                      {t("personal.home.yourWorkplace")}
                    </h3>
                    <p className="personal-owned-businesses__lede">
                      {t("personal.home.yourWorkplaceLede")}
                    </p>
                  </div>
                ) : null}
                <div className="personal-owned-businesses__copy">
                  <p className="personal-home-step-card__title personal-owned-businesses__name">
                    {row.displayName}
                  </p>
                </div>
                <div className="personal-owned-businesses__actions">
                  <Button
                    type="button"
                    variant="outline"
                    className="personal-owned-businesses__login personal-owned-businesses__action--glass"
                    data-testid={`personal-workplace-login-${row.organizationId}`}
                    onClick={() =>
                      toast.message(t("home.comingSoon"), {
                        description: t("homeData.quickActionUnavailable"),
                      })
                    }
                  >
                    {t("personal.home.staffLogin")}
                  </Button>
                </div>
              </div>
            </li>
          ))}
        </ul>
      ) : null}
    </section>
  );
}
