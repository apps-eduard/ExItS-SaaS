import { ChevronLeft } from "lucide-react";
import { Link } from "react-router-dom";
import { LanguageControl } from "@/components/exits/LanguageControl";
import { ThemeControl } from "@/components/exits/ThemeControl";
import { Button } from "@/components/ui/button";
import { useSession } from "@/auth/SessionProvider";
import { useI18n } from "@/i18n/I18nProvider";

export function AppearancePage() {
  const { t } = useI18n();
  const { session, signOut } = useSession();

  return (
    <div className="mx-auto flex w-full max-w-md flex-col gap-5">
      <div className="flex flex-col gap-3">
        <Button variant="ghost" size="default" className="-ml-2 w-fit px-2" asChild>
          <Link to="/">
            <ChevronLeft className="size-5" aria-hidden="true" />
            {t("shell.back")}
          </Link>
        </Button>
        <h1 className="m-0 text-[length:var(--exits-text-xl)] font-bold tracking-tight">
          {t("appearance.title")}
        </h1>
      </div>

      <div className="flex flex-col gap-5 rounded-[var(--exits-radius-lg)] border border-border bg-surface px-4 py-4 shadow-sm">
        <LanguageControl />
        <ThemeControl />
      </div>

      {session ? (
        <Button type="button" variant="outline" onClick={() => void signOut()}>
          {t("auth.signOut")}
        </Button>
      ) : null}
    </div>
  );
}
