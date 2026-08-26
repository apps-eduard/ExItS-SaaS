import { Link } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { PageHeader } from "@/components/exits/PageHeader";
import { usePreferences } from "@/hooks/use-preferences";

export function UnderDevelopmentPage() {
  const { t } = usePreferences();

  return (
    <section>
      <PageHeader
        title={t("underDevelopment.title")}
        description={t("underDevelopment.body")}
        actions={
          <Button asChild>
            <Link to="/admin">{t("underDevelopment.back")}</Link>
          </Button>
        }
      />
    </section>
  );
}
