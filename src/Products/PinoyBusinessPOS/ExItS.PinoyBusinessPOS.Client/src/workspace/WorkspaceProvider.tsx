import { useQueryClient } from "@tanstack/react-query";
import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from "react";
import { useLocation, useNavigate } from "react-router-dom";
import type { BrowserSessionSnapshot } from "@/api/platform/browser-session";
import { clearPosAccessToken } from "@/api/platform/pos-access-token";
import { clearPosSessionGrant, getPosSessionGrant } from "@/api/platform/pos-session-grant";
import { resolveRoleHomeRoute } from "@/access/pos-capabilities";
import {
  bindWorkspaceWithSessionGrant,
  listEligibleOrganizations,
  listOrganizationBranches,
  type PlatformBranch,
  type SessionGrantResponse,
} from "@/api/platform/platform-auth-client";
import { clearPlatformAntiforgeryToken } from "@/api/platform/platform-http";
import { selectOperationalBranch } from "@/api/pos/operational-branch-client";
import { sessionAccountClass, isOrganizationContextLocked } from "@/session/account-class";
import { ensureOrganizationSessionProfile } from "@/session/ensure-organization-profile";
import { useSession } from "@/session/SessionProvider";
import { DEFERRED_POS_DEVICE_CONTEXT, type PosDeviceContext } from "@/workspace/pos-device-context";
import type {
  AccessibleOrganizationWorkspace,
  BoundWorkspace,
  WorkspaceRoutingPlan,
} from "@/workspace/types";
import {
  buildAccessibleWorkspaces,
  resolveWorkspaceRoutingPlan,
} from "@/workspace/workspace-resolver";

export type WorkspaceStatus =
  "idle" | "loading" | "ready" | "binding" | "bound" | "access_denied" | "error";

type WorkspaceContextValue = {
  status: WorkspaceStatus;
  workspaces: AccessibleOrganizationWorkspace[];
  routingPlan: WorkspaceRoutingPlan | null;
  boundWorkspace: BoundWorkspace | null;
  sessionGrant: SessionGrantResponse | null;
  /** Honest device state — never invents an authorized POS terminal. */
  posDevice: PosDeviceContext;
  accessDeniedDetail: string | null;
  bindWorkspace: (organizationId: string, branchId: string) => Promise<boolean>;
  refreshWorkspaces: () => Promise<void>;
  clearBoundWorkspace: () => void;
};

const WorkspaceContext = createContext<WorkspaceContextValue | null>(null);

function findWorkspaceLabel(
  workspaces: AccessibleOrganizationWorkspace[],
  organizationId: string,
  branchId: string,
): BoundWorkspace | null {
  const organization = workspaces.find((item) => item.organizationId === organizationId);
  const branch = organization?.branches.find((item) => item.branchId === branchId);
  if (!organization || !branch) {
    return null;
  }
  return {
    organizationId: organization.organizationId,
    organizationDisplayName: organization.displayName,
    branchId: branch.branchId,
    branchName: branch.name,
  };
}

