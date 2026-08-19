# MOBILE-REACT-DOC-AMEND-02 — Language and Theme Defaults

**Package:** MOBILE-REACT-DOC-AMEND-02  
**Branch:** `docs/mobile-react-foundation`  
**Starting HEAD:** `ccb922189dbfcf1296581d6f9bb9e15cbf553e5c`  
**Baseline `origin/main`:** `5a9be9417b7a2217227ae93e9280102992861615`  
**Main drift:** none  

**Status:** Documentation amendment only. Implementation **NOT AUTHORIZED**. Merge **NOT AUTHORIZED**.

Does **not** rewrite [MOBILE-REACT-DOC-08-final-closeout.md](MOBILE-REACT-DOC-08-final-closeout.md).

---

## Reason

Lock canonical first-launch and client-wide defaults before any documentation merge:

- English (`en`) = default language
- Filipino / Tagalog (`fil-PH`) = required secondary (no `tl-PH` unless later decided)
- Theme: System / Light / Dark, with **System = default**

Existing docs already required EN, fil-PH, and all three theme modes. This amendment makes **System as the default theme** explicit and canonical (MOBILE-D-064).

---

## Decisions

| ID | Status |
|---|---|
| MOBILE-D-064 | **Accepted** — `en` default; `fil-PH` secondary; System default theme; client-wide persisted UI prefs; apply without restart; must not discard cart/form/auth/offline state |
| MOBILE-D-060 | Remains **Open** |

MOBILE-D-017 still lists EN + fil-PH and Light/Dark/System as UX principles; D-064 locks the **defaults**.

---

## Locked behavior

| Topic | Rule |
|---|---|
| First launch language | English — do **not** infer Filipino from device locale |
| First launch theme | System — a real stored value; do **not** persist OS Light/Dark as an explicit Light or Dark choice |
| Explicit Light/Dark | Overrides System until the user changes it |
| OS theme change | While preference is System, UI updates where the host supports it |
| Persistence | Non-sensitive local UI prefs; entire Mobile React Client (Web / PWA / Capacitor Android / iOS later) |
| Apply | Immediate; no restart; no sign-out; no cart/form/offline/auth/financial mutation |
| Shared UI | Consumes global locale and theme (MOBILE-D-061–D-063) |

**Current MAUI evidence (not a MAUI change):** `ThemePreference.System` is already the MAUI store default.

---

## Documents amended

- [product-surfaces-and-ux.md](../product-surfaces-and-ux.md)
- [frontend-architecture-and-reuse.md](../frontend-architecture-and-reuse.md)
- [migration-testing-and-implementation-gates.md](../migration-testing-and-implementation-gates.md)
- [decisions.md](../decisions.md)
- [documentation-status.md](../documentation-status.md)
- [README.md](../README.md)
- `FILE-MANIFEST.md`

---

## Explicit non-authorizations

React, PWA, Capacitor, MAUI, backend, migrations: **unchanged / not authorized**.  
Merge: **NOT PERFORMED**.
