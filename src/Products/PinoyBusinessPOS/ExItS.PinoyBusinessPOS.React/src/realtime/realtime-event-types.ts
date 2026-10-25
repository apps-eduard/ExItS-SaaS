/** Stable Platform RealtimeEvent envelope EventType values. Keep aligned with RealtimeEventTypes.cs. */
export const RealtimeEventTypes = {
  OrganizationSettingsChanged: "OrganizationSettingsChanged",
  BranchSettingsChanged: "BranchSettingsChanged",
  BranchOperatingHoursChanged: "BranchOperatingHoursChanged",
  BranchFulfillmentSettingsChanged: "BranchFulfillmentSettingsChanged",
  BranchDeliveryPolicyChanged: "BranchDeliveryPolicyChanged",
  BranchDeliveryAreasChanged: "BranchDeliveryAreasChanged",
  StorefrontAvailabilityChanged: "StorefrontAvailabilityChanged",
  PersonalSettingsChanged: "PersonalSettingsChanged",
  PersonalProfilePhotoChanged: "PersonalProfilePhotoChanged",
  MembershipAccessChanged: "MembershipAccessChanged",
  PersonalUtangEntryChanged: "PersonalUtangEntryChanged",
  PersonalUtangInvitationChanged: "PersonalUtangInvitationChanged",
  PersonalNotificationChanged: "PersonalNotificationChanged",
} as const;

export type RealtimeEventType = (typeof RealtimeEventTypes)[keyof typeof RealtimeEventTypes];

export type RealtimeEventEnvelope = {
  eventId: string;
  eventType: string;
  schemaVersion: number;
  occurredAtUtc: string;
  resourceType: string;
  resourceId: string;
  scopeType: string;
  scopeId?: string | null;
  resourceVersion?: string | null;
  correlationId?: string | null;
  data?: Record<string, string> | null;
};

/** Normalize ASP.NET camelCase or PascalCase envelope payloads. */
export function normalizeRealtimeEnvelope(raw: unknown): RealtimeEventEnvelope | null {
  if (!raw || typeof raw !== "object") return null;
  const r = raw as Record<string, unknown>;
  const pick = (a: string, b: string) => (r[a] ?? r[b]) as string | undefined;
  const eventType = pick("eventType", "EventType");
  const resourceType = pick("resourceType", "ResourceType");
  const resourceId = pick("resourceId", "ResourceId");
  const scopeType = pick("scopeType", "ScopeType");
  if (!eventType || !resourceType || !resourceId || !scopeType) return null;
  return {
    eventId: String(pick("eventId", "EventId") ?? ""),
    eventType,
    schemaVersion: Number(r.schemaVersion ?? r.SchemaVersion ?? 1),
    occurredAtUtc: String(pick("occurredAtUtc", "OccurredAtUtc") ?? ""),
    resourceType,
    resourceId,
    scopeType,
    scopeId: (pick("scopeId", "ScopeId") as string | null | undefined) ?? null,
    resourceVersion: (pick("resourceVersion", "ResourceVersion") as string | null | undefined) ?? null,
    correlationId: (pick("correlationId", "CorrelationId") as string | null | undefined) ?? null,
    data: (r.data ?? r.Data ?? null) as Record<string, string> | null,
  };
}
