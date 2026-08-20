# Mobile React / PWA / Capacitor — Documentation Status

Program name: `ExItS Mobile React / PWA / Capacitor` (documentation-only planning)

Target worktree branch: `docs/mobile-react-foundation`

Baseline origin/main SHA: `5a9be9417b7a2217227ae93e9280102992861615`

Documentation: `FINAL APPROVED` (DOC-00 … DOC-08; AMEND-01, AMEND-02, AMEND-03)

React implementation: `AUTHORIZED RECOVERY THROUGH POS-REACT-IMPL-05 ON feat/pos-react-client ONLY (WP03–WP05); WP06 checkout NOT in this recovery)`

PWA implementation: `PHASE A STATIC SHELL + BROWSER SESSION ON feat/pos-react-client; production rollout NOT AUTHORIZED`

Capacitor implementation: `NOT AUTHORIZED`

MAUI retirement: `NOT AUTHORIZED`

Merge: `PERFORMED` (`MOBILE-REACT-DOC-MERGE-01`) — implementation branch is **not** merged to `main`

Existing MAUI status: `Retained / Unmodified`

Existing Organization Web status: `Retained / Unmodified`

Existing Personal Web status: `Retained / Unmodified`

Existing .NET backends (Platform API + POS API + PostgreSQL): `Retained / Unmodified`

Future React / PWA / Capacitor status: `Gate C scaffold + Gate D Phase A PWA + browser session/workspace on feat/pos-react-client; not on main`

React implementation presence: `Present` (foundation + static PWA shell + browser session/workspace)

Capacitor implementation presence: `Absent`

PWA production presence: `Absent`

Queue state: `STOPPED AFTER MOBILE-REACT-DOC-MERGE-01` (planning baseline)

Implementation-readiness documentation queue: `STOPPED AFTER POS-REACT-READINESS-05`

Readiness branch: `docs/pos-react-implementation-readiness`

Readiness evidence SHA: `5979a9ce008bb24a3257abd28ae79bc1a5a9b569`

Implementation branch: `feat/pos-react-client` (starts at readiness tip `0954c1d11e5b9130f8411afb3f086c7e116d76ff`)

Gate C scaffold (`POS-REACT-IMPL-01`): `COMPLETE` on `feat/pos-react-client`

Gate D Phase A PWA shell (`POS-REACT-IMPL-02`): `COMPLETE` on `feat/pos-react-client`

Gate D browser auth / workspace (`POS-REACT-IMPL-03`): `COMPLETE` on `feat/pos-react-client`

Gate D browser auth / workspace: `PARTIAL` — session + CSRF + workspace + sell-floor shell complete; catalog/cart follow IMPL-05

MOBILE-D-060: `OPEN`

PWEB20_CSRF_COMPATIBILITY_REVIEW_REQUIRED: `SATISFIED FOR POS REACT CLIENT` (PWEB-20 contract applied; Platform Admin Web source not changed)

PLM_PWA_PATTERN_REVIEW_REQUIRED: `SATISFIED FOR ENGINEERING PATTERNS`

TYPED_CLIENT_GENERATION_CONTRACT_MISSING: `OPEN` (hand-typed Platform DTOs + validation)

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
| POS-REACT-READINESS-01 | Complete | Current MAUI implementation refresh vs `5979a9ce` |
| POS-REACT-READINESS-02 | Complete | Feature parity + UX migration matrix |
| POS-REACT-READINESS-03 | Complete | API + auth + browser security readiness |
| POS-REACT-READINESS-04 | Complete | PWA / offline / device migration sequence |
| POS-REACT-READINESS-05 | Complete | Master plan + open decisions; implementation still unauthorized |
| POS-REACT-IMPL-01 | Complete on `feat/pos-react-client` | React client scaffold; report [POS-REACT-IMPL-01-react-client-scaffold.md](Reports/POS-REACT-IMPL-01-react-client-scaffold.md) |
| POS-REACT-IMPL-02 | Complete on `feat/pos-react-client` | PWA static shell (Phase A); report [POS-REACT-IMPL-02-pwa-static-shell.md](Reports/POS-REACT-IMPL-02-pwa-static-shell.md) |
| POS-REACT-IMPL-03 | Complete on `feat/pos-react-client` | Browser session + workspace resolver; report [POS-REACT-IMPL-03-browser-session-workspace.md](Reports/POS-REACT-IMPL-03-browser-session-workspace.md) |
| POS-REACT-IMPL-04 | Complete in worktree (uncommitted) | POS sell-floor shell; report [POS-REACT-IMPL-04-sell-floor-shell.md](Reports/POS-REACT-IMPL-04-sell-floor-shell.md) |

## Authorization gates (locked)

The table below remains the `main` planning-baseline lock for Capacitor / MAUI retirement / merge. Product Owner recovery command authorized **POS-REACT-IMPL-03 → 05** on `feat/pos-react-client` only (WP06 checkout excluded until readiness gate). Do not merge to `main`.

| Gate | Status |
|---|---|
| React mobile implementation | **PARTIAL** — Gate C–D (scaffold, PWA Phase A, browser session/workspace) on `feat/pos-react-client`; not on `main` |
| PWA implementation / production rollout | **NOT AUTHORIZED** for production; Gate D Phase A static shell is complete on `feat/pos-react-client` |
| Capacitor implementation / production rollout | **NOT AUTHORIZED** |
| MAUI retirement | **NOT AUTHORIZED** |
| Merge to `main` | **NOT AUTHORIZED** for this implementation branch |
| Merge of `docs/pos-react-implementation-readiness` | **NOT AUTHORIZED** |

Do not add Capacitor and do not modify MAUI in this queue.

MOBILE-D-060 remains **Open**. Do not rewrite [DOC-08 closeout](Reports/MOBILE-REACT-DOC-08-final-closeout.md) as if it originally contained AMEND decisions or this merge.
