import { useEffect } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { useSession } from "@/session/SessionProvider";
import { useWorkspace } from "@/workspace/WorkspaceProvider";
import { workspaceRouteForOutcome } from "@/workspace/workspace-resolver";

/** After auth lands on `/`, route once to the AMEND-03 destination when unbound. */
export function WorkspaceBootNavigator() {
  const { status: sessionStatus } = useSession();
  const { status, routingPlan, boundWorkspace } = useWorkspace();
  const navigate = useNavigate();
  const location = useLocation();

  useEffect(() => {
    if (sessionStatus !== "authenticated" || boundWorkspace) {
      return;
    }
    if (status !== "ready" && status !== "access_denied") {
      return;
    }
    // Only boot-route from the post-login landing. Do not steal /settings, 404, etc.
    if (location.pathname !== "/") {
      return;
    }

    if (status === "access_denied") {
      navigate("/workspace", { replace: true });
      return;
    }

    if (!routingPlan || routingPlan.outcome === "AutoSelect") {
      return;
    }

    const target = workspaceRouteForOutcome(routingPlan.outcome);
    if (target !== "/") {
      navigate(target, { replace: true });
    }
  }, [boundWorkspace, location.pathname, navigate, routingPlan, sessionStatus, status]);

  return null;
}
