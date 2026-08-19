# Platform Admin Web — Design System and UX Foundation

**Status:** Documentation Only — implementation not authorized  
**Source:** PLATFORM-WEB-DOC-04  
**Branch:** `docs/platform-admin-web-v2`  
**Cross-reference:** `frontend-architecture.md` (DOC-03)

---

## 0. Existing Visual Foundation Audit

### 0.1 ExItS DesignSystem (`src/Shared/ExItS.DesignSystem/`)

The shared DesignSystem is the canonical source of ExItS semantic tokens (`--exits-*`). Key findings:

- **Brand palette:** green-based (`--exits-primary: #166534` light, `#4ade80` dark). Teal accent. Warm neutrals.
- **Token categories:** background, surface (regular/elevated/muted), text (primary/muted), border, primary (with hover/contrast/soft), secondary, accent, success/warning/danger/info (with backgrounds), focus, disabled (bg/text/border), overlay, shadows (sm/md/lg).
- **Typography:** IBM Plex Sans primary, Source Sans 3 fallback. Scale from 13px (xs) to 28px (2xl). Semantic type roles: display, page-title, section, body, compact, label, helper, button, monetary. Tabular-nums font for financial values.
- **Spacing:** 4px base scale (space-1 through space-10).
- **Radius:** sm (6px), md (8px), lg (12px), full (pill).
- **Motion:** fast (140ms), base (180ms), slow (220ms). Four easing curves. `prefers-reduced-motion` fully honored (0ms overrides).
- **Density:** compact (default for POS) and comfortable modes via `[data-density]`. Touch targets ≥ 48px (3rem).
- **Theming:** light/dark/system via `[data-theme]` attribute with full `prefers-color-scheme` media query support.
- **Z-index scale:** base(0), sticky(20), nav(30), dropdown(100), overlay(10), drawer(1100), dialog(1200), toast(1300).
- **Breakpoints:** phone (0), tablet (768px), desktop (1024px).
- **Component classes:** `exds-*` prefix. Includes button, icon-button, label, field/input/select/textarea, badge, avatar, spinner, skeleton, divider, stack, grid, card, surface, page, section, toolbar, page-header, tabs, drawer, dialog, toast, alert, loading-overlay, empty/error states, search bar, form groups, data tables (desktop table + mobile card), pagination, money display, quantity stepper, accordion, dropdown.

### 0.2 Platform Admin styling (`app.css`)

The current Admin uses Ant Design Blazor and overrides `--exits-*` tokens with Ant-compatible blue primary (`#1677ff`). This is specific to the Ant Design Admin and will **not** be carried to the React replacement. The React app will use the canonical green-based ExItS brand from DesignSystem.

### 0.3 Shared Web UI (`ExItS.Web.UI/exits-web.css`)

Provides Ant Design browser chrome tokens for Organization/Personal web hosts. Same Ant blue primary override. Not relevant for the React replacement stack.

### 0.4 Brand assets

The brand mark in the current Admin is a text-based "EA" mark rendered in CSS. No canonical logo image file was found. The React replacement should use the same ExItS brand identity conventions without inventing a new logo.

---

## 1. Design Philosophy

The ExItS Platform SaaS Control Center is a B2B administrative console. Its visual language follows these principles:

| Principle | Meaning |
|---|---|
| Clean | Generous whitespace between sections; no decorative borders or ornament. Content breathes. |
| Calm | Neutral backgrounds; color is reserved for status, actions, and brand accents. No competing visual noise. |
| Highly legible | Strong typographic hierarchy. Body text ≥ 14px. Sufficient line height (≥ 1.5 for body). High contrast ratios. |
| Efficient | Common admin tasks are reachable in minimal clicks. Dense tables where appropriate. No unnecessary intermediate screens. |
| Premium without decoration | Quality is expressed through spacing, type, and interaction polish — not gradients, illustrations, or ornamental graphics. |
| Data-dense when needed | List views, audit logs, and status dashboards prioritize information density. Detail views and forms prioritize readability. |
| Strong hierarchy | Page title → section title → body → caption. Weight, size, and color distinguish levels clearly. |
| Fast perceived response | Skeleton loading for data surfaces. Instant navigation transitions. Progress indicators for long operations. |
| Subtle motion | Transitions serve function (drawer open, status change, hover feedback). Never decorative. Never delays work. |
| Clear status semantics | Success (green), warning (amber), danger (red), info (teal) are consistent across all surfaces. Status is never communicated by color alone. |
| Keyboard interaction | All primary workflows are keyboard-operable. Tab order is logical. Focus is always visible. |
| Accessibility first | WCAG 2.2 AA as design target. Semantic HTML. Screen reader support. Contrast compliance. Reduced motion honored. |

