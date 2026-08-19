# Mobile React / PWA / Capacitor — Documentation Status

Program name: `ExItS Mobile React / PWA / Capacitor` (documentation-only planning)

Target worktree branch: `docs/mobile-react-foundation`

Baseline origin/main SHA: `5a9be9417b7a2217227ae93e9280102992861615`

Documentation: `FINAL APPROVED` (DOC-00 … DOC-08; AMEND-01, AMEND-02, AMEND-03)

React implementation: `NOT AUTHORIZED`

PWA implementation: `NOT AUTHORIZED`

Capacitor implementation: `NOT AUTHORIZED`

MAUI retirement: `NOT AUTHORIZED`

Merge: `AWAITING PRODUCT OWNER AUTHORIZATION`

Existing MAUI status: `Retained / Unmodified`

Existing Organization Web status: `Retained / Unmodified`

Existing Personal Web status: `Retained / Unmodified`

Existing .NET backends (Platform API + POS API + PostgreSQL): `Retained / Unmodified`

Future React / PWA / Capacitor status: `Documentation Only`

React implementation presence: `Absent`

Capacitor implementation presence: `Absent`

PWA production presence: `Absent`

Queue state: `DOCUMENTATION FINAL APPROVED — MERGE AWAITING PRODUCT OWNER AUTHORIZATION`

MOBILE-D-060: `OPEN`

## DOC queue

| Doc ID | Status | Notes |
|---|---|---|
| MOBILE-REACT-DOC-00 | Complete | Dedicated worktree and branch created |
| MOBILE-REACT-DOC-01 | Complete | Current-state audit, terminology, replacement boundaries, CSS/Razor contradiction recorded |
| MOBILE-REACT-DOC-02 | Complete | Product surfaces, device-class UX, role matrix, POS selling workflow, visual quality target |
| MOBILE-REACT-DOC-03 | Complete | React stack, reuse strategy, device adapters, recommended `ExItS.PinoyBusinessPOS.Client` path (not created) |
| MOBILE-REACT-DOC-04 | Complete | PWA vs Capacitor delivery, static cache vs LocalStore, iOS interim, Windows browser/PWA, independent release channels |
| MOBILE-REACT-DOC-05 | Complete | Offline/outbox/idempotency audit, cash-only current checkout queue, auth/security, conflict policy |
| MOBILE-REACT-DOC-06 | Complete | Device/payment adapters, HID vs camera vs QR, no current printer/NFC/terminal, capability matrix |
| MOBILE-REACT-DOC-07 | Complete | Coexistence stages 0–8, feature parity fields, testing layers, visual checkpoint, gates A–K |
| MOBILE-REACT-DOC-08 | Complete | Consistency audit, canonical cross-references, final closeout |
| MOBILE-REACT-DOC-AMEND-01 | Approved | Trusted-device PIN UX, Lock/Sign Out/Remove, connectivity messages, Copy Diagnostics |
| MOBILE-REACT-DOC-AMEND-02 | Approved | Canonical `en` default, `fil-PH` secondary, System default theme |
| MOBILE-REACT-DOC-AMEND-03 | Approved | Smart workspace + product context; AppTopBar shared context |
| Product Owner approval | Recorded | [MOBILE-REACT-DOC-APPROVAL-record.md](Reports/MOBILE-REACT-DOC-APPROVAL-record.md) |

## Authorization gates (locked)

| Gate | Status |
|---|---|
| React mobile implementation | **NOT AUTHORIZED** |
| PWA implementation / production rollout | **NOT AUTHORIZED** |
| Capacitor implementation / production rollout | **NOT AUTHORIZED** |
| MAUI retirement | **NOT AUTHORIZED** |
| Merge to `main` | **AWAITING PRODUCT OWNER AUTHORIZATION** |
| Documentation approval | Does **not** authorize React, PWA, Capacitor, MAUI retirement, or merge |

Do not scaffold React, add Capacitor, add a PWA service worker, add Node dependencies, or modify MAUI.

Do not merge until the Product Owner explicitly authorizes merge to `main`.

MOBILE-D-060 remains **Open**. Do not rewrite [DOC-08 closeout](Reports/MOBILE-REACT-DOC-08-final-closeout.md) as if it originally contained AMEND decisions or this approval.
