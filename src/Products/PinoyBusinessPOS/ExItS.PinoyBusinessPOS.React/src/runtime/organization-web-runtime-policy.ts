/**
 * Authoritative Organization Web/PWA runtime policy (ORG-PWA-ONLINE-ONLY-01).
 *
 * Organization business operations on the browser/PWA channel require a live
 * server connection. The offline engine (LocalStore, outbox, grants, PIN/DEK,
 * reconciliation) remains in the codebase for future Capacitor/native use —
 * this policy only disables Web/PWA *activation* of those capabilities.
 *
 * Do not scatter `navigator.onLine` checks across Organization pages; consume
 * this policy (and the connectivity layer) instead.
 */
export type OrganizationWebRuntimePolicy = {
  /** Live authenticated session required to operate Organization surfaces. */
  readonly requiresOnlineSession: true;
  /** Cold-start / grant-based Organization offline session. */
  readonly offlineSession: false;
  /** Serve Organization business reads from LocalStore while offline. */
  readonly offlineBusinessReads: false;
  /** Perform Organization business mutations while offline. */
  readonly offlineBusinessMutations: false;
  /** Complete money transactions without server confirmation. */
  readonly offlineTransactions: false;
  /** Enqueue new Organization outbox financial/business operations. */
  readonly offlineQueueing: false;
  /** Background-sync Organization mutations as an offline operating mode. */
  readonly offlineBackgroundSync: false;
};

export const organizationWebRuntimePolicy: OrganizationWebRuntimePolicy = {
  requiresOnlineSession: true,
  offlineSession: false,
  offlineBusinessReads: false,
  offlineBusinessMutations: false,
  offlineTransactions: false,
  offlineQueueing: false,
  offlineBackgroundSync: false,
};

/** True when Organization Web may enter an offline operating session. */
export function organizationWebAllowsOfflineSession(): boolean {
  return organizationWebRuntimePolicy.offlineSession;
}

/** True when Organization Web may enqueue new offline outbox operations. */
export function organizationWebAllowsOfflineQueueing(): boolean {
  return organizationWebRuntimePolicy.offlineQueueing;
}

/** True when Organization Web may use LocalStore as an offline read path. */
export function organizationWebAllowsOfflineBusinessReads(): boolean {
  return organizationWebRuntimePolicy.offlineBusinessReads;
}

/** True when Organization Web may perform business mutations while offline. */
export function organizationWebAllowsOfflineBusinessMutations(): boolean {
  return organizationWebRuntimePolicy.offlineBusinessMutations;
}


/**
 * Fail-closed guard for Organization Web enqueue paths.
 * Engine unit tests that exercise enqueue directly should not call this;
 * Web/PWA entry points and shared enqueue wrappers must.
 */
export function assertOrganizationWebAllowsOfflineQueueing(): void {
  if (!organizationWebAllowsOfflineQueueing()) {
    throw new OrganizationWebOnlineOnlyError();
  }
}

export class OrganizationWebOnlineOnlyError extends Error {
  readonly code = "organization_web.online_only" as const;

  constructor(message = "Organization Web/PWA requires an internet connection.") {
    super(message);
    this.name = "OrganizationWebOnlineOnlyError";
  }
}

export type OfflineEnqueueRuntimeOptions = {
  /**
   * Opt into the preserved offline engine (unit tests / future Capacitor).
   * Organization Web/PWA must never pass this for user-driven actions.
   */
  allowOfflineEngine?: boolean;
};

/** Fail closed unless Web policy allows queueing or the caller opts into the engine. */
export function guardOrganizationWebOfflineEnqueue(options?: OfflineEnqueueRuntimeOptions): void {
  if (options?.allowOfflineEngine) {
    return;
  }
  assertOrganizationWebAllowsOfflineQueueing();
}
