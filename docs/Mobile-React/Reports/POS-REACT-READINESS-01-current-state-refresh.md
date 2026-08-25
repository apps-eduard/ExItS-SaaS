# POS-REACT-READINESS-01 — Current State Refresh Report

**Package:** POS-REACT-READINESS-01  
**Branch:** `docs/pos-react-implementation-readiness`  
**Worktree:** `C:/Users/speed/Desktop/ExItS-SaaS-pos-react-docs`  
**Base `origin/main`:** `5979a9ce008bb24a3257abd28ae79bc1a5a9b569`  
**Status:** Documentation complete for this package. Implementation is **NOT AUTHORIZED**.

This report records the current-main MAUI audit. It does not rewrite accepted MOBILE-D decisions. Historical reports (`MOBILE-REACT-DOC-08`, AMEND-01…03, MERGE-01) remain historical.

Canonical deliverable: [01-current-maui-implementation-refresh.md](../Implementation-Readiness/01-current-maui-implementation-refresh.md)

---

## 1. Scope held

| Constraint | Held |
|---|---|
| Documentation only | Yes |
| Work only in this worktree | Yes |
| No MAUI / API / database / React source change | Yes |
| No rewrite of accepted MOBILE-D decisions | Yes |
| Current MAUI treated as Auth + Personal + Owner + POS Operations | Yes |
| Route count not used as parity | Yes |

---

## 2. Current MAUI summary

| Item | Current evidence |
|---|---|
| Host | `ExItS.PinoyBusinessPOS.Maui` — MAUI Blazor Hybrid |
| TFM | `net10.0-android` |
| Min API | 24 |
| RIDs | `android-arm64;android-x64` |
| App id | `com.exits.pinoybusinesspos` |
| Display version | `0.5.0` |
| Native view | `BlazorWebView` in `MainPage.xaml` |
| Default layout | `PosShell` |
| Shells | Auth, Personal, Pos (Owner is capability inside PosShell) |
| `@page` templates | **171** |
| Offline | LocalStore encrypted SQLite outbox consumed by MAUI |
| Cart | In-memory `SaleCartService` (not persisted) |
| Hardware | Still-image QR, camera photos, Share, SecureStorage, Connectivity, FileSystem |
| Absent hardware | HID SDK, live product barcode camera, printer, cash-drawer kick, NFC, real terminal |

---

## 3. Experience coverage (must survive eventual MAUI retirement)

| Experience | Current host status | First React slice |
|---|---|---|
| Auth | Present (`AuthShell`) | In first slice (session shell) |
| Personal Mobile | Present (`PersonalShell`) | **Not** first selling slice; still required for Gate J |
| Organization Owner Mobile | Present (PosShell reduced nav / `/org/*`) | **Not** first selling slice; still required for Gate J |
| POS Operations | Present (PosShell full nav) | First vertical slice may be POS selling |

---

## 4. Deltas vs approved DOC-01

Approved [current-state-and-replacement-boundaries.md](../current-state-and-replacement-boundaries.md) architecture is **VERIFIED**.

Recorded deltas (inventory/clarification only):

1. Evidence SHA moved from `5a9be941…` to `5979a9ce…` (this queue’s required main).
2. Exhaustive route inventory (171 templates) vs DOC-01 representative lists.
3. Min SDK, RIDs, display version, and OAuth callback `exitspos://auth/callback` now documented from source.
4. `DEBUG_LOCAL_VALIDATION_CREDENTIAL_EMBEDDED` (value not published here).
5. Cart persistence clarification: current MAUI cart is session memory.

No MOBILE-D identifier was opened, closed, or weakened.

---

## 5. Composition-root warning for later React work

`MauiProgram` is a single large composition root (UI prefs, auth, workspace, HTTP clients, LocalStore, sync dispatchers, selling, catalog, Personal, device adapters, diagnostics). A future React client must split these concerns. Do not port one giant provider.

---

## 6. Local Validation

Debug builds still point at Local Validation HTTP hosts and enable the Local Validation client. A shared Development credential is embedded in Debug source.

Flag: **DEBUG_LOCAL_VALIDATION_CREDENTIAL_EMBEDDED**

Later replacement: runtime / developer-secret injection. Not in this documentation package.

---

## 7. Build-complexity note

MAUI 10 Android workload, sideload Debug APK embedding, and explicit Release trim/AOT **off** are current cost. A React/PWA/Capacitor strategy may simplify that surface. **No performance claim** is made without measurement.

---

## 8. Authorization lock

| Item | Status |
|---|---|
| React implementation | **NO** |
| MAUI retirement | **NO** |
| PWA production | **NO** |
| Capacitor production | **NO** |
| Main merge of this docs branch | **NO** (push of this documentation branch is allowed) |

---

## 9. Next package

POS-REACT-READINESS-02: feature parity + UX migration matrix (`02-feature-parity-matrix.md`), after this package PASSes.
