# Platform–Product Contract Matrix

[Contracts](platform-product-contracts.md) | [Entitlement states](entitlement-state-matrix.md) | [ADR-012](../decisions/ADR-012-versioned-platform-contracts-and-local-projections.md)

| Contract or Data | Producer | Consumer | Authority | Delivery Style | Versioning | Idempotency | Sensitive Fields Excluded | Failure Behavior |
|---|---|---|---|---|---|---|---|---|
| Identity projection | Platform | HC, POS, Admin | Platform | Event + optional sync pull | IdentityVersion / schema | EventId / version | Passwords, tokens, MFA, login dumps | Keep last good projection; suspend access on suspension events |
| Organization projection | Platform | HC, POS | Platform | Event + optional sync pull | ProjectionVersion | EventId / version | Internal billing secrets | Stale within policy; then constrain |
| Membership projection | Platform | HC, POS | Platform | Event | Membership version | EventId | Extra PII | Deny new product access if never initialized |
| Product activation | Platform | Products | Platform | Event / admin API | Catalog compatibility version | EventId | — | Unknown product code → ignore/quarantine |
| Subscription state | Platform | Products | Platform | Event | Subscription Version | EventId / AggregateVersion | Card PAN/secrets | Enforce via entitlements; do not invent Active |
| Entitlement snapshot | Platform | Products | Platform | Event or reconcil. pull | EntitlementVersion + SchemaVersion | SnapshotId / EventId | Clinical/POS ops payloads | Never initialized → no paid features |
| Entitlement update | Platform | Products | Platform | Async event (preferred) | EntitlementVersion | EventId; reject older | Same | Out-of-order → buffer/reconcile |
| Payment confirmation (SaaS) | Platform | Products (status), Admin | Platform | Event after verify | Payment Version | SaaSPaymentId / EventId | Full payment instrument data | Duplicate confirm → no-op |
| POS retail / credit payment (cash, gcash) | POS | POS only | POS | Product-local (offline OK) | Product schema later | Local payment id; sync idempotent | GCash secrets; Platform SaaS fields | Manual GCash; ref warn on dup (OD-11) |
| User suspension | Platform | Products | Platform | Async event + sync on login | IdentityVersion | EventId | — | Fail closed on protected ops when applied |
| Organization suspension | Platform | Products | Platform | Async event | Org ProjectionVersion | EventId | — | Block entitlement-protected writes |
| Reconciliation snapshot | Platform | Product admin/runtime | Platform | Sync admin API | Snapshot + schema | Request idempotency key | Same as snapshot | Replace commercial projection only |
| Audit correlation metadata | Any | Peer audit stores | Split | Propagate IDs only | N/A | CorrelationId | PHI, remarks, line items, secrets | Missing correlation → still audit locally |
| Token issuance | Platform | Clients | Platform | Sync API | Token/API later | — | Refresh material not to products DB | Login fails if Platform down |
| Trial expiry effect | Platform | POS (esp.) | Platform | Entitlement/subscription events | EntitlementVersion | EventId | — | Enforce allowed/blocked trial matrix |