This is not a clone of any third-party product's appearance. It is an ExItS-specific administrative experience built on the repository's canonical brand primitives.

---

## 2. Design Token Architecture

The React replacement adopts the canonical `--exits-*` token architecture from `ExItS.DesignSystem`, mapped to Tailwind CSS custom properties / shadcn/ui theming at implementation time.

### 2.1 Color tokens

| Category | Light | Dark | Purpose |
|---|---|---|---|
| `--exits-bg` | `#f3f6f4` | `#0e1411` | Page background |
| `--exits-surface` | `#ffffff` | `#161e1a` | Card/panel background |
| `--exits-surface-elevated` | `#ffffff` | `#1e2822` | Elevated surface (dropdowns, popovers) |
| `--exits-surface-muted` | `#e8eeea` | `#1a221e` | Subtle distinction surface |
| `--exits-text` | `#14201a` | `#eef4f0` | Primary text |
| `--exits-text-muted` | `#5a6b62` | `#a3b3aa` | Secondary/helper text |
| `--exits-border` | `#d5ddd8` | `#2c3a32` | Borders and dividers |
| `--exits-primary` | `#166534` | `#4ade80` | Primary brand / interactive |
| `--exits-primary-hover` | `#14532d` | `#6ee7a0` | Primary hover |
| `--exits-primary-contrast` | `#ffffff` | `#06280f` | Text on primary |
| `--exits-primary-soft` | `#e4f3e9` | `#163524` | Subtle primary background |
| `--exits-secondary` | `#3f5a4a` | `#94a89c` | Secondary actions |
| `--exits-accent` | `#0f766e` | `#2dd4bf` | Accent / teal |
| `--exits-success` | `#166534` / bg `#e4f3e9` | `#4ade80` / bg `#163524` | Success status |
| `--exits-warning` | `#92400e` / bg `#fef3c7` | `#fbbf24` / bg `#3a2f0d` | Warning status |
| `--exits-danger` | `#b42318` / bg `#fbe7e5` | `#ff8a80` / bg `#3a1c1c` | Danger / destructive |
| `--exits-info` | `#0f766e` / bg `#ccfbf1` | `#2dd4bf` / bg `#134e4a` | Informational |
| `--exits-focus` | `#166534` | `#4ade80` | Focus ring color |
| `--exits-disabled-*` | bg/text/border variants | bg/text/border variants | Disabled state |
| `--exits-overlay` | `rgba(15, 23, 30, 0.5)` | `rgba(0, 0, 0, 0.6)` | Modal/drawer backdrop |

### 2.2 Spacing tokens

4px base scale. Defined as `--exits-space-{n}`:

| Token | Value |
|---|---|
| `space-1` | 0.25rem (4px) |
| `space-2` | 0.5rem (8px) |
| `space-3` | 0.75rem (12px) |
| `space-4` | 1rem (16px) |
| `space-5` | 1.25rem (20px) |
| `space-6` | 1.5rem (24px) |
| `space-8` | 2rem (32px) |
| `space-10` | 2.5rem (40px) |

### 2.3 Typography tokens

| Token | Size | Role |
|---|---|---|
| `text-xs` | 13px | Helper text, error messages, captions |
| `text-sm` | 14px | Compact body, labels, secondary text |
| `text-md` | 15px | Body, buttons |
| `text-lg` | 17px | Section titles |
| `text-xl` | 22px | Page titles |
| `text-2xl` | 28px | Display / summary totals |

Font stack: IBM Plex Sans → Source Sans 3 → system-ui fallback.  
Tabular font: IBM Plex Sans with `font-variant-numeric: tabular-nums` for financial/numeric columns.  
Weights: regular (400), semibold (600), bold (700).

### 2.4 Radius tokens

| Token | Value |
|---|---|
| `radius-sm` | 6px |
| `radius-md` | 8px |
| `radius-lg` | 12px |
| `radius-full` | 999px (pill) |

### 2.5 Elevation tokens

| Token | Usage |
|---|---|
| `shadow-sm` | Cards, inputs |
| `shadow-md` | Dropdowns, elevated cards |
| `shadow-lg` | Dialogs, drawers |

