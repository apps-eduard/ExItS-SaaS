# Content

Reviewed against [Apple’s Content guidance](https://developer.apple.com/design/human-interface-guidelines/content) and the individual pages for charts, image views, text views, and web views. Inspection only. No component, token, or screen was changed.

This is not locked.

Universal principles that apply: show the meaning of data without relying on color alone, use a symbol for an icon and a button for a clickable image, keep long text readable and selectable, and use a single-line field for a short value. Apple-only pieces that do not transfer: Swift Charts, Audio Graphs, SF Symbols, image wells, and WKWebView.

## What exists

Verified in the Premium source.

- There is no chart component and no chart library. Money is a numeric balance and one sample transaction row.
- There is no image view, photo, or avatar image. Icons are Material Symbols Rounded through `ExIcon`. An icon with a `label` is named. An icon without a label is `aria-hidden`. The brand mark and the sample transaction initial are letters, not images. The only image file is `public/favicon.svg`, used as the browser icon.
- `Input` is a single-line field. The sample note is `Input` with the placeholder “Optional”. Descriptions, hints, and errors are paragraphs. Amounts, errors, and informational text use `user-select: text`. Buttons and the time wheel do not.
- There is no iframe, embedded HTML, or external web view. Navigation stays inside the playground.

## Width check

Headless Chrome, personal concept, Light and Dark.

| Width | Theme | Page overflow | Intro |
| --- | --- | --- | --- |
| 390 | Light and Dark | No | Wraps. The paragraph does not overflow itself. |
| 1440 | Light and Dark | No | Fits one line. It does not overflow. |

No `img` or `iframe` was in the document. The balance stays selectable. Supporting text was `rgb(75, 85, 99)` on `rgb(245, 246, 248)` in Light (6.99:1) and `rgb(203, 213, 225)` on `rgb(15, 23, 42)` in Dark (12.02:1). On Advanced at 390px the note control is an `input` of type `text`.

## Decisions

| Apple principle | Existing ExItS | Decision | Reason | Priority |
| --- | --- | --- | --- | --- |
| Charts | No chart. A balance and one transaction row | Defer | A chart needs a real series. Do not add a library or a chart component before that. | Later |
| Image views | `ExIcon` and letter marks. No photo component | Do not adopt | Icons are symbols. A clickable mark is already a button. An image view waits until a product photo exists. | None |
| Text views | Single-line `Input`. Paragraphs for descriptions and errors | Preserve | The sample note is one short line. Paragraphs wrap, scroll with the page, and can be selected. | None |
| Web views | No embedded page | Do not adopt | Nothing in Premium is an external document. A link is enough if a page must open elsewhere. | None |

## Strengths

- Financial figures use the locked numeric role and can be selected.
- Errors and supporting text are readable in Light and Dark without a second type scale.
- Icons do not pretend to be photos, and decorative icons are hidden from assistive technology.
- The page does not embed another site.

## Future requirements

- A chart, only when Personal has a series to explain. Lead with a sentence that states the result. Label the axes. Do not use color as the only difference between series. Provide a text summary for assistive technology. Do not require a drag to learn the main point.
- A multiline note, only when a note is longer than one line. Keep `Input` for a short value.
- A photo or avatar, only when the product has an image. Give it a fixed aspect ratio, a text fallback, and an accessible name. If it performs an action, use a button.

## Unchanged

Palettes, semantic colors, typography, spacing, density, radius, icons, inputs, pickers, navigation, and the locked layout. No Liquid Glass, chart library, image component, rich-text editor, or web view.

## Approval before any build

1. Which financial series, if any, deserves a chart.
2. Whether any note is long enough to leave the single-line field.
3. Whether any screen needs a photo rather than an icon or a letter.

Until those are named, do not add the component.
