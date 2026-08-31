# POS Release Validation Checklist

Prevents future sessions from blindly re-running every suite. Use with `POS-MASTER-VALIDATION-LEDGER.md` and `POS-MASTER-TEST-MATRIX.md`.

| Field | Value |
|-------|-------|
| **AUTHORITY** | POS-PILOT-TO-MULTIBRANCH-MASTER-VALIDATION-01 |
| **START_SHA** | `ad1f9171dbafc1e71c6ca94f2732dfda787b81ce` |

---

## CLASS A — ALWAYS RUN BEFORE RELEASE

| Check | Command / evidence | Notes |
|-------|-------------------|-------|
| Conflict markers | Search `<<<<<<<` / `=======` / `>>>>>>>` | Fail release if found |
| `git diff --check` | Whitespace / conflict markers | |
| Typecheck (React) | `npm run typecheck` in PinoyBusinessPOS.React | When React ships |
| Lint (React) | `npm run lint` | 0 errors required |
| Build (React) | `npm run build` | When React ships |
| Solution Release build | `dotnet build ExItS.slnx -c Release` | When backend ships |
| Critical auth smoke | Platform login + POS token bind (LocalValidation or staging) | Do not invent fake prod auth |
| Migration compatibility | Apply / rollback / re-apply if migration added | Never `Migrate()` on production startup |

---

## CLASS B — RUN WHEN DOMAIN CHANGED

| Domain change | Run | Skip when |
|---------------|-----|-----------|
| Inventory mutation / branch balance | `PosStockUseApiTests`, `PosWaste*`, BranchBalanceMutation unit, inventory ops Postgres | Docs-only |
| Supplier payables | `PosSupplierPayablesApiTests` | Unrelated UI copy |
| Purchasing / GRN / reversal | Receipt reversal + purchasing API tests | |
| RBAC / reporting auth | Role matrix + `PosReportingApiTests` + Platform invite tests | |
| Customer Utang / AR | Customer/Utang API + React customer tests | |
| Costing / profitability | SaleCostProfit + profitability suites | |
| Customer-order COGS | CustomerOrderSettlementCogs tests | |
| React UI feature area | Targeted Vitest + related Playwright | Full 1354 only if shared shell/nav/session/i18n infra |
| Shared React harness / session | Full `npx vitest run` | |
| Branch fulfillment / customer-ordering UI | UI-CLOSURE-01 live Playwright + targeted React; full vitest if React prod/shared infra changed | Backend already closed in OPERATOR-VALIDATION-01 — do not re-blind-run Platform/POS suites |
| Schema / migration | Affected Postgres integration suite | |

**Full React suite (baseline 1354/1354 as of POS-BRANCH-FULFILLMENT-UI-CLOSURE-01):** run at major release checkpoint or when shared React infrastructure / broad UI / permission navigation changes. Do **not** re-run solely for documentation packages. Branch Fulfillment UI evidence is closed — retest **NOT_REQUIRED** unless fulfillment/customer ordering/shared React infrastructure changes.

---

## CLASS C — MANUAL / FIELD

| Check | When |
|-------|------|
| Cashier daily sell workflow | Real operator / field pilot |
| Owner purchasing + payables UX | Real operator |
| Responsive 360 / 768 / desktop | After layout changes; reuse SC21 if unchanged |
| Discoverability / training friction | Real operator feedback |
| Hardware / device / offline | Later roadmap — **out of scope** until authorized |
| Real GCash gateway | Deferred |

---

## Decision shortcuts

1. **Docs-only commit** → CLASS A conflict/`diff --check` only; reuse all Class B/C evidence.
2. **Backend inventory change** → CLASS A + inventory Class B; invalidate related ledger rows; do not auto-run payables.
3. **i18n only** → locale parity + affected React; skip Postgres purchasing.
4. **Operator feedback P2 polish** → targeted React + changed pages; not full Postgres.
5. **P0/P1 fix** → targeted regression + invalidate only affected evidence + operator retest of exact workflow.

---

## Out of scope until separately authorized

- Device / offline / SQLite / desktop helper / native wrapper
- Real payment gateway / cards
- B2B checkout
- FIFO / GL
- Supplier payment reversal / partial receipt reversal
- Major delivery redesign
