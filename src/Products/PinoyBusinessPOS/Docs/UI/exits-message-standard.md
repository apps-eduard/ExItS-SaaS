# ExItS Message Standard

**Status:** APPROVED / LOCKED
**Scope:** `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.React`

**Canonical product components:**

| Concern | Component | Path |
|---------|-----------|------|
| Inline / contextual notice | `Notice` | `components/exits/Notice.tsx` |
| Transient toast | `ToastProvider` / `useExitsToast` | `components/exits/ToastProvider.tsx` |

**Visual authority:** `/ui-standards` → **Messages** → **Gallery**
(`ExItS.PinoyBusinessPOS.React/src/features/ui-standards/UiStandardsMessageGallery.tsx`)

**Locked visual authority:** the **Diamond PrimeNG Message Full sample** layout (Toast, Severity, Inline, Custom, Expanded, Action, Message filled / outlined / simple).

Reference: https://diamond.primeng.dev/uikit/message

That Full sample **is** the ExItS Message visual standard for feedback coverage. Product pages **MUST** reuse `Notice` and `useExitsToast` — do **not** invent page-local colored message / alert / banner chrome when those APIs can express the need.

This standard evolves with the shared component contract. Do **not** pin the standard permanently to a commit SHA. Reference the component paths and `/ui-standards` → Messages instead.

---

## Product APIs vs gallery

| Layer | Role |
|-------|------|
| **Diamond Full sample cards** (UI Standards gallery) | Reference layout of message treatments: toast triggers, severity rows, inline / custom / expanded / action, filled / outlined / simple |
| **`Notice`** | Canonical **inline** contextual message on product pages (info / warning / danger / success) |
| **`useExitsToast`** | Canonical **transient** toast on product pages (success / info / warning / error) |

Gallery markup may use showcase severity classes for visual parity with Diamond. **Product chrome keeps ExItS Notice / Toast APIs and semantic tokens** (`--exits-info`, `--exits-success`, `--exits-warning`, `--exits-danger`).

### Severity colors

Gallery Secondary / Contrast and Diamond severity fills are part of the **locked Message visual standard**. Product **Notice** tones remain `info` / `success` / `warning` / `danger` (map Warn→warning, Error→danger). Product **Toast** remains success / info / warning / error.

Do not invent page-local Secondary / Contrast notice chrome — express Secondary as Neutral-style guidance via `Notice` tone `info` or quiet copy; Contrast is gallery / toast-demo only unless a future Notice tone is authorized.

---

## When to use which

| Need | Use |
|------|-----|
| Persistent contextual guidance / form feedback on the page | `Notice` |
| Brief confirmation after an action | `useExitsToast` (success / info / warning / error) |
| Destructive / status confirmation | `ConfirmActionDialog` (see [EXITS_UI_STANDARD.md](./EXITS_UI_STANDARD.md)) — not Message gallery chrome |

Do not invent custom colored toast or notice markup on feature pages. `showToast` remains for legacy callers.

---

## Notice tones (locked)

| Tone | Meaning | Tokens |
|------|---------|--------|
| **info** | Informational / guidance | `--exits-info` |
| **success** | Positive confirmation in-page | `--exits-success` |
| **warning** | Caution / reversible risk | `--exits-warning` |
| **danger** | Error / severe / blocking | `--exits-danger` |

`Notice` is not an EmptyState and not a page-level ErrorState. Prefer title + short body; optional action slot is permission-gated by the caller.

---

## Toast variants (locked)

See [EXITS_UI_STANDARD.md](./EXITS_UI_STANDARD.md) → Toast.

```ts
const toast = useExitsToast();
toast.success("Changes saved successfully.");
toast.info("Quotation saved as draft.");
toast.warning("Check is pending clearing.");
toast.error("Payment could not be recorded.");
```

---

## Gallery mapping (UI Standards)

| Diamond / Prime section | ExItS product mapping |
|-------------------------|------------------------|
| Toast | `useExitsToast` |
| Severity | Toast / Notice tone rows (showcase colors in gallery) |
| Inline / Custom / Expanded / Action | Patterns expressible with `Notice` (title, body, action) |
| Message (filled) | Soft tint + border — closest to default `Notice` |
| Outlined | Lower-emphasis bordered treatment (prefer `Notice`; do not fork) |
| Simple | Minimal text treatment — use sparingly; prefer `Notice` for product |

Map Diamond labels to ExItS APIs. Do **not** add Prime-only message variants to product components.

---

## UI Standards page

`/ui-standards` → **Messages** shows the locked Full sample gallery. Separate **Toasts** card exercises the live `useExitsToast` API.

Locking this gallery does **not** require rewriting existing product notices/toasts. Migrate call sites incrementally.

---

## Explicit prompt overrides

1. Explicit task instruction
2. ExItS Message Standard (this document)
3. Existing page presentation

Domain correctness and accessibility always remain mandatory.

---

## Related

- [EXITS_UI_STANDARD.md](./EXITS_UI_STANDARD.md) — Toast / Confirm / states overview
- [exits-button-standard.md](./exits-button-standard.md) — gallery toast-trigger buttons reuse shared `Button`
- [exits-motion-standard.md](./exits-motion-standard.md) — toast motion respects reduced motion
