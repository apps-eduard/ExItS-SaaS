# ExItS Product Foundation

Authoritative concise reference for creating or modifying ExItS SaaS products:

- **[exits-product-foundation-reference.md](exits-product-foundation-reference.md)** (P12-WP02)
- **[Templates/](Templates/README.md)** — reusable product documentation templates (P12-WP03)
- **Cursor product context:** `.cursor/rules/exits-product-context.mdc` (P12-WP04)
- **[product-bootstrap-prompt.md](product-bootstrap-prompt.md)** — copy-paste Cursor prompt for docs-only product bootstrap (P12-WP05)
- **[Reference-Product/](Reference-Product/README.md)** — fictional ReferenceLoan dry-run docs (P12-WP06; **not** a real product)

## Scale-ready architecture (EXITS-SCALE-00)

Portfolio-wide **planning** documents. They do **not** implement stamps, sharding, queues, or microservices. They do **not** claim millions of users are currently supported.

After this pack is reviewed and merged to `main`, it is **authoritative architecture guidance** for future product and Platform work. Until merge, treat it as the proposed scale pack on `docs/exits-scale-foundation`.

- **[exits-scale-and-growth-architecture.md](exits-scale-and-growth-architecture.md)** — index, D-SCALE-01–10, honesty gates
- **[unified-control-plane-and-product-plane.md](unified-control-plane-and-product-plane.md)** — one logical control plane; independent product planes
- **[tenant-isolation-routing-and-partitioning.md](tenant-isolation-routing-and-partitioning.md)** — isolation, routing, logical vs physical data
- **[deployment-stamps-and-data-scaling.md](deployment-stamps-and-data-scaling.md)** — stamps/cells; multi-region deferred
- **[async-events-idempotency-and-resilience.md](async-events-idempotency-and-resilience.md)** — durable events, idempotency, cache
- **[capacity-slos-observability-and-disaster-recovery.md](capacity-slos-observability-and-disaster-recovery.md)** — capacity stages, SLI/SLO, backup/DR
- **[service-evolution-and-extraction-strategy.md](service-evolution-and-extraction-strategy.md)** — modularity-first
- **[scale-readiness-checklist.md](scale-readiness-checklist.md)** — review checklist

Related:

- [P12-WP01 contract audit](../reports/P12-WP01-platform-product-contract-audit.md)
- [P12-WP07 foundation closeout](../reports/P12-WP07-foundation-hardening-and-closeout.md)
- [Phase 12 roadmap](../phases/phase-12-product-foundation-and-bootstrap.md)
- `.cursor/rules/exits-workflow.mdc`

Do not invent product business rules from these files alone.
Do not create `src/Products/<Name>/` until a product bootstrap is explicitly authorized.
Default bootstrap outcome is documentation only — no implementation unless separately authorized.

**Phase 12 status:** Complete with documented open decisions (see closeout report). Phase 13 in progress — [authentication architecture](../engineering/authentication-architecture.md) (P13-WP01). Exact next: **P13-WP02** when authorized — do not begin from this foundation alone.
