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
import { useNavigate } from "react-router-dom";
import type { BrowserSessionSnapshot } from "@/api/platform/browser-session";
import { clearPosAccessToken } from "@/api/platform/pos-access-token";
import {
  bindWorkspaceWithSessionGrant,
  listEligibleOrganizations,
  listOrganizationBranches,
  type PlatformBranch,
} from "@/api/platform/platform-auth-client";
import { clearPlatformAntiforgeryToken } from "@/api/platform/platform-http";
import { useSession } from "@/session/SessionProvider";
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
  const { status: sessionStatus, session } = useSession();
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const autoBindAttempted = useRef(false);

  const [status, setStatus] = useState<WorkspaceStatus>("idle");
  const [workspaces, setWorkspaces] = useState<AccessibleOrganizationWorkspace[]>([]);
  const [routingPlan, setRoutingPlan] = useState<WorkspaceRoutingPlan | null>(null);
  const [boundWorkspace, setBoundWorkspace] = useState<BoundWorkspace | null>(null);
  const [accessDeniedDetail, setAccessDeniedDetail] = useState<string | null>(null);

  const clearBoundWorkspace = useCallback(() => {
    setBoundWorkspace(null);
    clearPosAccessToken();
    autoBindAttempted.current = false;
  }, []);

  const loadWorkspaces = useCallback(async (currentSession: BrowserSessionSnapshot | null) => {
    setStatus("loading");
    setAccessDeniedDetail(null);

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
    });

    setWorkspaces(accessible);
    setRoutingPlan(plan);
    setStatus("ready");

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
          setBoundWorkspace(label);
          setStatus("bound");
        }
      }
    }
  }, []);

  const refreshWorkspaces = useCallback(async () => {
    await loadWorkspaces(session);
  }, [loadWorkspaces, session]);

  const bindWorkspace = useCallback(
    async (organizationId: string, branchId: string) => {
      setStatus("binding");
      setAccessDeniedDetail(null);
      const result = await bindWorkspaceWithSessionGrant(organizationId, branchId);
      if (!result.ok) {
        if (result.reason === "access_denied") {
          setAccessDeniedDetail(result.body?.detail ?? null);
          setBoundWorkspace(null);
          setStatus("access_denied");
          return false;
        }
        setStatus("error");
        return false;
      }

      const label = findWorkspaceLabel(workspaces, organizationId, branchId);
      if (label) {
        setBoundWorkspace(label);
      } else {
        setBoundWorkspace({
          organizationId,
          organizationDisplayName: organizationId,
          branchId,
          branchName: branchId,
        });
      }
      setStatus("bound");
      return true;
    },
    [workspaces],
  );

  useEffect(() => {
    if (sessionStatus !== "authenticated") {
      setWorkspaces([]);
      setRoutingPlan(null);
      setBoundWorkspace(null);
      setAccessDeniedDetail(null);
      setStatus("idle");
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
      navigate("/", { replace: true });
    });

    return () => {
      cancelled = true;
    };
  }, [bindWorkspace, boundWorkspace, navigate, routingPlan, sessionStatus, status]);

  const signOutReset = useCallback(() => {
    queryClient.clear();
    clearPlatformAntiforgeryToken();
    clearPosAccessToken();
    clearBoundWorkspace();
  }, [clearBoundWorkspace, queryClient]);

  useEffect(() => {
    if (sessionStatus === "unauthenticated") {
      signOutReset();
    }
  }, [sessionStatus, signOutReset]);

  const value = useMemo<WorkspaceContextValue>(
    () => ({
      status,
      workspaces,
      routingPlan,
      boundWorkspace,
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
      refreshWorkspaces,
      routingPlan,
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
