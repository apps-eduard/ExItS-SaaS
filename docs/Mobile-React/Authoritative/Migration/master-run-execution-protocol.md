# Master Run Execution Protocol

**Status:** AUTHORITATIVE operating model for React/POS migration implementation **after** Product Owner + ChatGPT approve the roadmap.
**Owner authorization:** Product Owner explicitly authorizes this batching/push model for future approved master runs.
**This document does not authorize starting RMAP-00 or any implementation package.**

## Purpose

Eliminate “stop after every WP to ask permission for the next one” once a master run is approved. Continue automatically while gates pass. Hard-stop the batch when risk or contradiction appears. Review between batches of ten.

## Master run model

Future implementation normally executes in **batches of ten** approved roadmap packages.

```text
MASTER RUN A
  RMAP-00 → RMAP-01 → … (up to 10 packages in the approved batch list)
  → HARD STOP FOR PRODUCT OWNER + CHATGPT REVIEW

If defects:
  → dedicated repair/reconciliation command first

When repair passes / review approves:
MASTER RUN B
  next ≤10 packages
  → STOP FOR REVIEW
```

Do not stop after every successful WP merely to ask permission for the next one inside an approved master run.

## Per-WP Definition of Done

For **every** implementation WP:

1. PRECHECK (git baseline, clean tree, dependencies complete)
2. IMPLEMENT approved scope only
3. RUN focused tests
4. RUN required regression
5. UI/responsive validation if UI ([06-react-ui-ux-and-responsive-foundation.md](../06-react-ui-ux-and-responsive-foundation.md))
6. INSPECT diff (explicit paths only)
7. UPDATE WP report
8. UPDATE related Authoritative docs
9. UPDATE capability matrix
10. UPDATE roadmap status
11. UPDATE validation matrix if needed
12. FOCUSED implementation commit
13. FOCUSED docs/report commit where appropriate
14. PUSH
15. VERIFY remote (local HEAD == origin branch HEAD)
16. RECORD SHAs
17. MARK WP PASS
18. CONTINUE to next WP in the approved batch

**Documentation is part of Definition of Done.** A WP is not complete with code only.

UI WPs additionally require Functional + Mobile + Tablet + Desktop + Accessibility + Responsive validation.

## Per-WP Git rules

Every successful implementation WP should normally **push** its validated commits (Product Owner request for future approved master runs).

| Rule | Requirement |
|------|-------------|
| Stage | Explicit paths only |
| Forbidden | `git add .` / `git add -A` / `git add --all` |
| Forbidden | amend, reset, restore/clean to hide unexpected changes, rebase, force-push, automatic main merge |
| Prefer | `feat(pos-react): …` + `docs(pos-react): …` (backend scopes as appropriate) |

After every WP:

```text
git fetch origin
git rev-parse HEAD
git rev-parse origin/<active-branch>
```

Require **local HEAD == remote branch HEAD** before continuing.

## Master-run hard-stop conditions

Stop the entire remaining batch immediately if any WP discovers:

- baseline mismatch
- unexpected dirty/unrelated files
- backend/domain contract contradiction
- unresolved Product Owner business policy
- migration/schema conflict
- authorization/security defect
- cross-organization isolation risk
- accounting/financial invariant conflict
- offline/data-authority conflict
- relevant failing tests that cannot safely be resolved in scope
- remote push failure
- local ≠ remote after push
- dependency required by next WP is not actually complete
- UI contract requires redesign beyond authorized scope
- documentation proves the roadmap order is wrong

When blocked: do not guess; do not continue; preserve valid work; update report if safe; report exact blocker; wait for Product Owner + ChatGPT review.

## Review between master runs

After each batch of ten (or the approved batch size), **STOP** and return a complete report:

- each WP PASS/BLOCKED
- implementation SHAs
- docs/report SHAs
- tests
- visual validation
- migrations
- API changes
- authoritative docs changed
- capability matrix changes
- roadmap changes
- unresolved markers
- branch
- final HEAD
- remote HEAD
- clean working tree
- production readiness flags

Product Owner sends that report to ChatGPT. ChatGPT reviews report, Git state, pushed commits, authoritative docs, material diffs, and roadmap consistency.

If repair is needed: a separate repair master command is issued. Only after repair is approved does the next batch begin.

## Relationship to roadmap

- Roadmap IDs (RMAP-00, RMAP-B00, RMAP-01, …) define **what** to build and in what dependency order.
- This protocol defines **how** approved batches execute, push, and stop.
- Staff identity desired contract (**RMAP-B00**) must complete before React staff-identity parity that depends on the owner-desired person-link model.
- Visual packages depend on **RMAP-00** unless explicitly marked non-UI.
