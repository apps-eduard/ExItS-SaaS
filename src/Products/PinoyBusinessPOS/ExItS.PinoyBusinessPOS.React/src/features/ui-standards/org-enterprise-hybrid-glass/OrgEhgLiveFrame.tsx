import type { ReactNode } from "react";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

type OrgEhgLiveFrameProps = {
  title: string;
  description: string;
  children: ReactNode;
};

/**
 * DEV-only host for live Organization pages inside UI Standards.
 * Visual isolation: `.org-ehg` + `data-exits-product="organization"`.
 * Does not redesign production Organization routes.
 */
export function OrgEhgLiveFrame({ title, description, children }: OrgEhgLiveFrameProps) {
  const { boundWorkspace } = useWorkspace();
  const orgName = boundWorkspace?.organizationDisplayName?.trim() || "No organization bound";
  const branchName = boundWorkspace?.branchName?.trim() || "No branch bound";
  const branchType = boundWorkspace?.branchType ?? "—";

  return (
    <div
      className="org-ehg org-ehg--live"
      data-org-ehg=""
      data-exits-product="organization"
      data-testid="org-ehg-live-frame"
    >
      <p className="org-ehg__banner" data-testid="org-ehg-live-banner">
        <strong>{title}</strong> — {description} Uses the bound workspace and real Platform/POS APIs.
        Mutations affect shared development data.
      </p>
      <div className="org-ehg__context" data-testid="org-ehg-live-context">
        <span className="org-ehg__context-item">
          <span className="org-ehg__context-label">Organization</span>
          <span className="org-ehg__context-value">{orgName}</span>
        </span>
        <span className="org-ehg__context-item">
          <span className="org-ehg__context-label">Branch</span>
          <span className="org-ehg__context-value">
            {branchName} · {branchType}
          </span>
        </span>
      </div>
      <div className="org-ehg__live-surface" data-testid="org-ehg-live-surface">
        {children}
      </div>
    </div>
  );
}
