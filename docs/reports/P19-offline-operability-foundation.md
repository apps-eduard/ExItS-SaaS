# P19 — Offline Operability Foundation (Cold-Start PIN + Offline Cash Sale)

| Field | Value |
|---|---|
| Status | **Code Complete** · Physical Android A–S validation **In Progress / Incomplete** |
| Phase | [Phase 19](../phases/phase-19-mobile-pos-operations-and-cashier-experience.md) — **Open** |
| Closeout checklist | [P19-WP08](P19-WP08-end-to-end-validation-and-closeout.md) — **Retest** |
| Device Verified | **No** |
| Production Ready | **No** |
| Date | 2026-08-08 |
| Feature commits | `f476172` (grant + PIN cold-start), `cc64ba3` (local catalog + offline cash), `10a1fc5` (PhysicalDevice unlock hang fix) |

## 1. Objective

Keep a previously authorized POS device usable during internet outages (including after app process death / cold start) without weakening organization or role security. Reuse Phase 7 LocalStore / outbox patterns; do not redesign wholesale auth or invent a second POS role system.

## 2. Explicit status (do not over-claim)

| Claim | Value |
|---|---|
| Phase 19 | **Open** |
| Device Verified | **No** |
| Production Ready | **No** |
| Physical Android A–S | **Incomplete** — partial Galaxy A52 evidence recorded; sync + negative PIN lockout not closed |
| Emulator-only proof | Insufficient for Device Verified |

Do **not** mark physical-device validation complete until the full A–S checklist in §12 passes and the user confirms phone validation per P19-WP08.

## 3. Original cold-start failure

On cold start while the server was unreachable, `AuthenticationService.RestoreBearerSessionAsync` treated **network / transport failure like authorization denial** (`HasPosAccess=false`, reconnect-required). `ProtectedShellAccessPolicy` only trusted process-lifetime **online** validation, so restart offline always forced `/reconnect` even when a durable offline operate grant existed on the device.

## 4. Unreachable vs explicit authorization denial

| Server outcome | Client behavior |
|---|---|
| Transport failure / timeout / unavailable (unreachable) | Offer offline PIN unlock when a valid grant + PIN exist; do **not** clear the grant |
| Explicit denial (`Active:false`, access revoked, forbidden assignment) | Clear offline operate grant; fail closed to reconnect / sign-in |
| Successful online validation | Refresh / establish grant from the online session |

PIN unlock never creates or extends the grant window.

## 5. Offline operating grant + expiry

Durable grant (`OfflineOperatingGrant`) in secure storage:

- Established only after successful online org/POS validation
- Bound to user, organization, device id, optional role snapshot, commercial feature codes, subscription status snapshot
- **No** passwords, raw bearer tokens, or refresh secrets in the grant
- Time-bounded (`ExpiresAtUtc`; duration from `OfflineOperatingGrant` options)
- Cleared on logout and on explicit server denial
- Schema versioned; expired grants cannot unlock

## 6. PIN security / throttling

| Rule | Behavior |
|---|---|
| Format | Numeric PIN, minimum 6 digits |
| Storage | PBKDF2 verifier (salt + hash) in secure storage — not reversible PIN |
| Wrong attempts | Incremented; lockout after configured max failures |
| Lockout | Temporary `LockedUntilUtc`; does **not** delete or extend the operate grant |
| Success | Resets attempt counter; unlock is process-scoped (`IsUnlockedThisProcess`) |

UI: Settings (online) for optional PIN change; `/offline-pin-setup` for **mandatory** enrollment after online auth + setup when no PIN exists; `/offline-pin` for unlock; Sign-in surfaces **Use PIN** when eligible; Reconnect surfaces PIN offer when grant is valid.

## 6b. Auth UX layer (mandatory PIN + login readiness)

