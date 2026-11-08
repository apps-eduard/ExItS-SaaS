import { Compass, X } from "lucide-react";
import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { dismissGuideHomeCard } from "@/api/personal-guide";
import { Button } from "@/components/ui/button";

export function PersonalGuideHomeCard({
  userId,
  onDismissed,
}: {
  userId?: string;
  onDismissed: () => void;
}) {
  const { t } = useTranslation();

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
          onClick={() => {
            dismissGuideHomeCard(userId);
            onDismissed();
          }}
        >
          <X className="size-4" aria-hidden />
        </Button>
      </div>
      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
        {t("personal.home.guideCardLede")}
      </p>
      <Button
        type="button"
        className="w-full sm:w-auto"
        data-testid="personal-guide-home-continue"
        onClick={() =>
          toast.message(t("home.comingSoon"), {
            description: t("homeData.quickActionUnavailable"),
          })
        }
      >
        {t("personal.home.guideCardContinue")}
      </Button>
    </section>
  );
}
