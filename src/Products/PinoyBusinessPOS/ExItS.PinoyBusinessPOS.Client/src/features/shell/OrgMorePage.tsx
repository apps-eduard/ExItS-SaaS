import { ActionTileGrid } from "@/components/exits/ActionTileGrid";
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
      <ActionTileGrid
        tiles={links.map((link) => ({
          key: link.testId,
          label: t(link.labelKey),
          icon: link.icon,
          testId: link.testId,
          to: link.to,
        }))}
      />
    </div>
  );
}
