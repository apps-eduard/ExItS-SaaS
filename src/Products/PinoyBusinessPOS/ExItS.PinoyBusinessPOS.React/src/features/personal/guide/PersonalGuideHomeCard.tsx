import { Compass, X } from "lucide-react";
import { Link } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { PERSONAL_GUIDE_ROUTE } from "@/features/personal/guide/personal-guide-features";
import { usePersonalGuideProgress } from "@/features/personal/guide/use-personal-guide-progress";
import { useI18n } from "@/i18n/I18nProvider";
import { useSession } from "@/session/SessionProvider";

export function PersonalGuideHomeCard() {
  const { t } = useI18n();
  const { session } = useSession();
  const accountKey = session?.userId?.trim() || null;
  const guide = usePersonalGuideProgress(accountKey);

  if (guide.homeCardDismissed) {
    return null;
  }

  return (
    <section
      className="catalog-form-section exits-animate-panel personal-section gap-2"
      data-testid="personal-guide-home-card"
      aria-label={t("personal.home.guideCardTitle")}
    >
      <div className="flex items-start justify-between gap-2">
        <h2 className="catalog-form-section__title personal-todo-create-form__title m-0 text-muted">
          <Compass
            className="personal-todo-create-form__title-icon size-[1.1rem] shrink-0"
            aria-hidden
          />
          {t("personal.home.guideCardTitle")}
        </h2>
        <Button
          type="button"
          variant="ghost"
          size="icon"
          className="shrink-0"
          data-testid="personal-guide-home-dismiss"
          aria-label={t("personal.home.guideCardDismiss")}
          onClick={() => guide.setHomeCardDismissed(true)}
        >
          <X className="size-4" aria-hidden />
        </Button>
      </div>
      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
        {t("personal.home.guideCardLede")}
      </p>
      <p className="m-0 text-[length:var(--exits-text-sm)] font-medium" data-testid="personal-guide-home-progress">
        {t("personal.guide.progress")
          .replace("{explored}", String(guide.explored))
          .replace("{total}", String(guide.total))}
      </p>
      <Button asChild className="min-h-11 w-full sm:w-auto" data-testid="personal-guide-home-continue">
        <Link to={PERSONAL_GUIDE_ROUTE}>{t("personal.home.guideCardContinue")}</Link>
      </Button>
    </section>
  );
}
