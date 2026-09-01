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
import {
  AUTH_ORGANIZATION_CONTEXT_PATH,
  AUTH_TOKEN_PATH,
  type BrowserSessionSnapshot,
} from "@/api/platform/browser-session";
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
  type WorkspaceBindFailureReason,
} from "@/api/platform/platform-auth-client";
import { clearPlatformAntiforgeryToken } from "@/api/platform/platform-http";
import { selectOperationalBranch } from "@/api/pos/operational-branch-client";
import { getPosDeviceAuthorizationPolicy } from "@/api/pos/pos-runtime-policy-client";
import { issueOfflineOperatingGrant } from "@/api/pos/pos-offline-operating-grant-client";
import {
  buildBoundWorkspaceFromGrant,
  buildColdStartSessionGrantFacts,
  buildPosDeviceFromGrant,
  persistServerSignedGrantFromApi,
} from "@/offline/offline-operating-grant";
import { isOfflinePinAndDekConfigured } from "@/offline/local-store-key";
import { organizationWebAllowsOfflineSession } from "@/runtime/organization-web-runtime-policy";
import { sessionAccountClass, isOrganizationContextLocked } from "@/session/account-class";
import { ensureOrganizationSessionProfile } from "@/session/ensure-organization-profile";
import { isAuthenticatedOrColdStartOffline, useSession } from "@/session/SessionProvider";
import { INITIAL_POS_DEVICE_CONTEXT, type PosDeviceContext } from "@/workspace/pos-device-context";
import { hydratePosDeviceContext } from "@/workspace/hydrate-pos-device";
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
  type WorkspaceBindFailure,
  type WorkspaceBindFailureKind,
} from "@/workspace/workspace-bind-error";
import { normalizePosError } from "@/diagnostics/normalize-pos-error";
import type { PosErrorReportInput } from "@/diagnostics/pos-error-report";
import { PlatformApiError } from "@/api/platform/platform-http";
import {
  resolveDestinationRouting,
  type WorkspaceDestination,
} from "@/workspace/workspace-destinations";
import type { WorkingExperience } from "@/workspace/working-experience";
import { hasPendingPostSubscriptionOnboarding } from "@/features/onboarding/post-subscription-onboarding";

export type WorkspaceGrantProbeFailure = {
  status: number;
  errorCode?: string;
  detail?: string;
};

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
  /** Latest failed session-grant probe per org (does not block retry). */
  grantProbeFailureByOrganizationId: ReadonlyMap<string, WorkspaceGrantProbeFailure>;
  /** Honest device state — never invents an authorized POS terminal. */
  posDevice: PosDeviceContext;
  /**
   * Server PosDeviceAuthorization.EnforcementEnabled.
   * null = not loaded yet (UX treats enforcement as on / fail-closed).
   * false = temporary PWA Local Validation pause — money UX skips device gate.
   */
  deviceEnforcementEnabled: boolean | null;
  /** Re-run durable identity + Platform authorize for the bound org/branch. */
  refreshPosDevice: (options?: { branchId?: string | null }) => Promise<void>;
  accessDeniedDetail: string | null;
  /** Classified bind failure for user-facing copy (null when no denial). */
  bindFailureKind: WorkspaceBindFailureKind | null;
  /** Sanitized copyable diagnostics for the latest bind/load failure. */
  failureDiagnostic: PosErrorReportInput | null;
  /** @deprecated Prefer bindDestination — kept for legacy callers. */
  bindWorkspace: (organizationId: string, branchId: string) => Promise<boolean>;
  bindDestination: (destination: WorkspaceDestination) => Promise<boolean>;
  ensureOrganizationGrantHint: (organizationId: string) => Promise<SessionGrantResponse | null>;
  retryOrganizationGrantHint: (organizationId: string) => Promise<SessionGrantResponse | null>;
  refreshWorkspaces: () => Promise<void>;
  clearBoundWorkspace: () => void;
};

const WorkspaceContext = createContext<WorkspaceContextValue | null>(null);

