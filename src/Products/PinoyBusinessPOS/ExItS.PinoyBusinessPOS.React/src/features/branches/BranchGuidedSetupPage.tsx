import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useParams } from "react-router-dom";
import { getBranchReadiness, upsertBranchSetupProgress } from "@/api/pos/branch-readiness-client";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { Button } from "@/components/ui/button";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const SECTION_ORDER = [
  "Details",
  "Staff",
  "Products",
  "Pricing",
  "Inventory",
  "Parties",
  "Fulfillment",
  "Device",
] as const;

type SetupSectionKey = (typeof SECTION_ORDER)[number];

const SECTION_LABEL_KEYS: Record<SetupSectionKey, MessageKey> = {
  Details: "branches.setup.section.Details",
  Staff: "branches.setup.section.Staff",
  Products: "branches.setup.section.Products",
  Pricing: "branches.setup.section.Pricing",
  Inventory: "branches.setup.section.Inventory",
  Parties: "branches.setup.section.Parties",
  Fulfillment: "branches.setup.section.Fulfillment",
  Device: "branches.setup.section.Device",
};

const OVERALL_STATUS_KEYS = {
  NotStarted: "branches.setup.overallStatus.NotStarted",
  NeedsAttention: "branches.setup.overallStatus.NeedsAttention",
  Ready: "branches.setup.overallStatus.Ready",
} as const satisfies Record<string, MessageKey>;

const SECTION_STATUS_KEYS = {
  Complete: "branches.setup.sectionStatus.Complete",
  NeedsAttention: "branches.setup.sectionStatus.NeedsAttention",
  Optional: "branches.setup.sectionStatus.Optional",
  NotApplicable: "branches.setup.sectionStatus.NotApplicable",
} as const satisfies Record<string, MessageKey>;

export function BranchGuidedSetupPage() {
  const { branchId = "" } = useParams();
  const { t } = useI18n();
  const { boundWorkspace } = useWorkspace();
  const organizationId = boundWorkspace?.organizationId ?? null;
  const workspaceBranchId = boundWorkspace?.branchId ?? null;
  const queryClient = useQueryClient();

  const readinessQuery = useQuery({
    queryKey: ["branch-readiness", organizationId, branchId],
    queryFn: () => getBranchReadiness(organizationId!, branchId),
    enabled: Boolean(organizationId && branchId),
  });

  const visitMutation = useMutation({
    mutationFn: (step: string) =>
      upsertBranchSetupProgress(organizationId!, branchId, { lastVisitedStep: step }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["branch-readiness", organizationId, branchId] });
    },
  });

  if (readinessQuery.isLoading) {
    return <LoadingSkeleton count={6} />;
  }

  if (readinessQuery.isError || !readinessQuery.data) {
    return (
      <ErrorState
        title={t("error.title")}
        detail={t("branches.setup.loadError")}
        error={readinessQuery.error}
        operation="branch-readiness.load"
      />
    );
  }

  const readiness = readinessQuery.data;
  const sections = SECTION_ORDER.map((key) => readiness.sections.find((s) => s.key === key)).filter(Boolean);
  const overallStatusKey =
    OVERALL_STATUS_KEYS[readiness.overallStatus as keyof typeof OVERALL_STATUS_KEYS] ??
    "branches.setup.overallStatus.NeedsAttention";

  return (
    <div className="space-y-6">
      <PageHeader
        title={t("branches.setup.title")}
        description={t("branches.setup.description")}
        backTo={`/org/branches/${branchId}`}
        backLabel={t("branches.setup.backToBranch")}
      />

      <div className="flex flex-wrap items-center gap-2">
        <span className="text-sm text-muted-foreground">{t("branches.setup.overall")}</span>
        <StatusChip tone={readiness.overallStatus === "Ready" ? "success" : "warning"}>
          {t(overallStatusKey)}
        </StatusChip>
      </div>

      <ul className="space-y-3">
        {sections.map((section) => {
          const sectionKey = section!.key as SetupSectionKey;
          const sectionLabelKey = SECTION_LABEL_KEYS[sectionKey] ?? "branches.setup.section.Details";
          const sectionStatusKey =
            SECTION_STATUS_KEYS[section!.status as keyof typeof SECTION_STATUS_KEYS] ??
            "branches.setup.sectionStatus.NeedsAttention";

          return (
            <li key={section!.key} className="rounded-lg border p-4">
              <div className="flex flex-wrap items-start justify-between gap-3">
                <div>
                  <h2 className="font-medium">{t(sectionLabelKey)}</h2>
                  {section!.summary ? <p className="text-sm text-muted-foreground">{section!.summary}</p> : null}
                </div>
                <StatusChip tone={section!.status === "Complete" ? "success" : "info"}>
                  {t(sectionStatusKey)}
                </StatusChip>
              </div>
              <div className="mt-3 flex flex-wrap gap-2">
                {section!.managementPath ? (
                  <Button variant="outline" className="h-8 px-3 text-xs" asChild>
                    <Link to={section!.managementPath}>{t("branches.setup.openSection")}</Link>
                  </Button>
                ) : null}
                <Button
                  className="h-8 px-3 text-xs"
                  variant="ghost"
                  onClick={() => visitMutation.mutate(section!.key)}
                >
                  {t("branches.setup.markVisited")}
                </Button>
              </div>
            </li>
          );
        })}
      </ul>

      {workspaceBranchId && workspaceBranchId !== branchId ? (
        <p className="text-xs text-muted-foreground">{t("branches.setup.workspaceBranchHint")}</p>
      ) : null}
    </div>
  );
}
