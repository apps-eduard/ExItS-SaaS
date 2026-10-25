import { DashboardRoutePage } from "@/features/reports/DashboardRoutePage";
import { OrgEhgLiveFrame } from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgLiveFrame";

/** UI Standards · Organization Dashboard — live POS reporting. */
export function OrgEhgDashboardLive() {
  return (
    <OrgEhgLiveFrame
      title="Organization Dashboard"
      description="Retail and warehouse dashboards from existing POS reporting endpoints."
    >
      <DashboardRoutePage />
    </OrgEhgLiveFrame>
  );
}
