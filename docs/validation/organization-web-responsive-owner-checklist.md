# Organization Web — Owner responsive validation checklist

**Status:** Unchecked — Owner Validation Pending  
**Phase 25:** OPEN  
**Do not mark items verified in documentation until the Owner completes them.**

## Accounts to exercise

- Owner (single org)
- Manager / Organization Administrator
- Cashier (must be denied Organization Web)
- Multi-org Owner/Administrator (workspace chooser)

## Environments

- Desktop (≥1440 and 1024–1439)
- Tablet (768–1023)
- Phone (480–767 and &lt;480)
- Light theme
- Dark theme

## Pages

For each page below, check: layout, primary actions, empty state, error/retry, theme, responsive, authorization.

| Page | Desktop Owner | Tablet Owner | Phone Owner | Desktop Manager | Tablet Manager | Phone Manager | Notes |
|------|---------------|--------------|-------------|-----------------|----------------|---------------|-------|
| Overview | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | |
| Business profile | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | |
| Branches | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | |
| Devices | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | |
| Registers | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | |
| Staff | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | |
| Roles | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | |
| Customers | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | |
| Products | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | |
| Categories | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | |
| Global Catalog | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | |
| Stock | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | |
| Transfers | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | |
| Expiration | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | |
| Sales history | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | Transaction Summary only |
| Sales report | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | |
| Business credit | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | |
| Shifts | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | Read/inspect only |
| Cash / shift report | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | |
| Inventory report | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | |
| Operational settings | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | |
| Sales documents | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | Owner education |
| Subscription | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | |
| Ownership | ☐ | ☐ | ☐ | ☐ deny mutate | ☐ | ☐ | Owner-only mutations |
| Alerts | ☐ | ☐ | ☐ | ☐ | ☐ | ☐ | |

## Cashier denial

| Check | Result |
|-------|--------|
| Login as Cashier | ☐ Denied Organization Web |
| Direct `/organization/branches` | ☐ Denied |
| Direct `/overview` | ☐ Denied |
| Workspace chooser shows Org Web for Cashier org | ☐ Must not |

## Navigation / chrome

| Check | Desktop | Tablet | Phone |
|-------|---------|--------|-------|
| Side nav usable | ☐ | ☐ collapse | ☐ drawer |
| Header (org + profile) | ☐ | ☐ | ☐ prioritized |
| Theme switch | ☐ | ☐ | ☐ |
| Language switch | ☐ | ☐ | ☐ |
| No horizontal page overflow | ☐ | ☐ | ☐ |

## Auth regression smoke

| Check | Result |
|-------|--------|
| Development Test User fills username only | ☐ |
| Manual password + Sign in | ☐ |
| Owner → Organization Web | ☐ |
| Branches loads without development-operator error | ☐ |
