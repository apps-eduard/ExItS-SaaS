import { ActionTileGrid } from "@/components/exits/ActionTileGrid";
import { PageHeader } from "@/components/exits/PageHeader";
import { buildOrgMoreSections } from "@/features/shell/org-nav-config";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function OrgMorePage() {
  const { t } = useI18n();
  const { sessionGrant } = useWorkspace();
  const sections = buildOrgMoreSections(sessionGrant);

  return (
    <div
      className="org-more-page exits-page mx-auto flex w-full max-w-2xl min-w-0 flex-col gap-3"
      data-testid="org-more-page"
    >
      <PageHeader title={t("org.more.title")} description={t("org.more.lede")} />

      {sections.map((section) => (
        <section
          key={section.id}
          className="catalog-form-section exits-animate-panel org-more-section gap-3"
          data-testid={section.testId}
        >
          <h2 className="catalog-form-section__title text-muted">{t(section.titleKey)}</h2>
          <ActionTileGrid
            tiles={section.links.map((link) => ({
              key: link.testId,
              label: t(link.labelKey),
              icon: link.icon,
              testId: link.testId,
              to: link.to,
            }))}
          />
        </section>
      ))}
    </div>
  );
}
