# Repository Boundaries

[Home](../index.md) | [Development environment](development-environment.md) | [Dashboard](../portfolio-progress.md)

## Current model

```text
ExItS-SaaS root repository
├── ExItS.slnx, global.json, Directory.Build.props, Directory.Packages.props
├── src/Platform/* — Platform foundation (contracts, projections, catalog, auth)
├── src/Products/PinoyBusinessPOS/* — PinoyBusinessPOS product
├── src/Products/PinoyLoanManager/* — PinoyLoanManager product
├── src/Products/PinoyPawnManager/* — PinoyPawnManager (PPM-01 scaffold; no operational database)
├── src/Shared/* — shared DesignSystem and contracts as applicable
├── tests/* — Platform / POS / architecture safety tests
├── deploy/docker/* — Local Validation, packaging, production compose
└── Portfolio documentation — tracked by root Git
```

Active portfolio: **Platform** + **PinoyBusinessPOS** + **PinoyLoanManager** + **PinoyPawnManager** (PPM-01 scaffold only). Adding another product requires explicit authorization and the same ownership and isolation rules.

## Root Git responsibility

- Owns ExITS portfolio docs, phase tracking, engineering standards, Platform, and PinoyBusinessPOS.
- Must never track unapproved product application trees, secrets, or build outputs.
- Root remote: `https://github.com/apps-eduard/ExItS-SaaS.git`.

## Current ignore policy

Root `.gitignore` excludes at least:

- Unapproved repository-root product trees covered by portfolio safety rules
- `**/.env` / `**/.env.*` (with `!.env.example` / `!**/.env.example`)
- `**/bin/`, `**/obj/`, TestResults, coverage, IDE folders
- Local DB and certificate patterns

## Prohibitions

Do not:

- Add an unapproved product source tree
- Convert third-party source into a submodule/subtree without an approved work package
- Commit nested `.env`, `bin/`, `obj/`, or certificates into the root repo
- Point Local Validation or POS MAUI docs at external product AVDs, APKs, databases, or scripts

## Safety commands

```powershell
# From ExItS-SaaS root
git status --short --branch
git ls-files
dotnet sln ExItS.slnx list
```

Expected result: Git tracks only approved portfolio sources, and `ExItS.slnx` lists only active portfolio projects.
