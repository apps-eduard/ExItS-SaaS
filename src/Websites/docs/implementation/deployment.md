# Deployment

## Status

Deployment infrastructure is **TBD — WEB-D-03**. The specifications below are architectural guidance and candidates, not finalized configuration.

---

## Container

The Next.js website is packaged as a Docker image.

Suggested `Dockerfile` approach (to be created during WEB-01):
- Multi-stage build: `node:lts-alpine` for build, `node:lts-alpine` for runtime
- `npm ci` → `npm run build` → copy `.next/standalone` output
- Expose port 3000
- Non-root user

---

## Hosting Candidates (TBD WEB-D-03)

| Option | Notes |
|---|---|
| Vercel | Natural Next.js fit; managed CDN; pay-per-use |
| Fly.io | Docker-native; good for Asia Pacific presence |
| Render | Simple Docker deployment |
| Self-hosted (VPS + Nginx) | Maximum control; higher maintenance |
| Azure Container Apps | If ExItS backend is already on Azure |

Choose based on existing ExItS infrastructure, cost, and regional latency requirements for Philippine users.

---

## Domain (TBD WEB-D-03)

Candidate:
```
exits.ph   → Next.js marketing website
app.exits.ph → ExItS SaaS application (TBD WEB-D-04)
api.exits.ph → Backend API gateway (TBD WEB-D-04)
```

DNS configuration is not finalized. Do not claim these subdomains exist until verified.

---

## CI/CD

Recommended:
- GitHub Actions (or equivalent)
- On push to `main`: build Docker image → run Playwright E2E → deploy to staging
- On tag/release: deploy to production

---

## Environment Variables

The marketing website requires minimal environment variables initially:

| Variable | Purpose | Notes |
|---|---|---|
| `NEXT_PUBLIC_PLATFORM_URL` | ExItS Platform API base URL | Required for "Get Started" / "Sign In" link generation |
| `CONTACT_FORM_ENDPOINT` | Contact/waitlist submission endpoint | TBD WEB-D-08 |
| `ANALYTICS_ID` | Analytics provider ID | TBD WEB-D-02 |

Do not commit secrets or production values.

---

## Performance Targets

| Metric | Target |
|---|---|
| Largest Contentful Paint (LCP) | < 2.5s |
| Interaction to Next Paint (INP) | < 200ms |
| Cumulative Layout Shift (CLS) | < 0.1 |
| Time to First Byte (TTFB) | < 800ms |

Verify with Lighthouse CI on every production deployment.

---

## No Separate Database

The marketing website requires no database server. All data flows through existing ExItS Platform APIs or a managed service (TBD WEB-D-08).
