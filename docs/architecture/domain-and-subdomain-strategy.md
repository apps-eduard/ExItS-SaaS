# Domain and subdomain strategy

**Status:** Authoritative for ExItS Apps public domain and hostname naming.  
**Owns:** Brand domain, marketing paths, application subdomains, environment hostname convention, and the related DNS/email intent.  
**Does not own:** Account classes, organization membership, Personal scope, product entitlement, product-local roles, customer-link semantics, or authorization rules. Those remain in [SaaS scopes, users, boundaries, and navigation](saas-scopes-users-boundaries-navigation.md) and [Product catalog, entitlement, and role model](product-catalog-entitlement-and-role-model.md).

This is a domain and deployment-target architecture. It does not mean the hosts below are deployed, that DNS is configured, or that current production settings have been migrated.

Hosting mode stays [hosted multi-tenant SaaS as the portfolio direction](../Product-Foundation/hosting-and-deployment-operating-model.md) (**D-HOST-01**), with on-prem topology still described separately. Browser host separation and cookies stay [ADR-022](../decisions/ADR-022-separated-antdesign-web-hosts-and-unified-auth.md). This document names the public hostnames for that split. It does not merge those applications.

---

## 1. Purpose

`exitsapps.com` is the official root domain for the ExItS Apps ecosystem.

The root domain is the umbrella for multiple ExItS products. It is not the domain of Pinoy Business POS, Pinoy Loan Manager, or any other single product.

---

## 2. Canonical domains

| Host | Role | Status |
|---|---|---|
| `https://exitsapps.com` | Public marketing website | **DECIDED** name. Deployment **PLANNED**. |
| `www.exitsapps.com` | Redirect to `https://exitsapps.com` | **DECIDED**, unless a later implementation deliberately changes it. |
| `app.exitsapps.com` | Business SaaS entry and organization product launcher | **DECIDED** name. Deployment **PLANNED**. |
| `personal.exitsapps.com` | Personal account and Personal application | **DECIDED** name. Deployment **PLANNED**. |
| `admin.exitsapps.com` | ExItS Platform Administration | **DECIDED** name. Deployment **PLANNED**. |
| `api.exitsapps.com` | Public production API entry and ASP.NET Core backend boundary | **DECIDED** name. Deployment **PLANNED**. |

`app`, `personal`, `admin`, and `api` are platform surfaces. They are not additional SaaS products.

Local development stays on localhost. Production hostnames are not required for local validation.

---

## 3. Marketing paths

Product marketing stays on the root domain so brand and search authority stay in one place.

| Path | Subject |
|---|---|
| `https://exitsapps.com/pos` | Pinoy Business POS |
| `https://exitsapps.com/buy-now-pay-later` | Pinoy Buy Now Pay Later |
| `https://exitsapps.com/loan` | Pinoy Loan Manager |
| `https://exitsapps.com/pawn` | Pinoy Pawn Manager |
| `https://exitsapps.com/service-pro` | Pinoy Service Pro |

Likely shared public paths, not application hosts:

- `/pricing`
- `/features`
- `/help`
- `/blog`

A marketing path is not the runtime application. `https://exitsapps.com/pos` describes the product. `https://pos.exitsapps.com` is the POS application host.

---

## 4. Product application domains

The current catalog has five products. Each has one target application subdomain.

| Subdomain | Product | Product code |
|---|---|---|
| `pos.exitsapps.com` | Pinoy Business POS | `pinoy-business-pos` |
| `bnpl.exitsapps.com` | Pinoy Buy Now Pay Later | `pinoy-buy-now-pay-later` |
| `loan.exitsapps.com` | Pinoy Loan Manager | `pinoy-loan-manager` |
| `pawn.exitsapps.com` | Pinoy Pawn Manager | `pinoy-pawn-manager` |
| `service.exitsapps.com` | Pinoy Service Pro | `pinoy-service-pro` |

`service.exitsapps.com` belongs only to Pinoy Service Pro. Buy Now Pay Later, Loan Manager, and Pawn Manager do not live under that host. The five products stay distinct even when they share Platform infrastructure.

---

## 5. Target hierarchy

