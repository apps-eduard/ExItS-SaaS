# Mobile React / PWA / Capacitor — Documentation Status

Program name: `ExItS Mobile React / PWA / Capacitor` (documentation-only planning)

Target worktree branch: `docs/mobile-react-foundation`

Baseline origin/main SHA: `5979a9ce008bb24a3257abd28ae79bc1a5a9b569`

Documentation: `FINAL APPROVED` (DOC-00 … DOC-08; AMEND-01, AMEND-02, AMEND-03)

React implementation: `Gate C AUTHORIZED` (MOBILE-REACT-IMPL-01 foundation)

PWA implementation: `NOT AUTHORIZED`

Capacitor implementation: `NOT AUTHORIZED`

MAUI retirement: `NOT AUTHORIZED`

Merge: `PERFORMED` (`MOBILE-REACT-DOC-MERGE-01`)

Existing MAUI status: `Retained / Unmodified`

Existing Organization Web status: `Retained / Unmodified`

Existing Personal Web status: `Retained / Unmodified`

Existing .NET backends (Platform API + POS API + PostgreSQL): `Retained / Unmodified`

Future React / PWA / Capacitor status: `Gate C foundation in progress; PWA and Capacitor locked`

React implementation presence: `Present` (`src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Client/` — foundation only)

Capacitor implementation presence: `Absent`

PWA production presence: `Absent`

Queue state: `STOPPED AFTER MOBILE-REACT-IMPL-01`

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
| MOBILE-REACT-DOC-MERGE-01 | Complete | Approved planning baseline merged to `main` |
| MOBILE-REACT-IMPL-01 | Complete | Gate C React Mobile Client foundation (shell, theme, i18n, HTTP stubs, diagnostics) |

## Authorization gates (locked)

| Gate | Status |
|---|---|
| React mobile implementation | **Gate C AUTHORIZED** (IMPL-01 foundation only) |
| PWA implementation / production rollout | **NOT AUTHORIZED** |
| Capacitor implementation / production rollout | **NOT AUTHORIZED** |
| MAUI retirement | **NOT AUTHORIZED** |
| Merge to `main` | **PERFORMED** (`MOBILE-REACT-DOC-MERGE-01`) — does **not** authorize PWA, Capacitor, or MAUI retirement |
| Gate D+ | **NOT AUTHORIZED** |

Do not add a PWA service worker, Capacitor, authentication, PIN, workspace chooser, selling, or MAUI changes in this package.

MOBILE-D-060 remains **Open**. Do not rewrite [DOC-08 closeout](Reports/MOBILE-REACT-DOC-08-final-closeout.md) as if it originally contained AMEND decisions or this implementation.