| Topic | Behavior |
|---|---|
| Mandatory PIN enrollment | After successful online auth and Personal/Org/setup completion, if this device has no PIN and a valid operate grant exists → force `/offline-pin-setup` (no Skip / Maybe later) |
| Existing-user migration | Existing users who never enrolled a PIN are gated the same way on next online POS entry |
| Use PIN on Sign-in | Shown only when enrolled PIN + usable offline operate grant/session identity exist on this device |
| Offline Sign-in | Use PIN is primary; username/password and Google/Facebook placeholders show Internet-required messaging (do not fake auth) |
| Offline warning | Once per unlocked offline session: “You're working offline” + device-data warning → Continue offline |
| Top-bar sync | `ShellSyncStatus`: Offline • N waiting; panel shows device storage copy + Retry connection; Syncing… then Online / All changes synced |
| Online-required guard | Shared `OnlineRequiredGuard` + dialog; stays in context; does **not** destroy offline session or bounce to Reconnect for ordinary online-only actions |
| Lock vs Sign out | **Lock** keeps grant + PIN, returns to PIN unlock. **Sign out** clears operate grant (PIN verifier may remain); next auth requires internet |
| Google / Facebook | UI-ready placeholders only (shared provider button style + icons). **Real OAuth is not implemented** and is explicitly deferred |

PIN remains per **user on this device**, not per organization. Offline grant/context still determines org, role, permissions, and expiry.

## 7. Device / org / permission binding

- Device id from existing `IDeviceIdentityProvider` must match grant `DeviceId`
- User id and organization id must match the restored session / grant
- Org preference mismatch or user mismatch → deny PIN offer
- Effective capabilities while offline use the **last online permission / feature snapshot** on the session/grant — not a live permissions round-trip
- After unlock, home routing uses grant role snapshot + Owner working-as preference (`RoleHomeResolver.ResolveFromOfflineGrantSnapshot`) so unlock does not hang on permissions HTTP

PhysicalDevice Debug connectivity: `NetworkAccess.None` is treated as **offline** (emulator-only exception remains for adb-reverse Local Validation).

## 8. Local catalog cache

Local schema **v5** tables (LocalStore):

- `local_catalog_categories` / `local_catalog_products`
- `local_open_shift_snapshot`
- `local_cash_sale` (+ related line/inventory local records as implemented)

`LocalSellingCatalogSyncService.RefreshFromServerAsync` runs while online (e.g. Sell checkout init) to replace the cached active catalog and capture the open-shift snapshot for offline continuity.

Offline Sell loads categories/products and open-shift snapshot from LocalStore when connectivity reports offline.

## 9. Offline cash sale atomic persistence

- Cash checkout only when offline operate grant is unlocked and open-shift snapshot is present
- `SaleCheckoutOfflineDispatcher` / local cash-sale store persist sale + outbox enqueue atomically for the local context
- Idempotency keys prevent duplicate local/server application on retry
- Unit coverage: persist, idempotency, restart survival, sync-failure survival (`LocalCashSaleOfflineStoreTests`)

## 10. Local inventory deduction

For tracked products, offline cash sale applies local on-hand deduction with the sale persistence path. Untracked products (template “Stock not tracked”) sell without stock gating. Server reconciliation remains via outbox sync when online.

## 11. Receipt / pending-sync behavior

- Successful offline cash sale navigates to a local/pending receipt surface (e.g. `LocalSaleReceipt` / “Sale completed - pending sync”)
- Local sale number format observed on device: `OFF-…`
- Shell sync status shows pending outbox count (“Pending sync (N items)”) until processor succeeds
- Pending count survives app force-stop / cold start while still offline

## 12. Outbox / idempotency / reconnect

Reuses Phase 7 encrypted `offline_operations` outbox + `OfflineQueueProcessor`:

- Enqueue at local cash-sale commit
- Claim / sync when connectivity returns
- Idempotent server apply; failure classes preserve pending work for retry
- Reconnect UI remains available; offline PIN path is an alternative when grant is valid

## 13. Supported offline capabilities

| Capability | Notes |
|---|---|
| Cold-start offline PIN unlock | After prior online grant + PIN setup |
| Cached catalog browse / search | Last online refresh |
| Open-shift continuity | Snapshot only — cannot open a **new** shift offline without snapshot |
| Offline cash checkout | Local persist + outbox |
| Local pending receipt | Until sync |
| Existing customer/credit offline paths | Prior Phase 7 capability unchanged |

## 14. Intentionally online-only

Staff/org admin, billing, global catalog import / business-template import, supplier linking, permission changes, reports, Card / GCash / ManualGCash / Utang checkout, opening a new shift when no local snapshot exists, sales history that requires server list APIs.

## 15. Revocation behavior

| Event | Effect |
|---|---|
| Explicit server access denial / inactive | Clear operate grant; protected shell denied |
| Logout | Clear grant (PIN verifier may remain for reuse after next online establish); next authentication requires internet |
| Lock | Keep grant + PIN; clear process unlock; return to PIN unlock |
| Grant expiry | PIN unlock denied (`Offline_GrantExpired`) |
| Device id mismatch | PIN unlock denied |

