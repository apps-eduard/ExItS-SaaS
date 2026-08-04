# Repository Boundaries

[Home](../index.md) | [Development environment](development-environment.md) | [Dashboard](../portfolio-progress.md)

## Current model

```text
ExItS-SaaS root repository
├── ExItS.slnx, global.json, Directory.Build.props, Directory.Packages.props
├── src/Platform/* — Platform foundation (including versioned Integration contracts)
├── src/Products/PinoyBusinessPOS/* — PinoyBusinessPOS product
├── src/Shared/* — shared DesignSystem and contracts as applicable
├── tests/* — Platform / POS / architecture safety tests
├── deploy/docker/* — Local Validation, packaging, production compose
└── Portfolio documentation — tracked by root Git
```

The historical HealthCare product source tree is **not** part of this workspace. Do **not** recreate, clone, import, or nest a `HealthCare/` product tree without explicit authorization.

Root `.gitignore` still ignores `/HealthCare/` as a guard against accidental reintroduction. That ignore is not permission to restore the product.

## Root Git responsibility

- Owns ExITS portfolio docs, phase tracking, engineering standards, Platform, and PinoyBusinessPOS.
- Must never track a nested HealthCare application tree, secrets, or build outputs.
- Root remote: `https://github.com/apps-eduard/ExItS-SaaS.git`.

## Platform Integration contracts (not a product tree)

Platform versioned contracts under:

`src/Platform/ExItS.Platform.Application/Integration/HealthCare/`

are **tracked Platform files** (contract boundaries for a possible future product reconnection). They are **not** the HealthCare product source tree and must not be confused with restoring HealthCare into this repo.

## Current ignore policy

Root `.gitignore` excludes at least:

- `/HealthCare/` (repo-root nested product only — not Platform `Application/Integration/HealthCare/` contracts)
- `**/.env` / `**/.env.*` (with `!.env.example` / `!**/.env.example`)
- `**/bin/`, `**/obj/`, TestResults, coverage, IDE folders
- Local DB and certificate patterns

## Prohibitions

Do not:

- `git add HealthCare` / restore a nested HealthCare product tree
- Convert HealthCare into a submodule/subtree without an approved WP
- Commit nested `.env`, `bin/`, `obj/`, or certificates into the root repo
- Point Local Validation or POS MAUI docs at HealthCare AVDs, APKs, DBs, or scripts

## Safety commands

```powershell
# From ExItS-SaaS root
git status --short --branch
Test-Path HealthCare
git ls-files -- HealthCare/
git check-ignore -v HealthCare/
dotnet sln ExItS.slnx list
```

Expected result: no `HealthCare/` directory; `git ls-files -- HealthCare/` empty; `ExItS.slnx` lists no HealthCare projects. Platform contract paths under `Integration/HealthCare/` may be tracked.
