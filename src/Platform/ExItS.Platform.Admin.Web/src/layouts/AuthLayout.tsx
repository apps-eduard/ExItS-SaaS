import type { ReactNode } from "react";
import { PreferencesMenu } from "@/components/exits/PreferencesMenu";
import { usePreferences } from "@/hooks/use-preferences";

export function AuthLayout({ children }: { children: ReactNode }) {
  const { t } = usePreferences();

  return (
    <div className="min-h-dvh overflow-x-hidden bg-background lg:grid lg:grid-cols-[minmax(16rem,32%)_minmax(0,1fr)]">
      <aside className="relative hidden bg-primary px-8 py-10 text-primary-foreground lg:flex lg:flex-col lg:justify-center">
        <p className="text-[length:var(--exits-text-xs)] font-semibold tracking-[0.16em] uppercase">
          ExItS
        </p>
        <p className="mt-3 text-[length:var(--exits-text-xl)] font-semibold leading-tight">
          {t("auth.product")}
        </p>
        <p className="mt-2 max-w-xs text-[length:var(--exits-text-sm)] text-primary-foreground/80">
          {t("auth.productSubtitle")}
        </p>
      </aside>

      <div className="flex min-h-dvh flex-col">
        <header className="flex h-12 items-center justify-between gap-2 px-4">
          <div className="min-w-0 lg:hidden">
            <p className="text-[length:var(--exits-text-xs)] font-semibold tracking-[0.16em] text-primary uppercase">
              ExItS
            </p>
            <p className="truncate text-[length:var(--exits-text-sm)] text-muted">
              {t("auth.product")}
            </p>
          </div>
          <div className="ml-auto">
            <PreferencesMenu />
          </div>
        </header>

        <div className="flex flex-1 justify-center px-4 pb-8 sm:items-center sm:px-6">
          <div className="w-full max-w-[26.25rem]">{children}</div>
        </div>
      </div>
    </div>
  );
}
