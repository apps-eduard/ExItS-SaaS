import { Check } from "lucide-react";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import type { BranchFulfillmentSetupSummaryFields } from "@/api/platform/branch-fulfillment-client";
import {
  BRANCH_SETUP_TABS,
  BRANCH_SETUP_TAB_LABEL_KEYS,
  branchFulfillmentEditPath,
  branchSetupTabComplete,
  type BranchSetupTab,
} from "@/features/branches/branch-setup-tabs";
import type { MessageKey } from "@/i18n/messages";

function TabCompleteIcon({ complete }: { complete: boolean }) {
  if (!complete) {
    return null;
  }
  return <Check className="size-3.5 shrink-0 text-[color:var(--exits-success)]" aria-hidden />;
}

type BranchSetupTabLinksProps = {
  branchId: string;
  summary: BranchFulfillmentSetupSummaryFields;
  t: (key: MessageKey) => string;
  testIdPrefix?: string;
  /** Single-branch list shows Overview selected; multi-branch leaves all tabs idle. */
  activeTab?: BranchSetupTab | null;
};

export function BranchSetupTabLinks({
  branchId,
  summary,
  t,
  testIdPrefix = "branch",
  activeTab = null,
}: BranchSetupTabLinksProps) {
  const prefix = `${testIdPrefix}-${branchId}`;

  return (
    <div className="branch-setup-tabs-scroll">
      <ExitsChipBar
        variant="filter"
        ariaLabel={t("branches.setupTabsLabel")}
        testId={`${prefix}-setup-tabs`}
        className="branch-setup-tabs"
        items={BRANCH_SETUP_TABS.map((key) => {
          const complete = branchSetupTabComplete(key, summary);
          return {
            key,
            label: (
              <span className="branch-setup-tab-label">
                {complete ? <TabCompleteIcon complete /> : null}
                {t(BRANCH_SETUP_TAB_LABEL_KEYS[key])}
              </span>
            ),
            href: branchFulfillmentEditPath(branchId, key),
            state: activeTab === key ? "active" : "idle",
            testId: `${prefix}-setup-tab-${key}`,
          };
        })}
      />
    </div>
  );
}