### 2.6 Motion tokens

| Token | Duration | Usage |
|---|---|---|
| `motion-fast` | 140ms | Button hover, border transitions |
| `motion-base` | 180ms | Drawer/dialog open, toast appear |
| `motion-slow` | 220ms | Complex layout transitions |

Easing: `ease` (standard), `ease-in`, `ease-out`, `ease-emphasized`.

### 2.7 Density tokens

| Token | Compact | Comfortable |
|---|---|---|
| `control-height` | 2.75rem | 3rem |
| `density-font-size` | 15px | 15px |
| `density-space-unit` | 0.75rem | 1rem |
| `density-card-padding` | 0.75rem | 1.125rem |
| `density-row-height` | 3rem | 3.25rem |
| `density-radius` | 6px | 8px |
| `density-icon-size` | 18px | 20px |
| `touch-target-min` | 3rem (48px) | 3rem (48px) |

The existing shared C# DesignSystem defines Compact and Comfortable density modes. The React Platform Admin adds Balanced as the default density. Compact and Comfortable remain available as user preferences.

---

## 3. Typography / Spacing / Density

### 3.1 Page title hierarchy

1. **Page title** (`text-xl` / 22px, bold 700, `--exits-text`) — one per page, top of content area
2. **Section title** (`text-lg` / 17px, bold 700, `--exits-text`) — groups related content within a page
3. **Subsection / form group title** (`text-md` / 15px, bold 700, `--exits-text`) — within sections

### 3.2 Body and supporting text

- **Body** (`text-md` / 15px, regular 400) — primary content
- **Labels** (`text-sm` / 14px, semibold 600, `--exits-text-muted`) — form labels, column headers
- **Captions / helpers** (`text-xs` / 13px, regular 400, `--exits-text-muted`) — field hints, timestamps, error messages
- **Tabular / numeric** (same size as body, tabular-nums variant, semibold) — financial values, counts, IDs

### 3.3 Table typography

- **Column headers:** `text-xs` (13px), bold, uppercase letter-spacing 0.02em, muted color, background `--exits-bg`
- **Cell text:** `text-md` or `text-sm` depending on density
- **Row hover:** subtle primary-soft background tint

### 3.4 Spacing scale philosophy

All spacing uses the 4px base scale. Consistent gap/padding patterns:
- Inline element gap: `space-2` (8px)
- Form field vertical gap: `space-3` (12px)
- Section gap: `space-6` (24px)
- Page padding: `space-4` (16px) compact, `space-6` (24px) comfortable

### 3.5 Density behavior

Three density modes are available:

- **Compact:** Denser tables, shorter control heights, tighter padding. Suited for experienced operators who prefer maximum information density.
- **Balanced (default for React Platform Admin):** Moderate density that balances information density with readability. Suited for most administrative workflows. This is the approved future React Admin web density model; it does not exist in the current shared C# DesignSystem implementation (which defines only Compact and Comfortable).
- **Comfortable:** Taller controls, more generous padding. Suited for form-heavy flows where readability and input comfort matter (user creation, plan editing).

Forms remain readable even in Compact mode — only table/list density is aggressively compact.

Density preference is remembered per user where persistence is available.

---

## 4. Component Patterns

These are ExItS-level patterns that sit above shadcn/ui primitives. shadcn/ui provides the implementation substrate; these patterns define product-level consistency.

### 4.1 App Shell

Collapsible sidebar navigation (dark theme), sticky top header bar, scrollable content area. Brand mark in sidebar header. User identity, organization context, and session controls in the header.

### 4.2 Page Header

Title + optional subtitle + optional action buttons. Border-bottom separator. Breadcrumbs above the title when navigating into entity detail.

### 4.3 Section Header

Section title + optional subtitle + optional inline actions (e.g., "Add" button). Used to group related content blocks within a page.

### 4.4 Summary / Stat Card

Elevated card showing a key metric (count, status summary). Title label (muted), large display value (tabular-nums, bold), optional trend indicator or status badge.

### 4.5 Status Badge

Pill-shaped badge with semantic color background and text: neutral, primary, success, warning, danger, info. Always includes text label — never color-only.

### 4.6 Search / Filter Toolbar

Search input (pill-shaped, with icon and clear button) + optional filter controls + optional action buttons. Wraps responsively.

### 4.7 Data Table

