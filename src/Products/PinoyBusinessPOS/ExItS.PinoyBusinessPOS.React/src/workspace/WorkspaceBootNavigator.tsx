import { useEffect } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { sessionAccountClass } from "@/session/account-class";
import { isAuthenticatedOrColdStartOffline, useSession } from "@/session/SessionProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";
import { postLoginRoute } from "@/workspace/workspace-resolver";

/** After auth lands on `/`, route once to a shell this account class is allowed to open. */
export function WorkspaceBootNavigator() {
  const { status: sessionStatus, session } = useSession();
  const { status, routingPlan, boundWorkspace } = useWorkspace();
  const navigate = useNavigate();
  const location = useLocation();

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
  }, [boundWorkspace, location.pathname, navigate, routingPlan, session, sessionStatus, status]);

  return null;
}
