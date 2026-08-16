# Repository Boundaries

[Home](../index.md) | [Development environment](development-environment.md) | [Dashboard](../portfolio-progress.md)

## Current model

```text
ExItS-SaaS root repository
├── ExItS.slnx, global.json, Directory.Build.props, Directory.Packages.props
├── src/Platform/* — Platform foundation (contracts, projections, catalog, auth)
├── src/Products/PinoyBusinessPOS/* — PinoyBusinessPOS product
├── src/Shared/* — shared DesignSystem and contracts as applicable
├── tests/* — Platform / POS / architecture safety tests
├── deploy/docker/* — Local Validation, packaging, production compose
└── Portfolio documentation — tracked by root Git
```

Active portfolio: **Platform** + **PinoyBusinessPOS** only. No nested foreign product source tree is part of this workspace. Do **not** recreate, clone, import, or nest external product source without explicit authorization.

## Root Git responsibility

- Owns ExITS portfolio docs, phase tracking, engineering standards, Platform, and PinoyBusinessPOS.
- Must never track a nested foreign product application tree, secrets, or build outputs.
- Root remote: `https://github.com/apps-eduard/ExItS-SaaS.git`.

## Current ignore policy

Root `.gitignore` excludes at least:

- Repository-root foreign product trees covered by portfolio safety rules
- `**/.env` / `**/.env.*` (with `!.env.example` / `!**/.env.example`)
- `**/bin/`, `**/obj/`, TestResults, coverage, IDE folders
- Local DB and certificate patterns

## Prohibitions

Do not:

- Add or restore a nested foreign product source tree
- Convert external product source into a submodule/subtree without an approved WP
- Commit nested `.env`, `bin/`, `obj/`, or certificates into the root repo
- Point Local Validation or POS MAUI docs at external product AVDs, APKs, databases, or scripts

## Safety commands

```powershell
# From ExItS-SaaS root
git status --short --branch
git ls-files
dotnet sln ExItS.slnx list
```

Expected result: Git tracks no nested foreign product tree, and `ExItS.slnx` lists only active portfolio projects.
