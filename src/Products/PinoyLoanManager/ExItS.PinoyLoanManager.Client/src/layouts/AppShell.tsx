import { Outlet } from "react-router-dom";
import { AppTopBar } from "@/components/exits/AppTopBar";
import { useI18n } from "@/i18n/I18nProvider";
import { PwaUpdateHost } from "@/pwa/PwaUpdateHost";

export function AppShell() {
  const { t } = useI18n();

  return (
    <div className="flex min-h-dvh flex-col bg-background">
      <a
        href="#main"
        className="sr-only focus:not-sr-only focus:absolute focus:left-3 focus:top-3 focus:z-50 focus:rounded-md focus:bg-surface focus:px-3 focus:py-2"
      >
        {t("app.skipToContent")}
      </a>
      <AppTopBar />
      <main id="main" className="mx-auto w-full max-w-5xl flex-1 px-4 py-4">
        <Outlet />
      </main>
      <PwaUpdateHost />
    </div>
  );
}
