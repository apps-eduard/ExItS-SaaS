# Layout and organization

Reviewed against [Apple’s Layout and Organization guidance](https://developer.apple.com/design/human-interface-guidelines/layout-and-organization) and the individual pages for boxes, collections, column views, disclosure controls, labels, lists and tables, lockups, outline views, split views, and tab views.

**LOCKED.** Selectable read-only text, one safe-area inset, and a 44px drawer row are part of the lock. A product list or table stays a future requirement. A real notched phone was not available, so that safe-area measurement is **NOT VERIFIED**. Do not treat that exception as a pass.

Apple’s native pieces that do not transfer: size classes as an API, Dynamic Island, tvOS focus grids, visionOS windows and ornaments, watchOS page tabs, and Liquid Glass scroll-edge controls. The applicable principles are reading order, grouping, progressive disclosure, the same tasks at every width, and keeping extra panes off a narrow screen.

## What exists

Verified in the Premium source, not assumed.

- The playground shell is a width-based split: sidebar and content from 768px, plus a 300px appearance inspector from 1180px (`Playground.module.scss`). Under 768px the sidebar is hidden, a top bar opens a section drawer, and the inspector is a dialog.
- Current location uses `aria-current="page"` on the sidebar item. The top bar title is the current section label. A breadcrumb sits above the page title.
- Sidebar groups can expand one level. The branch button sets `aria-expanded`, and the child group is indented.
- `Card` is the box: border, padding, optional eyebrow and title, elevation 1 by default.
- `Tabs` is a tab list with arrow keys, Home, and End. It is used for related content panes. `SegmentedControl` switches a setting, such as theme, radius, or density. It is not page navigation.
- The personal concept is one column: balance, three actions, search, one transaction row, and a primary action. The row title and note truncate with an ellipsis. The balance uses the locked numeric role. The row amount stays smaller and tabular.
- `EmptyState` and `Skeleton` exist. There is no table component, no collection view, no column browser, and no outline view.
- The viewport uses `viewport-fit=cover`. The shell applies `env(safe-area-inset-*)` once. The section drawer and the appearance dialog apply their own insets because they are in the top layer. Dialog sheets no longer add a second bottom inset on top of the stage inset. A bottom sheet keeps one bottom inset and adds the side insets.

## Width check

Headless Chrome, personal concept, stored appearance. No horizontal overflow means `scrollWidth` was not wider than the viewport.

| Width | Shell | Overflow |
| --- | --- | --- |
| 320 | Top bar. Sidebar and inspector hidden. Menu button 44×44. | No |
| 390 | Same as 320. | No |
| 768 | Sidebar 248px and content 520px. Top bar and inspector hidden. | No |
| 1024 | Sidebar 248px and content 776px. Inspector still hidden. | No |
| 1440 | Sidebar 248px, content 892px, inspector 300px. | No |

At 390px with document zoom at 200%, the personal concept still had no horizontal overflow. The transaction title’s overflow style was `ellipsis` at every width.

## Refinement check

Headless Chrome. Light and Dark. Personal concept unless noted.

| Check | 320 | 390 | 1440 |
| --- | --- | --- | --- |
| Light overflow | No | No | No |
| Dark overflow | No | No | No |
| Balance and row amount `user-select` | text | text | text |
| Button `user-select` | none | none | none |

The balance text `₱24,580.00` was selectable in both themes. The error “Choose a category.” on Selects is `user-select: text`. Buttons stay `user-select: none`. The time wheel scroller stays `user-select: none` so a drag does not select the numbers.

At 390px the section drawer opened in Light and Dark. Top-level rows and the nested “Button dropdown” row were 44px tall. The nested row kept its existing corner radius. Tab moved to a row inside the drawer. Escape closed the drawer. There was no horizontal overflow.

Safe-area padding computed to 0px in this Chrome because it has no notch. `viewport-fit=cover` is set, so a device inset can apply. A physical notch is **NOT VERIFIED**.

## Future product requirement

Do not build a list or table component yet. When Personal has real records, add one text list or table with succinct rows, a persistent current row when the row opens a detail, and column sort only if the columns are real attributes. Keep the sample transaction row until then.

## Decisions

| Apple topic | Current ExItS | Decision | Reason | Priority |
| --- | --- | --- | --- | --- |
| Boxes | `Card`, with a border, padding, and an optional title | Preserve | A card groups related content and stays smaller than the page. Nested boxes are not the pattern. | None |
| Collections | Swatch and foundation grids only | Do not adopt | Collections are for image sets. Text in Premium stays in rows. There is no image library to lay out. | None |
| Column views | Not built | Do not adopt | This is the macOS column browser. Personal data is not a deep folder hierarchy. | None |
| Disclosure controls | Sidebar branches only | Preserve | One expandable level, with a name and `aria-expanded`, matches the current nav. A second disclosure widget has no screen yet. | None |
| Labels | Type roles and text colors. Amounts, errors, and informational text can be selected | Preserve | Buttons and the time wheel do not select, so a tap or drag is unchanged. | None |
| Lists and tables | One sample transaction row. No table | Future requirement | Do not build it now. Add it when Personal has real records. | Later |
| Lockups | Not built | Do not adopt | Lockups are a tvOS focus pattern. Cards already cover grouped content. | None |
| Outline views | Not built | Do not adopt | This is a macOS hierarchical table. The sidebar is a short nav tree, not an outline. | None |
| Split views | Sidebar, content, and inspector | Preserve | Narrow widths drop the extra panes and keep the same sections. The inspector returns as a dialog. Fixed pane widths are enough for this shell. | None |
| Tab views | `Tabs` for panes, segmented control for settings | Preserve | Tabs are not app navigation. Segmented controls change one setting. Sentence case stays; Apple title case does not. | None |

## Layout principles

Preserve:

- Important content starts at the top: page title, then the section, then the actions.
- Related items share a card, a gap, or a nav group. Nav children are indented under their branch.
- Menus, sheets, and nav branches hide detail until it is needed.
- The same sections exist at every width. Only the number of visible panes changes.
- The visible menu button is 44×44 under 768px. Drawer rows are at least 44px. The shell, drawer, and overlays each carry one safe-area inset.

Do not adopt:

- Liquid Glass, a scroll-edge blur, or a mirrored background under the sidebar.
- A user-resizable splitter. The sidebar and inspector widths are fixed on purpose.
- Title-style tab labels. The writing review keeps sentence case.
- A change of features when the window gets wider.

The desktop sidebar rows stay at 2.5rem and nested rows at 2.25rem. Only the drawer under 768px uses 44px.

## Lock

Locked decisions:

- Mobile-first layout, with the current sidebar, top bar, drawer, inspector, and breakpoints.
- Cards, breadcrumbs, tabs, segmented controls, and the current navigation hierarchy.
- Text selection for amounts, errors, and informational content. Controls and the time wheel do not select.
- Drawer rows are at least 44px on screens under 768px.
- One safe-area inset per surface. Do not add a second inset on top of an existing one.
- Light and Dark keep the same layout.
- Do not build a list or table until a Personal screen has real records.

Known exception: safe-area inset on a real notched phone is **NOT VERIFIED**.
