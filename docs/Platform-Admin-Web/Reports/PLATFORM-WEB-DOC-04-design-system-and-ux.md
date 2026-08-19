# PLATFORM-WEB-DOC-04 — Design System and UX Foundation Report

**Status:** Complete  
**Branch:** `docs/platform-admin-web-v2`  
**Prerequisite:** DOC-01, DOC-02, DOC-03

---

## 1. Delivered Capability

This package defines the ExItS design system and UX foundation for the future React-based Platform Admin Web (SaaS Control Center). It documents:

- **Existing visual foundation audit:** canonical DesignSystem tokens, current Admin/Web UI styling (Ant Design overrides identified and excluded from React replacement), brand asset inventory
- **Design philosophy:** 12 principles for a B2B SaaS control-plane experience (clean, calm, legible, efficient, data-dense, accessible)
- **Design token architecture:** full color palette (light/dark), spacing scale, typography scale with semantic roles, radius, elevation, motion duration/easing, and density tokens — all derived from the existing `ExItS.DesignSystem` canonical source
- **Typography/spacing/density rules:** page title hierarchy, body/label/caption roles, table typography conventions, spacing scale philosophy, compact vs comfortable density behavior
- **21 component patterns:** app shell, page header, section header, stat card, status badge, search/filter toolbar, data table, empty/zero-result/skeleton/error/forbidden states, confirmation/destructive dialogs, drawer, detail panel, form section, toast, audit timeline, key-value metadata view
- **Motion rules:** allowed/avoided motion categories, reduced-motion support
- **Accessibility target:** WCAG 2.2 AA as design intent with specific requirements for keyboard, focus, labels, contrast, form errors, tables, and dialog focus management
- **Theming:** light/dark/system support with shared semantic tokens
- **Responsive foundation:** desktop-first with four breakpoint tiers and explicit behavior rules

## 2. Evidence Sources

- `src/Shared/ExItS.DesignSystem/wwwroot/exits-design-system.css` — canonical `--exits-*` token definitions, component classes, responsive rules, density modes, motion tokens
- `src/Platform/ExItS.Platform.Admin/wwwroot/app.css` — current Ant Design Admin overrides (excluded from React replacement)
- `src/Shared/ExItS.Web.UI/wwwroot/exits-web.css` — shared web host chrome (excluded)
- `docs/engineering/ui-design-system.md` — authoritative UI architecture and product UI decisions

## 3. Key Decisions

- React replacement uses the canonical green-based ExItS brand (`#166534` light / `#4ade80` dark), not the Ant Design blue overrides from the current Admin
- Tailwind CSS + shadcn/ui will consume `--exits-*` tokens at implementation time
- Compact density is the default for administrative data views
- WCAG 2.2 AA is the accessibility design target (not a compliance claim)
- Motion is restrained and functional; `prefers-reduced-motion` fully honored

## 4. Exclusions

- No CSS implementation files created
- No frontend scaffold or package files
- No existing Admin or DesignSystem code edits
- No backend or PLM changes
- No new logo or brand assets invented
