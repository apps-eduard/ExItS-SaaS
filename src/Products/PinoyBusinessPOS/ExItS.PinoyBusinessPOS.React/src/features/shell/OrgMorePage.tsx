import { useQuery } from "@tanstack/react-query";
import { getOnboardingProgress } from "@/api/pos/pos-onboarding-client";
import { PosApiError } from "@/api/pos/pos-http";
import { ActionTileGrid } from "@/components/exits/ActionTileGrid";
import { PageHeader } from "@/components/exits/PageHeader";
import { shouldShowFinishSetupEntry } from "@/features/onboarding/onboarding-steps";
import { buildOrgMoreSections } from "@/features/shell/org-nav-config";
import { useI18n } from "@/i18n/I18nProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function OrgMorePage() {
  const { t } = useI18n();
  const { sessionGrant, boundWorkspace } = useWorkspace();
  const organizationId = boundWorkspace?.organizationId ?? null;

  const progressQuery = useQuery({
    queryKey: ["pos", "onboarding", "progress", organizationId, "more-entry"],
    enabled: Boolean(organizationId && sessionGrant?.accessToken),
    staleTime: 60_000,
    meta: { suppressGlobalError: true, operation: "onboarding progress (more)" },
    queryFn: async ({ signal }) => {
      try {
        return await getOnboardingProgress(
          {
            organizationId: organizationId!,
            branchId: boundWorkspace?.branchId ?? null,
          },
          signal,
        );
      } catch (error) {
        if (error instanceof PosApiError && error.status === 404) {
          return null;
        }
        throw error;
      }
    },
    retry: false,
  });

  const sections = buildOrgMoreSections(sessionGrant, {
    showFinishSetup:
      boundWorkspace?.experience === "manage_business"
        ? shouldShowFinishSetupEntry(progressQuery.data)
        : false,
    branchType: boundWorkspace?.branchType,
    // Manager More must not expose Admin configuration destinations.
    excludeAdminDestinations: boundWorkspace?.experience !== "manage_business",
  });

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
