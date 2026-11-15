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

Reviewed against [Apple’s Content guidance](https://developer.apple.com/design/human-interface-guidelines/content). Inspection only. Not locked.

Detail is in [CONTENT_REVIEW.md](./CONTENT_REVIEW.md). There is no chart, photo view, multiline text view, or embedded web page. Keep `ExIcon`, the single-line `Input`, and selectable informational text. Do not add a chart library, an image component, a rich-text editor, or a web view until a product screen names the content.

## Motion

Review only. Durations stay 120 / 180 / 280ms. Detail and the Apple comparison are in [MOTION_STANDARDS.md](./MOTION_STANDARDS.md). Preserve the current springs, control feedback, and Reduce Motion behavior. The one justified gap is that dialog sheets spring while picker sheets ease. Do not add page transitions or a second motion scale.

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

`http://127.0.0.1:5180/design-system` shows foundations, components, and the sample personal concept. Colors and Elevation are the visual reference for later Personal work. The concept uses labeled sample data and does not call an API.
