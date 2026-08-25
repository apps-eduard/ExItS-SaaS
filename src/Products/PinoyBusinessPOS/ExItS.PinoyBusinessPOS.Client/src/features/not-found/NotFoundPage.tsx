import { Link } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { PageHeader } from "@/components/exits/PageHeader";
import { useI18n } from "@/i18n/I18nProvider";

export function NotFoundPage() {
  const { t } = useI18n();

  return (
    <div className="flex min-w-0 flex-col gap-4">
      <PageHeader title={t("notFound.title")} description={t("notFound.detail")} />
      <Button asChild>
        <Link to="/">{t("notFound.home")}</Link>
      </Button>
    </div>
  );
}