function workspaceBindFailureDiagnosticContext(reason: WorkspaceBindFailureReason): {
  operation: string;
  httpMethod: "PUT" | "POST";
  path: string;
} {
  switch (reason) {
    case "organization_context":
      return {
        operation: "organization context",
        httpMethod: "PUT",
        path: AUTH_ORGANIZATION_CONTEXT_PATH,
      };
    case "branch_context":
      return {
        operation: "Select branch",
        httpMethod: "PUT",
        path: "/api/v1/platform/organizations/{organizationId}/branch-context",
      };
    case "grant":
      return {
        operation: "workspace session grant",
        httpMethod: "POST",
        path: AUTH_TOKEN_PATH,
      };
    default:
      return {
        operation: "workspace session grant",
        httpMethod: "POST",
        path: AUTH_TOKEN_PATH,
      };
  }
}

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
  const { status: sessionStatus, session, refreshSession, coldStartGrant } = useSession();
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
  const [grantProbeFailureByOrganizationId, setGrantProbeFailureByOrganizationId] = useState<
    Map<string, WorkspaceGrantProbeFailure>
  >(() => new Map());
  const grantProbeFailureByOrganizationIdRef = useRef(grantProbeFailureByOrganizationId);
  grantProbeFailureByOrganizationIdRef.current = grantProbeFailureByOrganizationId;
  const [posDevice, setPosDevice] = useState<PosDeviceContext>(INITIAL_POS_DEVICE_CONTEXT);
  /** null until policy hydrate; fail-closed UX treats null as enforced. */
  const [deviceEnforcementEnabled, setDeviceEnforcementEnabled] = useState<boolean | null>(null);
  const [accessDeniedDetail, setAccessDeniedDetail] = useState<string | null>(null);
  const [bindFailureKind, setBindFailureKind] = useState<WorkspaceBindFailureKind | null>(null);
  const [failureDiagnostic, setFailureDiagnostic] = useState<PosErrorReportInput | null>(null);

  const refreshPosDevice = useCallback(
    async (options?: { branchId?: string | null }) => {
      const bound = boundWorkspaceRef.current;
      if (!isAuthenticatedOrColdStartOffline(sessionStatus)) {
        setPosDevice(INITIAL_POS_DEVICE_CONTEXT);
        return;
      }
      if (sessionStatus === "cold_start_offline") {
        return;
      }
      setPosDevice((current) => ({
        ...current,
        status: "loading",
        registrationStatus: "loading",
        detail: "Resolving browser POS installation identity…",
      }));
      const next = await hydratePosDeviceContext({
        organizationId: bound?.organizationId,
        branchId: options?.branchId !== undefined ? options.branchId : bound?.branchId,
      });
      setPosDevice(next);
    },
    [sessionStatus],
  );

  useEffect(() => {
    if (sessionStatus === "cold_start_offline") {
      return;
    }
    if (!isAuthenticatedOrColdStartOffline(sessionStatus)) {
      setPosDevice(INITIAL_POS_DEVICE_CONTEXT);
      setDeviceEnforcementEnabled(null);
      return;
    }
    void refreshPosDevice();
  }, [boundWorkspace?.organizationId, boundWorkspace?.branchId, refreshPosDevice, sessionStatus]);

  useEffect(() => {
    if (sessionStatus === "cold_start_offline") {
      return;
    }
    if (!isAuthenticatedOrColdStartOffline(sessionStatus) || !boundWorkspace?.organizationId) {
      setDeviceEnforcementEnabled(null);
      return;
    }

    let cancelled = false;
    void getPosDeviceAuthorizationPolicy({
      organizationId: boundWorkspace.organizationId,
      branchId: boundWorkspace.branchId,
    })
      .then((policy) => {
        if (!cancelled) {
          setDeviceEnforcementEnabled(policy.enforcementEnabled);
        }
      })
      .catch(() => {
        // Fail closed: assume enforcement until the server policy is known.
        if (!cancelled) {
          setDeviceEnforcementEnabled(true);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [boundWorkspace?.organizationId, boundWorkspace?.branchId, sessionStatus]);

  useEffect(() => {
    if (sessionStatus !== "cold_start_offline" || !coldStartGrant) {
      return;
    }
    const bound = buildBoundWorkspaceFromGrant(coldStartGrant);
    const device = buildPosDeviceFromGrant(coldStartGrant);
    if (!bound || !device) {
      setStatus("error");
      return;
    }
    setBoundWorkspace(bound);
    setPosDevice(device);
    setSessionGrantState(buildColdStartSessionGrantFacts(coldStartGrant));
    setStatus("bound");
  }, [coldStartGrant, sessionStatus]);

  const clearBoundWorkspace = useCallback(() => {
    setBoundWorkspace(null);
    setSessionGrantState(null);
    clearPosAccessToken();
    clearPosSessionGrant();
    autoDestinationAttempted.current = false;
    // Intentionally do not clear durable installation device id (RMAP-10b).
  }, []);

  const denyBind = useCallback(
    (
      kind: WorkspaceBindFailureKind,
      technicalDetail: string | null,
      detailKey: WorkspaceBindFailure["detailKey"],
      diagnostic?: PosErrorReportInput | null,
    ) => {
      setBindFailureKind(kind);
      setAccessDeniedDetail(detailKey);
      setFailureDiagnostic(diagnostic ?? null);
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

  const loadGenerationRef = useRef(0);
  const loadInFlightRef = useRef<Promise<void> | null>(null);
  const sessionRef = useRef(session);
  sessionRef.current = session;

  const loadWorkspaces = useCallback(async (currentSession: BrowserSessionSnapshot | null) => {
    const generation = ++loadGenerationRef.current;
    const hadBound = Boolean(boundWorkspaceRef.current);
    // Keep a valid bind visible during background refresh so branch/org guards
    // never flash endless "Checking session…" over an already-bound workspace.
    if (!hadBound) {
      setStatus("loading");
    }
    setAccessDeniedDetail(null);
    setBindFailureKind(null);
    setFailureDiagnostic(null);
    setGrantProbeFailureByOrganizationId(new Map());

    const accountClass = sessionAccountClass(currentSession);

    const organizationsResult = await listEligibleOrganizations();
    if (generation !== loadGenerationRef.current) {
      return;
    }
    if (!organizationsResult.ok) {
      setFailureDiagnostic(
        normalizePosError({
          source: "workspace",
          error: new PlatformApiError(
            organizationsResult.status,
            organizationsResult.body ?? {},
          ),
          operation: "workspace bootstrap",
          httpMethod: "GET",
          path: "/api/v1/platform/auth/organizations",
          screen: "Choose workspace",
          accountClass: accountClass ?? undefined,
        }),
      );
      setStatus("error");
      setWorkspaces([]);
      setRoutingPlan(null);
      return;
    }

    const organizations = Array.isArray(organizationsResult.organizations)
      ? organizationsResult.organizations
      : [];
    const branchesByOrganizationId = new Map<string, PlatformBranch[]>();
    for (const organization of organizations) {
      const branchesResult = await listOrganizationBranches(organization.organizationId);
      if (generation !== loadGenerationRef.current) {
        return;
      }
      branchesByOrganizationId.set(
        organization.organizationId,
        branchesResult.ok ? branchesResult.branches : [],
      );
    }

    const accessible = buildAccessibleWorkspaces(organizations, branchesByOrganizationId, {
      includeManagementOrgsWithoutBranches: true,
    });
    const plan = resolveWorkspaceRoutingPlan({
      organizationCount: organizations.length,
      workspaces: accessible,
      accountClass,
    });

    setWorkspaces(accessible);
    setRoutingPlan(plan);

    const nextGrants = new Map<string, SessionGrantResponse | null>();
    const nextFailures = new Map<string, WorkspaceGrantProbeFailure>();

    // Single-org: probe grant for smart destination routing after offline PIN is configured.
    if (
      accessible.length === 1 &&
      accountClass !== "Personal" &&
      currentSession?.userId &&
      isOfflinePinAndDekConfigured(currentSession.userId)
    ) {
      const only = accessible[0];
      const probed = await probeOrganizationSessionGrant(only.organizationId);
      if (generation !== loadGenerationRef.current) {
        return;
      }
      if (probed.ok) {
        nextGrants.set(only.organizationId, probed.grant);
      } else {
        nextFailures.set(only.organizationId, {
          status: probed.status,
          errorCode: probed.body?.errorCode,
          detail: probed.body?.detail,
        });
        console.warn(
          "[workspace-grant-probe]",
          only.organizationId,
          probed.status,
          probed.body?.errorCode,
        );
      }
    }

    setGrantByOrganizationId(nextGrants);
    setGrantProbeFailureByOrganizationId(nextFailures);

    if (generation !== loadGenerationRef.current) {
      return;
    }

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
        denial = "workspace.previousWorkspaceInaccessible";
        autoDestinationAttempted.current = false;
      } else if (previousBound.branchId) {
        const branchStillValid = findBranchLabel(
          accessible,
          previousBound.organizationId,
          previousBound.branchId,
        );
        if (!branchStillValid) {
          denial = "workspace.previousBranchInaccessible";
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
    const existing = loadInFlightRef.current;
    if (existing) {
      await existing;
      return;
    }
    // Read session from a ref so isomorphic session object replacements do not
    // recreate this callback and re-trigger the bootstrap effect on every nav.
    const run = loadWorkspaces(sessionRef.current).finally(() => {
      if (loadInFlightRef.current === run) {
        loadInFlightRef.current = null;
      }
    });
    loadInFlightRef.current = run;
    await run;
  }, [loadWorkspaces]);

  const ensureOrganizationGrantHint = useCallback(
    async (organizationId: string, options?: { force?: boolean }) => {
      if (!options?.force) {
        const cachedGrant = grantByOrganizationIdRef.current.get(organizationId);
        if (cachedGrant) {
          return cachedGrant;
        }
        if (grantProbeFailureByOrganizationIdRef.current.has(organizationId)) {
          return null;
        }
      } else {
        setGrantProbeFailureByOrganizationId((prev) => {
          const next = new Map(prev);
          next.delete(organizationId);
          return next;
        });
        setGrantByOrganizationId((prev) => {
          const next = new Map(prev);
          next.delete(organizationId);
          return next;
        });
      }

      let activeSession = session;
      if (sessionAccountClass(activeSession) === "Personal") {
        const ensured = await ensureOrganizationSessionProfile({
          session: activeSession,
          refreshSession,
        });
        if (!ensured.ok) {
          setGrantProbeFailureByOrganizationId((prev) => {
            const next = new Map(prev);
            next.set(organizationId, {
              status: 403,
              detail: ensured.detail,
              errorCode: "application.auth.profile_required",
            });
            return next;
          });
          return null;
        }
        activeSession = ensured.session;
      }

      if (sessionAccountClass(activeSession) !== "Organization") {
        setGrantProbeFailureByOrganizationId((prev) => {
          const next = new Map(prev);
          next.set(organizationId, {
            status: 403,
            errorCode: "application.auth.profile_required",
          });
          return next;
        });
        return null;
      }

      const probed = await probeOrganizationSessionGrant(organizationId);
      if (!probed.ok) {
        setGrantProbeFailureByOrganizationId((prev) => {
          const next = new Map(prev);
          next.set(organizationId, {
            status: probed.status,
            errorCode: probed.body?.errorCode,
            detail: probed.body?.detail,
          });
          return next;
        });
        console.warn(
          "[workspace-grant-probe]",
          organizationId,
          probed.status,
          probed.body?.errorCode,
        );
        return null;
      }

      setGrantByOrganizationId((prev) => {
        const next = new Map(prev);
        next.set(organizationId, probed.grant);
        return next;
      });
      setGrantProbeFailureByOrganizationId((prev) => {
        const next = new Map(prev);
        next.delete(organizationId);
        return next;
      });
      return probed.grant;
    },
    [refreshSession, session],
  );

  const retryOrganizationGrantHint = useCallback(
    async (organizationId: string) => ensureOrganizationGrantHint(organizationId, { force: true }),
    [ensureOrganizationGrantHint],
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
      setFailureDiagnostic(null);

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
          denyBind(
            classified.kind,
            classified.technicalDetail,
            classified.detailKey,
            normalizePosError({
              source: "workspace",
              error: new PlatformApiError(result.status, result.body ?? {}),
              operation: "workspace management grant",
              httpMethod: "POST",
              path: "/api/v1/platform/auth/token",
              screen: "Choose workspace",
              accountClass: sessionAccountClass(activeSession) ?? undefined,
              organizationName:
                findOrganizationLabel(workspaces, destination.organizationId) ?? undefined,
            }),
          );
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
        const diagnosticContext = workspaceBindFailureDiagnosticContext(result.reason);
        denyBind(
          classified.kind,
          classified.technicalDetail,
          classified.detailKey,
          normalizePosError({
            source: "workspace",
            error: new PlatformApiError(result.status, result.body ?? {}),
            operation: diagnosticContext.operation,
            httpMethod: diagnosticContext.httpMethod,
            path: diagnosticContext.path,
            screen: "Choose workspace",
            accountClass: sessionAccountClass(activeSession) ?? undefined,
            organizationName:
              findOrganizationLabel(workspaces, destination.organizationId) ?? undefined,
            branchName: destination.branchName ?? undefined,
          }),
        );
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
        denyBind(
          classified.kind,
          classified.technicalDetail,
          classified.detailKey,
          normalizePosError({
            source: "workspace",
            error: new Error(operational.detail ?? "Operational branch bind failed"),
            operation: "operational branch bind",
            httpMethod: "PUT",
            path: "/api/v1/pos/operational-branch",
            screen: "Choose workspace",
            accountClass: sessionAccountClass(activeSession) ?? undefined,
            organizationName:
              findOrganizationLabel(workspaces, destination.organizationId) ?? undefined,
            branchName: destination.branchName ?? undefined,
            status: operational.status,
            errorCode: operational.errorCode,
          }),
        );
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
      setFailureDiagnostic(null);
      setStatus("bound");

      if (previousBranchId && previousBranchId !== destination.branchId) {
        void queryClient.invalidateQueries({ queryKey: ["customers"] });
        void queryClient.invalidateQueries({ queryKey: ["suppliers"] });
      }

      const hydratedDevice = await hydratePosDeviceContext({
        organizationId: destination.organizationId,
        branchId: destination.branchId,
      });
      setPosDevice(hydratedDevice);
      if (
        destination.branchId &&
        activeSession.userId &&
        hydratedDevice.status === "authorized" &&
        hydratedDevice.installationDeviceId &&
        hydratedDevice.posDeviceId &&
        organizationWebAllowsOfflineSession()
      ) {
        void issueOfflineOperatingGrant(
          {
            organizationId: destination.organizationId,
            branchId: destination.branchId,
          },
          {
            installationDeviceId: hydratedDevice.installationDeviceId,
            organizationDisplayName:
              findOrganizationLabel(workspaces, destination.organizationId) ??
              destination.organizationDisplayName,
            branchName:
              destination.branchName ??
              findBranchLabel(workspaces, destination.organizationId, destination.branchId)
                ?.branchName ??
              destination.branchId,
            displayName: activeSession.displayName ?? null,
            username: activeSession.username ?? null,
          },
        )
          .then((grant) => {
            persistServerSignedGrantFromApi(grant);
          })
          .catch((error: unknown) => {
            console.warn("[offline-grant] server grant issuance failed", error);
          });
      }

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
    if (sessionStatus === "cold_start_offline") {
      return;
    }
    if (sessionStatus !== "authenticated") {
      setWorkspaces((current) => (current.length === 0 ? current : []));
      setRoutingPlan((current) => (current === null ? current : null));
      setBoundWorkspace((current) => (current === null ? current : null));
      setAccessDeniedDetail((current) => (current === null ? current : null));
      setBindFailureKind((current) => (current === null ? current : null));
      setSessionGrantState((current) => (current === null ? current : null));
      setGrantByOrganizationId((current) => (current.size === 0 ? current : new Map()));
      setGrantProbeFailureByOrganizationId((current) =>
        current.size === 0 ? current : new Map(),
      );
      setStatus((current) => (current === "idle" ? current : "idle"));
      autoDestinationAttempted.current = false;
      return;
    }

    void refreshWorkspaces();
  }, [refreshWorkspaces, sessionStatus]);

  useEffect(() => {
    let cancelled = false;
    if (sessionStatus !== "authenticated" || status !== "ready" || boundWorkspace) {
      return;
    }
    if (sessionAccountClass(session) === "Personal") {
      return;
    }
    try {
      if (hasPendingPostSubscriptionOnboarding()) {
        return;
      }
    } catch {
      // sessionStorage can throw in restricted browser contexts.
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
      grantProbeFailureByOrganizationId,
      posDevice,
      deviceEnforcementEnabled,
      refreshPosDevice,
      accessDeniedDetail,
      bindFailureKind,
      failureDiagnostic,
      bindWorkspace,
      bindDestination,
      ensureOrganizationGrantHint,
      retryOrganizationGrantHint,
      refreshWorkspaces,
      clearBoundWorkspace,
    }),
    [
      accessDeniedDetail,
      bindDestination,
      bindFailureKind,
      failureDiagnostic,
      bindWorkspace,
      boundWorkspace,
      clearBoundWorkspace,
      deviceEnforcementEnabled,
      ensureOrganizationGrantHint,
      grantByOrganizationId,
      grantProbeFailureByOrganizationId,
      posDevice,
      refreshPosDevice,
      refreshWorkspaces,
      retryOrganizationGrantHint,
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