Desktop: bordered table with column headers, sortable columns, row hover highlight, row actions. Mobile: card-based layout with label/value pairs. Pagination below. Loading state shows skeleton rows. Empty state shows centered message with optional action.

### 4.8 Empty State

Centered layout with optional icon, title, descriptive message, and optional action button. Used when a list/table has no data.

### 4.9 Zero-Result State

Similar to empty state but specifically for search/filter results returning nothing. Message acknowledges the search term and suggests adjustments.

### 4.10 Skeleton / Loading State

Shimmer animation placeholders matching the shape of expected content (text lines, table rows, cards). Used during initial data fetch. Respects `prefers-reduced-motion`.

### 4.11 Inline Error

Field-level error message below the input, danger color, `text-xs`. Input border changes to danger color with danger-bg focus ring.

### 4.12 Full-Page Error

Centered error state with danger icon, error title, descriptive message, and retry/return action. Used for unrecoverable page-level failures.

### 4.13 Forbidden State

Centered message indicating the user lacks permission to access this page. Includes a return-to-dashboard action. Distinct from 404 (route not found).

### 4.14 Confirmation Dialog

Modal dialog with title, message, cancel button (ghost/secondary), and confirm button (primary). Focus trapped within the dialog. Escape dismisses.

### 4.15 Destructive Confirmation

Same as confirmation dialog but the confirm button uses danger styling. May require typing a confirmation phrase for high-impact operations. The destructive action button should never be the default-focused element.

### 4.16 Side Drawer / Sheet

Slides in from the right (or left). Overlay backdrop. Header with title and close button, scrollable body, optional footer actions. Used for detail views, edit forms, and secondary workflows without losing list context.

### 4.17 Detail Panel

Full-width or split-view panel showing entity detail with tabs. Used for organization detail, user detail, subscription detail. Tabs follow the drill-down architecture from DOC-02.

### 4.18 Form Section

Grouped form fields with a section title and optional description. Fields stack vertically. Actions (save/cancel) at the bottom with a border-top separator.

### 4.19 Toast / Notification

Bottom-right stack of transient messages. Semantic border tint (success/warning/danger). Auto-dismiss after timeout. Close button. Slide-up entrance animation.

### 4.20 Audit Timeline

Chronological list of audit entries with timestamp, action description, actor, and optional detail expansion. May use a vertical timeline indicator or a simple list depending on density.

### 4.21 Key / Value Metadata View

Two-column layout (label left, value right) for displaying entity metadata (IDs, dates, statuses). Muted labels, primary-color values. Used in detail panels and drawers.

---

## 5. Motion Rules

### 5.1 Allowed motion

| Interaction | Duration | Easing |
|---|---|---|
| Drawer/sheet open/close | `motion-base` (180ms) | `ease` |
| Dialog open/close | `motion-base` (180ms) | `ease` |
| Dropdown/menu appear | `motion-fast` (140ms) | `ease-out` |
| Toast entrance | `motion-base` (180ms) | `ease` |
| Skeleton shimmer | 1.4s continuous | ease (CSS animation) |
| Button hover/active | `motion-fast` (140ms) | `ease` |
| Focus ring appearance | `motion-fast` (140ms) | `ease` |
| Tab underline transition | `motion-fast` (140ms) | `ease` |
| Layout content transition | `motion-slow` (220ms) | `ease-emphasized` |

### 5.2 Avoided motion

- Decorative page entrance animations
- Bouncing, pulsing, or attention-grabbing elements
- Parallax or scroll-linked effects
- Motion that delays user action (e.g., waiting for an animation to complete before a button becomes clickable)
- Destructive-action animations that obscure confirmation state

### 5.3 Reduced motion

All motion tokens collapse to 0ms when `prefers-reduced-motion: reduce` is active. Skeleton shimmer stops. CSS animations and transitions are suppressed. Full usability is preserved without motion.

---

## 6. Accessibility

### 6.1 Target

WCAG 2.2 Level AA as design intent. This is a target for the replacement frontend, not a compliance claim for the current application.

### 6.2 Requirements

