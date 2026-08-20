import type { ReactNode } from "react";
import { LanguageControl } from "@/components/exits/LanguageControl";
import { ThemeControl } from "@/components/exits/ThemeControl";
import { useI18n } from "@/i18n/I18nProvider";

export function AppShell({ children }: { children: ReactNode }) {
  const { t } = useI18n();

  return (
    <div className="mx-auto flex min-h-[100dvh] w-full max-w-5xl min-w-0 flex-col overflow-x-hidden px-[max(var(--exits-page-padding),env(safe-area-inset-left))] pr-[max(var(--exits-page-padding),env(safe-area-inset-right))] pt-[env(safe-area-inset-top)] pb-[max(2rem,env(safe-area-inset-bottom))]">
      <a
        href="#main-content"
        className="sr-only z-50 rounded-[var(--exits-radius-md)] bg-primary px-3 py-2 text-primary-foreground"
      >
        {t("app.skipToContent")}
      </a>
      <header className="flex min-w-0 flex-col gap-4 border-b border-border py-4 sm:flex-row sm:items-start sm:justify-between">
        <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold tracking-wide uppercase text-muted">
          {t("app.name")}
        </p>
        <div className="flex min-w-0 flex-col gap-3 sm:max-w-md sm:items-stretch">
          <LanguageControl />
          <ThemeControl />
        </div>
      </header>
      <main id="main-content" className="flex min-w-0 flex-1 flex-col gap-4 pt-6" tabIndex={-1}>
        {children}
      </main>
    </div>
  );
}
