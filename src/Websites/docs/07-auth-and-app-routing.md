# 07 — Auth and App Routing

## Public Website vs SaaS Application Boundary

| Surface | Domain (candidate) | Purpose |
|---|---|---|
| Public marketing website | `exits.ph` | Presentation, conversion, product info |
| SaaS application workspace | `app.exits.ph` (TBD — WEB-D-04) | ExItS Platform + Product application |
| Backend API gateway | `api.exits.ph` (TBD — WEB-D-04) | ASP.NET Core APIs |

> These hostnames are **architecture candidates only**. Do not claim DNS or deployment is finalized. Confirm during WEB-01 / WEB-09.

---

## Authentication

**Next.js does NOT implement authentication.**

The website links users to the existing ExItS Platform authentication flow. Sign-in and registration are owned entirely by the ExItS Platform API.

The "Get Started" and "Sign In" CTAs on the public website are simple links that navigate the user to the Platform-hosted entry point.

| CTA | Behavior |
|---|---|
| "Get Started" | Link to Platform signup/registration |
| "Sign In" | Link to Platform login |
| "Request a Demo" | Submits contact/inquiry form via website |

No session state, tokens, cookies, or auth logic live in the Next.js website.

---

## Marketing Form Submissions

Contact, inquiry, waitlist, and partnership forms on the public website submit to:

- A backend endpoint on the ExItS Platform API (TBD — WEB-D-08), OR
- A managed email/CRM service (TBD — WEB-D-08)

The form payloads (name, email, message, interest) are **not** stored in a Next.js database. There is no marketing database initially.

Form handling implementation details are resolved during WEB-07.

---

## Public Store Pages

The ExItS Platform already exposes a public store discovery endpoint:

```
GET /api/v1/organizations/public/store/{publicId}
```

This can power future public-facing store landing pages within `exits.ph` or via the existing `ExItS.Personal.Web` surface. The ExItS.Web marketing site may reference but does not duplicate this API.

---

## Route Protection

The public marketing website (`exits.ph`) has no protected routes. All routes are publicly accessible.

Protected SaaS application routes exist only in the SaaS workspace (TBD domain), not in the marketing Next.js project.

---

## Deep Linking from Website to App

When a visitor clicks "Get Started" or "Sign In":
1. They are redirected to the ExItS Platform authentication entry.
2. After successful authentication, the Platform routes them to their appropriate product workspace.
3. The marketing website has no knowledge of or involvement in this routing.

---

## No Parallel Auth Implementation

Do not:
- Implement NextAuth.js or any auth library in `ExItS.Web`
- Create session management, JWT handling, or cookie auth in Next.js
- Mirror or duplicate Platform user accounts in a Next.js database
- Create "magic links" or social login in the marketing website

These are the Platform's exclusive responsibilities.