export function WorkspaceProvider({ children }: { children: ReactNode }) {
  const { status: sessionStatus, session, refreshSession } = useSession();
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const location = useLocation();
  const autoBindAttempted = useRef(false);

  const [status, setStatus] = useState<WorkspaceStatus>("idle");
  const [workspaces, setWorkspaces] = useState<AccessibleOrganizationWorkspace[]>([]);
  const [routingPlan, setRoutingPlan] = useState<WorkspaceRoutingPlan | null>(null);
  const [boundWorkspace, setBoundWorkspace] = useState<BoundWorkspace | null>(null);
  const [sessionGrant, setSessionGrantState] = useState<SessionGrantResponse | null>(() =>
    getPosSessionGrant(),
  );
  const [posDevice] = useState<PosDeviceContext>(DEFERRED_POS_DEVICE_CONTEXT);
  const [accessDeniedDetail, setAccessDeniedDetail] = useState<string | null>(null);

  const clearBoundWorkspace = useCallback(() => {
    setBoundWorkspace(null);
    setSessionGrantState(null);
    clearPosAccessToken();
    clearPosSessionGrant();
    autoBindAttempted.current = false;
  }, []);

  const loadWorkspaces = useCallback(async (currentSession: BrowserSessionSnapshot | null) => {
    setStatus("loading");
    setAccessDeniedDetail(null);

    const accountClass = sessionAccountClass(currentSession);

    const organizationsResult = await listEligibleOrganizations();
    if (!organizationsResult.ok) {
      setStatus("error");
      setWorkspaces([]);
      setRoutingPlan(null);
      return;
    }

    const branchesByOrganizationId = new Map<string, PlatformBranch[]>();
    for (const organization of organizationsResult.organizations) {
      const branchesResult = await listOrganizationBranches(organization.organizationId);
      branchesByOrganizationId.set(
        organization.organizationId,
        branchesResult.ok ? branchesResult.branches : [],
      );
    }

    const accessible = buildAccessibleWorkspaces(
      organizationsResult.organizations,
      branchesByOrganizationId,
    );
    const plan = resolveWorkspaceRoutingPlan({
      organizationCount: organizationsResult.organizations.length,
      workspaces: accessible,
      accountClass,
    });

    setWorkspaces(accessible);
    setRoutingPlan(plan);

    // Stale bound branch: revalidate against server-filtered Active accessible set.
    setBoundWorkspace((current) => {
      if (!current) {
        setStatus("ready");
        return current;
      }
      const stillValid = findWorkspaceLabel(accessible, current.organizationId, current.branchId);
      if (!stillValid) {
        clearPosAccessToken();
        clearPosSessionGrant();
        setSessionGrantState(null);
        setAccessDeniedDetail(
          "The previously selected branch is no longer accessible. Choose an active branch.",
        );
        setStatus("ready");
        autoBindAttempted.current = false;
        return null;
      }
      setStatus("bound");
      setSessionGrantState(getPosSessionGrant());
      return stillValid;
    });

    if (accountClass === "Personal") {
      return;
    }

    if (
      currentSession?.selectedOrganizationId &&
      accessible.some(
        (workspace) => workspace.organizationId === currentSession.selectedOrganizationId,
      )
    ) {
      const organization = accessible.find(
        (workspace) => workspace.organizationId === currentSession.selectedOrganizationId,
      );
      if (organization && organization.branches.length === 1) {
        const label = findWorkspaceLabel(
          accessible,
          organization.organizationId,
          organization.branches[0].branchId,
        );
        if (label) {
          setBoundWorkspace((current) => current ?? label);
          setSessionGrantState(getPosSessionGrant());
          setStatus((current) => (current === "bound" ? current : "bound"));
        }
      }
    }
  }, []);

  const refreshWorkspaces = useCallback(async () => {
    await loadWorkspaces(session);
  }, [loadWorkspaces, session]);

  const bindWorkspace = useCallback(
    async (organizationId: string, branchId: string) => {
      if (isOrganizationContextLocked(session)) {
        const homeOrg = session?.homeOrganizationId;
        if (homeOrg && homeOrg !== organizationId) {
          setAccessDeniedDetail(
            "This staff account is locked to its home organization. Sign in with a different account to use another organization.",
          );
          setStatus("access_denied");
          return false;
        }
      }

      const accessibleLabel = findWorkspaceLabel(workspaces, organizationId, branchId);
      if (!accessibleLabel && workspaces.length > 0) {
        setAccessDeniedDetail("That branch is not an accessible Active branch for this account.");
        setStatus("access_denied");
        return false;
      }

      let activeSession = session;
      if (sessionAccountClass(activeSession) === "Personal") {
        setStatus("binding");
        const ensured = await ensureOrganizationSessionProfile({
          session: activeSession,
          refreshSession,
        });
        if (!ensured.ok) {
          setAccessDeniedDetail(ensured.detail);
          setStatus("access_denied");
          return false;
        }
        activeSession = ensured.session;
      }

      if (sessionAccountClass(activeSession) !== "Organization") {
        setAccessDeniedDetail("Organization workspace requires an Organization account profile.");
        setStatus("access_denied");
        return false;
      }

      setStatus("binding");
      setAccessDeniedDetail(null);
      const previousBranchId = boundWorkspace?.branchId ?? null;
      const result = await bindWorkspaceWithSessionGrant(organizationId, branchId);
      if (!result.ok) {
        if (result.reason === "access_denied") {
          setAccessDeniedDetail(result.body?.detail ?? null);
          setBoundWorkspace(null);
          setStatus("access_denied");
          return false;
        }
        setAccessDeniedDetail(result.body?.detail ?? null);
        setStatus("ready");
        return false;
      }

      // MAUI parity: POS operational branch after Platform bind. Never invent deviceBoundBranchId.
      const operational = await selectOperationalBranch({
        organizationId,
        branchId,
        fromBranchId: previousBranchId,
      });
      if (
        !operational.ok &&
        operational.status !== 403 &&
        operational.errorCode !== "application.capability.denied"
      ) {
        clearPosAccessToken();
        clearPosSessionGrant();
        setSessionGrantState(null);
        setBoundWorkspace(null);
        setAccessDeniedDetail(operational.detail ?? "Operational branch could not be selected.");
        setStatus("access_denied");
        return false;
      }

      const label = accessibleLabel ??
        findWorkspaceLabel(workspaces, organizationId, branchId) ?? {
          organizationId,
          organizationDisplayName: organizationId,
          branchId,
          branchName: branchId,
        };
      setBoundWorkspace(label);
      setSessionGrantState(result.grant);
      setStatus("bound");
      return true;
    },
    [boundWorkspace?.branchId, refreshSession, session, workspaces],
  );

  useEffect(() => {
    if (sessionStatus !== "authenticated") {
      setWorkspaces((current) => (current.length === 0 ? current : []));
      setRoutingPlan((current) => (current === null ? current : null));
      setBoundWorkspace((current) => (current === null ? current : null));
      setAccessDeniedDetail((current) => (current === null ? current : null));
      setSessionGrantState((current) => (current === null ? current : null));
      setStatus((current) => (current === "idle" ? current : "idle"));
      autoBindAttempted.current = false;
      return;
    }

    void loadWorkspaces(session);
  }, [loadWorkspaces, session, sessionStatus]);

  useEffect(() => {
    let cancelled = false;
    if (sessionStatus !== "authenticated" || status !== "ready" || boundWorkspace) {
      return;
    }
    if (sessionAccountClass(session) === "Personal") {
      return;
    }
    if (!routingPlan || routingPlan.outcome !== "AutoSelect") {
      return;
    }
    if (!routingPlan.autoOrganizationId || !routingPlan.autoBranchId) {
      return;
    }
    if (autoBindAttempted.current) {
      return;
    }
    autoBindAttempted.current = true;

    void bindWorkspace(routingPlan.autoOrganizationId, routingPlan.autoBranchId).then((ok) => {
      if (cancelled || !ok) {
        return;
      }
      if (location.pathname !== "/") {
        return;
      }
      const grant = getPosSessionGrant();
      navigate(resolveRoleHomeRoute(grant), { replace: true });
    });

    return () => {
      cancelled = true;
    };
  }, [
    bindWorkspace,
    boundWorkspace,
    location.pathname,
    navigate,
    routingPlan,
    session,
    sessionStatus,
    status,
  ]);

  const signOutReset = useCallback(() => {
    queryClient.clear();
    clearPlatformAntiforgeryToken();
    clearPosAccessToken();
    clearPosSessionGrant();
    clearBoundWorkspace();
  }, [clearBoundWorkspace, queryClient]);

  const previousSessionStatus = useRef(sessionStatus);
  useEffect(() => {
    const previous = previousSessionStatus.current;
    previousSessionStatus.current = sessionStatus;
    if (
      sessionStatus === "unauthenticated" &&
      (previous === "authenticated" || previous === "expired" || previous === "loading")
    ) {
      signOutReset();
    }
  }, [sessionStatus, signOutReset]);

  const value = useMemo<WorkspaceContextValue>(
    () => ({
      status,
      workspaces,
      routingPlan,
      boundWorkspace,
      sessionGrant,
      posDevice,
      accessDeniedDetail,
      bindWorkspace,
      refreshWorkspaces,
      clearBoundWorkspace,
    }),
    [
      accessDeniedDetail,
      bindWorkspace,
      boundWorkspace,
      clearBoundWorkspace,
      posDevice,
      refreshWorkspaces,
      routingPlan,
      sessionGrant,
      status,
      workspaces,
    ],
  );

  return <WorkspaceContext.Provider value={value}>{children}</WorkspaceContext.Provider>;
}

export function useWorkspace(): WorkspaceContextValue {
  const context = useContext(WorkspaceContext);
  if (!context) {
    throw new Error("useWorkspace must be used within WorkspaceProvider");
  }
  return context;
}