## 16. Automated tests (focused)

| Area | Evidence |
|---|---|
| Grant / PIN / cold-start auth | `OfflineOperatingGrantServiceTests`, cold-start cases in `AuthenticationServiceTests` |
| Auth UX layer (enrollment / Use PIN / Lock / warning / guard) | `AuthOfflineUxLayerTests` |
| Offline home without permissions HTTP | `RoleHomeResolverTests` (grant snapshot; zero `GetEffective` calls) |
| Local cash sale | `LocalCashSaleOfflineStoreTests` (persist, idempotency, restart, sync-failure survival) |
| Sales offline architecture | `PosSalesScopeArchitectureTests` (cash offline path allowed) |
| Maui offline/auth filter (prior run) | Focused Maui suite green during implementation |

Physical Android A–S remains **incomplete** — do not mark complete from this auth UX layer alone.

Full-solution Release totals are not re-stated here; re-run before push per repository workflow.

## 17. Physical Android A–S validation status

**Incomplete — do not mark Device Verified.**

Device used for **partial** evidence (Debug PhysicalDevice + Local Validation + Tailscale):

| Item | Value |
|---|---|
| Device | Samsung Galaxy A52 (`SM-A525F`), serial `R58R61E3CAZ`, Android 14 |
| Build | Debug `-p:PosLocalValidationTarget=PhysicalDevice` → `http://100.120.79.81:8091/8092` |
| Identity | Local Validation Quick Login `maria.santos` / ABC Sari-Sari Store |

### Observed partial results (2026-08-08)

| Step | Result |
|---|---|
| Online Quick Login + operational setup | Pass |
| Offline PIN setup (Settings) | Pass |
| Open shift + warm catalog (online sell) | Pass |
| Airplane / unreachable → force-stop → cold start → PIN unlock | Pass (after `10a1fc5`) |
| Offline Sell with cached catalog + shift snapshot | Pass |
| Offline cash sale pending receipt | Pass — local sale `OFF-260808-6D34859D` (₱61.11) |
| Restart offline; pending sync count retained | Pass — “Pending sync (1 items)” |
| Restore network + sync once / no duplicate | **Not completed** — Tailscale/host reachability not restored in the same session |
| Wrong-PIN lockout without damaging grant | **Not run** |

### Current limitation

Physical-device validation remains **pending / incomplete**. Remaining blockers before any Device Verified claim: reconnect Tailscale (or equivalent) and confirm outbox sync clears once without duplicate stock/sale; run negative PIN lockout; user confirmation under P19-WP08.

## 18. Files / areas touched (implementation)

- Application: `OfflineOperatingGrant*`, `OfflinePinHasher`, `AuthenticationService` restore/unlock, `ProtectedShellAccessPolicy`, `LocalSellingCatalogSyncService`, `RoleHomeResolver`
- LocalStore: schema v5 catalog / shift snapshot / cash sale
- ApiClient: `SaleCheckoutOfflineDispatcher`, `PosPermissionClient` transport mapping
- MAUI: `OfflinePinUnlock.razor`, Settings PIN, `SaleCheckout.razor` offline path, `LocalSaleReceipt`, `MauiConnectivityService` PhysicalDevice offline semantics
- Tests: grant/auth/resolver/cash-sale/architecture as above

## 19. Git evidence (feature commits; docs commit separate)

| SHA | Message |
|---|---|
| `f476172` | `feat(pos): add offline operate grant and PIN cold-start unlock` |
| `cc64ba3` | `feat(pos): enable offline cash checkout with local catalog cache` |
| `10a1fc5` | `fix(pos): stop offline PIN unlock hanging on permissions HTTP` |
| `e3c1093` | `feat(pos): add Lock semantics, PIN enrollment gate, and online-required guard` |
| `e3a251f` | `feat(pos): prepare login providers and mandatory PIN enrollment UX` |
| `8cccb8c` | `test(pos): cover auth UX layer and document PIN enrollment` |

**Not pushed** until validation gates agreed. Do not force-push. Google/Facebook OAuth remains **not implemented**.

## 20. Status

**Code Complete** for the offline operability foundation described here. Phase 19 remains **Open**. Physical Android A–S **incomplete**. **Not Device Verified.** **Not production-ready.**
