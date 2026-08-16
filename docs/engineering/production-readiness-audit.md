# Production Readiness Audit

[Home](../index.md) | [Deployment architecture](production-deployment-architecture.md) | [Pilot env matrix](../operations/pilot-and-deployment/environment-matrix.md) | [Risks](../risks-and-issues.md) | [Phase 14](../phases/phase-14-production-deployment-and-operations.md) | [P14-WP01 report](../reports/P14-WP01-deployment-architecture-and-production-readiness-audit.md)

**Status:** Authoritative Production readiness audit as of **P14-WP01** (2026-07-31). Discovery and documentation only.

**Verdict:** Portfolio remains **not Production-ready**. Controlled internal technical pilot remains the highest honest readiness state until release blockers below are closed with evidence.

---

## 1. Scope

Audit answers:

1. What Production topology is intended?
2. What packaging/ops already exist?
3. What blocks Production and restricted external pilot?
4. What must later Phase 14 WPs implement vs what is deferred product/business scope?

Out of scope for this document: implementing Docker, TLS, monitoring, or changing `ExItS.Deployment` code (noted as drift where relevant).

---

## 2. Environment readiness board (honest)

| Environment | Decision | Notes |
|---|---|---|
| Development | Ready for engineering | Dev/Testing headers allowed |
| Testing/CI | Ready for automated proof | Testcontainers; headers allowed |
| Staging / internal technical pilot | Ready with documented risks | P9-WP05 packaging; no Dev headers on StagingPilot |
| Restricted external pilot | **Blocked** | TLS + Android interactive + residual auth/email + evaluator alignment |
| Production | **Blocked** | See §3 |

---

## 3. Production release blockers (current)

| ID | Topic | Status | Evidence / residual |
|---|---|---|---|
| **TLS-PROD** | Production TLS + certificate validation end-to-end | **Open** | Pilot nginx TLS template only |
| **MAUI-HTTPS** | MAUI HTTPS-only Production network policy | **Open** | Cleartext limited to localhost/emulator; Production policy not shipped |
| **R-109** | Interactive Android install/TalkBack/network/workflow validation | **Open** | Release build evidence exists; interactive not claimed |
| **R-129** | SQLitePCLRaw NU1903 / local encryption posture | **Open** | Row-level AES-GCM queue; advisory remains |
| **AUTH-EMAIL** | Production outbound email for reset/verification/recovery | **Open** | Auth message sink is no-op; vendor not selected |
| **MFA-ENFORCE** | MFA enrollment/enforcement | **Deferred** | Readiness only (**D-P13-05**) |
| **D-P12-03** | Commercial-state transport contract | **Open** | Must not invent under deployment guise |
| **EVAL-DRIFT** | `ExItS.Deployment` still hard-blocks Production on R-091 / stale POS-ROLES text | **Open (docs finding)** | Phase 13 closed R-091 for scope; P10 closed product-local POS roles — **code evaluator/register not updated in P14-WP01** (docs-only WP) |

### Closed or mitigated relative to older boards

| ID | Disposition |
|---|---|
| **R-091** | **Closed for Phase 13 scope** — passwords, sessions, Bearer, external login, recovery email. Residuals: MFA enforcement, enterprise SSO/AD beyond Google/Facebook, email vendor |
| **POS-ROLES** | **Mitigated (P10-WP06)** — product-local POS role matrix shipped; still ≠ Platform Admin |

---

## 4. Capability inventory (deployment-relevant)

| Capability | Owner | Status | Limitation |
|---|---|---|---|
| Platform authN (session + Bearer) | Platform | Implemented (Phase 13) | Email vendor / MFA enforce residual |
| Platform↔product DB separation | Architecture | Implemented | Must preserve in Production topology |
| Pilot Docker/Compose/nginx | Ops (P9) | Implemented non-prod | Not Production cutover |
| Backup/restore tooling | Platform ops lib | Implemented logical dumps | PITR deferred; off-host schedule env-owned |
| Health/readiness | APIs | Implemented | Not full monitoring |
| Production config fail-closed | APIs (P9-WP01) | Implemented | Necessary not sufficient |
| Production packaging Compose | Ops (P14-WP02) | Implemented local baseline | Not Production cutover; no TLS |
| Production TLS validation | P14-WP03 template | **Partial** — nginx/Compose baseline; customer cutover **TLS-PROD** still open |
| Central monitoring/alerting | — | **Not implemented** | Later WP |
| CI/CD Production pipeline | — | **Not claimed** | Later WP if authorized |

---

## 5. Gap analysis → Phase 14 work packages

| Gap | Suggested WP (do not begin until authorized) |
|---|---|
| Authoritative topology + readiness board | **P14-WP01** (this WP) |
| Production images/Compose/versioning baseline | **P14-WP02** |
| Reverse proxy, TLS, network hardening evidence | **P14-WP03** |
| Production backup/restore ops + rehearsal evidence | **P14-WP04** |
| Monitoring, alerting, support model | **P14-WP05** |
| Align `ExItS.Deployment` readiness/risk register with Phase 13/10 evidence; Production gate honesty | **P14-WP06** |
| Phase 14 closeout | **P14-WP07** |

Business residuals (Manual GCash, online-only limits, report export, tax/accounting, formal WCAG) remain **product/portfolio** items — not silently “fixed” by deployment packaging.

---

## 6. Security honesty checklist

- [x] No Production secrets in repository
- [x] Dev/Testing headers fail closed outside approved environments
- [x] Separate Platform vs product databases required
- [ ] Production TLS end-to-end evidenced
- [ ] MAUI Production HTTPS-only policy evidenced
- [ ] Interactive Android validation evidenced
- [ ] Auth email vendor selected and tested
- [ ] Deployment evaluator text matches closed risks (R-091 / POS-ROLES)
- [ ] Portfolio **not** labeled Production-ready

---

## 7. Recommended next work package

**P14-WP03 — Reverse Proxy, TLS, and Network Hardening** when explicitly authorized.

**Honesty note (Phase 29):** [P29-WP11](../reports/P29-WP11-database-verification-and-constraint-closeout.md) recorded PostgreSQL Testcontainers migration apply/rollback and constraint evidence. [P29-WP12](../reports/P29-WP12-electronic-payment-transaction-reliability-hardening.md) hardens FakePaymentGateway electronic reservation/recovery — that does **not** mean Production Payment Ready or Production Ready. Production backup/restore rehearsal (**P14-WP04**) remains open.
