import { useEffect, useRef } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { getOnboardingProgress } from "@/api/pos/pos-onboarding-client";
import { PosApiError } from "@/api/pos/pos-http";
import { shouldResumeOnboarding } from "@/features/onboarding/onboarding-steps";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

function shouldSkipOnboardingResume(pathname: string): boolean {
  return pathname.startsWith("/onboarding") || pathname.startsWith("/personal");
}

/**
 * Resumes post-subscription onboarding when server progress is InProgress.
 * Runs once per bound organization — never on bottom-nav path changes.
 */
export function OnboardingResumeGate() {
  const navigate = useNavigate();
  const location = useLocation();
  const { boundWorkspace, status } = useWorkspace();
  const checkedOrgIdRef = useRef<string | null>(null);
  const pathnameRef = useRef(location.pathname);
  pathnameRef.current = location.pathname;

  const organizationId = boundWorkspace?.organizationId ?? null;
  const branchId = boundWorkspace?.branchId ?? null;

  useEffect(() => {
    if (!organizationId) {
      checkedOrgIdRef.current = null;
      return;
    }
    if (checkedOrgIdRef.current && checkedOrgIdRef.current !== organizationId) {
      checkedOrgIdRef.current = null;
    }
  }, [organizationId]);

  useEffect(() => {
    if (status !== "bound" && status !== "ready") {
      return;
    }
    if (!organizationId) {
      return;
    }
    if (shouldSkipOnboardingResume(pathnameRef.current)) {
      return;
    }
    if (checkedOrgIdRef.current === organizationId) {
      return;
    }

    const orgIdAtStart = organizationId;
    let cancelled = false;

    void (async () => {
      try {
        const progress = await getOnboardingProgress({
          organizationId: orgIdAtStart,
          branchId,
        });
        if (cancelled || checkedOrgIdRef.current === orgIdAtStart) {
          return;
        }
        if (shouldSkipOnboardingResume(pathnameRef.current)) {
          return;
        }
        checkedOrgIdRef.current = orgIdAtStart;
        if (shouldResumeOnboarding(progress)) {
          navigate("/onboarding", { replace: true });
        }
      } catch (error) {
        if (cancelled) {
          return;
        }
        checkedOrgIdRef.current = orgIdAtStart;
        if (!(error instanceof PosApiError && error.status === 404)) {
          console.warn("[onboarding] resume check failed", error);
        }
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [branchId, navigate, organizationId, status]);

  return null;
}
