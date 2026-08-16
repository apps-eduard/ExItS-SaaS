# P9-WP01 — Security and Privacy Hardening

Phase marker: `P9-WP01-security-and-privacy-hardening`

## Status

**Complete with documented risks.** Hardened Platform and PinoyBusinessPOS against Development/Testing bypass misuse, unsafe Production configuration, missing transport/CORS/rate-limit/exception controls, and forged identity headers. **No new business features.** **Not production-ready** while R-091 (real auth), R-109 (interactive Android), R-129 (full-DB encryption), Manual GCash verification, and related blockers remain open. **P9-WP02 was not started.**

Feature commit: de4fac64739f5b368a6b1f2490223fa032201b65

## Delivered hardening

| Area | Delivered |
|---|---|
| Production config guard | Startup fails when Production lacks DB connection string, uses known-dev password, or `AllowedHosts=*` |
| Dev/test headers | POS org/actor headers rejected outside Development/Testing (`pos.development_headers.unavailable`); commercial headers ignored → Unknown fail-closed; Platform `X-Dev-Platform-User-Id` ignored outside Dev/Testing |
| Dev routes | `/api/v1/pos/dev/offline-probe` not mapped outside Development/Testing |
| Pipeline | Safe exception ProblemDetails (no stack); security headers; CORS deny-by-default; partitioned rate limiting; 1 MiB request body limit; HTTPS/HSTS when `Security:EnforceHttps` (default outside Dev/Testing) |
| Secrets | Base `appsettings.json` connection strings emptied; known-dev password only in `appsettings.Development.json` |
| Architecture tests | Phase marker, rate limiter presence, no Migrate(), no embedded base password, header gating source guards |
| MAUI | Cleartext network config documented as Development-only with Production HTTPS release gate |

## Threat model (focused)

| Threat | Boundary | Existing mitigation | Hardening this WP | Residual / release impact |
|---|---|---|---|---|
| Cross-organization data access | POS API | Org filter + 404 conceal | Org headers unavailable in Production | Open until JWT/org claims (R-091) |
| Authorization bypass | Platform/POS | Capability matrix; PlatformAuthz | DevOperator full access remains Dev/Testing-only; forged user header ignored in Production | Real auth still required |
| Forged Dev/Testing headers | Both APIs | Commercial ignore in Production | Org/actor/Platform user headers gated | Headers remain for Dev/Testing only |
| IDOR / enumeration | POS | Cross-org 404 | Unchanged; Production denies header scope | Open without production principal |
| Token/secret leakage | Config/logs | SecureStorage; audit substring bans | Empty base CS; Production rejects known-dev password | R-129; logging redaction still policy-level |
| Replay / idempotency abuse | POS | Idempotency records | Rate limit partitions; probe not in Production | Keep monitoring |
| Mass assignment | APIs | Typed DTOs | Unchanged (no entity binding) | Maintain on new endpoints |
| Unsafe ProblemDetails | APIs | Controlled domain errors | Global exception handler without stacks | Keep verifying |
| Sensitive logging | APIs | Auth event key filter | No body logging of rejects | Expand scrubber later if request logging added |
| Local SQLite theft | MAUI | Row AES-GCM; context isolation | Reviewed; SQLCipher deferred | **R-129 production gate** |
| SecureStorage key loss | MAUI | Fail closed | Unchanged | Documented |
| Offline queue tampering | LocalStore | AES-GCM AAD | Unchanged | Retained |
| Malicious barcode/SKU/text | Catalog | Domain validation | Unchanged | Keep bounds |
| Oversized requests | APIs | Pagination/report bounds | 1 MiB Kestrel limit + rate limits | Tune in P9-WP02 |
| Dependency vulns | NuGet | Tracked NU1903 | Documented; no unsafe suppress | **R-129 open** |
| Misconfigured Production | Hosting | — | Startup fail-closed | Required for deploy |
| Insecure transport | API/MAUI | Admin HTTPS | API HTTPS when EnforceHttps; MAUI cleartext Dev-only | Production TLS gate open |
| Debug/diagnostic exposure | MAUI/API | Dev-gated diagnostics | Probe unmapped in Production | Keep |
| Unauthorized financial mutation | POS | Capabilities + immutability | Production cannot use header auth | R-091 |

## Rate-limit policy

- Global fixed-window limiter partitioned by `X-Pos-Organization-Id` when present, else client IP (Platform: IP only).
- Sensitive policy applied to offline probe (Dev/Testing).
- Health checks disable rate limiting.
- 429 ProblemDetails use stable `pos.rate_limit.exceeded` / `platform.rate_limit.exceeded`.
- Idempotent replay is not broken (limits apply per request, not by suppressing replay semantics).

**Intentionally not per-route tighter limits yet:** every financial route already sits behind global partitions; finer policies deferred to performance WP if metrics demand.

## Local / offline encryption status

Phase 7 AES-GCM row encryption and SecureStorage key handling remain the MVP control. **Full-database encryption (SQLCipher) is an explicit production release gate (R-129).** Local DB existence never grants access.

## Dependency / advisory results

| Package | Advisory | Exposure | Mitigation | Release impact |
|---|---|---|---|---|
| SQLitePCLRaw via Microsoft.Data.Sqlite 10.0.4 | NU1903 (R-129) | LocalStore transitive | Row-level AES-GCM; no SQLCipher yet | **Open gate** — do not ship Production offline store without upgrade or full-DB encryption decision |

No debug-only packages added to Release. No warning suppressions.

## Android security findings

- Release APK builds (validated this WP).
- Cleartext allowed only to emulator/localhost domains; documented Production HTTPS replacement gate.
- Diagnostics / Dev auth remain Production-gated (prior phases).
- Interactive device validation **not** claimed (**R-109 open**).
- No penetration-testing claim.

## Explicit exclusions

- Production IdP / JWT/MFA/SSO (R-091) — not invented
- POS operational roles
- Tax/refund/accounting/gateway/export
- Full SQLCipher migration
- Penetration-test or compliance certification claims
- P9-WP02+

## Tests and builds

| Suite | Passed | Failed | Skipped |
|---:|---:|---:|---:|
| Full `ExItS.slnx` Release | **900** | **0** | **0** |

Baseline **882** preserved and exceeded.

## Release blockers (remain open)

- R-091 production authentication
- R-109 interactive Android validation
- R-129 / full-database encryption decision
- Production HTTPS endpoints + MAUI HTTPS-only network config
- Manual GCash verification (R-025)
- Report export / online-only Basic Store (by design until later phases)
- POS operational roles

## Portfolio independence

No unauthorized nested product tree at repo root; keep `ExItS.slnx` to authorized products only.

## Documentation and Git

| Field | Value |
|---|---|
| Feature commit | de4fac64739f5b368a6b1f2490223fa032201b65 |
| Docs hash-record commit | 5558de4e8524b9f2f342a6d3b0fd8c41ebc21303 |
| Final working tree | clean after push |

## Exact next work package

**P9-WP02 — Performance and Reliability** (do not begin until explicitly authorized).
