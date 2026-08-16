# Security Policy

ExItS takes security and tenant isolation seriously. This repository is under active development and is **not** claimed as Production-ready.

## Scope

Security-relevant design is documented in:

- [Security](docs/engineering/security.md)
- [Authentication architecture](docs/engineering/authentication-architecture.md)
- [Authentication threat model](docs/engineering/authentication-threat-model.md)
- [Production readiness audit](docs/engineering/production-readiness-audit.md)

## Reporting a vulnerability

A verified public vulnerability-reporting channel (for example a dedicated security mailbox or GitHub Security Advisories workflow) is **not yet published** for this repository.

Until a channel is published:

1. Do **not** open a public GitHub issue that discloses exploit details or credentials.
2. Prefer a private contact path through the repository owner’s existing GitHub account relationship.
3. Include impact, affected component, and reproduction steps only over a private channel.

Do not include secrets, production credentials, or customer data in any report.

## Expectations for contributors

- Never commit secrets, credentials, payment card data, or PHI.
- Preserve Platform / product database boundaries (no cross-database FK or joins).
- Do not weaken architecture or security tests to make a change pass.
