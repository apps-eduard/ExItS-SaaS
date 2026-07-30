# Platform Admin Terminology Guide — English ↔ Tagalog (fil-PH)

[UI design system](ui-design-system.md) | [Localization](localization.md) | [Theme system](theme-system.md) | [POS terminology](pos-terminology-guide.md)

Short glossary for `ExItS.Platform.Admin` shell/shared-component copy (`Localization/AdminResources.resx`
and `AdminResources.fil-PH.resx`, P4-WP04). Business pages (Users, Organizations, Subscriptions,
Payments, Entitlements) are not required to be fully localized in P4-WP04; this guide governs the
shell, navigation, and shared components that already use `IStringLocalizer<AdminResources>`.

## Principles

- Keep proper nouns, status codes, permission codes, and audit action codes in English — they are
  data, not copy, and must remain language-neutral for API/DB matching.
- Prefer natural Tagalog for everyday verbs and connective language (Save, Cancel, Loading…).
- Where a literal Tagalog translation would be more confusing than the English technical term
  (Subscription, Entitlement, Dashboard, Audit), keep the English term and, if useful, add a `mga`
  plural marker (e.g. *Mga Subscription*) rather than inventing a new word.
- Never translate usernames, emails, GUIDs, correlation IDs, or free-text reasons entered by an
  operator — these are data, displayed as-is regardless of UI language.

## Glossary

| English | Tagalog (fil-PH) | Notes |
|---|---|---|
| Dashboard | Dashboard | Kept as-is; widely understood as-is in PH tech usage. |
| Organization | Organisasyon | *Mga Organisasyon* for the plural nav label. |
| Trial | Trial | Kept in English; "pagsubok" reads as generic "test," not the commercial concept. |
| Subscription | Subscription | Kept in English; *Mga Subscription* for plural nav label. |
| Payment | Bayad / Pagbabayad | *Mga Bayad* used for the nav label; "Pagbabayad" acceptable in body copy. |
| Entitlement | Entitlement | Kept in English; no natural single-word Tagalog equivalent used in local SaaS products. |
| Audit | Audit | Kept in English; "pagsusuri" is too generic (could mean "review"). |
| User | User | Kept in English over "gumagamit," which reads more formal than the product's tone. |
| Membership | Membership | Kept in English on business pages; not localized in P4-WP04. |
| Product access | Product access | Kept in English; a defined commercial-eligibility term (see `data-authority-matrix.md`). |
| Permission | Permission | Kept in English; matches `PlatformPermission` codes shown in the authorization/me response. |
| Role | Role | Kept in English on business pages. |
| Theme | Tema | |
| Language | Wika | |
| Light (theme) | Maliwanag | |
| Dark (theme) | Madilim | |
| System (theme) | System | Follows OS preference; kept in English. |
| Save | I-save | |
| Cancel | Kanselahin | |
| Confirm | Kumpirmahin | |
| Create | Gumawa | |
| Search | Maghanap | |
| Filter | I-filter | |
| Apply | I-apply | |
| Close | Isara | |
| Loading… | Naglo-load… | |
| No records found. | Walang nahanap na record. | |
| Development-stage | Development-stage | Kept in English; a defined delivery-status term, not a mood word. |
| Unauthenticated | Walang authentication | |
| Actor | Actor | Kept in English; matches `AuditActorType`/audit vocabulary. |
| Outcome (audit) | Resulta | |
| Occurred at (audit) | Naganap noong | |

## Adding a new key

1. Add the English value to `Localization/AdminResources.resx` first (fallback language).
2. Add the Tagalog value to `Localization/AdminResources.fil-PH.resx` in the same commit — never
   leave it blank; `LocalizationResourceTests` fails the build on missing/empty nav-critical keys.
3. If the term is genuinely new (not covered above), add a row to this glossary explaining the
   choice, especially when keeping the English term instead of translating it.
