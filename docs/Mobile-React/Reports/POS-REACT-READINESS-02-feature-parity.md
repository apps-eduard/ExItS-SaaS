# POS-REACT-READINESS-02 — Feature Parity Report

**Package:** POS-REACT-READINESS-02  
**Branch:** `docs/pos-react-implementation-readiness`  
**Worktree:** `C:/Users/speed/Desktop/ExItS-SaaS-pos-react-docs`  
**Base `origin/main`:** `5979a9ce008bb24a3257abd28ae79bc1a5a9b569`  
**Depends on:** POS-REACT-READINESS-01 `d47ba02360bbd27c0ba0b4e484083e1c407e083d`  
**Status:** Documentation complete for this package. Implementation is **NOT AUTHORIZED**.

Canonical deliverable: [02-feature-parity-matrix.md](../Implementation-Readiness/02-feature-parity-matrix.md)

---

## 1. What this package added

An implementation-ready **feature** matrix (not a route clone) covering:

- Auth / session / device
- Workspace and product context
- POS selling
- Catalog / inventory
- Customers / credit
- Shifts / registers
- Purchasing / suppliers
- Reports / expenses
- Organization Owner Mobile
- Personal Mobile
- Settings / i18n / theme
- Explicit WONT_PORT and not-assumed hardware

Status vocabulary matches the queue: READY, READY_WITH_CONTRACT_CHECK, OFFLINE_PARITY_REQUIRED, CAPACITOR_REQUIRED, PRODUCT_DECISION_REQUIRED, DEFERRED, WONT_PORT.

---

## 2. Recommended first React slice (Gate E)

```text
Auth / session shell
→ workspace resolver
→ product context
→ POS sell-floor shell
→ product browse / search
→ session cart
→ cash checkout ONLINE
→ receipt / share fallback
→ connectivity + sync presentation
```

Offline financial implementation is **excluded** from that UI slice. Tablet landscape remains the reference selling layout. Phone must stay usable. Desktop/PWA must remain operational POS, not a Platform Admin clone.

Human Product Owner visual approval is required for phone, tablet portrait, tablet landscape, and desktop/PWA.

---

## 3. Eventual MAUI retirement (Gate J)

| Experience | First slice | Eventual retirement |
|---|---|---|
| Auth | Session shell yes; PIN policy open | Required |
| Personal Mobile | Deferred | Required unless Product Owner splits the host |
| Organization Owner Mobile | Deferred | Required unless explicit Web-only disposition |
| POS Operations | Selling slice first | Remaining ops still required |

**MAUI cannot retire after checkout parity alone.**

---

## 4. Important non-ports

| Item | Status | Reason |
|---|---|---|
| Platform Admin | WONT_PORT | MOBILE-D-014 |
| Organization Web full admin | WONT_PORT | Different host |
| Personal Web | WONT_PORT | Additional host, not Mobile Client |
| Fake Card/GCash as production UX | WONT_PORT | MOBILE-D-020 |
| Debug Local Validation credential | WONT_PORT | `DEBUG_LOCAL_VALIDATION_CREDENTIAL_EMBEDDED` |
| Thermal printer / drawer / NFC / real terminal | DEFERRED | Current MAUI also absent; not a Gate E blocker |

---

## 5. Open product decision affecting Auth parity

**MOBILE-D-060** remains **OPEN** (PIN length, weak/sequential rejection, identical PIN values across enrolled users). PIN unlock/enroll is `PRODUCT_DECISION_REQUIRED`.

---

## 6. Authorization lock

| Item | Status |
|---|---|
| React implementation | **NO** |
| MAUI retirement | **NO** |
| PWA production | **NO** |
| Capacitor production | **NO** |

---

## 7. Next package

POS-REACT-READINESS-03: API + auth + browser security readiness.
