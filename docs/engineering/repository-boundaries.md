# Repository Boundaries

[Home](../index.md) | [Runtime baseline](../reuse/healthcare-runtime-baseline.md) | [Dashboard](../portfolio-progress.md)

## Current model (Phase 2 foundation started)

```text
ExItS-SaaS root repository
├── ExItS.slnx, global.json, Directory.Build.props, Directory.Packages.props
├── src/Platform/* — new Platform foundation (P2-WP01)
├── tests/* — Platform unit + architecture/safety tests
├── Portfolio documentation — tracked by root Git
└── HealthCare/ — nested independent repository, ignored by root Git
```

HealthCare remains temporary-ignored until an approved import WP. **P2-WP01 did not import HealthCare.**

## Root Git responsibility

- Owns ExITS portfolio docs, phase tracking, reuse assessments, engineering standards, and **new Platform foundation projects**.
- Must never accidentally track nested HealthCare application sources, secrets, or build outputs.
- Root remote: `https://github.com/apps-eduard/ExItS-SaaS.git` (still empty / `origin/main` gone as of P2-WP01).
- Baseline tag: `phase-1-approved` → `01ab65b` (local; not pushed).

## Nested HealthCare Git responsibility

- Owns the completed HealthCare MVP history and product remotes (`https://github.com/apps-eduard/HealthCare.git`).
- Remains the authoritative product repository until an approved import/extraction plan.
- Nested `.git` must **not** be deleted without an approved work package.
- Pre-existing dirty nested working-tree files must not be cleaned or overwritten by ExITS portfolio WPs.

## Current ignore policy

Root `.gitignore` excludes at least:

- `HealthCare/`
- `**/.env` / `**/.env.*` (with `!.env.example` / `!**/.env.example`)
- `**/bin/`, `**/obj/`, TestResults, coverage, IDE folders
- Local DB and certificate patterns

## Prohibition against accidental parent tracking

Do not:

- `git add HealthCare`
- Convert HealthCare to a submodule/subtree without an approved WP
- Commit nested `.env`, `bin/`, `obj/`, or certificates into the root repo

## Future options (no import during Phase 0)

| Option | Notes |
|---|---|
| Controlled monorepo import | Remove nested `.git` only after baseline tag + approved plan; import sources with root ignore exceptions |
| Git submodule | Keeps separate history; more ops overhead |
| Separate repository | Continue current model longer; Platform/POS live elsewhere |
| Git subtree | Single tree with vendor history; harder reverse sync |

## Phase 0 closeout recommendation (P0-WP04)

**Recommended immediate direction:** keep the temporary topology. Begin Phase 1 by approving Platform/product boundaries and, when code is authorized, create **new Platform foundations in the root repository without importing HealthCare**. Defer controlled HealthCare monorepo import until after Platform contracts and extraction sequencing are approved (typically Phase 1–2).

Do **not** in Phase 1 start: delete nested `.git`, track `HealthCare/` in root, or create submodules without a dedicated approved work package.

**P1-WP03 confirms:** sequence and rollback are documented in [extraction-sequence.md](../reuse/extraction-sequence.md) and [extraction-rollback-plan.md](extraction-rollback-plan.md) (ADR-013). **P1-WP04 / ADR-014** approve controlled implementation starting at **P2-WP01** (narrow foundation). **No HealthCare import is executed in Phase 1.**

## Safety commands

```powershell
# From ExItS-SaaS root
git status --short --branch
git check-ignore -v HealthCare/
git ls-files HealthCare
git diff -- HealthCare/
git submodule status
Get-ChildItem -Recurse -Force -Directory -Filter .git | Select-Object FullName
```

Expected Phase 0/early Phase 1 result: `HealthCare/` ignored; `git ls-files HealthCare` empty; no HealthCare diff in the root index.
