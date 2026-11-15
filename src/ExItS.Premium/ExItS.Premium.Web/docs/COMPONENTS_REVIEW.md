# Components

Reviewed against [Apple’s Components guidance](https://developer.apple.com/design/human-interface-guidelines/components) and the category pages for menus and actions, navigation and search, presentation, selection and input, status, and system experiences. The three approved mobile hit-area corrections are recorded below. Components stay unlocked.

This is not locked. Layout and organization stays locked in [LAYOUT_ORGANIZATION.md](./LAYOUT_ORGANIZATION.md). Content stays locked in [CONTENT_REVIEW.md](./CONTENT_REVIEW.md). Those decisions are not reopened here.

Apple-only pieces that do not transfer: menu bars, Dock menus, Home Screen quick actions, ornaments, windows, UIKit sheets, SF Symbols, color wells, digit entry views, virtual keyboards, activity rings, gauges, status bars, widgets, Live Activities, complications, Top Shelf, and Watch faces.

## Playground check

Running playground at `http://127.0.0.1:5180/design-system`. Light and Dark were set with `data-theme`. Widths include 320, 390, 768, and 1440. Keyboard Tab was checked in headless Chrome. Reduced motion was emulated for the segmented thumb.

| Check | Result | Evidence |
| --- | --- | --- |
| Horizontal overflow, 390, Light, component sections listed below | PASS | Page and main overflow 0 on Button, Search, Selects, Dialogs, Checkboxes, Radio, Segmented, Badges, Breadcrumbs, Toasts, Date & Time, Advanced, Toggles |
| Horizontal overflow, 390, Dark, Date & Time and Dialogs | PASS | Page overflow 0 |
| Horizontal overflow, 768 and 1440, Light and Dark | PASS | Page and main overflow 0 on Button, Search, Dialogs, Segmented, Badges, Date & Time, Personal concept |
| Switch, checkbox, and radio row height at 390 Light | PASS | Visible labels measured at least 44px. The native input is visually hidden at 1px, which is expected |
| Dialog Escape at 390 Dark | PASS | The open “Save changes?” dialog closed after Escape |
| Segmented option height at 390 | PASS | See the mobile hit-area check. The visible option stays 38px. The radio hit area is 44px |
| Date clear target at 390 Dark | PASS | See the mobile hit-area check |
| Badge remove at 390 | CONSTRAINT | Painted control stays 18×18. A 44×44 hit area is not safe in this layout |
| Keyboard Tab and Shift+Tab | PASS with one dialog gap | Headless Chrome, real key events. See Keyboard focus |

## Mobile hit areas

Approved corrections. Measured on the running playground. Light and Dark. Overflow was 0 at 320, 390, and 1440.

| Fix | Result | Measured |
| --- | --- | --- |
| Segmented control | PASS | At 320 and 390 the visible option stays 38px and each radio hit box is 44px. Adjacent hit boxes overlap by 0. The seam hits the segment it belongs to. At 1440 the hit box matches the 34px option, so the mobile extension is not applied on desktop. Reduced motion sets the thumb transition to 0s |
| Date, time, date-time, and date-range clear | PASS | Painted control stays 32×32. At 390, a point 4px outside each edge hits Clear for Date, Bounded date, Start time, Date range, Light date, and Light time. An edge tap on Start time cleared 9:30 AM and left the field closed. At 1440 the same point does not hit Clear. Clear color follows Mint, Blue, Purple, Orange, and Rose |
| Badge remove | CONSTRAINT | Painted control stays 18×18 at 320. The label “Neutral” is not the remove button, and a point 6px past the control is not the remove button. A centered 44×44 box would cover about 8px of the label (the gap is 0.28rem) and would meet the next badge (the row gap is 0.6rem). The hit area was not enlarged. A future removable badge should put Remove on the parent row, as a labeled button or a menu item, instead of enlarging this mark |

## Category table

| Apple category | Existing ExItS | Decision | Issue / Reason | Priority |
| --- | --- | --- | --- | --- |
| Buttons | `Button` — tone, appearance, loading, icon-only | Preserve | Initiates an action. Primary, danger, and quieter styles exist. Loading and disabled exist. Tests exist; this review did not rerun them | None |
| Menus | `DropdownMenu` | Preserve | Opens on demand, skips disabled items, supports arrows and Escape in source, danger tone exists | None |
| Pull-down / pop-up buttons | `Select` for a value. `DropdownMenu` for a command | Preserve | A value picker and a command menu stay separate. Do not merge them | None |
| Context menus | None | Defer | No row yet needs a right-click or long-press menu. `DropdownMenu` covers an explicit action menu | Later |
| Button dropdown / button group | Nav marks them planned and next. `DropdownMenu` already exists | Defer | Do not add a second menu. A group waits until a screen needs adjacent actions | Later |
| Menu bar, Dock menu, Home Screen quick actions, ornaments | None | Do not adopt | Desktop and platform chrome. The PWA uses the sidebar and top bar | None |
| Toolbars | Playground top bar | Preserve | Layout lock already keeps the top bar. Do not add a second toolbar component | None |
| Search fields | `SearchField` | Preserve | Presentational field with a clear control. Filtering stays with the caller. The searching decisions stay as already written | None |
| Sidebars | Playground sidebar and drawer | Preserve | Layout lock. Drawer rows are already 44px under 768px | None |
| Tab bars / tab views | `Tabs` | Preserve | `tablist` and `aria-selected` in source. Tests exist. This review did not rerun them | None |
| Path controls | `Breadcrumb` | Preserve | Current page uses `aria-current="page"`. Collapse exists for long trails | None |
| Token fields | None | Defer | Search stays a single field until a workflow needs editable tokens | Later |
| Sheets | `BottomSheet` and dialog sheet | Preserve | Modal `<dialog>`, Escape, backdrop, and close in source. Sheet tests exist. This review did not rerun them | None |
| Alerts | `ConfirmDialog` | Preserve | Critical confirm, including destructive copy on the dialog playground. Escape closed the open sample. Tab can leave the open dialog; that keyboard gap stays open | Medium |
| Popovers | `AnchoredPopover`, menus, tooltips | Preserve | Popover API with viewport placement. Menus and tooltips use elevation 3 | None |
| Panels | Inspector | Preserve | Layout lock. Supplementary, not a second modal system | None |
| Scroll views | Page and dialog body scroll | Preserve | No custom scroll component. Dialog long-content sample exists | None |
| Page controls | None | Do not adopt | No carousel or paged story | None |
| Windows | None | Do not adopt | One web document. Dialogs and sheets are the temporary views | None |
| Action sheets | `DropdownMenu` and `ConfirmDialog` | Do not adopt | A separate action sheet would duplicate the menu and the confirm dialog | None |
| Text fields | `Input` and `Field` | Preserve | Label, error, disabled, and `aria-invalid` exist. Single-line input stays under the content lock | None |
| Toggles | `Switch`, `Checkbox`, `Radio` | Preserve | Opposing or exclusive states. Switch, checkbox, and radio rows measured at least 44px at 390. No component test file for these three | Low |
| Segmented controls | `SegmentedControl` | Preserve | Mobile hit area is 44px. The visible option stays `calc(var(--control-height) - 6px)`. Adjacent segments do not overlap | None |
| Pickers | Date, range, time, and date-time | Preserve | Already locked. Their clear controls use the same mobile 44px hit extension as Search. The Time/Dialog lint gate stays separate | None |
| Sliders, steppers, rating indicators | None | Defer | No quantity, rating, or continuous value in the current product | Later |
| Combo boxes | `Select` searchable | Preserve | In-menu filter. Do not add a second combo component | None |
| Color wells, digit entry, image wells, virtual keyboards | None | Do not adopt | Palette choice is an app setting. Amounts and times use the existing fields. Photos stay deferred under the content lock | None |
| Progress indicators | `Button` loading and `Skeleton` | Defer | Indeterminate loading exists. A determinate bar waits until a task has a known duration | Later |
| Badges | `Badge` | Constraint | Status chip is correct. Remove stays 18×18. A 44×44 hit area would cover the label and the next badge, so it was not added | None |
| Empty states | `EmptyState` | Preserve | Title, description, and an optional action. Not an alert | None |
| Notifications | `Toast` | Preserve | In-app, non-modal. Danger uses `role="alert"` in the component standard. This is not an operating-system notification | None |
| Activity rings, gauges, rating indicators, status bars | None | Do not adopt | No fitness, sensor, or system-status surface | None |
| Widgets, Live Activities, complications, App Shortcuts, Top Shelf, Watch faces | None | Do not adopt | Platform shells outside the PWA | None |
| Help | `Tooltip` | Preserve | Focus shows the tip and sets `aria-describedby`. Escape hides it. A focused test covers that contract | None |

## Keyboard focus

Headless Chrome on the running playground, using real Tab and Shift+Tab key events. A mouse click does not count as the ring. The ring is the state after Shift+Tab returns.

| Control | Result | Evidence |
| --- | --- | --- |
| Button Save | PASS | Shift+Tab returned to Save. `:focus-visible` with a solid 2px outline |
| Input | PASS | Shift+Tab returned to the same field (`Label`). `:focus-visible`. The active field uses its primary border, `rgb(27, 117, 108)` on Mint, instead of the outer outline |
| Select | PASS | Shift+Tab returned to “Appearance, Stacked”. `:focus-visible`. The trigger sets `outline: none` and uses the inset primary band |
| Segmented control | PASS | Tab reached the Day radio. `:focus-visible` with a solid 2px outline |
| Tooltip | PASS | Shift+Tab returned to “More information” with a solid 2px outline. `aria-describedby` pointed at “Collapse is not available in this row” |
| Dialog | FAIL | The open dialog focused Continue. Tab then moved focus to the page body while the dialog stayed open. Shift+Tab returned to Continue with a solid 2px outline |

Light and Dark overflow was 0 at 390 on Button, Inputs, Selects, Segmented, Dialogs, and Advanced. Reduced motion set the segmented thumb transition to 0s. The focus token on Mint was `#1b756c`.

## Lock recommendation

The component decisions can be locked only with these exceptions written down. This review does not lock them.

- Badge remove stays 18×18. Future removal belongs on the parent row.
- The light dialog panel keeps its custom shadow.
- A real notched-phone safe area is not verified.
- Tab can leave an open dialog. That is an open defect, not an accepted pattern.
- The Time/Dialog ESLint gate stays open and is separate.

Do not build a context menu, token field, slider, stepper, progress bar, page control, action sheet, or system widget until a product screen names it.

## Coverage notes from source

Focused tests cover the segmented mobile hit area, the date and time clear extension, and the behavior of Checkbox, Radio, Switch, Tooltip, Card, Field, EmptyState, and Skeleton. Those tests passed.

The light dialog panel still uses its documented custom shadow. The notched-phone safe area remains not verified. The Time/Dialog ESLint gate remains open and is not part of this review.

## Unchanged

Identity, tokens, palettes, typography, density, radius, elevation, motion, icons, locked layout, and locked content. No Liquid Glass and no new library.
