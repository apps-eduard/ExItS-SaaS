import { useCallback, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { ACCOUNT_CONTEXT_SWITCH_PATH } from "@/features/account/account-context-switch-route";
import { sessionAccountClass } from "@/session/account-class";
import { ensureOrganizationSessionProfile } from "@/session/ensure-organization-profile";
import { useSession } from "@/session/SessionProvider";
import {
  buildOrganizationDestinations,
  selectBusinessEntry,
} from "@/workspace/workspace-destinations";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export { ACCOUNT_CONTEXT_SWITCH_PATH };

export const PERSONAL_BUSINESSES_PATH = "/personal/businesses";

async function enterOrganizationBusiness(input: {
  organizationId: string;
  workspaces: ReturnType<typeof useWorkspace>["workspaces"];
  session: ReturnType<typeof useSession>["session"];
  refreshSession: ReturnType<typeof useSession>["refreshSession"];
  clearBoundWorkspace: () => void;
  refreshWorkspaces: () => Promise<void>;
  ensureOrganizationGrantHint: ReturnType<typeof useWorkspace>["ensureOrganizationGrantHint"];
  bindDestination: ReturnType<typeof useWorkspace>["bindDestination"];
  navigate: ReturnType<typeof useNavigate>;
  /** After the organization session is bound. Defaults to that business home. */
  route?: string;
}): Promise<void> {
  input.clearBoundWorkspace();
  const ensured = await ensureOrganizationSessionProfile({
    session: input.session,
    refreshSession: input.refreshSession,
  });
  if (!ensured.ok) {
    input.navigate("/personal", { replace: true });
    return;
  }

  await input.refreshWorkspaces();
  const organization =
    input.workspaces.find((workspace) => workspace.organizationId === input.organizationId) ??
    input.workspaces[0];
  if (!organization) {
    input.navigate("/workspace", { replace: true });
    return;
  }

  const grant = await input.ensureOrganizationGrantHint(organization.organizationId);
  const entry = selectBusinessEntry(
    buildOrganizationDestinations({ workspace: organization, grant }),
  );
  if (entry) {
    const ok = await input.bindDestination(entry);
    if (ok) {
      input.navigate(input.route ?? entry.route, { replace: true });
      return;
    }
  }

  input.navigate("/workspace", { replace: true });
}

/**
 * Personal → business entry. One business opens directly. Several businesses open the portfolio.
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
    navigate(ACCOUNT_CONTEXT_SWITCH_PATH, { replace: true });
    try {
      clearBoundWorkspace();

      if (workspaces.length > 1) {
        navigate(PERSONAL_BUSINESSES_PATH, { replace: true });
        return;
      }

      const onlyOrg = workspaces[0];
      if (!onlyOrg) {
        navigate("/personal", { replace: true });
        return;
      }

      await enterOrganizationBusiness({
        organizationId: onlyOrg.organizationId,
        workspaces,
        session,
        refreshSession,
        clearBoundWorkspace,
        refreshWorkspaces,
        ensureOrganizationGrantHint,
        bindDestination,
        navigate,
      });
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

/** Open one affiliated business from Personal. Clears the previous bound organization first. */
export function useEnterBusiness() {
  const navigate = useNavigate();
  const online = useBrowserOnline();
  const { session, refreshSession } = useSession();
  const {
    workspaces,
    clearBoundWorkspace,
    ensureOrganizationGrantHint,
    bindDestination,
    refreshWorkspaces,
  } = useWorkspace();
  const [entering, setEntering] = useState(false);

  const enterBusiness = useCallback(
    async (organizationId: string, route?: string) => {
      if (!online || entering) {
        return;
      }
      setEntering(true);
      navigate(ACCOUNT_CONTEXT_SWITCH_PATH, { replace: true });
      try {
        await enterOrganizationBusiness({
          organizationId,
          workspaces,
          session,
          refreshSession,
          clearBoundWorkspace,
          refreshWorkspaces,
          ensureOrganizationGrantHint,
          bindDestination,
          navigate,
          route,
        });
      } finally {
        setEntering(false);
      }
    },
    [
      bindDestination,
      clearBoundWorkspace,
      entering,
      ensureOrganizationGrantHint,
      navigate,
      online,
      refreshSession,
      refreshWorkspaces,
      session,
      workspaces,
    ],
  );

  return { enterBusiness, entering, online };
}
