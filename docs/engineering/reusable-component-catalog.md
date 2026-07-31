# Reusable Component Catalog

[UI design system](ui-design-system.md) | [UI reuse assessment](../reuse/healthcare-ui-reuse-assessment.md)

Documentation-only catalog originally authored in P0-WP03. **P5-WP04 implemented** the shared DesignSystem MVP foundation components listed below (forms, validation, responsive data, money, confirm/feedback). Ant Design remains only inside existing HealthCare Staff Web. Business-specific POS controls (PasswordBox, cart, pickers, Utang, payments) remain deferred.

**P11-WP01** produced an authoritative runtime inventory of Admin, DesignSystem, and POS MAUI surfaces, including consolidation candidates and the recommended shared-component boundary. See [P11-WP01 report](../reports/P11-WP01-web-ui-audit-and-component-inventory.md). This catalog remains the historical planned-component list; P11-WP01 is the source of truth for *what exists today*.

Phase labels:

- **MVP foundation** — Phase 5 MAUI / early POS shell
- **Utang MVP** — Phase 6
- **Basic Store** — Phase 8
- **Full POS** — Phase 10
- **Future** — after commercial MVP unless pulled forward by approved need

---

## Foundation

| Component | Required Phase | Core Features | Accessibility | Localization | Theme Support |
|---|---|---|---|---|---|
| ThemeProvider | MVP foundation | Light/Dark/System; persist; notify | Contrast/focus tokens | N/A | Required |
| LocalizationProvider | MVP foundation | `en`/`fil`; fallback; culture format | Announces language changes where needed | Required | N/A |
| DensityProvider | MVP foundation | Compact / Comfortable | Touch minima in comfortable | Labels for density if exposed | Token-driven |
| ResponsiveLayoutService | MVP foundation | Breakpoints phone/tablet/desktop | Landmark roles | N/A | Surfaces |
| FocusManagement | MVP foundation | Trap/restore for overlays | Required | N/A | Focus-ring tokens |
| ErrorBoundary | MVP foundation | Friendly failure UI | Alert role | Localized message | Themed |

---

## Actions

| Component | Required Phase | Core Features | Accessibility | Localization | Theme Support |
|---|---|---|---|---|---|
| Button | MVP foundation | Variants primary/secondary/danger; busy | Name, disabled | Label from resources | Yes |
| IconButton | MVP foundation | Icon-only with aria-label | Required name | aria-label localized | Yes |
| ButtonGroup | Utang MVP | Related actions | Grouping | Labels | Yes |
| SplitButton | Future | Primary + menu | Keyboard menu | Localized | Yes |

---

## Forms

| Component | Required Phase | Core Features | Accessibility | Localization | Theme Support |
|---|---|---|---|---|---|
| TextField | MVP foundation | Label, help, validation | Associated label | Yes | Yes |
| PasswordField | MVP foundation | Reveal toggle optional | Yes | Yes | Yes |
| NumberField | Utang MVP | Step, min/max | Yes | Localized numbers | Yes |
| CurrencyField | Utang MVP | PHP formatting | Yes | Culture currency | Yes |
| SearchField | MVP foundation | Debounce optional | Yes | Placeholder | Yes |
| TextArea | Utang MVP | Rows, max length | Yes | Yes | Yes |
| SelectField | MVP foundation | Single select; clear; loading; empty | Listbox/combobox pattern | Yes | Yes |
| CheckboxField | Utang MVP | Indeterminate later | Yes | Yes | Yes |
| RadioGroup | Basic Store | Exclusive options | Yes | Yes | Yes |
| ToggleField | Basic Store | On/off | Yes | Yes | Yes |
| DateField | MVP foundation | Native date wrapper | Platform a11y | Labels/validation | Yes |
| DateRangeField | Future / by need | Range | Full calendar a11y if custom | Yes | Yes |
| ValidationMessage | MVP foundation | Field error text | `aria-describedby` | Yes | Danger tokens |
| FormActions | MVP foundation | Primary/secondary layout | Yes | Yes | Yes |

---

## Data display

| Component | Required Phase | Core Features | Accessibility | Localization | Theme Support |
|---|---|---|---|---|---|
| CompactDataTable | MVP foundation | Sort/filter/page; row actions; selection optional | Headers, focus, keyboard | Headings/empty | Density + theme |
| ResponsiveList / table-to-card | MVP foundation | Mobile card mode | List semantics | Yes | Yes |
| Pagination | MVP foundation | Server-side page model | Yes | Page labels | Yes |
| SortControls | Utang MVP | Column sort state | Yes | Yes | Yes |
| FilterBar | Utang MVP | Composed filters | Yes | Yes | Yes |
| StatusBadge | MVP foundation | Semantic tones (not color-only) | Text + color | Status labels | Yes |
| StatCard | Basic Store | KPI display | Yes | Yes | Yes |
| MoneyDisplay | Utang MVP | PHP formatting | Yes | Culture | Yes |
| DateDisplay | MVP foundation | Culture-aware | Yes | Yes | Yes |
| EmptyState | MVP foundation | Illustration optional | Yes | Yes | Yes |
| LoadingState | MVP foundation | Spinner/skeleton | Busy announcement | Yes | Yes |
| SkeletonLoader | Basic Store | Placeholder shapes | Reduced motion | N/A | Yes |
| ErrorState | MVP foundation | Retry action | Alert | Yes | Yes |

---

## Overlays

| Component | Required Phase | Core Features | Accessibility | Localization | Theme Support |
|---|---|---|---|---|---|
| Modal | MVP foundation | Title, body, actions; focus trap | Required | Yes | Overlay tokens |
| ConfirmDialog | MVP foundation | Confirm/cancel | Escape, focus | Yes | Yes |
| Drawer | Utang MVP | Side panel | Focus trap | Yes | Yes |
| Toast | MVP foundation | Success/error/info; auto-dismiss | Live region | Yes | Yes |
| Alert | MVP foundation | Inline persistent | Role alert | Yes | Yes |

---

## Navigation

| Component | Required Phase | Core Features | Accessibility | Localization | Theme Support |
|---|---|---|---|---|---|
| AppShell | MVP foundation | Regions header/nav/content | Landmarks | Yes | Yes |
| Sidebar | MVP foundation | Collapse on tablet/phone | Yes | Nav labels | Yes |
| MobileBottomNavigation | Utang MVP | Primary destinations | Yes | Yes | Yes |
| Tabs | Utang MVP | Panels | Tablist pattern | Yes | Yes |
| Breadcrumbs | Basic Store | Hierarchy | Yes | Yes | Yes |
| PageHeader | MVP foundation | Title + actions | Heading level | Yes | Yes |

---

## HealthCare mapping (informational)

| Future POS component | Closest HealthCare lesson | Do not copy |
|---|---|---|
| Modal / Confirm / Toast | `IUiModalService`, `IUserNotificationService` | Ant implementations |
| SelectField / pickers | Clinic/Org/Patient pickers | Ant `Select` markup |
| CompactDataTable | `.hc-table` + `PagedResponse` | Ant `Table` (unused) / Ant-only chrome |
| DateField | Ant `DatePicker` UX (min/max, labels) | Ant DatePicker control |
| StatusBadge | `StatusTone` + Tag badge | Ant `Tag` dependency |
| Empty/Loading/Error | Web Shared + Mobile state components | Clinical copy |

---

## Build rule

Before creating a component in a later phase, search HealthCare and classify: reusable model, pattern only, product-specific, or unsuitable — then implement in the **native** Platform Admin / POS libraries. Never add Ant Design wrappers to the new Platform Admin.
