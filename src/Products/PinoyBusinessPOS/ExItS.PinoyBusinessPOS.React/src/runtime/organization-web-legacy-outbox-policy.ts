/**
 * Legacy pending Organization offline records (ORG-PWA-ONLINE-ONLY-01).
 *
 * Policy: never silently delete potentially unsynchronized financial outbox rows.
 * While Organization Web is online, LocalStore may still open so OutboxSyncHost can
 * drain existing Pending/Syncing/Conflict rows to the server.
 * New enqueue is blocked by organizationWebRuntimePolicy.offlineQueueing === false.
 */
export const ORGANIZATION_WEB_LEGACY_PENDING_OUTBOX_POLICY =
  "preserve-and-drain-when-online" as const;
