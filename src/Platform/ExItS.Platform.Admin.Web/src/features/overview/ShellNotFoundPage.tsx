import { PageHeader } from "@/components/exits/PageHeader";
import { usePreferences } from "@/hooks/use-preferences";

export function ShellNotFoundPage() {
  const { t } = usePreferences();

  return (
    <section>
      <PageHeader title={t("shell.notFound.title")} description={t("shell.notFound.body")} />
    </section>
  );
}
