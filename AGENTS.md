# Repository Instructions

## Git workflow

- Start every task from a clean working tree.
- Verify `main = origin/main`.
- Review staged and unstaged changes before committing.
- Build and test before pushing.
- Create focused commits.
- Push normally to `origin/main`.
- Never force-push, reset shared history, or rewrite shared history.
- Report every commit hash.

## Conventional Commits

Use these prefixes:

- `feat:` new user-facing behavior or UI
- `fix:` bug correction
- `test:` tests only
- `docs:` documentation only
- `refactor:` internal restructuring without behavior change
- `chore:` maintenance, tooling, or dependency work

Use scopes where helpful:

- `feat(pos): improve manager dashboard`
- `fix(platform): prevent duplicate role assignments`
- `test(pos): cover account-menu navigation`
- `docs(p15): record RBAC implementation`

## New and copied files

- Treat every non-ignored file added anywhere under the repository root as part of the working tree.
- Always run `git status --short` before and after implementation.
- Review all `??` untracked files.
- Never silently ignore or discard a new file.
- Add intended new files explicitly or with `git add -A` only after reviewing them.
- Do not commit generated outputs, secrets, credentials, local environment files, build artifacts, caches, APKs, binaries, or IDE folders.
- Use `.gitignore` only for files that should never be version-controlled.
- If a new file is unexpectedly ignored, run:
  `git check-ignore -v <path>`
- If a copied file belongs in source control, ensure it is not covered by an incorrect ignore rule.
- Do not automatically commit every copied file without review.

## End-of-task verification

Run:

- `git status --short`
- `git diff`
- `git diff --cached`
- relevant build/tests
- `git push origin main`
- final `git status`

Finish only when:

- `main = origin/main`
- working tree is clean
- all intended new files are committed
- ignored/generated/local files remain uncommitted

## Pre-commit safety check (manual)

This repository does **not** use a managed Git hooks path (no `.githooks` / `core.hooksPath`). Do not silently change other developers' local Git config.

Before committing, run:

```powershell
powershell -File scripts/git/pre-commit-check.ps1
```

The script reports untracked files, staged files, and obvious staged secrets or generated artifacts. It does **not** auto-stage or auto-commit.

### Optional local hook wiring

A developer may opt in locally (not required for the team):

```powershell
# Example: .git/hooks/pre-commit (Git Bash / sh):
#   #!/bin/sh
#   powershell.exe -NoProfile -File scripts/git/pre-commit-check.ps1
```

Prefer documenting and running the script manually so configuration stays opt-in.