| Area | Requirement |
|---|---|
| Keyboard access | All interactive elements reachable and operable via keyboard. Logical tab order following visual layout. |
| Visible focus | 3px solid focus ring (`--exits-focus`) with 2px offset on all focusable elements. Never hidden or suppressed. |
| Semantic labels | All form inputs have associated `<label>` elements. Groups use `<fieldset>` + `<legend>` where appropriate. |
| Screen-reader names | Icons-only buttons have `aria-label`. Status badges include text, not just color. Loading states announce via `aria-live`. |
| Contrast | Text contrast ≥ 4.5:1 (normal text), ≥ 3:1 (large text, UI components). Both light and dark themes. |
| Form errors | Errors associated with inputs via `aria-describedby`. Error messages use `role="alert"` or `aria-live="assertive"`. |
| Table accessibility | Tables use `<th scope="col">` headers. Sortable columns indicate sort direction via `aria-sort`. |
| Reduced motion | Fully honored per §5.3. |
| No color-only status | Every status badge, alert, and indicator includes a text label or icon in addition to color. |
| Modal/dialog focus management | Focus trapped within open dialogs. Focus moves to dialog on open. Focus returns to trigger on close. Escape key closes. |

---

## 7. Theming

### 7.1 Supported themes

| Theme | Mechanism |
|---|---|
| Light | Default. `[data-theme="light"]` or no attribute. |
| Dark | `[data-theme="dark"]`. Full dark palette defined in DesignSystem. |
| System | `[data-theme="system"]` or no attribute. Follows `prefers-color-scheme` media query. |

### 7.2 Token sharing

All semantic tokens (`--exits-*`) are defined for both light and dark themes. Component styles reference tokens, never hardcoded colors. This ensures theme switching is a token-layer change only — no per-component overrides.

### 7.3 Rules

- No page-specific arbitrary colors outside the token system.
- Environment indicators (development/staging banners) must remain distinguishable in both themes.
- Theme preference is persisted per user (localStorage or server-side setting).
- Theme switching must not cause layout shifts, data loss, or application restart.

---

## 8. Responsive Foundation

### 8.1 Approach

Desktop-first. The SaaS Control Center is primarily used on desktop/laptop viewports. Tablet and narrow viewports are supported for read/navigation tasks but are not the primary optimization target.

### 8.2 Breakpoints

| Name | Width | Behavior |
|---|---|---|
| Large desktop | ≥ 1024px | Full sidebar + content. Dense tables. Multi-column dashboards. Max content width ~64rem. |
| Laptop | 768px – 1023px | Collapsible sidebar. Tables may scroll horizontally. Max content width ~56rem. |
| Tablet | 480px – 767px | Sidebar becomes drawer. Tables switch to card layout. Single-column forms. Page header actions stack. |
| Narrow | < 480px | Drawer navigation. Card-based data. Toasts span full width. Dialogs span full width. |

### 8.3 Rules

- Sidebar navigation collapses to a hamburger-triggered drawer below tablet breakpoint.
- Data tables switch to mobile card layout below 768px (existing DesignSystem pattern).
- Forms remain single-column on all viewports.
- Page header actions wrap to full-width below tablet.
- Dialogs expand to full viewport width on narrow screens.
- Touch targets remain ≥ 48px on all viewports.
- Admin does not need to be optimized for phone-first workflows, but core read and navigation tasks must not break catastrophically on any supported width.
---

## 9. Localization

### 9.1 Supported languages

| Language | Code | Role |
|---|---|---|
| English | `en` | Default language |
| Filipino | `fil-PH` | Supported secondary language |

### 9.2 Localization rules

- No hard-coded user-facing application strings. All visible text uses localization keys/resources from the beginning.
- Do not translate user-entered data (organization names, product names, person names).
- Do not translate technical identifiers (capability IDs, error codes, API paths).
- Locale-aware formatting for dates, numbers, and currencies.
- Test longer Filipino labels for clipping and wrapping in all UI surfaces (sidebar, buttons, table headers, form labels, badges, tooltips).

### 9.3 User preferences

| Preference | Options | Persistence |
|---|---|---|
| Language | English / Filipino | Remembered per user where persistence is available |
| Theme | System / Light / Dark | Remembered per user where persistence is available |
| Density | Comfortable / Balanced / Compact | Remembered per user where persistence is available |
| Sidebar | Expanded / Collapsed | Remembered per user/session |
| Table display | Column visibility, sort order (where supported) | Remembered per user where persistence is available |
| Regional / time-zone | Display preferences for date/time formatting (where supported) | Remembered per user where persistence is available |

### 9.4 Responsive authentication

Authentication screens (Sign In, Register, Forgot Password, Reset Password) must be deliberately polished for phone-sized screens. Mobile login is a first-class UX requirement, unlike the administrative shell which is desktop-first.
