# Admin UI standards compliance checklist

Phase marker: temporary audit after Ant Design Admin UI alignment pass.
Authority: `docs/ui/ant-design-admin-ui-standards.md`
Starting SHA: `f318006`
Audit date: 2026-08-03

Legend: **Compliant** = structure + shared tokens/components applied for dates, money, status, elevation, terminology. **Deferred** = intentional with reason.

| page/component | pattern | previous | changes | final | shared components | residual |
|---|---|---|---|---|---|---|
| AdminPageHeader | Shared | Partial | CSS unify with page-header | Compliant | self | — |
| AdminStatusTag (new) | Shared | Missing | Ant Tag status map | Compliant | StatusBadge | — |
| AdminMoneyDisplay (new) | Shared | Alias | Wraps AmountDisplay | Compliant | AmountDisplay | — |
| AdminDateTimeDisplay (new) | Shared | Alias | Wraps LocalTimestamp | Compliant | LocalTimestamp | — |
| AdminSection (new) | Shared | Missing | Elevated Card wrapper | Compliant | Card | — |
| AdminSummaryCard (new) | Shared | Partial | Metric card | Compliant | Card | — |
| AdminEmpty/Loading/Error/FormActions/FilterBar | Shared | Partial | Thin Ant wrappers | Compliant | Empty/Spin/Alert/FilterBar | — |
| UtcTimestamp | Shared | Technical UTC text | Delegates to LocalTimestamp | Compliant | LocalTimestamp | Tooltip still shows UTC |
| StatusBadge | Shared | Trialing=warning | Trialing=info | Compliant | — | Prefer AdminStatusTag in new UI |
| app.css | Shared | Flat cards / max-content tables | Elevation + full-width tables + spacing | Compliant | tokens | Nested table-in-card scroll edge cases |
| AdminDashboard | Dashboard | Bare Statistic | AdminSummaryCard | Compliant | AdminSummaryCard | — |
| Users | List/Detail | Format=u, Billing Admin label | LocalTimestamp, AdminStatusTag, role label | Compliant | AdminStatusTag, LocalTimestamp | Dual account-profile tags by design |
| PlatformRoles | List/Detail | Format=u | LocalTimestamp, AdminStatusTag | Compliant | AdminStatusTag, LocalTimestamp | — |
| Organizations | List/Detail | Flat status/money | AdminStatusTag, AdminMoneyDisplay | Compliant | AdminStatusTag, AdminMoneyDisplay | — |
| Products | List/Detail | Raw status tags | AdminStatusTag | Compliant | AdminStatusTag | — |
| Plans | List/Detail | Raw money/status | AdminStatusTag, AdminMoneyDisplay | Compliant | AdminStatusTag, AdminMoneyDisplay | — |
| Subscriptions | List/Detail | GUID primary, UTC | AdminStatusTag, AdminMoneyDisplay, Record ID | Compliant | AdminStatusTag, AdminMoneyDisplay | Advanced technical fields remain secondary |
| Payments | List/Detail | Mixed | AdminMoneyDisplay, LocalTimestamp, AdminStatusTag | Compliant | AdminMoneyDisplay, LocalTimestamp, AdminStatusTag | — |
| Entitlements | List/Detail | GUID tooltips | Friendly links, AdminStatusTag | Compliant | AdminStatusTag, LocalTimestamp | — |
| Audit | List/Detail | UtcTimestamp, GUID | LocalTimestamp, AdminStatusTag | Compliant | AdminStatusTag, LocalTimestamp | List still uses ReportHeader |
| LocalValidationTestPayments | Form | Flat card | admin-elevated-card | Compliant | Card | GUID inputs intentional for LV ops |
| ProductEntry | Form | ToString(u) | LocalTimestamp | Compliant | LocalTimestamp | Legacy HTML form deferred (product entry tool) |
| OrganizationCommercial | Commercial | Flat cards, raw money | Elevated cards, AdminMoneyDisplay, AdminStatusTag, role labels | Compliant | AdminMoneyDisplay, AdminStatusTag | — |
| PersonalStartBusiness | Commercial | Flat plan cards | Elevated, AdminMoneyDisplay | Compliant | AdminMoneyDisplay | Re-login UX preserved |
| OrganizationMembers | List | Format=u, OrgAdmin assignable | LocalTimestamp, AdminStatusTag, no new OrgAdmin assign | Compliant | AdminStatusTag, LocalTimestamp | Legacy OrgAdmin only when already assigned |
| OrganizationInvitations | List | Format=u | LocalTimestamp, AdminStatusTag | Compliant | AdminStatusTag, LocalTimestamp | — |
| OrganizationEnabledProducts | List | Mixed | AdminStatusTag, LocalTimestamp, elevated | Compliant | AdminStatusTag | — |
| OrganizationProductAccess | Form | UtcTimestamp | LocalTimestamp, AdminStatusTag | Compliant | LocalTimestamp, AdminStatusTag | Evaluation IDs secondary |
| OrganizationRoles | List/Detail | Flat | Elevated, AdminStatusTag | Compliant | AdminStatusTag | — |
| OrganizationBranding | Settings | Flat | Elevated card | Compliant | Card | — |
| OrganizationUsers | List | Thin | Header/status polish | Compliant | AdminPageHeader where used | Empty-state polish residual |
| PersonalScopePages | List/Settings | Raw dates/money | LocalTimestamp, AdminMoneyDisplay, AdminStatusTag | Compliant | AdminMoneyDisplay, LocalTimestamp, AdminStatusTag | — |
| Login / Register / ActivateAccount | Auth | Ant login-card | Terminology notice only | Compliant | — | Auth shell intentionally not AdminPageHeader |
| ForgotPassword / ResetPassword / ChangePassword / Recovery* / ExternalLoginCallback | Auth | Raw login-panel HTML | Deferred | Deferred | — | Migrate to Ant login-card in follow-up WP; behavior preserved |
| AcceptOrganizationInvitation | Auth | Ant OK | Deferred polish | Deferred | — | Minor Ant alignment only |
| Error / NotFound | System | Minimal | Deferred | Deferred | — | System pages; Result pattern follow-up |
| LocalValidationIdentityPicker | Auth widget | OK | Deferred visual only | Deferred | — | Embedded Quick Login; Production-hidden |
| MainLayout / AdminNav / OrgSwitcher / EmptyLayout / ReconnectModal | Layout | OK | Role label via shell | Compliant | AdminShellContext | — |

## Intentionally deferred

1. Full migration of every `PageHeader Class="exits-page-header"` to `AdminPageHeader` — visual CSS unified; markup migration is non-functional risk.
2. Auth recovery/password pages still on legacy HTML login-panel — convert to Ant Design login-card in a dedicated auth UI WP.
3. Report* shared reporting kit — already has its own ReportHeader/ReportSummaryCard standards; left intact.
4. ProductEntry legacy HTML form chrome — functional tool; LocalTimestamp fixed only.
5. Manual pixel QA of every breakpoint/theme in a browser session — code-level alignment complete; operator LV visual pass recommended after restart.

## Validation notes

- No `Format="u"` remaining under `Components/Pages`.
- Friendly dates: `dd MMM yyyy, h:mm tt` via `UserTimeZoneState.FormatLocal`.
- Money: `PHP 699.00` via `AmountDisplay` / `AdminMoneyDisplay`.
- Business logic, authorization, Start Business, Quick Login, payments unchanged.
