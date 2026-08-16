# First Cursor Command — P0-WP01

Copy everything below and paste it into Cursor after opening the **ExITS SaaS** repository that contains the copied completed legacy product and this documentation folder.

---

# ExITS SaaS — Repository and legacy product Reuse Inventory

## Assignment

Execute only:

- Phase: `Phase 0 — Existing legacy product Assessment`
- Work package: `P0-WP01 — Repository and Reuse Inventory`

This is an assessment-first task. Do not move, rename, extract, generalize or refactor production code.

## Required reading

Read:

- `README.md`
- `docs/index.md`
- `docs/portfolio-progress.md`
- `docs/product/`
- `docs/reuse/`
- `docs/engineering/`
- `docs/phases/` (phase pages for the active assignment)
- `docs/cursor/completion-report-template.md`
- `docs/risks-and-issues.md`

## Repository inspection

1. Run `git status` and identify the current branch.
2. Discover the exact folder and solution names. Do not assume the legacy product folder name.
3. Inspect all `.sln`, `.slnx`, `.csproj`, package-management, Docker, CI and deployment files.
4. Identify whether the copied legacy product source is tracked, untracked or nested as another Git repository. Report this clearly; do not delete `.git` metadata without approval.
5. Identify the current .NET version, database provider, authentication model, web render mode and major packages.
6. Search for Ant Design Blazor usage, wrappers, theme configuration and direct component coupling.
7. Search for identity, organizations, memberships, platform admin, subscriptions, feature entitlements, auditing, tenant isolation and tests.
8. Search for legacy product-only domains including clinics, patients, appointments, medical notes and patient self-scope.

## Approved outputs

Update only documentation/report files unless a small non-production validation script is necessary:

- `docs/reuse/reuse-classification-matrix.md`
- `docs/portfolio-progress.md`
- `docs/risks-and-issues.md` when evidence requires it
- `docs/reports/P0-WP01-completion.md`

Record:

- Repository tree and project inventory
- Generic SaaS capabilities and exact locations
- legacy product-specific capabilities and exact locations
- Ant Design usage and whether it is isolated or spread across pages
- Existing reusable table, select/dropdown, date/calendar, modal, validation and theme components
- Existing localization support
- Existing light/dark theme support
- Build/test projects and safe commands
- Database/migration ownership
- CI/CD and deployment structure
- Reuse classification with evidence
- Risks, unknowns and recommended next assessment task

## Explicitly out of scope

Do not:

- Move or rename the legacy product folder or projects
- Create `Platform/` or `Products/` implementation folders
- Change namespaces
- Extract NuGet packages or shared libraries
- Modify runtime behavior
- Change Ant Design usage
- Create PinoyBusinessPOS code
- Modify database migrations
- Upgrade dependencies
- Begin P0-WP02

## Validation

1. Validate Markdown links changed by this task.
2. Run a read-only build/test baseline only when the repository already provides safe commands and required local dependencies are available.
3. If a test requires unavailable secrets/services, do not invent them; record the exact limitation.
4. Report exact commands, passed/failed/skipped totals and exit codes.
5. Do not claim tests passed if they were not run.

## Git requirements

1. Review `git status` before changes and report pre-existing files.
2. Review the final diff.
3. Commit only documentation/report changes with:

```text
chore(docs): assess legacy product platform reuse
```

4. Record the commit hash in the dashboard, phase page and completion report.
5. Confirm the final working tree state. If copied legacy product files remain intentionally untracked, report that honestly rather than claiming clean.

## Required final response

Return exactly:

1. Work package status
2. Repository and solution inventory
3. Reusable platform capabilities
4. legacy product-specific capabilities
5. Ant Design and reusable UI findings
6. Localization and theme findings
7. Database, migration and deployment findings
8. Build/test commands and exact results
9. Documentation changed
10. Risks, blockers and unknowns
11. Commit hash
12. Final Git status
13. Next approved work package

Do not start implementation or extraction.
