import { useCallback, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { sessionAccountClass } from "@/session/account-class";
import { ensureOrganizationSessionProfile } from "@/session/ensure-organization-profile";
import { useSession } from "@/session/SessionProvider";
import { resolveDestinationRouting } from "@/workspace/workspace-destinations";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

/**
 * Personal → Organization/Business entry reuses workspace bind + smart destination routing.
 * Multiple accessible orgs always land on the unified workspace chooser.
 */
export function useSwitchToBusiness() {
  const navigate = useNavigate();
  const online = useBrowserOnline();
  const { session, refreshSession } = useSession();
  const {
    workspaces,
    status,
    clearBoundWorkspace,
    ensureOrganizationGrantHint,
    bindDestination,
    refreshWorkspaces,
  } = useWorkspace();
  const [switching, setSwitching] = useState(false);

  const canSwitch =
    sessionAccountClass(session) === "Personal" &&
    workspaces.length > 0 &&
    (status === "ready" || status === "bound");

  const switchToBusiness = useCallback(async () => {
    if (!online || switching || workspaces.length === 0) {
      return;
    }

    setSwitching(true);
    try {
      clearBoundWorkspace();

      const ensured = await ensureOrganizationSessionProfile({ session, refreshSession });
      if (!ensured.ok) {
        return;
      }

      await refreshWorkspaces();

      if (workspaces.length > 1) {
        navigate("/workspace", { replace: true });
        return;
      }

      const onlyOrg = workspaces[0];
      const grant = await ensureOrganizationGrantHint(onlyOrg.organizationId);
      const routing = resolveDestinationRouting({
        workspaces,
        grantByOrganizationId: new Map([[onlyOrg.organizationId, grant]]),
      });

      if (routing.outcome === "AutoDestination") {
        const ok = await bindDestination(routing.destination);
        if (ok) {
          navigate(routing.destination.route, { replace: true });
          return;
        }
      }

      navigate("/workspace", { replace: true });
    } finally {
      setSwitching(false);
    }
  }, [
    bindDestination,
    clearBoundWorkspace,
    ensureOrganizationGrantHint,
    navigate,
    online,
    refreshSession,
    refreshWorkspaces,
    session,
    switching,
    workspaces,
  ]);

  return { canSwitch, switching, switchToBusiness, online };
}