```text
exitsapps.com
├── Marketing / public website
│   ├── /pos
│   ├── /buy-now-pay-later
│   ├── /loan
│   ├── /pawn
│   └── /service-pro
│
├── app.exitsapps.com       Business SaaS / organization launcher
├── personal.exitsapps.com  Personal experience
├── admin.exitsapps.com     Platform Admin
├── api.exitsapps.com       API
│
├── pos.exitsapps.com       Pinoy Business POS
├── bnpl.exitsapps.com      Pinoy Buy Now Pay Later
├── loan.exitsapps.com      Pinoy Loan Manager
├── pawn.exitsapps.com      Pinoy Pawn Manager
└── service.exitsapps.com   Pinoy Service Pro
```

---

## 6. Separation rules

- Marketing identity and application identity are separate.
- Product marketing stays at `exitsapps.com/<product-path>`.
- Product runtime applications may use the dedicated subdomains in the table above.
- Platform Admin, Personal, the API, and the business launcher are platform surfaces, not extra SaaS products.
- A dedicated product subdomain does not change the modular-monolith or product-domain ownership already defined for that product.
- Domain routing is not an authorization boundary and must not weaken tenant isolation.

Organization public store paths, such as `/store/{slug}`, remain the future landing design in [platform organization public landing](../engineering/platform-organization-public-landing.md). That design does not move a product under another product's subdomain.

---

## 7. Security principles

These are principles for later deployment. They do not describe an existing production edge configuration.

- HTTPS everywhere.
- Cloudflare is the registrar, DNS, and proxy layer for `exitsapps.com` where that edge applies.
- Production origin addresses and secrets stay out of the repository.
- Sharing the `exitsapps.com` parent domain is not a reason to use `AllowAnyOrigin`.
- CORS allows only the intended application origins, explicitly.
- A cookie or session that spans subdomains is a deliberate decision. ADR-022 already rejects one unrestricted cookie across Platform, organization, and Personal hosts.
- Choosing a hostname does not authorize the caller. Server-side organization, product, and Personal authorization stays authoritative.
- Prefer Cloudflare SSL/TLS Full (strict) once each production origin has a valid certificate.
- Enable HSTS only after every required HTTPS subdomain has been validated.

---

## 8. Environment names

**PLANNED** naming convention. Staging hosts are not claimed to exist.

| Production | Staging |
|---|---|
| `app.exitsapps.com` | `app.staging.exitsapps.com` |
| `api.exitsapps.com` | `api.staging.exitsapps.com` |
| `pos.exitsapps.com` | `pos.staging.exitsapps.com` |

The same `{name}.staging.exitsapps.com` pattern applies to `personal`, `admin`, `bnpl`, `loan`, `pawn`, and `service`.

Do not invent separate environment domains such as `exits-pos-prod.com` or `api-prod-exits.com`.

---

## 9. DNS intent

Logical names only. A records, CNAME targets, origin addresses, hosting vendors, and Cloudflare rules are deployment configuration and are not specified here.

| Name | Intent |
|---|---|
| `@` | Marketing site origin |
| `www` | Redirect to the canonical root |
| `app` | Business application |
| `personal` | Personal application |
| `admin` | Platform Admin |
| `api` | Backend API |
| `pos` | POS application |
| `bnpl` | Buy Now Pay Later application |
| `loan` | Loan application |
| `pawn` | Pawn application |
| `service` | Service Pro application |

---

## 10. Email namespace

Reserved operational addresses. They are not claimed to be provisioned.

- `hello@exitsapps.com`
- `support@exitsapps.com`
- `privacy@exitsapps.com`
- `security@exitsapps.com`
- `billing@exitsapps.com`

When production email is configured, SPF, DKIM, and DMARC are required.

---

## 11. Earlier domain notes

No `exits.ph` references were present in the repository when this decision was recorded.

`docs/engineering/platform-organization-public-landing.md` had illustrated a public host as `exitsapp.com`. Those examples now use `exitsapps.com`. The landing document remains a future store-page design. It is not evidence that those URLs are live.

`docs/Mobile-React/Reports/POS-REACT-RMAP-19-customer-ordering.md` still mentions `exitsapp.com` as historical report text. Leave that report as written.

---

## 12. Decision status

**DECIDED**

- `exitsapps.com` is the canonical root domain for ExItS Apps.
- The five product subdomains in section 4 are the target application names.
- Marketing paths and runtime subdomains are different.
- `www.exitsapps.com` redirects to the apex unless a later change says otherwise.

**PLANNED**

- DNS records, deployments, redirects, certificates, Cloudflare SSL mode, HSTS, staging hostnames, and the reserved mailboxes.

**NOT IMPLIED**

- These hosts are not live because they are documented.
- Current production configuration is not migrated by this document.
- Authorization and business rules are unchanged.
