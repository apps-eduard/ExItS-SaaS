import type { ReactNode } from "react";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";

type AuthTab = "sign-in" | "sign-up";

type AuthExperienceLayoutProps = {
  activeTab: AuthTab;
  onTabChange: (tab: AuthTab) => void;
  children: ReactNode;
  belowCard?: ReactNode;
  offlineBanner?: ReactNode;
};

export function AuthExperienceLayout({
  activeTab,
  onTabChange,
  children,
  belowCard,
  offlineBanner,
}: AuthExperienceLayoutProps) {
  const { t } = useI18n();

  return (
    <div
      className="auth-experience relative mx-auto flex min-h-[100dvh] w-full min-w-0 flex-col overflow-x-hidden bg-[var(--exits-bg)]"
      data-testid="auth-experience"
    >
      <div
        className="auth-experience__hero relative shrink-0 bg-primary px-[max(var(--exits-page-padding),env(safe-area-inset-left))] pr-[max(var(--exits-page-padding),env(safe-area-inset-right))] pt-[max(2.5rem,env(safe-area-inset-top))] pb-24 text-primary-foreground sm:pb-28"
        data-testid="auth-experience-hero"
      >
        <div className="mx-auto flex w-full max-w-[28rem] flex-col gap-1 text-center sm:max-w-[32rem] lg:max-w-[36rem]">
          <p className="m-0 text-[length:var(--exits-text-xs)] font-semibold tracking-[0.18em] uppercase opacity-90">
            {t("auth.brandLine")}
          </p>
          <h1 className="m-0 text-[length:var(--exits-text-2xl)] font-bold tracking-tight sm:text-[length:var(--exits-text-3xl)]">
            {t("auth.productLine")}
          </h1>
        </div>
      </div>

      <div className="auth-experience__sheet-wrap relative z-[1] -mt-16 flex flex-1 flex-col px-[max(var(--exits-page-padding),env(safe-area-inset-left))] pr-[max(var(--exits-page-padding),env(safe-area-inset-right))] pb-[max(2rem,env(safe-area-inset-bottom))]">
        <div
          className="auth-experience__sheet mx-auto flex w-full max-w-[28rem] min-w-0 flex-col gap-5 rounded-[1.25rem] border border-border bg-surface p-5 shadow-[0_18px_40px_rgba(20,32,26,0.12)] sm:max-w-[32rem] sm:p-6 lg:max-w-[36rem]"
          data-testid="auth-experience-sheet"
        >
          <div
            className="grid grid-cols-2 gap-1 rounded-[var(--exits-radius-lg)] bg-[var(--exits-surface-muted)] p-1"
            role="tablist"
            aria-label={t("auth.tabsLabel")}
          >
            <button
              type="button"
              role="tab"
              aria-selected={activeTab === "sign-in"}
              data-testid="auth-tab-sign-in"
              className={cn(
                "min-h-11 rounded-[var(--exits-radius-md)] text-[length:var(--exits-text-sm)] font-semibold transition-colors",
                activeTab === "sign-in"
                  ? "bg-surface text-foreground shadow-sm"
                  : "text-muted hover:text-foreground",
              )}
              onClick={() => onTabChange("sign-in")}
            >
              {t("auth.tabSignIn")}
            </button>
            <button
              type="button"
              role="tab"
              aria-selected={activeTab === "sign-up"}
              data-testid="auth-tab-sign-up"
              className={cn(
                "min-h-11 rounded-[var(--exits-radius-md)] text-[length:var(--exits-text-sm)] font-semibold transition-colors",
                activeTab === "sign-up"
                  ? "bg-surface text-foreground shadow-sm"
                  : "text-muted hover:text-foreground",
              )}
              onClick={() => onTabChange("sign-up")}
            >
              {t("auth.tabSignUp")}
            </button>
          </div>

          {offlineBanner}

          {children}
        </div>

        {belowCard ? (
          <div className="mx-auto mt-5 w-full max-w-[28rem] min-w-0 sm:max-w-[32rem] lg:max-w-[36rem]">
            {belowCard}
          </div>
        ) : null}
      </div>
    </div>
  );
}

export function AuthOrDivider() {
  const { t } = useI18n();
  return (
    <div className="auth-or-divider flex items-center gap-3" role="separator">
      <span className="h-px flex-1 bg-border" aria-hidden />
      <span className="text-[length:var(--exits-text-xs)] font-medium uppercase tracking-wide text-muted">
        {t("auth.orDivider")}
      </span>
      <span className="h-px flex-1 bg-border" aria-hidden />
    </div>
  );
}
