# P19 — Offline Connectivity Capability Matrix

| Field | Value |
|---|---|
| Status | **Code Complete** (policy + nav/action guards) · Physical offline validation **Incomplete** |
| Phase | [Phase 19](../phases/phase-19-mobile-pos-operations-and-cashier-experience.md) — **Open** |
| Related | [P19-offline-operability-foundation](P19-offline-operability-foundation.md), [P19-personal-scope-offline-operability](P19-personal-scope-offline-operability.md), [P19-support-diagnostics](P19-support-diagnostics.md) |
| Date | 2026-08-08 |
| Feature commits | `61a2bf8` (policy + guards), `9d725bd` (tests), `5412e4b` + `829fcd8` (matrix docs) |
| Device Verified | **No** |
| Production Ready | **No** |

## 1. Goal

When offline, continue safe local/offline POS work. Destinations and actions that truly need the server show **one** shared Internet-required dialog. Do **not** clear the current org/session or bounce to `/reconnect` for ordinary OnlineRequired features.

Authorization and offline-grant rules remain separate: **SERVER UNREACHABLE ≠ SERVER DENIED ACCESS**.

## 2. Shared architecture

| Piece | Location |
|---|---|
| Requirement enum | `Application/Offline/PosConnectivityRequirement.cs` — `OfflineCapable`, `Queueable`, `OnlineRequired` |
| Action keys | `Application/Offline/PosOfflineActionKeys.cs` |
| Policy (metadata) | `Application/Offline/PosOfflineCapabilityPolicy.cs` implementing `IPosOfflineCapabilityPolicy` |
| Dialog guard | `Application/Offline/OnlineRequiredGuard.cs` |
| Dialog host | `Maui/Components/Shared/OnlineRequiredDialogHost.razor` (+ optional Retry connection) |
| Nav helper | `Maui/Services/OfflineAwareNavigation.cs` |

Unknown / unclassified operational routes **fail closed** as `OnlineRequired`.

## 3. Dialog behavior

**Default (most OnlineRequired routes/actions)**

- Title: Internet required  
- Message: You're currently offline… offline work is safe and will sync…  
- Actions: Retry connection · OK  

**Organization switch**

- Same title  
- Message: You're currently working offline in {CurrentOrg}. Connect… before switching organizations…  
- Does **not** discard pending local transactions or clear the offline grant  

Choosing OK keeps the user in the current safe context. Retry re-checks connectivity and refreshes sync status when online returns.

## 4. Route classification summary

Source of truth: `PosOfflineCapabilityPolicy.ImportantRoutes`.

### OfflineCapable (examples)

| Route | Reason |
|---|---|
| `/offline-pin`, `/offline-pin-setup` | Local PIN / grant |
| `/signin`, onboarding prefs | Offline PIN CTA / local prefs |
| `/owner`, `/manager`, `/cashier`, `/more`, `/settings` | Shell hubs (tiles may still be OnlineRequired) |
| `/sales` | List reachable; **New Sale** allowed; history load is online-only behavior on the page |
| `/sales/local…` | Local pending receipt |

### Queueable / local-first

| Route | Reason |
|---|---|
| `/sales/new` | Cash checkout + local catalog + outbox (`sale.checkout`) |
| `/customers`, `/customers/new`, customer edit/credit/repayment create | Existing customer/credit offline outbox paths |

### OnlineRequired (examples)

| Route / area | Reason |
|---|---|
| `/organization-select` | Server membership / bind / entitlement |
| `/catalog` (+ import, categories, global, barcode) | Admin catalog HTTP; global/template import |
| `/inventory`, `/purchasing`, `/expenses`, `/suppliers` | No local UI store |
| `/registers`, `/shifts`, `/setup` | Server operational APIs (shift open-snapshot is checkout-only) |
| `/permissions`, `/reports`, `/dashboard`, `/overdue` | Server lists / reports |
| `/org`, staff, subscription | Platform/org administration |
| `/reconnect` | Explicit reconnect surface |
| Personal utang / start-business | Platform personal APIs |

## 5. Mixed-page action rules

| Page | Offline / Queueable | OnlineRequired action |
|---|---|---|
| `/sales/new` | Cash browse + cash commit | Non-cash payments (`sale.payment.non_cash`) |
| `/sales` | Open page + New Sale | Server sales history paths `/sales/{id}` (+ receipt) classified OnlineRequired |
| `/customers` (+ detail) | Local list / queued create-edit-credit-repay | Ledger, statement, customer overdue via `OfflineAwareNavigation` + action keys |
| `/catalog` via bottom nav | — | Whole admin catalog entry blocked offline |
| `/more` hub | Hub opens | Inventory, purchasing, permissions, reports, org, etc. |

## 6. Org-switch offline behavior

- Current validated org remains operational under the offline grant.  
- Switching organization / switching to Personal is **OnlineRequired**.  
- Guarded in: `ShellAccountMenu`, `AccountContextSwitcher`, `Settings`.  
- Pending local work for the current org is retained.

## 7. Known unsupported offline features

Staff/roles/permissions, billing/subscription, global catalog import, inventory counts/adjust, purchasing, expenses, suppliers, registers admin, shift open/close UI, server sales history, reports, Card/GCash/Utang checkout, multi-org switch while offline.

## 8. Tests

| Coverage | Location |
|---|---|
| Policy classifications + fail-closed unknown | `PosOfflineCapabilityPolicyTests` |
| Guard dialog / org-switch / no reconnect | `PosOfflineCapabilityPolicyTests`, dialog host wiring |
| Existing offline grant / sales / catalog / … guards | Prior Maui page-guard + offline foundation suites |

Physical-device validation of this matrix: **not completed**.

## 9. Ambiguous classifications (review)

| Item | Current | Note |
|---|---|---|
| `/catalog` bottom tab | OnlineRequired | Selling uses local catalog on `/sales/new`; admin Products hub stays online-only until a local catalog browse UI exists |
| `/sales` history empty offline | OfflineCapable route | Page may show empty/history unavailable; New Sale remains available |
| Personal home tiles | Mostly OnlineRequired under `/personal/utang` | Personal utang has no POS local store |
| Merchant catalog create/edit | OnlineRequired | Not in outbox today — reclassify to Queueable only if local persistence is added |

## 10. Git

Feature commits: `61a2bf8`, `9d725bd`, `5412e4b`. **Not pushed** (physical offline matrix validation incomplete).
