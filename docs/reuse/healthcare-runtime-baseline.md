# HealthCare Runtime and Repository Baseline (historical)

> **Historical P0-WP02 record only.** The HealthCare product tree is **not** in this ExItS-SaaS workspace and must **not** be restored. Do **not** run any HealthCare restore/build/Compose commands from this document. Current Local Validation and POS MAUI guidance: [development-environment.md](../engineering/development-environment.md) and [repository-boundaries.md](../engineering/repository-boundaries.md).

[Dashboard](../portfolio-progress.md) | [Repository boundaries](../engineering/repository-boundaries.md) | [Dev environment](../engineering/development-environment.md) | [P0-WP02 report](../reports/P0-WP02-baseline-runtime-map.md)

**Work package:** P0-WP02  
**Date:** 2026-07-29  
**Status:** Historical assessment — HealthCare was later removed from the ExItS workspace (see P10-WP02).

---

## 1. Repository topology (as assessed 2026-07-29 — obsolete)

At assessment time the temporary model was a nested ignored `HealthCare/` product repository. That nested product tree is **gone** from ExItS-SaaS. Current topology is Platform + PinoyBusinessPOS only (see [repository-boundaries.md](../engineering/repository-boundaries.md)).

```text
# OBSOLETE Phase 0 sketch — do not recreate
ExItS-SaaS/
└── HealthCare/   # removed from this workspace; do not restore
```

## 2. Git boundary (historical)

| Repo | Responsibility | Current ExItS expectation |
|---|---|---|
| ExItS-SaaS root | Portfolio, Platform, PinoyBusinessPOS | Active |
| HealthCare product remote | Separate product history | Outside this workspace — do not nest |

## 3. Current ExItS development (use these instead)

Follow [development-environment.md](../engineering/development-environment.md):

- Local Validation Platform/POS on **8091** / **8092**
- Prefer PhysicalDevice Tailscale POS APK (`com.exits.pinoybusinesspos`)
- Emulator optional/secondary with an ExItS-named AVD — never HealthCare AVD names

## 4. Historical note on tools and suites

P0-WP02 recorded HealthCare-specific restore/build/test commands and Android SDK gaps against a nested `HealthCare.sln`. Those commands are **not** valid in this repository. ExItS builds via `ExItS.slnx` only.

## 5. Secrets

Never place real connection passwords, JWT signing keys, or Compose `.env` values in docs, commits, or chat logs.
