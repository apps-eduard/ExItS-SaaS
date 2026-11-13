# Visual QA checklist

Review the running playground. A passing build is not a visual review.

## Widths

320, 375, 390, 768, 1024, 1440. Include light and dark, and all five primary palettes on Colors.

## Look

- No horizontal overflow (`scrollWidth` equals the viewport).
- Spacing, type, and icons match neighboring sections.
- Focus rings are visible.
- Colors shows Semantic (9 roles), Foundation, Neutral Scale, five theme palettes, and the 40-family reference library. Copy reports HEX or leaf OKLCH. See [UI/ExItS-Premium-Color-System.md](./UI/ExItS-Premium-Color-System.md).
- Elevation shows levels 0–4. The shadow on each sample is the elevation token.
- Text on solid fills stays readable in light and dark. Warning text stays dark on `#ff5722`.
- Dialogs, sheets, menus, tooltips, and toasts stay inside the viewport.
- Sheets can be closed without dragging.
- Reduced motion removes entrance animation.
- Normal motion is short and does not block the click.
- Icon names do not flash as text.

## Do not claim

Do not publish a 9 or 9.5 score from this checklist alone. Record what was screenshotted and what is still open.
