# POS-REACT-RMAP-22G — Start Business + trial/subscription + owner workspace

## Status

**PASS** (React Personal Start a Business + Explore plans; trial path wired; Local Validation subscribe gated)

**Base tip:** `4d4148b039d10d46acf2275baca6794b4b9a3b13` (RMAP-22F Personal stores / customer links / orders)

## Delivered

- Real Start a Business form at `/personal/start-business` (no user-facing org slug; slug auto-generated)
- Explore plans at `/personal/explore-pos` → trial CTA; Local Validation subscribe CTA only when frontend Local Validation mode is on
- `POST /api/v1/personal/start-business` + `GET /api/v1/personal/onboarding/business-types` + `GET /api/v1/commercial/plans` + `GET /api/v1/personal/profile` clients
- Platform cookie refresh on start-business success (`AppendSessionCookie` + `ExpiresAtUtc` on result) so browser session rotates Personal → Organization
- After success: `refreshSession` → `refreshWorkspaces` → bind org → ensure onboarding progress → `/onboarding` (optional post-subscription setup; see [POS-POST-SUBSCRIPTION-ONBOARDING-01](../../reports/POS-POST-SUBSCRIPTION-ONBOARDING-01.md)). Historical note: earlier builds navigated to `/workspace` immediately.
- Owner return to Personal: Account menu **Switch to Personal** via `ensurePersonalSessionProfile` (blocked when `OrganizationContextLocked`)
- Staff principals cannot use Personal Start Business (`RequirePersonalSession` retained)
- Five locales + message-key parity (`en`, `fil-PH`, `ceb-PH`, `ilo-PH`, `hil-PH`)

## Trial / subscription evidence

| Path | Verdict | Evidence |
| --- | --- | --- |
| **Trial** (`StartAsTrial=true`, `PayNow=false`) | **PASS** | Default Explore → Start trial; Platform `StartBusinessForPersonalUser` starts trial subscription when plan allows trial; React posts trial flags; unit coverage on client + shell |
| **LocalValidation PayNow** | **PASS** (code-supported, UI gated) | Repo already has `LocalValidationPaymentProvider` when `LocalValidation:Enabled` and not Production (`PaymentProviderServiceCollectionExtensions`). Explore shows Subscribe only under `isFrontendLocalValidationMode()`. No production card UI. |
| **Production payment provider** | **N-A** | Not invented. Null/unconfigured provider remains; PayNow outside Local Validation is not offered in React UI. |

## Explicit non-claims

- RMAP-22H integrated E2E
- RMAP-21 Offline / LocalStore / outbox
- RMAP-B04 statement / buyer purchase projection
- Fake production payment provider / card capture
- Native-speaker certification of PH locales

## Tests

| Suite | Result |
| --- | --- |
| `organization-slug.test.ts` | PASS |
| `commercial-plans-client.test.ts` | PASS |
| `start-business-client.test.ts` | PASS |
| `ensure-personal-profile.test.ts` | PASS |
| `personal-shell-home.test.tsx` | PASS |
| `message-parity.test.ts` | PASS |
| `typecheck` | PASS |
| Platform.Api Release build | PASS |

Native-speaker certification: **PENDING**

## Next

**RMAP-22H — Integrated Personal ↔ Business E2E** (do not start until authorized)
