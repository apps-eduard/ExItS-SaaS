# Organization Web — UI & responsive standard

**Status:** Phase 25 OPEN — Owner Validation Pending  
**Audience:** Organization Web (management host only)

## Authentication (Development Test User)

| Step | Behavior |
|------|----------|
| Development Test User selector | Development/Testing **only** |
| Selecting a test user | Fills **username / login identifier** only |
| Password | Remains **empty** — operator types Local Validation shared password manually |
| Sign in | Normal `POST` credentials → Platform login → session cookie → membership resolve → workspace routing |
| Not allowed | Auto-auth, password in browser/JS/API DTOs, fake development actors, auth bypass |

Outside Development/Testing the selector is absent.

## Post-login routing (server-authoritative)

1. **Platform** account → Platform Admin  
2. **One** qualifying Organization Web membership (Owner or Organization Administrator) → that Organization Web workspace  
3. **Multiple** qualifying Organization workspaces → workspace chooser (Cashier / OrganizationMember-only orgs **excluded**)  
4. **Personal-only** → Personal Web  
5. **Cashier** → never Organization Web  

Workspace switcher remains available after login (Personal ↔ authorized Organizations).

## Shared page patterns

Prefer Ant Design + `ExItS.Web.UI` components:

- `ExitsPageHeader` — title + short business description + primary action  
- `ExitsEmptyState` — empty guidance  
- `ExitsField` — labeled form controls  
- `Alert` — success / error (sanitized; never actor ids, permission codes, GUIDs, stack traces)  
- `Modal` / Drawer — create/edit forms (not raw inline DTO forms)  
- `Skeleton` — loading  

### Error copy

| Condition | Message |
|-----------|---------|
| 403 / missing permission | You don't have permission to view this section. |
| Session expired/invalid | Your session has expired. Please sign in again. |
| Generic load failure | We couldn't load this information. Try again. |

`OrgWebUi.Error` sanitizes development-operator and `platform.permission.*` detail.

## Responsive targets

| Breakpoint | Behavior |
|------------|----------|
| ≥1440 | Full sidebar; wide tables/reports |
| 1024–1439 | Persistent sidebar; sensible form max-width |
| 768–1023 | Collapsible / drawer nav; stacked filters |
| 480–767 | Drawer nav; one-column forms; touch actions |
| &lt;480 | No horizontal page overflow; prioritize menu + org + profile |

## Role visibility

| Role | Organization Web |
|------|------------------|
| Owner | Allowed (management) |
| Organization Administrator / Manager | Allowed (non–owner-only mutations denied) |
| Cashier / POS-only Member | **Denied** |
| Device identity | **Denied** |

Organization Web remains **management-only** (no checkout / cart / payment taking).

## Privacy

Test User may show display name, username, and role/scope hint. It must **not** expose passwords, tokens, or extra Personal PII. Phase 21 remains OPEN.
