import type { ReactNode } from "react";
import { useI18n } from "@/i18n/I18nProvider";

export function AppShell({ children, header }: { children: ReactNode; header?: ReactNode }) {
  const { t } = useI18n();

  return (
    <div className="mx-auto flex min-h-[100dvh] w-full max-w-5xl min-w-0 flex-col overflow-x-hidden px-[max(var(--exits-page-padding),env(safe-area-inset-left))] pr-[max(var(--exits-page-padding),env(safe-area-inset-right))] pt-[env(safe-area-inset-top)] pb-[max(2rem,env(safe-area-inset-bottom))]">
      <a
        href="#main-content"
        className="sr-only z-50 rounded-[var(--exits-radius-md)] bg-primary px-3 py-2 text-primary-foreground"
      >
        {t("app.skipToContent")}
      </a>
      {header}
      <main id="main-content" className="flex min-w-0 flex-1 flex-col gap-4 pt-6" tabIndex={-1}>
        {children}
      </main>
    </div>
  );
}
