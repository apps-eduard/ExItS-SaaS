import { useSession } from "@/auth/SessionProvider";
import { useI18n } from "@/i18n/I18nProvider";

export function HomePage() {
  const { t } = useI18n();
  const { session } = useSession();

  return (
    <section className="flex min-h-[calc(100dvh-7rem)] max-w-lg flex-col justify-center gap-3">
      <h1 className="m-0 text-[length:var(--exits-text-2xl)] font-bold tracking-tight">
        {t("home.title")}
      </h1>
      <p className="m-0 text-[length:var(--exits-text-md)] leading-relaxed text-muted">
        {t("home.tagline")}
      </p>
      {session?.displayName ? (
        <p className="m-0 pt-1 text-[length:var(--exits-text-sm)] text-muted">
          {`${t("home.welcome")} ${session.displayName}`}
        </p>
      ) : null}
    </section>
  );
}
