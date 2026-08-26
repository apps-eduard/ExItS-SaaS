import { useEffect, useRef } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { getOnboardingProgress } from "@/api/pos/pos-onboarding-client";
import { PosApiError } from "@/api/pos/pos-http";
import { shouldResumeOnboarding } from "@/features/onboarding/onboarding-steps";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

/**
 * Resumes post-subscription onboarding when server progress is InProgress.
 * Existing organizations without a progress row are never redirected.
 */
export function OnboardingResumeGate() {
  const navigate = useNavigate();
  const location = useLocation();
  const { boundWorkspace, status } = useWorkspace();
  const inFlight = useRef(false);

  useEffect(() => {
    if (status !== "bound" && status !== "ready") {
      return;
    }
    const organizationId = boundWorkspace?.organizationId;
    if (!organizationId) {
      return;
    }
    if (location.pathname.startsWith("/onboarding")) {
      return;
    }
    if (location.pathname.startsWith("/personal")) {
      return;
    }
    if (inFlight.current) {
      return;
    }

    let cancelled = false;
    inFlight.current = true;

    void (async () => {
      try {
        const progress = await getOnboardingProgress({
          organizationId,
          branchId: boundWorkspace.branchId ?? null,
        });
        if (cancelled) return;
        if (shouldResumeOnboarding(progress)) {
          navigate("/onboarding", { replace: true });
        }
      } catch (error) {
        // 404 = existing org or never started wizard — do not force.
        if (!(error instanceof PosApiError && error.status === 404)) {
          console.warn("[onboarding] resume check failed", error);
        }
      } finally {
        inFlight.current = false;
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [
    boundWorkspace?.branchId,
    boundWorkspace?.organizationId,
    location.pathname,
    navigate,
    status,
  ]);

  return null;
}
