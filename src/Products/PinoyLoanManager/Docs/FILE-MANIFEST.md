# Pinoy Loan Manager — File Manifest

**Status:** Foundation / planning only
**Implementation present:** No
**Work package:** PLM-00-WP01 — Product Workspace & Documentation Structure

This file is the navigation map for future Cursor work. Load this product’s `Docs/` after the shared Product Foundation reference. Do not scan PinoyBusinessPOS implementation by default.

Shared contracts to load with this product:

- `.cursor/rules/exits-workflow.mdc`
- `.cursor/rules/exits-product-context.mdc`
- `docs/Product-Foundation/exits-product-foundation-reference.md`

---

## Created by PLM-00-WP01

| Path | Purpose | Status | Implementation present |
|---|---|---|---|
| `src/Products/PinoyLoanManager/` | Product workspace root (documentation only in this WP) | Foundation / Planning Only | No |
| `src/Products/PinoyLoanManager/Docs/` | Authoritative product documentation root (D-P12-02) | Foundation / Planning Only | No |
| `Docs/README.md` | Product identity, ownership boundary, and documentation index | Foundation / Planning Only | No |
| `Docs/FILE-MANIFEST.md` | This navigation map | Foundation / Planning Only | No |
| `Docs/Product/` | Future loan-product behavior and business rules | Foundation / Planning Only | No |
| `Docs/Product/README.md` | Purpose of `Product/` and deferred policy subjects | Foundation / Planning Only | No |
| `Docs/Architecture/` | Future technical structure and product boundaries | Foundation / Planning Only | No |
| `Docs/Architecture/README.md` | Architecture intent, identity model, and client direction | Foundation / Planning Only | No |
| `Docs/Security/` | Future security, privacy, authorization, and audit rules | Foundation / Planning Only | No |
| `Docs/Security/README.md` | Purpose of `Security/` and deferred security decisions | Foundation / Planning Only | No |
| `Docs/Decisions/` | Future Architecture Decision Records | Foundation / Planning Only | No |
| `Docs/Decisions/README.md` | ADR purpose and currently open decisions | Foundation / Planning Only | No |
| `Docs/Phases/` | Future phase and work-package sequencing | Foundation / Planning Only | No |
| `Docs/Phases/README.md` | Purpose of `Phases/` and current planning phase | Foundation / Planning Only | No |
| `Docs/Reports/` | Future completed work-package evidence | Foundation / Planning Only | No |
| `Docs/Reports/README.md` | Purpose of `Reports/` | Foundation / Planning Only | No |
| `Docs/Validation/` | Future owner / device / browser / calculation evidence | Foundation / Planning Only | No |
| `Docs/Validation/README.md` | Purpose of `Validation/` | Foundation / Planning Only | No |
| `Docs/Operations/` | Future deployment and production-operations documentation | Foundation / Planning Only | No |
| `Docs/Operations/README.md` | Purpose of `Operations/` | Foundation / Planning Only | No |

---

## Not present (intentionally)

The following are **out of scope** for PLM-00-WP01 and must not be assumed to exist:

| Item | Reason |
|---|---|
| `ExItS.PinoyLoanManager.Domain` | Code projects not authorized |
| `ExItS.PinoyLoanManager.Application` | Code projects not authorized |
| `ExItS.PinoyLoanManager.Infrastructure` | Code projects not authorized |
| `ExItS.PinoyLoanManager.Api` | Code projects not authorized |
| `ExItS.PinoyLoanManager.ApiClient` | Code projects not authorized |
| `ExItS.PinoyLoanManager.Web` | Code projects not authorized |
| `ExItS.PinoyLoanManager.Maui` | Code projects not authorized |
| `ExItS.PinoyLoanManager.LocalStore` | Code projects not authorized |
| Test projects | Code projects not authorized |
| Database / migration folders | Persistence not authorized |
| Docker / deploy implementation | Operations implementation not authorized |
| `ExItS.slnx` entries | Solution wiring not authorized |
| Filled Product Foundation templates (`product-definition.md`, `architecture.md`, `security.md`, `authorization-matrix.md`, `development-plan.md`, `roadmap.md`, `risks-and-decisions.md`) | Deferred to a later planning package |

---

## Future documentation (not created here)

When authorized, new files belong in the matching directory above. Do not invent loan policy in those files until the product owner decides it.
