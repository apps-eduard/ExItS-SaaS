# Design system

ExItS Premium is a Modern Solid interface for the Personal module. The reference is Apple’s level of finish, not a copy of Apple’s UI.

## Surfaces

- Page, surface, and elevated surface come from theme tokens.
- Borders are 1px. Elevation is five levels, 0 through 4. Level 0 is flat. Shadows stay short and are tuned separately for light and dark.
- Default corners are Soft. Pill rounds controls, and keeps cards, dialogs, and sheets practical.
- One primary palette colors interactive fills. Status roles (Success, Info, Warning, Help, Danger), Secondary/Contrast, and Personal feature colors do not become extra Primary themes.
- Color detail: [UI/ExItS-Premium-Color-System.md](./UI/ExItS-Premium-Color-System.md). Success `#34c759`, Warning `#ff8d28`, Danger `#ff383c`, and Info `#0088ff` use dark foregrounds. Help stays `#9333ea`.
- Text on a fill needs a 4.5:1 contrast ratio. Status and feature ink on page and surface uses the same bar.

## Color

**LOCKED.** Reviewed against [Apple’s Color guidance](https://developer.apple.com/design/human-interface-guidelines/color) and checked in `tokens.contrast.test.ts`. Detail is in [UI/ExItS-Premium-Color-System.md](./UI/ExItS-Premium-Color-System.md).

Five palettes: Mint (stored `teal`, default), Blue, Purple (stored `violet`), Orange, Rose. Light primary is the 700 stop with white text. Dark Mint is the 400 stop `#00c8b3` with dark text. Dark Purple is the 300 stop with dark text. Blue, Orange, and Rose scales are unchanged.

Success, Warning, Danger, and Info use the reference fills above, with Light and Dark hover, pressed, and ink variants. They do not follow the selected palette. Help, feature colors, foundation surfaces, and the five elevations are unchanged.

Buttons and solid badges use `--color-{role}-foreground`. Amounts stay on the text color. Do not add a palette or Liquid Glass.

## Type

System font stack (`--font-sans`). Do not replace the font or copy Apple point sizes. Body text is `1rem`. Do not add a second type scale.

**LOCKED.** One semantic scale, declared in `src/styles/tokens.scss` and `global.scss`. Do not add roles or replace `--font-sans`.

| Role | Token / class | Standard size | Weight | Line height |
| --- | --- | --- | --- | --- |
| Display | `--type-display` / `.type-display` | `clamp(1.75rem, …, 2.125rem)` | 700 | 1.15 |
| Page title | `--type-page` / `.type-page` | `clamp(1.375rem, …, 1.625rem)` | 600 | 1.25 |
| Section heading | `--type-section` / `.type-section` | `1.125rem` | 600 | 1.35 |
| Card title | `--type-card` / `.type-card` | `1rem` | 600 | 1.4 |
| Body | `--type-body` / `.type-body` | `1rem` | 400 | 1.5 |
| Supporting text | `--type-secondary` / `.type-secondary` | `0.9375rem` | 400 | 1.45 |
| Label | `--type-label` / `.type-label` | `0.875rem` | 600 | 1.3 |
| Caption | `--type-caption` / `.type-caption` | `0.8125rem` | 500 | 1.4 |
| Numeric | `--type-numeric` / `.type-numeric` | `clamp(1.625rem, …, 2rem)` | 600 | 1.1 |

Caption is the floor for interface chrome. Form controls, buttons, and lists use the density fonts (`--density-control-font`, `--density-button-font`, `--density-label-font`, `--density-list-font`). Density does not change page, section, body, or numeric type. Compact / Standard / Large control text is 14/15/16px, 13/14/16px, 13/14/15px, and 13/14/15px. Badge size is the only reading token density changes.

### Locked and verified

- The nine roles above, `--font-sans`, and body at `1rem` for Compact, Standard, and Large.
- Dialog title uses section type. Dialog description uses supporting text. Toast title uses label. Toast description and Tooltip use caption (`0.8125rem`).
- Playground `font-weight: 650` is now 600 or caption weight.
- Light and dark, 1280px and 390px. A long dialog title wraps inside the panel with no horizontal page scroll. Body stays 16px at Compact, Standard, and Large. At 200% page scale the dialog stays inside the viewport.
- Caption, secondary, and body text on page and surface meet 4.5:1 (`tokens.contrast.test.ts`).
- Financial numeric is **LOCKED.** Size is `clamp(1.625rem, 1.3rem + 0.7vw, 2rem)` (26–32px), weight 600, line height 1.1, tabular lining figures, text color. Density does not change it. The earlier 28–36px weight 700 size is not production.

### Pending

- Time wheel and time input stay at `1.25rem` (20px). That size is a specialized exception until it is checked visually and either kept or mapped to section or card.
- The design-system wordmark (`1.0625rem`) and color-page swatch labels (`0.75rem` / `0.6875rem`) are playground chrome, not product roles.

## Spatial layout

Reviewed against [Apple’s Spatial Layout guidance](https://developer.apple.com/design/human-interface-guidelines/spatial-layout). No new tokens or surfaces.

Already covered:

- Five elevations, `--elevation-0` through `--elevation-4`. Flat lists and inputs stay at 0. Cards rest at 1 and raise to 2. Menus, tooltips, and toasts use 3. Sheets use 4. Dark dialogs use 4. The light dialog panel still uses its own inset highlight and two-layer shadow. That exception is pending approval.
- Page, card surface, and elevated dialog fills stay the Light/Dark tokens in the color system. Dark mode separates page, surface, and elevated fill. Light mode uses the same white for surface and elevated, and separates overlays with the short shadow and the 1px border.
- Text on those fills stays at the existing 4.5:1 bar.

Adopted as principles, using the current system:

- Depth is only for hierarchy. Do not stack extra shadows on a surface that is already separated by fill or border.
- Interactive layers stay distinct: controls sit on a surface, floating layers use level 3, and modal layers use level 4 and the browser top layer.
- Readability stays on solid fills. Do not add 3D layout, blur, or see-through panels.

Out of scope: visionOS windows and volumes, eye tracking, gaze, and visionOS scale or position. Do not add an elevation system or a 3D framework.

No layout gap turned up. The desktop dialog spring is a motion exception, documented in [MOTION_STANDARDS.md](./MOTION_STANDARDS.md), not a spatial-layout change.

## Layout and organization

**LOCKED.** Reviewed against [Apple’s Layout and Organization guidance](https://developer.apple.com/design/human-interface-guidelines/layout-and-organization). Detail is in [LAYOUT_ORGANIZATION.md](./LAYOUT_ORGANIZATION.md).

Keep the mobile-first shell: sidebar, top bar, drawer, inspector, and the current breakpoints. Keep cards, breadcrumbs, tabs, segmented controls, and the navigation hierarchy. Amounts, errors, and informational text stay selectable. Drawer rows stay at least 44px under 768px. Each surface keeps one safe-area inset. Light and Dark share this layout. Do not add a column browser, an outline table, a tvOS lockup, a resizable splitter, Liquid Glass, or a list or table until a product screen needs one.

Known exception: the safe-area inset on a real notched phone is **NOT VERIFIED**.

## Content

**LOCKED.** Reviewed against [Apple’s Content guidance](https://developer.apple.com/design/human-interface-guidelines/content). Detail is in [CONTENT_REVIEW.md](./CONTENT_REVIEW.md).

Use the approved type and financial numeric tokens. Informational text stays readable and selectable. Keep `ExIcon` and Material Symbols Rounded, with accessible names and text contrast. Keep the existing single-line `Input`. Content must fit without horizontal overflow. Do not add a chart, image, or web-view library while the current screens do not need one.

Charts, profile photos, business logos, product images, and multiline notes stay deferred until a workflow requires them. They are not prohibited.

## Components

Reviewed against [Apple’s Components guidance](https://developer.apple.com/design/human-interface-guidelines/components). The category is not locked. Menus & Actions, Navigation & Search, Presentation, Selection & Input, Status, and System experiences are locked on their own below. Detail is in [COMPONENTS_REVIEW.md](./COMPONENTS_REVIEW.md).

Keep the existing buttons, menus, search, selects, navigation, dialogs, sheets, fields, toggles, pickers, badges, and toasts. Do not add an Apple-only system component. Layout and Content stay locked.

## Motion

Review only. The playground review is `#hig-motion` and is not locked. Foundations Motion stays at `#motion`. Durations stay 120 / 180 / 280ms. Detail and the Apple comparison are in [MOTION_STANDARDS.md](./MOTION_STANDARDS.md). Preserve the current springs, control feedback, and Reduce Motion behavior. The one justified gap is that dialog sheets spring while picker sheets ease. Do not add page transitions or a second motion scale.

## Entering data

Reviewed against [Apple’s Entering Data guidance](https://developer.apple.com/design/human-interface-guidelines/entering-data). No components were changed. Inputs, selects, and date, time, and date-time pickers stay as they are.

### Preserve

- `Input` forwards `type`, `inputMode`, `autoComplete`, and `name`. Callers choose the keyboard and autofill token. Playground fields set `autoComplete="off"` so samples do not fill personal data.
- Time entry uses `inputMode="numeric"`, turns autofill and spellcheck off, accepts a pasted time such as `9:30 AM`, and keeps arrow-key steps. Desktop focuses the hour field. Mobile does not, so the keyboard does not open by itself.
- Dates are chosen from the calendar. Today and Now set a value only when the person taps them. Empty stays empty.
- A required text field keeps the native `required` attribute and a visible asterisk. The field error is supplied by the caller.
- Time and date-time commit on Apply. Cancel restores the previous value. A committed field error waits until the editor has closed.
- Date and time display use the locale and the 12-hour or 24-hour cycle already on the picker. Sheets keep the bottom safe area.

### Refine later

- `TimeInput` calls `readTimeEntry` on each change. After the hour is typed and the minute is still empty, it shows “Enter an hour and minute.” as an alert. That fires before the person can finish. Show that message on Apply or when leaving the pair, not after the first field.
- A required date-time picker with no value shows “Choose a date and time.” as soon as the editor opens. On the mobile date step, a chosen date with no time shows “Choose a time.” before the time step. Show those after Apply, or after the person reaches that step.

### Do not adopt

- A password or currency control. None exists. `Input` can already be `type="password"` when a real field needs it.
- Drag-and-drop into fields. Paste and the calendar cover the data these controls accept.
- A second formatter. Do not invent amounts, names, or dates the person did not enter.

## Button

**LOCKED.** Default appearance is `solid`. Convex Solid is opt-in. Do not replace Solid with Convex Solid, and do not restyle Solid, Gradient, Soft, Outline, Ghost, Raise, Soft Raise, or Solid Raise to match it.

### Preserve

- Tones are primary, secondary, accent, success, info, warning, help, danger, contrast, and ghost. Appearances are solid, gradient, soft, outline, ghost, raise, soft-raise, solid-raise, and convex-solid.
- A button with no appearance uses `solid`. Solid is a flat fill of the tone token and the tone foreground. It has no radial, no gradient image, and no drop shadow.
- Gradient runs from the tone to the hover token, top to bottom. Secondary keeps `--color-secondary-border`.
- Raise, soft raise, and solid raise sit on elevation 3, lift to elevation 4 on hover, and settle to elevation 1 while pressed. Solid raise keeps the solid fill.
- Convex Solid (`appearance="convex-solid"`) keeps the center as the exact semantic color. The rim is a radial ellipse, 92% by 100% at 50% 38%, ending in a 72% mix of the pressed token into the tone. Hover moves that ellipse to 50% 32% and mixes 8% white into the upper middle. Pressed clears the image and uses the pressed fill. Disabled uses the disabled fill, disabled text, no image, and no shadow. There is no shadow under the button and no backdrop blur.
- Radius follows `--radius-button`: 8px Standard, 14px Soft, 999px Pill. Height follows density: 34px Compact, 40px Standard, 52px Large.
- Hover, pressed, loading, disabled, focus, and the ripple stay on the shared button rules. Reduced motion removes the press transform and the ripple.
- The playground compares Solid, Gradient, Solid Raise, and Convex Solid with one shared label.

### Do not adopt

- A lighter center on Convex Solid. The center stays the semantic color so the tone foreground keeps its contrast. Help’s solid fill is already at that floor.
- A drop shadow, blur, or a looping animation as the raised center.
- Painting Solid, Gradient, Soft, Outline, Ghost, Raise, Soft Raise, or Solid Raise with the Convex Solid radial.
- `!important` on a button fill.

## Select dropdown

**LOCKED.** One menu for a searchable or plain Select, including Pill. The trigger follows Standard, Soft, and Pill. The menu stays a card. `DropdownMenu` is a command menu and is not this lock.

### Preserve

- The panel width matches the trigger. It opens below and flips above when space is short. Elevation is `--elevation-3`. The corner is `min(var(--radius-control), var(--radius-card))`: 8px Standard, 12px Soft, 22px Pill. Pill does not turn the panel into a capsule.
- Options follow the global radius on the row, not a second scale. Standard is 8px, Soft is 12px, and Pill is fully rounded. Desktop height is 36px Compact, 40px Standard, and 48px Large. These heights are on the option only. Do not change `--control-height` to get them. At 767px and below the option and the search row are at least 44px tall.
- The list padding is `--space-2`, so a highlight does not touch the panel edge. A selected row is an 8% primary tint and a checkmark. It has no left accent bar. Hover, keyboard, and selected stay distinct. A keyboard row uses a 1px primary ring. A selected keyboard row keeps the tint and adds that ring.
- Search is borderless and only present when `searchable` is true. The icon and the field sit on the menu surface. There is no second pill inside the menu. A hairline divider sits under the search row. Keyboard focus turns the search icon `--color-focus`. It does not draw a box around the field. The accessible name is “Search {label}”. A query with nothing left says “No matches”.
- A narrow viewport opens the same menu in the bottom sheet. Single and multiple selection, keyboard movement, filtering, light, dark, and the five palettes stay.

### Do not adopt

- A bordered search field, a focus rectangle on the search row, or a second pill inside a Pill select.
- A left accent on the selected option.
- Option pills on Standard or Soft. Only Pill options are fully rounded.
- A separate dropdown component for this menu.

## Menus and actions

**LOCKED.** Ordinary `DropdownMenu` and `ActionMenu` stay one popover. Opening one closes any other ordinary menu and does not move focus to the menu that closed.

Future changes keep this pattern unless a change is explicitly approved, fixes a verified bug, or addresses an accessibility or security defect. A defect correction is allowed.

### Preserve

- One ordinary menu is open at a time. Select, tooltip, and date or time popovers are not in that set. `ActionMenu` is the icon-only form of `DropdownMenu`, not a second menu.
- Escape closes the active menu and returns focus to its trigger. An outside click closes it. Tab closes it and leaves focus to move on. Arrow Up and Arrow Down move. Home and End jump. Disabled items are skipped and cannot be chosen. Choosing an item runs its callback once.
- A row may include an icon. A normal row uses the text color. A danger row uses the danger token. Hover and keyboard focus use the hover wash. The panel uses `--radius-card`, elevation 3, and the elevated surface. The row uses `--radius-control`. Standard is 12px on the panel and 8px on the row. Soft is 18px and 12px. Pill is 22px and fully rounded.
- The menu stays a popover at every width. It does not become a bottom sheet. At 767px and below each row is at least 44px. Long labels wrap inside the panel. Placement shifts inward so the panel stays in the viewport.
- The review page keeps the standard menu, the three-dot action menu, disabled and danger rows, and the long-label menu at the row end.

### Do not adopt

- A second menu implementation, a context menu, or an action sheet.
- Button dropdown and button group. Both stay deferred. They are not approved components.
- Moving focus to a trigger that closed only because another menu opened.

## Navigation and search

**LOCKED.** The sidebar, drawer, breadcrumb, and `SearchField` stay on the components already in the playground. Searchable Select stays on the Select dropdown lock.

Future changes keep this pattern unless a change is explicitly approved, fixes a verified bug, or addresses an accessibility or security defect. A defect correction is allowed. This lock does not change the Layout and Organization foundation.

### Preserve

- The shell sidebar expands and collapses. The active item uses `aria-current="page"`. Nested groups open and close. Under 768px that sidebar is hidden and the same list opens in the drawer. Closing the drawer returns focus to the control that opened it. Drawer rows stay at least 44px.
- A breadcrumb link is a previous crumb. The current page uses `aria-current="page"`. Long labels truncate. When the trail is narrower than 36rem, or the viewport is 767px or below, the middle crumbs collapse and the current page stays readable.
- `SearchField` covers empty, filled, disabled, and error. Clear is named “Clear search” and resets the query. A disabled field cannot be edited. The field does not filter, suggest, or store history. The screen that owns the list filters it. An empty filtered list says “No matches”.
- Searchable Select remains the locked Select. It is not a second search field.

### Do not adopt

- A second sidebar, drawer, breadcrumb, or search component.
- Filtering, suggestions, or history inside `SearchField`.
- A global search.

### Still separate

The Searching section below still has open decisions: whether Escape clears `SearchField`, whether a filtered list shows a count, and whether empty copy may name the collection. Those decisions are not part of this lock. The 18×18 badge remove control, the light dialog shadow, and the Time/Dialog lint stay unrelated. Dialog Tab containment is part of the Presentation lock.

## Presentation

**LOCKED.** Cards, tooltips, popovers, dialogs, confirmations, and sheets stay on the components already in the playground. The Components category stays unlocked.

Future changes keep this pattern unless a change is explicitly approved, fixes a verified bug, or addresses an accessibility or security defect. A defect correction is allowed.

### Preserve

- A card rests on `--color-surface`. In dark mode that is `#1e293b`. Elevation 1 uses `--elevation-1`. Elevation 2 uses `--elevation-2` on that same surface. Do not brighten elevation 2. Light cards stay on the light surface.
- Tooltip and popover share `--color-surface-elevated`. In dark mode that is `#31445f`. Use the token. Do not paste the hex into a component. Borders stay `--color-border`. Text stays `--color-text`. Light mode stays on the light elevated surface.
- The tooltip radius is `min(var(--radius-control), 12px)`: 8px Standard, 12px Soft, and 12px Pill. A small directional tail points at the trigger and covers the panel border where it joins. The popover has no tail. The tip is not interactive. Hover waits 300ms. Focus waits 40ms. Escape hides it. It prefers the top and flips below.
- The popover uses `--radius-card` and `--shadow-2`, which is elevation 3. It stays interactive, shifts inside the viewport, closes on an outside click or Escape, and returns focus to the trigger.
- Dialog and ConfirmDialog keep their current panel, backdrop, and footer. This lock does not recolor the dialog. The review samples are Edit transaction, Delete transaction, and Transaction details. Tab and Shift+Tab stay inside. Escape and Cancel dismiss. Focus returns to the trigger. Save and Delete run once. When `mobilePresentation="sheet"`, the same dialog docks under 640px. The body scrolls and the footer stays visible.
- Empty state, badge, and skeleton stay the existing samples. A skeleton is a loading state, not a dialog.

### Do not adopt

- A second dark fill for the tooltip or the popover.
- A tail on the popover.
- A brighter elevation 2 card, a gradient, or a heavier card border.
- A new dialog color or a second dialog component.

### Still separate

`Dialog.tsx` still fails ESLint because `setOpenCycle` calls setState inside an effect (`react-hooks/set-state-in-effect`). This lock does not treat that lint as passed. The light dialog shadow, the 18×18 badge remove control, and the Time picker lint stay unrelated.

Context menus, button dropdown, button group, token fields, sliders, steppers, determinate progress, page controls, action sheets, and platform widgets stay deferred or not adopted. Do not add them to fill this page.

## Selection and input

**LOCKED.** Text fields, single and multiple selects, checkboxes, radios, switches, segmented controls, and the existing date and time pickers stay on the components already in the playground. The Components category stays unlocked.

Future changes keep this pattern unless a change is explicitly approved, fixes a verified bug, or addresses an accessibility or security defect. A defect correction is allowed.

### Preserve

- Input appearances stay standard, floating, outlined, and soft. Soft input uses its own height: 48px Compact, 56px Standard, and 64px Large. An empty unfocused label stays centered. Focus or a value scales it and shifts it over 360ms. Search fields and selects do not use that taller field.
- Running accent is one 960ms lap. Border reveal and focus draw stay on their existing durations.
- Select stays single, multiple, searchable, and disabled on the locked dropdown. A multiple select stores the choice on the select `value`. The menu does not change.
- Checkbox can be indeterminate. Radio groups name the choice. A switch exposes on and off. A segmented control can disable one option. Year stays disabled in the review sample.
- Date, range, time, and date-time stay on the date and time picker lock. This lock does not restyle them.
- Validation stays with the caller. The review Save checks the draft once. Clear removes it. Preferences update the sentence beside them.
- Light and dark, Compact, Standard, and Large, and Standard, Soft, and Pill stay on the shared tokens. At 320, 390, 768, and 1440 the review page does not overflow. Choice rows stay at least 44px under 768px. Reduced motion removes the soft-label travel and the segmented indicator travel.

### Do not adopt

- A second input, select, checkbox, radio, switch, segmented control, or picker.
- A visual change to the locked dropdown or the locked date and time pickers.

### Still separate

`Dialog.tsx` still fails ESLint because `setOpenCycle` calls setState inside an effect. The Time picker lint stays open. The light dialog shadow, the 18×18 badge remove control, and a real notched-phone safe area stay unrelated.

## Status

**LOCKED.** Badges, indeterminate loading, toasts, and empty states stay on the components already in the playground. The Components category stays unlocked. Motion stays unlocked.

Future changes keep this pattern unless a change is explicitly approved, fixes a verified bug, or addresses an accessibility or security defect. A defect correction is allowed.

### Preserve

- Paid, Pending, Overdue, Scheduled, and Draft each keep a word, a pip, and an icon. Color is not the only signal.
- There is no progress bar. Save receipt uses the button spinner. Stop save ends it. Show payment rows uses the existing skeletons.
- Success, warning, danger, and info feedback use the existing toast. Dismiss closes it. A danger toast is an alert. The others are status. An auto-dismiss toast keeps the locked right-edge countdown: 6px, the toast tone, full height shrinking toward the top over the 4000ms dismiss clock. Hover or keyboard focus pauses that clock. Reduced motion hides the countdown and keeps the dismiss timing.
- No records, failed loading, and no search results use `EmptyState` with an action.
- Light and dark, Compact, Standard, and Large, and Standard, Soft, and Pill stay on the shared tokens. At 320, 390, 768, and 1440 the review page does not overflow. Save receipt stays at least 44px at 390. Reduced motion removes the button spinner spin and the skeleton sweep.

### Do not adopt

- A determinate progress bar, a separate spinner, or a second snackbar.
- A visual change to Badge, Button, Skeleton, Toast, or EmptyState. The right-edge countdown is part of the locked toast.

### Still separate

The 18×18 badge remove control stays. The Dialog and Time picker lint stay open. A real notched-phone safe area stays unverified.

## System experiences

**LOCKED.** Confirmations, sample access, connection recovery, and session recovery stay on the dialogs, toasts, badges, skeletons, and empty states already in the playground. The Components category stays unlocked. Motion stays unlocked.

Future changes keep this pattern unless a change is explicitly approved, fixes a verified bug, or addresses an accessibility or security defect. A defect correction is allowed.

### Preserve

- Delete uses the danger confirm. Leave and cancel use the safer confirm. Cancel on the dialog returns without that action and restores focus to the trigger.
- Permission required, permission denied, and restricted feature explain the next step. They do not change a device permission.
- An offline or failed send keeps the payment note in the field. Retry records it only on this device.
- Session expired can recover the local draft or discard it. Discard removes it from the page only.
- A local sync uses the pending badge and skeleton, then the existing success or danger toast.
- Light and dark, and 320, 390, 768, and 1440, do not overflow. Delete transaction stays at least 44px at 390. Reduced motion follows the existing dialog and skeleton behavior.

### Do not adopt

- A second confirm dialog, a real permission prompt, or a server call.
- A visual change to Dialog, ConfirmDialog, Button, Toast, Badge, Skeleton, or EmptyState. The right-edge countdown is part of the locked toast.

### Still separate

`Dialog.tsx` still fails ESLint because `setOpenCycle` calls setState inside an effect. That lint is not part of this lock. The light dialog shadow stays unrelated.

## Date and time pickers

**LOCKED.** Reviewed against [Apple’s Pickers guidance](https://developer.apple.com/design/human-interface-guidelines/pickers). Compact, graphical, and wheel styles stay split by surface. Do not add a clock face, a date library, or a second picker.

### Preserve

- Closed fields are compact and share one trigger: a leading icon, the date-field padding, a hover border, and a single-line value. Date uses the calendar mark. Time uses a clock. Date and time uses both.
- The calendar is graphical: month title, previous and next month, a tappable title that opens months and then years, weekday captions, dimmed outside days, a primary fill on the selected day, and a primary ring plus dot for today. Unselected days follow `--radius-badge`. On the date picker, the selected day and today use `--radius-control`. Range days stay on `--radius-badge`.
- Date picker Today and Clear are outline neutral, share the button height, radius, and padding, and stay apart: Today at the start, Clear at the end. Clear has no solid fill.
- A date commits when the day or Today is chosen. Time and date-time commit on Apply. Cancel restores the previous value. Clear removes it.
- Desktop time is three compact columns: Hour, Minute, and AM/PM when the clock is 12-hour. The default minute column is 00 through 59. A minute step still limits that column. Each choice is a 2rem square. The selected value stays primary on the neutral highlight. Arrow keys, digits, and paste still work. While the editor is open, the field and the centered header show the draft in the normal text color, including while a column is scrolling. Now and Clear change that draft. Apply commits it once. Cancel, Escape, and dismissal restore the previous time. Mobile time keeps the touch wheel and the typed fields for paste. That wheel snaps, fades the rows above and below, and keeps the 20px value.
- Desktop date-time is one popover: calendar and the compact time columns together, then one Apply. Mobile date-time stays on two steps inside the bottom sheet, with the touch wheel and typed time. The sheet keeps its handle, scroll, and bottom safe area.
- Light and dark use the existing surface, elevated, primary, hover, and focus tokens. Popovers share card radius, `--space-3`, elevation 3, and `--z-menu`. Width and max height stay with the content. Sheets use elevation 4.

### Unified and stepped date-time

Desktop is the unified pattern: both parts stay visible, and Apply commits them together. Mobile stays stepped. A calendar, a five-row wheel, typed time, and Today, Now, Clear, Cancel, and Apply do not fit a phone sheet unless the date scrolls away while the time is set. The step control avoids that. Do not merge the mobile steps.

### Locked

- Time and date-time triggers match the date field. Checked in light and dark, at Compact, Standard, and Large, and at 390px width.
- Time and date-time popovers use the date popover tokens. The hardcoded `z-index: 60` is gone. Desktop date-time stays one split popover.
- The wheel highlight is one bar. On the phone, selected text is primary at 20px. On the desktop time popover, each choice is a 2rem square and the actions are a 2 by 2 grid of equal size: Clear and Cancel are outline neutral, Now is soft primary, and Apply is solid primary.

### Do not adopt

- An always-visible calendar in a form. The compact field is the right closed state.
- The phone wheel on the desktop popover, or a typed-only time on mobile. Desktop time uses the compact columns. The phone sheet keeps the touch wheel.
- A fully round day cell. Selected and today on the date picker use `--radius-control`. Other days stay on `--radius-badge`.
- A clock face, a new picker component, or a new token.

## Writing

**LOCKED.** Reviewed against [Apple’s Writing guidance](https://developer.apple.com/design/human-interface-guidelines/writing). English stays the only catalog. Do not add locales, Apple product names, or playful copy. Do not rename business words such as payment, record, or groceries.

### Preserve

- Actions already name the outcome: Apply, Cancel, Today, Delete, Close, Clear search, Record payment.
- Confirmations name the action. The destructive sample is “Delete this record?” with confirm label “Delete” and “This action cannot be undone.”
- Validation that tells the person what to do stays: “Choose a time.”, “Enter a valid time, such as 9:30 AM or 21:30.”, “Choose 9:30 or later.”
- Empty search “No matches” and the sample empty state “No payments yet” / “Add a payment” are clear.
- Toast samples state the result: “Saved”, “Could not save”, “Try the action again.”
- The same Apply, Cancel, and Close labels are used on mobile sheets and desktop popovers.

### Adopted principles

Use a short action label, sentence case, and the same word for the same action. Errors say what to do next. Empty states name the next action. Destructive buttons name the destruction. Toasts state the result, not the decoration.

### Locked

- Date placeholders are “Choose a date” and “Choose dates”.
- Range presets are sentence case: “Last 7 days”, “Last 30 days”, “This week”, “This month”, “Last month”.
- The sheet handle is named Close. Toast dismiss stays “Dismiss notification”.
- The “Save changes?” sample action is Save.
- A bad minute interval says “That minute interval is not available.” It does not ask the person to pick an interval.
- A bad earliest or latest time says “The earliest time is not valid.” or “The latest time is not valid.”
- An unrecognized zone says “That time zone is not recognized.” A browser that cannot resolve zones says “This browser can't use that time zone.” It does not tell the person to remove a zone the picker does not expose.
- A repeated clock time says “This time happens twice. Choose a different time.” The picker can change the time. It has no earlier/later control.
- Validation codes and developer comments stay in place. The daylight-saving sentence that says the clock time does not exist and was not moved stays.
- The last datetime check, when no instant remains and the result is not ambiguous, says “Unable to use this date and time.” That sentence is an intentional safety message. It does not replace the specific gap, overlap, zone, or browser messages.

### Do not adopt

Apple’s system wording, a second language catalog, and rewrites of labels that are already specific.

## Searching

Reviewed against [Apple’s Searching guidance](https://developer.apple.com/design/human-interface-guidelines/searching). Inspection only. No search component, token, or string was changed.

### Preserve

- `SearchField` is the shared search control. It is a labeled `type="search"` field with the search icon, the visible label, an optional placeholder, error text, and a clear button named “Clear search”. It has no success state. Theme, type, density, radius, and icons stay on the existing tokens.
- The search icon is decorative. The label is the accessible name. The browser’s native search-cancel glyph is hidden so only “Clear search” clears the field.
- Select search is not a second search component. `searchable` is opt-in. The query filters that menu by label, the accessible name is “Search {label}”, and “No matches” appears when nothing remains. Arrow keys move, Enter chooses, Escape closes, and clear returns focus to the query. On a narrow viewport the same search sits in the bottom sheet.
- The color reference library filters its own 400 swatches and says how many are showing. That behavior stays local to that page.
- Playground navigation stays a section list. There is no global search and no stored search history.
- “No matches” and “Clear search” stay the approved words.

### Adopt as rules, using the current controls

- Put search at the top of the list it filters. One list, one search. Do not add a search field that does not change that list.
- The placeholder names the scope, as in “Name or note” and “Name, shade, or HEX”. The visible label stays. The placeholder is not the name of the field.
- Search is local. The caller filters the current collection. `SearchField` does not search by itself.
- An empty filtered list says “No matches”. The clear control is the way back to the full list.
- Do not add suggestions or search history unless a later task names the list and the privacy rule.

### Refine after approval

- The personal sample and the component gallery render `SearchField` but do not filter a list. The color library and a searchable select are the only working searches.
- Empty copy is not one sentence. Select says “No matches”. The color library says “No reference shades match that search.”
- The color library announces a count. A searchable select does not.
- `SearchField` does not clear on Escape. Select Escape closes the menu instead of clearing the query.
- The select placeholder is the generic word “Search”. The scope is only in the accessible name.
- The painted clear control stays 2rem on Search and 1.75rem on Select. Under 768px the touch area extends to 44px. The icon size stays 20px on Search and 16px on Select.
- Select and the reference library render every match. There is no paging or windowing for a large list.

### Do not adopt

- A global search across the design system or future Personal screens.
- Spotlight, a native search framework, or a second search component.
- Predictive text, saved queries, or search history.
- New tokens or a search-specific type size.

### Decisions before a search lock

1. Use “No matches” for every filtered list, or allow a scoped sentence when the collection name matters.
2. Decide whether Escape clears `SearchField`. Select Escape already closes the menu.
3. Decide whether a filtered list must show a count.
4. Resolved: the painted clear control stays 2rem on Search and 1.75rem on Select. Under 768px the touch area is 44px.
5. Keep `SearchField` presentational. The screen that owns the list does the filtering.

## Icons

Material Symbols Rounded through `ExIcon`. See `ICON_STANDARDS.md`.

## Playground

`http://127.0.0.1:5180/design-system` shows foundations, components, Apple HIG reviews, and the sample personal concept. Colors and Elevation are the visual reference for later Personal work. The concept uses labeled sample data and does not call an API.

Data Table at `#data-table` is integrated and stays **unlocked** until visual approval. Personal Home and the login screen are integrated and stay **unlocked** until visual approval. Motion at `#hig-motion` stays unlocked. Foundations Motion stays at `#motion`.
