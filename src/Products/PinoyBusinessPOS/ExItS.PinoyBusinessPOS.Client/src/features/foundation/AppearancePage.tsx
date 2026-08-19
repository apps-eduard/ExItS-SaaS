import { LanguageControl } from "@/components/exits/LanguageControl";
import { PageHeader } from "@/components/exits/PageHeader";
import { ThemeControl } from "@/components/exits/ThemeControl";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { useSession } from "@/auth/SessionProvider";
import { useI18n } from "@/i18n/I18nProvider";

export function AppearancePage() {
  const { t } = useI18n();
  const { session, signOut } = useSession();

  return (
    <div className="flex flex-col gap-5">
      <PageHeader title={t("appearance.title")} subtitle={t("appearance.subtitle")} />
      <Card className="flex flex-col gap-6">
        <LanguageControl />
        <ThemeControl />
      </Card>
      {session ? (
        <Card className="flex flex-col gap-3">
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("auth.signedInAs")} {session.displayName}
          </p>
          <Button type="button" variant="outline" onClick={() => void signOut()}>
            {t("auth.signOut")}
          </Button>
        </Card>
      ) : null}
    </div>
  );
}
