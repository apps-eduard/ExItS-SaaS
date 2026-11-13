# Responsive standards

Layout is mobile-first. Use the existing breakpoints in `Playground.module.scss`. Do not add a second responsive system.

| Width | Playground |
| --- | --- |
| under 768px | Top bar, drawer navigation, appearance dialog |
| 768–1179px | Sidebar, appearance button in the sidebar |
| 1180px and up | Sidebar, content, appearance inspector |

## Rules

- Content uses `minmax(0, 1fr)` and `max-width: 100%` so a flex child cannot widen the page.
- Desktop control heights are 34, 40, and 52. Under 768px, Compact and Standard controls grow to at least 44px. The switch track stays at its density size, and the switch control is the touch target.
- Dialogs use `min(28rem, calc(100vw - 2rem))`.
- Bottom sheets use `min(40rem, 100%)`, stick to the bottom, and scroll their body.
- Menus and tooltips reposition inside an 8px viewport margin.
- Check 320, 375, 390, 768, 1024, and 1440. Confirm `scrollWidth` equals the viewport width.
