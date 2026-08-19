# Platform Admin Web Modernization — Decisions

Accepted decision identifiers for this planning track (documentation-only).

| ID | Decision | Status |
|---|---|---|
| PWEB-D-001 | Existing `src/Platform/ExItS.Platform.Admin` remains untouched during replacement | Accepted |
| PWEB-D-002 | Future Platform Admin frontend is a separate application | Accepted |
| PWEB-D-003 | Platform Admin remains a Platform control-plane surface, not a POS or PLM operational console | Accepted |
| PWEB-D-004 | New frontend must consume server-authoritative Platform APIs/contracts; must never directly access Platform persistence | Accepted |
| PWEB-D-005 | Documentation completion does not authorize implementation | Accepted |

Do not decide detailed frontend libraries here. That belongs to DOC-03 and later docs.

