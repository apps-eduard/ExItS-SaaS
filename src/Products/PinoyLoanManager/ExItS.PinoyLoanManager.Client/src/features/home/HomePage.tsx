import { LanguageControl } from "@/components/exits/LanguageControl";
import { PageHeader } from "@/components/exits/PageHeader";
import { ThemeControl } from "@/components/exits/ThemeControl";
import { Card } from "@/components/ui/card";
import { useProductAccess } from "@/access/ProductAccessProvider";
import { useI18n } from "@/i18n/I18nProvider";

export function HomePage() {
  const { t } = useI18n();
  const { selectedOrganization } = useProductAccess();

  return (
    <section className="flex max-w-md flex-col gap-6 pt-6">
      <PageHeader title={t("home.title")} description={t("home.workspaceReady")} />
      {selectedOrganization ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
          {selectedOrganization.displayName}
        </p>
      ) : null}
      <Card className="flex flex-col gap-4">
        <LanguageControl />
        <ThemeControl />
      </Card>
    </section>
  );
}
