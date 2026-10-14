# ExItS Toggle Standard

**Status:** APPROVED / LOCKED
**Scope:** `src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.React`
**Visual authority:** `/ui-standards` → **Toggle**
(`ExItS.PinoyBusinessPOS.React/src/features/ui-standards/`)

**Locked global command:** the Toggle playground on `/ui-standards` → **Toggle**. New toggles are requested with `ext toggle`. That command is the ExItS Toggle standard to use.

This standard evolves with the Toggle playground. Do **not** pin it permanently to a commit SHA. Reference `/ui-standards` → Toggle and this document.

Locking this standard does **not** restyle the product `Switch`. Existing switches stay in place until a control is explicitly migrated. Radio and checkbox stay on their own samples. Buttons stay on [exits-button-standard.md](./exits-button-standard.md).

---

## Model (locked)

Intent, variety, and appearance are **separate axes**. They compose.

```
Toggle
├─ Intent                 (color)
│  ├─ Standard            (gray #9e9e9e, same as checkbox Standard)
│  ├─ Primary
│  ├─ Secondary
│  ├─ Success
│  ├─ Info
│  ├─ Amber
│  ├─ Danger
│  ├─ Warn
│  ├─ Help
│  └─ Black
│
├─ Variety
│  ├─ Standard            (Quasar soft toggle)
│  ├─ Solid
│  └─ Outline
│
├─ Appearance
│  ├─ Standard
│  ├─ Soft
│  ├─ Soft outline
│  ├─ Outline
│  ├─ Raised
│  ├─ Glossy
│  ├─ Gradient
│  └─ Flat
│
├─ Icon                   (same set as the button playground)
├─ Density                (Tight · Compact · Standard · Comfort · Relaxed)
└─ Disabled
```

Standard intent stays gray in light mode and in Force dark mode. Black uses a light treatment in dark mode so the control stays visible.

## Command

Defaults are omitted. `ext toggle` is Standard intent, Standard variety, Standard appearance, Standard density, no icon, enabled.

```
ext toggle [intent] [variety] [appearance] [density] [icon] [disabled]
```

Example: `ext toggle primary solid raised comfort check`

## Samples

Standard, Soft, Soft outline, Standard with icons, Solid, Solid soft, Solid soft outline, Outline, Solid with icons, Solid with label, and Toggle order. Samples can be clicked. The playground preview is a card and can be clicked. Reset is a pill icon button, and the theme icon switches light and dark for the whole app.
