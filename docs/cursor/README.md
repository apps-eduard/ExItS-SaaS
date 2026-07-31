# Cursor Workflow

[Home](../index.md) | [Dashboard](../portfolio-progress.md)

1. Use one work package per prompt.
2. Cursor reads dashboard, active phase and relevant architecture/security documents.
3. Cursor inspects Git and existing code before changes.
4. Cursor implements only approved scope.
5. Cursor runs tests and records exact evidence.
6. Cursor updates dashboard, phase page and a saved report.
7. Cursor creates one focused commit and reports the hash.
8. The owner pastes Cursor’s report into ChatGPT for review and the next prompt.

## Permanent rules

- `.cursor/rules/exits-workflow.mdc` — always apply (repo safety, Git, build, security)
- `.cursor/rules/exits-product-context.mdc` — product work context loading (see [Product Foundation](../Product-Foundation/exits-product-foundation-reference.md))
- [Product bootstrap prompt](../Product-Foundation/product-bootstrap-prompt.md) — docs-only new-product bootstrap (P12-WP05)

- [First Cursor command](first-cursor-command.md)
- [Reusable prompt template](cursor-prompt-template.md)
- [Completion report template](completion-report-template.md)
