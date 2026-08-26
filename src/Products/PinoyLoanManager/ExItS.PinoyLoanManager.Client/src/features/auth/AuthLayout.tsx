import type { ReactNode } from "react";
import { Link } from "react-router-dom";
import { ExItsMark } from "@/components/exits/ExItsMark";
import { LanguageControl } from "@/components/exits/LanguageControl";
import { ThemeControl } from "@/components/exits/ThemeControl";
import { useI18n } from "@/i18n/I18nProvider";

export function AuthLayout({
  title,
  children,
  banner,
  showAccountLinks = false,
}: {
  title: string;
  children: ReactNode;
  banner?: ReactNode;
  showAccountLinks?: boolean;
}) {
  const { t } = useI18n();
  return (
    <div className="auth-stage flex min-h-dvh flex-col md:items-center md:justify-center md:px-4 md:py-10">
      <div className="flex flex-col px-5 pb-6 pt-[max(2.5rem,env(safe-area-inset-top))] text-white md:hidden">
        <ExItsMark size="lg" className="bg-white text-[#166534]" />
        <p className="mb-0 mt-4 text-[length:var(--exits-text-sm)] font-semibold">
          {t("app.name")}
        </p>
      </div>
      <section className="auth-sheet flex min-h-0 flex-1 flex-col px-5 pb-[max(1.5rem,env(safe-area-inset-bottom))] pt-6 md:h-auto md:w-[420px] md:flex-none md:px-7 md:py-8">
        <div className="mb-6 hidden items-center gap-3 md:flex">
          <ExItsMark size="md" />
          <p className="m-0 font-semibold text-foreground">{t("app.name")}</p>
        </div>
        {showAccountLinks ? (
          <nav
            className="mb-4 flex gap-4 text-[length:var(--exits-text-sm)]"
            aria-label={t("auth.accountNav")}
          >
            <Link className="text-muted underline-offset-4 hover:underline" to="/sign-in">
              {t("auth.signIn")}
            </Link>
            <Link className="text-muted underline-offset-4 hover:underline" to="/sign-up">
              {t("auth.createAccount")}
            </Link>
          </nav>
        ) : null}
        <h1 className="m-0 text-[length:var(--exits-text-xl)] font-bold tracking-tight">{title}</h1>
        {banner}
        {children}
        <div className="mt-8 flex flex-col gap-4">
          <LanguageControl />
          <ThemeControl />
        </div>
      </section>
    </div>
  );
}
