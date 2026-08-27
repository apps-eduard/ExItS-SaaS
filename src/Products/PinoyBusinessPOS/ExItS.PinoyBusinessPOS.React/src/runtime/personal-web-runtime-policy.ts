/**
 * Authoritative Personal Web/PWA runtime policy (PERS-WEB-ONLINE-ONLY-01).
 *
 * Personal business operations on the browser/PWA channel require a live
 * server connection. The offline engine (LocalStore, encrypted outbox, grants,
 * PIN/DEK, Todo/Utang queue models) remains in the codebase for future
 * Capacitor/native use — this policy only disables Web/PWA *activation* of
 * those capabilities for new Personal operations.
 *
 * Do not scatter `navigator.onLine` checks across Personal pages; consume
 * this policy (and the connectivity layer) instead.
 *
 * PERS-IDEM-01 remains required for online network-ambiguous money mutations.
 */
export type PersonalWebRuntimePolicy = {
  /** Live authenticated session required to operate Personal surfaces. */
  readonly requiresOnlineSession: true;
  /** Cold-start / grant-based Personal offline session. */
  readonly offlineSession: false;
  /** Serve Personal reads from LocalStore while offline. */
  readonly offlineBusinessReads: false;
  /** Perform Personal business mutations while offline. */
  readonly offlineBusinessMutations: false;
  /** Enqueue new Personal outbox operations (Todo / Utang / People). */
  readonly offlineQueueing: false;
  /** Background-sync Personal mutations as an offline operating mode. */
  readonly offlineBackgroundSync: false;
};

export const personalWebRuntimePolicy: PersonalWebRuntimePolicy = {
  requiresOnlineSession: true,
  offlineSession: false,
  offlineBusinessReads: false,
  offlineBusinessMutations: false,
  offlineQueueing: false,
  offlineBackgroundSync: false,
};

/** True when Personal Web may enter an offline operating session. */
export function personalWebAllowsOfflineSession(): boolean {
  return personalWebRuntimePolicy.offlineSession;
}

/** True when Personal Web may enqueue new offline outbox operations. */
export function personalWebAllowsOfflineQueueing(): boolean {
  return personalWebRuntimePolicy.offlineQueueing;
}

/** True when Personal Web may use LocalStore as an offline read path. */
export function personalWebAllowsOfflineBusinessReads(): boolean {
  return personalWebRuntimePolicy.offlineBusinessReads;
}

/** True when Personal Web may perform business mutations while offline. */
export function personalWebAllowsOfflineBusinessMutations(): boolean {
  return personalWebRuntimePolicy.offlineBusinessMutations;
}

/**
 * Fail-closed guard for Personal Web enqueue paths.
 * Engine unit tests that exercise enqueue directly should not call this;
 * Web/PWA entry points and shared enqueue wrappers must.
 */
export function assertPersonalWebAllowsOfflineQueueing(): void {
  if (!personalWebAllowsOfflineQueueing()) {
    throw new PersonalWebOnlineOnlyError();
  }
}

export class PersonalWebOnlineOnlyError extends Error {
  readonly code = "personal_web.online_only" as const;

  constructor(message = "Personal Web/PWA requires an internet connection.") {
    super(message);
    this.name = "PersonalWebOnlineOnlyError";
  }
}

export type PersonalOfflineEnqueueRuntimeOptions = {
  /**
   * Opt into the preserved offline engine (unit tests / future Capacitor).
   * Personal Web/PWA must never pass this for user-driven actions.
   */
  allowOfflineEngine?: boolean;
};

/** Fail closed unless Web policy allows queueing or the caller opts into the engine. */
export function guardPersonalWebOfflineEnqueue(
  options?: PersonalOfflineEnqueueRuntimeOptions,
): void {
  if (options?.allowOfflineEngine) {
    return;
  }
  assertPersonalWebAllowsOfflineQueueing();
}
