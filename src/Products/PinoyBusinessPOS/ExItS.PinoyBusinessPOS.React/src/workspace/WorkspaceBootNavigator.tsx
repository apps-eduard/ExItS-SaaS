import { useEffect, useRef } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { isOrganizationContextLocked, sessionAccountClass } from "@/session/account-class";
import { ensurePersonalSessionProfile } from "@/session/ensure-personal-profile";
import { isAuthenticatedOrColdStartOffline, useSession } from "@/session/SessionProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";
import { postLoginRoute } from "@/workspace/workspace-resolver";

/** After auth lands on `/`, route once to a shell this account class is allowed to open. */
export function WorkspaceBootNavigator() {
  const { status: sessionStatus, session, refreshSession } = useSession();
  const { status, routingPlan, boundWorkspace, clearBoundWorkspace } = useWorkspace();
  const navigate = useNavigate();
  const location = useLocation();
  const personalRedirectStarted = useRef(false);

  useEffect(() => {
    if (!isAuthenticatedOrColdStartOffline(sessionStatus) || boundWorkspace) {
      return;
    }
    if (status !== "ready" && status !== "access_denied") {
      return;
    }
    // Only boot-route from the post-login landing. Do not steal /settings, 404, etc.
    if (location.pathname !== "/") {
      return;
    }

    const accountClass = sessionAccountClass(session);
    if (
      accountClass === "Organization" &&
      !isOrganizationContextLocked(session) &&
      status !== "access_denied"
    ) {
      if (personalRedirectStarted.current) {
        return;
      }
      personalRedirectStarted.current = true;
      void (async () => {
        const personal = await ensurePersonalSessionProfile({ session, refreshSession });
        if (!personal.ok) {
          personalRedirectStarted.current = false;
          const fallback = postLoginRoute({
            outcome: routingPlan?.outcome ?? "ShowChooser",
            accountClass,
          });
          if (fallback !== "/") {
            navigate(fallback, { replace: true });
          }
          return;
        }
        clearBoundWorkspace();
        navigate("/personal", { replace: true });
      })();
      return;
    }

    if (status === "access_denied") {
      const deniedTarget = postLoginRoute({
        outcome: routingPlan?.outcome ?? "ShowChooser",
        accountClass,
        accessDenied: true,
      });
      if (deniedTarget !== "/") {
        navigate(deniedTarget, { replace: true });
      }
      return;
    }

    if (!routingPlan || routingPlan.outcome === "AutoSelect" || routingPlan.outcome === "AutoDestination") {
      return;
    }

    const target = postLoginRoute({ outcome: routingPlan.outcome, accountClass });
    if (target !== "/") {
      navigate(target, { replace: true });
    }
  }, [
    boundWorkspace,
    clearBoundWorkspace,
    location.pathname,
    navigate,
    refreshSession,
    routingPlan,
    session,
    sessionStatus,
    status,
  ]);

  return null;
}
