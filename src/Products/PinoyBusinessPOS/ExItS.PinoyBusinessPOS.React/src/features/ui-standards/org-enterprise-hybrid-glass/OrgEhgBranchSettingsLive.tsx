import { useMemo, useState } from "react";
import { MemoryRouter, Navigate, Route, Routes } from "react-router-dom";
import { BranchFulfillmentEditPage } from "@/features/branches/BranchFulfillmentEditPage";
import { ConnectedCommerceSettingsPage } from "@/features/connected-commerce/ConnectedCommerceSettingsPage";
import { PaymentMethodsSettingsPage } from "@/features/settings/PaymentMethodsSettingsPage";
import { EmptyState } from "@/components/exits/EmptyState";
import { ExitsTabs } from "@/components/exits/ExitsTabs";
import { OrgEhgLiveFrame } from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgLiveFrame";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type SettingsTab = "fulfillment" | "commerce" | "payments";

/**
 * UI Standards · Branch Settings & Connected Commerce — live Platform/POS settings.
 * Nested MemoryRouter supplies :branchId for BranchFulfillmentEditPage without leaving /ui-standards.
 */
export function OrgEhgBranchSettingsLive() {
  const { boundWorkspace } = useWorkspace();
  const branchId = boundWorkspace?.branchId ?? "";
  const [tab, setTab] = useState<SettingsTab>("fulfillment");

  const tabItems = useMemo(
    () => [
      { key: "fulfillment", label: "Branch fulfillment" },
      { key: "commerce", label: "Connected Commerce" },
      { key: "payments", label: "Payment methods" },
    ],
    [],
  );

  return (
    <OrgEhgLiveFrame
      title="Branch Settings & Connected Commerce"
      description="Operating hours, fulfillment, Connected Commerce, and payment methods from existing APIs."
    >
      <div className="org-ehg__settings-tabs" data-testid="org-ehg-branch-settings-tabs">
        <ExitsTabs
          ariaLabel="Branch settings sections"
          testId="org-ehg-branch-settings-exits-tabs"
          items={tabItems}
          value={tab}
          onValueChange={(value) => setTab(value as SettingsTab)}
        />
      </div>
      {tab === "fulfillment" ? (
        branchId ? (
          <MemoryRouter initialEntries={[`/org/branches/${branchId}/fulfillment`]}>
            <Routes>
              <Route
                path="/org/branches/:branchId/fulfillment"
                element={<BranchFulfillmentEditPage />}
              />
              <Route
                path="*"
                element={<Navigate to={`/org/branches/${branchId}/fulfillment`} replace />}
              />
            </Routes>
          </MemoryRouter>
        ) : (
          <EmptyState
            title="Branch required"
            detail="Bind an organization branch in Workspace to edit fulfillment settings."
            size="compact"
            testId="org-ehg-branch-settings-no-branch"
          />
        )
      ) : null}
      {tab === "commerce" ? <ConnectedCommerceSettingsPage /> : null}
      {tab === "payments" ? <PaymentMethodsSettingsPage /> : null}
    </OrgEhgLiveFrame>
  );
}
