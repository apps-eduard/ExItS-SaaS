# Contributing

Thank you for helping improve ExItS. This guide covers expectations for contributors working in this repository.

## Prerequisites

- .NET SDK matching [`global.json`](global.json) (currently **10.0.302**)
- Docker Desktop (for Local Validation PostgreSQL / Mailpit and optional full Docker app stack)
- PowerShell 7+ recommended for operator scripts

## Architecture boundaries

- Platform and product databases are separate authorities.
- Domain remains persistence-independent; Application must not reference Infrastructure.
- UI projects must not reference Infrastructure, EF Core, or Npgsql.
- Platform Admin uses Ant Design Blazor; PinoyBusinessPOS uses native CSS / DesignSystem unless an ADR says otherwise.
- Do not add an unauthorized nested product tree to the repository.

See [repository boundaries](docs/engineering/repository-boundaries.md) and [development standards](docs/engineering/development-standards.md).

## Local development

Preferred daily workflow:

```powershell
.\tools\Start-LocalValidation.ps1 -PublicHost <your-host>
```

Details: [Local Validation workflow](deploy/docker/README.local-validation-workflow.md) and [README.md](README.md).

## Build and test

```powershell
dotnet restore ExItS.slnx
dotnet build ExItS.slnx -c Release
dotnet test ExItS.slnx -c Release
```

Do not weaken or remove tests to force a pass. Use PostgreSQL/Testcontainers for relational behavior proofs.

## Commits and documentation

- Prefer focused conventional commits (`feat:`, `fix:`, `docs:`, `test:`, `chore:`, …).
- Update only documents affected by the change.
- Do not claim unimplemented capability or Production readiness without evidence.
- Never commit secrets, `.env` files with credentials, or generated junk.

## Pull requests

Keep changes scoped. Include:

- What changed and why
- Test evidence (commands + pass/fail totals when relevant)
- Notes on intentional exclusions

## Security

See [SECURITY.md](SECURITY.md). Do not publish vulnerability details in public issues.
