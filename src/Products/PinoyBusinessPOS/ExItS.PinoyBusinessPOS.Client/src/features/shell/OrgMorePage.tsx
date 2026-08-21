import { Link } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { PageHeader } from "@/components/exits/PageHeader";
import { buildOrgMoreLinks } from "@/features/shell/org-nav-config";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function OrgMorePage() {
  const { t } = useI18n();
  const { sessionGrant } = useWorkspace();
  const links = buildOrgMoreLinks(sessionGrant);

  return (
    <div className="flex min-w-0 flex-col gap-4" data-testid="org-more-page">
      <PageHeader title={t("org.more.title")} description={t("org.more.lede")} />
      <div className="flex flex-col gap-2">
        {links.map((link, index) => (
          <Button
            key={link.to}
            asChild
            variant={index === 0 ? "default" : "ghost"}
            className="min-h-11 justify-start"
            data-testid={link.testId}
          >
            <Link to={link.to}>{t(link.labelKey)}</Link>
          </Button>
        ))}
      </div>
    </div>
  );
}
