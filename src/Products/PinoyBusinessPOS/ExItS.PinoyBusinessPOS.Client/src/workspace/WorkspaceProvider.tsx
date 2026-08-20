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
import { clearPosSessionGrant, getPosSessionGrant } from "@/api/platform/pos-session-grant";
import {
  bindOrganizationManagementGrant,
  bindWorkspaceWithSessionGrant,
  listEligibleOrganizations,
  listOrganizationBranches,
  probeOrganizationSessionGrant,
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
import {
  classifyWorkspaceBindFailure,
  type WorkspaceBindFailureKind,
} from "@/workspace/workspace-bind-error";
import {
  resolveDestinationRouting,
  type WorkspaceDestination,
} from "@/workspace/workspace-destinations";
import type { WorkingExperience } from "@/workspace/working-experience";

export type WorkspaceStatus =
  "idle" | "loading" | "ready" | "binding" | "bound" | "access_denied" | "error";

type WorkspaceContextValue = {
  status: WorkspaceStatus;
  workspaces: AccessibleOrganizationWorkspace[];
  routingPlan: WorkspaceRoutingPlan | null;
  boundWorkspace: BoundWorkspace | null;
  sessionGrant: SessionGrantResponse | null;
  /** Authoritative grants probed per org for destination visibility (may be unbound). */
  grantByOrganizationId: ReadonlyMap<string, SessionGrantResponse | null>;
  /** Honest device state — never invents an authorized POS terminal. */
  posDevice: PosDeviceContext;
  accessDeniedDetail: string | null;
  /** Classified bind failure for user-facing copy (null when no denial). */
  bindFailureKind: WorkspaceBindFailureKind | null;
  /** @deprecated Prefer bindDestination — kept for legacy callers. */
  bindWorkspace: (organizationId: string, branchId: string) => Promise<boolean>;
  bindDestination: (destination: WorkspaceDestination) => Promise<boolean>;
  ensureOrganizationGrantHint: (organizationId: string) => Promise<SessionGrantResponse | null>;
  refreshWorkspaces: () => Promise<void>;
  clearBoundWorkspace: () => void;
};

const WorkspaceContext = createContext<WorkspaceContextValue | null>(null);

function findBranchLabel(
  workspaces: AccessibleOrganizationWorkspace[],
  organizationId: string,
  branchId: string,
): { organizationDisplayName: string; branchName: string } | null {
  const organization = workspaces.find((item) => item.organizationId === organizationId);
  const branch = organization?.branches.find((item) => item.branchId === branchId);
  if (!organization || !branch) {
    return null;
  }
  return {
    organizationDisplayName: organization.displayName,
    branchName: branch.name,
  };
}

function findOrganizationLabel(
  workspaces: AccessibleOrganizationWorkspace[],
  organizationId: string,
): string | null {
  return workspaces.find((item) => item.organizationId === organizationId)?.displayName ?? null;
}

function boundFromDestination(
  destination: WorkspaceDestination,
  workspaces: AccessibleOrganizationWorkspace[],
): BoundWorkspace {
  const orgName =
    findOrganizationLabel(workspaces, destination.organizationId) ??
    destination.organizationDisplayName;
  if (destination.branchId) {
    const labels = findBranchLabel(workspaces, destination.organizationId, destination.branchId);
    return {
      organizationId: destination.organizationId,
      organizationDisplayName: labels?.organizationDisplayName ?? orgName,
      branchId: destination.branchId,
      branchName: labels?.branchName ?? destination.branchName ?? destination.branchId,
      experience: destination.experience,
    };
  }
  return {
    organizationId: destination.organizationId,
    organizationDisplayName: orgName,
    branchId: null,
    branchName: null,
    experience: destination.experience,
  };
}

export function WorkspaceProvider({ children }: { children: ReactNode }) {
  const { status: sessionStatus, session, refreshSession } = useSession();
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const autoDestinationAttempted = useRef(false);

  const [status, setStatus] = useState<WorkspaceStatus>("idle");
  const [workspaces, setWorkspaces] = useState<AccessibleOrganizationWorkspace[]>([]);
  const [routingPlan, setRoutingPlan] = useState<WorkspaceRoutingPlan | null>(null);
  const [boundWorkspace, setBoundWorkspace] = useState<BoundWorkspace | null>(null);
  const boundWorkspaceRef = useRef<BoundWorkspace | null>(null);
  boundWorkspaceRef.current = boundWorkspace;
  const [sessionGrant, setSessionGrantState] = useState<SessionGrantResponse | null>(() =>
    getPosSessionGrant(),
  );
  const [grantByOrganizationId, setGrantByOrganizationId] = useState<
    Map<string, SessionGrantResponse | null>
  >(() => new Map());
  const grantByOrganizationIdRef = useRef(grantByOrganizationId);
  grantByOrganizationIdRef.current = grantByOrganizationId;
  const [posDevice] = useState<PosDeviceContext>(DEFERRED_POS_DEVICE_CONTEXT);
  const [accessDeniedDetail, setAccessDeniedDetail] = useState<string | null>(null);
  const [bindFailureKind, setBindFailureKind] = useState<WorkspaceBindFailureKind | null>(null);

  const clearBoundWorkspace = useCallback(() => {
    setBoundWorkspace(null);
    setSessionGrantState(null);
    clearPosAccessToken();
    clearPosSessionGrant();
    autoDestinationAttempted.current = false;
  }, []);

  const denyBind = useCallback(
    (
      kind: WorkspaceBindFailureKind,
      technicalDetail: string | null,
      detailKey:
        | "accessDenied.detail"
        | "accessDenied.sessionExpired"
        | "accessDenied.staffOrgLock"
        | "accessDenied.branchNotAccessible"
        | "accessDenied.profileRequired"
        | "accessDenied.serviceUnavailable"
        | "accessDenied.generic",
    ) => {
      setBindFailureKind(kind);
      setAccessDeniedDetail(detailKey);
      if (technicalDetail) {
        console.warn("[workspace-bind]", kind, technicalDetail);
      }
    },
    [],
  );

  const ensureOrganizationSession = useCallback(async () => {
    let activeSession = session;
    if (sessionAccountClass(activeSession) === "Personal") {
      const ensured = await ensureOrganizationSessionProfile({
        session: activeSession,
        refreshSession,
      });
      if (!ensured.ok) {
        denyBind("profile_required", ensured.detail, "accessDenied.profileRequired");
        setStatus("access_denied");
        return null;
      }
      activeSession = ensured.session;
    }
    if (sessionAccountClass(activeSession) !== "Organization") {
      denyBind("profile_required", null, "accessDenied.profileRequired");
      setStatus("access_denied");
      return null;
    }
    return activeSession;
  }, [denyBind, refreshSession, session]);

  const loadWorkspaces = useCallback(async (currentSession: BrowserSessionSnapshot | null) => {
    setStatus("loading");
    setAccessDeniedDetail(null);
    setBindFailureKind(null);

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
      { includeManagementOrgsWithoutBranches: true },
    );
    const plan = resolveWorkspaceRoutingPlan({
      organizationCount: organizationsResult.organizations.length,
      workspaces: accessible,
      accountClass,
    });

    setWorkspaces(accessible);
    setRoutingPlan(plan);

    const nextGrants = new Map<string, SessionGrantResponse | null>();

    // Single-org: probe grant for smart destination routing (no branch inventing).
    if (accessible.length === 1 && accountClass !== "Personal") {
      const only = accessible[0];
      const probed = await probeOrganizationSessionGrant(only.organizationId);
      nextGrants.set(only.organizationId, probed.ok ? probed.grant : null);
    }

    setGrantByOrganizationId(nextGrants);

    const previousBound = boundWorkspaceRef.current;
    let resolvedBound: BoundWorkspace | null = null;
    let resolvedStatus: WorkspaceStatus = "ready";
    let denial: string | null = null;
    let keepGrant = false;

    if (previousBound) {
      const orgStillPresent = accessible.some(
        (workspace) => workspace.organizationId === previousBound.organizationId,
      );
      if (!orgStillPresent) {
        denial = "The previously selected workspace is no longer accessible. Choose again.";
        autoDestinationAttempted.current = false;
      } else if (previousBound.branchId) {
        const branchStillValid = findBranchLabel(
          accessible,
          previousBound.organizationId,
          previousBound.branchId,
        );
        if (!branchStillValid) {
          denial =
            "The previously selected branch is no longer accessible. Choose an active branch.";
          autoDestinationAttempted.current = false;
        } else {
          resolvedBound = {
            ...previousBound,
            organizationDisplayName: branchStillValid.organizationDisplayName,
            branchName: branchStillValid.branchName,
          };
          resolvedStatus = "bound";
          keepGrant = true;
        }
      } else {
        resolvedBound = {
          ...previousBound,
          organizationDisplayName:
            findOrganizationLabel(accessible, previousBound.organizationId) ??
            previousBound.organizationDisplayName,
        };
        resolvedStatus = "bound";
        keepGrant = true;
      }
    }

    if (denial) {
      clearPosAccessToken();
      clearPosSessionGrant();
      setSessionGrantState(null);
      setAccessDeniedDetail(denial);
    } else if (keepGrant) {
      setSessionGrantState(getPosSessionGrant());
      setAccessDeniedDetail(null);
    }

    setBoundWorkspace(resolvedBound);
    setStatus(resolvedStatus);
  }, []);

  const refreshWorkspaces = useCallback(async () => {
    await loadWorkspaces(session);
  }, [loadWorkspaces, session]);

  const ensureOrganizationGrantHint = useCallback(
    async (organizationId: string) => {
      if (grantByOrganizationIdRef.current.has(organizationId)) {
        return grantByOrganizationIdRef.current.get(organizationId) ?? null;
      }

      let activeSession = session;
      if (sessionAccountClass(activeSession) === "Personal") {
        const ensured = await ensureOrganizationSessionProfile({
          session: activeSession,
          refreshSession,
        });
        if (!ensured.ok) {
          setGrantByOrganizationId((prev) => {
            const next = new Map(prev);
            next.set(organizationId, null);
            return next;
          });
          return null;
        }
        activeSession = ensured.session;
      }

      if (sessionAccountClass(activeSession) !== "Organization") {
        setGrantByOrganizationId((prev) => {
          const next = new Map(prev);
          next.set(organizationId, null);
          return next;
        });
        return null;
      }

      const probed = await probeOrganizationSessionGrant(organizationId);
      const grant = probed.ok ? probed.grant : null;
      setGrantByOrganizationId((prev) => {
        const next = new Map(prev);
        next.set(organizationId, grant);
        return next;
      });
      return grant;
    },
    [refreshSession, session],
  );

  const bindDestination = useCallback(
    async (destination: WorkspaceDestination) => {
      if (isOrganizationContextLocked(session)) {
        const homeOrg = session?.homeOrganizationId;
        if (homeOrg && homeOrg !== destination.organizationId) {
          denyBind("staff_org_lock", null, "accessDenied.staffOrgLock");
          setStatus("access_denied");
          return false;
        }
      }

      if (destination.branchId) {
        const accessible = findBranchLabel(
          workspaces,
          destination.organizationId,
          destination.branchId,
        );
        if (!accessible && workspaces.length > 0) {
          denyBind("branch_not_accessible", null, "accessDenied.branchNotAccessible");
          setStatus("access_denied");
          return false;
        }
      }

      setStatus("binding");
      setBindFailureKind(null);
      setAccessDeniedDetail(null);

      const activeSession = await ensureOrganizationSession();
      if (!activeSession) {
        return false;
      }

      const previousBranchId = boundWorkspace?.branchId ?? null;

      if (destination.experience === "manage_business") {
        const result = await bindOrganizationManagementGrant(destination.organizationId);
        if (!result.ok) {
          const classified = classifyWorkspaceBindFailure({
            reason: result.reason,
            status: result.status,
            errorCode: result.body?.errorCode,
            detail: result.body?.detail,
          });
          denyBind(classified.kind, classified.technicalDetail, classified.detailKey);
          setBoundWorkspace(null);
          setStatus(classified.kind === "product_access_denied" ? "access_denied" : "ready");
          return false;
        }
        setBoundWorkspace(boundFromDestination(destination, workspaces));
        setSessionGrantState(result.grant);
        setGrantByOrganizationId((prev) => {
          const next = new Map(prev);
          next.set(destination.organizationId, result.grant);
          return next;
        });
        setBindFailureKind(null);
        setAccessDeniedDetail(null);
        setStatus("bound");
        return true;
      }

      if (!destination.branchId) {
        denyBind("branch_not_accessible", null, "accessDenied.branchNotAccessible");
        setStatus("access_denied");
        return false;
      }

      const result = await bindWorkspaceWithSessionGrant(
        destination.organizationId,
        destination.branchId,
      );
      if (!result.ok) {
        const classified = classifyWorkspaceBindFailure({
          reason: result.reason,
          status: result.status,
          errorCode: result.body?.errorCode,
          detail: result.body?.detail,
        });
        denyBind(classified.kind, classified.technicalDetail, classified.detailKey);
        setBoundWorkspace(null);
        setStatus(classified.kind === "product_access_denied" ? "access_denied" : "ready");
        return false;
      }

      const operational = await selectOperationalBranch({
        organizationId: destination.organizationId,
        branchId: destination.branchId,
        fromBranchId: previousBranchId,
      });
      if (
        !operational.ok &&
        operational.status !== 403 &&
        operational.errorCode !== "application.capability.denied"
      ) {
        const classified = classifyWorkspaceBindFailure({
          status: operational.status,
          errorCode: operational.errorCode,
          detail: operational.detail,
        });
        clearPosAccessToken();
        clearPosSessionGrant();
        setSessionGrantState(null);
        setBoundWorkspace(null);
        denyBind(classified.kind, classified.technicalDetail, classified.detailKey);
        setStatus(classified.kind === "product_access_denied" ? "access_denied" : "ready");
        return false;
      }

      setBoundWorkspace(boundFromDestination(destination, workspaces));
      setSessionGrantState(result.grant);
      setGrantByOrganizationId((prev) => {
        const next = new Map(prev);
        next.set(destination.organizationId, result.grant);
        return next;
      });
      setBindFailureKind(null);
      setAccessDeniedDetail(null);
      setStatus("bound");
      return true;
    },
    [boundWorkspace?.branchId, denyBind, ensureOrganizationSession, session, workspaces],
  );

  const bindWorkspace = useCallback(
    async (organizationId: string, branchId: string) => {
      const experience: WorkingExperience = "start_selling";
      return bindDestination({
        organizationId,
        organizationDisplayName:
          findOrganizationLabel(workspaces, organizationId) ?? organizationId,
        branchId,
        branchName: findBranchLabel(workspaces, organizationId, branchId)?.branchName ?? branchId,
        experience,
        route: "/sell",
        labelKey: "experience.startSelling",
      });
    },
    [bindDestination, workspaces],
  );

  useEffect(() => {
    if (sessionStatus !== "authenticated") {
      setWorkspaces((current) => (current.length === 0 ? current : []));
      setRoutingPlan((current) => (current === null ? current : null));
      setBoundWorkspace((current) => (current === null ? current : null));
      setAccessDeniedDetail((current) => (current === null ? current : null));
      setBindFailureKind((current) => (current === null ? current : null));
      setSessionGrantState((current) => (current === null ? current : null));
      setGrantByOrganizationId((current) => (current.size === 0 ? current : new Map()));
      setStatus((current) => (current === "idle" ? current : "idle"));
      autoDestinationAttempted.current = false;
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
    if (workspaces.length !== 1) {
      return;
    }
    if (autoDestinationAttempted.current) {
      return;
    }

    const onlyOrg = workspaces[0];
    const grant = grantByOrganizationId.get(onlyOrg.organizationId) ?? null;
    const routing = resolveDestinationRouting({
      workspaces,
      grantByOrganizationId: new Map([[onlyOrg.organizationId, grant]]),
    });

    if (routing.outcome !== "AutoDestination") {
      return;
    }

    autoDestinationAttempted.current = true;
    void bindDestination(routing.destination).then((ok) => {
      if (cancelled || !ok) {
        return;
      }
      navigate(routing.destination.route, { replace: true });
    });

    return () => {
      cancelled = true;
    };
  }, [
    bindDestination,
    boundWorkspace,
    grantByOrganizationId,
    navigate,
    session,
    sessionStatus,
    status,
    workspaces,
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
      grantByOrganizationId,
      posDevice,
      accessDeniedDetail,
      bindFailureKind,
      bindWorkspace,
      bindDestination,
      ensureOrganizationGrantHint,
      refreshWorkspaces,
      clearBoundWorkspace,
    }),
    [
      accessDeniedDetail,
      bindDestination,
      bindFailureKind,
      bindWorkspace,
      boundWorkspace,
      clearBoundWorkspace,
      ensureOrganizationGrantHint,
      grantByOrganizationId,
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
