# MOBILE-REACT-DOC-AMEND-01 — Auth, Connectivity UX, and Copy Diagnostics

**Package:** MOBILE-REACT-DOC-AMEND-01  
**Branch:** `docs/mobile-react-foundation`  
**Starting HEAD:** `3c195fbdf53e7a5c232041ec8a83c78a02c51603`  
**Baseline `origin/main`:** `5a9be9417b7a2217227ae93e9280102992861615`  
**Main drift:** none  

**Status:** Documentation amendment only. Implementation **NOT AUTHORIZED**. Merge **NOT AUTHORIZED**.

This amendment records Product Owner decisions from final review of the Mobile React planning baseline. It does **not** rewrite [MOBILE-REACT-DOC-08-final-closeout.md](MOBILE-REACT-DOC-08-final-closeout.md) as if those decisions were original.

---

## Review reason

Product Owner required the planning set to capture:

1. Trusted-device / multi-user PIN-first daily authentication UX  
2. Lock vs Sign Out vs Remove From This Device  
3. Automatic lock expectations  
4. Centralized OfflineCapable / Queueable / OnlineRequired UX  
5. Short-lived Internet-required messages for ordinary blocked actions  
6. Persistent explanatory dialog for sensitive context-switch actions  
7. Single-message one-click Copy Diagnostics for Cursor/debugging  

---

## Documents amended

| Document | Change |
|---|---|
| [offline-sync-auth-and-security.md](../offline-sync-auth-and-security.md) | PIN-first flows, multi-user isolation, Lock/Sign Out/Remove, auto-lock, PIN OPEN items, connectivity messages |
| [product-surfaces-and-ux.md](../product-surfaces-and-ux.md) | Account UX, cart vs Lock, Copy Diagnostics compact UI |
| [frontend-architecture-and-reuse.md](../frontend-architecture-and-reuse.md) | Copy Diagnostics service, format, redaction, sources, Cursor purpose |
| [migration-testing-and-implementation-gates.md](../migration-testing-and-implementation-gates.md) | Tests and visual checkpoint rows |
| [decisions.md](../decisions.md) | MOBILE-D-054 … D-059 Accepted; MOBILE-D-060 Open |
| [documentation-status.md](../documentation-status.md) | AMEND-01 complete; still not authorized to implement/merge |
| [README.md](../README.md) | Link this report |
| `FILE-MANIFEST.md` | This report path |

No MAUI, React, PWA, Capacitor, backend, or migration files were changed.

---

## New decisions

| ID | Status |
|---|---|
| MOBILE-D-054 | Accepted — trusted-device PIN-first daily UX |
| MOBILE-D-055 | Accepted — multiple enrolled local identities |
| MOBILE-D-056 | Accepted — Lock / Sign Out / Remove distinction |
| MOBILE-D-057 | Accepted — auto-lock is Lock, not Sign Out |
| MOBILE-D-058 | Accepted — centralized connectivity + ordinary toast vs sensitive dialog |
| MOBILE-D-059 | Accepted — one-click safe Copy Diagnostics |
| MOBILE-D-060 | **Open** — PIN length, weak/sequential rejection, identical PIN across users |

---

## Current MAUI evidence vs future React planning

| Topic | Current MAUI (evidence) | Future React planning |
|---|---|---|
| PIN-first daily use | Enrollment after online auth; `/offline-pin` chooser; online PIN revalidates same user; offline uses bounded grant | Canonical proposed flow (same semantics) |
| Multi-user | `OfflineEnrolledUserSummary` safe fields; per-user verifier/lockout | Preserve isolation; no impersonation |
| Lock | `LockAsync` retains grant/PIN; navigates to `/offline-pin` | Same; Lock ≠ Sign Out |
| Sign Out | `LogoutAsync` clears active session/bearer; keeps grant + PIN + session handle for PIN recovery; OD-10 outbox | Canonical name **Sign Out** |
| Remove From This Device | `RemoveEnrolledOfflineUserAsync` in Application; dedicated Settings control **not found** | Must be an explicit UX action |
| Auto-lock | **Not found** | Planning requirement; no numeric timeout documented |
| PIN complexity | Digits; min 6 default; max 12 input; 5 fails / 15 min lockout; no weak-PIN list found | **OPEN** (D-060) |
| Ordinary OnlineRequired | Persistent shared **Dialog** for all OnlineRequired | Short-lived shared toast/banner (~4–5 s, not a business invariant) |
| Org/workspace switch | `OnlineRequired_OrgSwitchMessage` persistent dialog | Persistent dismissible dialog (Got it) |
| Reconnect redirect | Ordinary OnlineRequired must not go to `/reconnect` | Preserve |
| Back online | `Connectivity_BackOnline` string exists; global toast **not wired** in this audit | Optional restrained auto-dismiss when API reachable |
| Diagnostics copy | Settings page `FormatReport` + forbidden-marker check | Global one-click Copy Diagnostics on runtime errors |

---

## Explicit non-authorizations

| Item | Status |
|---|---|
| React implementation | **NOT AUTHORIZED** |
| PWA implementation | **NOT AUTHORIZED** |
| Capacitor implementation | **NOT AUTHORIZED** |
| MAUI changes | **NOT MADE** |
| Backend / migrations | **NOT MADE** |
| Merge | **NOT AUTHORIZED** / **NOT PERFORMED** |

Wait for Product Owner + ChatGPT **final** review.
