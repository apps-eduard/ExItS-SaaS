# ExItS V2 Home — functional parity matrix

Reference: original Personal Home + Org Essentials (`/org`).  
V2 destinations for Utang / Todo / People / Stores / Businesses are **not migrated** — actions show an explicit unavailable state.

| Original feature | Data source | Original navigation | Permissions | V2 Home | Gap / recommendation |
|---|---|---|---|---|---|
| Greeting + avatar + date | Session / profile | — | Personal session | Implemented | Profile photo URL deferred (initials OK) |
| Personal Tracker My Record (I owed / I lent) | `GET /personal/dashboard` totals | `/personal/utang/owe\|lent` | Personal | Implemented (display) | Nav unavailable until V2 Utang routes |
| Shared with Me (I owed / I lent) | dashboard shared totals | same | Personal | Implemented (display) | Same |
| Friends / active relationship metrics | dashboard counts | `/personal/people`, `/personal/utang` | Personal | Implemented (display) | Nav unavailable |
| Needs attention (pending / overdue / due soon) | dashboard + lent/borrowed + contacts | Utang routes | Personal | Implemented (display) | Deep-links unavailable |
| Stores to pay | linked-merchants + POS statements | linked-merchant routes | Personal + buyer token | Implemented (display) | Nav unavailable |
| Businesses / workplaces | product-affiliations + workplaces | enterBusiness / staff login | Personal | Implemented (display) | Enter-business unavailable in V2 |
| Quick actions | static | people/utang/businesses/stores | Personal | Implemented (disabled) | Pending migration |
| To-do Today / Upcoming / Overdue | `GET /personal/todos` | `/personal/todo?tab=` | Personal | Implemented (counts) | Nav unavailable |
| Personal guide card | localStorage | `/personal/guide` | Personal | Implemented (dismissible) | Guide page unavailable |
| Business Today KPIs | management/overview | — | Org management / dashboard | Implemented on Business Home | — |
| Open utang / low stock / open shifts | overview | inventory/customers | same | Implemented | Destinations unavailable |
| Near/expired lots attention | overview | inventory/expiration | same | Implemented | Destinations unavailable |
| Plan & capacity meters | Platform capacity/plan APIs | `/org/*` | Owner/admin gates | **Not in V2 Home** | Documented gap — wire later with Platform capacity clients |
| Glance locations/staff/devices | Platform APIs | `/org/*` | Owner/admin | **Not in V2 Home** | Same |
| Context switch Personal ↔ Business | session + affiliations | workspace switch | membership | Preference toggle when both available | Full workspace bind entry deferred |

## Unmigrated actions (explicit unavailable)

Start Business, I lent, I owe, Stores, Add people, Add to-do, Enter business, Staff login, Guide continue, Utang/Todo/People/Stores destinations, Org Branches/Staff/Areas/Dashboard/Reports.
