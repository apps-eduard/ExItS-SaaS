/**
 * Legacy pending Personal offline records (PERS-WEB-ONLINE-ONLY-01).
 *
 * Policy: never silently delete potentially unsynchronized Personal outbox rows
 * (Todo / Utang / contact) left by earlier builds that allowed Web offline queueing.
 * While Personal Web is online, LocalStore may still open so OutboxSyncHost can
 * drain existing Pending/Syncing/Conflict rows to the server.
 * New enqueue is blocked by personalWebRuntimePolicy.offlineQueueing === false.
 */
export const PERSONAL_WEB_LEGACY_PENDING_OUTBOX_POLICY =
  "preserve-and-drain-when-online" as const;
