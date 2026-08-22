import type { ReactNode } from "react";
import { UnderlineTabBar } from "@/components/exits/UnderlineTabBar";
import { useI18n } from "@/i18n/I18nProvider";

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
        className="auth-experience__hero relative shrink-0 overflow-hidden bg-primary px-[max(var(--exits-page-padding),env(safe-area-inset-left))] pr-[max(var(--exits-page-padding),env(safe-area-inset-right))] pt-[max(2.75rem,env(safe-area-inset-top))] pb-[5.5rem] text-primary-foreground sm:pb-[6.25rem]"
        data-testid="auth-experience-hero"
      >
        <div className="auth-experience__hero-shapes pointer-events-none absolute inset-0 overflow-hidden" aria-hidden>
          <span className="auth-experience__hero-shape auth-experience__hero-shape--one" />
          <span className="auth-experience__hero-shape auth-experience__hero-shape--two" />
          <span className="auth-experience__hero-shape auth-experience__hero-shape--three" />
        </div>
        <div className="relative mx-auto flex w-full max-w-[28rem] flex-col gap-1 text-center sm:max-w-[32rem] lg:max-w-[36rem]">
          <p className="m-0 text-[length:var(--exits-text-sm)] font-bold tracking-[0.14em] uppercase sm:text-[length:var(--exits-text-md)]">
            {t("auth.brandLine")}
          </p>
          <h1 className="m-0 text-[length:var(--exits-text-xl)] font-semibold tracking-tight sm:text-[length:var(--exits-text-2xl)]">
            {t("auth.productLine")}
          </h1>
        </div>
      </div>

      <div className="auth-experience__sheet-wrap relative z-[1] -mt-[4.75rem] flex flex-1 flex-col overflow-y-auto px-[max(var(--exits-page-padding),env(safe-area-inset-left))] pr-[max(var(--exits-page-padding),env(safe-area-inset-right))] pb-[max(2rem,env(safe-area-inset-bottom))] sm:-mt-[5.25rem]">
        <div
          className="auth-experience__sheet mx-auto flex w-full max-w-[min(100%,28rem)] min-w-0 flex-col gap-5 rounded-[1.875rem] bg-surface p-5 shadow-[0_20px_48px_rgba(20,32,26,0.14)] sm:max-w-[min(100%,30rem)] sm:p-6 md:max-w-[min(100%,32rem)] lg:max-w-[min(100%,32rem)]"
          data-testid="auth-experience-sheet"
        >
          <UnderlineTabBar
            className="auth-experience__tabs grid w-full grid-cols-2 [&>button]:justify-center [&>button]:text-[length:var(--exits-text-md)]"
            items={[
              { key: "sign-in", label: t("auth.tabSignIn"), testId: "auth-tab-sign-in" },
              { key: "sign-up", label: t("auth.tabSignUp"), testId: "auth-tab-sign-up" },
            ]}
            activeKey={activeTab}
            onChange={(key) => onTabChange(key as AuthTab)}
            ariaLabel={t("auth.tabsLabel")}
          />

          {offlineBanner}

          {children}
        </div>

        {belowCard ? (
          <div className="mx-auto mt-5 w-full max-w-[min(100%,28rem)] min-w-0 sm:max-w-[min(100%,30rem)] md:max-w-[min(100%,32rem)]">
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
      <span className="text-[length:var(--exits-text-sm)] font-normal lowercase text-muted">
        {t("auth.orDivider")}
      </span>
      <span className="h-px flex-1 bg-border" aria-hidden />
    </div>
  );
}
