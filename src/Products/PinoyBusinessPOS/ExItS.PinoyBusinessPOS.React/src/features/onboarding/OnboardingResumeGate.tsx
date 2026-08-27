import { useEffect, useRef } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { getOnboardingProgress } from "@/api/pos/pos-onboarding-client";
import { PosApiError } from "@/api/pos/pos-http";
import { isAbortError } from "@/diagnostics/global-error-reporter";
import { shouldResumeOnboarding } from "@/features/onboarding/onboarding-steps";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

/**
 * Resumes post-subscription onboarding when server progress is InProgress.
 * Existing organizations without a progress row are never redirected.
 *
 * Runs once per bound organization — not on every bottom-nav path change.
 * Tab switching previously re-fired this check and stacked with Catalog/Orders
 * remount refetches, which made the org shell feel hung after a few clicks.
 */
export function OnboardingResumeGate() {
  const navigate = useNavigate();
  const location = useLocation();
  const { boundWorkspace, status } = useWorkspace();
  const checkedOrgIdRef = useRef<string | null>(null);
  const inFlightOrgIdRef = useRef<string | null>(null);

  const organizationId = boundWorkspace?.organizationId ?? null;
  const branchId = boundWorkspace?.branchId ?? null;
  const skipPath =
    location.pathname.startsWith("/onboarding") ||
    location.pathname.startsWith("/personal");

  useEffect(() => {
    if (!organizationId) {
      checkedOrgIdRef.current = null;
      inFlightOrgIdRef.current = null;
    } else if (
      checkedOrgIdRef.current &&
      checkedOrgIdRef.current !== organizationId
    ) {
      checkedOrgIdRef.current = null;
    }
  }, [organizationId]);

  useEffect(() => {
    if (status !== "bound" && status !== "ready") {
      return;
    }
    if (!organizationId || skipPath) {
      return;
    }
    if (checkedOrgIdRef.current === organizationId) {
      return;
    }
    // Same org check already running — ignore further tab clicks.
    if (inFlightOrgIdRef.current === organizationId) {
      return;
    }

    const controller = new AbortController();
    const orgIdAtStart = organizationId;
    inFlightOrgIdRef.current = orgIdAtStart;

    void (async () => {
      try {
        const progress = await getOnboardingProgress(
          {
            organizationId: orgIdAtStart,
            branchId,
          },
          controller.signal,
        );
        if (controller.signal.aborted) {
          return;
        }
        checkedOrgIdRef.current = orgIdAtStart;
        if (shouldResumeOnboarding(progress)) {
          navigate("/onboarding", { replace: true });
        }
      } catch (error) {
        if (controller.signal.aborted || isAbortError(error)) {
          return;
        }
        // Avoid retry storms on every tab click after a transient failure / 404.
        checkedOrgIdRef.current = orgIdAtStart;
        if (!(error instanceof PosApiError && error.status === 404)) {
          console.warn("[onboarding] resume check failed", error);
        }
      } finally {
        if (inFlightOrgIdRef.current === orgIdAtStart) {
          inFlightOrgIdRef.current = null;
        }
      }
    })();

    return () => {
      // Abort when organizationId/status/branch changes or the gate unmounts.
      // Pathname-only re-runs are blocked by inFlightOrgIdRef above, so they
      // do not reach this cleanup for a *new* request.
      controller.abort();
      if (inFlightOrgIdRef.current === orgIdAtStart) {
        inFlightOrgIdRef.current = null;
      }
    };
  }, [branchId, navigate, organizationId, skipPath, status]);

  return null;
}
